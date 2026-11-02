using System.Windows;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Bridges the hosted Adventure control to the Chill Lounge's single music owner. The route is installed
/// once and only announces mode changes; MainWindow remains responsible for creating and disposing music.
/// </summary>
internal static class AdventureMusicRouter
{
    private static Action<bool>? switchMode;
    private static bool installed;
    private static bool adventureActive;

    internal static void Attach(Action<bool> switcher)
    {
        switchMode = switcher ?? throw new ArgumentNullException(nameof(switcher));
        EnsureInstalled();
    }

    internal static void Detach()
    {
        switchMode = null;
        adventureActive = false;
    }

    private static void EnsureInstalled()
    {
        if (installed) return;
        installed = true;

        EventManager.RegisterClassHandler(
            typeof(AdventureHubControl),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((_, _) => SetAdventureActive(true)),
            handledEventsToo: true);

        EventManager.RegisterClassHandler(
            typeof(AdventureHubControl),
            FrameworkElement.UnloadedEvent,
            new RoutedEventHandler((_, _) => SetAdventureActive(false)),
            handledEventsToo: true);
    }

    private static void SetAdventureActive(bool active)
    {
        if (adventureActive == active) return;
        adventureActive = active;
        switchMode?.Invoke(active);
    }
}
