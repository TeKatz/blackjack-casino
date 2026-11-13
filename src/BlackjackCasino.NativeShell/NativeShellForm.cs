using System.Drawing;
using System.Diagnostics;
using System.Text.Json;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Update;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Genuine Windows Forms shell. The local React game pages remain the only
/// gameplay UI; this host has no WPF control, XAML resource or WPF dispatcher.
/// The native preview stores its player state separately from the installed app.
/// </summary>
internal sealed class NativeShellForm : Form
{
    private const string VirtualHost = "midnight-manor.local";
    private static readonly JsonSerializerOptions MessageJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NativePreviewRuntime runtime;
    private readonly bool managedDevelopment;
    private readonly NativeLobbyController lobbyController;
    private readonly NativeHostAudio hostAudio = new();
    private string? recordsPage;
    private bool shopOpen, updatesOpen;
    private readonly DesktopUpdateService updateService = new();
    private readonly CancellationTokenSource updateCancellation = new();
    private bool checkingUpdates, availableUpdates;
    private UpdateManifest? pendingUpdate;
    private string updateStatus = "This isolated test candidate cannot install updates. You can check the published release status.";
    private readonly Panel content = new() { Dock = DockStyle.Fill };
    private readonly Label status = new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(14, 0, 0, 0),
        ForeColor = Color.FromArgb(225, 210, 174),
        BackColor = Color.FromArgb(14, 32, 29),
    };
    private readonly System.Windows.Forms.Timer watchdog = new() { Interval = 18000 };
    private readonly Lazy<Task<CoreWebView2Environment>> environment;
    private WebView2? browser;
    private BlackjackModernSession? blackjackEpoch;
    private ModernReelsSession? reelsEpoch;
    private ModernNeonVaultSession? neonEpoch;
    private ModernNineLivesSession? nineEpoch;
    private ModernMergeSession? mergeEpoch;
    private ModernDiceSession? diceEpoch;
    private bool diceAiScheduling;
    private Guid? reelsCompletedAudio, neonCompletedAudio, nineCompletedAudio;
    private Guid? reelsStopSpin, neonStopSpin, nineStopSpin;
    private readonly HashSet<int> reelsStoppedAudio = new();
    private readonly HashSet<int> neonStoppedAudio = new();
    private readonly HashSet<int> nineStoppedAudio = new();
    private bool manorProcessing;
    private string? activeMode;
    private bool listening, ready, browserFailed, closing;
    private bool lobbyShown, lobbyBrowserDisabled;
    private Process? descentProcess;
    private EmpireModernSession? empireEpoch;
    private NativeLoungeCaseBoardForm? loungeBoard;
    private NativeEmpireDetachedForm? detachedEmpire;
    private readonly System.Windows.Forms.Timer loungeRefresh = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer loungeTowerRefresh = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer empireRefresh = new() { Interval = 2000 };
    private DateTimeOffset empireLastPersist = DateTimeOffset.UtcNow;
    private readonly string frontendFolder = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");

    internal NativeShellForm(string? previewDirectory = null, bool migrationCandidate = false,
        bool managedDevelopment = false)
    {
        this.managedDevelopment = managedDevelopment;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        previewDirectory ??= HostPreviewStorage.DefaultDirectory(localAppData);
        previewDirectory = HostPreviewStorage.RequireIsolatedDirectory(previewDirectory, localAppData);
        runtime = new NativePreviewRuntime(previewDirectory);
        lobbyController = new NativeLobbyController(runtime);
        environment = new Lazy<Task<CoreWebView2Environment>>(() =>
            CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(previewDirectory, "WebView2")));

        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Text = managedDevelopment
            ? "Blackjack Casino — NativeShell Development"
            : migrationCandidate
                ? "Blackjack Casino — Isolated Host Migration Candidate"
                : "Blackjack Casino — WPF-free Native Shell Preview";
        if (managedDevelopment)
            updateStatus = "Development NativeShell active. Updates can be installed through the bundled updater.";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(940, 640);
        ClientSize = new Size(1360, 860);
        BackColor = Color.FromArgb(7, 23, 20);
        ForeColor = Color.WhiteSmoke;
        KeyPreview = true;

        if (managedDevelopment)
        {
            // The real React casino lobby already owns every game, menu and
            // navigation action. The extra WinForms test-nav and diagnostic
            // footer were only migration scaffolding. Give the React content
            // the entire client area (including when switching game routes).
            Controls.Add(content);
        }
        else
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                Margin = Padding.Empty, Padding = Padding.Empty,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 57));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            var navigation = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight, AutoScroll = true,
                BackColor = Color.FromArgb(11, 31, 29),
                Padding = new Padding(12, 9, 12, 8),
            };
            navigation.Controls.Add(MakeButton("Lobby", ReturnToHome));
            navigation.Controls.Add(MakeButton("Blackjack", () => _ = OpenModeAsync("blackjack")));
            navigation.Controls.Add(MakeButton("Roulette", () => _ = OpenModeAsync("roulette")));
            navigation.Controls.Add(MakeButton("Midnight Reels", () => _ = OpenModeAsync("reels")));
            navigation.Controls.Add(MakeButton("Neon Vault", () => _ = OpenModeAsync("neon-vault")));
            navigation.Controls.Add(MakeButton("Nine Lives", () => _ = OpenModeAsync("nine-lives")));
            navigation.Controls.Add(MakeButton("Midnight Merge", () => _ = OpenModeAsync("midnight-merge")));
            navigation.Controls.Add(MakeButton("Midnight Dice", () => _ = OpenModeAsync("midnight-dice")));
            navigation.Controls.Add(MakeButton("Midnight Manor", () => _ = OpenModeAsync("midnight-manor")));
            navigation.Controls.Add(MakeButton("Casino Empire", () => _ = OpenModeAsync("casino-empire")));
            navigation.Controls.Add(MakeButton("Chill Lounge", () => _ = OpenModeAsync("chill-lounge")));
            navigation.Controls.Add(new Label
            {
                AutoSize = true, Text = managedDevelopment ? "NATIVE HOST • DEVELOPMENT" : "NATIVE HOST • PREVIEW",
                ForeColor = Color.FromArgb(221, 192, 127),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(16, 10, 0, 0),
            });
            root.Controls.Add(navigation, 0, 0);
            root.Controls.Add(content, 0, 1);
            root.Controls.Add(status, 0, 2);
            Controls.Add(root);
        }
        loungeRefresh.Tick += (_, _) =>
        {
            if (closing || activeMode != "chill-lounge" || !ready) return;
            runtime.TickLounge();
            RefreshGameSnapshot();
        };
        loungeTowerRefresh.Tick += (_, _) =>
        {
            if (closing || activeMode != "chill-lounge" || !ready) return;
            if (runtime.TickLoungeTower()) RefreshGameSnapshot();
        };
        empireRefresh.Tick += (_, _) =>
        {
            if (closing || activeMode != "casino-empire" || !ready) return;
            runtime.TickEmpire();
            RefreshGameSnapshot();
            if (DateTimeOffset.UtcNow - empireLastPersist >= TimeSpan.FromSeconds(30))
            {
                runtime.Persist();
                empireLastPersist = DateTimeOffset.UtcNow;
            }
        };
        watchdog.Tick += (_, _) =>
        {
            watchdog.Stop();
            if (!ready && !closing) FailBrowser("The game browser did not acknowledge startup.");
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.F11) return;
            ToggleNativeFullscreen();
            e.Handled = e.SuppressKeyPress = true;
        };
        ShowHome();
        Shown += (_, _) =>
        {
            lobbyShown = true;
            if (activeMode is null && !lobbyBrowserDisabled)
                _ = OpenLobbyAsync();
        };
    }

    private void ToggleNativeFullscreen()
    {
        if (WindowState == FormWindowState.Maximized
            && FormBorderStyle == FormBorderStyle.None)
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.Sizable;
        }
        else
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
    }

    private static bool MatchesSpinMessage(JsonElement root, Guid expected, out int reel)
    {
        reel = -1;
        if (!root.TryGetProperty("spinId", out JsonElement raw)
            || raw.ValueKind != JsonValueKind.String
            || !Guid.TryParse(raw.GetString(), out Guid actual)
            || actual != expected) return false;
        if (root.TryGetProperty("reel", out JsonElement index))
        {
            if (index.ValueKind != JsonValueKind.Number
                || !index.TryGetInt32(out reel)) return false;
        }
        return true;
    }

    private void PlayStopOnce(string? mode, JsonElement root)
    {
        Guid? active = mode switch
        {
            "reels" => runtime.ReelsAudioResult?.SpinId,
            "neon-vault" => runtime.NeonAudioResult?.SpinId,
            "nine-lives" => runtime.NineAudioResult?.SpinId,
            _ => null,
        };
        if (active is not Guid id || !MatchesSpinMessage(root, id, out int index))
            return;
        int reelCount = mode == "neon-vault" ? 5 : 3;
        if (index < 0 || index >= reelCount) return;
        HashSet<int> stopped = mode switch
        {
            "reels" => reelsStoppedAudio,
            "neon-vault" => neonStoppedAudio,
            _ => nineStoppedAudio,
        };
        Guid? last = mode switch
        {
            "reels" => reelsStopSpin,
            "neon-vault" => neonStopSpin,
            _ => nineStopSpin,
        };
        if (last != id)
        {
            stopped.Clear();
            if (mode == "reels") reelsStopSpin = id;
            else if (mode == "neon-vault") neonStopSpin = id;
            else nineStopSpin = id;
        }
        if (stopped.Add(index))
            hostAudio.PlayGameCue(mode, "reel-stop", SoundEnabledForCurrentMode());
    }

    private void PlayResultOnce(string? mode, JsonElement root)
    {
        bool enabled = SoundEnabledForCurrentMode();
        switch (mode)
        {
            case "reels" when runtime.ReelsAudioResult is { } reels
                && MatchesSpinMessage(root, reels.SpinId, out _)
                && reelsCompletedAudio != reels.SpinId:
                reelsCompletedAudio = reels.SpinId;
                hostAudio.PlayReelsOutcome(reels, enabled);
                break;
            case "neon-vault" when runtime.NeonAudioResult is { } neon
                && MatchesSpinMessage(root, neon.SpinId, out _)
                && neonCompletedAudio != neon.SpinId:
                neonCompletedAudio = neon.SpinId;
                hostAudio.PlayNeonOutcome(neon, enabled);
                break;
            case "nine-lives" when runtime.NineAudioResult is { } nine
                && MatchesSpinMessage(root, nine.SpinId, out _)
                && nineCompletedAudio != nine.SpinId:
                nineCompletedAudio = nine.SpinId;
                hostAudio.PlayNineOutcome(nine, enabled);
                break;
        }
    }

    // Minimal internal acceptance interface; never used by React or game commands.
    internal bool PreviewReady => ready && !browserFailed;
    internal string? PreviewMode => activeMode;
    internal CoreWebView2? PreviewBrowser => browser?.CoreWebView2;
    internal Task OpenForAcceptanceAsync(string mode) => OpenModeAsync(mode);
    internal void ReturnForAcceptance() => ReturnToHome();

    private static Button MakeButton(string label, Action click)
    {
        var button = new Button
        {
            Text = label, AutoSize = true, MinimumSize = new Size(128, 35),
            BackColor = Color.FromArgb(25, 61, 51),
            ForeColor = Color.WhiteSmoke,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(3, 0, 8, 0),
            TabStop = true,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(113, 143, 116);
        button.Click += (_, _) => click();
        return button;
    }

    private void ShowHome()
    {
        activeMode = null;
        DisposeBrowser();
        content.Controls.Clear();
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(7, 23, 20),
            Padding = new Padding(35),
            RowCount = 4, ColumnCount = 1,
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, Text = "THE MIDNIGHT CLUB",
            Font = new Font("Georgia", 34, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 204, 151),
            TextAlign = ContentAlignment.MiddleCenter,
        }, 0, 0);
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(190, 218, 201),
            Font = new Font("Segoe UI", 13),
            Text = managedDevelopment
                ? "Die Casino-Lobby konnte nicht geladen werden.\n"
                    + "Du kannst die Spiele über diese Notfallnavigation öffnen.\n"
                    + "Dein NativeShell-Spielstand bleibt erhalten."
                : "WPF-freier Windows-Host mit echten React-Oberflächen und C#-Spielregeln.\n"
                    + "Die reguläre React-Lobby ist vorübergehend nicht verfügbar.",
        }, 0, 1);
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, AutoSize = false,
        };
        actions.Controls.Add(MakeButton("BLACKJACK SPIELEN", () => _ = OpenModeAsync("blackjack")));
        actions.Controls.Add(MakeButton("ROULETTE", () => _ = OpenModeAsync("roulette")));
        actions.Controls.Add(MakeButton("MIDNIGHT REELS", () => _ = OpenModeAsync("reels")));
        actions.Controls.Add(MakeButton("NEON VAULT", () => _ = OpenModeAsync("neon-vault")));
        actions.Controls.Add(MakeButton("NINE LIVES", () => _ = OpenModeAsync("nine-lives")));
        actions.Controls.Add(MakeButton("MIDNIGHT MERGE", () => _ = OpenModeAsync("midnight-merge")));
        actions.Controls.Add(MakeButton("MIDNIGHT DICE", () => _ = OpenModeAsync("midnight-dice")));
        actions.Controls.Add(MakeButton("MIDNIGHT MANOR", () => _ = OpenModeAsync("midnight-manor")));
        actions.Controls.Add(MakeButton("CASINO EMPIRE", () => _ = OpenModeAsync("casino-empire")));
        actions.Controls.Add(MakeButton("EMPIRE · SEPARATES FENSTER", OpenDetachedEmpire));
        actions.Controls.Add(MakeButton("CHILL LOUNGE", () => _ = OpenModeAsync("chill-lounge")));
        if (NativeDescentLaunch.IsPackaged(AppContext.BaseDirectory))
            actions.Controls.Add(MakeButton("MIDNIGHT DESCENT", () => _ = OpenDescentAsync()));
        page.Controls.Add(actions, 0, 2);
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, ForeColor = Color.FromArgb(150, 172, 166),
            Text = managedDevelopment
                ? "Notfallansicht: Spiele und Guthaben werden weiterhin im Development-"
                    + "NativeShell-Profil gespeichert. Du kannst das Fenster schließen "
                    + "und über den normalen Programmstart neu öffnen."
                : "Sicherer Testmodus: Spielstände liegen ausschließlich im isolierten "
                    + "NativeShell-Profil; deine ursprünglichen WPF-Daten bleiben erhalten.",
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font("Segoe UI", 10),
        }, 0, 3);
        content.Controls.Add(page);
        status.Text = "Lobby · Spielgeld: " + runtime.Bankroll.ToString("N2");
        hostAudio.Update(null, runtime.Preferences);
        recordsPage = null;
        shopOpen = false;
        updatesOpen = false;
        if (lobbyShown && !lobbyBrowserDisabled) _ = OpenLobbyAsync();
    }

    private void ReturnToHome()
    {
        if (activeMode == "blackjack" && runtime.HandInProgress)
        {
            status.Text = "Finish the Blackjack hand before returning to the lobby.";
            return;
        }
        if (activeMode == "reels") runtime.CloseReels();
        if (activeMode == "neon-vault") runtime.CloseNeonVault();
        if (activeMode == "nine-lives") runtime.CloseNineLives();
        if (activeMode == "midnight-merge")
        {
            runtime.CloseMerge();
            mergeEpoch = null;
        }
        if (activeMode == "midnight-dice")
        {
            runtime.CloseDice();
            diceEpoch = null;
        }
        if (activeMode == "midnight-manor")
            runtime.CloseManor();
        if (activeMode == "chill-lounge")
        {
            loungeRefresh.Stop();
            loungeTowerRefresh.Stop();
            loungeBoard?.Close();
            loungeBoard = null;
            runtime.CloseLounge();
        }
        if (activeMode == "casino-empire")
        {
            empireRefresh.Stop();
            runtime.CloseEmpire();
            empireEpoch = null;
        }
        ShowHome();
    }

    private void OpenDetachedEmpire()
    {
        if (closing || runtime.ManorRequiresRestart || runtime.RouletteRequiresRestart
            || runtime.LoungeRequiresRestart) return;
        if (detachedEmpire is { IsDisposed: false })
        {
            detachedEmpire.Activate();
            return;
        }
        if (runtime.HandInProgress)
        {
            status.Text = "Finish the current Blackjack hand first.";
            return;
        }
        try
        {
            runtime.OpenEmpire();
            var window = new NativeEmpireDetachedForm(
                runtime, environment.Value, frontendFolder, RefreshLobbySnapshot)
            {
                Owner = this,
            };
            detachedEmpire = window;
            window.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(detachedEmpire, window)) detachedEmpire = null;
                RefreshLobbySnapshot();
            };
            window.Show(this);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException)
        {
            status.Text = "Casino Empire could not open: " + ex.Message;
        }
    }

    private async Task OpenDescentAsync()
    {
        if (closing || descentProcess is not null || runtime.HandInProgress) return;

        FormWindowState previousState = WindowState;
        hostAudio.Suspend();
        Process? process = null;
        try
        {
            bool german = runtime.Preferences.Language == UiLanguagePreference.German;
            ProcessStartInfo start = NativeDescentLaunch.CreateStartInfo(
                AppContext.BaseDirectory, german);
            process = Process.Start(start)
                ?? throw new InvalidOperationException("Godot could not be started.");
            descentProcess = process;
            Enabled = false;
            WindowState = FormWindowState.Minimized;
            status.Text = "Midnight Descent is running.";
            await process.WaitForExitAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            if (!closing && !IsDisposed)
                status.Text = "Midnight Descent: " + ex.Message;
        }
        finally
        {
            if (ReferenceEquals(descentProcess, process)) descentProcess = null;
            process?.Dispose();
            if (!closing && !IsDisposed)
            {
                Enabled = true;
                WindowState = previousState == FormWindowState.Minimized
                    ? FormWindowState.Normal : previousState;
                Activate();
                RefreshLobbySnapshot();
                hostAudio.Update(activeMode, runtime.Preferences);
            }
        }
    }

    private async Task OpenLobbyAsync()
    {
        if (closing || lobbyBrowserDisabled || activeMode is not null
            || !File.Exists(Path.Combine(frontendFolder, "lobby.html"))) return;

        DisposeBrowser();
        var web = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(4, 22, 17),
        };
        // WebView2 processes F11 as a browser accelerator before it reaches
        // the parent Form. Handle the real physical key in the WebView2
        // control; the injected document shortcut remains a fallback for
        // browsers that deliver F11 directly to JavaScript.
        web.AcceleratorKeyPressed += (_, key) =>
        {
            if (key.VirtualKey != (int)Keys.F11) return;
            key.Handled = true;
            if (key.KeyEventKind == CoreWebView2KeyEventKind.KeyDown
                && !key.PhysicalKeyStatus.WasKeyDown
                && IsHandleCreated && !closing)
                BeginInvoke(new Action(ToggleNativeFullscreen));
        };
        browser = web;
        content.Controls.Clear();
        content.Controls.Add(web);
        listening = ready = browserFailed = false;
        status.Text = "Loading native React casino lobby…";
        watchdog.Start();
        try
        {
            CoreWebView2Environment env = await environment.Value;
            if (closing || browser != web || activeMode is not null) return;
            await web.EnsureCoreWebView2Async(env);
            if (closing || browser != web || activeMode is not null) return;
            CoreWebView2 core = web.CoreWebView2
                ?? throw new InvalidOperationException("Lobby WebView2 initialization failed.");
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            // WebView2 owns keyboard focus, so normal WinForms KeyDown does
            // not see F11. Forward the shortcut from each bundled React page.
            await core.AddScriptToExecuteOnDocumentCreatedAsync(
                "window.addEventListener('keydown',function(e){"
                + "if(e.key==='F11'&&!e.repeat){e.preventDefault();"
                + "e.stopImmediatePropagation();"
                + "window.chrome.webview.postMessage({type:'host.fullscreen.toggle'});"
                + "}},true);");
            core.SetVirtualHostNameToFolderMapping(
                VirtualHost, frontendFolder, CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) =>
            {
                if (!IsTrusted(null, args.Uri)) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess) ScheduleFailure("React lobby navigation failed.");
            };
            core.ProcessFailed += (_, _) => ScheduleFailure("React lobby browser failed.");
            core.WebMessageReceived += OnMessage;
            web.Source = new Uri("https://" + VirtualHost + "/lobby.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if (!closing && browser == web)
                FailBrowser("React lobby initialization failed: " + ex.Message);
        }
    }

    private void RefreshLobbySnapshot()
    {
        if (!closing && !browserFailed && activeMode is null
            && listening && browser?.CoreWebView2 is not null)
            Post("lobby.snapshot", runtime.LobbySnapshot());
    }

    private async Task OpenModeAsync(string mode)
    {
        if (closing || !HostMigrationCatalog.IsPreviewRoute(mode)) return;
        if (mode == "casino-empire" && detachedEmpire is { IsDisposed: false })
        {
            detachedEmpire.Activate();
            return;
        }
        if (runtime.ManorRequiresRestart || runtime.LoungeRequiresRestart || runtime.RouletteRequiresRestart)
        {
            status.Text = "Manor journal recovery requires an application restart.";
            return;
        }
        if (mode != "blackjack" && runtime.HandInProgress)
        {
            status.Text = "Finish the Blackjack hand first.";
            return;
        }
        if (activeMode == "reels" && mode != "reels") runtime.CloseReels();
        if (activeMode == "neon-vault" && mode != "neon-vault") runtime.CloseNeonVault();
        if (activeMode == "nine-lives" && mode != "nine-lives") runtime.CloseNineLives();
        if (activeMode == "midnight-merge" && mode != "midnight-merge")
        {
            runtime.CloseMerge();
            mergeEpoch = null;
        }
        if (activeMode == "midnight-dice" && mode != "midnight-dice")
        {
            runtime.CloseDice();
            diceEpoch = null;
        }
        if (activeMode == "midnight-manor" && mode != "midnight-manor")
            runtime.CloseManor();
        if (activeMode == "chill-lounge" && mode != "chill-lounge")
        {
            loungeRefresh.Stop();
            loungeTowerRefresh.Stop();
            loungeBoard?.Close();
            loungeBoard = null;
            runtime.CloseLounge();
        }
        if (activeMode == "casino-empire" && mode != "casino-empire")
        {
            empireRefresh.Stop();
            runtime.CloseEmpire();
            empireEpoch = null;
        }
        if (mode == "reels") runtime.OpenReels();
        if (mode == "neon-vault") runtime.OpenNeonVault();
        if (mode == "nine-lives") runtime.OpenNineLives();
        if (mode == "midnight-merge") runtime.OpenMerge();
        if (mode == "midnight-dice") runtime.OpenDice();
        if (mode == "casino-empire") runtime.OpenEmpire();
        if (mode == "chill-lounge") runtime.OpenLounge();
        if (mode == "midnight-manor")
        {
            try
            {
                runtime.OpenManor();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or InvalidDataException or JsonException or InvalidOperationException)
            {
                ShowRecovery(mode, "Manor checkpoint unavailable: " + ex.Message,
                    assetsAvailable: false);
                return;
            }
        }
        DisposeBrowser();
        activeMode = mode;
        hostAudio.Update(activeMode, runtime.Preferences);
        listening = ready = browserFailed = false;
        blackjackEpoch = mode == "blackjack" ? new BlackjackModernSession() : null;
        reelsEpoch = mode == "reels" ? new ModernReelsSession() : null;
        neonEpoch = mode == "neon-vault" ? new ModernNeonVaultSession() : null;
        nineEpoch = mode == "nine-lives" ? new ModernNineLivesSession() : null;
        // Keep Merge's request receipts when only the WebView process restarts.
        mergeEpoch = mode == "midnight-merge" ? mergeEpoch ?? new ModernMergeSession() : null;
        diceEpoch = mode == "midnight-dice" ? diceEpoch ?? new ModernDiceSession() : null;
        empireEpoch = mode == "casino-empire" ? empireEpoch ?? new EmpireModernSession() : null;

        content.Controls.Clear();
        string document = PageFor(mode);
        if (!File.Exists(Path.Combine(frontendFolder, document)))
        {
            ShowRecovery(mode, "The packaged React game files are missing.", assetsAvailable: false);
            return;
        }

        var web = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = mode == "blackjack"
                ? Color.FromArgb(6, 23, 20) : Color.FromArgb(12, 10, 25),
        };
        // WebView2 processes F11 as a browser accelerator before it reaches
        // the parent Form. Handle the real physical key in the WebView2
        // control; the injected document shortcut remains a fallback for
        // browsers that deliver F11 directly to JavaScript.
        web.AcceleratorKeyPressed += (_, key) =>
        {
            if (key.VirtualKey != (int)Keys.F11) return;
            key.Handled = true;
            if (key.KeyEventKind == CoreWebView2KeyEventKind.KeyDown
                && !key.PhysicalKeyStatus.WasKeyDown
                && IsHandleCreated && !closing)
                BeginInvoke(new Action(ToggleNativeFullscreen));
        };
        browser = web;
        content.Controls.Add(web);
        status.Text = "Loading " + mode + "…";
        watchdog.Start();
        try
        {
            CoreWebView2Environment env = await environment.Value;
            if (closing || browser != web) return;
            await web.EnsureCoreWebView2Async(env);
            if (closing || browser != web) return;
            CoreWebView2 core = web.CoreWebView2
                ?? throw new InvalidOperationException("WebView2 core was not initialized.");
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            // WebView2 owns keyboard focus, so normal WinForms KeyDown does
            // not see F11. Forward the shortcut from each bundled React page.
            await core.AddScriptToExecuteOnDocumentCreatedAsync(
                "window.addEventListener('keydown',function(e){"
                + "if(e.key==='F11'&&!e.repeat){e.preventDefault();"
                + "e.stopImmediatePropagation();"
                + "window.chrome.webview.postMessage({type:'host.fullscreen.toggle'});"
                + "}},true);");
            core.SetVirtualHostNameToFolderMapping(
                VirtualHost, frontendFolder, CoreWebView2HostResourceAccessKind.DenyCors);

            core.NavigationStarting += (_, args) =>
            {
                if (!IsTrusted(activeMode, args.Uri)) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess) ScheduleFailure("Browser navigation failed.");
            };
            core.ProcessFailed += (_, _) => ScheduleFailure("WebView2 process exited unexpectedly.");
            core.WebMessageReceived += OnMessage;
            web.Source = new Uri("https://" + VirtualHost + "/" + document);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if (!closing) FailBrowser("WebView2 initialization failed: " + ex.Message);
        }
    }

    private static string PageFor(string mode) => HostMigrationCatalog.PreviewPageFor(mode);

    private static bool IsTrusted(string? mode, string? source) => mode switch
    {
        null => string.Equals(source, "https://" + VirtualHost + "/lobby.html", StringComparison.Ordinal),
        "blackjack" => BlackjackModernProtocol.Trusted(source),
        "roulette" => string.Equals(source, "https://" + VirtualHost + "/roulette.html", StringComparison.Ordinal),
        "casino-empire" => EmpireModernProtocol.Trusted(source),
        "chill-lounge" => string.Equals(source, "https://" + VirtualHost + "/lounge.html", StringComparison.Ordinal),
        "reels" => ModernReelsProtocol.Trusted(source),
        "neon-vault" => ModernNeonVaultProtocol.Trusted(source),
        "nine-lives" => ModernNineLivesProtocol.Trusted(source),
        "midnight-merge" => ModernMergeProtocol.Trusted(source),
        "midnight-dice" => ModernDiceProtocol.Trusted(source),
        "midnight-manor" => ManorPreviewProtocol.IsTrustedSource(source),
        _ => false,
    };

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (closing || browserFailed || browser is null
            || !IsTrusted(activeMode, args.Source)
            || args.WebMessageAsJson.Length > (activeMode is "roulette" or "casino-empire" ? 8192 :
                activeMode == "chill-lounge" ? 2048 : 4096)) return;
        try
        {
            if (activeMode == "midnight-manor")
            {
                HandleManorMessage(args);
                return;
            }
            using JsonDocument json = JsonDocument.Parse(args.WebMessageAsJson);
            JsonElement root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String) return;
            string? message = type.GetString();
            switch (message)
            {
                case "lobby.ready" when activeMode is null:
                    listening = true;
                    RefreshLobbySnapshot();
                    break;
                case "lobby.ui-ready" when activeMode is null && listening:
                    ready = true;
                    watchdog.Stop();
                    status.Text = "Native React lobby · preview storage · $" + runtime.Bankroll.ToString("N2");
                    break;
                case "lobby.refresh" when activeMode is null && listening:
                    RefreshLobbySnapshot();
                    RefreshLobbyRecords();
                    RefreshLobbyShop();
                    break;
                case "lobby.open" when activeMode is null && listening && ready:
                {
                    if (!root.TryGetProperty("id", out JsonElement rawMode)
                        || rawMode.ValueKind != JsonValueKind.String) break;
                    string? id = rawMode.GetString();
                    // External Godot modes have no React preview document, so they
                    // must be resolved against the full canonical route catalog.
                    HostCasinoRoute? mode = HostMigrationCatalog.CasinoRoutes.FirstOrDefault(
                        route => string.Equals(route.DesktopModeId, id, StringComparison.Ordinal));
                    if (mode?.PreviewModeId is { } alias)
                        BeginInvoke(new Action(() => { _ = OpenModeAsync(alias); }));
                    else if (mode?.HasNativeProcess == true &&
                             NativeDescentLaunch.IsPackaged(AppContext.BaseDirectory))
                        BeginInvoke(new Action(() => { _ = OpenDescentAsync(); }));
                    break;
                }
                case "lobby.open-secondary" when activeMode is null && listening && ready:
                    if (root.TryGetProperty("id", out JsonElement secondaryId)
                        && secondaryId.ValueKind == JsonValueKind.String
                        && secondaryId.GetString() == "casino-empire")
                        BeginInvoke(new Action(OpenDetachedEmpire));
                    break;
                case "lobby.language.set" when activeMode is null && listening && ready:
                {
                    if (!root.TryGetProperty("id", out JsonElement languageId)
                        || languageId.ValueKind != JsonValueKind.String) break;
                    string? choice = languageId.GetString();
                    if (choice is "de" or "en")
                    {
                        runtime.SetLobbyLanguage(choice == "de"
                            ? UiLanguagePreference.German : UiLanguagePreference.English);
                        RefreshLobbySnapshot();
                    }
                    break;
                }
                case "lobby.settings.fullscreen" when activeMode is null && ready:
                case "host.fullscreen.toggle" when ready:
                    ToggleNativeFullscreen();
                    break;
                case "lobby.exit" when activeMode is null && ready:
                    BeginInvoke(new Action(Close));
                    break;
                case "lounge.ready" when activeMode == "chill-lounge":
                    if (!LoungeMessageVersion(root)) break;
                    listening = true;
                    RefreshGameSnapshot();
                    break;
                case "lounge.ui-ready" when activeMode == "chill-lounge" && listening:
                    if (!LoungeMessageVersion(root)) break;
                    ready = true;
                    watchdog.Stop();
                    loungeRefresh.Start();
                    loungeTowerRefresh.Start();
                    status.Text = "Chill Lounge ready: eight native games.";
                    break;
                case "lounge.close" when activeMode == "chill-lounge" && ready:
                    if (LoungeMessageVersion(root))
                        BeginInvoke(new Action(ReturnToHome));
                    break;
                case "lounge.open" when activeMode == "chill-lounge" && listening && ready:
                    if (!LoungeMessageVersion(root)
                        || !root.TryGetProperty("id", out JsonElement loungeId)
                        || loungeId.ValueKind != JsonValueKind.String) break;
                    string? gameId = loungeId.GetString();
                    if (!ModernLoungeCatalog.Contains(gameId)) break;
                    runtime.OpenLoungeGame(gameId!);
                    RefreshGameSnapshot();
                    break;
                case "lounge.adventure.board-window" when activeMode == "chill-lounge"
                    && listening && ready:
                    if (LoungeMessageVersion(root))
                        BeginInvoke(new Action(OpenCaseBoardWindow));
                    break;
                case "lounge.sudoku.command" or "lounge.solitaire.command" or
                    "lounge.word-search.command" or "lounge.memory.command" or
                    "lounge.jigsaw.command" or "lounge.adventure.command" or
                    "lounge.tower-defense.command" or "lounge.minesweeper.command"
                    when activeMode == "chill-lounge" && listening && ready:
                {
                    NativeLoungeReply? reply = runtime.ApplyLounge(
                        message!, args.WebMessageAsJson);
                    if (reply is not null)
                    {
                        Post(reply.Type, reply.Data);
                        if (SuccessfulLoungeAction(reply.Data)
                            && root.TryGetProperty("action", out JsonElement loungeAction)
                            && loungeAction.ValueKind == JsonValueKind.String)
                            hostAudio.PlayGameCue(activeMode,
                                loungeAction.GetString() ?? "", SoundEnabledForCurrentMode());
                    }
                    RefreshGameSnapshot();
                    loungeBoard?.RefreshBoard();
                    break;
                }
                case "empire.ready" when activeMode == "casino-empire":
                    listening = true;
                    RefreshGameSnapshot();
                    break;
                case "empire.ui-ready" when activeMode == "casino-empire" && listening:
                    if (!root.TryGetProperty("visitId", out JsonElement visit)
                        || visit.ValueKind != JsonValueKind.Number
                        || !visit.TryGetInt32(out int visitId) || visitId != 1) break;
                    ready = true;
                    watchdog.Stop();
                    empireRefresh.Start();
                    Post("empire.activated", new { VisitId = 1 });
                    Post("empire.visibility", new { Visible = true });
                    status.Text = "Casino Empire ready.";
                    break;
                case "empire.refresh" when activeMode == "casino-empire" && listening:
                    RefreshGameSnapshot();
                    break;
                case "empire.close" when activeMode == "casino-empire":
                    BeginInvoke(new Action(ReturnToHome));
                    break;
                case "empire.dismiss-offline" when activeMode == "casino-empire":
                    RefreshGameSnapshot();
                    break;
                case "empire.command" when activeMode == "casino-empire" && listening && ready:
                {
                    EmpireModernCommand? command = EmpireModernProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || empireEpoch is null) break;
                    EmpireModernReply reply = empireEpoch.Execute(command,
                        runtime.CommitEmpire, runtime.EmpirePreview);
                    Post("empire.result", reply);
                    status.Text = reply.Message;
                    if (!command.Preview)
                    {
                        if (reply.Success)
                            hostAudio.PlayGameCue(activeMode, command.Action,
                                SoundEnabledForCurrentMode());
                        RefreshGameSnapshot();
                    }
                    break;
                }
                case "roulette.ready" when activeMode == "roulette":
                    listening = true;
                    Post("roulette.snapshot", runtime.RouletteSnapshot());
                    break;
                case "roulette.ui-ready" when activeMode == "roulette" && listening:
                    ready = true;
                    watchdog.Stop();
                    status.Text = runtime.Status;
                    break;
                case "roulette.close" when activeMode == "roulette" && ready:
                    BeginInvoke(new Action(ReturnToHome));
                    break;
                case "roulette.spin" when activeMode == "roulette" && listening && ready:
                {
                    if (!root.TryGetProperty("roundId", out JsonElement rouletteId)
                        || rouletteId.ValueKind != JsonValueKind.String
                        || !Guid.TryParse(rouletteId.GetString(), out Guid roundId)
                        || roundId == Guid.Empty
                        || !root.TryGetProperty("bets", out JsonElement bets)) break;
                    Post("roulette.result", runtime.ApplyRoulette(roundId, bets));
                    Post("roulette.snapshot", runtime.RouletteSnapshot());
                    status.Text = runtime.Status;
                    break;
                }
                case "blackjack.ready" when activeMode == "blackjack":
                case "reels.ready" when activeMode == "reels":
                case "neon-vault.ready" when activeMode == "neon-vault":
                case "nine-lives.ready" when activeMode == "nine-lives":
                case "midnight-merge.ready" when activeMode == "midnight-merge":
                case "midnight-dice.ready" when activeMode == "midnight-dice":
                    listening = true;
                    RefreshGameSnapshot();
                    BeginInvoke(new Action(RefreshGameSnapshot));
                    break;
                case "blackjack.ui-ready" when activeMode == "blackjack":
                case "reels.ui-ready" when activeMode == "reels":
                case "neon-vault.ui-ready" when activeMode == "neon-vault":
                case "nine-lives.ui-ready" when activeMode == "nine-lives":
                case "midnight-merge.ui-ready" when activeMode == "midnight-merge":
                case "midnight-dice.ui-ready" when activeMode == "midnight-dice":
                    if (!listening) break;
                    ready = true;
                    watchdog.Stop();
                    status.Text = runtime.Status;
                    if (activeMode == "midnight-dice") _ = AdvanceDiceAiIfNeededAsync();
                    break;
                case "blackjack.refresh" when activeMode == "blackjack":
                case "reels.refresh" when activeMode == "reels":
                case "neon-vault.refresh" when activeMode == "neon-vault":
                case "nine-lives.refresh" when activeMode == "nine-lives":
                case "midnight-merge.refresh" when activeMode == "midnight-merge":
                case "midnight-dice.refresh" when activeMode == "midnight-dice":
                    RefreshGameSnapshot();
                    break;
                case "blackjack.close" when activeMode == "blackjack":
                case "reels.close" when activeMode == "reels":
                case "neon-vault.close" when activeMode == "neon-vault":
                case "nine-lives.close" when activeMode == "nine-lives":
                case "midnight-merge.close" when activeMode == "midnight-merge":
                case "midnight-dice.close" when activeMode == "midnight-dice":
                    BeginInvoke(new Action(ReturnToHome));
                    break;
                case "blackjack.command" when activeMode == "blackjack" && listening && ready:
                {
                    BlackjackModernCommand? command = BlackjackModernProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || blackjackEpoch is null) break;
                    BlackjackModernReply reply = blackjackEpoch.Execute(command, runtime.ApplyBlackjack);
                    Post("blackjack.result", reply);
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    break;
                }
                case "reels.command" when activeMode == "reels" && listening && ready:
                {
                    ModernReelsCommand? command = ModernReelsProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || reelsEpoch is null) break;
                    ModernReelsReply reply = reelsEpoch.Execute(command, runtime.ApplyReels);
                    Post("reels.result", reply);
                    if (reply.Success && command.Action is not ("sound.set" or "animations.set"))
                        hostAudio.PlayGameCue(activeMode, command.Action, SoundEnabledForCurrentMode());
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    break;
                }
                case "neon-vault.command" when activeMode == "neon-vault" && listening && ready:
                {
                    ModernNeonVaultCommand? command = ModernNeonVaultProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || neonEpoch is null) break;
                    ModernNeonVaultReply reply = neonEpoch.Execute(command, runtime.ApplyNeonVault);
                    Post("neon-vault.result", reply);
                    if (reply.Success && command.Action is not ("sound.set" or "animations.set"))
                        hostAudio.PlayGameCue(activeMode, command.Action, SoundEnabledForCurrentMode());
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    break;
                }
                case "nine-lives.command" when activeMode == "nine-lives" && listening && ready:
                {
                    ModernNineLivesCommand? command = ModernNineLivesProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || nineEpoch is null) break;
                    ModernNineLivesReply reply = nineEpoch.Execute(command, runtime.ApplyNineLives);
                    Post("nine-lives.result", reply);
                    if (reply.Success && command.Action is not ("sound.set" or "animations.set"))
                        hostAudio.PlayGameCue(activeMode, command.Action, SoundEnabledForCurrentMode());
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    break;
                }
                case "midnight-merge.command" when activeMode == "midnight-merge" && listening && ready:
                {
                    ModernMergeCommand? command = ModernMergeProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || mergeEpoch is null) break;
                    ModernMergeReply reply = mergeEpoch.Execute(command,
                        runtime.MergeRevision, runtime.ApplyMerge);
                    Post("midnight-merge.result", reply);
                    if (reply.Success && command.Action is not ("sound.set" or "animations.set"))
                        hostAudio.PlayGameCue(activeMode, command.Action, SoundEnabledForCurrentMode());
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    break;
                }
                case "midnight-dice.command" when activeMode == "midnight-dice" && listening && ready:
                {
                    ModernDiceCommand? command = ModernDiceProtocol.Parse(args.WebMessageAsJson);
                    if (command is null || diceEpoch is null) break;
                    ModernDiceReply reply = diceEpoch.Execute(command, runtime.ApplyDice);
                    Post("midnight-dice.result", reply);
                    if (reply.Success && command.Action is not ("sound.set" or "animations.set" or "music.set"))
                        hostAudio.PlayGameCue(activeMode, command.Action, SoundEnabledForCurrentMode());
                    status.Text = reply.Message;
                    RefreshGameSnapshot();
                    if (runtime.DiceWaiting) _ = AdvanceDiceAiIfNeededAsync();
                    break;
                }
                case "neon-vault.recover" when activeMode == "neon-vault" && listening && ready:
                case "nine-lives.recover" when activeMode == "nine-lives" && listening && ready:
                case "midnight-merge.recover" when activeMode == "midnight-merge" && listening && ready:
                case "midnight-dice.recover" when activeMode == "midnight-dice" && listening && ready:
                    ScheduleFailure("The game requested WebView2 recovery.");
                    break;
                // Browser presentation cues cannot settle a second spin.
                case "reels.reel-stop" when activeMode == "reels" && ready:
                case "neon-vault.reel-stop" when activeMode == "neon-vault" && ready:
                case "nine-lives.reel-stop" when activeMode == "nine-lives" && ready:
                    PlayStopOnce(activeMode, root);
                    break;
                case "reels.animation-complete" when activeMode == "reels" && ready:
                case "neon-vault.animation-complete" when activeMode == "neon-vault" && ready:
                case "nine-lives.animation-complete" when activeMode == "nine-lives" && ready:
                    PlayResultOnce(activeMode, root);
                    break;
                case "neon-vault.feature-impact" when activeMode == "neon-vault" && ready:
                    if (runtime.NeonAudioResult is { } impact
                        && MatchesSpinMessage(root, impact.SpinId, out _))
                        hostAudio.PlayNeonFeature(impact, SoundEnabledForCurrentMode());
                    break;
                case "nine-lives.feature-impact" when activeMode == "nine-lives" && ready:
                    if (runtime.NineAudioResult is { } feature
                        && MatchesSpinMessage(root, feature.SpinId, out _))
                        hostAudio.PlayGameCue(activeMode, "feature-impact", SoundEnabledForCurrentMode());
                    break;
            }
            if (activeMode is null && listening && ready)
                HandleLobbyAction(message ?? string.Empty, root);
            else if (ready && message is "reels.command" or "neon-vault.command"
                or "nine-lives.command" or "midnight-merge.command" or "midnight-dice.command"
                or "lounge.sudoku.command" or "lounge.solitaire.command"
                or "lounge.word-search.command" or "lounge.memory.command"
                or "lounge.jigsaw.command" or "lounge.adventure.command"
                or "lounge.tower-defense.command" or "lounge.minesweeper.command")
                hostAudio.Update(activeMode, runtime.Preferences);
        }
        catch (JsonException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException or OverflowException)
        {
            if (activeMode == "chill-lounge")
                FailBrowser("Native Lounge save or gameplay error: " + ex.Message);
        }
    }

    private static string? ValidLobbyId(JsonElement root)
    {
        if (!root.TryGetProperty("id", out JsonElement value)) return null;
        if (value.ValueKind != JsonValueKind.String) return null;
        string? id = value.GetString();
        return id is { Length: >= 1 and <= 80 }
            && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or ' ' or '\'')
            ? id : null;
    }

    private void RefreshLobbyRecords()
    {
        if (recordsPage is { } page && activeMode is null && listening)
            Post("lobby.records.snapshot", new { Page = page, Snapshot = lobbyController.Snapshot(page) });
    }

    private void RefreshLobbyShop()
    {
        if (shopOpen && activeMode is null && listening)
            Post("lobby.shop.snapshot", lobbyController.ShopSnapshot());
    }

    private void SendLobbyResult(string id, NativeLobbyOutcome result) =>
        Post("lobby.records.result", new { Id = id, result.Success, result.Message });

    private void HandleLobbyAction(string action, JsonElement root)
    {
        string? id = ValidLobbyId(root);
        switch (action)
        {
            case "lobby.records.open"
                when id is "challenges" or "statistics" or "profile" or
                    "history" or "season" or "vault" or "album":
                shopOpen = false;
                recordsPage = id;
                RefreshLobbyRecords();
                break;
            case "lobby.records.close":
                recordsPage = null;
                break;
            case "lobby.progression"
                when id is "Season Pass" or "Cosmetic Shop" or "Collector's Album"
                    or "Challenges" or "Statistics":
            {
                if (id == "Cosmetic Shop")
                {
                    recordsPage = null;
                    shopOpen = true;
                    Post("lobby.shop.show", (object?)null);
                    RefreshLobbyShop();
                }
                else
                {
                    recordsPage = id switch
                    {
                        "Season Pass" => "season",
                        "Collector's Album" => "album",
                        "Challenges" => "challenges",
                        _ => "statistics",
                    };
                    shopOpen = false;
                    Post("lobby.records.show", new { Page = recordsPage });
                    RefreshLobbyRecords();
                }
                break;
            }
            case "lobby.profile" or "lobby.profile.native":
                shopOpen = false;
                recordsPage = "profile";
                Post("lobby.records.show", new { Page = recordsPage });
                RefreshLobbyRecords();
                break;
            case "lobby.vault":
                shopOpen = false;
                recordsPage = "vault";
                Post("lobby.records.show", new { Page = recordsPage });
                RefreshLobbyRecords();
                break;
            case "lobby.shop.open":
                recordsPage = null;
                shopOpen = true;
                RefreshLobbyShop();
                break;
            case "lobby.shop.close":
                shopOpen = false;
                break;
            case "lobby.shop.action" when shopOpen && id is not null:
            {
                NativeLobbyOutcome result = lobbyController.PurchaseOrEquip(id);
                Post("lobby.shop.result", new { Id = id, result.Success, result.Message });
                RefreshLobbyShop();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.shop.pack" when shopOpen && id is not null:
            {
                NativeLobbyOutcome result = lobbyController.PurchasePack(id);
                Post("lobby.shop.result", new { Id = id, result.Success, result.Message });
                RefreshLobbyShop();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.album.open" when recordsPage == "album" && id is not null:
            case "lobby.album.complete" when recordsPage == "album" && id is not null:
            case "lobby.album.claim" when recordsPage == "album" && id is not null:
            {
                string operation = action["lobby.album.".Length..];
                NativeLobbyOutcome result = operation switch
                {
                    "open" => lobbyController.OpenAlbumPack(id),
                    "complete" => lobbyController.CompleteAlbumOpening(id),
                    _ => lobbyController.ClaimAlbumTheme(id),
                };
                Post("lobby.album.result", new
                {
                    Operation = operation,
                    Id = id,
                    result.Success,
                    result.Message,
                    Pending = lobbyController.AlbumPending(),
                });
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.records.claim" when recordsPage == "challenges" && id is not null:
            {
                SendLobbyResult(id, lobbyController.ClaimChallenge(id));
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.records.claim-all" when recordsPage == "challenges":
                SendLobbyResult("all", lobbyController.ClaimAllChallenges());
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            case "lobby.profile.rename" when recordsPage == "profile":
            {
                if (!root.TryGetProperty("value", out JsonElement value)
                    || value.ValueKind != JsonValueKind.String
                    || value.GetString() is not { Length: <= 256 } playerName) break;
                SendLobbyResult("rename", lobbyController.Rename(playerName));
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.profile.equip" when recordsPage == "profile" && id is not null:
                SendLobbyResult(id, lobbyController.EquipProfile(id));
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            case "lobby.records.season.activate" when recordsPage == "season" && id is not null:
                SendLobbyResult(id, lobbyController.ActivateSeason(id));
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            case "lobby.records.vault.open" when recordsPage == "vault":
            {
                NativeLobbyOutcome outcome = lobbyController.OpenVault();
                Post("lobby.records.vault.result", new
                {
                    outcome.Success, outcome.Message, outcome.Reward,
                });
                RefreshLobbyRecords();
                RefreshLobbySnapshot();
                break;
            }
            case "lobby.daily.claim":
                _ = lobbyController.ClaimDaily();
                RefreshLobbySnapshot();
                break;
            case "lobby.arcade.run" when id is not null:
            {
                if (!root.TryGetProperty("score", out JsonElement scoreValue)
                    || !scoreValue.TryGetInt32(out int score)
                    || !root.TryGetProperty("seconds", out JsonElement secondsValue)
                    || !secondsValue.TryGetInt32(out int seconds)
                    || !root.TryGetProperty("runId", out JsonElement runValue)
                    || runValue.ValueKind != JsonValueKind.String
                    || !Guid.TryParse(runValue.GetString(), out Guid runId)) break;
                NativeLobbyOutcome result = lobbyController.RecordArcade(id, score, seconds, runId);
                if (result.Success)
                {
                    RefreshLobbySnapshot();
                    RefreshLobbyRecords();
                }
                break;
            }
            case "lobby.settings.music" when id is "true" or "false":
                runtime.UpdateLoungePreferences(p => p with { MusicEnabled = id == "true" });
                hostAudio.Update(activeMode, runtime.Preferences);
                RefreshLobbySnapshot();
                break;
            case "lobby.settings.empire" when id is "true" or "false":
                runtime.UpdateLoungePreferences(p => p with { ShowEmpireBalance = id == "true" });
                RefreshLobbySnapshot();
                break;
            case "lobby.settings.volume"
                when int.TryParse(id, out int units) && units is >= 0 and <= 32:
                runtime.UpdateLoungePreferences(p => p with { MusicVolume = units / 100d });
                hostAudio.Update(activeMode, runtime.Preferences);
                RefreshLobbySnapshot();
                break;
            case "lobby.updates.open":
                updatesOpen = true;
                RefreshNativeUpdates();
                break;
            case "lobby.updates.close":
                updatesOpen = false;
                break;
            case "lobby.updates.channel.set" when updatesOpen && !checkingUpdates
                && id is "stable" or "development":
                runtime.UpdateLoungePreferences(p => p with
                {
                    UpdateChannel = id == "stable"
                        ? UpdateChannel.Stable : UpdateChannel.Development,
                });
                availableUpdates = false;
                pendingUpdate = null;
                updateStatus = managedDevelopment
                    ? "Development NativeShell updates are supported. Select Development and check again."
                    : "Channel changed. You may check published updates, but installation remains disabled in this test candidate.";
                RefreshNativeUpdates();
                break;
            case "lobby.updates.check" when updatesOpen && !checkingUpdates:
                _ = CheckNativeUpdatesAsync();
                break;
            case "lobby.updates.install" when updatesOpen:
                if (managedDevelopment && pendingUpdate is not null
                    && activeMode is null && !checkingUpdates)
                    _ = InstallManagedDevelopmentUpdateAsync();
                else
                {
                    updateStatus = "Only the managed Development host may install verified updates.";
                    RefreshNativeUpdates();
                }
                break;
        }
    }

    private void RefreshNativeUpdates()
    {
        if (!updatesOpen || activeMode is not null || !listening) return;
        string? version = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductVersion;
        Post("lobby.updates.snapshot", new
        {
            InstalledVersion = string.IsNullOrWhiteSpace(version) ? "Native preview" : version,
            Channel = (runtime.Preferences.UpdateChannel ?? UpdateChannel.Development).ToManifestValue(),
            Status = updateStatus,
            Busy = checkingUpdates,
            CanCheck = !checkingUpdates,
            CanInstall = managedDevelopment && !checkingUpdates
                && availableUpdates && pendingUpdate is not null
                && (runtime.Preferences.UpdateChannel ?? UpdateChannel.Development)
                    == UpdateChannel.Development
                && UpdaterLauncher.IsAvailable,
            UpdateAvailable = availableUpdates,
        });
    }

    private async Task CheckNativeUpdatesAsync()
    {
        if (closing || checkingUpdates) return;
        checkingUpdates = true;
        availableUpdates = false;
        pendingUpdate = null;
        updateStatus = managedDevelopment
            ? "Checking Development update…"
            : "Checking the published release manifest (read-only)…";
        RefreshNativeUpdates();
        try
        {
            UpdateChannel channel = runtime.Preferences.UpdateChannel ?? UpdateChannel.Development;
            string? version = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductVersion;
            if (!SemanticVersion.TryParse(version ?? "", out SemanticVersion current))
                current = SemanticVersion.Parse("0.1.0");
            UpdateCheckResult result = await updateService.CheckAsync(
                channel, current, updateCancellation.Token);
            if (closing || IsDisposed) return;
            availableUpdates = result.UpdateAvailable;
            pendingUpdate = managedDevelopment && channel == UpdateChannel.Development
                && result.UpdateAvailable ? result.Manifest : null;
            updateStatus = result.UpdateAvailable
                ? "A published " + channel.ToManifestValue() + " build is available ("
                    + result.LatestVersion + "). "
                    + (pendingUpdate is not null ? "Ready to install." : "Installation is restricted.")
                : "Published " + channel.ToManifestValue() + " releases checked. "
                    + (managedDevelopment ? "No newer build detected." : "Test candidate installation is disabled.");
        }
        catch (OperationCanceledException) when (closing || updateCancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
            or InvalidDataException or JsonException or FormatException)
        {
            if (closing || IsDisposed) return;
            updateStatus = ("Cannot check published releases: " + ex.Message)
                .Substring(0, Math.Min(2048, ("Cannot check published releases: " + ex.Message).Length));
        }
        finally
        {
            checkingUpdates = false;
            if (!closing && !IsDisposed) RefreshNativeUpdates();
        }
    }

    private async Task InstallManagedDevelopmentUpdateAsync()
    {
        if (!managedDevelopment || checkingUpdates || pendingUpdate is null || closing
            || activeMode is not null || !UpdaterLauncher.IsAvailable)
            return;
        checkingUpdates = true;
        updateStatus = "Revalidating Development update…";
        RefreshNativeUpdates();
        try
        {
            string? productVersion = FileVersionInfo.GetVersionInfo(
                Application.ExecutablePath).ProductVersion;
            if (!SemanticVersion.TryParse(productVersion ?? "", out SemanticVersion installed))
                throw new InvalidDataException("NativeShell version could not be verified.");
            UpdateCheckResult latest = await updateService.CheckAsync(
                UpdateChannel.Development, installed, updateCancellation.Token);
            if (!latest.UpdateAvailable)
            {
                updateStatus = "Development build is already up to date.";
                pendingUpdate = null;
                availableUpdates = false;
                return;
            }
            SemanticVersion updaterVersion = UpdaterLauncher.CurrentVersion ?? installed;
            if (DesktopUpdateService.MinimumUpdaterVersion(latest.Manifest) > updaterVersion)
                throw new InvalidOperationException("The installed updater is too old for this release.");
            pendingUpdate = latest.Manifest;
            updateStatus = "Downloading and verifying Development update…";
            RefreshNativeUpdates();
            string path = await updateService.DownloadAsync(
                latest.Manifest.Package, cancellationToken: updateCancellation.Token);
            // Mutable Development manifests can change during the download.
            UpdateCheckResult check = await updateService.CheckAsync(
                UpdateChannel.Development, installed, updateCancellation.Token);
            if (!check.UpdateAvailable
                || !Equals(check.Manifest.Package, latest.Manifest.Package))
                throw new InvalidDataException(
                    "The Development release changed while downloading. Check for updates again.");

            updateStatus = "Update verified. Closing NativeShell for installation…";
            RefreshNativeUpdates();
            UpdaterLauncher.Launch(path, latest.Manifest.Package.Sha256);
            // Updater waits for this PID before replacing the installation;
            // the existing WPF launcher redirects back to managed NativeShell.
            Close();
        }
        catch (OperationCanceledException) when (closing || updateCancellation.IsCancellationRequested)
        {
            // The main form is already shutting down.
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or HttpRequestException or InvalidDataException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            updateStatus = "NativeShell update failed: " + error.Message;
        }
        finally
        {
            checkingUpdates = false;
            if (!closing && !IsDisposed) RefreshNativeUpdates();
        }
    }

    private bool SoundEnabledForCurrentMode()
    {
        string area = activeMode switch
        {
            null => "lobby",
            "reels" => "midnight-reels",
            "midnight-dice" => "kniffel",
            _ => activeMode,
        };
        return runtime.Preferences.SoundEnabled
            && runtime.Preferences.Presentation(area).SoundEnabled;
    }

    private static bool SuccessfulLoungeAction(object receipt) => receipt switch
    {
        ModernSudokuReply r => r.Success,
        ModernSolitaireReply r => r.Success,
        ModernWordSearchReply r => r.Success,
        ModernMemoryReply r => r.Success,
        ModernJigsawReply r => r.Success,
        ModernAdventureReply r => r.Success,
        ModernTowerDefenseReply r => r.Success,
        ModernMinesweeperReply r => r.Success,
        _ => false,
    };

    private static bool LoungeMessageVersion(JsonElement message) =>
        message.TryGetProperty("version", out JsonElement version)
        && version.ValueKind == JsonValueKind.Number
        && version.TryGetInt32(out int number) && number == 1;

    private void OpenCaseBoardWindow()
    {
        if (closing || activeMode != "chill-lounge" || !runtime.LoungeCanOpenCaseBoard)
            return;
        if (loungeBoard is { IsDisposed: false })
        {
            loungeBoard.Activate();
            return;
        }
        var window = new NativeLoungeCaseBoardForm(
            runtime, environment.Value, frontendFolder, RefreshGameSnapshot)
        {
            Owner = this,
        };
        loungeBoard = window;
        runtime.SetLoungeBoardWindow(true);
        window.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(loungeBoard, window)) loungeBoard = null;
            runtime.SetLoungeBoardWindow(false);
            RefreshGameSnapshot();
        };
        window.Show(this);
    }

    private void RefreshGameSnapshot()
    {
        if (closing || browserFailed || browser?.CoreWebView2 is null || !listening)
            return;
        if (activeMode == "blackjack" && blackjackEpoch is not null)
            Post("blackjack.snapshot", new
            {
                Version = BlackjackModernProtocol.Version,
                Session = blackjackEpoch.Id,
                blackjackEpoch.Revision,
                State = runtime.BlackjackSnapshot(),
            });
        else if (activeMode == "reels" && reelsEpoch is not null)
            Post("reels.snapshot", new
            {
                Version = ModernReelsProtocol.Version,
                Session = reelsEpoch.Id,
                reelsEpoch.Revision,
                State = runtime.ReelsSnapshot(),
            });
        else if (activeMode == "neon-vault" && neonEpoch is not null)
            Post("neon-vault.snapshot", new
            {
                Version = ModernNeonVaultProtocol.Version,
                Session = neonEpoch.Id,
                neonEpoch.Revision,
                State = runtime.NeonVaultSnapshot(),
            });
        else if (activeMode == "nine-lives" && nineEpoch is not null)
            Post("nine-lives.snapshot", new
            {
                Version = ModernNineLivesProtocol.Version,
                Session = nineEpoch.Id,
                nineEpoch.Revision,
                State = runtime.NineLivesSnapshot(),
            });
        else if (activeMode == "midnight-merge" && mergeEpoch is not null)
            Post("midnight-merge.snapshot", new
            {
                Version = ModernMergeProtocol.Version,
                Session = mergeEpoch.Id,
                Revision = runtime.MergeRevision,
                State = runtime.MergeSnapshot(),
            });
        else if (activeMode == "chill-lounge")
            Post("lounge.snapshot", runtime.LoungeSnapshot());
        else if (activeMode == "casino-empire" && empireEpoch is not null)
            Post("empire.snapshot", new
            {
                Version = EmpireModernProtocol.Version,
                Session = empireEpoch.Id,
                empireEpoch.Revision,
                VisitId = 1,
                Notice = (string?)null,
                Offline = (object?)null,
                LastReply = (EmpireModernReply?)null,
                State = runtime.EmpireSnapshot(),
            });
        else if (activeMode == "midnight-dice" && diceEpoch is not null)
            Post("midnight-dice.snapshot", new
            {
                Version = ModernDiceProtocol.Version,
                Session = diceEpoch.Id,
                diceEpoch.Revision,
                State = runtime.DiceSnapshot(),
            });
    }

    private void HandleManorMessage(CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (manorProcessing || browser?.CoreWebView2 is not { } core
            || !ManorPreviewProtocol.TryParseCommand(args.Source,
                args.WebMessageAsJson, out ManorPreviewProtocol.Command? command)
            || command is null) return;

        if (command.Type == "ui.ready")
        {
            listening = ready = true;
            watchdog.Stop();
            status.Text = runtime.Status;
            return;
        }
        if (command.Type == "ui.close")
        {
            BeginInvoke(new Action(ReturnToHome));
            return;
        }
        if (command.Type is "client.ready" or "snapshot.request")
        {
            listening = true;
            core.PostWebMessageAsJson(
                ManorPreviewProtocol.SerializeSnapshot(runtime.ManorSnapshot(), command.RequestId));
            return;
        }

        manorProcessing = true;
        try
        {
            ManorPreviewProtocol.Snapshot snapshot = runtime.ApplyManor(command);
            core.PostWebMessageAsJson(
                ManorPreviewProtocol.SerializeSnapshot(snapshot, command.RequestId));
            status.Text = runtime.Status;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or InvalidDataException or InvalidOperationException
            or ArgumentException or OverflowException)
        {
            core.PostWebMessageAsJson(ManorPreviewProtocol.SerializeError(
                command.RequestId, ex.Message, runtime.ManorSnapshot(),
                runtime.ManorRequiresRestart));
            if (runtime.ManorRequiresRestart)
                status.Text = "Manor journal recovery required. Restart this preview.";
        }
        finally
        {
            manorProcessing = false;
        }
    }

    private async Task AdvanceDiceAiIfNeededAsync()
    {
        if (closing || diceAiScheduling || activeMode != "midnight-dice"
            || !runtime.DiceWaiting) return;
        diceAiScheduling = true;
        try
        {
            bool changed = await runtime.AdvanceDiceOpponentsAsync();
            if (activeMode == "midnight-dice" && !closing)
            {
                if (changed) diceEpoch?.Bump();
                RefreshGameSnapshot();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or OverflowException)
        {
            if (!closing) status.Text = "AI opponent recovery: " + ex.Message;
        }
        finally
        {
            diceAiScheduling = false;
        }
    }

    private void Post(string type, object? data)
    {
        if (!closing && !browserFailed && browser?.CoreWebView2 is { } core)
            core.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, MessageJson));
    }

    private void ScheduleFailure(string message)
    {
        if (closing || browserFailed || IsDisposed || !IsHandleCreated) return;
        BeginInvoke(new Action(() => FailBrowser(message)));
    }

    private void FailBrowser(string message)
    {
        if (closing || browserFailed) return;
        empireRefresh.Stop();
        loungeRefresh.Stop();
        loungeTowerRefresh.Stop();
        if (activeMode is null)
        {
            // The native WinForms home remains the emergency route when React
            // or WebView2 is unavailable. Do not spin in a reload/failure loop.
            lobbyBrowserDisabled = true;
            DisposeBrowser();
            ShowHome();
            status.Text = message;
            if (managedDevelopment)
                System.Windows.Forms.MessageBox.Show(this, message,
                    "NativeShell browser recovery", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            return;
        }
        browserFailed = true;
        watchdog.Stop();
        string mode = activeMode;
        DisposeBrowser();
        ShowRecovery(mode, message,
            File.Exists(Path.Combine(frontendFolder, PageFor(mode))));
    }

    private void ShowRecovery(string mode, string problem, bool assetsAvailable)
    {
        activeMode = mode;
        DisposeBrowser();
        content.Controls.Clear();

        var choices = new BrowserRecoverySession(assetsAvailable, legacyGamesAvailable: false);
        var recovery = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
            BackColor = Color.FromArgb(7, 23, 20),
            Padding = new Padding(35),
        };
        recovery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        recovery.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        recovery.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        recovery.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        recovery.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 15),
            Text = "Browserwiederherstellung — " + mode + "\n" + problem
                + "\nKeine Einsätze werden automatisch wiederholt.",
        }, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        var retry = MakeButton("Erneut versuchen", () =>
        {
            if (choices.TryChoose(BrowserRecoveryAction.RetryModern))
                _ = OpenModeAsync(mode);
        });
        retry.Enabled = assetsAvailable;
        actions.Controls.Add(retry);
        actions.Controls.Add(MakeButton("Zur Lobby", () =>
        {
            if (choices.TryChoose(BrowserRecoveryAction.ReturnToLobby))
                ReturnToHome();
        }));
        recovery.Controls.Add(actions, 0, 1);
        content.Controls.Add(recovery);
        status.Text = problem;
    }

    private void DisposeBrowser()
    {
        watchdog.Stop();
        listening = ready = false;
        WebView2? old = browser;
        browser = null;
        if (old is null) return;
        content.Controls.Remove(old);
        old.Dispose();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (runtime.HandInProgress && e.CloseReason == CloseReason.UserClosing)
        {
            DialogResult answer = System.Windows.Forms.MessageBox.Show(
                this, "A Blackjack hand is still in progress. Exit the preview anyway?",
                "Current hand", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) { e.Cancel = true; return; }
        }
        closing = true;
        if (descentProcess is { HasExited: false } process)
        {
            try { process.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }
        empireRefresh.Stop();
        loungeRefresh.Stop();
        loungeTowerRefresh.Stop();
        loungeBoard?.Close();
        loungeBoard = null;
        detachedEmpire?.Close();
        detachedEmpire = null;
        DisposeBrowser();
        updateCancellation.Cancel();
        updateService.Dispose();
        updateCancellation.Dispose();
        runtime.Dispose();
        hostAudio.Dispose();
        CasinoAudio.Shutdown();
        empireRefresh.Dispose();
        loungeRefresh.Dispose();
        loungeTowerRefresh.Dispose();
        watchdog.Dispose();
        base.OnFormClosing(e);
    }
}
