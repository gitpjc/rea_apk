using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Iamp.Desktop;

internal static class DesktopVersion
{
    // Generated from Directory.Build.props; omit optional source revision metadata.
    public static string Current {get;}=typeof(DesktopVersion).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
}

internal sealed record VersionHistoryEntry(
    [property:JsonPropertyName("version")] string Version,
    [property:JsonPropertyName("changedAt")] DateTimeOffset ChangedAt,
    [property:JsonPropertyName("summary")] string Summary,
    [property:JsonPropertyName("details")] string Details);

internal static class VersionHistoryStore
{
    public static IReadOnlyList<VersionHistoryEntry> Load()
    {
        var path=Path.Combine(AppContext.BaseDirectory,"assets","version_history.json");
        var entries=JsonSerializer.Deserialize<VersionHistoryEntry[]>(File.ReadAllText(path))
            ?? throw new FormatException("Missing version history entries.");
        if(entries.Length==0 ||
            entries.Any(e=>e is null || string.IsNullOrWhiteSpace(e.Version) || string.IsNullOrWhiteSpace(e.Summary) || string.IsNullOrWhiteSpace(e.Details) || e.ChangedAt==default) ||
            entries[0].Version!=DesktopVersion.Current ||
            entries.Select(e=>e.Version).Distinct(StringComparer.Ordinal).Count()!=entries.Length)
            throw new FormatException("Version history must have unique entries and start with the current build version.");
        return entries;
    }
}
