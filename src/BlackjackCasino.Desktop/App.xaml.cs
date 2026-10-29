using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        CasinoAudio.Shutdown();
        base.OnExit(e);
    }
}
