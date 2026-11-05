using System.IO;
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
    private readonly WebView2 browser = new() { Name = "ModernEmpireBrowser", Visibility = Visibility.Hidden,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 6, 22, 20) };
    private readonly EmpireModernSession commands = new();
    private readonly Func<object> snapshot;
    private readonly Func<EmpireModernCommand, EmpireActionResult> commit;
    private readonly Func<EmpireModernCommand, object> preview;
    private readonly Action close;
    private readonly TextBlock status = new() { Text = "CASINO EMPIRE", FontSize = 28, Foreground = Brushes.Wheat,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private bool initialized, listening, disposed;
    private string? notice;
    private object? offline;
    private EmpireModernReply? lastReply;
    private Window? owner;
    internal bool Ready { get; private set; }
    internal WebView2 Browser => browser;
    internal ModernEmpireView(Func<object> snapshot, Func<EmpireModernCommand, EmpireActionResult> commit,
        Func<EmpireModernCommand, object> preview, Action close)
    {
        this.snapshot = snapshot; this.commit = commit; this.preview = preview; this.close = close;
        Name = "ModernEmpirePresentation";
        var back = new Button { Content = "← Casino lobby", HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20) };
        back.Click += (_, _) => close();
        Content = new Grid { Background = new SolidColorBrush(Color.FromRgb(6, 22, 20)), Children = { status, back, browser } };
        Loaded += OnLoaded;
        IsVisibleChanged += OnVisibility;
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || disposed) return;
        initialized = true;
        owner = Window.GetWindow(this);
        if (owner is not null) owner.StateChanged += OnWindowState;
        string folder = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");
        if (!File.Exists(Path.Combine(folder, "empire.html"))) { Fail("Empire presentation is missing. Return to the lobby and update the application."); return; }
        try
        {
            await browser.EnsureCoreWebView2Async();
            if (disposed) return;
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
            core.NavigationCompleted += (_, args) => { if (!args.IsSuccess) Fail("Empire could not load. Return to the lobby and retry."); };
            core.ProcessFailed += (_, _) => Fail("Empire presentation stopped. Reopen it to restore the saved state.");
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/empire.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        { Fail("Empire could not start: " + ex.Message); }
    }
    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (disposed || !EmpireModernProtocol.Trusted(args.Source) || args.WebMessageAsJson.Length > 8192) return;
        try
        {
            using var doc = JsonDocument.Parse(args.WebMessageAsJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "empire.ready": listening = true; Refresh(); break;
                case "empire.ui-ready":
                    if (!listening) break;
                    Ready = true; browser.Visibility = Visibility.Visible; PostVisibility(); break;
                case "empire.refresh": Refresh(); break;
                case "empire.close": close(); break;
                case "empire.dismiss-offline": offline = null; Refresh(); break;
                case "empire.command" when listening:
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
            commands.Revision, Notice = notice is null ? null : Localization.TranslateSource(notice), Offline = offline, LastReply = lastReply, State = snapshot() });
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
    private void OnVisibility(object sender, DependencyPropertyChangedEventArgs e) { PostVisibility(); if (IsVisible) Refresh(); }
    private void OnWindowState(object? sender, EventArgs e) { PostVisibility(); Refresh(); }
    private void PostVisibility() => Post("empire.visibility", new { Visible = IsVisible && owner?.WindowState != WindowState.Minimized });
    private void Fail(string message)
    {
        Ready = false; listening = false; browser.Visibility = Visibility.Hidden;
        status.FontSize = 16; status.Text = message;
    }
    public void Dispose()
    {
        if (disposed) return;
        Post("empire.visibility", new { Visible = false });
        disposed = true;
        Loaded -= OnLoaded; IsVisibleChanged -= OnVisibility;
        if (owner is not null) owner.StateChanged -= OnWindowState;
        browser.Dispose();
    }
}
