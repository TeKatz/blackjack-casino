using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private bool tableRulesInitialized;
    private GameRules currentTableRules => blackjack.CurrentRules;
    private GameRules customTableRules => blackjack.CustomRules;
    private TablePresetId currentTablePreset => blackjack.CurrentPreset;

    private void InitializeTableRulesFeature()
    {
        // Rules, selected preset and bankroll were already restored into the
        // WPF-independent controller in the MainWindow constructor.
        tableRulesInitialized = true;
    }

    private void RecreateSessionForConfiguredRules(bool announce)
    {
        blackjack.RecreateConfiguredSession();
        preferences = preferences with
        {
            TablePreset = blackjack.CurrentPreset,
            CustomRules = blackjack.CustomRules with { },
        };
        if (announce)
        {
            string name = blackjack.CurrentPreset == TablePresetId.Custom
                ? Localization.T("Shell.Table.Custom")
                : Localization.T($"Shell.Table.Preset.{blackjack.CurrentPreset}.Name");
            StatusText.Text = Localization.F("Shell.Table.Ready", name);
        }
        SavePreferences();
        RefreshGameUi();
    }

    private void NormalizeBetForTable() => blackjack.NormalizeBet();

    private void ApplyBetLimitState()
    {
        if (session.RoundInProgress) return;
        decimal previous = selectedBet;
        blackjack.NormalizeBet();
        if (previous != selectedBet) SavePreferences();
    }

    private void UpdateTableRulePresentation()
    {
        // Rule labels are projected directly from the C# model into React.
        // No WPF table or hidden rule controls remain.
    }
}
