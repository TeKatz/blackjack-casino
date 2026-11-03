using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace BlackjackCasino.Desktop;

/// <summary>Shared native resizable frame. Window content and existing input/lifecycle remain intact.</summary>
internal static class CasinoWindowFrame
{
    public static readonly DependencyProperty IsFullscreenProperty = DependencyProperty.RegisterAttached(
        "IsFullscreen", typeof(bool), typeof(CasinoWindowFrame), new PropertyMetadata(false));
    public static bool GetIsFullscreen(DependencyObject value) => (bool)value.GetValue(IsFullscreenProperty);
    public static void SetIsFullscreen(DependencyObject value, bool fullscreen)
    {
        value.SetValue(IsFullscreenProperty, fullscreen);
        if (value is not Window window || window.Template != Application.Current?.TryFindResource("CasinoWindowTemplate"))
        {
            return;
        }

        // MainWindow uses the lighter client-area frame in both windowed and fullscreen modes.
        // Reintroducing WPF WindowChrome here would bring back the windowed rendering regression.
        WindowChrome.SetWindowChrome(window, window is MainWindow ? null : fullscreen ? null : Chrome());
    }

    private static readonly DependencyProperty AppliedProperty = DependencyProperty.RegisterAttached(
        "Applied", typeof(bool), typeof(CasinoWindowFrame), new PropertyMetadata(false));
    private static readonly BitmapFrame Icon = LoadIcon();

    internal static void Install() => EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
        new RoutedEventHandler(OnLoaded));

    private static BitmapFrame LoadIcon()
    {
        BitmapFrame image = BitmapFrame.Create(new Uri("pack://application:,,,/BlackjackCasino.Desktop;component/Assets/midnight-club.ico"),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        image.Freeze(); return image;
    }

    private static WindowChrome Chrome() => new()
    {
        CaptionHeight = 34, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0),
        CornerRadius = new CornerRadius(0), UseAeroCaptionButtons = false,
    };

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not Window window || !window.IsVisible || (bool)window.GetValue(AppliedProperty)) return;
        window.SetValue(AppliedProperty, true);
        window.Icon ??= Icon;
        // Dialogs with their own bespoke title bars keep their existing chrome.
        if (window.WindowStyle == WindowStyle.None || Application.Current?.TryFindResource("CasinoWindowTemplate") is not ControlTemplate template) return;
        window.WindowStyle = WindowStyle.None;
        WindowChrome.SetWindowChrome(window, window is MainWindow ? null : Chrome());
        window.Template = template;
        window.ApplyTemplate();

        if (window is MainWindow && template.FindName("CasinoTitleBar", window) is FrameworkElement titleBar)
        {
            CasinoClientWindowFrame.Attach(window, titleBar);
        }

        Bind("CasinoMinimize", "Minimize", () => SystemCommands.MinimizeWindow(window), window.ResizeMode != ResizeMode.NoResize);
        Bind("CasinoMaximize", "Maximize / Restore", () =>
        {
            if (window.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
            else SystemCommands.MaximizeWindow(window);
        }, window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip);
        Bind("CasinoClose", "Close", () => SystemCommands.CloseWindow(window), true);

        void Bind(string name, string label, Action action, bool visible)
        {
            if (template.FindName(name, window) is not Button button) return;
            button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (window is not MainWindow)
            {
                WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            }
            AutomationProperties.SetName(button, label);
            button.Click += (_, _) => action();
        }
    }
}
