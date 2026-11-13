using System.Text.Json;

namespace BlackjackCasino.Core.Hosting;

/// <summary>
/// Explicit, non-persistent launch selection for a dual-host rehearsal package.
/// No production save promotion happens here; NativeShell always receives the
/// name of an already staged, isolated copy of the player's data.
/// </summary>
public sealed record HostCutoverLaunchPlan(string Executable, string[] Arguments, bool UsesNativeHost);

public static class HostCutoverLaunchPolicy
{
    public const string LegacyExecutable = "BlackjackCasino.Desktop.exe";
    public const string NativeExecutable = "BlackjackCasino.NativeShell.exe";
    private const string Marker = "native-migration-manifest.json";

    public static HostCutoverLaunchPlan Resolve(string installDirectory,
        string localAppData, string? candidate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppData);
        string install = Path.GetFullPath(installDirectory);
        string legacy = Path.Combine(install, LegacyExecutable);
        RequirePlainFile(legacy, "WPF fallback executable");
        if (candidate is null)
            return new HostCutoverLaunchPlan(legacy, [], false);

        string folder = GetCandidateDirectory(localAppData, candidate);
        string native = Path.Combine(install, NativeExecutable);
        RequirePlainFile(native, "NativeShell executable");

        string current = Path.GetFullPath(localAppData);
        RequirePlainDirectory(current, "LocalAppData");
        foreach (string segment in new[] { "BlackjackCasino", HostPreviewStorage.PreviewFolderName,
            "Candidates", candidate })
        {
            current = Path.Combine(current, segment);
            RequirePlainDirectory(current, "NativeShell candidate directory");
        }
        if (!string.Equals(current, folder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Native candidate directory did not resolve within isolated storage.");

        string manifestFile = Path.Combine(folder, Marker);
        RequirePlainFile(manifestFile, "candidate manifest");
        if (new FileInfo(manifestFile).Length > 256 * 1024)
            throw new InvalidDataException("Candidate manifest is unreasonably large.");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(manifestFile));
        JsonElement root = manifest.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("Version", out JsonElement version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out int schema)
            || schema is not (1 or 2)
            || !root.TryGetProperty("Candidate", out JsonElement name)
            || name.ValueKind != JsonValueKind.String
            || name.GetString() != candidate
            || !root.TryGetProperty("Files", out JsonElement files)
            || files.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Unverified NativeShell candidate manifest.");
        foreach (string required in new[] { "player-state.json", "profile.json" })
        {
            if (!files.EnumerateArray().Any(entry =>
                entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("Name", out JsonElement value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() == required))
                throw new InvalidDataException("Candidate manifest does not include " + required);
            RequirePlainFile(Path.Combine(folder, required), "candidate save " + required);
        }
        return new HostCutoverLaunchPlan(native, ["--candidate", candidate], true);
    }

    public static IReadOnlyList<string> AvailableCandidates(string installDirectory, string localAppData)
    {
        string candidateRoot = Path.Combine(Path.GetFullPath(localAppData),
            "BlackjackCasino", HostPreviewStorage.PreviewFolderName, "Candidates");
        if (!Directory.Exists(candidateRoot)) return [];
        var names = new List<string>();
        foreach (string path in Directory.GetDirectories(candidateRoot))
        {
            string name = Path.GetFileName(path);
            try
            {
                _ = Resolve(installDirectory, localAppData, name);
                names.Add(name);
            }
            catch (Exception error) when (error is ArgumentException or IOException
                or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                // Unverified or incomplete candidates are never offered to launch.
            }
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public static string GetCandidateDirectory(string localAppData, string candidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppData);
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > 40
            || !char.IsAsciiLetterOrDigit(candidate[0])
            || candidate.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new ArgumentException("Invalid isolated NativeShell candidate name.", nameof(candidate));
        return Path.Combine(Path.GetFullPath(localAppData), "BlackjackCasino",
            HostPreviewStorage.PreviewFolderName, "Candidates", candidate);
    }

    private static void RequirePlainDirectory(string path, string description)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(description + " not found: " + path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(description + " may not be a directory link.");
    }

    private static void RequirePlainFile(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(description + " not found.", path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(description + " may not be a filesystem link.");
    }
}
