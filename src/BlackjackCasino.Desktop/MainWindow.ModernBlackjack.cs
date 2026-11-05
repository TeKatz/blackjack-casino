using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernBlackjackView? modernBlackjackView;
    private readonly List<(UIElement Element, bool Enabled)> blackjackCoveredElements = [];
    private bool ModernBlackjackOpen => modernBlackjackView is not null;

    // The lobby always opens the React table when packaged browser assets are
    // present. The native table is retained only as a browser/asset fallback.
    private void OpenBlackjackMode()
    {
        if (ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null) return;
        if (ModernBlackjackView.IsAvailable)
            OpenModernBlackjack();
        else
            ShowBlackjackTable();
    }

    private void OpenModernBlackjack()
    {
        if (ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null || !ModernBlackjackView.IsAvailable) return;
        EnsureEmbeddedGameHost();
        ProcessLatestCompletedRound();
        // Hide the active lobby browser before attaching Blackjack's WebView2.
        // The preview formerly did this indirectly by opening the native table
        // first. Keep both launch paths identical during host initialization.
        HideBlackjackTable();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        modernBlackjackView = new ModernBlackjackView(
            () => BlackjackModernSnapshot.Create(session, selectedBet,
                playMode == PlayModePreference.FullTable, aiSeatCount,
                currentTablePreset.ToString(), SelectedTheme.ToString(),
                progressionController?.Profile.EquippedCardSetId ?? CosmeticCatalog.DefaultCardSetId,
                SoundEnabled, AnimationsEnabled, StatusText.Text,
                Localization.Language == UiLanguagePreference.German ? "de" : "en"),
            ApplyModernBlackjackAction, CloseModernBlackjack, OnModernBlackjackFailed);
        foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>()
            .Where(child => child != embeddedGameOverlay))
        {
            blackjackCoveredElements.Add((covered, covered.IsEnabled));
            covered.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = modernBlackjackView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        KeyboardNavigation.SetTabNavigation(modernBlackjackView, KeyboardNavigationMode.Cycle);
        modeRegistry.NotifyStateChanged();
    }

    private (bool Success, string Message) ApplyModernBlackjackAction(BlackjackModernCommand command)
    {
        if (!ModernBlackjackOpen || modernBlackjackView?.Ready != true)
            return (false, "The modern table is not ready.");
        bool inHand = session.RoundInProgress;
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                    if (inHand) return (false, "Finish your hand before changing the bet.");
                    decimal bet = decimal.Parse(command.Value!, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    if (bet > session.Bankroll || bet > session.Rules.MaximumBet)
                        return (false, "The wager exceeds the available bankroll or limit.");
                    selectedBet = bet;
                    SavePreferences();
                    RefreshGameUi();
                    break;
                case "bet.clear":
                    if (inHand) return (false, "Finish your hand before clearing the bet.");
                    selectedBet = 0m;
                    SavePreferences();
                    RefreshGameUi();
                    break;
                case "deal":
                    if (inHand || selectedBet < session.Rules.MinimumBet
                        || selectedBet > session.Rules.MaximumBet || selectedBet > session.Bankroll)
                        return (false, "Place a valid bet before dealing.");
                    animateNextRender = false;
                    session.StartRound(selectedBet);
                    CasinoFeedback.PlayCard(SoundEnabled);
                    HandleRoundStateAfterMutation("Cards dealt.");
                    break;
                case "hit":
                case "stand":
                case "double":
                case "split":
                case "surrender":
                    PlayerAction action = command.Action switch
                    {
                        "hit" => PlayerAction.Hit,
                        "stand" => PlayerAction.Stand,
                        "double" => PlayerAction.DoubleDown,
                        "split" => PlayerAction.Split,
                        _ => PlayerAction.Surrender,
                    };
                    if (!session.GetAvailableActions().Contains(action))
                        return (false, "This action is not available.");
                    session.Act(action);
                    if (action is PlayerAction.Hit or PlayerAction.DoubleDown or PlayerAction.Split)
                        CasinoFeedback.PlayCard(SoundEnabled);
                    HandleRoundStateAfterMutation(action.ToString());
                    break;
                case "insurance.take":
                    if (session.CurrentRound?.Phase != RoundPhase.InsuranceDecision)
                        return (false, "No insurance decision is pending.");
                    session.TakeInsurance();
                    HandleRoundStateAfterMutation("Insurance taken.");
                    break;
                case "insurance.decline":
                    if (session.CurrentRound?.Phase != RoundPhase.InsuranceDecision)
                        return (false, "No insurance decision is pending.");
                    session.DeclineInsurance();
                    HandleRoundStateAfterMutation("No insurance.");
                    break;
                case "mode.set":
                case "seats.set":
                case "preset.set":
                case "rules.custom.set":
                    if (inHand) return (false, "Finish the current round before changing table settings.");
                    if (command.Action == "mode.set")
                        playMode = command.Value == "full" ? PlayModePreference.FullTable : PlayModePreference.Solo;
                    else if (command.Action == "seats.set")
                        aiSeatCount = int.Parse(command.Value!, CultureInfo.InvariantCulture);
                    else if (command.Action == "rules.custom.set")
                    {
                        if (command.CustomRules is null) return (false, "Invalid custom rules.");
                        customTableRules = command.CustomRules with { };
                        currentTableRules = customTableRules with { };
                        currentTablePreset = TablePresetId.Custom;
                    }
                    else
                    {
                        TablePresetId choice = Enum.Parse<TablePresetId>(command.Value!);
                        currentTablePreset = choice;
                        currentTableRules = choice == TablePresetId.Custom
                            ? customTableRules with { }
                            : TablePresets.Get(choice).Rules with { };
                    }
                    suppressPreferenceEvents = true;
                    try
                    {
                        GameModeComboBox.SelectedIndex = playMode == PlayModePreference.FullTable ? 1 : 0;
                        AiSeatCountComboBox.SelectedIndex = aiSeatCount - 1;
                        if (tablePresetComboBox is not null)
                            tablePresetComboBox.SelectedIndex = PresetIndex(currentTablePreset);
                    }
                    finally { suppressPreferenceEvents = false; }
                    ProcessLatestCompletedRound();
                    RecreateSessionForConfiguredRules(announce: true);
                    break;
                case "theme.set":
                    ThemeComboBox.SelectedIndex = command.Value switch
                    {
                        "MidnightBlue" => 1, "Burgundy" => 2, _ => 0,
                    };
                    break;
                case "sound.set":
                    SoundCheckBox.IsChecked = command.Value == "true";
                    break;
                case "animations.set":
                    AnimationsCheckBox.IsChecked = command.Value == "true";
                    break;
                case "bankroll.reset":
                    if (inHand) return (false, "Finish the current round before resetting bankroll.");
                    session.ResetBankroll();
                    selectedBet = 25m;
                    StatusText.Text = "Bankroll reset. Place a bet to begin.";
                    SavePreferences();
                    RefreshGameUi();
                    break;
                default:
                    return (false, "Unsupported table action.");
            }
            // Browser buttons do not raise WPF's progression click handlers.
            // Commit XP/rewards before the player can switch preset or table mode.
            if (session.CurrentRound?.Phase == RoundPhase.Settled)
                ProcessLatestCompletedRound();
            return (true, StatusText.Text);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            return (false, ex.Message);
        }
    }

    private void OnModernBlackjackFailed()
    {
        // The WebView host notifies us on the next dispatcher turn, outside
        // WebMessageReceived/NavigationCompleted. Preserve the Core hand, and
        // make the native table playable even if the browser fails mid-round.
        if (modernBlackjackView?.Failed == true)
            CloseModernBlackjack();
    }

    private void CloseModernBlackjack()
    {
        if (modernBlackjackView is null) return;
        if (session.RoundInProgress && !modernBlackjackView.Failed)
        {
            StatusText.Text = "Finish your hand before returning to the casino lobby.";
            modernBlackjackView.Refresh();
            return;
        }
        bool failed = modernBlackjackView.Failed;
        modernBlackjackView.Dispose();
        modernBlackjackView = null;
        embeddedGameHost!.Content = null;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach ((UIElement element, bool enabled) in blackjackCoveredElements)
            element.SetCurrentValue(IsEnabledProperty, enabled);
        blackjackCoveredElements.Clear();
        ProcessLatestCompletedRound();
        NormalizeBetForTable();
        SavePreferences();
        if (failed)
            ShowBlackjackTable();
        else if (lobby is not null)
            lobby.Visibility = Visibility.Visible;
        RefreshGameUi();
        ApplyBetLimitState();
        modeRegistry.NotifyStateChanged();
    }
}
