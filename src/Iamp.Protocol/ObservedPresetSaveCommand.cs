using System.Security.Cryptography;

namespace Iamp.Protocol;

/// <summary>
/// Save envelopes observed on V1.1.9 at slot 4, with original content or only
/// Noise Killer Threshold changed to 26. Renaming/other slots are unsupported.
/// </summary>
public static class ObservedPresetSaveCommand
{
    private const string StateSha256 = "BC565805361362423342FDD441FAC9FDB6B2CDB1B2B55F9617A30FA7F12ED5C5";

    public static byte[] CreateForObservedThreshold(string firmware,ReadOnlySpan<byte> state,ReadOnlySpan<byte> detail)
    {
        var decoded=PresetDataParser.Parse(detail);
        var block=decoded.Blocks[0];
        if(block.Position!=1 || block.TypeCode!=4 || block.Values.Count!=6 || block.Values[2] is not (25 or 26))
            throw new NotSupportedException("Saving is demonstrated only for original slot-4 content with Threshold 25 or 26.");
        var normalized=PresetDataParser.WithParameterValue(detail,1,2,25);
        // The complete normalized hash prevents unrelated edits, name changes,
        // reordered records and altered unknown bytes from passing this guard.
        return CreateUnchanged(firmware,state,normalized);
    }

    public static byte[] CreateUnchanged(string firmware, ReadOnlySpan<byte> state, ReadOnlySpan<byte> detail)
    {
        if(firmware!="V1.1.9" || state.Length!=92 || state[0]!=0x12 || state[1]!=4)
            throw new NotSupportedException("Only the captured V1.1.9 unchanged slot-4 save envelope is supported offline.");
        if(Convert.ToHexString(SHA256.HashData(state))!=StateSha256)
            throw new NotSupportedException("Current state differs from the observed save baseline.");
        PresetSelectionCommand.RequireObservedPreset(4,detail);
        var decoded=PresetDataParser.Parse(detail);
        if(decoded.WireIndex!=4 || decoded.Name!="British Lead Solo")
            throw new NotSupportedException("Save destination and name must match the unchanged captured preset.");

        var request=new byte[22];request[0]=0x2F;request[1]=4;
        // Copy the observed 20 raw name bytes, including padding. Do not infer a
        // general encoder or silently truncate names from one ASCII observation.
        detail.Slice(2,20).CopyTo(request.AsSpan(2));
        return request;
    }
}
