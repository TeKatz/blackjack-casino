using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>Resolution-independent foil cards, with theme scenes and numbered constellation seals.</summary>
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

    internal static Border Card(CollectionCard card, bool owned, string? badge = null)
    {
        CollectionTheme theme = CollectionCatalog.Theme(card);
        Color foil = RarityColor(card.Rarity);
        Color accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
        var content = new Grid { ClipToBounds = true };
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
        content.RowDefinitions.Add(new RowDefinition());
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
        content.Children.Add(new TextBlock { Text = $"{(theme.UltraRare ? "✧ " : "")}{card.Rarity.ToString().ToUpperInvariant()}  ·  {card.Number:00}",
            FontSize = 9, Foreground = new SolidColorBrush(foil), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        FrameworkElement art = owned ? Scene(theme, card.Number) : new TextBlock { Text = "?", FontSize = 54, Foreground = new SolidColorBrush(Color.FromRgb(70, 83, 94)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(art, 1); content.Children.Add(art);
        var label = new StackPanel { Margin = new Thickness(7, 4, 7, 3), VerticalAlignment = VerticalAlignment.Center };
        label.Children.Add(new TextBlock { Text = card.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.WhiteSmoke, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
        label.Children.Add(new TextBlock { Text = badge ?? (owned ? theme.Name : Localization.T("Album.Undiscovered")), FontSize = 9, Margin = new Thickness(0, 4, 0, 0), Foreground = new SolidColorBrush(owned ? foil : Color.FromRgb(132, 146, 155)), TextAlignment = TextAlignment.Center });
        Grid.SetRow(label, 2); content.Children.Add(label);
        var border = new Border
        {
            Width = 142, Height = 210, Margin = new Thickness(8), Padding = new Thickness(3), CornerRadius = new CornerRadius(11),
            BorderThickness = new Thickness(owned ? 1 + (int)card.Rarity * .35 : 1),
            BorderBrush = owned ? new LinearGradientBrush(foil, accent, 40) : new SolidColorBrush(Color.FromRgb(53, 65, 75)),
            Background = new LinearGradientBrush(Color.FromRgb(20, 27, 43), owned ? Color.FromArgb(255, (byte)(accent.R / 6), (byte)(accent.G / 6), (byte)(accent.B / 6)) : Color.FromRgb(14, 20, 29), 70),
            Child = content, ToolTip = $"{card.Name} · {theme.Name} · {card.Rarity}" + (theme.UltraRare ? " · Ultra rare theme" : ""),
        };
        if (owned && card.Rarity >= CollectionRarity.Rare)
            border.Effect = new DropShadowEffect { Color = foil, BlurRadius = 8 + (int)card.Rarity * 3, ShadowDepth = 0, Opacity = theme.UltraRare ? .65 : .23 + (int)card.Rarity * .06 };
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

    private static FrameworkElement Scene(CollectionTheme theme, int number)
    {
        var canvas = new Canvas { Width = 126, Height = 104, ClipToBounds = true };
        Color color = (Color)ColorConverter.ConvertFromString(theme.Accent);
        var ink = new SolidColorBrush(color);
        // Deterministic constellation, different on every card without generating assets at runtime.
        for (int i = 0; i < number + 5; i++)
        {
            double x = 7 + (i * 37 + number * 13) % 112, y = 5 + (i * 23 + number * 17) % 90;
            var star = new Ellipse { Width = i % 3 == 0 ? 3 : 1.5, Height = i % 3 == 0 ? 3 : 1.5, Fill = ink, Opacity = .3 + (i % 3) * .2 };
            Canvas.SetLeft(star, x); Canvas.SetTop(star, y); canvas.Children.Add(star);
        }
        for (int i = 0; i < 2; i++)
        {
            var ring = new Ellipse { Width = 78 + 12 * i, Height = 78 + 12 * i, Stroke = ink, StrokeThickness = i == 0 ? 1 : .5, Opacity = .35, StrokeDashArray = i == 0 ? new DoubleCollection([2, 5]) : null };
            Canvas.SetLeft(ring, 24 - i * 6); Canvas.SetTop(ring, 11 - i * 6); canvas.Children.Add(ring);
        }
        string geometry = theme.Id switch
        {
            "neon-city" => "M 25,83 L25,48 39,48 39,28 51,28 51,55 60,55 60,13 75,13 75,41 86,41 86,32 100,32 100,83 Z",
            "midnight-manor" or "royal-court" or "eternal-casino" => "M29,81 L29,39 39,29 49,39 49,52 57,52 57,21 63,11 69,21 69,52 78,52 78,39 88,29 98,39 98,81 Z M55,81 L55,65 63,59 71,65 71,81",
            "enchanted-forest" or "sakura-garden" => "M63,15 L40,45 51,45 30,65 54,65 54,86 72,86 72,65 96,65 75,45 86,45 Z",
            "celestial-voyage" or "astral-sovereigns" => "M63,12 L73,39 102,40 79,58 87,86 63,69 39,86 47,58 24,40 53,39 Z",
            "ocean-depths" => "M24,68 C39,19 79,22 95,48 C69,35 59,61 87,75 C66,64 50,85 24,68 Z",
            "desert-gold" => "M24,81 L63,24 102,81 Z M63,24 L70,81 M36,67 L91,67",
            "frost-kingdom" => "M63,14 L63,86 M28,30 L98,70 M28,70 L98,30 M49,21 L63,34 77,21 M49,79 L63,66 77,79 M30,46 L45,40 43,24 M83,76 L81,60 96,54 M30,54 L45,60 43,76 M83,24 L81,40 96,46",
            "clockwork" => "M52,18 L74,18 78,29 91,31 101,48 93,58 96,70 78,83 66,77 54,83 36,70 39,58 31,48 41,31 54,29 Z M63,37 A14,14 0 1 0 63,65 A14,14 0 1 0 63,37",
            "pirate-cove" => "M63,18 L63,81 M42,36 L84,36 M29,60 C30,93 96,93 97,60 M29,60 L24,70 M97,60 L102,70 M56,20 A7,7 0 1 0 70,20 A7,7 0 1 0 56,20",
            "primordial-dragons" => "M30,80 L44,55 33,32 57,41 76,18 98,27 80,43 98,65 72,58 61,78 Z M78,29 L83,30",
            "mythic-relics" => "M42,23 L84,23 79,50 69,61 69,76 84,82 42,82 57,76 57,61 47,50 Z M43,29 C20,22 22,51 49,49 M83,29 C106,22 104,51 77,49",
            _ => "M63,15 C57,32 27,37 29,57 C30,76 50,77 60,63 L52,84 74,84 66,63 C76,77 96,76 97,57 C99,37 69,32 63,15 Z",
        };
        var path = new System.Windows.Shapes.Path { Data = Geometry.Parse(geometry), Stroke = ink, StrokeThickness = 1.6,
            Fill = new SolidColorBrush(Color.FromArgb(45, color.R, color.G, color.B)), StrokeLineJoin = PenLineJoin.Round };
        canvas.Children.Add(path);
        var seal = new TextBlock { Text = number.ToString("00"), FontSize = 9, Foreground = ink, FontWeight = FontWeights.Bold };
        Canvas.SetLeft(seal, 57); Canvas.SetTop(seal, 90); canvas.Children.Add(seal);
        return new Viewbox { Stretch = Stretch.Uniform, Child = canvas, Margin = new Thickness(3) };
    }
}
