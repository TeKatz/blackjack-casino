using System.Windows;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Interim WPF frame adapter. Centralizes fullscreen and persisted window placement so
/// MainWindow no longer owns window-chrome transition state. Replace this adapter with
/// the native shell when the remaining emergency WPF surfaces are retired.
/// </summary>
internal sealed class CasinoWindowGeometry
{
    private readonly Window window;
    private bool isFullscreen;
    private WindowState previousWindowState;
    private WindowStyle previousWindowStyle;
    private ResizeMode previousResizeMode;
    private Rect previousWindowBounds;

    internal CasinoWindowGeometry(Window window)
        => this.window = window ?? throw new ArgumentNullException(nameof(window));

    internal bool IsFullscreen => isFullscreen;

    internal void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            previousWindowState = window.WindowState;
            previousWindowStyle = window.WindowStyle;
            previousResizeMode = window.ResizeMode;
            previousWindowBounds = window.WindowState == WindowState.Normal
                ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
                : window.RestoreBounds;

            CasinoWindowFrame.SetIsFullscreen(window, true);
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowState = WindowState.Maximized;
            isFullscreen = true;
            return;
        }

        window.WindowState = WindowState.Normal;
        CasinoWindowFrame.SetIsFullscreen(window, false);
        window.WindowStyle = previousWindowStyle;
        window.ResizeMode = previousResizeMode;
        if (previousWindowBounds.Width >= window.MinWidth && previousWindowBounds.Height >= window.MinHeight)
        {
            window.Left = previousWindowBounds.Left;
            window.Top = previousWindowBounds.Top;
            window.Width = previousWindowBounds.Width;
            window.Height = previousWindowBounds.Height;
        }
        window.WindowState = previousWindowState;
        isFullscreen = false;
    }

    internal CasinoWindowPlacement GetSavedPlacement()
    {
        Rect bounds = isFullscreen
            ? previousWindowBounds
            : window.WindowState == WindowState.Normal
                ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
                : window.RestoreBounds;
        bool maximized = isFullscreen
            ? previousWindowState == WindowState.Maximized
            : window.WindowState == WindowState.Maximized;
        return new CasinoWindowPlacement(bounds, maximized);
    }
}

internal readonly record struct CasinoWindowPlacement(Rect Bounds, bool Maximized);
