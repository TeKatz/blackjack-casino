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
/// A canonical desktop route and its currently available NativeShell preview bridge.
/// A preview bridge is NOT evidence of production parity or release approval.
/// </summary>
public sealed record HostCasinoRoute(
    string DesktopModeId,
    string DisplayName,
    string? PreviewModeId,
    string? PreviewDocument,
    bool HasSecondaryWindow = false)
{
    public bool HasPreviewBridge => PreviewModeId is not null && PreviewDocument is not null;
}

public sealed record HostReleaseRequirement(string Id, string Acceptance, bool Approved);

/// <summary>
/// Phase A inventory. IDs match the real MainWindow lobby, while the preview
/// aliases match NativeShellForm. No UI framework types or mutable delegates.
/// </summary>
public static class HostMigrationCatalog
{
    public const int Version = 1;

    public static IReadOnlyList<HostCasinoRoute> CasinoRoutes { get; } =
        Array.AsReadOnly(new HostCasinoRoute[]
        {
            new("blackjack", "Blackjack", "blackjack", "blackjack.html"),
            new("roulette", "Roulette", null, null),
            new("midnight-reels", "Midnight Reels", "reels", "reels.html"),
            new("neon-vault", "Neon Vault", "neon-vault", "neon-vault.html"),
            new("nine-lives", "Nine Lives", "nine-lives", "nine-lives.html"),
            new("midnight-manor", "Midnight Manor", "midnight-manor", "index.html"),
            new("casino-empire", "Casino Empire", null, null, true),
            new("midnight-merge", "Midnight Merge", "midnight-merge", "midnight-merge.html"),
            new("chill-lounge", "Chill Lounge", null, null),
            new("kniffel", "Midnight Dice", "midnight-dice", "midnight-dice.html"),
            new("midnight-descent", "Midnight Descent", null, null),
        });

    public static IReadOnlyList<string> LoungeGameIds { get; } = Array.AsReadOnly(new[]
    {
        "sudoku", "solitaire", "word-search", "memory", "jigsaw",
        "tales-backroom", "tower-defense", "minesweeper",
    });

    // All approvals start false: a successful preview smoke is not a release
    // sign-off. Each requires the acceptance evidence described below.
    public static IReadOnlyList<HostReleaseRequirement> ReleaseRequirements { get; } =
        Array.AsReadOnly(new HostReleaseRequirement[]
        {
            new("full-lobby", "All lobby actions and their authoritative C# bridges", false),
            new("all-game-routes", "Every casino mode, Lounge game, and detached window", false),
            new("user-data", "Production save import, wallet/history integrity, single writer", false),
            new("receipts", "Replay, crash, restart, and interrupted-commit tests", false),
            new("browser-recovery", "Missing assets/runtime, navigation and renderer crashes", false),
            new("system-services", "Audio, updates, window/focus/DPI, external Godot lifecycle", false),
            new("windows-acceptance", "Windows 10/11 and multi-monitor/DPI/device coverage", false),
            new("release-rollback", "Verified package upgrade and old-version rollback", false),
            new("test-parity", "Native tests replace, rather than disable, legacy WPF smokes", false),
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

    public static bool ApprovedForRelease =>
        ReleaseRequirements.Count != 0 && ReleaseRequirements.All(requirement => requirement.Approved);
}

/// <summary>
/// Prevent the pilot's experimental state writer from pointing at production
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
