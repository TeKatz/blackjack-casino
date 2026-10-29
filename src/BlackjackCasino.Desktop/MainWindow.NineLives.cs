using System.Windows;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void EmbeddedNineLives_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before entering Nine Lives.";
            return;
        }
        if (progressionController is null)
        {
            return;
        }

        ProcessLatestCompletedRound();
        slotProgression ??= new SlotProgression(progressionController.Service);
        bool levelUp = false;
        var window = new NineLivesWindow(
            session.Bankroll,
            preferences.NineLivesBet,
            SoundEnabled,
            AnimationsEnabled,
            progressionController,
            spin =>
            {
                session.ResetBankroll(spin.BankrollAfter);
                preferences = preferences with { NineLivesBet = spin.Bet };
                selectedBet = Math.Min(selectedBet, session.Bankroll);
                SavePreferences();
                ProgressionAward award = slotProgression.Process(spin);
                progressionController.SaveAll();
                levelUp |= award.HasLevelUp;
                return award;
            },
            musicEnabled: preferences.MusicEnabled,
            musicVolume: preferences.MusicVolume);

        ShowEmbeddedGame(window, "Nine Lives · Blackjack Casino", muteHostMusic: true, closed: () =>
        {
            preferences = preferences with
            {
                NineLivesBet = window.SelectedBet,
                SoundEnabled = window.SoundsEnabled,
                AnimationsEnabled = window.AnimationsEnabled,
            };
            suppressPreferenceEvents = true;
            SoundCheckBox.IsChecked = window.SoundsEnabled;
            AnimationsCheckBox.IsChecked = window.AnimationsEnabled;
            suppressPreferenceEvents = false;
            SavePreferences();
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
