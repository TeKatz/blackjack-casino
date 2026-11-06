using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;

/// <summary>
/// Exercises the shared React presentation boundary in the Windows STA process,
/// independently of WebView2 game assets, wallets and saved player sessions.
/// </summary>
internal static class ModernGameSurfaceSmoke
{
    internal static void Run()
    {
        var root = new Grid();
        var normal = new Button { IsEnabled = true };
        var protectedButton = new Button { IsEnabled = false };
        var host = new ContentControl();
        var overlay = new Grid { Visibility = Visibility.Collapsed };
        overlay.Children.Add(host);
        root.Children.Add(normal);
        root.Children.Add(protectedButton);
        root.Children.Add(overlay);

        var surface = new ModernGameSurface(root, overlay, host);
        var page = new Grid();
        Require(!surface.IsOccupied, "Fresh desktop surface must not claim a page.");
        surface.Mount(page);
        Require(surface.IsOccupied && ReferenceEquals(host.Content, page)
            && overlay.Visibility == Visibility.Visible,
            "React game page did not become the exclusive foreground surface.");
        Require(!normal.IsEnabled && !protectedButton.IsEnabled,
            "WPF controls remained interactive behind a modern game.");

        // A second game may not replace an active React view; a mismatched
        // close must not unmount it or release the disabled native controls.
        var secondPage = new Grid();
        Throws(() => surface.Mount(secondPage), "Overlapping React pages were accepted.");
        Throws(() => surface.Unmount(secondPage), "A different page was allowed to close the active game.");
        Require(surface.IsOccupied && ReferenceEquals(host.Content, page)
            && !normal.IsEnabled, "Overlapping game attempt corrupted the active surface.");

        surface.Unmount(page);
        Require(!surface.IsOccupied && host.Content is null
            && overlay.Visibility == Visibility.Collapsed,
            "React view teardown left the old page or overlay visible.");
        Require(normal.IsEnabled && !protectedButton.IsEnabled,
            "The host failed to restore exactly the previous WPF enabled states.");

        // The emergency native host uses the same ContentControl. Its active
        // content must block React attachment without disturbing player input.
        host.Content = new Border();
        Throws(() => surface.Mount(page), "React replaced an active native fallback page.");
        Require(normal.IsEnabled && !surface.IsOccupied && host.Content is Border,
            "A rejected React mount modified the emergency native fallback.");
        host.Content = null;

        // Reopening after a failure/close must not accumulate stale coverage.
        surface.Mount(secondPage);
        surface.Unmount(secondPage);
        Require(normal.IsEnabled && !protectedButton.IsEnabled && !surface.IsOccupied,
            "A second React opening did not restore original native input states.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }
}
