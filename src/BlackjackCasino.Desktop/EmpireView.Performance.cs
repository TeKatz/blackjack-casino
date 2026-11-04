using System.Windows;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private static readonly TimeSpan AmbientAnimationInterval = TimeSpan.FromMilliseconds(80);
    private bool ambientVisibilityHooked;

    static EmpireView()
    {
        EventManager.RegisterClassHandler(
            typeof(EmpireView),
            LoadedEvent,
            new RoutedEventHandler(OnEmpireViewLoaded));
    }

    private static void OnEmpireViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not EmpireView view)
        {
            return;
        }

        // Room artwork is decorative. The previous 25 Hz clock invalidated every open
        // custom-drawn room card even when the simulation itself had nothing to update.
        // A 12.5 Hz ambient budget keeps the motion alive while halving that render pressure.
        view.animationTimer.Interval = AmbientAnimationInterval;

        // WPF can transiently unload/reload complex views while their containing presentation
        // changes. The instance Unloaded handler correctly stops the clock, but relying only on
        // a later Loaded event leaves a narrow lifecycle race where a visible Empire view can
        // retain a stopped ambient timer. Visibility is the actual rendering contract, so use it
        // as the self-healing edge for the low-frequency artwork clock.
        if (!view.ambientVisibilityHooked)
        {
            view.IsVisibleChanged += OnEmpireViewIsVisibleChanged;
            view.ambientVisibilityHooked = true;
        }

        view.EnsureAmbientAnimationActive();
    }

    private static void OnEmpireViewIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is EmpireView view)
        {
            view.SynchronizeAmbientAnimationClock();
            if ((bool)e.NewValue) view.EnsureAmbientAnimationActive();
        }
    }

    // A WPF view can remain visible while its DispatcherTimer is suspended by a
    // transient unload/reparent. The economy's independent 1 Hz refresh and real
    // presentation changes must be capable of restoring ambient motion themselves.
    // This also makes the very first enabled frame deterministic rather than depending
    // on a timer tick racing a new preference value.
    private void EnsureAmbientAnimationActive()
    {
        if (disposed || !IsLoaded || !IsVisible) return;
        SynchronizeAmbientAnimationClock();
        if (animations() && !wasAnimating) AdvanceAmbientAnimationFrame();
    }

    private void AdvanceAmbientAnimationFrame()
    {
        bool active = !disposed && IsVisible && animations();
        if (active)
        {
            phase += .08;
            foreach (EmpireRoomCard card in cards)
                if (card.IsVisible && card.Artwork.Open) card.Artwork.Phase = phase;
        }
        else if (wasAnimating)
        {
            foreach (EmpireRoomCard card in cards) card.StopAnimation();
        }
        wasAnimating = active;
    }

    private void SynchronizeAmbientAnimationClock()
    {
        if (disposed)
        {
            animationTimer.Stop();
            return;
        }

        animationTimer.Interval = AmbientAnimationInterval;
        if (IsLoaded && IsVisible)
        {
            if (!animationTimer.IsEnabled)
            {
                animationTimer.Start();
            }
        }
        else if (!IsVisible)
        {
            animationTimer.Stop();
            wasAnimating = false;
        }
    }
}
