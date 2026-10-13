using Windows.Devices.Bluetooth.Advertisement;

namespace Iamp.Transport.Windows;

public sealed record F15DiscoveredDevice(ulong Address, string Name, short SignalDbm)
{
    public string DisplayName => $"{Name}  ·  {Address:X12}  ·  {SignalDbm} dBm";
}

public static class F15DeviceDiscovery
{
    /// <summary>Bounded BLE discovery. Does not connect to or modify an amplifier.</summary>
    public static async Task<IReadOnlyList<F15DiscoveredDevice>> ScanAsync(CancellationToken ct,
        TimeSpan? duration = null)
    {
        var seconds = Math.Clamp((duration ?? TimeSpan.FromSeconds(10)).TotalSeconds, 0.1, 15);
        var devices = new Dictionary<ulong, F15DiscoveredDevice>();
        var gate = new object();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        void Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var name = args.Advertisement.LocalName;
            if (!name.Contains("F15", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("MOOER", StringComparison.OrdinalIgnoreCase)) return;
            lock (gate) devices[args.BluetoothAddress] = new(args.BluetoothAddress, name, args.RawSignalStrengthInDBm);
        }
        watcher.Received += Received;
        try
        {
            ct.ThrowIfCancellationRequested();
            watcher.Start();
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
            if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
                throw new IOException("Bluetooth scanning stopped. Check that Bluetooth is enabled on this PC.");
            lock (gate) return devices.Values.OrderByDescending(d => d.SignalDbm).ToArray();
        }
        finally { watcher.Stop(); watcher.Received -= Received; }
    }
}
