using System.Reflection;
using System.Text.Json;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Offline, player-facing update highlights shipped with the installed build.
/// The technical commit history remains in release-notes.json "changes", but it
/// is never copied into the lobby news. Older builds receive a truthful overview.
/// </summary>
internal sealed record NativePlayerNewsEntry(
    string Title, string Description, string TitleDe, string DescriptionDe,
    string Accent, string Category);

internal sealed record NativePlayerNewsFeed(
    string InstalledVersion, bool ReleaseSpecific, NativePlayerNewsEntry[] Entries);

internal static class NativePlayerNews
{
    private static readonly Lazy<NativePlayerNewsFeed> CurrentFeed =
        new(ReadInstalled, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static NativePlayerNewsFeed Current => CurrentFeed.Value;

    private static NativePlayerNewsFeed ReadInstalled()
    {
        try
        {
            string file = Path.Combine(AppContext.BaseDirectory, "release-notes.json");
            if (File.Exists(file))
                return FromJson(File.ReadAllText(file));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or JsonException)
        {
            // Bundled notes must never prevent an offline casino from starting.
        }
        return Fallback();
    }

    /// <summary>Strictly parse curated notes; never fall back to the developer changelog.</summary>
    internal static NativePlayerNewsFeed FromJson(string? contents)
    {
        if (string.IsNullOrWhiteSpace(contents) || contents.Length > 512_000)
            return Fallback();
        try
        {
            using JsonDocument doc = JsonDocument.Parse(contents,
                new JsonDocumentOptions { MaxDepth = 20 });
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("playerNews", out JsonElement entries)
                || entries.ValueKind != JsonValueKind.Array
                || entries.GetArrayLength() is < 1 or > 6)
                return Fallback();

            var parsed = new List<NativePlayerNewsEntry>(entries.GetArrayLength());
            foreach (JsonElement entry in entries.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !Field(entry, "title", 90, out string title)
                    || !Field(entry, "description", 280, out string description)
                    || !Field(entry, "titleDe", 90, out string titleDe)
                    || !Field(entry, "descriptionDe", 280, out string descriptionDe)
                    || !Field(entry, "accent", 7, out string accent)
                    || !Field(entry, "category", 20, out string category)
                    || accent.Length != 7 || accent[0] != '#'
                    || !accent.Skip(1).All(Uri.IsHexDigit)
                    || category is not ("feature" or "improvement" or "fix"))
                    return Fallback();
                parsed.Add(new NativePlayerNewsEntry(
                    title, description, titleDe, descriptionDe, accent, category));
            }

            string version = Field(root, "version", 80, out string reportedVersion)
                ? reportedVersion : InstalledVersion();
            return new NativePlayerNewsFeed(version, true, parsed.ToArray());
        }
        catch (JsonException)
        {
            return Fallback();
        }
    }

    private static bool Field(JsonElement source, string name, int max, out string value)
    {
        value = "";
        if (!source.TryGetProperty(name, out JsonElement field)
            || field.ValueKind != JsonValueKind.String)
            return false;
        string? raw = field.GetString();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > max) return false;
        value = raw.Trim();
        return value.Length > 0;
    }

    private static string InstalledVersion()
    {
        string? version = typeof(NativePlayerNews).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version)
            ? "Development" : version.Split('+')[0];
    }

    private static NativePlayerNewsFeed Fallback() => new(
        InstalledVersion(), false,
        [
            new("Scratchcards in four editions",
                "Choose between Lucky Sevens, Gold Rush, Platinum Vault and Diamond Royale.",
                "Vier Rubbellos-Editionen",
                "Wähle zwischen Lucky Sevens, Gold Rush, Platinum Vault und Diamond Royale.",
                "#e7c486", "feature"),
            new("Casino Empire",
                "Build your casino, collect credits and unlock more upgrades.",
                "Casino Empire",
                "Baue dein eigenes Casino auf, sammle Credits und schalte Verbesserungen frei.",
                "#b7c6a4", "improvement"),
            new("Midnight Manor",
                "Explore the haunted manor during the slot's 3D bonus.",
                "Midnight Manor",
                "Erkunde beim 3D-Bonus des Slots ein geheimnisvolles Spukhaus.",
                "#b5a1d9", "feature"),
        ]);
}
