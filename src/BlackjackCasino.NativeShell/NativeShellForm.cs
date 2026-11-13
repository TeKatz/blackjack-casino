using System.Drawing;
using System.Diagnostics;
using System.Text.Json;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Hosting;
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
    private bool manorProcessing;
    private string? activeMode;
    private bool listening, ready, browserFailed, closing;
    private bool lobbyShown, lobbyBrowserDisabled;
    private Process? descentProcess;
    private readonly string frontendFolder = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");

    internal NativeShellForm(string? previewDirectory = null)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        previewDirectory ??= HostPreviewStorage.DefaultDirectory(localAppData);
        previewDirectory = HostPreviewStorage.RequireIsolatedDirectory(previewDirectory, localAppData);
        runtime = new NativePreviewRuntime(previewDirectory);
        environment = new Lazy<Task<CoreWebView2Environment>>(() =>
            CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(previewDirectory, "WebView2")));

        Text = "Blackjack Casino — WPF-free Native Shell Preview";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(940, 640);
        ClientSize = new Size(1360, 860);
        BackColor = Color.FromArgb(7, 23, 20);
        ForeColor = Color.WhiteSmoke;
        KeyPreview = true;

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
        navigation.Controls.Add(new Label
        {
            AutoSize = true, Text = "NATIVE HOST • PREVIEW",
            ForeColor = Color.FromArgb(221, 192, 127),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(16, 10, 0, 0),
        });

        root.Controls.Add(navigation, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(status, 0, 2);
        Controls.Add(root);
        watchdog.Tick += (_, _) =>
        {
            watchdog.Stop();
            if (!ready && !closing) FailBrowser("The game browser did not acknowledge startup.");
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.F11) return;
            FormBorderStyle = FormBorderStyle == FormBorderStyle.None
                ? FormBorderStyle.Sizable : FormBorderStyle.None;
            WindowState = FormBorderStyle == FormBorderStyle.None
                ? FormWindowState.Maximized : FormWindowState.Normal;
            e.Handled = true;
        };
        ShowHome();
        Shown += (_, _) =>
        {
            lobbyShown = true;
            if (activeMode is null && !lobbyBrowserDisabled)
                _ = OpenLobbyAsync();
        };
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
            Text = "WPF-freier Windows-Host mit echten React-Oberflächen und C#-Spielregeln.\n"
                + "Diese isolierte Testversion bindet acht Spiele mit C#-Regeln an.\n"
                + "Die übrigen Casino-Bereiche verbleiben vorerst in der regulären Desktop-Anwendung.",
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
        if (NativeDescentLaunch.IsPackaged(AppContext.BaseDirectory))
            actions.Controls.Add(MakeButton("MIDNIGHT DESCENT", () => _ = OpenDescentAsync()));
        page.Controls.Add(actions, 0, 2);
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, ForeColor = Color.FromArgb(150, 172, 166),
            Text = "Sicherer Testmodus: Fortschritt wird ausschließlich unter "
                + "%LOCALAPPDATA%\\BlackjackCasino\\NativeShellPreview gespeichert. "
                + "Der normale Spielerstand und der veröffentlichte Updater bleiben unverändert.",
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font("Segoe UI", 10),
        }, 0, 3);
        content.Controls.Add(page);
        status.Text = "Lobby · Spielgeld: " + runtime.Bankroll.ToString("N2");
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
        ShowHome();
    }

    private async Task OpenDescentAsync()
    {
        if (closing || descentProcess is not null || runtime.HandInProgress) return;

        FormWindowState previousState = WindowState;
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
        if (runtime.ManorRequiresRestart)
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
        if (mode == "reels") runtime.OpenReels();
        if (mode == "neon-vault") runtime.OpenNeonVault();
        if (mode == "nine-lives") runtime.OpenNineLives();
        if (mode == "midnight-merge") runtime.OpenMerge();
        if (mode == "midnight-dice") runtime.OpenDice();
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
        listening = ready = browserFailed = false;
        blackjackEpoch = mode == "blackjack" ? new BlackjackModernSession() : null;
        reelsEpoch = mode == "reels" ? new ModernReelsSession() : null;
        neonEpoch = mode == "neon-vault" ? new ModernNeonVaultSession() : null;
        nineEpoch = mode == "nine-lives" ? new ModernNineLivesSession() : null;
        // Keep Merge's request receipts when only the WebView process restarts.
        mergeEpoch = mode == "midnight-merge" ? mergeEpoch ?? new ModernMergeSession() : null;
        diceEpoch = mode == "midnight-dice" ? diceEpoch ?? new ModernDiceSession() : null;

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
            || args.WebMessageAsJson.Length > (activeMode == "roulette" ? 8192 : 4096)) return;
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
                    break;
                case "lobby.open" when activeMode is null && listening && ready:
                {
                    if (!root.TryGetProperty("id", out JsonElement rawMode)
                        || rawMode.ValueKind != JsonValueKind.String) break;
                    string? id = rawMode.GetString();
                    HostCasinoRoute? mode = HostMigrationCatalog.PreviewRoutes.FirstOrDefault(
                        route => string.Equals(route.DesktopModeId, id, StringComparison.Ordinal));
                    if (mode?.PreviewModeId is { } alias)
                        BeginInvoke(new Action(() => { _ = OpenModeAsync(alias); }));
                    else if (mode?.HasNativeProcess == true &&
                             NativeDescentLaunch.IsPackaged(AppContext.BaseDirectory))
                        BeginInvoke(new Action(() => { _ = OpenDescentAsync(); }));
                    break;
                }
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
                    FormBorderStyle = FormBorderStyle == FormBorderStyle.None
                        ? FormBorderStyle.Sizable : FormBorderStyle.None;
                    WindowState = FormBorderStyle == FormBorderStyle.None
                        ? FormWindowState.Maximized : FormWindowState.Normal;
                    break;
                case "lobby.exit" when activeMode is null && ready:
                    BeginInvoke(new Action(Close));
                    break;
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
                // Reel-stop and animation-complete signals are presentation cues,
                // never outcome calculations; the preview has no sound router yet.
            }
        }
        catch (JsonException) { }
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

    private void Post(string type, object data)
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
        if (activeMode is null)
        {
            // The native WinForms home remains the emergency route when React
            // or WebView2 is unavailable. Do not spin in a reload/failure loop.
            lobbyBrowserDisabled = true;
            DisposeBrowser();
            ShowHome();
            status.Text = message;
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
        DisposeBrowser();
        runtime.Dispose();
        watchdog.Dispose();
        base.OnFormClosing(e);
    }
}
