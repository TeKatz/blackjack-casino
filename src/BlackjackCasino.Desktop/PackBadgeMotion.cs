using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Gives the unopened-pack badge a short attention pulse when it becomes visible.
/// The badge deliberately settles afterwards so an idle lobby does not keep WPF's compositor active forever.
/// </summary>
internal static class PackBadgeMotion
{
    private sealed class State
    {
        internal required Border Badge;
        internal required Func<bool> Enabled;
        internal readonly ScaleTransform Scale = new(1, 1);
        internal bool Active;

        internal void Sync()
        {
            bool active = Badge.IsLoaded && Badge.IsVisible && Badge.IsEnabled && Enabled();
            if (active == Active) return;
            Active = active;
            Stop();
            if (!active) return;

            DoubleAnimation Pulse(double from, double to) => new(from, to, TimeSpan.FromMilliseconds(520))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop,
            };

            DoubleAnimation opacity = Pulse(.78, 1);
            opacity.Completed += (_, _) =>
            {
                if (Active) Stop();
            };

            Scale.BeginAnimation(ScaleTransform.ScaleXProperty, Pulse(1, 1.07));
            Scale.BeginAnimation(ScaleTransform.ScaleYProperty, Pulse(1, 1.07));
            Badge.BeginAnimation(UIElement.OpacityProperty, opacity);
        }

        private void Stop()
        {
            Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            Badge.BeginAnimation(UIElement.OpacityProperty, null);
            Scale.ScaleX = Scale.ScaleY = 1;
            Badge.Opacity = 1;
        }
    }

    private static readonly ConditionalWeakTable<Border, State> States = new();

    internal static void Attach(Border badge, Func<bool> enabled)
    {
        if (States.TryGetValue(badge, out _)) return;
        var state = new State { Badge = badge, Enabled = enabled };
        States.Add(badge, state);
        badge.RenderTransform = state.Scale;
        badge.RenderTransformOrigin = new Point(.5, .5);
        badge.Loaded += (_, _) => state.Sync();
        badge.Unloaded += (_, _) => state.Sync();
        badge.IsVisibleChanged += (_, _) => state.Sync();
        badge.IsEnabledChanged += (_, _) => state.Sync();
        state.Sync();
    }

    internal static void Sync(Border? badge)
    {
        if (badge is not null && States.TryGetValue(badge, out State? state)) state.Sync();
    }
}
