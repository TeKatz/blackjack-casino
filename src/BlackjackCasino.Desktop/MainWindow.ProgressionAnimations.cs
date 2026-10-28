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
            Color = Color.FromRgb(220, 62, 80),
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = 0,
        };
        profileButton.Effect = glow;

        var sweepEase = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var sweep = new DoubleAnimation
        {
            From = -92,
            To = 214,
            Duration = TimeSpan.FromMilliseconds(920),
            EasingFunction = sweepEase,
            FillBehavior = FillBehavior.Stop,
        };

        var sweepOpacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(920),
            FillBehavior = FillBehavior.Stop,
        };
        sweepOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        sweepOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130)), new SineEase { EasingMode = EasingMode.EaseOut }));
        sweepOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0.82, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(680))));
        sweepOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(920)), new SineEase { EasingMode = EasingMode.EaseIn }));

        var glowOpacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(1050),
            FillBehavior = FillBehavior.Stop,
        };
        glowOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        glowOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0.78, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170)), new SineEase { EasingMode = EasingMode.EaseOut }));
        glowOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0.46, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(720))));
        glowOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1050)), new SineEase { EasingMode = EasingMode.EaseIn }));

        var glowRadius = new DoubleAnimation
        {
            From = 10,
            To = 24,
            Duration = TimeSpan.FromMilliseconds(520),
            AutoReverse = true,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop,
        };

        glowOpacity.Completed += (_, _) =>
        {
            profileLevelUpSweep.Opacity = 0;
            profileLevelUpSweepTransform.X = -92;
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
