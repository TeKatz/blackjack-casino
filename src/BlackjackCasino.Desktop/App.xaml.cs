using System.Windows;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Localization.InstallAutomaticTranslation();
        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppCrashLog.Write("Unhandled WPF dispatcher exception.", e.Exception);
        // Keep the default crash behavior. The log makes failures actionable without
        // risking continuation from a partially initialized visual tree.
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        AppCrashLog.Write("Unhandled application-domain exception.", e.ExceptionObject as Exception);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CasinoAudio.Shutdown();
        base.OnExit(e);
    }
}
