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
internal sealed class ModernDiceView : ContentControl, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly WebView2 browser = new()
    {
        Name = "ModernDiceBrowser",
        Visibility = Visibility.Visible,
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 9, 19, 22),
    };
    private readonly ModernDiceSession commands;
    private readonly Func<object> snapshot;
    private readonly Func<ModernDiceCommand, (bool Success, string Message)> commit;
    private readonly Action close;
    private readonly Action failure;
    private readonly DispatcherTimer watchdog = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(18),
    };
    private bool initialized, disposed, listening;
    internal bool Ready { get; private set; }
    internal bool Failed { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => File.Exists(Path.Combine(
        AppContext.BaseDirectory, "ModernFrontend", "midnight-dice.html"));

    internal ModernDiceView(ModernDiceSession commands, Func<object> snapshot,
        Func<ModernDiceCommand, (bool Success, string Message)> commit,
        Action close, Action failure)
    {
        this.commands = commands;
        this.snapshot = snapshot;
        this.commit = commit;
        this.close = close;
        this.failure = failure;
        Name = "ModernDicePresentation";
        Content = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(11, 8, 22)),
            Children =
            {
                new TextBlock
                {
                    Text = "MIDNIGHT DICE — Preparing the casino felt…",
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
        Localization.LanguageChanged += LanguageChanged;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && listening && !disposed && !Failed)
                Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
        };
    }

    private void LanguageChanged(object? sender, EventArgs e) => Refresh();

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
                if (!ModernDiceProtocol.Trusted(args.Uri)) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, args) => { if (!args.IsSuccess) Fail(); };
            core.ProcessFailed += (_, _) => Fail();
            core.WebMessageReceived += OnMessage;
            browser.Source = new Uri("https://" + ManorPreviewProtocol.VirtualHost + "/midnight-dice.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Fail();
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || Failed || !ModernDiceProtocol.Trusted(e.Source)
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
                case "midnight-dice.ready":
                    listening = true;
                    Refresh();
                    Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
                    break;
                case "midnight-dice.ui-ready":
                    if (listening) { Ready = true; watchdog.Stop(); }
                    break;
                case "midnight-dice.refresh":
                    Refresh();
                    break;
                case "midnight-dice.close" when listening && Ready:
                    close();
                    break;
                case "midnight-dice.recover" when listening && Ready:
                    Fail();
                    break;
                case "midnight-dice.command" when listening && Ready && IsVisible:
                    ModernDiceCommand? intent = ModernDiceProtocol.Parse(e.WebMessageAsJson);
                    if (intent is null) break;
                    ModernDiceReply receipt = commands.Execute(intent, commit);
                    Post("midnight-dice.result", receipt);
                    Refresh();
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
        Post("midnight-dice.snapshot", new
        {
            Version = ModernDiceProtocol.Version, Session = commands.Id,
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
        Localization.LanguageChanged -= LanguageChanged;
        browser.Dispose();
    }
}
