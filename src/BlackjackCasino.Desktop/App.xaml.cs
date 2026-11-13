using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
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
        // Development channel ONLY: the installed app's existing shortcut and
        // updater still restart Desktop.exe. After a verified Dev package is
        // installed, forward the ordinary launch to the WPF-free host. A
        // standalone Stable or ordinary CI build has no signed-by-package
        // marker and keeps the original WPF behavior. --legacy is the manual
        // fallback when a native migration cannot run.
        if (e.Args.Length == 0 && AppVersion.Current.PreRelease is not null)
        {
            string marker = Path.Combine(AppContext.BaseDirectory,
                "native-development-default.flag");
            string native = Path.Combine(AppContext.BaseDirectory,
                "BlackjackCasino.NativeShell.exe");
            if (File.Exists(marker) && File.Exists(native))
            {
                try
                {
                    var start = new ProcessStartInfo(native)
                    {
                        WorkingDirectory = AppContext.BaseDirectory,
                        UseShellExecute = false,
                    };
                    start.ArgumentList.Add("--development");
                    start.ArgumentList.Add("--wait-pid");
                    start.ArgumentList.Add(Environment.ProcessId.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
                    _ = Process.Start(start)
                        ?? throw new InvalidOperationException("Could not start NativeShell.");
                    Shutdown(0);
                    return;
                }
                catch (Exception ex) when (ex is IOException
                    or UnauthorizedAccessException or InvalidOperationException
                    or System.ComponentModel.Win32Exception)
                {
                    MessageBox.Show("NativeShell could not start; running WPF instead.\n"
                        + ex.Message, "Development host fallback",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
            // Recover the existing Manor WAL first and the shared roulette
            // WAL last, exactly as NativeShell does. This must happen before
            // MainWindow loads any wallet, so old/returning receipt IDs cannot
            // silently be settled twice after an interrupted disk write.
            new ManorPersistence(playerRoot).Recover();
            new NativeRouletteJournal(playerRoot).Recover();
        }
        catch (Exception error) when (error is InvalidOperationException
            or UnauthorizedAccessException or InvalidDataException or IOException
            or JsonException)
        {
            MessageBox.Show(error.Message, "Blackjack Casino startup blocked",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            playerWriterLease?.Dispose();
            playerWriterLease = null;
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
