using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Embedded WebView2 preview of the new Manor renderer. All commands are read-only;
/// the established ManorWindow/ManorController remains the only playable entry point.
/// </summary>
internal sealed class ModernManorPreviewWindow : Window
{
    internal static string EntryPath => Path.Combine(AppContext.BaseDirectory, "ModernFrontend", "index.html");
    internal static bool IsAvailable => File.Exists(EntryPath);

    private readonly WebView2 browser = new();
    private readonly TextBlock notice;
    private readonly ManorPreviewProtocol.Snapshot snapshot;
    private bool initialized;
    private bool closed;

    internal ModernManorPreviewWindow(ManorSessionState state, AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(preferences);
        snapshot = ManorPreviewProtocol.CreateSnapshot(
            state, preferences.Presentation("midnight-manor").AnimationsEnabled);
        Title = "Midnight Manor 2.0 · Read-only preview";
        Width = 1360;
        Height = 870;
        MinWidth = 940;
        MinHeight = 660;
        Background = new SolidColorBrush(Color.FromRgb(14, 12, 21));

        var root = new Grid { Background = Background };
        notice = new TextBlock
        {
            Text = "Loading the new Manor renderer…",
            FontSize = 17,
            Foreground = Brushes.Bisque,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(40),
            Visibility = Visibility.Collapsed,
        };
        root.Children.Add(browser);
        root.Children.Add(notice);
        Content = root;
        browser.Loaded += Browser_Loaded;
        Closed += (_, _) =>
        {
            closed = true;
            browser.Dispose();
        };
    }

    private async void Browser_Loaded(object sender, RoutedEventArgs e)
    {
        if (initialized || closed)
            return;
        initialized = true;
        try
        {
            if (!IsAvailable)
                throw new FileNotFoundException("The ModernFrontend build is missing from the installed package.", EntryPath);

            await browser.EnsureCoreWebView2Async();
            if (closed)
                return;

            CoreWebView2 core = browser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.Settings.IsZoomControlEnabled = false;
            core.SetVirtualHostNameToFolderMapping(
                ManorPreviewProtocol.VirtualHost,
                Path.GetDirectoryName(EntryPath)!,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) =>
            {
                if (!ManorPreviewProtocol.IsTrustedSource(args.Uri))
                    args.Cancel = true;
            };
            core.NavigationCompleted += (_, args) =>
            {
                if (closed || args.IsSuccess) return;
                notice.Text = "Modern Manor navigation failed: " + args.WebErrorStatus + ". The original game is unchanged.";
                notice.Visibility = Visibility.Visible;
            };
            core.WebMessageReceived += OnWebMessageReceived;
            browser.Source = new Uri(ManorPreviewProtocol.EntryPoint);
        }
        catch (Exception ex)
        {
            notice.Text = "The modern preview could not start. The existing Midnight Manor is unchanged.\n\n" + ex.Message;
            notice.Visibility = Visibility.Visible;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (closed || !ManorPreviewProtocol.TryParseCommand(args.Source, args.WebMessageAsJson, out ManorPreviewProtocol.Command? command)
            || command is null)
            return;
        if (command.Type == "ui.close")
        {
            Close();
            return;
        }

        // No wager/result/state mutation is possible through this bridge version.
        browser.CoreWebView2.PostWebMessageAsJson(
            ManorPreviewProtocol.SerializeSnapshot(snapshot, command.RequestId));
    }
}
