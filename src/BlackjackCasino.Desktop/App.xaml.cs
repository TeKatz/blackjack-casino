using System.Windows;
using System.Windows.Threading;
using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.Desktop;

public partial class App : Application
{
    private bool packagedArtworkSmoke;
    private HostWriterLease? playerWriterLease;

    protected override void OnStartup(StartupEventArgs e)
    {
        // A diagnostic launch exercises the real published single-file resource loader
        // before any player stores or normal startup windows are initialized.
        if (e.Args.Length == 2 && e.Args[0] == "--packaged-artwork-smoke")
        {
            packagedArtworkSmoke = true;
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
        // Acquire before MainWindow reads/replays any wallet, history or Manor
        // journal. This protects an upcoming native cutover against an old WPF
        // process starting while its source data is being staged.
        try
        {
            string playerRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlackjackCasino");
            playerWriterLease = HostWriterLease.Acquire(playerRoot);
        }
        catch (Exception error) when (error is InvalidOperationException
            or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(error.Message, "Blackjack Casino already running",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }
        base.OnStartup(e);
        // Keep normal window creation explicit so the isolated diagnostic launch
        // can exit without creating MainWindow or touching player preferences.
        new MainWindow().Show();
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
        try
        {
            PlayerStateStore.FlushPending();
            PlayerProfileStore.FlushPending();
            CasinoAudio.Shutdown();
        }
        finally
        {
            // A second host may read the saves only after every debounced
            // legacy writer has finished, even on an exceptional shutdown.
            playerWriterLease?.Dispose();
            base.OnExit(e);
        }
    }
}
