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

    internal static Border Card(CollectionCard card, bool owned, string? badge = null, bool animationsEnabled = true, double scale = 1d)
    {
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        double S(double value) => value * scale;
        Thickness T(double uniform) => new(S(uniform));
        Thickness T4(double left, double top, double right, double bottom) => new(S(left), S(top), S(right), S(bottom));

        CollectionTheme theme = CollectionCatalog.Theme(card);
        Color foil = RarityColor(card.Rarity);
        bool premium = card.Rarity >= CollectionRarity.Rare;
        var content = new Grid { ClipToBounds = true, UseLayoutRounding = true, SnapsToDevicePixels = true };
        if (owned)
        {
            var image = new Image
            {
                Name = "CollectionCardArt",
                Tag = card.Id,
                Source = CollectionIllustrations.ImageFor(card),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            content.Children.Add(image);
        }
        else
        {
            content.Background = new LinearGradientBrush(Color.FromRgb(19, 31, 39), Color.FromRgb(5, 12, 21), 70);
            content.Children.Add(new TextBlock { Text = theme.Symbol, FontSize = S(60), Opacity = .16, Foreground = new SolidColorBrush(foil), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        }
        // Light falloff keeps typography legible without boxing the illustration into an icon slot.
        content.Children.Add(new Border { IsHitTestVisible = false, Background = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromArgb(190, 3, 8, 15), 0), new(Color.FromArgb(20, 3, 8, 15), .22),
            new(Colors.Transparent, .48), new(Color.FromArgb(130, 3, 8, 15), .68), new(Color.FromArgb(248, 3, 8, 15), 1),
        }, 90) });
        var header = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = T4(9, 8, 9, 0) };
        header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = card.Rarity.ToString().ToUpperInvariant(), FontSize = S(9), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(foil) });
        var number = new TextBlock { Text = $"{card.Number:00} / {theme.Cards.Count:00}", FontSize = S(9), Foreground = new SolidColorBrush(foil) };
        Grid.SetColumn(number, 1); header.Children.Add(number); content.Children.Add(header);
        var label = new StackPanel { Margin = T4(10, 0, 10, 10), VerticalAlignment = VerticalAlignment.Bottom };
        label.Children.Add(new TextBlock { Text = theme.UltraRare ? "✧  " + Localization.T("Album.UltraRare") + "  ✧" : new string('◆', 1 + (int)card.Rarity), FontSize = S(8), Foreground = new SolidColorBrush(foil), Margin = T4(0, 0, 0, 5), HorizontalAlignment = HorizontalAlignment.Center });
        label.Children.Add(new TextBlock { Text = card.Name, FontFamily = new FontFamily("Georgia"), FontSize = S(15), FontWeight = FontWeights.SemiBold, Foreground = Brushes.WhiteSmoke, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
        label.Children.Add(new TextBlock { Text = badge ?? (owned ? theme.Name : Localization.T("Album.Undiscovered")), FontSize = S(8.5), Margin = T4(0, 6, 0, 0), Foreground = new SolidColorBrush(owned ? foil : Color.FromRgb(150, 161, 171)), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(label);
        // Fine metal inlays stay inside the card. Rarer editions have additional corner engraving.
        content.Children.Add(new Border { IsHitTestVisible = false, Margin = T(3), BorderThickness = new Thickness(S(.6)), BorderBrush = new SolidColorBrush(Color.FromArgb(owned ? (byte)140 : (byte)45, foil.R, foil.G, foil.B)), CornerRadius = new CornerRadius(S(7)) });
        if (owned && premium)
        {
            var corners = new System.Windows.Shapes.Path { IsHitTestVisible = false, Stroke = new SolidColorBrush(foil), StrokeThickness = S(theme.UltraRare ? 1.4 : .8),
                Data = Geometry.Parse("M 8,31 L8,8 31,8 M 131,8 L154,8 154,31 M 8,211 L8,234 31,234 M 131,234 L154,234 154,211"), Opacity = .75,
                RenderTransform = new ScaleTransform(scale, scale), RenderTransformOrigin = new Point(0, 0) };
            content.Children.Add(corners);
            content.Children.Add(CollectionFoil.Create(card, animationsEnabled));
        }
        var metal = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromRgb(27, 33, 43), 0), new(foil, .18), new(Color.FromRgb(76, 68, 52), .42),
            new(Color.FromRgb(255, 242, 202), .58), new(foil, .73), new(Color.FromRgb(28, 32, 42), 1),
        }, 42);
        var border = new Border { Name = "CollectionCard", Tag = card.Id, Width = S(164), Height = S(248), Margin = T(8), Padding = T(2), CornerRadius = new CornerRadius(S(11)),
            BorderThickness = new Thickness(S(owned ? 1.2 + (int)card.Rarity * .28 : .8)), BorderBrush = owned ? metal : new SolidColorBrush(Color.FromRgb(56, 66, 80)), Background = Brushes.Black,
            Child = content, ToolTip = $"{card.Name} · {theme.Name} · {card.Rarity}" + (theme.UltraRare ? " · " + Localization.T("Album.UltraRare") : ""), UseLayoutRounding = true, SnapsToDevicePixels = true };
        if (owned && premium)
            border.Effect = new DropShadowEffect { Color = foil, BlurRadius = S(theme.UltraRare ? 20 : 8 + (int)card.Rarity * 2), ShadowDepth = 0, Opacity = theme.UltraRare ? .46 : .18 + (int)card.Rarity * .035 };
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
