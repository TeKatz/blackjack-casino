using System.Windows;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class App : Application
{
    private bool startupWelcomeQueued;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Localization.InstallAutomaticTranslation();
        CasinoCursors.Install();
        CasinoUxEnhancements.Install();
        CasinoWindowFrame.Install();
        base.OnStartup(e);
        Dispatcher.BeginInvoke(QueueStartupWelcome, DispatcherPriority.Loaded);
    }

    private void QueueStartupWelcome()
    {
        if (startupWelcomeQueued)
        {
            return;
        }

        if (MainWindow is not BlackjackCasino.Desktop.MainWindow main)
        {
            Dispatcher.BeginInvoke(QueueStartupWelcome, DispatcherPriority.ContextIdle);
            return;
        }

        if (main.IsLoaded)
        {
            ShowStartupWelcome(main);
            return;
        }

        RoutedEventHandler? loaded = null;
        loaded = (_, _) =>
        {
            main.Loaded -= loaded;
            ShowStartupWelcome(main);
        };
        main.Loaded += loaded;
    }

    private void ShowStartupWelcome(BlackjackCasino.Desktop.MainWindow main)
    {
        if (startupWelcomeQueued)
        {
            return;
        }

        startupWelcomeQueued = true;
        // MainWindow was initially transparent to prevent the unfinished Blackjack table
        // flashing before the lobby was assembled in PresentationLoaded. Reveal the fully
        // initialized lobby *before* showing its owned welcome dialog, not after it closes.
        // Leaving the owner transparent causes the desktop to appear black behind the dialog.
        main.Opacity = 1;
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (main.IsLoaded && main.IsVisible)
                    StartupWelcomeWindow.ShowFor(main);
            }
            finally
            {
                // A failed or dismissed welcome must never strand an invisible casino.
                main.Opacity = 1;
            }
        }, DispatcherPriority.ContextIdle);
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
        // MainWindow performs final routine saves while closing. They are deliberately debounced
        // now, so force the latest preference/profile/history snapshots to disk before process exit.
        PlayerStateStore.FlushPending();
        PlayerProfileStore.FlushPending();
        CasinoAudio.Shutdown();
        base.OnExit(e);
    }
}
