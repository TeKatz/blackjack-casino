using System.Text.Json;

namespace BlackjackCasino.Desktop;

internal sealed record ReleaseNote(string Title, string Description, string Accent);

internal static class ReleaseNotesCatalog
{
    private sealed class Document
    {
        public int SchemaVersion { get; set; }
        public string? Version { get; set; }
        public string? Channel { get; set; }
        public List<Entry>? Changes { get; set; }
    }

    private sealed class Entry
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }

    internal static IReadOnlyList<ReleaseNote> Current { get; } = Load(
        Path.Combine(AppContext.BaseDirectory, "release-notes.json"), AppVersion.Current.ToString());

    internal static IReadOnlyList<ReleaseNote> Load(string path, string installedVersion)
    {
        try
        {
            if (File.Exists(path))
            {
                var doc = JsonSerializer.Deserialize<Document>(File.ReadAllText(path),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                // Never show notes for the currently published *latest* build when an
                // older build is still installed (the development release tag is mutable).
                if (doc is { SchemaVersion: 1 } &&
                    string.Equals(doc.Version, installedVersion, StringComparison.OrdinalIgnoreCase) &&
                    doc.Changes is { Count: > 0 })
                {
                    ReleaseNote[] notes = doc.Changes
                        .Where(x => !string.IsNullOrWhiteSpace(x.Title))
                        .Take(24)
                        .Select(x => new ReleaseNote(x.Title!.Trim(),
                            x.Description?.Trim() ?? string.Empty, AccentFor(x.Category)))
                        .ToArray();
                    if (notes.Length > 0) return notes;
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }

        return [new ReleaseNote("Release notes unavailable",
            $"No version-matched changelog was bundled with {installedVersion}.",
            "#9DB7C8")];
    }

    private static string AccentFor(string? category) => category?.ToLowerInvariant() switch
    {
        "features" => "#D1AC63",
        "fixes" => "#86C7AF",
        "performance" => "#9DB7C8",
        "improvements" => "#E0BD70",
        _ => "#9FB1A8",
    };
}
