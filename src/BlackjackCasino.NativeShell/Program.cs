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

        string? candidateDirectory = null;
        if (args is ["--candidate", var name])
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
                "Usage: BlackjackCasino.NativeShell.exe [--smoke|--web-smoke|--stage-migration NAME|--candidate NAME]");
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
