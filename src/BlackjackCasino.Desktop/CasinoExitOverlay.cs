using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Pure WebView2 transport for the React exit confirmation. WPF owns only the
/// existing desktop window and native close cancellation; it draws no dialog.
/// </summary>
internal sealed class CasinoExitOverlay : Grid, IDisposable
{
    private const string EntryFile = "exit.html";
    private static readonly string EntryUri =
        "https://" + ManorPreviewProtocol.VirtualHost + "/" + EntryFile;
    private readonly WebView2 browser = new()
    {
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 9, 17, 24)
    };
    private readonly Action confirm;
    private readonly Action cancel;
    private bool ready, disposed, requested, german, loading;

    internal static bool IsAvailable => File.Exists(Path.Combine(
        AppContext.BaseDirectory, "ModernFrontend", EntryFile));

    internal CasinoExitOverlay(Action confirm, Action cancel)
    {
        this.confirm = confirm;
        this.cancel = cancel;
        Name = "CasinoReactExitOverlay";
        // An invisible WPF Opacity=0 ancestor does not reliably hide WebView2's
        // separate native rendering surface. Collapsing the entire control does.
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
        Children.Add(browser);
        Loaded += OnLoaded;
    }

    internal void Show(bool useGerman)
    {
        if (disposed) return;
        requested = true;
        german = useGerman;
        // Make the browser visible only after an explicit exit request. This
        // also allows the lazily mounted WebView2 to initialize on first use.
        Visibility = Visibility.Visible;
        if (ready) Reveal();
    }

    internal void Hide()
    {
        requested = false;
        // Opacity alone leaves an opaque WebView2 HWND on some Windows devices.
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
    }

    private void Reveal()
    {
        if (!ready || !requested || disposed || browser.CoreWebView2 is null) return;
        Visibility = Visibility.Visible;
        IsHitTestVisible = true;
        browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
        {
            type = "casino.exit.show",
            language = german ? "de" : "en"
        }));
        browser.Focus();
    }

    private static bool Trusted(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals(ManorPreviewProtocol.VirtualHost, StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort && uri.AbsolutePath == "/" + EntryFile
            && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (loading || disposed) return;
        loading = true;
        if (!IsAvailable) { cancel(); return; }
        try
        {
            CoreWebView2? core = await ModernWebViewBootstrap.InitializeAsync(
                browser, () => disposed,
                Path.Combine(AppContext.BaseDirectory, "ModernFrontend"), Trusted);
            if (core is null) return;
            core.WebMessageReceived += OnMessage;
            core.ProcessFailed += (_, _) => Unavailable();
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess) Unavailable();
            };
            browser.Source = new Uri(EntryUri);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Unavailable();
        }
    }

    private void Unavailable()
    {
        if (disposed) return;
        Hide();
        cancel();
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || !Trusted(e.Source) || e.WebMessageAsJson.Length > 512) return;
        try
        {
            using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "casino.exit.ready":
                    ready = true;
                    Reveal();
                    break;
                case "casino.exit.confirm" when ready && requested:
                    Hide();
                    confirm();
                    break;
                case "casino.exit.cancel" when ready && requested:
                    Hide();
                    cancel();
                    break;
            }
        }
        catch (JsonException) { }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Loaded -= OnLoaded;
        browser.Dispose();
    }
}
