using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Process? descentProcess;

    private void OpenMidnightDescent()
    {
        if (descentProcess is { HasExited: false }) return;
        bool de = Localization.Language == UiLanguagePreference.German;
        string executable = Path.Combine(AppContext.BaseDirectory, "MidnightDescent", "MidnightDescent.exe");
        if (!File.Exists(executable) || !File.Exists(Path.ChangeExtension(executable, ".pck")))
        {
            MessageBox.Show(this, de
                ? "Midnight Descent ist in diesem Build nicht enthalten. Installiere das vollständige Windows-Release."
                : "Midnight Descent is not included in this build. Install the complete Windows release.",
                "Midnight Descent", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        start.ArgumentList.Add("--");
        start.ArgumentList.Add(de ? "--language=de" : "--language=en");
        var previousState = WindowState;
        try
        {
            var process = Process.Start(start) ?? throw new InvalidOperationException("Godot did not start.");
            descentProcess = process;
            WindowState = WindowState.Minimized;
            IsEnabled = false;
            // Wait asynchronously on the UI dispatcher; no engine calls cross WebView2.
            _ = RestoreAfterDescentAsync(process, previousState);
        }
        catch (Exception ex)
        {
            IsEnabled = true;
            WindowState = previousState;
            MessageBox.Show(this, ex.Message, "Midnight Descent", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task RestoreAfterDescentAsync(Process process, WindowState previousState)
    {
        try { await process.WaitForExitAsync(); }
        finally
        {
            descentProcess = null;
            process.Dispose();
            if (IsLoaded)
            {
                IsEnabled = true;
                WindowState = previousState == WindowState.Minimized ? WindowState.Normal : previousState;
                Activate();
                modernLobby?.Refresh();
            }
        }
    }
}
