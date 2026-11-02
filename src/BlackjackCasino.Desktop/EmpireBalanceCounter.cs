using System.Windows;
using BlackjackCasino.Core.Empire;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

/// <summary>Presentation-only interpolation. The decimal service remains the source of truth.</summary>
public sealed class EmpireBalanceCounter : TextBlock
{
    public static readonly DependencyProperty DisplayedAmountProperty = DependencyProperty.Register(
        nameof(DisplayedAmount), typeof(double), typeof(EmpireBalanceCounter),
        new PropertyMetadata(0d, (d, _) => ((EmpireBalanceCounter)d).RefreshText()));
    internal string UnitText { get; init; } = " cr";
    private bool initialized;
    private decimal target;
    public double DisplayedAmount { get => (double)GetValue(DisplayedAmountProperty); set => SetValue(DisplayedAmountProperty, value); }

    public EmpireBalanceCounter()
    {
        IsVisibleChanged += (_, _) => { if (!IsVisible) StopInterpolation(); };
        IsEnabledChanged += (_, _) => { if (!IsEnabled) StopInterpolation(); };
        Unloaded += (_, _) => StopInterpolation();
    }

    internal void StopInterpolation()
    {
        BeginAnimation(DisplayedAmountProperty, null);
        if (initialized) DisplayedAmount = (double)target;
    }

    internal void SetAmount(decimal amount, bool animate)
    {
        if (initialized && target == amount && animate) return;
        double from = DisplayedAmount;
        BeginAnimation(DisplayedAmountProperty, null);
        DisplayedAmount = (double)amount;
        if (animate && initialized && target != amount)
            BeginAnimation(DisplayedAmountProperty, new DoubleAnimation(from, (double)amount, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop,
            });
        target = amount;
        initialized = true;
    }

    private void RefreshText()
    {
        string formatted = EmpireView.Number((decimal)Math.Clamp(DisplayedAmount, 0, (double)EmpireRules.MaximumCredits)) + UnitText;
        if (Text != formatted) Text = formatted;
    }
}
