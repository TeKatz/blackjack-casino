using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

/// <summary>Compositor-only breathing on the tiny badge, with no blur/layout animation or timer.</summary>
internal static class PackBadgeMotion
{
    private sealed class State
    {
        internal required Border Badge;
        internal required Func<bool> Enabled;
        internal readonly ScaleTransform Scale = new(1, 1);
        internal bool Running;
        internal void Sync()
        {
            bool run = Badge.IsLoaded && Badge.IsVisible && Badge.IsEnabled && Enabled();
            if (run == Running) return;
            Running = run;
            Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            Badge.BeginAnimation(UIElement.OpacityProperty, null);
            Scale.ScaleX = Scale.ScaleY = 1; Badge.Opacity = 1;
            if (!run) return;
            DoubleAnimation Pulse(double from, double to) => new(from, to, TimeSpan.FromSeconds(1.65))
            {
                AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            Scale.BeginAnimation(ScaleTransform.ScaleXProperty, Pulse(1, 1.085));
            Scale.BeginAnimation(ScaleTransform.ScaleYProperty, Pulse(1, 1.085));
            Badge.BeginAnimation(UIElement.OpacityProperty, Pulse(.72, 1));
        }
    }
    private static readonly ConditionalWeakTable<Border, State> States = new();
    internal static void Attach(Border badge, Func<bool> enabled)
    {
        if (States.TryGetValue(badge, out _)) return;
        var state = new State { Badge = badge, Enabled = enabled };
        States.Add(badge, state);
        badge.RenderTransform = state.Scale; badge.RenderTransformOrigin = new Point(.5, .5);
        badge.Loaded += (_, _) => state.Sync(); badge.Unloaded += (_, _) => state.Sync();
        badge.IsVisibleChanged += (_, _) => state.Sync(); badge.IsEnabledChanged += (_, _) => state.Sync();
        state.Sync();
    }
    internal static void Sync(Border? badge) { if (badge is not null && States.TryGetValue(badge, out State? state)) state.Sync(); }
}
