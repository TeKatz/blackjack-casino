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
        AreaPresentationSettings presentation = preferences.Presentation("neon-vault");
        var window = new NeonVaultWindow(
            session.Bankroll,
            preferences.NeonVaultBet,
            presentation.SoundEnabled,
            presentation.AnimationsEnabled,
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
            musicEnabled: presentation.MusicEnabled,
            musicVolume: presentation.MusicVolume);

        ShowEmbeddedGame(window, "Neon Vault · Blackjack Casino", muteHostMusic: true, closed: () =>
        {
            preferences = preferences with { NeonVaultBet = window.SelectedBet };
            preferences = preferences.WithPresentation("neon-vault", presentation with { SoundEnabled = window.SoundsEnabled, AnimationsEnabled = window.AnimationsEnabled });
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
