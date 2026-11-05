using System.IO;
using System.Text.Json;
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

    private readonly WebView2 browser = new() { Visibility = Visibility.Hidden };
    private readonly Func<object> snapshot;
    private readonly Func<object> shopSnapshot;
    private readonly Func<string, (bool Success, string Message)> cosmeticAction;
    private readonly Func<string, (bool Success, string Message)> packAction;
    private readonly Action shopClosed;
    private readonly CasinoModeRegistry modes;
    private readonly Action<string> progression;
    private readonly Action profile;
    private readonly Action vault;
    private readonly Action dailyClaim;
    private readonly Action exit;
    private readonly Action readyChanged;
    private bool initialized;
    private bool listening;
    private bool shopOpen;
    private bool ready;
    private bool failed;
    private bool disposed;

    internal bool Ready => ready && !failed;
    internal bool ShopOpen => shopOpen;
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
        Action<string> progression, Action profile, Action vault, Action dailyClaim, Action exit,
        Action readyChanged)
    {
        this.modes = modes;
        this.snapshot = snapshot;
        this.shopSnapshot = shopSnapshot;
        this.cosmeticAction = cosmeticAction;
        this.packAction = packAction;
        this.shopClosed = shopClosed;
        this.progression = progression;
        this.profile = profile;
        this.vault = vault;
        this.dailyClaim = dailyClaim;
        this.exit = exit;
        this.readyChanged = readyChanged;
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

    private void RefreshShop()
    {
        if (!shopOpen || !listening || disposed || failed || browser.CoreWebView2 is null) return;
        PostMessage("lobby.shop.snapshot", shopSnapshot());
    }

    internal void ShowShop()
    {
        if (!Ready || !listening || disposed || failed) return;
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
            await browser.EnsureCoreWebView2Async();
            if (disposed) return;
            CoreWebView2 core = browser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping(
                ManorPreviewProtocol.VirtualHost,
                Path.GetDirectoryName(EntryPath)!,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) => { if (!Trusted(args.Uri)) args.Cancel = true; };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
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
                    break;
                case "lobby.shop.open":
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
                    profile();
                    break;
                case "lobby.vault":
                    vault();
                    break;
                case "lobby.daily.claim":
                    dailyClaim();
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
