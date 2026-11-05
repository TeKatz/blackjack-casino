using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed class ModernEmpireView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    // Keep WebView2 in WPF's native composition tree from the first layout pass.
    // Hiding its HWND until React finishes can leave the WPF surface visually stale
    // until a later pointer/hover invalidation, even though the bridge is ready.
    private readonly WebView2 browser = new() { Name = "ModernEmpireBrowser", Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 6, 22, 20) };
    private readonly EmpireModernSession commands = new();
    private readonly Func<object> snapshot;
    private readonly Func<EmpireModernCommand, EmpireActionResult> commit;
    private readonly Func<EmpireModernCommand, object> preview;
    private readonly Action close;
    private readonly Action? retry;
    private readonly DispatcherTimer startupTimeout = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly Dictionary<string, double> visitTimings = new();
    private long visitStarted;
    private int visitId;
    private readonly TextBlock status = new() { Text = "Browser vorbereiten…", FontSize = 17, Foreground = Brushes.Wheat,
        HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
    private readonly Button retryButton = new() { Content = "Erneut versuchen / Retry", Margin = new Thickness(10), Visibility = Visibility.Collapsed };

    private bool initialized, listening, disposed;
    private string? notice;
    private object? offline;
    private EmpireModernReply? lastReply;
    private Window? owner;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal WebView2 Browser => browser;
    internal IReadOnlyDictionary<string, double> VisitTimings => new Dictionary<string, double>(visitTimings);
    internal void BeginVisit(long startedAt)
    {
        if (disposed) return;
        visitStarted = startedAt;
        visitId++;
        visitTimings.Clear();
        Mark("host_view_ready");
        if (browser.CoreWebView2 is not null) Mark("webview_reused");
        // Rendering and the in-browser Back action remain available; the native
        // command bridge stays closed until this visit's DOM acknowledgement.
        Ready = false;
        startupTimeout.Stop();
        startupTimeout.Start();
        if (!Ready) SetStage(Local("Browser vorbereiten…", "Preparing browser…"));
    }
    internal void PauseVisit() { startupTimeout.Stop(); }
    private void Mark(string stage)
    {
        if (visitStarted != 0) visitTimings[stage] = Math.Round(Stopwatch.GetElapsedTime(visitStarted).TotalMilliseconds, 2);
    }
    private static string Local(string german, string english)
        => Localization.Language == UiLanguagePreference.German ? german : english;
    private void SetStage(string text) { status.Text = text; }
    internal ModernEmpireView(Func<object> snapshot, Func<EmpireModernCommand, EmpireActionResult> commit,
        Func<EmpireModernCommand, object> preview, Action close, Action? retry = null)
    {
        this.snapshot = snapshot; this.commit = commit; this.preview = preview; this.close = close; this.retry = retry;
        Name = "ModernEmpirePresentation";
        var back = new Button { Content = "← Casino lobby", HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20) };
        back.Content = Local("← Casino-Lobby", "← Casino lobby");
        back.Click += (_, _) => close();
        var indicator = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        indicator.Children.Add(new TextBlock { Text = "CASINO EMPIRE", FontSize = 28, Foreground = Brushes.Wheat,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 18) });
        indicator.Children.Add(status);
        retryButton.Content = Local("Erneut versuchen", "Retry");
        retryButton.Click += (_, _) => this.retry?.Invoke();
        indicator.Children.Add(retryButton);
        Content = new Grid { Background = new SolidColorBrush(Color.FromRgb(6, 22, 20)), Children = { indicator, back, browser } };
        startupTimeout.Tick += (_, _) => Fail(Local("Das Laden dauert zu lange. Zur Lobby zurückkehren oder erneut versuchen.", "Loading timed out. Return to the lobby or retry."));
        BeginVisit(Stopwatch.GetTimestamp());
        Loaded += OnLoaded;
        IsVisibleChanged += OnVisibility;
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || disposed) return;
        initialized = true;
        Mark("host_loaded");
        owner = Window.GetWindow(this);
        if (owner is not null) owner.StateChanged += OnWindowState;
        string folder = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");
        if (!File.Exists(Path.Combine(folder, "empire.html"))) { Fail(Local("Die Casino-Empire-Dateien fehlen. Bitte die Anwendung aktualisieren.", "Empire presentation is missing. Update the application.")); return; }
        try
        {
            await browser.EnsureCoreWebView2Async();
            if (disposed) return;
            Mark("webview_initialized");
            var core = browser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.SetVirtualHostNameToFolderMapping(ManorPreviewProtocol.VirtualHost, folder, CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) => { if (!EmpireModernProtocol.Trusted(args.Uri)) args.Cancel = true; };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess) Fail(Local("Das Anwesen konnte nicht geladen werden.", "Empire could not load."));
                else if (!Failed) { Mark("navigation_completed"); SetStage(Local("Anwesen laden…", "Loading estate…")); }
            };
            core.ProcessFailed += (_, _) => Fail(Local("Die Empire-Ansicht wurde beendet. Erneut öffnen, um den gespeicherten Stand zu laden.", "Empire presentation stopped. Reopen it to restore the saved state."));
            core.WebMessageReceived += OnMessage;
            SetStage(Local("Browser vorbereiten…", "Preparing browser…"));
            Mark("navigation_started");
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/empire.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        { Fail(Local("Empire konnte nicht gestartet werden: ", "Empire could not start: ") + ex.Message); }
    }
    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (disposed || Failed || !EmpireModernProtocol.Trusted(args.Source) || args.WebMessageAsJson.Length > 8192) return;
        try
        {
            using var doc = JsonDocument.Parse(args.WebMessageAsJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "empire.ready":
                    listening = true; Mark("react_first_render"); Mark("bridge_ready"); SetStage("Anwesen laden… / Loading estate…"); Refresh(); break;
                case "empire.ui-ready":
                    if (!listening || !IsVisible || !root.TryGetProperty("visitId", out var visit) ||
                        visit.ValueKind != JsonValueKind.Number || !visit.TryGetInt32(out int acceptedVisit) || acceptedVisit != visitId) break;
                    Mark("dom_usable"); Ready = true; startupTimeout.Stop();
                    // Acknowledging the current snapshot opens the management controls,
                    // without toggling WPF WebView2 visibility or disturbing its HWND.
                    Post("empire.activated", new { VisitId = visitId });
                    PostVisibility(); break;
                case "empire.graphics-ready":
                    Mark("graphics_ready"); break;
                case "empire.refresh": Refresh(); break;
                case "empire.close": close(); break;
                case "empire.dismiss-offline": offline = null; Refresh(); break;
                case "empire.command" when listening && Ready && IsVisible &&
                    owner?.WindowState != WindowState.Minimized:
                    EmpireModernCommand? command = EmpireModernProtocol.Parse(args.WebMessageAsJson);
                    if (command is null)
                    {
                        if (root.TryGetProperty("requestId", out var id) && id.ValueKind == JsonValueKind.String
                            && Guid.TryParseExact(id.GetString(), "D", out _))
                            Post("empire.result", new EmpireModernReply(id.GetString()!, false,
                                Localization.TranslateSource("Invalid Empire command. Check the selected options and retry."), commands.Revision));
                        else Post("empire.invalid", new { Message = "Invalid Empire command." });
                        break;
                    }
                    EmpireModernReply result = commands.Execute(command, commit, preview);
                    result = result with { Message = Localization.TranslateSource(result.Message) };
                    if (!command.Preview) lastReply = result;
                    if (!command.Preview) notice = result.Message;
                    Post("empire.result", result);
                    if (!command.Preview) Refresh();
                    break;
            }
        }
        catch (JsonException) { }
    }
    private void Post(string type, object? data)
    {
        if (!disposed && listening && browser.CoreWebView2 is not null)
            browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, Json));
    }
    internal void Refresh()
    {
        if (!listening || disposed || !IsVisible || owner?.WindowState == WindowState.Minimized) return;
        Post("empire.snapshot", new { Version = EmpireModernProtocol.Version, Session = commands.Id,
            commands.Revision, VisitId = visitId, Notice = notice is null ? null : Localization.TranslateSource(notice), Offline = offline, LastReply = lastReply, State = snapshot() });
    }
    internal void RefreshLive() => Refresh();
    internal void SetNotice(string message) => notice = message;
    internal void ShowOfflineReport(EmpireAccrual report)
    {
        offline = new { Credits = EmpireModernSnapshot.Decimal(report.Credits), Dividend = EmpireModernSnapshot.Decimal(report.Dividend),
            Seconds = report.Elapsed.TotalSeconds, report.Capped,
            Rooms = report.Rooms.ToDictionary(p => p.Key, p => EmpireModernSnapshot.Decimal(p.Value)),
            Properties = report.Properties.ToDictionary(p => p.Key, p => EmpireModernSnapshot.Decimal(p.Value)) };
        Refresh();
    }
    internal void StopVisualAnimations() => Refresh();
    internal bool TryCloseManagement() { Post("empire.escape", null); return true; }
    private void OnVisibility(object sender, DependencyPropertyChangedEventArgs e) { if (IsVisible) Refresh(); PostVisibility(); }
    private void OnWindowState(object? sender, EventArgs e) { PostVisibility(); Refresh(); }
    private void PostVisibility() => Post("empire.visibility", new { Visible = IsVisible && owner?.WindowState != WindowState.Minimized });
    private void Fail(string message)
    {
        Ready = false; Failed = true; listening = false; startupTimeout.Stop(); browser.Visibility = Visibility.Hidden;
        // Only the failed-browser path hides the HWND to expose native retry/Back.
        status.FontSize = 15; SetStage(message);
        retryButton.Visibility = retry is null ? Visibility.Collapsed : Visibility.Visible;
    }
    public void Dispose()
    {
        if (disposed) return;
        Post("empire.visibility", new { Visible = false });
        PauseVisit();
        disposed = true;
        Loaded -= OnLoaded; IsVisibleChanged -= OnVisibility;
        if (owner is not null) owner.StateChanged -= OnWindowState;
        browser.Dispose();
    }
}
