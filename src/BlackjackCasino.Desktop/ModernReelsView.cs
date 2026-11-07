using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

internal sealed class ModernReelsView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly WebView2 browser = new()
    {
        Name = "ModernReelsBrowser",
        Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 12, 10, 25),
    };
    private readonly ModernReelsSession commands = new();
    private readonly Func<object> snapshot;
    private readonly Func<ModernReelsCommand, (bool Success, string Message)> commit;
    private readonly Action close;
    private readonly Action failure;
    private readonly Action<string, int> reelStop;
    private readonly Action<string> spinComplete;
    private readonly DispatcherTimer watchdog = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(18),
    };
    private bool initialized, disposed, listening, navigationComplete;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal string? FailureReason { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => FrontendAssetFailure() is null;

    // Checking only reels.html gives a misleading enabled Retry when the
    // updater left behind an HTML entry with missing hashed JS/CSS files.
    internal static string? FrontendAssetFailure()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");
        string entry = Path.Combine(folder, "reels.html");
        if (!File.Exists(entry)) return "Packaged reels.html is missing.";
        try
        {
            string markup = File.ReadAllText(entry);
            var assets = Regex.Matches(markup,
                @"(?:src|href)\s*=\s*[""'](?<file>(?:\.?/)?assets/[^""'?#]+)[""']",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            bool hasJavascript = false;
            foreach (Match asset in assets)
            {
                string relative = asset.Groups["file"].Value.TrimStart('.', '/')
                    .Replace('/', Path.DirectorySeparatorChar);
                hasJavascript |= relative.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
                if (!File.Exists(Path.Combine(folder, relative)))
                    return "Packaged Reels resource is missing: " + Path.GetFileName(relative);
            }
            return hasJavascript ? null : "Reels HTML has no compiled JavaScript entry.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Cannot read Reels frontend files (" + ex.GetType().Name + ").";
        }
    }

    internal ModernReelsView(Func<object> snapshot,
        Func<ModernReelsCommand, (bool Success, string Message)> commit,
        Action close, Action failure, Action<string, int> reelStop, Action<string> spinComplete)
    {
        this.snapshot = snapshot;
        this.commit = commit;
        this.close = close;
        this.failure = failure;
        this.reelStop = reelStop;
        this.spinComplete = spinComplete;
        Name = "ModernReelsPresentation";
        var status = new TextBlock
        {
            Text = "MIDNIGHT REELS — Preparing your cabinet…",
            FontSize = 18,
            Foreground = Brushes.Wheat,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Never hide the WebView2 HWND until a React acknowledgement: it can
        // deadlock visual invalidation until mouse hover (historical Empire bug).
        Content = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(12, 10, 25)),
            Children = { status, browser },
        };
        watchdog.Tick += (_, _) =>
        {
            watchdog.Stop();
            if (!Ready && !disposed)
                FailWithReason(!navigationComplete
                    ? "Reels HTML navigation timed out."
                    : !listening ? "Reels JavaScript did not send its startup handshake."
                    : "Reels did not acknowledge the native snapshot.");
        };
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && listening && !disposed && !Failed)
                Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (initialized || disposed) return;
        initialized = true;
        watchdog.Start();
        if (FrontendAssetFailure() is string assetFailure) { FailWithReason(assetFailure); return; }
        try
        {
            CoreWebView2? core = await ModernWebViewBootstrap.InitializeAsync(
                browser, () => disposed, Path.Combine(AppContext.BaseDirectory, "ModernFrontend"),
                ModernReelsProtocol.Trusted);
            if (core is null) return;
            core.NavigationCompleted += (_, e) =>
            {
                if (!e.IsSuccess) FailWithReason("Reels navigation failed (" + e.WebErrorStatus + ").");
                else navigationComplete = true;
            };
            core.ProcessFailed += (_, e) =>
                FailWithReason("WebView2 process failed (" + e.ProcessFailedKind + ").");
            core.WebMessageReceived += OnMessage;
            // Module-load failures can occur before React mounts and sends reels.ready.
            // A trusted, document-local error signal distinguishes them from a timeout.
            await core.AddScriptToExecuteOnDocumentCreatedAsync("""
                (() => {
                  const report = () => window.chrome?.webview?.postMessage({type:'reels.client-error'});
                  window.addEventListener('error', event => {
                    if (event.target === window || event.target instanceof HTMLScriptElement
                      || (event.target instanceof HTMLLinkElement && event.target.rel === 'stylesheet'))
                      report();
                  }, true);
                  window.addEventListener('unhandledrejection', report);
                })();
                """);
            if (disposed) return;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/reels.html");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            FailWithReason("WebView2 startup failed (" + ex.GetType().Name
                + ", 0x" + ex.HResult.ToString("X8") + ").");
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || Failed || !ModernReelsProtocol.Trusted(e.Source)
            || e.WebMessageAsJson.Length > 4096) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var kind)
                || kind.ValueKind != JsonValueKind.String) return;
            switch (kind.GetString())
            {
                case "reels.client-error":
                    FailWithReason("Reels JavaScript or a required frontend module failed.");
                    break;
                case "reels.snapshot-invalid":
                    FailWithReason("Reels frontend rejected the native snapshot.");
                    break;
                case "reels.ready":
                    listening = true;
                    Refresh();
                    Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
                    break;
                case "reels.ui-ready":
                    if (listening) { Ready = true; watchdog.Stop(); }
                    break;
                case "reels.refresh":
                    Refresh();
                    break;
                case "reels.close":
                    close();
                    break;
                case "reels.command" when listening && Ready && IsVisible:
                    ModernReelsCommand? command = ModernReelsProtocol.Parse(e.WebMessageAsJson);
                    if (command is null) break;
                    ModernReelsReply result = commands.Execute(command, commit);
                    Post("reels.result", result);
                    Refresh();
                    break;
                case "reels.reel-stop" when listening && Ready && IsVisible:
                    if (ReadSpinId(root, out var stopId)
                        && root.TryGetProperty("reel", out var index)
                        && index.ValueKind == JsonValueKind.Number
                        && index.TryGetInt32(out int number) && number is >= 0 and <= 2)
                        reelStop(stopId, number);
                    break;
                case "reels.animation-complete" when listening && Ready && IsVisible:
                    if (ReadSpinId(root, out var finished)) spinComplete(finished);
                    break;
            }
        }
        catch (JsonException) { }
    }

    private static bool ReadSpinId(JsonElement root, out string spinId)
    {
        spinId = "";
        if (!root.TryGetProperty("spinId", out var id)
            || id.ValueKind != JsonValueKind.String
            || !Guid.TryParseExact(id.GetString(), "D", out Guid parsed)) return false;
        spinId = parsed.ToString("D");
        return true;
    }

    private void Post(string type, object data)
    {
        if (!disposed && browser.CoreWebView2 is not null)
            browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void Refresh()
    {
        if (disposed || Failed || !listening || !IsVisible) return;
        try
        {
            Post("reels.snapshot", new
            {
                Version = ModernReelsProtocol.Version,
                Session = commands.Id,
                commands.Revision,
                State = snapshot(),
            });
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            FailWithReason("Native Reels snapshot failed (" + ex.GetType().Name + ").");
        }
    }

    // Kept parameterless for the existing Windows crash-injection regression smoke.
    private void Fail() => FailWithReason("The Reels browser was interrupted.");

    private void FailWithReason(string reason)
    {
        if (disposed || Failed) return;
        FailureReason = reason;
        Failed = true;
        Ready = false;
        listening = false;
        watchdog.Stop();
        browser.Visibility = Visibility.Collapsed;
        Dispatcher.BeginInvoke(failure, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        watchdog.Stop();
        Loaded -= OnLoaded;
        browser.Dispose();
    }
}
