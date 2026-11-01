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
        if (variant is not ("neon-noir" or "eclipse-thread" or "eclipse-halo" or "aurora-drift"
            or "neon-thread" or "dynasty-circuit" or "neon-pagoda" or "dynasty-crown")) return false;
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
            case "neon-thread": // Entry Dynasty frame: razor-thin cyan/magenta light on black glass.
                brush = new LinearGradientBrush(new GradientStopCollection
                {
                    new(C("#54F6F0"), 0), new(C("#12202B"), .28), new(C("#FF55B8"), .52),
                    new(C("#161323"), .72), new(C("#54F6F0"), 1),
                }, 20);
                thickness = new Thickness(2, 1, 2, 1); corners = new CornerRadius(6); glow = C("#55EDEB");
                break;
            case "dynasty-circuit": // Cyan signal paths separated by short imperial-gold nodes.
                var circuit = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(.16, 1), SpreadMethod = GradientSpreadMethod.Repeat };
                circuit.GradientStops.Add(new(C("#0C1720"), 0)); circuit.GradientStops.Add(new(C("#48E7E4"), .23));
                circuit.GradientStops.Add(new(C("#F3BF58"), .31)); circuit.GradientStops.Add(new(C("#171126"), .48));
                circuit.GradientStops.Add(new(C("#FF55B8"), .72)); circuit.GradientStops.Add(new(C("#0C1720"), 1));
                brush = circuit; thickness = new Thickness(2); corners = new CornerRadius(4, 14, 4, 14); glow = C("#E3B654");
                break;
            case "neon-pagoda": // Layered architectural bands; heavier at top/bottom like illuminated eaves.
                brush = new LinearGradientBrush(new GradientStopCollection
                {
                    new(C("#FF4FB5"), 0), new(C("#702C78"), .18), new(C("#111522"), .37),
                    new(C("#50F2EA"), .56), new(C("#C6943F"), .76), new(C("#FF4FB5"), 1),
                }, 90);
                thickness = new Thickness(2, 4, 2, 4); corners = new CornerRadius(2, 12, 2, 12); glow = C("#FF59BD");
                break;
            case "dynasty-crown": // Final frame: black-glass breaks between cyan, magenta and imperial gold.
                brush = new LinearGradientBrush(new GradientStopCollection
                {
                    new(C("#F7CF69"), 0), new(C("#54F6F0"), .16), new(C("#12101C"), .34),
                    new(C("#FF58BC"), .50), new(C("#12101C"), .66), new(C("#54F6F0"), .84), new(C("#F7CF69"), 1),
                }, 30);
                thickness = new Thickness(4, 2, 4, 2); corners = new CornerRadius(14, 3, 14, 3); glow = C("#F7CF69");
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
        bool quiet = variant is "eclipse-thread" or "neon-thread";
        var effect = new DropShadowEffect { Color = glow, ShadowDepth = 0, BlurRadius = quiet ? 4 : variant == "dynasty-crown" ? 16 : 12, Opacity = quiet ? .18 : .34 };
        target.Effect = effect;
        void Stop()
        {
            effect.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            if (brush.RelativeTransform is RotateTransform rotation) rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
        void Start()
        {
            if (!animate || !target.IsVisible) return;
            if (variant is "eclipse-halo" or "neon-thread" or "neon-pagoda")
            {
                double low = variant == "neon-thread" ? .12 : .18;
                double high = variant == "neon-pagoda" ? .56 : .48;
                effect.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(low, high, TimeSpan.FromSeconds(variant == "neon-pagoda" ? 2.5 : 3.2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            }
            else if (variant is "aurora-drift" or "dynasty-circuit" or "dynasty-crown")
            {
                var rotate = new RotateTransform(0, .5, .5); brush.RelativeTransform = rotate;
                double to = variant == "dynasty-crown" ? 360 : 34;
                double from = variant == "dynasty-crown" ? 0 : -14;
                double seconds = variant == "dynasty-crown" ? 7.5 : 5.5;
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = variant != "dynasty-crown",
                    RepeatBehavior = RepeatBehavior.Forever,
                });
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
