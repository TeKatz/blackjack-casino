using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>Soft foil light, attached only while an owned premium card is actually visible.</summary>
internal static class CollectionFoil
{
    internal static Border Create(CollectionCard card, bool enabled)
    {
        bool ultra = CollectionCatalog.Theme(card).UltraRare;
        Color tint = CollectionArtwork.RarityColor(card.Rarity);
        var brush = new LinearGradientBrush(new GradientStopCollection
        {
            new(Colors.Transparent, 0), new(Colors.Transparent, .27),
            new(Color.FromArgb(60, tint.R, tint.G, tint.B), .39),
            new(Color.FromArgb(ultra ? (byte)88 : (byte)58, 255, 248, 220), .49),
            new(ultra ? Color.FromArgb(72, 146, 228, 255) : Color.FromArgb(38, tint.R, tint.G, tint.B), .56),
            new(Colors.Transparent, .7), new(Colors.Transparent, 1),
        }, 24);
        var motion = new TranslateTransform(-500, 0);
        var layer = new Border { Name = "CollectionFoil", Background = brush, IsHitTestVisible = false,
            RenderTransform = motion, Opacity = card.Rarity == CollectionRarity.Rare ? .40 : ultra ? .80 : .60 };
        bool running = false;
        void Sync()
        {
            bool visible = enabled && layer.IsLoaded && layer.IsVisible;
            if (visible == running) return;
            running = visible;
            if (!visible) { motion.BeginAnimation(TranslateTransform.XProperty, null); motion.X = -500; return; }
            // Long rests between slow reflections. No render loop or per-card timer.
            double period = ultra ? 7.8 : card.Rarity == CollectionRarity.Legendary ? 8.8 : 10.5;
            var sweep = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(period), RepeatBehavior = RepeatBehavior.Forever };
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-500, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-500, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.4 + card.Number * .08))));
            sweep.KeyFrames.Add(new EasingDoubleKeyFrame(500, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(5.4)), new SineEase { EasingMode = EasingMode.EaseInOut }));
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(500, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(period))));
            motion.BeginAnimation(TranslateTransform.XProperty, sweep);
        }
        layer.Loaded += (_, _) => Sync();
        layer.IsVisibleChanged += (_, _) => Sync();
        layer.Unloaded += (_, _) => { running = false; motion.BeginAnimation(TranslateTransform.XProperty, null); motion.X = -500; };
        return layer;
    }
}
