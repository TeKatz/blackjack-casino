using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

public partial class MainWindow : Window
{
    private sealed record TablePalette(
        string Window,
        string Header,
        string Table,
        string Side,
        string Seat,
        string Border,
        string Action,
        string MutedButton,
        string Accent,
        string AccentLight,
        string MutedText,
        string SoftText,
        string Status,
        string AiWin);

    // Global status is application state, not a hidden WPF Blackjack label.
    private sealed class CasinoStatusState
    {
        internal string Text { get; set; } = "Ready.";
    }
    private readonly CasinoStatusState StatusText = new();

    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    private AppPreferences preferences;
    private BlackjackTableController blackjack = null!;
    private CasinoSession session => blackjack.Session;
    private PlayModePreference playMode => blackjack.PlayMode;
    private int aiSeatCount => blackjack.AiSeatCount;
    private decimal selectedBet { get => blackjack.SelectedBet; set => blackjack.SelectedBet = value; }
    private UpdateManifest? pendingManifest;
    private bool updateOperationBusy;
    private bool suppressPreferenceEvents;
    private bool isFullscreen;
    private WindowState previousWindowState;
    private WindowStyle previousWindowStyle;
    private ResizeMode previousResizeMode;
    private Rect previousWindowBounds;

    private readonly PlayerProfileState? initialProfile;
    private readonly bool testSession;
    public MainWindow() : this(null,null) { }
    internal MainWindow(AppPreferences? initialPreferences,PlayerProfileState? profile)
    {
        initialProfile=profile; testSession=initialPreferences is not null;
        InitializeComponent();
        // The lobby is the startup surface; Blackjack has no native WPF table.

        string? manorRecoveryNotice = null;
        try { if(!testSession) new ManorPersistence(ManorPersistence.DefaultDirectory).Recover(); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        { manorRecoveryNotice = "Manor checkpoint could not be recovered: " + ex.Message; }
        preferences = initialPreferences ?? PlayerStateStore.Load();
        preferences = preferences.InitializePresentationAreas();
        MenuAudio.Attach(this, () => CurrentPresentation.SoundEnabled);
        blackjack = new BlackjackTableController(preferences);
        VersionText.Text = AppVersion.Current.ToString();

        suppressPreferenceEvents = true;
        Width = preferences.WindowWidth;
        Height = preferences.WindowHeight;
        WindowState = preferences.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

        UpdateChannel initialChannel = preferences.UpdateChannel
            ?? (AppVersion.Current.PreRelease is null ? UpdateChannel.Stable : UpdateChannel.Development);
        UpdateChannelComboBox.SelectedIndex = initialChannel == UpdateChannel.Development ? 1 : 0;
        preferences = preferences with { UpdateChannel = initialChannel };
        suppressPreferenceEvents = false;

        ApplyTheme(preferences.Theme);

        UpdateChannelComboBox.SelectionChanged += UpdateChannelComboBox_SelectionChanged;

        Loaded += async (_, _) =>
        {
            RefreshGameUi();
            if (manorRecoveryNotice is not null) StatusText.Text = manorRecoveryNotice;
            if(!testSession) await CheckForUpdatesAsync(silent: true);
        };
        Closed += (_, _) =>
        {
            SavePreferences();
            httpClient.Dispose();
        };
    }

    private bool SoundEnabled => blackjack.SoundEnabled;
    private bool AnimationsEnabled => blackjack.AnimationsEnabled;
    private TableThemePreference SelectedTheme => blackjack.Theme;

    private UpdateChannel SelectedUpdateChannel => UpdateChannelComboBox.SelectedIndex == 1
        ? UpdateChannel.Development
        : UpdateChannel.Stable;

    private void UpdateChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressPreferenceEvents || !IsLoaded)
        {
            return;
        }

        pendingManifest = null;
        InstallUpdateButton.IsEnabled = false;
        SetUpdateMessage("Channel changed. Check for updates.");
        SavePreferences();
    }

    private void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            previousWindowState = WindowState;
            previousWindowStyle = WindowStyle;
            previousResizeMode = ResizeMode;
            previousWindowBounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth, ActualHeight)
                : RestoreBounds;

            CasinoWindowFrame.SetIsFullscreen(this, true);
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            isFullscreen = true;
            return;
        }

        WindowState = WindowState.Normal;
        CasinoWindowFrame.SetIsFullscreen(this, false);
        WindowStyle = previousWindowStyle;
        ResizeMode = previousResizeMode;
        if (previousWindowBounds.Width >= MinWidth && previousWindowBounds.Height >= MinHeight)
        {
            Left = previousWindowBounds.Left;
            Top = previousWindowBounds.Top;
            Width = previousWindowBounds.Width;
            Height = previousWindowBounds.Height;
        }
        WindowState = previousWindowState;
        isFullscreen = false;
    }

    private void HandleRoundStateAfterMutation(string fallbackStatus)
    {
        BlackjackRound? round = session.CurrentRound;
        if (round is null)
        {
            StatusText.Text = fallbackStatus;
            RefreshGameUi();
            return;
        }

        StatusText.Text = round.Phase switch
        {
            RoundPhase.InsuranceDecision => "Dealer shows an Ace. Insurance is available for half your original bet.",
            RoundPhase.PlayerTurn when round.PlayerHands.Count > 1 => $"Playing hand {round.ActiveHandIndex + 1} of {round.PlayerHands.Count}.",
            RoundPhase.PlayerTurn => fallbackStatus,
            RoundPhase.Settled => BuildSettlementMessage(round),
            _ => fallbackStatus,
        };

        if (round.Phase == RoundPhase.Settled)
        {
            selectedBet = Math.Min(selectedBet, session.Bankroll);
            CasinoFeedback.PlayOutcome(SoundEnabled, round.Result!.TotalNet);
            SavePreferences();
        }

        RefreshGameUi();
    }

    private string BuildSettlementMessage(BlackjackRound round)
    {
        RoundResult result = round.Result!;
        string headline = result.TotalNet switch
        {
            > 0m => $"You win {Money(result.TotalNet)}",
            < 0m => $"You lose {Money(Math.Abs(result.TotalNet))}",
            _ => "Push",
        };

        if (result.Hands.Count == 1)
        {
            return $"{headline} · {OutcomeText(result.Hands[0].Outcome)}";
        }

        string hands = string.Join(" · ", result.Hands.Select(x => $"H{x.HandIndex + 1}: {OutcomeText(x.Outcome)} {SignedMoney(x.Net)}"));
        return $"{headline} · {hands}";
    }

    // The real Blackjack presentation is React. There is no native card tree to repaint.
    private void RefreshGameUi()
    {
        BankrollText.Text = Money(session.Bankroll);
        if (lobby?.Visibility == Visibility.Visible)
            TableModeText.Text = "Casino lobby";
        modernLobby?.Refresh();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (MetaPageOpen)
        {
            if (e.Key == Key.Escape)
            {
                if (metaPages.Peek().Shell is not ProgressionWindow progression || !progression.TryCloseAlbumArt())
                    CloseMetaPage();
                e.Handled = true;
            }
            return;
        }

        // The hosted view owns its shortcuts and Escape; never route them to Blackjack.
        if (KniffelPageOpen || ModernBlackjackOpen || ModernReelsOpen) return;

        if (EmpirePageOpen)
        {
            if (e.Key == Key.Escape)
            {
                if (empireView?.TryCloseManagement() != true) CloseEmpire();
                e.Handled = true;
            }
            return;
        }

        if (embeddedGameShell is not null || lobby?.Visibility == Visibility.Visible)
        {
            return;
        }

    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(silent: false);
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
        => await InstallUpdateAsync();

    private async Task InstallUpdateAsync()
    {
        if (updateOperationBusy || pendingManifest is null || !UpdaterLauncher.IsAvailable)
            return;

        try
        {
            SetUpdateBusy(true);
            SetUpdateMessage("Downloading update…");

            string updateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlackjackCasino",
                "Updates");

            var progress = new Progress<double>(value =>
                SetUpdateMessage($"Downloading update… {value:P0}"));

            var downloader = new PackageDownloader(httpClient);
            string packagePath = await downloader.DownloadAndVerifyAsync(
                pendingManifest.Package,
                updateDirectory,
                progress);

            SavePreferences();
            SetUpdateMessage("Update verified. Restarting…");
            UpdaterLauncher.Launch(packagePath, pendingManifest.Package.Sha256);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetUpdateMessage($"Update failed: {ex.Message}");
            SetUpdateBusy(false);
        }
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (updateOperationBusy) return;
        UpdateChannel channel = SelectedUpdateChannel;

        try
        {
            SetUpdateBusy(true);
            if (!silent)
            {
                SetUpdateMessage($"Checking {channel}…");
            }

            var source = new HttpManifestUpdateSource(httpClient, GetManifestUri(channel));
            var client = new UpdateClient(source, channel.ToManifestValue());
            UpdateCheckResult result = await client.CheckAsync(AppVersion.Current);

            if (!result.UpdateAvailable)
            {
                pendingManifest = null;
                SetUpdateMessage($"{channel}: up to date (installed {result.CurrentVersion}, available {result.LatestVersion})");
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            SemanticVersion minimumUpdater = SemanticVersion.Parse(result.Manifest.MinimumUpdaterVersion);
            SemanticVersion updaterVersion = UpdaterLauncher.CurrentVersion ?? AppVersion.Current;
            if (minimumUpdater > updaterVersion)
            {
                pendingManifest = null;
                SetUpdateMessage($"Update {result.LatestVersion} requires updater {minimumUpdater} or newer (installed {updaterVersion}).");
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            pendingManifest = result.Manifest;
            SetUpdateMessage($"{channel}: version {result.LatestVersion} is available");
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;

            if (!UpdaterLauncher.IsAvailable)
            {
                AppendUpdateMessage(" (updater helper missing in this development build)");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            pendingManifest = null;
            SetUpdateMessage($"No {channel} release channel has been published yet");
            InstallUpdateButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            pendingManifest = null;
            InstallUpdateButton.IsEnabled = false;
            SetUpdateMessage(silent
                ? $"{channel} update check unavailable"
                : $"Update check failed: {ex.Message}");
        }
        finally
        {
            SetUpdateBusy(false);
        }
    }

    private static Uri GetManifestUri(UpdateChannel channel) => channel switch
    {
        UpdateChannel.Stable => new Uri("https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json"),
        UpdateChannel.Development => new Uri("https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json"),
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    private void SetUpdateMessage(string message)
    {
        UpdateStatusText.Text = message;
        modernLobby?.RefreshUpdates();
    }

    private void AppendUpdateMessage(string text) => SetUpdateMessage(UpdateStatusText.Text + text);

    private void SetUpdateBusy(bool busy)
    {
        updateOperationBusy = busy;
        CheckUpdateButton.IsEnabled = !busy;
        UpdateChannelComboBox.IsEnabled = !busy;
        if (busy)
        {
            InstallUpdateButton.IsEnabled = false;
        }
        else if (pendingManifest is not null)
        {
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;
        }
        modernLobby?.RefreshUpdates();
    }

    private void SavePreferences()
    {
        if(testSession) return;
        Rect bounds;
        bool maximized;

        if (isFullscreen)
        {
            bounds = previousWindowBounds;
            maximized = previousWindowState == WindowState.Maximized;
        }
        else
        {
            bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth, ActualHeight)
                : RestoreBounds;
            maximized = WindowState == WindowState.Maximized;
        }

        double width = bounds.Width >= MinWidth ? bounds.Width : preferences.WindowWidth;
        double height = bounds.Height >= MinHeight ? bounds.Height : preferences.WindowHeight;

        preferences = preferences with
        {
            Empire = empire?.Snapshot ?? preferences.Empire,
            Bankroll = session.Bankroll,
            SelectedBet = Math.Min(selectedBet, session.Bankroll),
            PlayMode = playMode,
            AiSeatCount = aiSeatCount,
            Theme = SelectedTheme,
            SoundEnabled = SoundEnabled,
            AnimationsEnabled = AnimationsEnabled,
            ShowEmpireBalance = preferences.ShowEmpireBalance,
            UpdateChannel = SelectedUpdateChannel,
            WindowWidth = width,
            WindowHeight = height,
            WindowMaximized = maximized,
        };

        PlayerStateStore.Save(preferences);
    }

    private void ApplyTheme(TableThemePreference theme)
    {
        TablePalette palette = theme switch
        {
            TableThemePreference.MidnightBlue => new TablePalette(
                "#060F1B", "#0B1B2D", "#0D304A", "#0A2034", "#13364F", "#426A83",
                "#1A5475", "#13374F", "#91C4DC", "#DCF2FF", "#B8CFE0", "#789CB5", "#08253E", "#82BED6"),
            TableThemePreference.Burgundy => new TablePalette(
                "#17090F", "#270F19", "#461D29", "#26111A", "#381B28", "#84505B",
                "#71333E", "#4C202C", "#C8967F", "#F8D8B8", "#D7B0AD", "#A27783", "#35101D", "#BA7C83"),
            _ => new TablePalette(
                "#07150E", "#091B12", "#0A291A", "#091B12", "#0B2117", "#31533F",
                "#163D29", "#102A1D", "#D4A84F", "#F0CD7A", "#9EB1A4", "#789484", "#071D13", "#6A8F55"),
        };

        SetBrushResource("WindowBackgroundBrush", palette.Window);
        SetBrushResource("HeaderBackgroundBrush", palette.Header);
        SetBrushResource("TableBackgroundBrush", palette.Table);
        SetBrushResource("SideBackgroundBrush", palette.Side);
        SetBrushResource("SeatBackgroundBrush", palette.Seat);
        SetBrushResource("PanelBorderBrush", palette.Border);
        SetBrushResource("ActionBackgroundBrush", palette.Action);
        SetBrushResource("MutedButtonBackgroundBrush", palette.MutedButton);
        SetBrushResource("AccentBrush", palette.Accent);
        SetBrushResource("AccentLightBrush", palette.AccentLight);
        SetBrushResource("MutedTextBrush", palette.MutedText);
        SetBrushResource("SoftTextBrush", palette.SoftText);
        SetBrushResource("StatusBackgroundBrush", palette.Status);
        SetBrushResource("AiWinBrush", palette.AiWin);
    }

    private void SetBrushResource(string key, string hex) => Resources[key] = Brush(hex);

    private Brush UiBrush(string key) => (Brush)FindResource(key);

    private static string OutcomeText(HandOutcome outcome) => outcome switch
    {
        HandOutcome.Win => "Win",
        HandOutcome.Loss => "Loss",
        HandOutcome.Push => "Push",
        HandOutcome.Blackjack => "Blackjack",
        HandOutcome.Surrender => "Surrender",
        _ => outcome.ToString(),
    };

    private static string Money(decimal amount) => $"${amount:N2}";

    private static string SignedMoney(decimal amount) => amount switch
    {
        > 0m => $"+{Money(amount)}",
        < 0m => $"-{Money(Math.Abs(amount))}",
        _ => "$0.00",
    };

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
