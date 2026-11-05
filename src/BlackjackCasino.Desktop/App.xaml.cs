using System.Windows;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class App : Application
{
    private bool packagedArtworkSmoke;

    protected override void OnStartup(StartupEventArgs e)
    {
        // A diagnostic launch exercises the real published single-file resource loader
        // before any player stores or normal startup windows are initialized.
        if (e.Args.Length == 2 && e.Args[0] == "--packaged-artwork-smoke")
        {
            packagedArtworkSmoke = true;
            StartupUri = null!;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(PackagedArtworkSmoke.Run(e.Args[1]));
            return;
        }
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Localization.InstallAutomaticTranslation();
        CasinoCursors.Install();
        CasinoUxEnhancements.Install();
        CasinoWindowFrame.Install();
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
        if (packagedArtworkSmoke) { base.OnExit(e); return; }
        // MainWindow performs final routine saves while closing. They are deliberately debounced
        // now, so force the latest preference/profile/history snapshots to disk before process exit.
        PlayerStateStore.FlushPending();
        PlayerProfileStore.FlushPending();
        CasinoAudio.Shutdown();
        base.OnExit(e);
    }
}
