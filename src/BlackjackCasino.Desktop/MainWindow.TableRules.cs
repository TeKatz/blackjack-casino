namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private BlackjackCasino.Core.TablePresetId currentTablePreset => blackjack.CurrentPreset;

    private void NormalizeBetForTable() => blackjack.NormalizeBet();

    private void ApplyBetLimitState()
    {
        if (session.RoundInProgress) return;
        decimal previous = selectedBet;
        blackjack.NormalizeBet();
        if (previous != selectedBet) SavePreferences();
    }
}
