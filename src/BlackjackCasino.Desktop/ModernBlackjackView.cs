using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

// WPF hosts the browser but never trusts it with cards, bets or payouts.
internal sealed class ModernBlackjackView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly WebView2 browser = new()
    {
        Name = "ModernBlackjackBrowser",
        Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 6, 23, 20),
    };
    private readonly BlackjackModernSession commands = new();
    private readonly Func<object> snapshot;
    private readonly Func<BlackjackModernCommand, (bool Success, string Message)> commit;
    private readonly Action close;
    private readonly Action onFailure;
    private readonly DispatcherTimer readinessWatchdog = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(18),
    };
    private readonly TextBlock status = new()
    {
        Text = "BLACKJACK — Preparing your table…",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 19,
        Foreground = Brushes.Wheat,
    };
    private bool initialized, disposed, listening;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => File.Exists(Path.Combine(AppContext.BaseDirectory,
        "ModernFrontend", "blackjack.html"));

    internal ModernBlackjackView(Func<object> snapshot,
        Func<BlackjackModernCommand, (bool Success, string Message)> commit, Action close,
        Action onFailure)
    {
        this.snapshot = snapshot;
        this.commit = commit;
        this.close = close;
        this.onFailure = onFailure;
        readinessWatchdog.Tick += (_, _) =>
        {
            readinessWatchdog.Stop();
            if (!Ready && !Failed && !disposed)
                Fail("Blackjack frontend did not respond. Opening native table.");
        };
        Name = "ModernBlackjackPresentation";
        var back = new Button
        {
            Content = "← Casino lobby",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(20),
        };
        back.Click += (_, _) => close();
        // WebView2 must be visible from the first layout pass. Waiting for an
        // acknowledgement while it is Hidden reproduced the Empire hover bug.
        Content = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(6, 23, 20)),
            Children = { status, back, browser },
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || disposed) return;
        initialized = true;
        readinessWatchdog.Start();
        if (!IsAvailable) { Fail("Blackjack frontend is missing. Return to the table."); return; }
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
            core.NavigationStarting += (_, args) => { if (!BlackjackModernProtocol.Trusted(args.Uri)) args.Cancel = true; };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess) Fail("Blackjack could not load. Return to the native table.");
            };
            core.ProcessFailed += (_, _) => Fail("Blackjack browser failed. Return to the native table.");
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/blackjack.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Fail("Blackjack browser unavailable: " + ex.Message);
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (disposed || Failed || !BlackjackModernProtocol.Trusted(args.Source)
            || args.WebMessageAsJson.Length > 4096) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(args.WebMessageAsJson);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "blackjack.ready":
                    listening = true;
                    Refresh();
                    break;
                case "blackjack.ui-ready":
                    if (listening)
                    {
                        Ready = true;
                        readinessWatchdog.Stop();
                    }
                    break;
                case "blackjack.refresh":
                    Refresh();
                    break;
                case "blackjack.close":
                    close();
                    break;
                case "blackjack.command" when listening && Ready && IsVisible:
                    BlackjackModernCommand? command = BlackjackModernProtocol.Parse(args.WebMessageAsJson);
                    if (command is null) break;
                    BlackjackModernReply result = commands.Execute(command, commit);
                    Post("blackjack.result", result);
                    Refresh();
                    break;
            }
        }
        catch (JsonException) { }
    }

    private void Post(string type, object data)
    {
        if (!disposed && browser.CoreWebView2 is not null)
            browser.CoreWebView2.PostWebMessageAsJson(
                JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void Refresh()
    {
        if (disposed || !listening || !IsVisible) return;
        Post("blackjack.snapshot", new
        {
            Version = BlackjackModernProtocol.Version,
            Session = commands.Id,
            commands.Revision,
            State = snapshot(),
        });
    }

    private void Fail(string message)
    {
        if (disposed || Failed) return;
        readinessWatchdog.Stop();
        Failed = true;
        Ready = false;
        listening = false;
        status.Text = message;
        browser.Visibility = Visibility.Collapsed;
        // Avoid disposing WebView2 while its own callback is still executing.
        Dispatcher.BeginInvoke(onFailure, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        readinessWatchdog.Stop();
        Loaded -= OnLoaded;
        browser.Dispose();
    }
}
