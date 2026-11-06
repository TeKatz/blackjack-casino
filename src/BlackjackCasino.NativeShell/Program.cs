using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--smoke"])
            return NativeShellContractSmoke.Run();

        if (args.Length != 0)
        {
            Console.Error.WriteLine("Usage: BlackjackCasino.NativeShell.exe [--smoke]");
            return 2;
        }

        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new NativeShellForm());
        return 0;
    }
}
