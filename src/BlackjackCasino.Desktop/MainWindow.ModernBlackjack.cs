using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernBlackjackView? modernBlackjackView;
    private bool ModernBlackjackOpen => modernBlackjackView is not null;

    private void OpenBlackjackMode()
    {
        if (ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null) return;
        if (!ModernBlackjackView.IsAvailable)
        {
            ShowBrowserRecovery("Blackjack", assetsAvailable: false, OpenBlackjackMode);
            return;
        }
        OpenModernBlackjack();
    }

    private void OpenModernBlackjack()
    {
        if (ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null || !ModernBlackjackView.IsAvailable) return;
        EnsureEmbeddedGameHost();
        ProcessLatestCompletedRound();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        modernBlackjackView = new ModernBlackjackView(
            () => BlackjackModernSnapshot.Create(session, selectedBet,
                playMode == PlayModePreference.FullTable, aiSeatCount,
                currentTablePreset.ToString(), SelectedTheme.ToString(),
                progressionController?.Profile.EquippedCardSetId ?? CosmeticCatalog.DefaultCardSetId,
                SoundEnabled, AnimationsEnabled, StatusText.Text,
                Localization.Language == UiLanguagePreference.German ? "de" : "en"),
            ApplyModernBlackjackAction, CloseModernBlackjack, OnModernBlackjackFailed);
        MountModernSurface(modernBlackjackView);
        modeRegistry.NotifyStateChanged();
    }

    private (bool Success, string Message) ApplyModernBlackjackAction(BlackjackModernCommand command)
    {
        if (!ModernBlackjackOpen || modernBlackjackView?.Ready != true)
            return (false, "The modern table is not ready.");

        // Flush the preceding settled round before a configuration change can
        // replace its C# session, including any earned progression.
        if (command.Action is "mode.set" or "seats.set" or "preset.set" or "rules.custom.set")
            ProcessLatestCompletedRound();

        BlackjackTableActionResult result = blackjack.Apply(command);
        if (!result.Success) return (false, result.Message);

        if (result.SettingsChanged)
        {
            preferences = preferences with
            {
                Theme = blackjack.Theme,
                SoundEnabled = blackjack.SoundEnabled,
                AnimationsEnabled = blackjack.AnimationsEnabled,
            };
            ApplyTheme(blackjack.Theme);
            if (!blackjack.SoundEnabled) CasinoFeedback.Stop();
        }

        if (result.TableChanged)
        {
            preferences = preferences with
            {
                TablePreset = blackjack.CurrentPreset,
                CustomRules = blackjack.CustomRules with { },
            };
        }

        // The modern React table owns its soft, gesture-unlocked Web Audio cues.
        // Replaying the legacy WPF card effect here would produce doubled sounds.
        // Non-modern game surfaces retain their existing native feedback.
        if (result.RoundChanged)
            HandleRoundStateAfterMutation(result.Message);
        else
        {
            StatusText.Text = result.Message;
            SavePreferences();
            RefreshGameUi();
        }

        if (session.CurrentRound?.Phase == RoundPhase.Settled)
            ProcessLatestCompletedRound();
        return (true, StatusText.Text);
    }

    private void OnModernBlackjackFailed()
    {
        // Preserve the Core session and the current hand. A failure never
        // silently replaces React with a second, obsolete game implementation.
        if (modernBlackjackView?.Failed != true) return;
        CloseModernBlackjack();
        if (IsLoaded)
            ShowBrowserRecovery("Blackjack", ModernBlackjackView.IsAvailable, OpenBlackjackMode);
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
        UnmountModernSurface(modernBlackjackView);
        modernBlackjackView = null;
        ProcessLatestCompletedRound();
        NormalizeBetForTable();
        SavePreferences();
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        if (failed) StatusText.Text = "Blackjack UI interrupted; reopen to resume the current hand.";
        RefreshGameUi();
        ApplyBetLimitState();
        modeRegistry.NotifyStateChanged();
    }
}
