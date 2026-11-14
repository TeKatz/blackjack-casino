namespace BlackjackCasino.Core.Hosting;

/// <summary>
/// Host-neutral navigation port. The active desktop registry implements this
/// without exposing WPF controls, and replacement hosts can use the same rules.
/// </summary>
public interface IHostModeNavigator
{
    bool CanOpen(string modeId);
    void Open(string modeId);
    bool CanOpenSecondary(string modeId);
    void OpenSecondary(string modeId);
}

/// <summary>
/// A canonical casino route with its native WebView2 document when applicable.
/// DesktopModeId is the durable historical gameplay identifier; never rename saved IDs.
/// </summary>
public sealed record HostCasinoRoute(
    string DesktopModeId,
    string DisplayName,
    string? PreviewModeId,
    string? PreviewDocument,
    bool HasSecondaryWindow = false,
    bool HasNativeProcess = false)
{
    public bool HasPreviewBridge => PreviewModeId is not null && PreviewDocument is not null;
}

/// <summary>
/// Canonical route inventory shared by Core and the current NativeShell host.
/// Historical property names are retained for save and protocol compatibility.
/// </summary>
public static class HostMigrationCatalog
{
    public const int Version = 1;

    public static IReadOnlyList<HostCasinoRoute> CasinoRoutes { get; } =
        Array.AsReadOnly(new HostCasinoRoute[]
        {
            new("blackjack", "Blackjack", "blackjack", "blackjack.html"),
            new("roulette", "Roulette", "roulette", "roulette.html"),
            new("midnight-reels", "Midnight Reels", "reels", "reels.html"),
            new("neon-vault", "Neon Vault", "neon-vault", "neon-vault.html"),
            new("nine-lives", "Nine Lives", "nine-lives", "nine-lives.html"),
            new("midnight-manor", "Midnight Manor", "midnight-manor", "index.html"),
            new("casino-empire", "Casino Empire", "casino-empire", "empire.html", true),
            new("midnight-merge", "Midnight Merge", "midnight-merge", "midnight-merge.html"),
            new("chill-lounge", "Chill Lounge", "chill-lounge", "lounge.html"),
            new("kniffel", "Midnight Dice", "midnight-dice", "midnight-dice.html"),
            new("midnight-descent", "Midnight Descent", null, null, HasNativeProcess: true),
        });

    public static IReadOnlyList<string> LoungeGameIds { get; } = Array.AsReadOnly(new[]
    {
        "sudoku", "solitaire", "word-search", "memory", "jigsaw",
        "tales-backroom", "tower-defense", "minesweeper",
    });

    public static IReadOnlyList<HostCasinoRoute> PreviewRoutes =>
        CasinoRoutes.Where(route => route.HasPreviewBridge).ToArray();

    public static bool IsPreviewRoute(string? previewModeId) =>
        previewModeId is not null && PreviewRoutes.Any(route =>
            string.Equals(route.PreviewModeId, previewModeId, StringComparison.Ordinal));

    public static string PreviewPageFor(string previewModeId)
    {
        HostCasinoRoute? route = PreviewRoutes.SingleOrDefault(route =>
            string.Equals(route.PreviewModeId, previewModeId, StringComparison.Ordinal));
        return route?.PreviewDocument ??
            throw new ArgumentOutOfRangeException(nameof(previewModeId), "Unknown native preview route.");
    }

}

/// <summary>
/// Prevent the NativeShell state writer from pointing at an older legacy
/// player-state.json, profile.json, or another directory within the live root.
/// A dedicated preview subtree and independently allocated test paths are allowed.
/// This is a path policy, not a replacement for transaction/journal recovery.
/// </summary>
public static class HostPreviewStorage
{
    public const string PreviewFolderName = "NativeShellPreview";

    public static string DefaultDirectory(string localAppData) =>
        Path.Combine(Path.GetFullPath(localAppData), "BlackjackCasino", PreviewFolderName);

    public static string RequireIsolatedDirectory(string candidate, string localAppData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppData);

        string directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        string production = Path.TrimEndingDirectorySeparator(
            Path.Combine(Path.GetFullPath(localAppData), "BlackjackCasino"));
        string preview = Path.Combine(production, PreviewFolderName);
        const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

        static bool IsWithin(string path, string root) =>
            path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        bool isPreview = string.Equals(directory, preview, Comparison)
            || IsWithin(directory, preview);
        bool productionOrChild = string.Equals(directory, production, Comparison)
            || IsWithin(directory, production);
        bool ancestorOfProduction = IsWithin(production, directory);

        if ((productionOrChild && !isPreview) || ancestorOfProduction)
            throw new InvalidOperationException(
                "NativeShell preview cannot use production player storage.");

        return directory;
    }
}
