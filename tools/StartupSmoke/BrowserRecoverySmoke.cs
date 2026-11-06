using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;

/// <summary>
/// Exercise the browser-independent fallback without starting any WPF games,
/// mutating player progress or requiring a WebView2 runtime.
/// </summary>
internal static class BrowserRecoverySmoke
{
    internal static void Run()
    {
        LoungeHostedGameRegistry.ClearSessionModules();
        var root = new Grid();
        var nativeButton = new Button();
        var host = new ContentControl();
        var overlay = new Grid { Visibility = Visibility.Collapsed };
        overlay.Children.Add(host);
        root.Children.Add(nativeButton);
        root.Children.Add(overlay);
        var surface = new ModernGameSurface(root, overlay, host);

        int choices = 0;
        BrowserRecoveryAction? chosen = null;
        var recovery = new BrowserRecoveryView(false, action =>
        {
            choices++;
            chosen = action;
        });

        surface.Mount(recovery);
        Require(!nativeButton.IsEnabled && !recovery.RetryButton.IsEnabled
                && recovery.LegacyButton.IsEnabled && recovery.LobbyButton.IsEnabled,
            "Browser recovery did not protect native input or present safe choices.");
        Require(LoungeHostedGameRegistry.Snapshot.Count == 0,
            "Recovery screen must not construct legacy game factories.");

        recovery.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 0, "Missing assets allowed an invalid browser retry.");

        recovery.LobbyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        recovery.LegacyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 1 && chosen == BrowserRecoveryAction.ReturnToLobby,
            "Recovery selected multiple actions or lost its lobby escape.");

        surface.Unmount(recovery);
        Require(nativeButton.IsEnabled && !surface.IsOccupied && host.Content is null,
            "Recovery dismissal left native input or overlay in a broken state.");

        var retryable = new BrowserRecoveryView(true, action =>
        {
            choices++;
            chosen = action;
        });
        surface.Mount(retryable);
        retryable.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 2 && chosen == BrowserRecoveryAction.RetryModern,
            "Available browser assets did not offer an explicit retry.");
        surface.Unmount(retryable);

        // New React-only destinations must never manufacture a native game
        // fallback when no compatible WPF implementation exists.
        var modernOnly = new BrowserRecoveryView("Nine Lives", true, false, action =>
        {
            choices++;
            chosen = action;
        });
        surface.Mount(modernOnly);
        Require(modernOnly.RetryButton.IsEnabled && modernOnly.LobbyButton.IsEnabled
                && !modernOnly.LegacyButton.IsEnabled
                && modernOnly.LegacyButton.Visibility == Visibility.Collapsed,
            "Modern-only recovery exposed an unavailable old WPF game.");
        modernOnly.LegacyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 2, "Disabled legacy action was processed by modern-only recovery.");
        modernOnly.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 3 && chosen == BrowserRecoveryAction.RetryModern,
            "Modern-only game lost its explicit browser retry.");
        surface.Unmount(modernOnly);
        Require(nativeButton.IsEnabled && !surface.IsOccupied,
            "Modern-only recovery left host input disabled.");

        var assetsMissing = new BrowserRecoveryView("Blackjack", false, false, action =>
        {
            choices++;
            chosen = action;
        });
        surface.Mount(assetsMissing);
        assetsMissing.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        assetsMissing.LegacyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 3, "Missing-assets recovery accepted an unavailable game route.");
        assetsMissing.LobbyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(choices == 4 && chosen == BrowserRecoveryAction.ReturnToLobby,
            "Missing-assets recovery did not permit returning to the casino lobby.");
        surface.Unmount(assetsMissing);
        Require(LoungeHostedGameRegistry.Snapshot.Count == 0,
            "Generic recovery unexpectedly created WPF Lounge factories.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
