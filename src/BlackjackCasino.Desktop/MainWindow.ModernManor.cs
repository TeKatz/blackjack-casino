using System.IO;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void OpenModernManorPreview()
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish your Blackjack hand before visiting Midnight Manor.";
            return;
        }
        if (progressionController is null)
            return;
        if (!ModernManorPreviewWindow.IsAvailable)
        {
            StatusText.Text = "The modern Manor preview has not been included in this build.";
            return;
        }

        ProcessLatestCompletedRound();
        try
        {
            // Reuse the same recovered, committed state as legacy Manor.
            // There are deliberately no mutation callbacks from the browser.
            var controller = new ManorController(
                preferences, session.Bankroll, progressionController, _ => { });
            var preview = new ModernManorPreviewWindow(controller.State, controller.Preferences);
            ShowEmbeddedGame(preview,
                "Midnight Manor 2.0 · Modern preview",
                muteHostMusic: true,
                closed: () => StatusText.Text = "Returned from the read-only Midnight Manor preview.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or System.Text.Json.JsonException or ArgumentException)
        {
            StatusText.Text = "Modern Manor preview unavailable: " + ex.Message;
        }
    }
}
