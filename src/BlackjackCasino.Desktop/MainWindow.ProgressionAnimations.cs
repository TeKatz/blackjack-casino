using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void AnimateLevelUpCue()
    {
        if (profileButton is null)
        {
            return;
        }

        var scale = new ScaleTransform(1, 1);
        profileButton.RenderTransformOrigin = new Point(0.5, 0.5);
        profileButton.RenderTransform = scale;

        var glow = new DropShadowEffect
        {
            Color = Color.FromRgb(255, 215, 124),
            BlurRadius = 7,
            ShadowDepth = 0,
            Opacity = 0,
        };
        profileButton.Effect = glow;

        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        var scalePulse = new DoubleAnimation
        {
            From = 1,
            To = 1.045,
            Duration = TimeSpan.FromMilliseconds(230),
            AutoReverse = true,
            EasingFunction = ease,
        };
        var glowPulse = new DoubleAnimation
        {
            From = 0,
            To = 0.48,
            Duration = TimeSpan.FromMilliseconds(260),
            AutoReverse = true,
            EasingFunction = ease,
        };
        var blurPulse = new DoubleAnimation
        {
            From = 7,
            To = 16,
            Duration = TimeSpan.FromMilliseconds(260),
            AutoReverse = true,
            EasingFunction = ease,
        };

        glowPulse.Completed += (_, _) =>
        {
            profileButton.ClearValue(RenderTransformProperty);
            profileButton.ClearValue(RenderTransformOriginProperty);
            profileButton.ClearValue(EffectProperty);
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scalePulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scalePulse.Clone());
        glow.BeginAnimation(DropShadowEffect.OpacityProperty, glowPulse);
        glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurPulse);
    }
}
