using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Shared React Lounge, Sudoku and Solitaire presentation. Native C# retains rules, saves and rewards;
/// this browser sends only versioned, whitelisted gameplay intentions.
/// </summary>
internal sealed class ModernLoungeView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private readonly WebView2 browser = new()
    {
        Name = "ModernLoungeBrowser",
        Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 7, 23, 20),
    };
    private readonly Func<object> snapshot;
    private readonly Action<string> openGame;
    private readonly Func<ModernSudokuCommand, ModernSudokuReply> sudokuCommand;
    private readonly Action sudokuTick;
    private readonly Func<ModernSolitaireCommand, ModernSolitaireReply> solitaireCommand;
    private readonly Action solitaireTick;
    private readonly Func<ModernWordSearchCommand, ModernWordSearchReply> wordSearchCommand;
    private readonly Action wordSearchTick;
    private readonly Func<ModernMemoryCommand, ModernMemoryReply> memoryCommand;
    private readonly Action memoryTick;
    private readonly Action close;
    private readonly Action failure;
    private readonly DispatcherTimer watchdog = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(18),
    };
    private readonly DispatcherTimer refreshClock = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(1),
    };
    private bool initialized, disposed, listening;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => File.Exists(Path.Combine(
        AppContext.BaseDirectory, "ModernFrontend", "lounge.html"));

    internal ModernLoungeView(Func<object> snapshot, Action<string> openGame,
        Func<ModernSudokuCommand, ModernSudokuReply> sudokuCommand, Action sudokuTick,
        Func<ModernSolitaireCommand, ModernSolitaireReply> solitaireCommand, Action solitaireTick,
        Func<ModernWordSearchCommand, ModernWordSearchReply> wordSearchCommand, Action wordSearchTick,
        Func<ModernMemoryCommand, ModernMemoryReply> memoryCommand, Action memoryTick,
        Action close, Action failure)
    {
        this.snapshot = snapshot;
        this.openGame = openGame;
        this.sudokuCommand = sudokuCommand;
        this.sudokuTick = sudokuTick;
        this.solitaireCommand = solitaireCommand;
        this.solitaireTick = solitaireTick;
        this.wordSearchCommand = wordSearchCommand;
        this.wordSearchTick = wordSearchTick;
        this.memoryCommand = memoryCommand;
        this.memoryTick = memoryTick;
        refreshClock.Tick += (_, _) =>
        {
            if (!Ready || !IsVisible || disposed) return;
            sudokuTick();
            solitaireTick();
            wordSearchTick();
            memoryTick();
            Refresh();
        };
        this.close = close;
        this.failure = failure;
        Name = "ModernLoungePresentation";
        Content = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(7, 23, 20)),
            Children =
            {
                new TextBlock
                {
                    Text = "CHILL LOUNGE — Preparing your quiet corner…",
                    Foreground = Brushes.Wheat,
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                browser,
            },
        };
        // Do not hide the WebView HWND while loading: hidden composition can
        // wait indefinitely for a mouse hover before painting the React UI.
        watchdog.Tick += (_, _) => { watchdog.Stop(); if (!Ready && !disposed) Fail(); };
        Loaded += OnLoaded;
        Localization.LanguageChanged += LanguageChanged;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && listening && !disposed && !Failed)
                Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
        };
    }

    private void LanguageChanged(object? sender, EventArgs e) => Refresh();

    internal static bool Trusted(string? source)
        => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals(ManorPreviewProtocol.VirtualHost, StringComparison.OrdinalIgnoreCase)
        && uri.IsDefaultPort && uri.AbsolutePath == "/lounge.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || disposed) return;
        initialized = true;
        watchdog.Start();
        if (!IsAvailable) { Fail(); return; }
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
            core.SetVirtualHostNameToFolderMapping(ManorPreviewProtocol.VirtualHost,
                Path.Combine(AppContext.BaseDirectory, "ModernFrontend"),
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) =>
            {
                if (!Trusted(args.Uri)) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) => { if (!args.IsSuccess) Fail(); };
            core.ProcessFailed += (_, _) => Fail();
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/lounge.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Fail();
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || Failed || !Trusted(e.Source) || e.WebMessageAsJson.Length > 2048) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("version", out JsonElement version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out int protocol) || protocol != 1) return;

            switch (type.GetString())
            {
                case "lounge.ready":
                    listening = true;
                    Refresh();
                    Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
                    break;
                case "lounge.ui-ready" when listening:
                    Ready = true;
                    watchdog.Stop();
                    refreshClock.Start();
                    break;
                case "lounge.close" when listening && Ready:
                    close();
                    break;
                case "lounge.sudoku.command" when listening && Ready && IsVisible:
                    ModernSudokuCommand? request = ModernSudokuProtocol.Parse(e.WebMessageAsJson);
                    if (request is null) break;
                    ModernSudokuReply result = sudokuCommand(request);
                    Post("lounge.sudoku.result", result);
                    Refresh();
                    break;
                case "lounge.solitaire.command" when listening && Ready && IsVisible:
                    ModernSolitaireCommand? solitaireRequest = ModernSolitaireProtocol.Parse(e.WebMessageAsJson);
                    if (solitaireRequest is null) break;
                    ModernSolitaireReply solitaireResult = solitaireCommand(solitaireRequest);
                    Post("lounge.solitaire.result", solitaireResult);
                    Refresh();
                    break;
                case "lounge.word-search.command" when listening && Ready && IsVisible:
                    ModernWordSearchCommand? wordSearchRequest = ModernWordSearchProtocol.Parse(e.WebMessageAsJson);
                    if (wordSearchRequest is null) break;
                    ModernWordSearchReply wordSearchResult = wordSearchCommand(wordSearchRequest);
                    Post("lounge.word-search.result", wordSearchResult);
                    Refresh();
                    break;
                case "lounge.memory.command" when listening && Ready && IsVisible:
                    ModernMemoryCommand? memoryRequest = ModernMemoryProtocol.Parse(e.WebMessageAsJson);
                    if (memoryRequest is null) break;
                    ModernMemoryReply memoryResult = memoryCommand(memoryRequest);
                    Post("lounge.memory.result", memoryResult);
                    Refresh();
                    break;
                case "lounge.open" when listening && Ready && IsVisible:
                    if (!root.TryGetProperty("id", out JsonElement id)
                        || id.ValueKind != JsonValueKind.String) return;
                    string? route = id.GetString();
                    if (route is "sudoku" or "solitaire" or "word-search"
                        or "memory" or "jigsaw" or "tales-backroom")
                        openGame(route);
                    break;
            }
        }
        catch (JsonException) { }
    }

    private void Post(string type, object data)
    {
        if (!disposed && browser.CoreWebView2 is not null)
            browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void Refresh()
    {
        if (disposed || Failed || !listening || !IsVisible) return;
        Post("lounge.snapshot", snapshot());
    }

    private void Fail()
    {
        if (disposed || Failed) return;
        Failed = true;
        Ready = false;
        listening = false;
        watchdog.Stop();
        refreshClock.Stop();
        browser.Visibility = Visibility.Collapsed;
        Dispatcher.BeginInvoke(failure, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        watchdog.Stop();
        refreshClock.Stop();
        Loaded -= OnLoaded;
        Localization.LanguageChanged -= LanguageChanged;
        browser.Dispose();
    }
}
