using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Iamp.Transport.Windows;
using Iamp.Protocol;

namespace Iamp.DeviceProbe;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args is ["usb"] or [])
            {
                Print(new { operation = "usb_metadata", target = "VID_34DB&PID_000B", devices = HidInventory.ReadMooer() });
            }
            else if (args is ["scan", var duration] && int.TryParse(duration, out var seconds) && seconds is >= 1 and <= 60)
            {
                await ScanAsync(seconds);
            }
            else if (args is ["gatt", var addressText] && TryAddress(addressText, out var address))
            {
                await InspectGattAsync(address);
            }
            else if (args is ["read-info", var target] && TryAddress(target, out var targetAddress))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await using var session = await F15ReadSession.ConnectAsync(targetAddress, timeout.Token);
                var identity = await session.ReadIdentityAsync(timeout.Token);
                var names = await session.ReadPresetNamesAsync(timeout.Token);
                Print(new { operation = "observed_information_queries", session.DeviceName, identity, presets = names });
            }
            else if (args is ["read-current", var currentTarget] && TryAddress(currentTarget, out var currentAddress))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await using var session = await F15ReadSession.ConnectAsync(currentAddress, timeout.Token);
                var identity = await session.ReadIdentityAsync(timeout.Token);
                var current = await session.ReadCurrentPresetAsync(timeout.Token);
                Print(new { operation = "current_preset_snapshot", session.DeviceName, identity, current });
            }
            else if (args is ["threshold-roundtrip", var editTarget] && TryAddress(editTarget, out var editAddress))
            {
                await ThresholdRoundtripAsync(editAddress);
            }
            else if (args is ["persistence-threshold-step", var stepTarget, var stepValue, var baselinePath] &&
                TryAddress(stepTarget,out var stepAddress) && ushort.TryParse(stepValue,out var threshold) && threshold is 25 or 26)
            {
                await PersistenceThresholdStepAsync(stepAddress,threshold,baselinePath);
            }
            else if (args is ["selection-roundtrip", var selectionTarget] && TryAddress(selectionTarget, out var selectionAddress))
            {
                await SelectionRoundtripAsync(selectionAddress);
            }
            else if (args is ["tone-roundtrip", var toneTarget] && TryAddress(toneTarget, out var toneAddress))
            {
                await ToneRoundtripAsync(toneAddress);
            }
            else if (args is ["controls-roundtrip", var controlsTarget] && TryAddress(controlsTarget, out var controlsAddress))
            {
                await ControlsRoundtripAsync(controlsAddress);
            }
            else
            {
                Console.Error.WriteLine("Usage: Iamp.DeviceProbe usb | scan <1-60 seconds> | gatt <BLE address> | read-info <BLE address> | read-current <BLE address> | threshold-roundtrip <BLE address> | selection-roundtrip <BLE address>");
                Console.Error.WriteLine("threshold-roundtrip performs the experimental V1.1.9 Noise Killer 25→26→25 test with read-back; no saving.");
                return 2;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Print(new { error = ex.GetType().Name, message = ex.Message });
            return 1;
        }
    }

    private static bool TryAddress(string text, out ulong address)
    {
        var normalized = text.Replace(":", "").Replace("-", "");
        address = 0;
        return normalized.Length == 12 && ulong.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
    }

    private static void Print(object value) => Console.WriteLine(JsonSerializer.Serialize(value, Json));

    // Explicit single live edit for the approved phone-save experiment. No save
    // packet is sent here. The complete recorded baseline must match except for
    // the one approved threshold field, including during restorative cleanup.
    private static async Task PersistenceThresholdStepAsync(ulong address,ushort value,string baselinePath)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(baselinePath));
        var root=document.RootElement;
        var baselineState=Convert.FromHexString(root.GetProperty("current").GetProperty("RawStatePayloadHex").GetString()!);
        var baselineDetail=Convert.FromHexString(root.GetProperty("current").GetProperty("RawDetailPayloadHex").GetString()!);
        var firmware=root.GetProperty("identity").GetProperty("Firmware").GetString()!;
        _=ObservedPresetSaveCommand.CreateUnchanged(firmware,baselineState,baselineDetail);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var session=await F15ReadSession.ConnectAsync(address,deadline.Token);
        var identity=await session.ReadIdentityAsync(deadline.Token);
        var current=await session.ReadCurrentPresetAsync(deadline.Token);
        var currentDetail=Convert.FromHexString(current.RawDetailPayloadHex);
        var normalized=PresetDataParser.WithParameterValue(currentDetail,1,2,25);
        var previous=current.Blocks.Single(b=>b.Position==1).Values[2];
        if(identity.Firmware!=firmware || current.RawStatePayloadHex!=Convert.ToHexString(baselineState) ||
            !normalized.AsSpan().SequenceEqual(baselineDetail) || previous is not (25 or 26))
            throw new InvalidOperationException("Complete state/detail differs beyond the approved threshold; no edit performed.");
        if(previous==value)throw new InvalidOperationException("Threshold already matches; no edit performed.");
        var changed=await session.SetNoiseKillerThresholdAsync(current,value,deadline.Token);
        Print(new {operation="approved_persistence_live_threshold_step",identity,current=changed,previousThreshold=previous,requestedThreshold=value,saved=false});
    }

    private static async Task ThresholdRoundtripAsync(ulong address)
    {
        CurrentPresetSnapshot baseline;
        CurrentPresetSnapshot? changed = null;
        CurrentPresetSnapshot? restored = null;
        var attempted = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await using var session = await F15ReadSession.ConnectAsync(address, deadline.Token);
            baseline = await session.ReadCurrentPresetAsync(deadline.Token);
            if (baseline.Blocks[0].Values.Count != 6 || baseline.Blocks[0].Values[2] != 25)
                throw new InvalidOperationException("The controlled round trip requires the captured baseline threshold 25.");
            NoiseKillerCommand.Create(baseline.Blocks[0], 26); // Validate before entering the restore path.
            try
            {
                attempted = true;
                changed = await session.SetNoiseKillerThresholdAsync(baseline, 26, deadline.Token);
            }
            finally
            {
                if (attempted)
                {
                    // A failed request may still have reached the amp. Use a fresh connection and deadline.
                    await session.DisposeAsync();
                    using var recoveryDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    await using var recovery = await F15ReadSession.ConnectAsync(address, recoveryDeadline.Token);
                    var live = await recovery.ReadCurrentPresetAsync(recoveryDeadline.Token);
                    if (live.WireIndex != baseline.WireIndex)
                        throw new IOException("Preset changed externally; automatic threshold restoration stopped. Inspect the amplifier.");
                    var liveBytes = Convert.FromHexString(live.RawDetailPayloadHex);
                    var baselineBytes = Convert.FromHexString(baseline.RawDetailPayloadHex);
                    liveBytes[29] = baselineBytes[29]; liveBytes[30] = baselineBytes[30];
                    if (!liveBytes.SequenceEqual(baselineBytes))
                        throw new IOException("Other parameters changed externally; automatic restoration stopped. Inspect the amplifier.");
                    restored = await recovery.SetNoiseKillerThresholdAsync(live, 25, recoveryDeadline.Token);
                    if (restored.RawDetailPayloadHex != baseline.RawDetailPayloadHex)
                        throw new IOException("Baseline restoration could not be verified.");
                }
            }
            Print(new { operation = "threshold_25_26_25", baseline, changed, restored, baselineRestored = true, saved = false });
        }
        catch (Exception ex)
        {
            Print(new { operation = "threshold_25_26_25", error = ex.Message, attempted, restored });
            throw;
        }
    }

    private static async Task SelectionRoundtripAsync(ulong address)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var session = await F15ReadSession.ConnectAsync(address, deadline.Token);
        var baseline = await session.ReadCurrentPresetAsync(deadline.Token);
        if (baseline.WireIndex != 4) throw new InvalidOperationException("The controlled selection round trip must start at slot 4.");
        PresetSelectionCommand.Create(Convert.FromHexString(baseline.RawStatePayloadHex), 5);
        PresetSelectionCommand.RequireObservedPreset(baseline.WireIndex, Convert.FromHexString(baseline.RawDetailPayloadHex));
        CurrentPresetSnapshot? changed = null;
        CurrentPresetSnapshot? restored = null;
        try
        {
            changed = await session.SelectObservedPresetAsync(baseline, 5, deadline.Token);
        }
        finally
        {
            await session.DisposeAsync();
            using var restoreDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var recovery = await F15ReadSession.ConnectAsync(address, restoreDeadline.Token);
            var live = await recovery.ReadCurrentPresetAsync(restoreDeadline.Token);
            var liveState = Convert.FromHexString(live.RawStatePayloadHex);
            var baselineState = Convert.FromHexString(baseline.RawStatePayloadHex);
            liveState[1] = baselineState[1];
            if (live.WireIndex is not (4 or 5) || !liveState.SequenceEqual(baselineState))
                throw new IOException("External state changed during the test. Automatic recall stopped; restore the original preset manually.");
            restored = await recovery.SelectObservedPresetAsync(live, checked((byte)baseline.WireIndex), restoreDeadline.Token);
            var matches = restored.RawStatePayloadHex == baseline.RawStatePayloadHex && restored.RawDetailPayloadHex == baseline.RawDetailPayloadHex;
            Print(new { operation = "preset_4_5_4", baseline, changed, restored, baselineRestored = matches, saved = false });
            if (!matches) throw new IOException("Original preset values or global state did not match baseline after recall. Stop hardware tests.");
        }
    }

    private static async Task ToneRoundtripAsync(ulong address)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var session = await F15ReadSession.ConnectAsync(address, deadline.Token);
        var baseline = await session.ReadCurrentPresetAsync(deadline.Token);
        PresetSelectionCommand.RequireObservedPreset(baseline.WireIndex, Convert.FromHexString(baseline.RawDetailPayloadHex));
        if (baseline.WireIndex != 4) throw new InvalidOperationException("808 tone test must start at the captured slot 4 baseline.");
        Od808ToneCommand.Create(baseline.Blocks[1], 98);
        CurrentPresetSnapshot? changed = null;
        CurrentPresetSnapshot? restored = null;
        try { changed = await session.Set808ToneAsync(baseline, 98, deadline.Token); }
        finally
        {
            await session.DisposeAsync();
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var recovery = await F15ReadSession.ConnectAsync(address, cleanup.Token);
            var live = await recovery.ReadCurrentPresetAsync(cleanup.Token);
            var normalized = PresetDataParser.WithParameterValue(Convert.FromHexString(live.RawDetailPayloadHex), 2, 3, 100);
            if (live.RawStatePayloadHex != baseline.RawStatePayloadHex || Convert.ToHexString(normalized) != baseline.RawDetailPayloadHex)
                throw new IOException("Other state changed; automatic tone restoration stopped. Inspect the amplifier.");
            restored = await recovery.Set808ToneAsync(live, 100, cleanup.Token);
            var matches = restored.RawDetailPayloadHex == baseline.RawDetailPayloadHex && restored.RawStatePayloadHex == baseline.RawStatePayloadHex;
            Print(new { operation = "808_tone_100_98_100", baseline, changed, restored, baselineRestored = matches, saved = false });
            if (!matches) throw new IOException("Full original tone-test baseline restoration was not verified. Stop hardware tests.");
        }
    }

    private static async Task ControlsRoundtripAsync(ulong address)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await using var session = await F15ReadSession.ConnectAsync(address, deadline.Token);
        var baseline = await session.ReadCurrentPresetAsync(deadline.Token);
        if (baseline.WireIndex != 4) throw new InvalidOperationException("Combined-control test requires captured preset 4.");
        PresetSelectionCommand.RequireObservedPreset(4, Convert.FromHexString(baseline.RawDetailPayloadHex));
        var live = baseline;
        var steps = new List<CurrentPresetSnapshot>();
        CurrentPresetSnapshot? restored = null;
        async Task Step(Func<CurrentPresetSnapshot, Task<CurrentPresetSnapshot>> apply)
        {
            live = await apply(live);
            steps.Add(live);
        }
        try
        {
            await Step(s => session.SetNoiseKillerLevelAsync(s, 48, deadline.Token));
            await Step(s => session.SetNoiseKillerThresholdAsync(s, 26, deadline.Token));
            await Step(s => session.SetNoiseKillerThresholdAsync(s, 25, deadline.Token));
            await Step(s => session.SetNoiseKillerLevelAsync(s, 50, deadline.Token));
            await Step(s => session.Set808GainAsync(s, 37, deadline.Token));
            await Step(s => session.Set808ToneAsync(s, 98, deadline.Token));
            await Step(s => session.Set808VolumeAsync(s, 98, deadline.Token));
            await Step(s => session.Set808GainAsync(s, 39, deadline.Token));
            await Step(s => session.Set808ToneAsync(s, 100, deadline.Token));
            await Step(s => session.Set808VolumeAsync(s, 100, deadline.Token));
        }
        finally
        {
            await session.DisposeAsync();
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await using var recovery = await F15ReadSession.ConnectAsync(address, cleanup.Token);
            var actual = await recovery.ReadCurrentPresetAsync(cleanup.Token);
            ObservedParameterCommands.Create(actual.Blocks[0], ObservedParameter.NoiseThreshold, 25);
            ObservedParameterCommands.Create(actual.Blocks[1], ObservedParameter.OdTone, 100);
            var normalized = Convert.FromHexString(actual.RawDetailPayloadHex);
            foreach (var (position, index, value) in new (byte, int, ushort)[] { (1,2,25),(1,3,50),(2,2,39),(2,3,100),(2,4,100) })
                normalized = PresetDataParser.WithParameterValue(normalized, position, index, value);
            if (actual.RawStatePayloadHex != baseline.RawStatePayloadHex || Convert.ToHexString(normalized) != baseline.RawDetailPayloadHex)
                throw new IOException("Other state changed during controls test. Automatic restoration stopped; inspect the amplifier.");
            actual = await recovery.SetNoiseKillerThresholdAsync(actual, 25, cleanup.Token);
            actual = await recovery.SetNoiseKillerLevelAsync(actual, 50, cleanup.Token);
            actual = await recovery.Set808GainAsync(actual, 39, cleanup.Token);
            actual = await recovery.Set808ToneAsync(actual, 100, cleanup.Token);
            actual = await recovery.Set808VolumeAsync(actual, 100, cleanup.Token);
            restored = await recovery.ReadCurrentPresetAsync(cleanup.Token);
            var matches = restored.RawStatePayloadHex == baseline.RawStatePayloadHex && restored.RawDetailPayloadHex == baseline.RawDetailPayloadHex;
            Print(new { operation = "five_parameter_combined_roundtrip", baseline, steps, restored, baselineRestored = matches, saved = false });
            if (!matches) throw new IOException("Complete controls-test baseline restoration was not verified. Stop hardware tests.");
        }
    }

    private static async Task ScanAsync(int seconds)
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter is null || !adapter.IsLowEnergySupported)
            throw new InvalidOperationException("No usable Bluetooth LE adapter found.");
        var records = new ConcurrentDictionary<ulong, Advertisement>();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        BluetoothError? stoppedError = null;
        watcher.Received += (_, e) =>
        {
            var item = new Advertisement(e.BluetoothAddress.ToString("X12"), e.Advertisement.LocalName,
                e.RawSignalStrengthInDBm, e.Advertisement.ServiceUuids.Select(u => u.ToString()).ToArray());
            records.AddOrUpdate(e.BluetoothAddress, item, (_, previous) => item with
            {
                Name = string.IsNullOrEmpty(item.Name) ? previous.Name : item.Name,
                Services = previous.Services.Union(item.Services).ToArray()
            });
        };
        watcher.Stopped += (_, e) => stoppedError = e.Error;
        watcher.Start();
        try { await Task.Delay(TimeSpan.FromSeconds(seconds)); }
        finally { watcher.Stop(); }
        if (stoppedError is not null and not BluetoothError.Success)
            throw new InvalidOperationException($"Bluetooth scan failed: {stoppedError}");
        Print(new { operation = "ble_advertisements", seconds, devices = records.Values.OrderBy(d => d.Name).ThenBy(d => d.Address).ToArray() });
    }

    private static async Task InspectGattAsync(ulong address)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var ct = deadline.Token;
        using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(ct)
            ?? throw new InvalidOperationException("Windows could not open this BLE device.");
        var result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(ct);
        if (result.Status != GattCommunicationStatus.Success)
            throw new InvalidOperationException($"GATT service discovery: {result.Status}; protocol error {result.ProtocolError}");
        var services = new List<object>();
        try
        {
            foreach (var service in result.Services)
            {
                var chars = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask(ct);
                services.Add(new
                {
                    uuid = service.Uuid, handle = service.AttributeHandle, status = chars.Status.ToString(),
                    protocolError = chars.ProtocolError,
                    characteristics = chars.Characteristics.Select(c => new
                    {
                        uuid = c.Uuid, handle = c.AttributeHandle, properties = c.CharacteristicProperties.ToString()
                    }).ToArray()
                });
            }
        }
        finally { foreach (var service in result.Services) service.Dispose(); }
        Print(new { operation = "gatt_metadata", address = address.ToString("X12"), device.Name, services });
    }

    private sealed record Advertisement(string Address, string Name, short Rssi, string[] Services);
}
