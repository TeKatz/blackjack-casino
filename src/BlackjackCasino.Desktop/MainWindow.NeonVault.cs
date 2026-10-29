using System.Windows;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void EmbeddedNeonVault_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before entering Neon Vault.";
            return;
        }
        if (progressionController is null)
        {
            return;
        }

        ProcessLatestCompletedRound();
        slotProgression ??= new SlotProgression(progressionController.Service);
        bool levelUp = false;
        var window = new NeonVaultWindow(
            session.Bankroll,
            preferences.NeonVaultBet,
            SoundEnabled,
            AnimationsEnabled,
            progressionController,
            spin =>
            {
                session.ResetBankroll(spin.BankrollAfter);
                preferences = preferences with { NeonVaultBet = spin.Bet };
                selectedBet = Math.Min(selectedBet, session.Bankroll);
                SavePreferences();
                ProgressionAward award = slotProgression.Process(spin);
                progressionController.SaveAll();
                levelUp |= award.HasLevelUp;
                return award;
            },
            musicEnabled: preferences.MusicEnabled,
            musicVolume: preferences.MusicVolume);

        ShowEmbeddedGame(window, "Neon Vault · Blackjack Casino", muteHostMusic: true, closed: () =>
        {
            preferences = preferences with { NeonVaultBet = window.SelectedBet };
            suppressPreferenceEvents = true;
            SoundCheckBox.IsChecked = window.SoundsEnabled;
            AnimationsCheckBox.IsChecked = window.AnimationsEnabled;
            suppressPreferenceEvents = false;
            if (session.CurrentRound is null)
            {
                StatusText.Text = "Welcome back. Place a bet to play Blackjack.";
            }
            if (levelUp)
            {
                Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
            }
        });
    }
}
