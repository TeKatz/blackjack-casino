using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

/// <summary>Presentation-only interpolation. The decimal service remains the source of truth.</summary>
public sealed class EmpireBalanceCounter : TextBlock
{
    public static readonly DependencyProperty DisplayedAmountProperty = DependencyProperty.Register(
        nameof(DisplayedAmount), typeof(double), typeof(EmpireBalanceCounter),
        new PropertyMetadata(0d, (d, _) => ((EmpireBalanceCounter)d).RefreshText()));
    private bool initialized;
    private decimal target;
    public double DisplayedAmount { get => (double)GetValue(DisplayedAmountProperty); set => SetValue(DisplayedAmountProperty, value); }

    internal void SetAmount(decimal amount, bool animate)
    {
        if (initialized && target == amount && animate) return;
        double from = DisplayedAmount;
        BeginAnimation(DisplayedAmountProperty, null);
        DisplayedAmount = (double)amount;
        if (animate && initialized && target != amount)
            BeginAnimation(DisplayedAmountProperty, new DoubleAnimation(from, (double)amount, TimeSpan.FromMilliseconds(700))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop,
            });
        target = amount;
        initialized = true;
    }

    private void RefreshText() => Text = EmpireView.Number((decimal)Math.Clamp(DisplayedAmount, 0, 1e24)) + " cr";
}
