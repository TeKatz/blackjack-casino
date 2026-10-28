using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void Manor_Click(object sender,RoutedEventArgs e)
    {
        if(session.RoundInProgress) { StatusText.Text="Finish the current Blackjack hand before entering Midnight Manor."; return; }
        if(progressionController is null) return;
        ProcessLatestCompletedRound();
        try
        {
            var controller=new ManorController(preferences,session.Bankroll,progressionController,committed=>
            {
                session.ResetBankroll(committed.Bankroll);
                preferences=committed;
                selectedBet=Math.Min(selectedBet,session.Bankroll);
            });
            backgroundMusic.SetEnabled(false);
            var window=new ManorWindow(controller) { Owner=this };
            window.ShowDialog();
            preferences=window.Preferences;
            SoundCheckBox.IsChecked=preferences.SoundEnabled;
            AnimationsCheckBox.IsChecked=preferences.AnimationsEnabled;
            MusicCheckBox.IsChecked=preferences.MusicEnabled;
        }
        catch(Exception ex) when(ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        { StatusText.Text="Midnight Manor could not open: "+ex.Message; }
        finally
        {
            backgroundMusic.SetEnabled(preferences.MusicEnabled);
            backgroundMusic.SetVolume(preferences.MusicVolume);
            NormalizeBetForTable(); SavePreferences(); RefreshGameUi(); ApplyBetLimitState(); RefreshProfileButton();
        }
    }
}
