using Iamp.Protocol;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Iamp.Transport.Windows;

/// <summary>Captured information queries and one restricted V1.1.9 live update. Callers serialize complete workflows.</summary>
public sealed class F15ReadSession : IAsyncDisposable
{
    private readonly BluetoothLEDevice device;
    private readonly GattDeviceService service;
    private readonly GattCharacteristic notify;
    private readonly GattCharacteristic write;
    private readonly FrameStreamDecoder decoder = new();
    private readonly SemaphoreSlim queue = new(1, 1);
    private readonly object gate = new();
    private Func<byte[], bool>? predicate;
    private TaskCompletionSource<byte[]>? pending;
    private CancellationTokenSource? pendingCancellation;
    private bool disposed;
    private bool faulted;

    private F15ReadSession(BluetoothLEDevice device, GattDeviceService service,
        GattCharacteristic notify, GattCharacteristic write)
    {
        this.device = device; this.service = service; this.notify = notify; this.write = write;
        notify.ValueChanged += OnValueChanged;
        device.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    public string DeviceName => device.Name;

    public static async Task<F15ReadSession> ConnectAsync(ulong address, CancellationToken ct)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(ct)
            ?? throw new IOException("Windows could not open the selected amplifier.");
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
            session = new F15ReadSession(device, chosen, notify, write);
            Require(await notify.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(ct), "Notification subscription");
            if (device.ConnectionStatus != BluetoothConnectionStatus.Connected)
                throw new IOException("The amplifier disconnected during session initialization.");
            return session;
        }
        catch
        {
            if (session is not null)
            {
                session.notify.ValueChanged -= session.OnValueChanged;
                session.device.ConnectionStatusChanged -= session.OnConnectionStatusChanged;
            }
            chosen?.Dispose(); device.Dispose(); throw;
        }
    }

    public async Task<F15Identity> ReadIdentityAsync(CancellationToken ct)
    {
        var payload = await QueryAsync(new byte[] { 0x01, 0x01 }, p => p.Length >= 3 && p[0] == 0x02, ct);
        if (payload.Length != 16 || payload[9] != 0 || payload[10] != 5)
            throw new FormatException("Identity schema differs from captured firmware; retain raw evidence before supporting this device.");
        return new F15Identity(payload[1], payload[2], Encoding.ASCII.GetString(payload, 3, 6),
            payload[11], payload[12], payload[13], payload[14], payload[15], Convert.ToHexString(payload));
    }

    public async Task<IReadOnlyList<PresetName>> ReadPresetNamesAsync(CancellationToken ct)
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

    public async Task<CurrentPresetSnapshot> ReadCurrentPresetAsync(CancellationToken ct)
    {
        var state = await QueryAsync(new byte[] { 0x11 }, p => p.Length >= 2 && p[0] == 0x12, ct);
        var index = state[1];
        if (index is < 1 or > 80) throw new FormatException("Current preset index is outside the verified range.");
        // Only query the already-selected preset; other-slot query side effects are not yet established.
        var detail = await QueryAsync(new byte[] { 0x21, index }, p => p.Length >= 22 && p[0] == 0x22 && p[1] == index, ct);
        var decoded = PresetDataParser.Parse(detail);
        var after = await QueryAsync(new byte[] { 0x11 }, p => p.Length >= 2 && p[0] == 0x12, ct);
        if (after[1] != index) throw new IOException("The current preset changed during the read; refresh the snapshot.");
        return new CurrentPresetSnapshot(index, decoded.Name, Convert.ToHexString(state), Convert.ToHexString(detail), decoded.Blocks);
    }

    /// <summary>One observed live effect update, followed by complete current-preset read-back. Does not save.</summary>
    public async Task<CurrentPresetSnapshot> SetNoiseKillerThresholdAsync(CurrentPresetSnapshot expected, ushort threshold, CancellationToken ct)
    {
        var identity = await ReadIdentityAsync(ct);
        if (identity.Firmware != "V1.1.9") throw new NotSupportedException("Parameter editing is verified only on F15i V1.1.9.");
        var before = await ReadCurrentPresetAsync(ct);
        if (before.WireIndex != expected.WireIndex || before.RawDetailPayloadHex != expected.RawDetailPayloadHex)
            throw new IOException("The amplifier preset changed since the snapshot. Read it again before editing.");
        var block = before.Blocks[0];
        var command = NoiseKillerCommand.Create(block, threshold);
        if (block.Values[2] == threshold) return before;
        await WriteObservedUpdateAsync(command, ct);
        // ATT write success alone is not an application acknowledgement. Confirm every preset byte below.
        var after = await ReadCurrentPresetAsync(ct);
        var expectedDetail = PresetDataParser.WithParameterValue(Convert.FromHexString(before.RawDetailPayloadHex), 1, 2, threshold);
        if (after.WireIndex != before.WireIndex || after.RawStatePayloadHex != before.RawStatePayloadHex ||
            after.RawDetailPayloadHex != Convert.ToHexString(expectedDetail))
            throw new IOException("Edit read-back differs from the requested change. Read the amplifier before another operation; nothing was saved.");
        return after;
    }

    private async Task WriteObservedUpdateAsync(byte[] payload, CancellationToken ct)
    {
        await queue.WaitAsync(ct);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (faulted) throw new IOException("Reconnect before sending another command.");
            }
            var result = await write.WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(FrameCodec.Encode(payload)),
                GattWriteOption.WriteWithResponse).AsTask(deadline.Token);
            Require(result.Status, "Live parameter update");
        }
        catch { lock (gate) { faulted = true; decoder.Reset(); } throw; }
        finally { queue.Release(); }
    }

    /// <summary>Restricted unsaved tone edit on the captured second-position 808 model, with full read-back.</summary>
    public async Task<CurrentPresetSnapshot> Set808ToneAsync(CurrentPresetSnapshot expected, ushort tone, CancellationToken ct)
    {
        var identity = await ReadIdentityAsync(ct);
        if (identity.Firmware != "V1.1.9") throw new NotSupportedException("808 tone editing is verified only on F15i V1.1.9.");
        var before = await ReadCurrentPresetAsync(ct);
        if (before.WireIndex != expected.WireIndex || before.RawDetailPayloadHex != expected.RawDetailPayloadHex ||
            before.RawStatePayloadHex != expected.RawStatePayloadHex)
            throw new IOException("The amplifier changed since the snapshot. Read it again before editing.");
        var block = before.Blocks[1];
        var request = Od808ToneCommand.Create(block, tone);
        if (block.Values[3] == tone) return before;
        await WriteObservedUpdateAsync(request, ct);
        var after = await ReadCurrentPresetAsync(ct);
        var expectedDetail = PresetDataParser.WithParameterValue(Convert.FromHexString(before.RawDetailPayloadHex), 2, 3, tone);
        if (after.WireIndex != before.WireIndex || after.RawStatePayloadHex != before.RawStatePayloadHex ||
            after.RawDetailPayloadHex != Convert.ToHexString(expectedDetail))
            throw new IOException("808 tone read-back differs from the requested change. Read the amplifier before another operation; nothing was saved.");
        return after;
    }

    /// <summary>Recall one of the two captured slots. Preserves all other whole-state fields; does not save presets.</summary>
    public async Task<CurrentPresetSnapshot> SelectObservedPresetAsync(CurrentPresetSnapshot expected, byte targetIndex, CancellationToken ct)
    {
        var identity = await ReadIdentityAsync(ct);
        if (identity.Firmware != "V1.1.9") throw new NotSupportedException("Preset selection is verified only on F15i V1.1.9.");
        var before = await ReadCurrentPresetAsync(ct);
        if (before.WireIndex != expected.WireIndex || before.RawDetailPayloadHex != expected.RawDetailPayloadHex ||
            before.RawStatePayloadHex != expected.RawStatePayloadHex)
            throw new IOException("The amplifier changed since the snapshot. Read it again before selecting a preset.");
        var request = PresetSelectionCommand.Create(Convert.FromHexString(before.RawStatePayloadHex), targetIndex);
        PresetSelectionCommand.RequireObservedPreset(before.WireIndex, Convert.FromHexString(before.RawDetailPayloadHex));
        if (before.WireIndex == targetIndex) return before;
        await WriteObservedUpdateAsync(request, ct);
        var after = await ReadCurrentPresetAsync(ct);
        request[0] = 0x12;
        if (after.WireIndex != targetIndex || after.RawStatePayloadHex != Convert.ToHexString(request))
            throw new IOException("Preset selection or preserved global state could not be confirmed. Read the amplifier before another operation.");
        PresetSelectionCommand.RequireObservedPreset(after.WireIndex, Convert.FromHexString(after.RawDetailPayloadHex));
        return after;
    }

    private async Task<byte[]> QueryAsync(byte[] request, Func<byte[], bool> match, CancellationToken ct)
    {
        await queue.WaitAsync(ct);
        using var requestDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestDeadline.CancelAfter(TimeSpan.FromSeconds(15));
        var requestToken = requestDeadline.Token;
        try
        {
            var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (faulted) throw new IOException("The information session failed; reconnect before another request.");
                predicate = match; pending = completion; pendingCancellation = requestDeadline;
            }
            try
            {
                var result = await write.WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(FrameCodec.Encode(request)),
                    GattWriteOption.WriteWithResponse).AsTask(requestToken);
                Require(result.Status, "Information request");
                // No automatic replay: ambiguous/time-out writes require a new session.
                return await completion.Task.WaitAsync(requestToken);
            }
            catch
            {
                lock (gate) { faulted = true; decoder.Reset(); }
                throw;
            }
        }
        finally
        {
            lock (gate) { predicate = null; pending = null; pendingCancellation = null; }
            queue.Release();
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var bytes);
        lock (gate)
            foreach (var frame in decoder.Push(bytes))
                if (predicate?.Invoke(frame.Payload) == true) pending?.TrySetResult(frame.Payload);
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus != BluetoothConnectionStatus.Disconnected) return;
        lock (gate)
        {
            faulted = true;
            decoder.Reset();
            pending?.TrySetException(new IOException("The amplifier disconnected during the information request."));
            pendingCancellation?.Cancel();
        }
    }

    private static Guid Uuid(ushort value) => new($"0000{value:x4}-0000-1000-8000-00805f9b34fb");
    private static void Require(GattCommunicationStatus status, string action)
    {
        if (status != GattCommunicationStatus.Success) throw new IOException($"{action}: {status}");
    }

    public async ValueTask DisposeAsync()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            pending?.TrySetException(new ObjectDisposedException(nameof(F15ReadSession)));
            pendingCancellation?.Cancel();
        }
        notify.ValueChanged -= OnValueChanged;
        device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        try
        {
            await notify.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.None).AsTask().WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or System.Runtime.InteropServices.COMException) { }
        finally { service.Dispose(); device.Dispose(); }
    }
}

public sealed record F15Identity(byte ModeCode, byte FirmwareType, string Firmware, int PresetSlots,
    int GirSlots, int GnrSlots, int DrumPatterns, int LooperFiles, string RawPayloadHex);
public sealed record PresetName(int WireIndex, string Name);
public sealed record CurrentPresetSnapshot(int WireIndex, string Name, string RawStatePayloadHex, string RawDetailPayloadHex,
    IReadOnlyList<PresetEffectBlock> Blocks);
