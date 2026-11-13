using System.Text.Json;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal enum RollbackFault { None, AfterOldMove, AfterNewMove }

internal sealed record RollbackRehearsalJournal(
    int Version, string Name, string ExportName, string State,
    bool AppliedToProduction, CutoverFile[] OldFiles, CutoverFile[] NewFiles);

/// <summary>
/// Tests a journaled data-directory switch in an isolated sandbox. Does NOT
/// expose a method to modify the live WPF data root or change a default host.
/// </summary>
internal static class NativeLegacyRollbackRehearsal
{
    private const int Version = 1;
    private const string JournalName = "rollback-rehearsal-journal.json";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    internal static string PathFor(string production, string name)
    {
        _ = NativeCutoverCheckpoint.CheckpointPath(production, name);
        return Path.Combine(Path.GetFullPath(production),
            HostPreviewStorage.PreviewFolderName, "LegacyRollbackRehearsals", name);
    }

    internal static string Run(string production, string exportName, string name,
        bool checkProcesses = true, RollbackFault fault = RollbackFault.None)
    {
        string root = Path.GetFullPath(production);
        string target = PathFor(root, name);
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("A rollback rehearsal with this name already exists.");
        if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();
        using var legacyLease = HostWriterLease.Acquire(root);
        LegacyReturnExportManifest exported = NativeLegacyReturnExport.Verify(root, exportName);
        CutoverCheckpoint checkpoint = NativeCutoverCheckpoint.Verify(root, exported.Checkpoint);
        if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root)))
            throw new InvalidOperationException("WPF progress diverged after the cutover checkpoint.");

        string workspace = NativeCutoverPromotion.Open(root, exported.Workspace);
        using var nativeLease = OpenNativeWriter(workspace);
        NativeCutoverCheckpoint.EnsureNoUnfinishedJournal(workspace);
        RequireNoUnbridgedRouletteReceipts(workspace);

        CutoverFile[] currentNative = NativeCutoverCheckpoint.Capture(workspace)
            .Where(f => !exported.ExcludedNativeJournals.Contains(
                f.RelativePath, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (!currentNative.SequenceEqual(exported.ExportFiles))
            throw new InvalidOperationException(
                "NativeShell progressed since this export. Create a new verified return export.");
        if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();

        string preview = Path.Combine(root, HostPreviewStorage.PreviewFolderName);
        RequirePlainDirectory(preview);
        string parent = Path.GetDirectoryName(target)!;
        if (Directory.Exists(parent)) RequirePlainDirectory(parent);
        Directory.CreateDirectory(parent);
        RequirePlainDirectory(parent);

        string temp = Path.Combine(parent, ".preparing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        bool published = false;
        try
        {
            string oldDir = Path.Combine(temp, "current");
            string newDir = Path.Combine(temp, "incoming");
            Directory.CreateDirectory(oldDir);
            Directory.CreateDirectory(newDir);
            NativeCutoverCheckpoint.CopyAndCheck(
                Path.Combine(NativeCutoverCheckpoint.CheckpointPath(root, exported.Checkpoint),
                    "legacy"), oldDir, checkpoint.Legacy);
            NativeCutoverCheckpoint.CopyAndCheck(
                NativeLegacyReturnExport.ExportPath(root, exportName),
                newDir, exported.ExportFiles);
            VerifySide(oldDir, checkpoint.Legacy);
            VerifySide(newDir, exported.ExportFiles);

            var journal = new RollbackRehearsalJournal(
                Version, name, exportName, "prepared", false,
                checkpoint.Legacy, exported.ExportFiles);
            WriteJournal(temp, journal);
            if (!checkpoint.Legacy.SequenceEqual(NativeCutoverCheckpoint.Capture(root))
                || !currentNative.SequenceEqual(NativeCutoverCheckpoint.Capture(workspace)
                    .Where(f => !exported.ExcludedNativeJournals.Contains(
                        f.RelativePath, StringComparer.OrdinalIgnoreCase)).ToArray()))
                throw new IOException("Source data changed during sandbox preparation.");
            if (checkProcesses) NativeCutoverCheckpoint.RequireCasinoClosed();
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException("Rehearsal name was claimed during preparation.");
            Directory.Move(temp, target);
            published = true;

            // Atomic same-volume renames. If power fails between them, the
            // journal allows recovery of the OLD WPF directory and preserves
            // the NEW NativeShell directory separately for investigation.
            Directory.Move(Path.Combine(target, "current"), Path.Combine(target, "previous"));
            if (fault == RollbackFault.AfterOldMove)
                throw new IOException("Simulated crash after old WPF directory move.");
            Directory.Move(Path.Combine(target, "incoming"), Path.Combine(target, "current"));
            if (fault == RollbackFault.AfterNewMove)
                throw new IOException("Simulated crash after native directory move.");
            VerifySide(Path.Combine(target, "current"), exported.ExportFiles);
            VerifySide(Path.Combine(target, "previous"), checkpoint.Legacy);
            WriteJournal(target, journal with { State = "completed" });
            return target;
        }
        finally
        {
            if (!published && Directory.Exists(temp))
                Directory.Delete(temp, recursive: true);
        }
    }

    internal static RollbackRehearsalJournal Inspect(string production, string name)
    {
        string path = PathFor(production, name);
        RollbackRehearsalJournal state = ReadJournal(path, name);
        if (state.State == "completed")
        {
            VerifySide(Path.Combine(path, "current"), state.NewFiles);
            VerifySide(Path.Combine(path, "previous"), state.OldFiles);
        }
        else if (state.State == "rolled-back")
            VerifySide(Path.Combine(path, "current"), state.OldFiles);
        else if (state.State != "prepared")
            throw new InvalidDataException("Unknown rollback rehearsal state.");
        return state;
    }

    internal static string Recover(string production, string name)
    {
        string path = PathFor(production, name);
        RollbackRehearsalJournal journal = ReadJournal(path, name);
        if (journal.State != "prepared")
            throw new InvalidOperationException("Only interrupted rehearsals can be recovered.");
        string current = Path.Combine(path, "current");
        string previous = Path.Combine(path, "previous");
        if (Directory.Exists(previous))
        {
            VerifySide(previous, journal.OldFiles);
            if (Directory.Exists(current))
            {
                VerifySide(current, journal.NewFiles);
                string interrupted = Path.Combine(path, "interrupted-new");
                if (Directory.Exists(interrupted))
                    throw new IOException("Unresolved interrupted-native evidence already exists.");
                Directory.Move(current, interrupted);
            }
            Directory.Move(previous, current);
        }
        else
            VerifySide(current, journal.OldFiles);
        VerifySide(current, journal.OldFiles);
        WriteJournal(path, journal with { State = "rolled-back" });
        return path;
    }

    private static void RequireNoUnbridgedRouletteReceipts(string workspace)
    {
        string file = Path.Combine(workspace, "native-roulette-checkpoint.json");
        if (!File.Exists(file)) return;
        if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Roulette journal cannot be a filesystem link.");
        NativeRouletteCheckpoint journal = JsonSerializer.Deserialize<NativeRouletteCheckpoint>(
            File.ReadAllBytes(file), ManorPersistence.Json)
            ?? throw new InvalidDataException("Empty native roulette journal.");
        if (journal.Version != 1 || journal.Pending || journal.Receipts is null)
            throw new InvalidDataException("Native roulette journal has unresolved state.");
        if (journal.Receipts.Length > 0)
            throw new InvalidOperationException(
                "WPF rollback needs roulette receipt-ID continuity; "
                + "native-only receipts must not be discarded or replayed.");
    }

    private static FileStream OpenNativeWriter(string workspace)
    {
        try
        {
            return new FileStream(Path.Combine(workspace, ".native-host-writer.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("Close the NativeShell session before rollback.", ex);
        }
    }

    private static void VerifySide(string directory, CutoverFile[] expected)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("Missing rollback directory: " + directory);
        if (!NativeCutoverCheckpoint.Capture(directory).SequenceEqual(expected))
            throw new InvalidDataException("Rollback files or SHA-256 checksums do not match.");
        NativeCutoverCheckpoint.ValidateWalletAndProfile(directory);
    }

    private static RollbackRehearsalJournal ReadJournal(string folder, string name)
    {
        RequirePlainDirectory(folder);
        string file = Path.Combine(folder, JournalName);
        if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0
            || new FileInfo(file).Length > 128 * 1024)
            throw new InvalidDataException("Rollback rehearsal journal is missing or invalid.");
        RollbackRehearsalJournal journal = JsonSerializer.Deserialize<RollbackRehearsalJournal>(
            File.ReadAllBytes(file), Json)
            ?? throw new InvalidDataException("Rollback rehearsal journal is empty.");
        if (journal.Version != Version || journal.Name != name || journal.AppliedToProduction
            || journal.OldFiles is null || journal.NewFiles is null)
            throw new InvalidDataException("Rollback rehearsal is not an approved sandbox.");
        return journal;
    }

    private static void WriteJournal(string folder, RollbackRehearsalJournal journal)
    {
        string path = Path.Combine(folder, JournalName);
        string temp = path + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, journal, Json);
            file.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }

    private static void RequirePlainDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Rollback rehearsal cannot follow directory links.");
    }
}
