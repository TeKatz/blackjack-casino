using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal sealed record NativeMigrationFile(string Name, long Bytes, string Sha256);

/// <summary>
/// An immutable inventory of the imported source files. It is NOT a release
/// approval: WebView2 localStorage, updater, audio and cross-host acceptance
/// still require their own migration work.
/// </summary>
internal sealed record NativeMigrationManifest(
    int Version, string Candidate, DateTimeOffset StagedAtUtc,
    NativeMigrationFile[] Files, NativeMigrationFile[] BrowserFiles,
    string[] RemainingGates);

/// <summary>
/// Offline, copy-only rehearsal of the old WPF host's state in a separate
/// NativeShell candidate directory. Never writes to the live player's files.
/// </summary>
internal static class NativeHostMigration
{
    private const int SchemaVersion = 2;
    private const int MaxFiles = 128;
    private const long MaxFileBytes = 32L * 1024 * 1024;
    private const long MaxSnapshotBytes = 128L * 1024 * 1024;
    private const long MaxBrowserBytes = 192L * 1024 * 1024;
    private const int MaxBrowserFiles = 512;
    private static readonly string BrowserRelativePath = Path.Combine(
        "WebView2", "Default", "Local Storage", "leveldb");
    private const string ManifestFile = "native-migration-manifest.json";
    private static readonly JsonSerializerOptions SaveJson = CreateJson();
    private static readonly string[] RequiredFiles = ["player-state.json", "profile.json"];

    internal static string ProductionDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    internal static string CandidatePath(string sourceDirectory, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40
            || !char.IsAsciiLetterOrDigit(name[0])
            || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new ArgumentException("Candidate name must be 1–40 letters, numbers, - or _.", nameof(name));
        string source = Path.GetFullPath(sourceDirectory);
        return Path.Combine(source, HostPreviewStorage.PreviewFolderName,
            "Candidates", name);
    }

    /// <summary>
    /// Only a fresh target is accepted. The source remains untouched, including
    /// on interrupted copy, corrupt JSON, changed hashes or an aborted rename.
    /// </summary>
    internal static string Stage(string sourceDirectory, string name,
        bool checkRunningHost = true)
    {
        string source = Path.GetFullPath(sourceDirectory);
        string target = CandidatePath(source, name);
        string parent = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("No existing casino player-data directory.");
        AssertNotReparsePoint(source);
        AssertNotReparsePoint(Path.Combine(source, HostPreviewStorage.PreviewFolderName));
        AssertNotReparsePoint(parent);
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("Candidate already exists; existing progress will never be overwritten.");
        if (checkRunningHost) RequireLegacyStopped();
        // The installed WPF host now holds this OS lease throughout its
        // lifetime; no second host can race the migration snapshot.
        using var productionLease = HostWriterLease.Acquire(source);

        // Validate the real formats, not merely the presence of files.
        NativeMigrationFile[] files = Capture(source);
        NativeMigrationFile[] browserFiles = CaptureBrowser(source);
        ValidateSource(source);
        if (checkRunningHost) RequireLegacyStopped();

        Directory.CreateDirectory(parent);
        AssertNotReparsePoint(parent);
        string temporary = Path.Combine(parent,
            ".staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        bool finished = false;
        try
        {
            foreach (NativeMigrationFile entry in files)
            {
                string from = Path.Combine(source, entry.Name);
                string to = Path.Combine(temporary, entry.Name);
                // No files in a child directory, no links, no content rewriting.
                AssertNotReparsePoint(from);
                File.Copy(from, to, overwrite: false);
                if (!Matches(to, entry))
                    throw new IOException("Copied state differs from source: " + entry.Name);
            }
            // Chromium stores the twenty Arcade high-score keys in a LevelDB
            // beneath the old WebView2 user profile, not in player-state.json.
            // Copy the complete small database, never parse/rewrite LevelDB
            // tables or open the installed browser profile for writing.
            string browserSource = Path.Combine(source, BrowserRelativePath);
            string browserDestination = Path.Combine(temporary, BrowserRelativePath);
            if (browserFiles.Length > 0)
            {
                Directory.CreateDirectory(browserDestination);
                foreach (NativeMigrationFile entry in browserFiles)
                {
                    string from = Path.Combine(browserSource, entry.Name);
                    string to = Path.Combine(browserDestination, entry.Name);
                    AssertNotReparsePoint(from);
                    File.Copy(from, to, overwrite: false);
                    if (!Matches(to, entry))
                        throw new IOException("Browser storage copy did not match: " + entry.Name);
                }
            }
            ValidateSource(temporary);
            NativeMigrationFile[] after = Capture(source);
            if (!files.SequenceEqual(after)
                || !browserFiles.SequenceEqual(CaptureBrowser(source)))
                throw new IOException("The legacy player's data or WebView2 storage changed during staging. Close it and retry.");
            if (checkRunningHost) RequireLegacyStopped();

            var manifest = new NativeMigrationManifest(SchemaVersion, name,
                DateTimeOffset.UtcNow, files, browserFiles,
                ["Only the original WebView2 localStorage database is copied; the browser profile's cookies and cache are not imported.",
                 "Updater and Windows runtime parity are not approved.",
                 "This is a test candidate; it cannot replace the installed host."]);
            string marker = Path.Combine(temporary, ManifestFile);
            using (var stream = new FileStream(marker, FileMode.CreateNew,
                FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, manifest, SaveJson);
                stream.Flush(flushToDisk: true);
            }
            // Atomic publication of a fully verified snapshot; never mutate
            // or replace an existing candidate.
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException("Candidate appeared while staging; refusing overwrite.");
            Directory.Move(temporary, target);
            finished = true;
            return target;
        }
        finally
        {
            if (!finished && Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    internal static string OpenCandidate(string sourceDirectory, string name)
    {
        string path = CandidatePath(sourceDirectory, name);
        AssertNotReparsePoint(path);
        string marker = Path.Combine(path, ManifestFile);
        if (!File.Exists(marker))
            throw new InvalidDataException("Candidate has not passed the verified staging operation.");
        NativeMigrationManifest? manifest =
            JsonSerializer.Deserialize<NativeMigrationManifest>(File.ReadAllBytes(marker), SaveJson);
        // v1 candidates created before browser-localStorage migration remain
        // readable. Staging always writes v2 with an explicit browser manifest.
        if (manifest is null || manifest.Version is not (1 or SchemaVersion)
            || manifest.Candidate != name
            || (manifest.Version == SchemaVersion && manifest.BrowserFiles is null)
            || manifest.Files.Length < RequiredFiles.Length
            || !RequiredFiles.All(required => manifest.Files.Any(f => f.Name == required))
            || RequiredFiles.Any(required => !File.Exists(Path.Combine(path, required))))
            throw new InvalidDataException("Invalid or incomplete native migration marker.");
        // The game's save files may have legitimately changed since the first
        // candidate run. The marker's original SHA256 values are an audit trail,
        // not a reason to discard later candidate progress.
        return path;
    }

    private static NativeMigrationFile[] CaptureBrowser(string source)
    {
        string cursor = source;
        foreach (string part in new[] { "WebView2", "Default", "Local Storage", "leveldb" })
        {
            cursor = Path.Combine(cursor, part);
            AssertNotReparsePoint(cursor);
            if (!Directory.Exists(cursor)) return [];
        }
        string[] paths = Directory.GetFiles(cursor, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length > MaxBrowserFiles)
            throw new InvalidDataException("Browser localStorage contains too many files to migrate safely.");
        var entries = new List<NativeMigrationFile>(paths.Length);
        long total = 0;
        foreach (string path in paths)
        {
            AssertNotReparsePoint(path);
            var file = new FileInfo(path);
            total = checked(total + file.Length);
            if (file.Length > MaxBrowserBytes || total > MaxBrowserBytes)
                throw new InvalidDataException("Browser localStorage exceeds safe migration limits.");
            using var stream = File.OpenRead(path);
            entries.Add(new NativeMigrationFile(file.Name, file.Length,
                Convert.ToHexString(SHA256.HashData(stream))));
        }
        return entries.ToArray();
    }

    private static NativeMigrationFile[] Capture(string source)
    {
        foreach (string required in RequiredFiles)
            if (!File.Exists(Path.Combine(source, required)))
                throw new InvalidDataException("Required existing player save is missing: " + required);

        string[] paths = Directory.GetFiles(source, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length > MaxFiles)
            throw new InvalidDataException("Too many source JSON files for a safe migration snapshot.");
        var files = new List<NativeMigrationFile>(paths.Length);
        long total = 0;
        foreach (string path in paths)
        {
            AssertNotReparsePoint(path);
            string name = Path.GetFileName(path);
            long length = new FileInfo(path).Length;
            total = checked(total + length);
            if (length <= 0 || length > MaxFileBytes || total > MaxSnapshotBytes)
                throw new InvalidDataException("Source data exceeds the verified copy bounds: " + name);
            byte[] bytes = File.ReadAllBytes(path);
            using (JsonDocument.Parse(bytes)) { }
            files.Add(new NativeMigrationFile(name, bytes.LongLength,
                Convert.ToHexString(SHA256.HashData(bytes))));
        }
        return files.ToArray();
    }

    private static void ValidateSource(string directory)
    {
        byte[] preferencesBytes = File.ReadAllBytes(Path.Combine(directory, RequiredFiles[0]));
        byte[] profileBytes = File.ReadAllBytes(Path.Combine(directory, RequiredFiles[1]));
        AppPreferences? preferences = JsonSerializer.Deserialize<AppPreferences>(preferencesBytes, SaveJson);
        PlayerProfileState? profile = JsonSerializer.Deserialize<PlayerProfileState>(profileBytes, SaveJson);
        if (preferences is null || preferences.Bankroll < 0m || profile is null
            || profile.SchemaVersion < 1 || profile.SchemaVersion > 6
            || string.IsNullOrWhiteSpace(profile.DisplayName))
            throw new InvalidDataException("Legacy wallet/profile is invalid; migration cancelled.");
        // A pending Manor transaction must remain authoritative. Copy the
        // checkpoint verbatim; the candidate runtime recovers it before load.
        if (File.Exists(Path.Combine(directory, "manor-checkpoint.json")))
            _ = new ManorPersistence(directory).Load();
        if (File.Exists(Path.Combine(directory, "hand-history.json")))
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, "hand-history.json"));
            using JsonDocument history = JsonDocument.Parse(bytes);
            if (history.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Hand history must be a JSON array.");
            RoundHistoryEntry[]? parsed = JsonSerializer.Deserialize<RoundHistoryEntry[]>(
                bytes, SaveJson);
            if (parsed is null || parsed.Any(entry => entry is null || entry.RoundId == Guid.Empty))
                throw new InvalidDataException("Hand history contains invalid round receipts.");
        }
    }

    private static bool Matches(string path, NativeMigrationFile file)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return bytes.LongLength == file.Bytes
            && Convert.ToHexString(SHA256.HashData(bytes)) == file.Sha256;
    }

    private static void AssertNotReparsePoint(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path))
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Migration may not follow a filesystem link: " + path);
    }

    private static void RequireLegacyStopped()
    {
        foreach (string name in new[] { "BlackjackCasino.Desktop", "BlackjackCasino.Updater" })
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                using (process)
                    if (!process.HasExited)
                        throw new InvalidOperationException(
                            "Close the installed casino and its updater before staging player data.");
            }
        }
    }
}
