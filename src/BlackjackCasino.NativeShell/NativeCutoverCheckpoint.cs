using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal sealed record CutoverFile(string RelativePath, long Bytes, string Sha256);
internal sealed record CutoverCheckpoint(
    int Version, string Candidate, DateTimeOffset CreatedUtc,
    bool ProductionActivated, CutoverFile[] Legacy, CutoverFile[] Native);

/// <summary>
/// Creates an immutable, restorable two-sided save snapshot without promoting
/// either host or touching the installed casino's JSON/Chromium data.
/// </summary>
internal static class NativeCutoverCheckpoint
{
    private const int Version = 1;
    private const int MaxFiles = 640;
    private const long MaxBytes = 256L * 1024 * 1024;
    private const string Marker = "cutover-checkpoint.json";
    private static readonly string BrowserStorage = Path.Combine(
        "WebView2", "Default", "Local Storage", "leveldb");
    private static readonly JsonSerializerOptions Json = CreateJson();

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    internal static string CheckpointPath(string production, string backupName)
    {
        CheckName(backupName);
        return Path.Combine(Path.GetFullPath(production), "HostCutoverBackups", backupName);
    }

    private static void CheckName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 40
            || !char.IsAsciiLetterOrDigit(value[0])
            || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new ArgumentException("Invalid cutover checkpoint name.");
    }

    /// <summary>
    /// Requires the production state still to match the imported source.
    /// If the WPF user played since staging, a new candidate must be imported;
    /// this method never merges two independently modified wallets.
    /// </summary>
    internal static string Prepare(string production, string candidateName,
        string backupName, bool checkProcesses = true)
    {
        string source = Path.GetFullPath(production);
        string candidate = NativeHostMigration.OpenCandidate(source, candidateName);
        string target = CheckpointPath(source, backupName);
        string parent = Path.GetDirectoryName(target)!;

        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("An existing cutover checkpoint cannot be replaced.");
        AssertPlainDirectory(source);
        AssertPlainDirectory(candidate);
        AssertPlainDirectory(parent, required: false);
        if (checkProcesses) RequireCasinoClosed();
        // Prevent the WPF owner from changing the original save while hashes
        // and both sides of the checkpoint are being captured.
        using var productionLease = HostWriterLease.Acquire(source);

        // Uses the same exclusive lease as a live NativePreviewRuntime.
        // An open NativeShell window cannot be snapshotted mid-transaction.
        using var lease = OpenCandidateWriterLease(candidate);
        NativeMigrationManifest imported = ReadCandidateManifest(candidate, candidateName);
        if (imported.Version != 2)
            throw new InvalidDataException(
                "Cutover preparation requires a v2 import with browser-storage checksums.");
        CutoverFile[] original = Capture(source);
        CutoverFile[] native = Capture(candidate);
        EnsureOriginalUnchanged(imported, original);
        ValidateWalletAndProfile(source);
        ValidateWalletAndProfile(candidate);
        EnsureNoUnfinishedJournal(source);
        EnsureNoUnfinishedJournal(candidate);
        if (checkProcesses) RequireCasinoClosed();

        Directory.CreateDirectory(parent);
        AssertPlainDirectory(parent);
        string working = Path.Combine(parent, ".preparing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);
        bool committed = false;
        try
        {
            CopyAndCheck(source, Path.Combine(working, "legacy"), original);
            CopyAndCheck(candidate, Path.Combine(working, "native"), native);
            if (!original.SequenceEqual(Capture(source)) || !native.SequenceEqual(Capture(candidate)))
                throw new IOException("Player state changed during the cutover checkpoint.");
            if (checkProcesses) RequireCasinoClosed();
            var evidence = new CutoverCheckpoint(Version, candidateName,
                DateTimeOffset.UtcNow, false, original, native);
            using (var stream = new FileStream(Path.Combine(working, Marker),
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, evidence, Json);
                stream.Flush(flushToDisk: true);
            }
            VerifySnapshot(working, evidence);
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException("Checkpoint name was claimed during preparation.");
            Directory.Move(working, target);
            committed = true;
            return target;
        }
        finally
        {
            if (!committed && Directory.Exists(working))
                Directory.Delete(working, recursive: true);
        }
    }

    /// <summary>
    /// Verifies both historical snapshots without changing any installed data.
    /// </summary>
    internal static CutoverCheckpoint Verify(string production, string backupName)
    {
        string target = CheckpointPath(production, backupName);
        AssertPlainDirectory(target);
        string marker = Path.Combine(target, Marker);
        AssertPlainFile(marker);
        if (new FileInfo(marker).Length > 256 * 1024)
            throw new InvalidDataException("Checkpoint manifest exceeds its size limit.");
        CutoverCheckpoint checkpoint = JsonSerializer.Deserialize<CutoverCheckpoint>(
            File.ReadAllBytes(marker), Json)
            ?? throw new InvalidDataException("Cutover checkpoint has no manifest.");
        if (checkpoint.Version != Version || checkpoint.ProductionActivated
            || checkpoint.Legacy is null || checkpoint.Native is null)
            throw new InvalidDataException("Checkpoint is not an immutable pre-cutover snapshot.");
        VerifySnapshot(target, checkpoint);
        return checkpoint;
    }

    /// <summary>
    /// Proves that rollback bytes can be reconstructed in a NEW directory.
    /// It will never write into the production casino, a candidate, or an
    /// existing destination; applying a rollback requires a later explicit gate.
    /// </summary>
    internal static string RestoreRehearsal(string production, string backupName,
        string kind, string destination)
    {
        CutoverCheckpoint checkpoint = Verify(production, backupName);
        CutoverFile[] list = kind switch
        {
            "legacy" => checkpoint.Legacy,
            "native" => checkpoint.Native,
            _ => throw new ArgumentException("Restore rehearsal kind must be legacy or native."),
        };
        string source = Path.Combine(CheckpointPath(production, backupName), kind);
        string output = Path.GetFullPath(destination);
        string casino = Path.GetFullPath(production);
        string candidateRoot = Path.Combine(casino, "NativeShellPreview");
        if (output.Equals(casino, StringComparison.OrdinalIgnoreCase)
            || output.StartsWith(casino + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || casino.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || output.Equals(candidateRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Rollback rehearsal output must be separate from the installed player data.");
        if (Directory.Exists(output) || File.Exists(output))
            throw new IOException("Rollback rehearsal refuses to overwrite existing files.");
        AssertPlainDirectory(Path.GetDirectoryName(output)!);
        Directory.CreateDirectory(output);
        bool done = false;
        try
        {
            CopyAndCheck(source, output, list);
            done = true;
            return output;
        }
        finally
        {
            if (!done && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static FileStream OpenCandidateWriterLease(string directory)
    {
        try
        {
            return new FileStream(Path.Combine(directory, ".native-host-writer.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new InvalidOperationException(
                "Close the NativeShell player session before preparing a cutover.", error);
        }
    }

    private static NativeMigrationManifest ReadCandidateManifest(string path, string name)
    {
        string manifest = Path.Combine(path, "native-migration-manifest.json");
        AssertPlainFile(manifest);
        if (new FileInfo(manifest).Length > 256 * 1024)
            throw new InvalidDataException("Migration manifest is unreasonably large.");
        NativeMigrationManifest state = JsonSerializer.Deserialize<NativeMigrationManifest>(
            File.ReadAllBytes(manifest), Json)
            ?? throw new InvalidDataException("Migration manifest could not be read.");
        if (state.Candidate != name || state.Files is null || state.BrowserFiles is null)
            throw new InvalidDataException("Migration manifest is incomplete.");
        return state;
    }

    private static void EnsureOriginalUnchanged(NativeMigrationManifest imported, CutoverFile[] current)
    {
        CutoverFile[] baseline = imported.Files
            .Select(f => new CutoverFile(f.Name, f.Bytes, f.Sha256))
            .Concat(imported.BrowserFiles.Select(f => new CutoverFile(
                Path.Combine(BrowserStorage, f.Name), f.Bytes, f.Sha256)))
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
        if (!baseline.SequenceEqual(current))
            throw new InvalidOperationException(
                "The installed WPF save changed since this NativeShell candidate was imported. "
                + "Create a new migration candidate; do not merge divergent progress.");
    }

    internal static CutoverFile[] Capture(string source)
    {
        AssertPlainDirectory(source);
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(source, "*.json", SearchOption.TopDirectoryOnly));
        string browser = Path.Combine(source, BrowserStorage);
        string cursor = source;
        foreach (string part in new[] { "WebView2", "Default", "Local Storage", "leveldb" })
        {
            cursor = Path.Combine(cursor, part);
            AssertPlainDirectory(cursor, required: false);
        }
        if (Directory.Exists(browser))
            files.AddRange(Directory.GetFiles(browser, "*", SearchOption.TopDirectoryOnly));
        if (files.Count > MaxFiles)
            throw new InvalidDataException("Too many files in the cutover snapshot.");
        long total = 0;
        var entries = new List<CutoverFile>(files.Count);
        foreach (string path in files)
        {
            AssertPlainFile(path);
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            total = checked(total + stream.Length);
            if (total > MaxBytes)
                throw new InvalidDataException("Cutover snapshot exceeds safe size limit.");
            entries.Add(new CutoverFile(Path.GetRelativePath(source, path),
                stream.Length, Convert.ToHexString(SHA256.HashData(stream))));
        }
        return entries.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    internal static void ValidateWalletAndProfile(string directory)
    {
        foreach (string file in new[] { "player-state.json", "profile.json" })
            AssertPlainFile(Path.Combine(directory, file));
        AppPreferences? state = JsonSerializer.Deserialize<AppPreferences>(
            File.ReadAllBytes(Path.Combine(directory, "player-state.json")), Json);
        PlayerProfileState? profile = JsonSerializer.Deserialize<PlayerProfileState>(
            File.ReadAllBytes(Path.Combine(directory, "profile.json")), Json);
        if (state is null || state.Bankroll < 0 || profile is null
            || profile.SchemaVersion < 1 || profile.SchemaVersion > 6
            || string.IsNullOrWhiteSpace(profile.DisplayName))
            throw new InvalidDataException("Cutover source has an invalid wallet or profile.");
        if (File.Exists(Path.Combine(directory, "hand-history.json")))
        {
            using var history = JsonDocument.Parse(File.ReadAllBytes(
                Path.Combine(directory, "hand-history.json")));
            if (history.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Cutover source has invalid hand history.");
        }
    }

    internal static void EnsureNoUnfinishedJournal(string directory)
    {
        foreach (string name in new[] { "manor-checkpoint.json",
            "native-player-aggregate.json", "native-roulette-checkpoint.json" })
        {
            string path = Path.Combine(directory, name);
            if (!File.Exists(path)) continue;
            using JsonDocument file = JsonDocument.Parse(File.ReadAllBytes(path));
            JsonElement root = file.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Invalid transactional journal: " + name);
            foreach (string field in new[] { "Pending", "NeedsRecovery" })
                if (root.TryGetProperty(field, out JsonElement value)
                    && value.ValueKind == JsonValueKind.True)
                    throw new InvalidOperationException(
                        "Recover the pending game transaction before cutover: " + name);
        }
    }

    internal static void CopyAndCheck(string source, string target, CutoverFile[] files)
    {
        foreach (CutoverFile file in files)
        {
            string origin = Path.GetFullPath(Path.Combine(source, file.RelativePath));
            string dest = Path.GetFullPath(Path.Combine(target, file.RelativePath));
            if (!origin.StartsWith(Path.GetFullPath(source) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                || !dest.StartsWith(Path.GetFullPath(target) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Cutover manifest contains a path outside the snapshot.");
            AssertPlainFile(origin);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(origin, dest, overwrite: false);
            AssertMatches(dest, file);
        }
    }

    private static void VerifySnapshot(string directory, CutoverCheckpoint evidence)
    {
        foreach (var section in new[] { ("legacy", evidence.Legacy), ("native", evidence.Native) })
        {
            var actual = Capture(Path.Combine(directory, section.Item1));
            if (!actual.SequenceEqual(section.Item2))
                throw new InvalidDataException("Cutover snapshot failed SHA-256 verification: "
                    + section.Item1);
        }
    }

    private static void AssertMatches(string path, CutoverFile expected)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length != expected.Bytes ||
            !string.Equals(Convert.ToHexString(SHA256.HashData(stream)),
                expected.Sha256, StringComparison.Ordinal))
            throw new InvalidDataException("Cutover snapshot bytes do not match: "
                + expected.RelativePath);
    }

    private static void AssertPlainDirectory(string path, bool required = true)
    {
        if (!Directory.Exists(path))
        {
            if (required) throw new DirectoryNotFoundException(path);
            return;
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Filesystem links are not permitted for cutover: " + path);
    }

    private static void AssertPlainFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Missing cutover file.", path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Filesystem links are not permitted for cutover: " + path);
    }

    internal static void RequireCasinoClosed()
    {
        foreach (string name in new[] { "BlackjackCasino.Desktop",
            "BlackjackCasino.Updater", "BlackjackCasino.NativeShell",
            "BlackjackCasino.HostCutoverLauncher" })
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                using (process)
                    if (process.Id != Environment.ProcessId && !process.HasExited)
                        throw new InvalidOperationException(
                            "Close all casino hosts, launchers and updater before preparing a cutover.");
            }
        }
    }
}
