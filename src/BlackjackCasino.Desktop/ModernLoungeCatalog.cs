namespace BlackjackCasino.Desktop;

/// <summary>
/// Stable React Lounge manifest. No WPF factories or game views may be used
/// to build the normal browser snapshot; those belong to the emergency path.
/// </summary>
internal static class ModernLoungeCatalog
{
    internal const int SnapshotVersion = 1;

    internal static IReadOnlyList<string> GameIds { get; } =
    [
        "sudoku",
        "solitaire",
        "word-search",
        "memory",
        "jigsaw",
        "tales-backroom",
    ];

    internal static IReadOnlyList<ModernLoungeGame> Create(Func<string, string?> status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return GameIds.Select(id => new ModernLoungeGame(id, status(id))).ToArray();
    }

    internal static bool Contains(string? id) =>
        id is not null && GameIds.Contains(id, StringComparer.Ordinal);
}

internal sealed record ModernLoungeGame(string Id, string? Status);
