using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Iamp.Transport.Windows;

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
            else
            {
                Console.Error.WriteLine("Usage: Iamp.DeviceProbe usb | scan <1-60 seconds> | gatt <BLE address> | read-info <BLE address> | read-current <BLE address>");
                Console.Error.WriteLine("read-info sends only captured identity/name queries; no preset/settings changes are implemented.");
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
