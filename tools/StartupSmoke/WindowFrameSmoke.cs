using System.Windows;
using BlackjackCasino.Desktop;

internal static class WindowFrameSmoke
{
    internal static async Task Run(MainWindow window)
    {
        WindowState originalState = window.WindowState;
        WindowStyle originalStyle = window.WindowStyle;
        ResizeMode originalResizeMode = window.ResizeMode;
        double originalMinWidth = window.MinWidth;
        double originalMinHeight = window.MinHeight;
        Rect originalBounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
            : window.RestoreBounds;
        Window? activationProbe = null;

        try
        {
            // Hosted Windows runners can expose a 1024px desktop, which is narrower than the app's
            // production MinWidth=1080. Temporarily remove that product constraint so this smoke measures
            // the native maximize/fullscreen frame contract rather than minimum-layout policy.
            window.MinWidth = 0;
            window.MinHeight = 0;

            CasinoWindowFrame.SetIsFullscreen(window, false);
            window.WindowState = WindowState.Normal;
            window.ResizeMode = ResizeMode.CanResize;
            window.WindowState = WindowState.Maximized;
            await Settle(window);

            Rect work = SystemParameters.WorkArea;
            RequireNear(window.ActualWidth, work.Width, 3d,
                "Maximized windowed mode leaves a horizontal native-frame inset instead of filling the work area.");
            RequireNear(window.ActualHeight, work.Height, 3d,
                "Maximized windowed mode leaves a vertical native-frame inset instead of filling the work area.");
            Require(window.Template.FindName("CasinoTitleBar", window) is FrameworkElement { IsVisible: true },
                "Maximized windowed mode unexpectedly hid the custom title bar.");
            Require(CasinoClientWindowFrame.IsNativeBorderSuppressed(window),
                "Main window retained a DWM-owned border that can repaint as bright strips while inactive.");

            activationProbe = new Window
            {
                Width = 240,
                Height = 120,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                ShowActivated = true,
                Title = "Window frame activation probe",
            };
            activationProbe.Show();
            activationProbe.Activate();
            await Settle(window);

            RequireNear(window.ActualWidth, work.Width, 3d,
                "Inactive maximized windowed mode reintroduced a horizontal native-frame inset.");
            RequireNear(window.ActualHeight, work.Height, 3d,
                "Inactive maximized windowed mode reintroduced a vertical native-frame inset.");
            Require(CasinoClientWindowFrame.IsNativeBorderSuppressed(window),
                "Losing activation restored the DWM-owned main-window border.");

            activationProbe.Close();
            activationProbe = null;
            window.Activate();
            await Settle(window);

            CasinoWindowFrame.SetIsFullscreen(window, true);
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowState = WindowState.Maximized;
            await Settle(window);

            RequireNear(window.ActualWidth, SystemParameters.PrimaryScreenWidth, 3d,
                "Fullscreen did not expand to the complete monitor width.");
            RequireNear(window.ActualHeight, SystemParameters.PrimaryScreenHeight, 3d,
                "Fullscreen remained constrained to the taskbar work area instead of using the complete monitor height.");
            Require(window.Template.FindName("CasinoTitleBar", window) is FrameworkElement { IsVisible: false },
                "Fullscreen retained the custom title bar.");
        }
        finally
        {
            activationProbe?.Close();
            window.WindowState = WindowState.Normal;
            CasinoWindowFrame.SetIsFullscreen(window, false);
            window.WindowStyle = originalStyle;
            window.ResizeMode = originalResizeMode;
            window.MinWidth = originalMinWidth;
            window.MinHeight = originalMinHeight;
            if (originalBounds.Width >= originalMinWidth && originalBounds.Height >= originalMinHeight)
            {
                window.Left = originalBounds.Left;
                window.Top = originalBounds.Top;
                window.Width = originalBounds.Width;
                window.Height = originalBounds.Height;
            }
            window.WindowState = originalState;
            await Settle(window);
        }
    }

    private static async Task Settle(Window window)
    {
        await Task.Delay(120);
        window.UpdateLayout();
    }

    private static void RequireNear(double actual, double expected, double tolerance, string message)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException($"{message} Expected {expected:0.##}, actual {actual:0.##}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
