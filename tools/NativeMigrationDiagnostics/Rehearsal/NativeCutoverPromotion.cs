using System.Security.Cryptography;
using System.Text.Json;
using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.NativeShell;

internal sealed record CutoverPromotionRehearsal(
    int Version, string Name, string Checkpoint, string Candidate,
    DateTimeOffset StagedAtUtc, bool ProductionActivated, string SnapshotSha256);

/// <summary>
/// Rehearses a cutover by copying the verified native snapshot into a new
/// isolated writable workspace. Neither original WPF saves nor the candidate
/// are ever modified; production activation is deliberately unavailable.
/// </summary>
internal static class NativeCutoverPromotion
{
    private const int Version = 1;
    private const string Marker = "native-promotion-rehearsal.json";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    internal static string WorkspacePath(string production, string name)
    {
        // Reuse the exact path-component name policy already enforced for
        // immutable checkpoints. No relative paths or absolute paths allowed.
        _ = NativeCutoverCheckpoint.CheckpointPath(production, name);
        return Path.Combine(Path.GetFullPath(production),
            HostPreviewStorage.PreviewFolderName, "CutoverWorkspaces", name);
    }

    internal static string Stage(string production, string checkpointName,
        string name, bool checkProcesses = true)
    {
        string root = Path.GetFullPath(production);
        string target = WorkspacePath(root, name);
        string parent = Path.GetDirectoryName(target)!;
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("An existing live-rehearsal workspace cannot be overwritten.");
        if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();

        // The legacy WPF process and both migration steps now use the same
        // process-wide exclusive file handle. A second WPF cannot start while
        // current hashes and immutable snapshots are checked.
        using var legacyLease = HostWriterLease.Acquire(root);
        CutoverCheckpoint checkpoint = NativeCutoverCheckpoint.Verify(root, checkpointName);
        string candidate = NativeHostMigration.OpenCandidate(root, checkpoint.Candidate);
        using var nativeLease = AcquireCandidateLease(candidate);

        if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root)))
            throw new InvalidOperationException(
                "Legacy WPF data changed since the cutover backup. Create a new candidate and checkpoint.");
        if (!checkpoint.Native.SequenceEqual(NativeCutoverCheckpoint.Capture(candidate)))
            throw new InvalidOperationException(
                "NativeShell candidate changed since the cutover backup. Prepare a new checkpoint.");

        NativeCutoverCheckpoint.ValidateWalletAndProfile(root);
        NativeCutoverCheckpoint.ValidateWalletAndProfile(candidate);
        NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(root);
        NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(candidate);
        if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();

        string preview = Path.Combine(root, HostPreviewStorage.PreviewFolderName);
        AssertNotLink(preview);
        Directory.CreateDirectory(preview);
        AssertNotLink(Path.Combine(preview, "CutoverWorkspaces"));
        Directory.CreateDirectory(parent);
        AssertNotLink(parent);

        string working = Path.Combine(parent, ".preparing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);
        bool published = false;
        try
        {
            // Reconstruct from the read-only checkpoint, NOT from a moving
            // live wallet. File-by-file SHA-256 checks are repeated after copy.
            string immutableNative = Path.Combine(
                NativeCutoverCheckpoint.CheckpointPath(root, checkpointName), "native");
            NativeCutoverCheckpoint.CopyAndCheck(immutableNative, working, checkpoint.Native);
            NativeCutoverCheckpoint.ValidateWalletAndProfile(working);
            NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(working);
            if (!checkpoint.Native.SequenceEqual(NativeCutoverCheckpoint.Capture(working)))
                throw new InvalidDataException("Cutover rehearsal copy failed integrity checks.");

            string checkpointManifest = Path.Combine(
                NativeCutoverCheckpoint.CheckpointPath(root, checkpointName),
                "cutover-checkpoint.json");
            string checksum = Convert.ToHexString(SHA256.HashData(
                File.ReadAllBytes(checkpointManifest)));
            var marker = new CutoverPromotionRehearsal(Version, name,
                checkpointName, checkpoint.Candidate, DateTimeOffset.UtcNow,
                ProductionActivated: false, SnapshotSha256: checksum);
            using (var file = new FileStream(Path.Combine(working, Marker),
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, marker, Json);
                file.Flush(flushToDisk: true);
            }
            if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root))
                || !checkpoint.Native.SequenceEqual(NativeCutoverCheckpoint.Capture(candidate)))
                throw new IOException("Player state changed during the rehearsal handover.");
            if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException("A workspace with this name appeared during staging.");
            Directory.Move(working, target);
            published = true;
            return target;
        }
        finally
        {
            if (!published && Directory.Exists(working))
                Directory.Delete(working, recursive: true);
        }
    }

    internal static string Open(string production, string name)
    {
        string root = Path.GetFullPath(production);
        string target = WorkspacePath(root, name);
        string cursor = root;
        foreach (string segment in new[] { HostPreviewStorage.PreviewFolderName,
            "CutoverWorkspaces", name })
        {
            cursor = Path.Combine(cursor, segment);
            if (!Directory.Exists(cursor))
                throw new DirectoryNotFoundException("No verified cutover rehearsal exists: " + name);
            AssertNotLink(cursor);
        }
        string markerFile = Path.Combine(target, Marker);
        if (!File.Exists(markerFile) || (File.GetAttributes(markerFile) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The rehearsal workspace has no valid provenance marker.");
        if (new FileInfo(markerFile).Length > 64 * 1024)
            throw new InvalidDataException("Rehearsal marker exceeds its size bound.");
        CutoverPromotionRehearsal marker =
            JsonSerializer.Deserialize<CutoverPromotionRehearsal>(File.ReadAllBytes(markerFile), Json)
            ?? throw new InvalidDataException("Empty rehearsal marker.");
        if (marker.Version != Version || marker.Name != name || marker.ProductionActivated
            || string.IsNullOrWhiteSpace(marker.SnapshotSha256)
            || marker.SnapshotSha256.Length != 64)
            throw new InvalidDataException("Rehearsal provenance is invalid.");

        CutoverCheckpoint checkpoint = NativeCutoverCheckpoint.Verify(root, marker.Checkpoint);
        string immutableManifest = Path.Combine(
            NativeCutoverCheckpoint.CheckpointPath(root, marker.Checkpoint), "cutover-checkpoint.json");
        string digest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(immutableManifest)));
        if (marker.Candidate != checkpoint.Candidate || marker.SnapshotSha256 != digest)
            throw new InvalidDataException("Rehearsal checkpoint provenance has changed.");

        // The workspace is intentionally writable after first launch, so
        // current player files may differ from the original snapshot. Validate
        // their structure, not their historical hashes.
        NativeCutoverCheckpoint.ValidateWalletAndProfile(target);
        return target;
    }

    private static FileStream AcquireCandidateLease(string candidate)
    {
        try
        {
            return new FileStream(Path.Combine(candidate, ".native-host-writer.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new InvalidOperationException(
                "Close the NativeShell candidate before staging a rehearsal.", error);
        }
    }

    private static void AssertNotLink(string path)
    {
        if ((Directory.Exists(path) || File.Exists(path))
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Rehearsal may not follow a filesystem link: " + path);
    }
}
