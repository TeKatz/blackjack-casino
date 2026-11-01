using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

internal static class DistinctCosmeticFrames
{
    private sealed record Lifecycle(Action Dispose);
    private static readonly ConditionalWeakTable<FrameworkElement, Lifecycle> Lifecycles = new();

    internal static bool Apply(FrameworkElement target, string variant, bool animate)
    {
        if (Lifecycles.TryGetValue(target, out Lifecycle? previous))
        {
            previous.Dispose(); Lifecycles.Remove(target);
        }
        if (variant is not ("neon-noir" or "eclipse-thread" or "eclipse-halo" or "aurora-drift")) return false;
        Color C(string value) => (Color)ColorConverter.ConvertFromString(value);
        Brush brush;
        Thickness thickness;
        CornerRadius corners;
        Color glow;
        switch (variant)
        {
            case "neon-noir": // Split neon tubes and angular corners.
                brush = new LinearGradientBrush(new GradientStopCollection
                {
                    new(C("#58F5EE"), 0), new(C("#58F5EE"), .47),
                    new(C("#111826"), .48), new(C("#111826"), .52),
                    new(C("#FF4DAB"), .53), new(C("#FF4DAB"), 1),
                }, 0);
                thickness = new Thickness(3, 1, 3, 1); corners = new CornerRadius(2); glow = C("#FF4DAB");
                break;
            case "eclipse-thread": // Delicate woven silver, deliberately quiet and fine.
                var thread = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(.12, 1), SpreadMethod = GradientSpreadMethod.Repeat };
                thread.GradientStops.Add(new(C("#41394E"), 0)); thread.GradientStops.Add(new(C("#E0CDEB"), .45));
                thread.GradientStops.Add(new(C("#766885"), .55)); thread.GradientStops.Add(new(C("#41394E"), 1));
                brush = thread; thickness = new Thickness(1); corners = new CornerRadius(16, 4, 16, 4); glow = C("#B8A4C8");
                break;
            case "eclipse-halo": // Warm corona around a dark eclipse, no teal/magenta gradient.
                brush = new RadialGradientBrush(new GradientStopCollection
                {
                    new(C("#241522"), .45), new(C("#9A563F"), .69), new(C("#FFCB75"), .84), new(C("#FFF0C6"), 1),
                }) { Center = new Point(.5, .5), GradientOrigin = new Point(.48, .48), RadiusX = .55, RadiusY = .65 };
                thickness = new Thickness(3); corners = new CornerRadius(26); glow = C("#FFC071");
                break;
            default: // Translucent glacial facets with a moving reflected light.
                brush = new LinearGradientBrush(new GradientStopCollection
                {
                    new(C("#98F4E3"), 0), new(C("#385C81"), .22), new(C("#DAFFF5"), .40),
                    new(C("#7895CF"), .43), new(C("#527F92"), .69), new(C("#C5FAE8"), 1),
                }, 38);
                thickness = new Thickness(2, 3, 2, 1); corners = new CornerRadius(10); glow = C("#A2EFDF");
                break;
        }
        if (target is Border border)
        {
            border.BorderBrush = brush; border.BorderThickness = thickness; border.CornerRadius = corners;
        }
        else if (target is Control control) { control.BorderBrush = brush; control.BorderThickness = thickness; }
        var effect = new DropShadowEffect { Color = glow, ShadowDepth = 0, BlurRadius = variant == "eclipse-thread" ? 3 : 12, Opacity = variant == "eclipse-thread" ? .13 : .32 };
        target.Effect = effect;
        void Stop()
        {
            effect.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            if (brush.RelativeTransform is RotateTransform rotation) rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
        void Start()
        {
            if (!animate || !target.IsVisible) return;
            if (variant == "eclipse-halo")
                effect.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(.18, .52, TimeSpan.FromSeconds(3)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            else if (variant == "aurora-drift")
            {
                var rotate = new RotateTransform(0, .5, .5); brush.RelativeTransform = rotate;
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-12, 32, TimeSpan.FromSeconds(6)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            }
        }
        RoutedEventHandler loaded = (_, _) => Start();
        RoutedEventHandler unloaded = (_, _) => Stop();
        DependencyPropertyChangedEventHandler visible = (_, _) => { if (target.IsVisible) Start(); else Stop(); };
        target.Loaded += loaded; target.Unloaded += unloaded; target.IsVisibleChanged += visible;
        Lifecycles.Add(target, new Lifecycle(() =>
        {
            Stop(); target.Loaded -= loaded; target.Unloaded -= unloaded; target.IsVisibleChanged -= visible;
        }));
        if (target.IsLoaded) Start();
        return true;
    }
}
