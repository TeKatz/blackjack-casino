namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Window templates are not guaranteed to have a visual tree during the constructor.
        // The constructor attempts eager initialization, while this guarantees Player and
        // Challenges are present once WPF has created the actual TabControl visual.
        if (challengesPanel is null)
        {
            InitializeContentTabs();
            RefreshChallenges();
        }
    }
}
