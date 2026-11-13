using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.HostCutoverLauncher;

/// <summary>
/// A rehearsal launcher only: no installation, save migration, updater
/// invocation, default-host change or production state promotion is possible.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string install = AppContext.BaseDirectory;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (args is ["--legacy"])
            return Start(install, appData, null) ? 0 : 1;
        if (args is ["--native", var requestedCandidate])
            return Start(install, appData, requestedCandidate) ? 0 : 1;
        if (args.Length != 0)
        {
            MessageBox.Show("Usage: BlackjackCasino.HostCutoverLauncher.exe [--legacy|--native CANDIDATE]",
                "Unknown launch option", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 2;
        }

        using var window = new Form
        {
            Text = "Blackjack Casino — Host Migration Test",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(620, 305),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(14, 30, 31),
            ForeColor = Color.FromArgb(244, 232, 202),
            Font = new Font("Segoe UI", 10),
        };

        var header = new Label
        {
            Text = "Host Migration • Isolated Test",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            Bounds = new Rectangle(22, 19, 570, 37),
        };
        var note = new Label
        {
            Text = "Default: launch the installed WPF host. The NativeShell option uses ONLY an existing copied test profile; it does not change your installed game, save files or update channel.",
            Bounds = new Rectangle(22, 71, 568, 68),
        };
        var caption = new Label
        {
            Text = "Verified NativeShell test profile",
            Bounds = new Rectangle(22, 151, 400, 24),
        };
        var select = new ComboBox
        {
            Bounds = new Rectangle(22, 178, 565, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        try
        {
            foreach (string candidate in HostCutoverLaunchPolicy.AvailableCandidates(install, appData))
                select.Items.Add(candidate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "Candidate discovery", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        if (select.Items.Count > 0) select.SelectedIndex = 0;

        var legacy = new Button
        {
            Text = "Start existing WPF game",
            Bounds = new Rectangle(22, 229, 234, 42),
            BackColor = Color.FromArgb(44, 97, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        var native = new Button
        {
            Text = "Test NativeShell (isolated)",
            Bounds = new Rectangle(271, 229, 315, 42),
            BackColor = Color.FromArgb(140, 107, 64),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = select.SelectedItem is string,
        };
        legacy.Click += (_, _) =>
        {
            if (Start(install, appData, null)) window.Close();
        };
        native.Click += (_, _) =>
        {
            if (select.SelectedItem is string candidate
                && Start(install, appData, candidate)) window.Close();
        };
        select.SelectedIndexChanged += (_, _) => native.Enabled = select.SelectedItem is string;
        window.Controls.AddRange([header, note, caption, select, legacy, native]);
        window.AcceptButton = legacy;
        Application.Run(window);
        return 0;
    }

    private static bool Start(string install, string appData, string? candidate)
    {
        try
        {
            HostCutoverLaunchPlan plan = HostCutoverLaunchPolicy.Resolve(install, appData, candidate);
            var info = new ProcessStartInfo(plan.Executable)
            {
                WorkingDirectory = install,
                UseShellExecute = false,
            };
            foreach (string argument in plan.Arguments)
                info.ArgumentList.Add(argument);
            using Process process = Process.Start(info)
                ?? throw new InvalidOperationException("Windows could not start the selected host.");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(ex.Message, "Host launch blocked",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
