namespace BlackjackCasino.Desktop;

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
    internal BrowserRecoverySession(bool modernAssetsAvailable)
        => ModernAssetsAvailable = modernAssetsAvailable;

    internal bool ModernAssetsAvailable { get; }
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
        Completed = true;
        return true;
    }
}
