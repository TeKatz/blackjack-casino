using System.Windows;
using BlackjackCasino.Desktop;

internal static class CasinoWindowGeometrySmoke
{
    internal static void Run(MainWindow window)
    {
        var geometry = new CasinoWindowGeometry(window);
        WindowState originalState = window.WindowState;
        WindowStyle originalStyle = window.WindowStyle;
        ResizeMode originalResize = window.ResizeMode;
        CasinoWindowPlacement originalPlacement = geometry.GetSavedPlacement();

        try
        {
            geometry.ToggleFullscreen();
            Require(geometry.IsFullscreen && window.WindowState == WindowState.Maximized
                && window.WindowStyle == WindowStyle.None && window.ResizeMode == ResizeMode.NoResize,
                "Extracted frame adapter failed to enter fullscreen.");

            CasinoWindowPlacement duringFullscreen = geometry.GetSavedPlacement();
            Require(duringFullscreen.Maximized == originalPlacement.Maximized
                && duringFullscreen.Bounds == originalPlacement.Bounds,
                "Fullscreen overwrote the original window placement for persisted preferences.");
        }
        finally
        {
            if (geometry.IsFullscreen) geometry.ToggleFullscreen();
        }

        Require(!geometry.IsFullscreen && window.WindowState == originalState
            && window.WindowStyle == originalStyle && window.ResizeMode == originalResize,
            "Extracted frame adapter failed to restore window mode.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
