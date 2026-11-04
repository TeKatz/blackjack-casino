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
        public string? TitleDe { get; set; }
        public string? DescriptionDe { get; set; }
        public string? Category { get; set; }
    }

    internal static IReadOnlyList<ReleaseNote> Current => Load(
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
                    // Existing releases may have English-only notes: do not expose raw
                    // developer commit subjects in the German UI.
                    bool german = Localization.Language == UiLanguagePreference.German;
                    ReleaseNote[] notes = doc.Changes
                        .Where(x => !string.IsNullOrWhiteSpace(x.Title))
                        .Take(24)
                        .Select(x => new ReleaseNote(
                            german ? (string.IsNullOrWhiteSpace(x.TitleDe) ? GermanCategoryTitle(x.Category) : x.TitleDe.Trim()) : x.Title!.Trim(),
                            german ? (string.IsNullOrWhiteSpace(x.TitleDe) ? GermanCategoryBody(x.Category) : x.DescriptionDe?.Trim() ?? string.Empty) : x.Description?.Trim() ?? string.Empty,
                            AccentFor(x.Category)))
                        .ToArray();
                    if (notes.Length > 0) return notes;
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }

        return [new ReleaseNote(Localization.T("Shell.Welcome.NotesMissingTitle"),
            Localization.F("Shell.Welcome.NotesMissingBody", installedVersion),
            "#9DB7C8")];
    }

    private static string GermanCategoryTitle(string? category) => category?.ToLowerInvariant() switch
    {
        "features" => "Neue Funktionen",
        "fixes" => "Fehlerbehebungen",
        "performance" => "Leistungsverbesserungen",
        "improvements" => "Verbesserungen",
        _ => "Technische Änderungen",
    };

    private static string GermanCategoryBody(string? category) => category?.ToLowerInvariant() switch
    {
        "features" => "Neue Inhalte oder Funktionen wurden hinzugefügt.",
        "fixes" => "Ein Fehler wurde behoben.",
        "performance" => "Die Leistung wurde überarbeitet.",
        "improvements" => "Bestehende Spielbereiche wurden verbessert.",
        _ => "Technische Wartungsarbeiten wurden durchgeführt.",
    };

    private static string AccentFor(string? category) => category?.ToLowerInvariant() switch
    {
        "features" => "#D1AC63",
        "fixes" => "#86C7AF",
        "performance" => "#9DB7C8",
        "improvements" => "#E0BD70",
        _ => "#9FB1A8",
    };
}
