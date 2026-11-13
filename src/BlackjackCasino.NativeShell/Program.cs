using BlackjackCasino.Desktop;

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
                "Usage: BlackjackCasino.NativeShell.exe [--smoke|--web-smoke|--stage-migration NAME|--candidate NAME|--prepare-cutover CANDIDATE BACKUP|--verify-cutover BACKUP|--rehearse-rollback BACKUP legacy\\|native DESTINATION|--stage-live-rehearsal BACKUP WORKSPACE|--live-rehearsal WORKSPACE]");
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
