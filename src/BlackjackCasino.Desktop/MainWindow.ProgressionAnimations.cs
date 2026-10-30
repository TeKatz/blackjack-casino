using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void AnimateLevelUpCue()
    {
        if (profileButton is null || profileLevelUpSweep is null || profileLevelUpSweepTransform is null)
        {
            return;
        }

        profileLevelUpSweep.BeginAnimation(OpacityProperty, null);
        profileLevelUpSweepTransform.BeginAnimation(TranslateTransform.XProperty, null);

        var glow = new DropShadowEffect
        {
            Color = Color.FromRgb(255, 186, 82),
            BlurRadius = 14,
            ShadowDepth = 0,
            Opacity = 0,
        };
        profileButton.Effect = glow;

        const double sweepDurationMs = 4600;
        const double glowDurationMs = 5400;
        var sweepEase = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var sweep = new DoubleAnimation
        {
            From = -125,
            To = 315,
            Duration = TimeSpan.FromMilliseconds(sweepDurationMs),
            EasingFunction = sweepEase,
            FillBehavior = FillBehavior.Stop,
        };

        var sweepOpacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(sweepDurationMs),
            FillBehavior = FillBehavior.Stop,
        };
        sweepOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        sweepOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(650)), new SineEase { EasingMode = EasingMode.EaseOut }));
        sweepOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3550))));
        sweepOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(sweepDurationMs)), new SineEase { EasingMode = EasingMode.EaseIn }));

        var glowOpacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(glowDurationMs),
            FillBehavior = FillBehavior.Stop,
        };
        glowOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        glowOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(750)), new SineEase { EasingMode = EasingMode.EaseOut }));
        glowOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0.76, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(4050))));
        glowOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(glowDurationMs)), new SineEase { EasingMode = EasingMode.EaseIn }));

        var glowRadius = new DoubleAnimation
        {
            From = 14,
            To = 34,
            Duration = TimeSpan.FromMilliseconds(2250),
            AutoReverse = true,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop,
        };

        glowOpacity.Completed += (_, _) =>
        {
            profileLevelUpSweep.Opacity = 0;
            profileLevelUpSweepTransform.X = -125;
            profileButton.ClearValue(EffectProperty);
            // The level-up glow temporarily replaces the equipped cosmetic frame glow.
            // Restore the normal profile presentation once the one-shot sweep is over.
            RefreshProfileButton();
        };

        profileLevelUpSweepTransform.BeginAnimation(TranslateTransform.XProperty, sweep);
        profileLevelUpSweep.BeginAnimation(OpacityProperty, sweepOpacity);
        glow.BeginAnimation(DropShadowEffect.OpacityProperty, glowOpacity);
        glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, glowRadius);
    }
}
