using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed record PresetSaveDestination(byte WireIndex, string Name, string RawDetailPayloadHex);
public sealed class PresetSavePreconditionException(string message) : IOException(message);

public sealed partial class F15ReadSession
{
    /// <summary>Read the exact destination that will be shown in the overwrite confirmation. No recall or save.</summary>
    public Task<PresetSaveDestination> ReadPresetSaveDestinationAsync(CurrentPresetSnapshot expected, byte destination, CancellationToken ct)
        => RunWorkflowAsync(async token => {
            var identity = await ReadIdentityCoreAsync(token);
            RequireSaveIdentity(identity);
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireSaveSnapshot(before, expected);
            PresetSaveCommand.ValidateSource(identity.Firmware, Convert.FromHexString(before.RawStatePayloadHex), Convert.FromHexString(before.RawDetailPayloadHex));
            var result = await ReadSaveDestinationCoreAsync(destination, token);
            RequireSaveSnapshot(await ReadCurrentPresetCoreAsync(token), before);
            return result;
        }, ct);

    /// <summary>Save live content once, then recall the chosen slot and verify every byte. Never replay on failure.</summary>
    public Task<CurrentPresetSnapshot> SavePresetAsync(CurrentPresetSnapshot expected, PresetSaveDestination destination, string name, CancellationToken ct)
        => RunWorkflowAsync(async token => {
            ArgumentNullException.ThrowIfNull(destination);
            var retainedDestination = PresetDataParser.Parse(Convert.FromHexString(destination.RawDetailPayloadHex));
            if (retainedDestination.WireIndex != destination.WireIndex || retainedDestination.Name != destination.Name)
                throw new ArgumentException("The confirmed destination is inconsistent.", nameof(destination));
            var identity = await ReadIdentityCoreAsync(token);
            RequireSaveIdentity(identity);
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireSaveSnapshot(before, expected);
            var state = Convert.FromHexString(before.RawStatePayloadHex);
            var detail = Convert.FromHexString(before.RawDetailPayloadHex);
            var request = PresetSaveCommand.Create(identity.Firmware, state, detail, destination.WireIndex, name);
            var freshDestination = await ReadSaveDestinationCoreAsync(destination.WireIndex, token);
            if (freshDestination != destination)
                throw new PresetSavePreconditionException("The destination changed after confirmation. Nothing was saved; review it again.");
            RequireSaveSnapshot(await ReadCurrentPresetCoreAsync(token), before);

            var response = await QueryAsync(request, p => p.Length > 0 && p[0] == 0x30, token);
            if (!response.AsSpan().SequenceEqual(new byte[] { 0x30, 0x01 }))
                throw new IOException("Save response differs from the verified 30 01. Stored outcome is unknown; do not retry automatically.");
            var expectedState = PresetSaveCommand.ExpectedState(state, destination.WireIndex);
            var expectedDetail = PresetSaveCommand.ExpectedDetail(detail, destination.WireIndex, name);
            var recall = expectedState.ToArray(); recall[0] = 0x13;
            var recalled = await QueryAsync(recall, p => p.Length > 0 && p[0] == 0x14, token);
            if (!recalled.AsSpan().SequenceEqual(new byte[] { 0x14, 0x01 }))
                throw new IOException("Post-save recall response differs. Stored outcome is unknown; read the amplifier before another action.");
            var after = await ReadCurrentPresetCoreAsync(token);
            if (after.RawStatePayloadHex != Convert.ToHexString(expectedState) || after.RawDetailPayloadHex != Convert.ToHexString(expectedDetail))
                throw new IOException("The saved destination does not exactly match the requested content and name. Do not repeat the save automatically.");
            return after;
        }, ct);

    private static void RequireSaveIdentity(F15Identity identity)
    {
        if (identity.Firmware != "V1.1.9" || identity.PresetSlots != 80)
            throw new NotSupportedException("Preset saving is verified on F15i V1.1.9 with 80 preset slots only.");
    }

    private static void RequireSaveSnapshot(CurrentPresetSnapshot actual, CurrentPresetSnapshot expected)
    {
        if(actual.WireIndex!=expected.WireIndex || actual.Name!=expected.Name || actual.RawStatePayloadHex!=expected.RawStatePayloadHex || actual.RawDetailPayloadHex!=expected.RawDetailPayloadHex)
            throw new PresetSavePreconditionException("The amplifier changed since the snapshot. Nothing was saved; refresh before trying again.");
    }

    private async Task<PresetSaveDestination> ReadSaveDestinationCoreAsync(byte destination, CancellationToken ct)
    {
        if (destination is < 1 or > 80) throw new ArgumentOutOfRangeException(nameof(destination));
        var payload = await QueryAsync(new byte[] { 0x21, destination }, p => p.Length >= 22 && p[0] == 0x22 && p[1] == destination, ct);
        var decoded = PresetDataParser.Parse(payload);
        return new(destination, decoded.Name, Convert.ToHexString(payload));
    }
}
