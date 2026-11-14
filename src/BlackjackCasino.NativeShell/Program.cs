using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.NativeShell;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--smoke"])
            return NativeShellContractSmoke.Run();
        if (args is ["--web-smoke"])
        {
            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            return NativeShellWebSmoke.Run();
        }

        // NativeShell is the release entry point. Keep the already-used
        // Development save directory; import an older WPF save only once.
        if (args is [] or ["--development"] or ["--development", "--wait-pid", _])
        {
            try
            {
                if (args is ["--development", "--wait-pid", var parentText])
                {
                    if (!int.TryParse(parentText, out int parentPid) || parentPid <= 0)
                        throw new ArgumentException("Invalid parent process ID.");
                    try
                    {
                        using var parent = System.Diagnostics.Process.GetProcessById(parentPid);
                        if (!parent.WaitForExit(TimeSpan.FromSeconds(30)))
                            throw new IOException("The previous desktop process has not exited.");
                    }
                    catch (ArgumentException) { /* Parent already exited. */ }
                }
                string root = NativeHostMigration.ProductionDirectory;
                string saved = NativeHostMigration.CandidatePath(root, "development");
                string currentNative = HostPreviewStorage.DefaultDirectory(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                // Preserve a pre-existing fresh-install NativeShell profile too:
                // an older WPF save copied in later must never take precedence.
                bool existingNativeSave =
                    File.Exists(Path.Combine(currentNative, "player-state.json"))
                    || File.Exists(Path.Combine(currentNative, "profile.json"));
                bool existingLegacySave =
                    File.Exists(Path.Combine(root, "player-state.json"))
                    || File.Exists(Path.Combine(root, "profile.json"));
                string destination = HostManagedProfilePolicy.Select(
                    Directory.Exists(saved), existingNativeSave, existingLegacySave) switch
                {
                    ManagedProfileSource.Candidate =>
                        NativeHostMigration.OpenCandidate(root, "development"),
                    ManagedProfileSource.ExistingNative or ManagedProfileSource.FreshNative =>
                        currentNative,
                    ManagedProfileSource.ImportLegacy =>
                        NativeHostMigration.Stage(root, "development"),
                    _ => throw new InvalidOperationException("Unsupported NativeShell startup profile."),
                };

                System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                System.Windows.Forms.Application.Run(new NativeShellForm(
                    destination, migrationCandidate: false, managedRelease: true));
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException
                or ArgumentException or System.ComponentModel.Win32Exception)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Blackjack Casino could not safely open your existing save.\n\n"
                    + error.Message + "\n\nNo existing files were overwritten. "
                    + "Close other casino instances and use the verified migration "
                    + "recovery tools before retrying.",
                    "NativeShell startup blocked", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return 1;
            }
        }

        if (args is ["--stage-migration", var candidateName])
        {
            try
            {
                string target = NativeHostMigration.Stage(
                    NativeHostMigration.ProductionDirectory, candidateName);
                System.Windows.Forms.MessageBox.Show(
                    "A verified COPY of your player data was staged at:\n" + target
                    + "\n\nThe installed casino and updater are unchanged. "
                    + "Start this executable with --candidate " + candidateName
                    + " to test the new host.",
                    "Native Host Migration Candidate",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or InvalidDataException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Migration blocked",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return 1;
            }
        }

        if (args is ["--prepare-cutover", var existingCandidate, var checkpointName])
        {
            try
            {
                string backup = NativeCutoverCheckpoint.Prepare(
                    NativeHostMigration.ProductionDirectory, existingCandidate, checkpointName);
                System.Windows.Forms.MessageBox.Show(
                    "Both the WPF save and NativeShell test save were verified and backed up to:\n"
                    + backup + "\n\nNo installed save data or default launcher was changed.",
                    "Host Cutover Safety Check",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Cutover blocked",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return 1;
            }
        }
        if (args is ["--verify-cutover", var checkpoint])
        {
            try
            {
                CutoverCheckpoint result = NativeCutoverCheckpoint.Verify(
                    NativeHostMigration.ProductionDirectory, checkpoint);
                System.Windows.Forms.MessageBox.Show(
                    "Backup verified for " + result.Candidate + ".\n"
                    + "WPF files: " + result.Legacy.Length + "\n"
                    + "NativeShell files: " + result.Native.Length
                    + "\n\nProduction activation remains disabled.",
                    "Verified Cutover Backup");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Cutover backup invalid",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return 1;
            }
        }
        if (args is ["--rehearse-rollback", var restoreCheckpoint,
            var restoreKind, var restoreDestination])
        {
            try
            {
                string restored = NativeCutoverCheckpoint.RestoreRehearsal(
                    NativeHostMigration.ProductionDirectory, restoreCheckpoint,
                    restoreKind, restoreDestination);
                System.Windows.Forms.MessageBox.Show(
                    "The verified " + restoreKind + " backup was restored to the separate test folder:\n"
                    + restored + "\n\nThe installed player data was NOT changed.",
                    "Rollback Rehearsal");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Rollback rehearsal blocked",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return 1;
            }
        }

        if (args is ["--stage-live-rehearsal", var rehearsalCheckpointName, var workspaceName])
        {
            try
            {
                string workspace = NativeCutoverPromotion.Stage(
                    NativeHostMigration.ProductionDirectory, rehearsalCheckpointName, workspaceName);
                System.Windows.Forms.MessageBox.Show(
                    "A verified, independently writable NativeShell rehearsal was staged at:\n"
                    + workspace + "\n\nThe installed WPF save and original candidate are unchanged.",
                    "Native cutover rehearsal");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Rehearsal blocked");
                return 1;
            }
        }

        if (args is ["--stage-legacy-return", var nativeWorkspaceName, var returnName])
        {
            try
            {
                string export = NativeLegacyReturnExport.Stage(
                    NativeHostMigration.ProductionDirectory, nativeWorkspaceName, returnName);
                LegacyReturnExportManifest report = NativeLegacyReturnExport.Verify(
                    NativeHostMigration.ProductionDirectory, returnName);
                System.Windows.Forms.MessageBox.Show(
                    "A verified WPF-compatible copy of the latest NativeShell progress is available at:\n"
                    + export + "\n\nOriginal WPF wallet: " + report.LegacyBankroll
                    + "\nNativeShell wallet: " + report.NativeBankroll
                    + "\n\nThis is a DRY RUN. No installed saves or startup settings changed.",
                    "Offline legacy return export");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message,
                    "Legacy return export blocked");
                return 1;
            }
        }
        if (args is ["--verify-legacy-return", var returnExportName])
        {
            try
            {
                LegacyReturnExportManifest report = NativeLegacyReturnExport.Verify(
                    NativeHostMigration.ProductionDirectory, returnExportName);
                System.Windows.Forms.MessageBox.Show(
                    "Export " + report.Name + " verified.\n"
                    + "Files: " + report.ExportFiles.Length
                    + "\nOld WPF wallet: " + report.LegacyBankroll
                    + "\nCurrent NativeShell wallet: " + report.NativeBankroll
                    + "\n\nNo installed data was overwritten.",
                    "Legacy return verified");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message,
                    "Legacy return verification failed");
                return 1;
            }
        }

        if (args is ["--rehearse-wpf-return", var verifiedExport, var rollbackRehearsal])
        {
            try
            {
                string folder = NativeLegacyRollbackRehearsal.Run(
                    NativeHostMigration.ProductionDirectory, verifiedExport, rollbackRehearsal);
                System.Windows.Forms.MessageBox.Show(
                    "The WPF-style return was tested in an isolated folder:\n" + folder
                    + "\n\nPrevious WPF and latest NativeShell saves are preserved."
                    + "\nNo installed saves were changed.", "WPF rollback rehearsal");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Rollback rehearsal blocked");
                return 1;
            }
        }
        if (args is ["--recover-wpf-rehearsal", var failedRehearsal])
        {
            try
            {
                _ = NativeLegacyRollbackRehearsal.Recover(
                    NativeHostMigration.ProductionDirectory, failedRehearsal);
                System.Windows.Forms.MessageBox.Show(
                    "The previous WPF state was restored in the test folder."
                    + "\nThe installed casino was not changed.", "Rollback test recovered");
                return 0;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Rollback recovery blocked");
                return 1;
            }
        }

        string? candidateDirectory = null;
        if (args is ["--live-rehearsal", var workspaceNameToOpen])
        {
            try
            {
                candidateDirectory = NativeCutoverPromotion.Open(
                    NativeHostMigration.ProductionDirectory, workspaceNameToOpen);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Invalid rehearsal");
                return 1;
            }
        }
        else if (args is ["--candidate", var name])
        {
            try { candidateDirectory = NativeHostMigration.OpenCandidate(
                NativeHostMigration.ProductionDirectory, name); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or InvalidDataException or InvalidOperationException or ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show(error.Message, "Invalid migration candidate");
                return 1;
            }
        }
        else if (args.Length != 0)
        {
            Console.Error.WriteLine(
                "Usage: BlackjackCasino.NativeShell.exe [--smoke|--web-smoke|--stage-migration NAME|--candidate NAME|--prepare-cutover CANDIDATE BACKUP|--verify-cutover BACKUP|--rehearse-rollback BACKUP legacy\\|native DESTINATION|--stage-live-rehearsal BACKUP WORKSPACE|--live-rehearsal WORKSPACE|--stage-legacy-return WORKSPACE EXPORT|--verify-legacy-return EXPORT|--rehearse-wpf-return EXPORT NAME|--recover-wpf-rehearsal NAME]");
            return 2;
        }

        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(
            new NativeShellForm(candidateDirectory, migrationCandidate: candidateDirectory is not null));
        return 0;
    }
}
