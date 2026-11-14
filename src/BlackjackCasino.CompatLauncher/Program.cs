using System.Diagnostics;
using System.Windows.Forms;

namespace BlackjackCasino.CompatLauncher;

// Older installed versions and updater contracts require this executable name.
// It contains no WPF; gameplay, saves and updates belong to NativeShell.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 1 || (args.Length == 1
                && args[0] is not ("--legacy" or "--development")))
                throw new ArgumentException("Unsupported legacy launcher arguments.");

            string directory = AppContext.BaseDirectory;
            string executable = Path.Combine(directory, "BlackjackCasino.NativeShell.exe");
            if (!File.Exists(executable))
                throw new FileNotFoundException("The NativeShell executable is missing.", executable);

            _ = Process.Start(new ProcessStartInfo(executable)
            {
                WorkingDirectory = directory,
                UseShellExecute = false,
            }) ?? throw new InvalidOperationException("Could not start Blackjack Casino.");
            return 0;
        }
        catch (Exception error) when (error is ArgumentException or IOException
            or UnauthorizedAccessException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show("Blackjack Casino could not start. Your saves have not been changed.\n\n"
                + error.Message, "Blackjack Casino", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
