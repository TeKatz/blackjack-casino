using System.Windows;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private static readonly TimeSpan AmbientAnimationInterval = TimeSpan.FromMilliseconds(80);

    static EmpireView()
    {
        EventManager.RegisterClassHandler(
            typeof(EmpireView),
            LoadedEvent,
            new RoutedEventHandler(OnEmpireViewLoaded));
    }

    private static void OnEmpireViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is EmpireView view)
        {
            // Room artwork is decorative. The previous 25 Hz clock invalidated every open
            // custom-drawn room card even when the simulation itself had nothing to update.
            // A 12.5 Hz ambient budget keeps the motion alive while halving that render pressure.
            view.animationTimer.Interval = AmbientAnimationInterval;
        }
    }
}
