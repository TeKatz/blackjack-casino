using System.Windows;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private SlotProgression? slotProgression;

    private void Slots_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before visiting the slot machine.";
            return;
        }
        if (progressionController is null)
        {
            return;
        }

        // Flush a just-completed keyboard/button round before transferring the wallet.
        ProcessLatestCompletedRound();
        slotProgression ??= new SlotProgression(progressionController.Service);
        bool levelUp = false;
        AreaPresentationSettings presentation = preferences.Presentation("midnight-reels");
        var window = new SlotMachineWindow(session.Bankroll, preferences.SlotBet, presentation.SoundEnabled, presentation.AnimationsEnabled,
            progressionController, spin =>
            {
                // Existing public wallet operation, permitted only between blackjack hands.
                // Retains the blackjack shoe/history; no blackjack engine changes are needed.
                session.ResetBankroll(spin.BankrollAfter);
                preferences = preferences with { SlotBet = spin.Bet };
                selectedBet = Math.Min(selectedBet, session.Bankroll);
                SavePreferences();
                ProgressionAward award = slotProgression.Process(spin);
                progressionController.SaveAll();
                levelUp |= award.HasLevelUp;
                return award;
            }, musicEnabled: presentation.MusicEnabled, musicVolume: presentation.MusicVolume) { Owner = this };
        backgroundMusic.SetEnabled(false);
        try { window.ShowDialog(); }
        finally { backgroundMusic.SetEnabled(HostMusicSettings.MusicEnabled); }

        preferences = preferences with { SlotBet = window.SelectedBet };
        preferences = preferences.WithPresentation("midnight-reels", presentation with { SoundEnabled = window.SoundsEnabled, AnimationsEnabled = window.AnimationsEnabled });
        NormalizeBetForTable();
        SavePreferences();
        RefreshGameUi();
        ApplyBetLimitState();
        if (session.CurrentRound is null)
        {
            StatusText.Text = "Welcome back. Place a bet to play Blackjack.";
        }
        RefreshProfileButton();
        if (levelUp)
        {
            AnimateLevelUpCue();
        }
    }
}
