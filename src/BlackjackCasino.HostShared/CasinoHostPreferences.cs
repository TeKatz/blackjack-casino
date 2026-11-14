using BlackjackCasino.Core.Empire;
using BlackjackCasino.Update;

namespace BlackjackCasino.HostShared;

/// <summary>
/// WPF-independent projection of current native host state into the existing save
/// format. Callers decide when to persist; this does not write to disk.
/// </summary>
internal static class CasinoHostPreferences
{
    internal static AppPreferences Capture(
        AppPreferences preferences,
        BlackjackTableController blackjack,
        EmpireState? empire,
        UpdateChannel updateChannel,
        double width,
        double height,
        bool maximized)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(blackjack);

        return preferences with
        {
            Empire = empire ?? preferences.Empire,
            Bankroll = blackjack.Session.Bankroll,
            SelectedBet = Math.Min(blackjack.SelectedBet, blackjack.Session.Bankroll),
            PlayMode = blackjack.PlayMode,
            AiSeatCount = blackjack.AiSeatCount,
            Theme = blackjack.Theme,
            SoundEnabled = blackjack.SoundEnabled,
            AnimationsEnabled = blackjack.AnimationsEnabled,
            ShowEmpireBalance = preferences.ShowEmpireBalance,
            UpdateChannel = updateChannel,
            WindowWidth = width,
            WindowHeight = height,
            WindowMaximized = maximized,
        };
    }
}
