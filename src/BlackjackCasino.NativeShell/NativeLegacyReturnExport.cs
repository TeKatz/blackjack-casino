using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal sealed record LegacyReturnExportManifest(
    int Version, string Name, string Workspace, string Checkpoint,
    DateTimeOffset CreatedUtc, bool AppliedToProduction,
    decimal LegacyBankroll, decimal NativeBankroll,
    CutoverFile[] ExportFiles, string[] ExcludedNativeJournals);

/// <summary>
/// Produces a WPF-readable rollback handoff from a CLOSED native workspace.
/// Never writes the old WPF profile or merges divergent host progress.
/// Native-only receipt journals remain preserved in the source workspace.
/// </summary>
internal static class NativeLegacyReturnExport
{
    private const int Version = 1;
    private const string Marker = "legacy-return-export.json";
    private const string BrowserRelative = "WebView2";
    private static readonly JsonSerializerOptions Json = CreateJson();
    private static readonly HashSet<string> NativeOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "native-promotion-rehearsal.json",
        "native-migration-manifest.json",
        "native-player-aggregate.json",
    };

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    internal static string ExportPath(string production, string name)
    {
        _ = NativeCutoverCheckpoint.CheckpointPath(production, name);
        return Path.Combine(Path.GetFullPath(production),
            HostPreviewStorage.PreviewFolderName, "LegacyReturnExports", name);
    }

    internal static string Stage(string production, string workspaceName, string exportName,
        bool checkProcesses = true)
    {
        string root = Path.GetFullPath(production);
        string target = ExportPath(root, exportName);
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("An existing legacy return export cannot be overwritten.");
        if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();

        using var legacyLease = HostWriterLease.Acquire(root);
        string workspace = NativeCutoverPromotion.Open(root, workspaceName);
        using var nativeLease = OpenWriterLease(workspace);
        string markerPath = Path.Combine(workspace, "native-promotion-rehearsal.json");
        CutoverPromotionRehearsal provenance =
            JsonSerializer.Deserialize<CutoverPromotionRehearsal>(
                File.ReadAllBytes(markerPath), Json)
            ?? throw new InvalidDataException("Native workspace provenance is missing.");
        CutoverCheckpoint checkpoint =
            NativeCutoverCheckpoint.Verify(root, provenance.Checkpoint);
        if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root)))
            throw new InvalidOperationException(
                "The original WPF player state has changed since cutover. "
                + "Do not replace either fork or merge different wallets.");

        NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(workspace);
        NativeCutoverCheckpoint.ValidateWalletAndProfile(workspace);
        // A successful native host close must leave no incomplete JSON or
        // unexpected save file outside the established migration inventory.
        foreach (string path in Directory.GetFiles(workspace, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(path);
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && !name.Equals(".native-host-writer.lock", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Unrecognized native save file requires explicit migration support: " + name);
        }
        CutoverFile[] nativeBefore = NativeCutoverCheckpoint.Capture(workspace);
        decimal legacyWallet = ReadWallet(Path.Combine(root, "player-state.json"));
        decimal nativeWallet = ReadWallet(Path.Combine(workspace, "player-state.json"));

        string preview = Path.Combine(root, HostPreviewStorage.PreviewFolderName);
        string parent = Path.GetDirectoryName(target)!;
        AssertPlainDirectory(preview);
        if (Directory.Exists(parent)) AssertPlainDirectory(parent);
        Directory.CreateDirectory(parent);
        string temporary = Path.Combine(parent,
            ".preparing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        bool published = false;
        try
        {
            // Export the native workspace's CURRENT state; never restore old
            // WPF JSON into it, and never copy private native receipt journals
            // that the WPF recovery logic cannot replay.
            CutoverFile[] portable = nativeBefore.Where(f =>
                !NativeOnly.Contains(f.RelativePath)).ToArray();
            NativeCutoverCheckpoint.CopyAndCheck(workspace, temporary, portable);
            ValidateLegacyReaders(temporary);
            CutoverFile[] saved = NativeCutoverCheckpoint.Capture(temporary);
            if (!portable.SequenceEqual(saved))
                throw new InvalidDataException("Legacy return lost a player-data file.");

            var report = new LegacyReturnExportManifest(Version, exportName,
                workspaceName, provenance.Checkpoint, DateTimeOffset.UtcNow,
                AppliedToProduction: false, legacyWallet, nativeWallet, saved,
                nativeBefore.Where(f => NativeOnly.Contains(f.RelativePath))
                    .Select(f => f.RelativePath).ToArray());
            using (var file = new FileStream(Path.Combine(temporary, Marker),
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, report, Json);
                file.Flush(flushToDisk: true);
            }

            // The legacy and native hashes must still match the two inputs
            // after the entire export. Any race invalidates the handoff.
            if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root))
                || !nativeBefore.SequenceEqual(NativeCutoverCheckpoint.Capture(workspace)))
                throw new IOException("A source save changed during legacy return export.");
            if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException("Return export name was claimed while staging.");
            Directory.Move(temporary, target);
            published = true;
            return target;
        }
        finally
        {
            if (!published && Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    internal static LegacyReturnExportManifest Verify(string production, string exportName)
    {
        string target = ExportPath(production, exportName);
        AssertPlainDirectory(target);
        string marker = Path.Combine(target, Marker);
        if (!File.Exists(marker) ||
            (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0
            || new FileInfo(marker).Length > 256 * 1024)
            throw new InvalidDataException("Legacy export manifest is absent or invalid.");
        LegacyReturnExportManifest report =
            JsonSerializer.Deserialize<LegacyReturnExportManifest>(
                File.ReadAllBytes(marker), Json)
            ?? throw new InvalidDataException("Legacy export manifest could not be read.");
        if (report.Version != Version || report.Name != exportName
            || report.AppliedToProduction || report.ExportFiles is null
            || report.ExcludedNativeJournals is null)
            throw new InvalidDataException("Legacy export is not an offline rehearsal.");
        // Provenance: a valid export must be rooted in the immutable backup.
        _ = NativeCutoverCheckpoint.Verify(production, report.Checkpoint);
        CutoverFile[] actual = NativeCutoverCheckpoint.Capture(target)
            .Where(f => f.RelativePath != Marker).ToArray();
        if (!report.ExportFiles.SequenceEqual(actual))
            throw new InvalidDataException("Legacy return export failed SHA-256 verification.");
        ValidateLegacyReaders(target);
        return report;
    }

    private static decimal ReadWallet(string path)
    {
        AppPreferences state = JsonSerializer.Deserialize<AppPreferences>(
            File.ReadAllBytes(path), Json)
            ?? throw new InvalidDataException("Missing player wallet in legacy return.");
        if (state.Bankroll < 0)
            throw new InvalidDataException("A negative casino wallet cannot be returned.");
        return state.Bankroll;
    }

    private static void ValidateLegacyReaders(string path)
    {
        NativeCutoverCheckpoint.ValidateWalletAndProfile(path);
        NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(path);
        AppPreferences original = JsonSerializer.Deserialize<AppPreferences>(
            File.ReadAllBytes(Path.Combine(path, "player-state.json")), Json)
            ?? throw new InvalidDataException("Missing exported preferences.");
        AppPreferences parsed = PlayerStateStore.Load(path);
        if (parsed.Bankroll != original.Bankroll)
            throw new InvalidDataException("WPF cannot read the exported casino bankroll.");
        string profileFile = Path.Combine(path, "profile.json");
        PlayerProfileState profile = JsonSerializer.Deserialize<PlayerProfileState>(
            File.ReadAllBytes(profileFile), Json)
            ?? throw new InvalidDataException("WPF cannot read the exported player profile.");
        if (profile.SchemaVersion is < 1 or > 6)
            throw new InvalidDataException("WPF profile schema is unsupported.");

        string historyFile = Path.Combine(path, "hand-history.json");
        if (File.Exists(historyFile))
        {
            RoundHistoryEntry[] history = JsonSerializer.Deserialize<RoundHistoryEntry[]>(
                File.ReadAllBytes(historyFile), Json)
                ?? throw new InvalidDataException("Invalid exported hand history.");
            if (history.Any(entry => entry is null || entry.RoundId == Guid.Empty))
                throw new InvalidDataException("Exported history contains invalid round receipts.");
            _ = PlayerProfileStore.LoadHistory(path);
        }
        if (File.Exists(Path.Combine(path, "manor-checkpoint.json")))
            _ = new ManorPersistence(path).Load();

        // WPF now compiles the SAME ledger as the native host. Validate it
        // as WPF would on startup, without replaying or changing the export.
        _ = NativeRouletteJournal.ValidateSettled(path);
    }

    private static FileStream OpenWriterLease(string workspace)
    {
        string marker = Path.Combine(workspace, ".native-host-writer.lock");
        if (File.Exists(marker) &&
            (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Native writer lock cannot be a filesystem link.");
        try
        {
            return new FileStream(marker, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                "Close the NativeShell workspace before exporting recent progress.", ex);
        }
    }

    private static void AssertPlainDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("Legacy export parent directory is missing.");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Legacy export may not follow a directory link.");
    }
}
