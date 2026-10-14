using Iamp.Protocol;
using System.Text;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Iamp.Transport.Windows;

/// <summary>Captured queries and restricted V1.1.9 controls. Complete workflows are serialized within each session.</summary>
public sealed class F15ReadSession : IAsyncDisposable
{
    private readonly BluetoothLEDevice device;
    private readonly GattDeviceService service;
    private readonly GattCharacteristic notify;
    private readonly GattCharacteristic write;
    private readonly CommandSession commands;
    private readonly WorkflowQueue workflows = new();
    private int disposed;
    private readonly DiagnosticJournal? diagnostics;

    private F15ReadSession(BluetoothLEDevice device, GattDeviceService service,
        GattCharacteristic notify, GattCharacteristic write, DiagnosticJournal? diagnostics)
    {
        this.device = device; this.service = service; this.notify = notify; this.write = write;
        this.diagnostics = diagnostics;
        commands = new CommandSession(async (payload, ct) =>
        {
            var result = await write.WriteValueWithResultAsync(
                CryptographicBuffer.CreateFromByteArray(FrameCodec.Encode(payload)),
                GattWriteOption.WriteWithResponse).AsTask(ct);
            Require(result.Status, "Amplifier command");
        }, diagnostics: diagnostics);
        notify.ValueChanged += OnValueChanged;
        device.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    public string DeviceName => device.Name;

    public static async Task<F15ReadSession> ConnectAsync(ulong address, CancellationToken ct, DiagnosticJournal? diagnostics = null)
    {
        var began = Stopwatch.GetTimestamp();
        diagnostics?.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Started);
        try
        {
            var session = await ConnectCoreAsync(address, ct, diagnostics);
            diagnostics?.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Completed,
                (long)Stopwatch.GetElapsedTime(began).TotalMilliseconds);
            return session;
        }
        catch (Exception ex)
        {
            diagnostics?.Record(DiagnosticOperation.Connection, DiagnosticJournal.OutcomeFor(ex),
                (long)Stopwatch.GetElapsedTime(began).TotalMilliseconds);
            throw;
        }
    }

    private static async Task<F15ReadSession> ConnectCoreAsync(ulong address, CancellationToken ct, DiagnosticJournal? diagnostics)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(ct);
        if (device is null)
        {
            // A known address may not yet exist in Windows' discovery cache after startup.
            // This retries opening only, before any service subscription or application write.
            await F15DeviceDiscovery.WaitForAdvertisementAsync(address, ct);
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(ct);
        }
        if (device is null) throw new IOException("Windows could not open the selected amplifier after discovery.");
        GattDeviceService? chosen = null;
        F15ReadSession? session = null;
        try
        {
            var discovery = await device.GetGattServicesForUuidAsync(Uuid(0xFFF0), BluetoothCacheMode.Uncached).AsTask(ct);
            Require(discovery.Status, "F15 service discovery");
            if (discovery.Services.Count != 1)
            {
                foreach (var item in discovery.Services) item.Dispose();
                throw new IOException("Expected one FFF0 service on the selected amplifier.");
            }
            chosen = discovery.Services[0];
            var chars = await chosen.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask(ct);
            Require(chars.Status, "Characteristic discovery");
            var notify = chars.Characteristics.Single(c => c.Uuid == Uuid(0xFFF2));
            var write = chars.Characteristics.Single(c => c.Uuid == Uuid(0xFFF3));
            if (!notify.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) ||
                !write.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                throw new IOException("The discovered characteristics do not match the captured F15 transport.");
            session = new F15ReadSession(device, chosen, notify, write, diagnostics);
            Require(await notify.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(ct), "Notification subscription");
            if (device.ConnectionStatus != BluetoothConnectionStatus.Connected)
                throw new IOException("The amplifier disconnected during session initialization.");
            session.commands.ThrowIfUnavailable();
            return session;
        }
        catch
        {
            if (session is not null)
            {
                session.commands.Dispose();
                session.workflows.Dispose();
                session.notify.ValueChanged -= session.OnValueChanged;
                session.device.ConnectionStatusChanged -= session.OnConnectionStatusChanged;
            }
            chosen?.Dispose(); device.Dispose(); throw;
        }
    }

    public Task<F15Identity> ReadIdentityAsync(CancellationToken ct)
        => RunWorkflowAsync(ReadIdentityCoreAsync, ct);

    private async Task<F15Identity> ReadIdentityCoreAsync(CancellationToken ct)
    {
        var payload = await QueryAsync(new byte[] { 0x01, 0x01 }, p => p.Length >= 3 && p[0] == 0x02, ct);
        if (payload.Length != 16 || payload[9] != 0 || payload[10] != 5)
            throw new FormatException("Identity schema differs from captured firmware; retain raw evidence before supporting this device.");
        return new F15Identity(payload[1], payload[2], Encoding.ASCII.GetString(payload, 3, 6),
            payload[11], payload[12], payload[13], payload[14], payload[15], Convert.ToHexString(payload));
    }

    public Task<IReadOnlyList<PresetName>> ReadPresetNamesAsync(CancellationToken ct)
        => RunWorkflowAsync(ReadPresetNamesCoreAsync, ct);

    private async Task<IReadOnlyList<PresetName>> ReadPresetNamesCoreAsync(CancellationToken ct)
    {
        var names = new List<PresetName>();
        foreach (var (start, end) in new[] { (1, 40), (41, 80) })
        {
            var response = await QueryAsync(new byte[] { 0x29, (byte)start, (byte)end },
                p => p.Length >= 3 && p[0] == 0x2A && p[1] == start && p[2] == end, ct);
            if (response.Length != 3 + 20 * (end - start + 1))
                throw new FormatException("Preset-name response differs from the captured 20-byte records.");
            for (var index = start; index <= end; index++)
            {
                var text = response.AsSpan(3 + (index - start) * 20, 20);
                var zero = text.IndexOf((byte)0);
                names.Add(new PresetName(index, Encoding.UTF8.GetString(zero < 0 ? text : text[..zero])));
            }
        }
        return names;
    }

    public Task<CurrentPresetSnapshot> ReadCurrentPresetAsync(CancellationToken ct)
        => RunWorkflowAsync(ReadCurrentPresetCoreAsync, ct);

    private async Task<CurrentPresetSnapshot> ReadCurrentPresetCoreAsync(CancellationToken ct)
    {
        var state = await QueryAsync(new byte[] { 0x11 }, p => p.Length >= 2 && p[0] == 0x12, ct);
        CurrentPresetReadGuard.ValidateState(state);
        var index = state[1];
        // Only query the already-selected preset; other-slot query side effects are not yet established.
        var detail = await QueryAsync(new byte[] { 0x21, index }, p => p.Length >= 22 && p[0] == 0x22 && p[1] == index, ct);
        var after = await QueryAsync(new byte[] { 0x11 }, p => p.Length >= 2 && p[0] == 0x12, ct);
        var decoded = CurrentPresetReadGuard.Validate(state, detail, after);
        return new CurrentPresetSnapshot(index, decoded.Name, Convert.ToHexString(state), Convert.ToHexString(detail), decoded.Blocks);
    }

    /// <summary>One observed live effect update, followed by complete current-preset read-back. Does not save.</summary>
    public Task<CurrentPresetSnapshot> SetNoiseKillerThresholdAsync(CurrentPresetSnapshot expected, ushort threshold, CancellationToken ct)
        => SetObservedParameterAsync(expected, ObservedParameter.NoiseThreshold, threshold, ct);

    public Task<CurrentPresetSnapshot> SetNoiseKillerLevelAsync(CurrentPresetSnapshot expected, ushort level, CancellationToken ct)
        => SetObservedParameterAsync(expected, ObservedParameter.NoiseLevel, level, ct);

    public Task<CurrentPresetSnapshot> Set808GainAsync(CurrentPresetSnapshot expected, ushort gain, CancellationToken ct)
        => SetObservedParameterAsync(expected, ObservedParameter.OdGain, gain, ct);

    public Task<CurrentPresetSnapshot> Set808VolumeAsync(CurrentPresetSnapshot expected, ushort volume, CancellationToken ct)
        => SetObservedParameterAsync(expected, ObservedParameter.OdVolume, volume, ct);

    private Task WriteObservedUpdateAsync(byte[] payload, CancellationToken ct)
        => commands.WriteAsync(payload, ct);

    /// <summary>Restricted unsaved tone edit on the captured second-position 808 model, with full read-back.</summary>
    public Task<CurrentPresetSnapshot> Set808ToneAsync(CurrentPresetSnapshot expected, ushort tone, CancellationToken ct)
        => SetObservedParameterAsync(expected, ObservedParameter.OdTone, tone, ct);

    private Task<CurrentPresetSnapshot> SetObservedParameterAsync(CurrentPresetSnapshot expected, ObservedParameter parameter, ushort value, CancellationToken ct)
        => RunWorkflowAsync(token => SetObservedParameterCoreAsync(expected, parameter, value, token), ct);

    private async Task<CurrentPresetSnapshot> SetObservedParameterCoreAsync(CurrentPresetSnapshot expected, ObservedParameter parameter, ushort value, CancellationToken ct)
    {
        var identity = await ReadIdentityCoreAsync(ct);
        if (identity.Firmware != "V1.1.9") throw new NotSupportedException("Parameter editing is verified only on F15i V1.1.9.");
        var before = await ReadCurrentPresetCoreAsync(ct);
        if (before.WireIndex != expected.WireIndex || before.RawDetailPayloadHex != expected.RawDetailPayloadHex ||
            before.RawStatePayloadHex != expected.RawStatePayloadHex)
            throw new IOException("The amplifier changed since the snapshot. Read it again before editing.");
        var definition = ObservedParameterCommands.Definition(parameter);
        var block = before.Blocks[definition.Position - 1];
        var request = ObservedParameterCommands.Create(block, parameter, value);
        if (block.Values[definition.ValueIndex] == value) return before;
        await WriteObservedUpdateAsync(request, ct);
        var after = await ReadCurrentPresetCoreAsync(ct);
        var expectedDetail = PresetDataParser.WithParameterValue(Convert.FromHexString(before.RawDetailPayloadHex), definition.Position, definition.ValueIndex, value);
        if (after.WireIndex != before.WireIndex || after.RawStatePayloadHex != before.RawStatePayloadHex ||
            after.RawDetailPayloadHex != Convert.ToHexString(expectedDetail))
            throw new IOException("Parameter read-back differs from the requested change. Read the amplifier before another operation; nothing was saved.");
        return after;
    }

    /// <summary>Recall one of the two captured slots. Preserves all other whole-state fields; does not save presets.</summary>
    public Task<CurrentPresetSnapshot> SelectObservedPresetAsync(CurrentPresetSnapshot expected, byte targetIndex, CancellationToken ct)
        => RunWorkflowAsync(token => SelectPresetCoreAsync(expected, targetIndex, true, token), ct);

    public Task<CurrentPresetSnapshot> SelectPresetAsync(CurrentPresetSnapshot expected, byte targetIndex, CancellationToken ct)
        => RunWorkflowAsync(token => SelectPresetCoreAsync(expected, targetIndex, false, token), ct);

    private async Task<CurrentPresetSnapshot> SelectPresetCoreAsync(CurrentPresetSnapshot expected, byte targetIndex, bool observedOnly, CancellationToken ct)
    {
        var identity = await ReadIdentityCoreAsync(ct);
        if (identity.Firmware != "V1.1.9") throw new NotSupportedException("Preset selection is verified only on F15i V1.1.9.");
        var before = await ReadCurrentPresetCoreAsync(ct);
        if (before.WireIndex != expected.WireIndex || before.RawDetailPayloadHex != expected.RawDetailPayloadHex ||
            before.RawStatePayloadHex != expected.RawStatePayloadHex)
            throw new IOException("The amplifier changed since the snapshot. Read it again before selecting a preset.");
        if (identity.PresetSlots != 80) throw new NotSupportedException("Preset recall requires the observed 80-slot capacity.");
        var request = observedOnly ? PresetSelectionCommand.Create(Convert.FromHexString(before.RawStatePayloadHex), targetIndex) :
            PresetSelectionCommand.CreateForAnySlot(Convert.FromHexString(before.RawStatePayloadHex), targetIndex);
        if (observedOnly) PresetSelectionCommand.RequireObservedPreset(before.WireIndex, Convert.FromHexString(before.RawDetailPayloadHex));
        if (before.WireIndex == targetIndex) return before;
        await WriteObservedUpdateAsync(request, ct);
        var after = await ReadCurrentPresetCoreAsync(ct);
        request[0] = 0x12;
        if (after.WireIndex != targetIndex || after.RawStatePayloadHex != Convert.ToHexString(request))
            throw new IOException("Preset selection or preserved global state could not be confirmed. Read the amplifier before another operation.");
        if (observedOnly) PresetSelectionCommand.RequireObservedPreset(after.WireIndex, Convert.FromHexString(after.RawDetailPayloadHex));
        return after;
    }

    /// <summary>Explicit same-slot save for the demonstrated slot-4 Threshold 25/26 profiles.
    /// A response is followed by same-slot recall and full verification. Never replay on failure.</summary>
    public Task<CurrentPresetSnapshot> SaveObservedThresholdAsync(CurrentPresetSnapshot expected,CancellationToken ct)
        => RunWorkflowAsync(token => SaveObservedThresholdCoreAsync(expected, token), ct);

    private async Task<CurrentPresetSnapshot> SaveObservedThresholdCoreAsync(CurrentPresetSnapshot expected,CancellationToken ct)
    {
        var identity=await ReadIdentityCoreAsync(ct);
        var before=await ReadCurrentPresetCoreAsync(ct);
        if(before.WireIndex!=expected.WireIndex || before.RawStatePayloadHex!=expected.RawStatePayloadHex || before.RawDetailPayloadHex!=expected.RawDetailPayloadHex)
            throw new IOException("The amplifier changed since the snapshot. Nothing was sent to save; read it again.");
        var state=Convert.FromHexString(before.RawStatePayloadHex);
        var request=ObservedPresetSaveCommand.CreateForObservedThreshold(identity.Firmware,state,Convert.FromHexString(before.RawDetailPayloadHex));
        var response=await QueryAsync(request,p=>p.Length>=1 && p[0]==0x30,ct);
        if(!response.AsSpan().SequenceEqual(new byte[]{0x30,0x01}))
            throw new IOException("Save response is not the observed 30 01. Stored outcome is unknown; do not retry automatically.");
        // Match the phone's post-save same-slot recall. This checks the stored
        // content without broadening selection to another destination.
        state[0]=0x13;
        var recall=await QueryAsync(state,p=>p.Length>=1 && p[0]==0x14,ct);
        if(!recall.AsSpan().SequenceEqual(new byte[]{0x14,0x01}))
            throw new IOException("Post-save recall response differs. Stored outcome is unknown; read the amplifier.");
        var after=await ReadCurrentPresetCoreAsync(ct);
        if(after.WireIndex!=before.WireIndex || after.RawStatePayloadHex!=before.RawStatePayloadHex || after.RawDetailPayloadHex!=before.RawDetailPayloadHex)
            throw new IOException("Saved preset did not match the complete expected readback. Do not repeat the save; inspect the actual preset.");
        return after;
    }

    private Task<byte[]> QueryAsync(byte[] request, Func<byte[], bool> match, CancellationToken ct)
        => commands.QueryAsync(request, match, ct);

    private Task<T> RunWorkflowAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        => workflows.RunAsync(async token =>
        {
            commands.ThrowIfUnavailable();
            var result = await action(token);
            commands.ThrowIfUnavailable();
            return result;
        }, ct);

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        if (Volatile.Read(ref disposed) != 0) return;
        CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var bytes);
        commands.Receive(bytes);
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (Volatile.Read(ref disposed) != 0) return;
        try { if (sender.ConnectionStatus != BluetoothConnectionStatus.Disconnected) return; }
        catch (ObjectDisposedException) { return; }
        diagnostics?.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Disconnected);
        commands.Fail(new IOException("The amplifier disconnected during the command session. Reconnect before another operation."));
    }

    private static Guid Uuid(ushort value) => new($"0000{value:x4}-0000-1000-8000-00805f9b34fb");
    private static void Require(GattCommunicationStatus status, string action)
    {
        if (status != GattCommunicationStatus.Success) throw new IOException($"{action}: {status}");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        diagnostics?.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Disposed);
        workflows.Dispose();
        commands.Dispose();
        notify.ValueChanged -= OnValueChanged;
        device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        await SessionCleanup.CloseAsync(async () =>
        {
            await notify.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.None).AsTask();
        }, () => service.Dispose(), () => device.Dispose());
    }
}

public sealed record F15Identity(byte ModeCode, byte FirmwareType, string Firmware, int PresetSlots,
    int GirSlots, int GnrSlots, int DrumPatterns, int LooperFiles, string RawPayloadHex);
public sealed record PresetName(int WireIndex, string Name)
{
    public string SlotLabel => PresetAddress.FromWireIndex(WireIndex).Label;
    public bool IsCurrent {get;init;}
}
public sealed record CurrentPresetSnapshot(int WireIndex, string Name, string RawStatePayloadHex, string RawDetailPayloadHex,
    IReadOnlyList<PresetEffectBlock> Blocks)
{
    public string SlotLabel => PresetAddress.FromWireIndex(WireIndex).Label;
}
