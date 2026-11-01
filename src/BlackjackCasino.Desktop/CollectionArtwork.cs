using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>Full-art collectible editions with unique painted subjects and tiered metallic finishing.</summary>
internal static class CollectionArtwork
{
    internal static Color RarityColor(CollectionRarity rarity) => (Color)ColorConverter.ConvertFromString(rarity switch
    {
        CollectionRarity.Common => "#B4C4CA",
        CollectionRarity.Uncommon => "#74D5AA",
        CollectionRarity.Rare => "#67B8FF",
        CollectionRarity.Epic => "#C494FF",
        _ => "#FFD57A",
    });

    internal static Border Card(CollectionCard card, bool owned, string? badge = null, bool animationsEnabled = true)
    {
        CollectionTheme theme = CollectionCatalog.Theme(card);
        Color foil = RarityColor(card.Rarity);
        bool premium = card.Rarity >= CollectionRarity.Rare;
        var content = new Grid { ClipToBounds = true };
        if (owned)
            content.Children.Add(new Image { Name = "CollectionCardArt", Tag = card.Id, Source = CollectionIllustrations.ImageFor(card), Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch });
        else
        {
            content.Background = new LinearGradientBrush(Color.FromRgb(19, 31, 39), Color.FromRgb(5, 12, 21), 70);
            content.Children.Add(new TextBlock { Text = theme.Symbol, FontSize = 60, Opacity = .16, Foreground = new SolidColorBrush(foil), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        }
        // Light falloff keeps typography legible without boxing the illustration into an icon slot.
        content.Children.Add(new Border { IsHitTestVisible = false, Background = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromArgb(190, 3, 8, 15), 0), new(Color.FromArgb(20, 3, 8, 15), .22),
            new(Colors.Transparent, .48), new(Color.FromArgb(130, 3, 8, 15), .68), new(Color.FromArgb(248, 3, 8, 15), 1),
        }, 90) });
        var header = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(9, 8, 9, 0) };
        header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = card.Rarity.ToString().ToUpperInvariant(), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(foil) });
        var number = new TextBlock { Text = $"{card.Number:00} / {theme.Cards.Count:00}", FontSize = 9, Foreground = new SolidColorBrush(foil) };
        Grid.SetColumn(number, 1); header.Children.Add(number); content.Children.Add(header);
        var label = new StackPanel { Margin = new Thickness(10, 0, 10, 10), VerticalAlignment = VerticalAlignment.Bottom };
        label.Children.Add(new TextBlock { Text = theme.UltraRare ? "✧  " + Localization.T("Album.UltraRare") + "  ✧" : new string('◆', 1 + (int)card.Rarity), FontSize = 8, Foreground = new SolidColorBrush(foil), Margin = new Thickness(0, 0, 0, 5), HorizontalAlignment = HorizontalAlignment.Center });
        label.Children.Add(new TextBlock { Text = card.Name, FontFamily = new FontFamily("Georgia"), FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.WhiteSmoke, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
        label.Children.Add(new TextBlock { Text = badge ?? (owned ? theme.Name : Localization.T("Album.Undiscovered")), FontSize = 8.5, Margin = new Thickness(0, 6, 0, 0), Foreground = new SolidColorBrush(owned ? foil : Color.FromRgb(150, 161, 171)), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(label);
        // Fine metal inlays stay inside the card. Rarer editions have additional corner engraving.
        content.Children.Add(new Border { IsHitTestVisible = false, Margin = new Thickness(3), BorderThickness = new Thickness(.6), BorderBrush = new SolidColorBrush(Color.FromArgb(owned ? (byte)140 : (byte)45, foil.R, foil.G, foil.B)), CornerRadius = new CornerRadius(7) });
        if (owned && premium)
        {
            var corners = new System.Windows.Shapes.Path { IsHitTestVisible = false, Stroke = new SolidColorBrush(foil), StrokeThickness = theme.UltraRare ? 1.4 : .8,
                Data = Geometry.Parse("M 8,31 L8,8 31,8 M 131,8 L154,8 154,31 M 8,211 L8,234 31,234 M 131,234 L154,234 154,211"), Opacity = .75 };
            content.Children.Add(corners);
            content.Children.Add(CollectionFoil.Create(card, animationsEnabled));
        }
        var metal = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromRgb(27, 33, 43), 0), new(foil, .18), new(Color.FromRgb(76, 68, 52), .42),
            new(Color.FromRgb(255, 242, 202), .58), new(foil, .73), new(Color.FromRgb(28, 32, 42), 1),
        }, 42);
        var border = new Border { Name = "CollectionCard", Tag = card.Id, Width = 164, Height = 248, Margin = new Thickness(8), Padding = new Thickness(2), CornerRadius = new CornerRadius(11),
            BorderThickness = new Thickness(owned ? 1.2 + (int)card.Rarity * .28 : .8), BorderBrush = owned ? metal : new SolidColorBrush(Color.FromRgb(56, 66, 80)), Background = Brushes.Black,
            Child = content, ToolTip = $"{card.Name} · {theme.Name} · {card.Rarity}" + (theme.UltraRare ? " · " + Localization.T("Album.UltraRare") : "") };
        if (owned && premium)
            border.Effect = new DropShadowEffect { Color = foil, BlurRadius = theme.UltraRare ? 20 : 8 + (int)card.Rarity * 2, ShadowDepth = 0, Opacity = theme.UltraRare ? .46 : .18 + (int)card.Rarity * .035 };
        System.Windows.Automation.AutomationProperties.SetName(border, $"{card.Name}, {card.Rarity}, {(owned ? "collected" : "missing")}");
        return border;
    }

    internal static Border Pack(CollectionRarity rarity)
    {
        Color color = RarityColor(rarity);
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = new string('✦', 1 + (int)rarity), Foreground = new SolidColorBrush(color), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "♠", FontSize = 40, Foreground = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) });
        stack.Children.Add(new TextBlock { Text = Localization.T("Album.FiveCards"), FontSize = 9, Foreground = Brushes.WhiteSmoke, HorizontalAlignment = HorizontalAlignment.Center });
        return new Border { Width = 90, Height = 118, CornerRadius = new CornerRadius(7), Padding = new Thickness(5), Margin = new Thickness(5),
            BorderThickness = new Thickness(1 + (int)rarity * .4), BorderBrush = new SolidColorBrush(color),
            Background = new LinearGradientBrush(Color.FromRgb(19, 30, 44), Color.FromRgb((byte)(color.R / 4), (byte)(color.G / 4), (byte)(color.B / 4)), 45),
            Effect = rarity >= CollectionRarity.Rare ? new DropShadowEffect { Color = color, BlurRadius = 8 + (int)rarity * 3, Opacity = .35, ShadowDepth = 0 } : null, Child = stack };
    }

}
