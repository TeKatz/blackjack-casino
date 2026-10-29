using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

// Purely visual: this control receives values, never a session or game action.
internal sealed class TableWagerView : StackPanel
{
    private readonly TableChipDrawing chips;
    private readonly TranslateTransform slide = new();
    private readonly ScaleTransform scale = new(1, 1);
    private readonly double restingOpacity;
    private readonly decimal settlementNet;

    internal TableWagerView(decimal amount, bool settled, decimal net, bool preview)
    {
        Name = "PlayerWager";
        IsHitTestVisible = false;
        Focusable = false;
        HorizontalAlignment = HorizontalAlignment.Center;
        Margin = new Thickness(0, 8, 0, 0);
        settlementNet = net;
        restingOpacity = settled ? .52 : 1;
        var groups = TableWagerPresentation.Chips(amount);
        string exact = TableWagerPresentation.Amount(amount);
        string description = amount == 0m ? "No wager" : (preview ? "Selected wager " : "Wager ") + exact;
        if (settled) description += ", settled";
        AutomationProperties.SetName(this, description);

        chips = new TableChipDrawing(groups)
        {
            Opacity = restingOpacity,
            RenderTransformOrigin = new Point(.5, .7),
            RenderTransform = new TransformGroup { Children = { scale, slide } },
        };
        Children.Add(chips);
        var label = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink(settled ? "#C6C6AC" : "#FFE09B"),
        };
        label.Inlines.Add(new Run(settled ? "SETTLED  " : preview ? "SELECTED  " : "WAGER  ")
        {
            FontSize = 9,
            FontWeight = FontWeights.Normal,
            Foreground = Ink("#A7BBAE"),
        });
        label.Inlines.Add(new Run(exact));
        Children.Add(label);
    }

    internal void StopAnimations()
    {
        chips.BeginAnimation(OpacityProperty, null);
        slide.BeginAnimation(TranslateTransform.YProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        chips.Opacity = restingOpacity;
        slide.Y = 0;
        scale.ScaleX = scale.ScaleY = 1;
    }

    internal void Animate(TableWagerTransition transition, int handIndex)
    {
        if (transition == default) return;
        bool deposit = transition.Change != TableWagerChange.None;
        double delay = handIndex * 45;
        var motion = Track();
        var opacity = Track();
        var size = Track();
        Add(motion, deposit ? 22 : 0, 0);
        Add(opacity, deposit ? 0 : 1, 0);
        Add(size, deposit ? .86 : 1, 0);
        double arrival = delay + (deposit ? 280 : 0);
        if (deposit)
        {
            Add(motion, 22, delay);
            Add(opacity, 0, delay);
            Add(size, .86, delay);
            Add(motion, 0, arrival);
            Add(opacity, 1, arrival);
            Add(size, 1, arrival);
        }

        if (transition.Settle)
        {
            // Deal-time blackjack and double-to-settle first place the new wager,
            // then collect/return it. The receipt and exact stake remain readable.
            double start = arrival + 100;
            Add(motion, 0, start);
            Add(opacity, 1, start);
            Add(size, 1, start);
            Add(motion, settlementNet < 0m ? -12 : 10, start + 190);
            Add(size, settlementNet > 0m ? 1.12 : .94, start + 190);
            Add(motion, 0, start + 420);
            Add(opacity, restingOpacity, start + 420);
            Add(size, 1, start + 420);
        }

        chips.BeginAnimation(OpacityProperty, opacity);
        slide.BeginAnimation(TranslateTransform.YProperty, motion);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, size);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, size);
    }

    private static DoubleAnimationUsingKeyFrames Track() => new() { FillBehavior = FillBehavior.Stop };

    private static void Add(DoubleAnimationUsingKeyFrames animation, double value, double milliseconds) =>
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(milliseconds)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });

    private static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}

internal sealed class TableChipDrawing : FrameworkElement
{
    private readonly IReadOnlyList<TableChipGroup> groups;
    private static readonly Typeface Face = new("Segoe UI");

    internal TableChipDrawing(IReadOnlyList<TableChipGroup> groups)
    {
        this.groups = groups;
        Width = Math.Max(1, Math.Min(groups.Count, 5)) * 36 + 12;
        Height = Math.Max(1, (groups.Count + 4) / 5) * 58;
        HorizontalAlignment = HorizontalAlignment.Center;
    }

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (groups.Count == 0)
        {
            drawing.DrawEllipse(null, new Pen(Ink("#507A866B"), 1), new Point(Width / 2, 32), 18, 10);
            return;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            TableChipGroup group = groups[i];
            int row = i / 5;
            int columns = Math.Min(5, groups.Count - row * 5);
            double x = (Width - columns * 36) / 2 + (i % 5) * 36 + 18;
            double y = row * 58 + 35;
            drawing.DrawEllipse(Ink("#65000000"), null, new Point(x + 2, y + 7), 19, 7);
            Brush color = Ink(group.Denomination switch
            {
                >= 1000m => "#B98A36",
                >= 500m => "#7855A5",
                >= 100m => "#283640",
                >= 25m => "#21826A",
                >= 5m => "#BD4E53",
                >= 1m => "#DED6C0",
                >= .25m => "#4B8BA9",
                _ => "#A4764C",
            });
            var edge = new Pen(Ink("#BCE8DCC0"), .8);
            for (int chip = 0; chip < group.VisibleCount; chip++)
            {
                double cy = y - chip * 3.4;
                drawing.DrawEllipse(color, edge, new Point(x, cy), 16, 9);
                drawing.DrawLine(new Pen(Ink("#B4F3E7CE"), 2), new Point(x - 12, cy + 3), new Point(x - 10, cy + 6));
                drawing.DrawLine(new Pen(Ink("#B4F3E7CE"), 2), new Point(x + 12, cy + 3), new Point(x + 10, cy + 6));
            }
            double top = y - (group.VisibleCount - 1) * 3.4;
            drawing.DrawEllipse(null, new Pen(Ink("#9CEFE4CC"), .7), new Point(x, top), 11.5, 6.5);
            Label(drawing, group.Face, x, top - 5.5, 8, group.Denomination is >= 1m and < 5m ? "#26382E" : "#FFF4DB");
            if (group.Count > 1m) Label(drawing, "×" + group.Count.ToString("0", CultureInfo.CurrentCulture), x, y + 11, 8, "#C4D2C3");
        }
    }

    private void Label(DrawingContext drawing, string value, double x, double y, double size, string color)
    {
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Face, size,
            Ink(color), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawing.DrawText(text, new Point(x - text.Width / 2, y));
    }

    private static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
