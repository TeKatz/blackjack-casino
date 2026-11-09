using System.IO;
using System.Text.Json;
using BlackjackCasino.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Presentation-only WebView2 lobby. All navigation and player state stay in the WPF host.
/// A fully functional WPF lobby remains underneath as the startup/error fallback.
/// </summary>
internal sealed class ModernLobbyView : UserControl, IDisposable
{
    internal static string EntryPath => Path.Combine(AppContext.BaseDirectory, "ModernFrontend", "lobby.html");
    internal static bool IsAvailable => File.Exists(EntryPath);
    private const string EntryPoint = "https://midnight-manor.local/lobby.html";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly WebView2 browser = new()
    {
        Visibility = Visibility.Hidden,
        // The first native WebView2 surface must match the WPF/HTML splash.
        // Otherwise WebView2 can briefly paint its default white background.
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 4, 22, 17),
    };
    private readonly Func<object> snapshot;
    private readonly Func<object> shopSnapshot;
    private readonly Func<string, (bool Success, string Message)> cosmeticAction;
    private readonly Func<string, (bool Success, string Message)> packAction;
    private readonly Action shopClosed;
    private readonly Func<object> challengesSnapshot;
    private readonly Func<object> statisticsSnapshot;
    private readonly Func<object> accountSnapshot;
    private readonly Func<object> historySnapshot;
    private readonly Func<object> seasonsSnapshot;
    private readonly Func<object> vaultSnapshot;
    private readonly Func<object> albumSnapshot;
    private readonly Func<object?> albumPendingSnapshot;
    private readonly Func<string, (bool Success, string Message)> albumOpen;
    private readonly Func<string, (bool Success, string Message)> albumComplete;
    private readonly Func<string, (bool Success, string Message)> albumClaim;
    private readonly Func<(bool Success, string Message, object? Reward)> vaultOpen;
    private readonly Func<string, (bool Success, string Message)> renameProfile;
    private readonly Func<string, (bool Success, string Message)> equipProfile;
    private readonly Func<string, (bool Success, string Message)> activateSeason;
    private readonly Func<string, (bool Success, string Message)> claimChallenge;
    private readonly Func<(bool Success, string Message)> claimAllChallenges;
    private readonly Func<string, int, int, Guid, bool> arcadeRun;
    private readonly Action recordsClosed;
    private readonly CasinoModeRegistry modes;
    private readonly Action<string> progression;
    private readonly Action profile;
    private readonly Action vault;
    private readonly Action dailyClaim;
    private readonly Action<string> languageChange;
    private readonly Action exit;
    private readonly Action readyChanged;
    private readonly Func<object> updateSnapshot;
    private readonly Action<string> setUpdateChannel;
    private readonly Func<Task> checkUpdate;
    private readonly Func<Task> installUpdate;
    private readonly Action<string, string?> presentationSetting;
    private bool updatesOpen;
    private bool initialized;
    private bool listening;
    private bool shopOpen;
    private string? recordsPage;
    private bool ready;
    private bool failed;
    private bool disposed;

    internal bool Ready => ready && !failed;
    internal bool ShopOpen => shopOpen;
    internal string? RecordsPage => recordsPage;
    internal bool Failed => failed;
    internal string? FailureReason { get; private set; }
    internal string? LastNavigation { get; private set; }
    internal string? LastMessageSource { get; private set; }
    internal string? LastMessageType { get; private set; }
    internal WebView2 Browser => browser;

    internal ModernLobbyView(CasinoModeRegistry modes, Func<object> snapshot,
        Func<object> shopSnapshot,
        Func<string, (bool Success, string Message)> cosmeticAction,
        Func<string, (bool Success, string Message)> packAction, Action shopClosed,
        Func<object> challengesSnapshot, Func<object> statisticsSnapshot,
        Func<object> accountSnapshot, Func<object> historySnapshot, Func<object> seasonsSnapshot,
        Func<object> vaultSnapshot, Func<object> albumSnapshot,
        Func<object?> albumPendingSnapshot,
        Func<string, (bool Success, string Message)> albumOpen,
        Func<string, (bool Success, string Message)> albumComplete,
        Func<string, (bool Success, string Message)> albumClaim,
        Func<string, (bool Success, string Message)> renameProfile,
        Func<string, (bool Success, string Message)> equipProfile,
        Func<string, (bool Success, string Message)> activateSeason,
        Func<(bool Success, string Message, object? Reward)> vaultOpen,
        Func<string, (bool Success, string Message)> claimChallenge,
        Func<(bool Success, string Message)> claimAllChallenges, Action recordsClosed,
        Action<string> progression, Func<string, int, int, Guid, bool> arcadeRun,
        Action profile, Action vault, Action dailyClaim,
        Action<string> languageChange, Action exit, Action readyChanged,
        Func<object> updateSnapshot, Action<string> setUpdateChannel,
        Func<Task> checkUpdate, Func<Task> installUpdate,
        Action<string, string?> presentationSetting)
    {
        this.modes = modes;
        this.snapshot = snapshot;
        this.shopSnapshot = shopSnapshot;
        this.cosmeticAction = cosmeticAction;
        this.packAction = packAction;
        this.shopClosed = shopClosed;
        this.challengesSnapshot = challengesSnapshot;
        this.statisticsSnapshot = statisticsSnapshot;
        this.accountSnapshot = accountSnapshot;
        this.historySnapshot = historySnapshot;
        this.seasonsSnapshot = seasonsSnapshot;
        this.vaultSnapshot = vaultSnapshot;
        this.albumSnapshot = albumSnapshot;
        this.albumPendingSnapshot = albumPendingSnapshot;
        this.albumOpen = albumOpen;
        this.albumComplete = albumComplete;
        this.albumClaim = albumClaim;
        this.vaultOpen = vaultOpen;
        this.renameProfile = renameProfile;
        this.equipProfile = equipProfile;
        this.activateSeason = activateSeason;
        this.claimChallenge = claimChallenge;
        this.claimAllChallenges = claimAllChallenges;
        this.recordsClosed = recordsClosed;
        this.progression = progression;
        this.arcadeRun = arcadeRun;
        this.profile = profile;
        this.vault = vault;
        this.dailyClaim = dailyClaim;
        this.languageChange = languageChange;
        this.exit = exit;
        this.readyChanged = readyChanged;
        this.updateSnapshot = updateSnapshot;
        this.setUpdateChannel = setUpdateChannel;
        this.checkUpdate = checkUpdate;
        this.installUpdate = installUpdate;
        this.presentationSetting = presentationSetting;
        Name = "ModernLobbyPresentation";
        Background = Brushes.Transparent;
        IsHitTestVisible = false;
        Content = new Grid { Background = Brushes.Transparent, Children = { browser } };
        Loaded += OnLoaded;
    }

    internal void SetLobbyVisible(bool visible)
    {
        if (disposed || failed) return;
        Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (visible)
        {
            Refresh();
            if (shopOpen) RefreshShop();
            if (recordsPage is not null) RefreshRecords();
            RefreshUpdates();
        }
    }

    private void PostMessage(string type, object? data)
    {
        if (!listening || disposed || failed || browser.CoreWebView2 is null) return;
        browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void Refresh()
    {
        if (!listening || disposed || failed || browser.CoreWebView2 is null) return;
        try
        {
            PostMessage("lobby.snapshot", snapshot());
        }
        catch (InvalidOperationException ex) { Fail("Snapshot delivery failed: " + ex.Message); }
    }

    internal void RefreshUpdates()
    {
        if (!updatesOpen || !Ready || !IsVisible) return;
        PostMessage("lobby.updates.snapshot", updateSnapshot());
    }

    private void RefreshRecords()
    {
        if (recordsPage is null || !listening || disposed || failed || browser.CoreWebView2 is null) return;
        PostMessage("lobby.records.snapshot", new
        {
            Page = recordsPage,
            Snapshot = recordsPage switch
            {
                "challenges" => challengesSnapshot(),
                "statistics" => statisticsSnapshot(),
                "profile" => accountSnapshot(),
                "history" => historySnapshot(),
                "season" => seasonsSnapshot(),
                "vault" => vaultSnapshot(),
                "album" => albumSnapshot(),
                _ => throw new InvalidOperationException("Unsupported progression route."),
            },
        });
    }

    internal void ShowProgression(string page)
    {
        if (!Ready || disposed || failed || page is not ("challenges" or "statistics" or "profile" or "history" or "season" or "vault" or "album")) return;
        shopOpen = false;
        recordsPage = page;
        PostMessage("lobby.records.show", new { Page = page });
        RefreshRecords();
    }

    private void RefreshShop()
    {
        if (!shopOpen || !listening || disposed || failed || browser.CoreWebView2 is null) return;
        PostMessage("lobby.shop.snapshot", shopSnapshot());
    }

    internal void ShowShop()
    {
        if (!Ready || !listening || disposed || failed) return;
        recordsPage = null;
        shopOpen = true;
        PostMessage("lobby.shop.show", null);
        RefreshShop();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || disposed || failed) return;
        initialized = true;
        if (!IsAvailable) { Fail("The packaged ModernFrontend/lobby.html is missing."); return; }

        try
        {
            CoreWebView2? core = await ModernWebViewBootstrap.InitializeAsync(
                browser, () => disposed, Path.GetDirectoryName(EntryPath)!,
                Trusted);
            if (core is null) return;
            core.NavigationCompleted += (_, args) =>
            {
                LastNavigation = args.IsSuccess ? "success" : "failure: " + args.WebErrorStatus;
                if (!args.IsSuccess) Fail("Lobby navigation failed: " + args.WebErrorStatus);
            };
            core.ProcessFailed += (_, args) => Fail("Lobby browser process failed: " + args.ProcessFailedKind);
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri(EntryPoint);
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Fail("Lobby WebView initialization failed: " + ex.Message);
        }
    }

    private static bool Trusted(string? source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals(ManorPreviewProtocol.VirtualHost, StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && uri.AbsolutePath.Equals("/lobby.html", StringComparison.Ordinal)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        LastMessageSource = args.Source;
        LastMessageType = args.WebMessageAsJson.Length <= 1024 ? args.WebMessageAsJson : "oversize";
        if (disposed || failed || !Trusted(args.Source) || args.WebMessageAsJson.Length > 1024) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(args.WebMessageAsJson);
            JsonElement value = doc.RootElement;
            if (value.ValueKind != JsonValueKind.Object
                || !value.TryGetProperty("type", out JsonElement type) || type.ValueKind != JsonValueKind.String)
                return;

            string? command = type.GetString();
            string? profileName = null;
            if (command == "lobby.profile.rename")
            {
                if (!value.TryGetProperty("value", out JsonElement rawName)
                    || rawName.ValueKind != JsonValueKind.String) return;
                profileName = rawName.GetString();
                if (profileName is null || profileName.Length > 256) return;
            }
            string? id = null;
            if (value.TryGetProperty("id", out JsonElement rawId))
            {
                if (rawId.ValueKind != JsonValueKind.String) return;
                id = rawId.GetString();
                if (id is null || id.Length is <1 or >80
                    || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or ' ' or '\'')))
                    return;
            }

            switch (command)
            {
                case "lobby.ready":
                    listening = true;
                    Refresh();
                    break;
                case "lobby.ui-ready":
                    if (!listening || ready) break;
                    ready = true;
                    browser.Visibility = Visibility.Visible;
                    IsHitTestVisible = true;
                    Opacity = 0;
                    BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1,
                        TimeSpan.FromMilliseconds(280))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    readyChanged();
                    break;
                case "lobby.refresh":
                    Refresh();
                    if (recordsPage is not null) RefreshRecords();
                    break;
                case "lobby.arcade.run" when id is not null && ArcadeChallengeCatalog.IsGame(id):
                {
                    // Only a terminal run receipt from the trusted local WebView may be
                    // committed. Enforce bounded numeric values and a deduplication token.
                    if (!value.TryGetProperty("score", out JsonElement rawScore)
                        || rawScore.ValueKind != JsonValueKind.Number
                        || !rawScore.TryGetInt32(out int score) || score is < 0 or > 100_000_000
                        || !value.TryGetProperty("seconds", out JsonElement rawSeconds)
                        || rawSeconds.ValueKind != JsonValueKind.Number
                        || !rawSeconds.TryGetInt32(out int seconds) || seconds is < 0 or > 21_600
                        || !value.TryGetProperty("runId", out JsonElement rawRun)
                        || rawRun.ValueKind != JsonValueKind.String
                        || !Guid.TryParse(rawRun.GetString(), out Guid runId)) break;
                    if (arcadeRun(id, score, seconds, runId))
                    {
                        Refresh();
                        if (recordsPage == "challenges") RefreshRecords();
                    }
                    break;
                }
                case "lobby.records.open" when id is "challenges" or "statistics" or "profile" or "history" or "season" or "vault" or "album":
                    shopOpen = false;
                    recordsPage = id;
                    RefreshRecords();
                    break;
                case "lobby.records.close":
                    recordsPage = null;
                    recordsClosed();
                    break;
                case "lobby.album.open" when recordsPage == "album" && id is not null:
                {
                    var result = albumOpen(id);
                    // A successful command carries its committed receipt as
                    // part of the acknowledgement. The browser no longer depends
                    // on a separate full-collection snapshot to begin revealing.
                    PostMessage("lobby.album.result", new {
                        Operation = "open", Id = id, result.Success, result.Message,
                        Pending = albumPendingSnapshot()
                    });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.album.complete" when recordsPage == "album" && id is not null:
                {
                    var result = albumComplete(id);
                    PostMessage("lobby.album.result", new {
                        Operation = "complete", Id = id, result.Success, result.Message,
                        Pending = albumPendingSnapshot()
                    });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.album.claim" when recordsPage == "album" && id is not null:
                {
                    var result = albumClaim(id);
                    PostMessage("lobby.album.result", new { Operation = "claim", Id = id, result.Success, result.Message });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.records.vault.open" when recordsPage == "vault":
                {
                    var result = vaultOpen();
                    PostMessage("lobby.records.vault.result", new { result.Success, result.Message, result.Reward });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.profile.rename" when recordsPage == "profile" && profileName is not null:
                {
                    var result = renameProfile(profileName);
                    PostMessage("lobby.records.result", new { result.Success, result.Message, Id = "rename" });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.profile.equip" when recordsPage == "profile" && id is not null:
                {
                    var result = equipProfile(id);
                    PostMessage("lobby.records.result", new { result.Success, result.Message, Id = id });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.records.season.activate" when recordsPage == "season" && id is not null:
                {
                    var result = activateSeason(id);
                    PostMessage("lobby.records.result", new { result.Success, result.Message, Id = id });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.records.claim" when recordsPage == "challenges" && id is not null:
                {
                    var result = claimChallenge(id);
                    PostMessage("lobby.records.result", new { result.Success, result.Message, Id = id });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.records.claim-all" when recordsPage == "challenges":
                {
                    var result = claimAllChallenges();
                    PostMessage("lobby.records.result", new { result.Success, result.Message, Id = "all" });
                    RefreshRecords();
                    Refresh();
                    break;
                }
                case "lobby.shop.open":
                    recordsPage = null;
                    shopOpen = true;
                    RefreshShop();
                    break;
                case "lobby.shop.close":
                    shopOpen = false;
                    shopClosed();
                    break;
                case "lobby.shop.action" when shopOpen && id is not null:
                {
                    var result = cosmeticAction(id);
                    PostMessage("lobby.shop.result", new { Id = id, result.Success, result.Message });
                    RefreshShop();
                    Refresh();
                    break;
                }
                case "lobby.shop.pack" when shopOpen && id is not null:
                {
                    var result = packAction(id);
                    PostMessage("lobby.shop.result", new { Id = id, result.Success, result.Message });
                    RefreshShop();
                    Refresh();
                    break;
                }
                case "lobby.open" when id is not null:
                    modes.Open(id);
                    break;
                case "lobby.open-secondary" when id is not null:
                    modes.OpenSecondary(id);
                    break;
                case "lobby.progression" when id is "Season Pass" or "Cosmetic Shop"
                    or "Collector's Album" or "Challenges" or "Statistics":
                    progression(id);
                    break;
                case "lobby.profile":
                    ShowProgression("profile");
                    break;
                case "lobby.profile.native":
                    profile();
                    break;
                case "lobby.vault":
                    vault();
                    break;
                case "lobby.daily.claim":
                    dailyClaim();
                    Refresh();
                    break;
                case "lobby.language.set" when id is "en" or "de":
                    languageChange(id);
                    Refresh();
                    break;
                case "lobby.updates.open" when ready && IsVisible:
                    updatesOpen = true;
                    RefreshUpdates();
                    break;
                case "lobby.updates.close":
                    updatesOpen = false;
                    break;
                case "lobby.updates.channel.set" when updatesOpen && ready && IsVisible && (id is "stable" or "development"):
                    setUpdateChannel(id);
                    RefreshUpdates();
                    break;
                case "lobby.updates.check" when updatesOpen && ready && IsVisible:
                    _ = checkUpdate();
                    break;
                case "lobby.updates.install" when updatesOpen && ready && IsVisible:
                    _ = installUpdate();
                    break;
                case "lobby.settings.music" when ready && IsVisible && (id is "true" or "false"):
                case "lobby.settings.empire" when ready && IsVisible && (id is "true" or "false"):
                    presentationSetting(command!, id);
                    Refresh();
                    break;
                case "lobby.settings.volume" when ready && IsVisible
                    && int.TryParse(id, out int volume) && volume is >= 0 and <= 32:
                    presentationSetting(command!, id);
                    Refresh();
                    break;
                case "lobby.settings.fullscreen" when ready && IsVisible:
                    presentationSetting(command!, null);
                    Refresh();
                    break;
                case "lobby.exit":
                    exit();
                    break;
            }
        }
        catch (JsonException) { }
    }

    private void Fail(string reason)
    {
        if (failed || disposed) return;
        FailureReason = reason;
        System.Diagnostics.Trace.TraceWarning("Modern Lobby fallback activated: " + reason);
        failed = true;
        ready = false;
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
        readyChanged(); // restore original WPF header and fallback lobby
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Loaded -= OnLoaded;
        browser.Dispose();
    }
}
