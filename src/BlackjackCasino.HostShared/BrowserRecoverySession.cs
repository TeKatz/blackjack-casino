namespace BlackjackCasino.HostShared;

/// <summary>
/// Browser-independent recovery choice. No game registration, WPF view, save
/// mutation, browser state or wallet operation is performed by this policy.
/// </summary>
internal enum BrowserRecoveryAction
{
    RetryModern,
    OpenLegacyGames,
    ReturnToLobby,
}

internal sealed class BrowserRecoverySession
{
    internal BrowserRecoverySession(bool modernAssetsAvailable, bool legacyGamesAvailable = true)
    {
        ModernAssetsAvailable = modernAssetsAvailable;
        LegacyGamesAvailable = legacyGamesAvailable;
    }

    internal bool ModernAssetsAvailable { get; }
    internal bool LegacyGamesAvailable { get; }
    internal bool Completed { get; private set; }

    /// <summary>Consume a single explicit choice; never silently open legacy games.</summary>
    internal bool TryChoose(BrowserRecoveryAction action)
    {
        if (Completed || action is not (
            BrowserRecoveryAction.RetryModern or
            BrowserRecoveryAction.OpenLegacyGames or
            BrowserRecoveryAction.ReturnToLobby))
            return false;
        if (action == BrowserRecoveryAction.RetryModern && !ModernAssetsAvailable)
            return false;
        if (action == BrowserRecoveryAction.OpenLegacyGames && !LegacyGamesAvailable)
            return false;
        Completed = true;
        return true;
    }
}
