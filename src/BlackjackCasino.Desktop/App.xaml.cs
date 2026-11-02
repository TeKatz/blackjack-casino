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
        Dispatcher.BeginInvoke(() => StartupWelcomeWindow.ShowFor(main), DispatcherPriority.ContextIdle);
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
