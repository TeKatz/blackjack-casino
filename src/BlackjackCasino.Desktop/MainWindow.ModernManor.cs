using System.IO;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void OpenModernManorPreview()
    {
        if(session.RoundInProgress)
        {
            StatusText.Text="Finish your Blackjack hand before visiting Midnight Manor.";
            return;
        }
        if(progressionController is null) return;
        if(!ModernManorPreviewWindow.IsAvailable)
        {
            StatusText.Text="The modern Manor frontend has not been included in this build.";
            return;
        }

        ProcessLatestCompletedRound();
        try
        {
            // The exact same recovered and journaled C# engine as the legacy Manor.
            var controller=new ManorController(preferences,session.Bankroll,progressionController,committed=>
            {
                session.ResetBankroll(committed.Bankroll);
                preferences=committed;
                selectedBet=Math.Min(selectedBet,session.Bankroll);
            });
            var modern=new ModernManorPreviewWindow(controller);
            ShowEmbeddedGame(modern,"Midnight Manor 2.0 · Development playtest",
                muteHostMusic:true,
                closed:()=>
                {
                    preferences=controller.Preferences;
                    StatusText.Text="Returned from Midnight Manor 2.0. Your committed game state is saved.";
                });
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException
            or System.Text.Json.JsonException or ArgumentException or InvalidDataException)
        {
            StatusText.Text="Modern Manor unavailable: "+ex.Message;
        }
    }
}
