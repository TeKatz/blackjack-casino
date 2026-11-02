using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Static premium-card sheen. The previous forever animation kept one animation clock alive for every
/// visible premium card, so album browsing could continuously wake the compositor even when idle.
/// </summary>
internal static class CollectionFoil
{
    internal static Border Create(CollectionCard card, bool enabled)
    {
        bool ultra = CollectionCatalog.Theme(card).UltraRare;
        Color tint = CollectionArtwork.RarityColor(card.Rarity);
        var brush = new LinearGradientBrush(new GradientStopCollection
        {
            new(Colors.Transparent, 0),
            new(Colors.Transparent, .28),
            new(Color.FromArgb(38, tint.R, tint.G, tint.B), .40),
            new(Color.FromArgb(ultra ? (byte)62 : (byte)42, 255, 248, 220), .49),
            new(ultra ? Color.FromArgb(48, 146, 228, 255) : Color.FromArgb(28, tint.R, tint.G, tint.B), .57),
            new(Colors.Transparent, .70),
            new(Colors.Transparent, 1),
        }, 24);

        if (brush.CanFreeze) brush.Freeze();
        return new Border
        {
            Name = "CollectionFoil",
            Background = brush,
            IsHitTestVisible = false,
            Opacity = enabled ? card.Rarity == CollectionRarity.Rare ? .34 : ultra ? .62 : .48 : 0,
        };
    }
}
