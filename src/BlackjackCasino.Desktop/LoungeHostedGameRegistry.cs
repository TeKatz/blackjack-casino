using System.Windows;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Session-scoped extension point for Lounge games that own their own hub/view lifecycle.
/// The Lounge shell only receives descriptors and factories; it never knows a game's save format.
/// </summary>
internal sealed record LoungeHostedGame(LoungeGame Definition, Func<Action, FrameworkElement> CreateContent);

internal static class LoungeHostedGameRegistry
{
    private static IReadOnlyList<LoungeHostedGame> modules = [];

    internal static IReadOnlyList<LoungeHostedGame> Snapshot => modules;

    internal static void SetSessionModules(IEnumerable<LoungeHostedGame> hostedGames)
    {
        ArgumentNullException.ThrowIfNull(hostedGames);
        LoungeHostedGame[] next = hostedGames.ToArray();
        _ = LoungeGameCatalog.Normalize(next.Select(m => m.Definition));
        modules = next;
    }

    internal static void ClearSessionModules() => modules = [];
}
