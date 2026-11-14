using BlackjackCasino.HostShared;
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

        Console.Error.WriteLine("Unsupported casino argument. Migration diagnostics are provided as a separate offline development tool.");
        return 2;
    }
}
