using System.Windows;

namespace BlackjackCasino.Desktop;

internal sealed record LoungeGame(
    string Id,
    string Title,
    string Subtitle,
    string Description,
    Action Open,
    Func<FrameworkElement>? Artwork = null,
    int SortOrder = 100,
    Func<string?>? Status = null);

/// <summary>
/// Normalizes and validates Lounge registrations so the menu can stay data-driven as the
/// collection grows. A duplicate or blank game id is a developer error and fails fast.
/// </summary>
internal static class LoungeGameCatalog
{
    public static IReadOnlyList<LoungeGame> Normalize(IEnumerable<LoungeGame> games)
    {
        ArgumentNullException.ThrowIfNull(games);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<LoungeGame>();
        foreach (LoungeGame game in games)
        {
            ArgumentNullException.ThrowIfNull(game);
            if (string.IsNullOrWhiteSpace(game.Id))
            {
                throw new ArgumentException("A Chill Lounge game id is required.", nameof(games));
            }
            if (string.IsNullOrWhiteSpace(game.Title))
            {
                throw new ArgumentException($"Chill Lounge game '{game.Id}' requires a title.", nameof(games));
            }
            if (!seen.Add(game.Id))
            {
                throw new InvalidOperationException($"Duplicate Chill Lounge game id '{game.Id}'.");
            }

            normalized.Add(game);
        }

        return normalized
            .OrderBy(game => game.SortOrder)
            .ThenBy(game => game.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
