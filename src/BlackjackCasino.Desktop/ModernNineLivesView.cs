using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>WPF is only the temporary host; gameplay and presentation live elsewhere.</summary>
internal sealed class ModernNineLivesView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly WebView2 browser = new()
    {
        Name = "ModernNineLivesBrowser",
        Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 11, 8, 22),
    };
    private readonly ModernNineLivesSession commands;
    private readonly Func<object> snapshot;
    private readonly Func<ModernNineLivesCommand, (bool Success, string Message)> commit;
    private readonly Action close;
    private readonly Action failure;
    private readonly Action<string, int> reelStop;
    private readonly Action<string> spinComplete;
    private readonly DispatcherTimer watchdog = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(18),
    };
    private bool initialized, disposed, listening;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => File.Exists(Path.Combine(
        AppContext.BaseDirectory, "ModernFrontend", "nine-lives.html"));

    internal ModernNineLivesView(ModernNineLivesSession commands, Func<object> snapshot,
        Func<ModernNineLivesCommand, (bool Success, string Message)> commit,
        Action close, Action failure, Action<string, int> reelStop, Action<string> spinComplete)
    {
        this.commands = commands;
        this.snapshot = snapshot;
        this.commit = commit;
        this.close = close;
        this.failure = failure;
        this.reelStop = reelStop;
        this.spinComplete = spinComplete;
        Name = "ModernNineLivesPresentation";
        Content = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(11, 8, 22)),
            Children =
            {
                new TextBlock
                {
                    Text = "NINE LIVES — Opening the rooftop…",
                    Foreground = Brushes.Wheat, FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                browser,
            },
        };
        // Keep the actual WebView HWND visible. Hidden-until-ready causes a hover deadlock.
        watchdog.Tick += (_, _) => { watchdog.Stop(); if (!Ready && !disposed) Fail(); };
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && listening && !disposed && !Failed)
                Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
        };
    }

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
                if (!ModernNineLivesProtocol.Trusted(args.Uri)) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) => { if (!args.IsSuccess) Fail(); };
            core.ProcessFailed += (_, _) => Fail();
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/nine-lives.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Fail();
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || Failed || !ModernNineLivesProtocol.Trusted(e.Source)
            || e.WebMessageAsJson.Length > 4096) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "nine-lives.ready":
                    listening = true;
                    Refresh();
                    Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
                    break;
                case "nine-lives.ui-ready":
                    if (listening) { Ready = true; watchdog.Stop(); }
                    break;
                case "nine-lives.refresh":
                    Refresh();
                    break;
                case "nine-lives.close":
                    close();
                    break;
                case "nine-lives.recover" when listening && Ready:
                    Fail();
                    break;
                case "nine-lives.command" when listening && Ready && IsVisible:
                    ModernNineLivesCommand? intent = ModernNineLivesProtocol.Parse(e.WebMessageAsJson);
                    if (intent is null) break;
                    ModernNineLivesReply receipt = commands.Execute(intent, commit);
                    Post("nine-lives.result", receipt);
                    Refresh();
                    break;
                case "nine-lives.reel-stop" when listening && Ready && IsVisible:
                    if (ReadSpinId(root, out string stopId)
                        && root.TryGetProperty("reel", out JsonElement index)
                        && index.ValueKind == JsonValueKind.Number
                        && index.TryGetInt32(out int reel) && reel is >= 0 and < 3)
                        reelStop(stopId, reel);
                    break;
                case "nine-lives.animation-complete" when listening && Ready && IsVisible:
                    if (ReadSpinId(root, out string spinId)) spinComplete(spinId);
                    break;
            }
        }
        catch (JsonException) { }
    }

    private static bool ReadSpinId(JsonElement root, out string id)
    {
        id = "";
        if (!root.TryGetProperty("spinId", out JsonElement element)
            || element.ValueKind != JsonValueKind.String
            || !Guid.TryParseExact(element.GetString(), "D", out Guid parsed)) return false;
        id = parsed.ToString("D");
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
        Post("nine-lives.snapshot", new
        {
            Version = ModernNineLivesProtocol.Version, Session = commands.Id,
            commands.Revision, State = snapshot(),
        });
    }

    private void Fail()
    {
        if (disposed || Failed) return;
        Failed = true; Ready = false; listening = false;
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
