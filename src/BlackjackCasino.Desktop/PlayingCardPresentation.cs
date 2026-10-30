using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class PlayingCardPresentation
{
    private sealed record Palette(
        Color FaceA,
        Color FaceB,
        Color Border,
        Color BlackInk,
        Color RedInk,
        Color BackA,
        Color BackB,
        Color BackAccent,
        Color Ornament,
        bool DoubleFrame = false,
        bool DarkFace = false);

    internal static Border Create(Card card, bool faceDown, bool compact, string? cardSetId)
    {
        double width = compact ? 48 : 76;
        double height = compact ? 68 : 108;
        double rankSize = compact ? 13 : 20;
        double suitSize = compact ? 22 : 34;
        CosmeticDefinition? cosmetic = CosmeticCatalog.TryGet(cardSetId ?? CosmeticCatalog.DefaultCardSetId);
        Palette palette = Resolve(cosmetic?.VisualVariant);

        var border = new Border
        {
            Width = width,
            Height = height,
            Margin = new Thickness(compact ? 2 : 4),
            CornerRadius = new CornerRadius(compact ? 5 : 7),
            BorderThickness = new Thickness(palette.DoubleFrame ? 2 : 1),
            BorderBrush = new SolidColorBrush(palette.Border),
            Background = new LinearGradientBrush(palette.FaceA, palette.FaceB, 125),
            ClipToBounds = true,
        };

        if (faceDown)
        {
            border.Background = new LinearGradientBrush(palette.BackA, palette.BackB, 35);
            var back = new Grid { Margin = new Thickness(compact ? 4 : 6) };
            back.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(compact ? 3 : 5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(175, palette.BackAccent.R, palette.BackAccent.G, palette.BackAccent.B)),
                Margin = new Thickness(1),
            });
            back.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(compact ? 2 : 4),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, palette.BackAccent.R, palette.BackAccent.G, palette.BackAccent.B)),
                Margin = new Thickness(compact ? 4 : 7),
            });
            back.Children.Add(new TextBlock
            {
                Text = cosmetic?.VisualVariant == "card-midnight-sovereign" ? "✦" : "♠",
                FontSize = compact ? 20 : 31,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(palette.BackAccent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
            border.Child = back;
            border.ToolTip = cosmetic?.Name ?? "Classic Cards";
            return border;
        }

        bool red = card.Suit is Suit.Hearts or Suit.Diamonds;
        Brush ink = new SolidColorBrush(red ? palette.RedInk : palette.BlackInk);
        var grid = new Grid { Margin = new Thickness(compact ? 5 : 8) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());

        if (palette.DoubleFrame)
        {
            grid.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, palette.Ornament.R, palette.Ornament.G, palette.Ornament.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(compact ? 2 : 4),
                Margin = new Thickness(compact ? -2 : -4),
                IsHitTestVisible = false,
            });
        }

        var rank = new TextBlock
        {
            Text = RankText(card.Rank),
            FontSize = rankSize,
            FontWeight = FontWeights.Bold,
            Foreground = ink,
        };
        grid.Children.Add(rank);

        var suit = new TextBlock
        {
            Text = SuitText(card.Suit),
            FontSize = suitSize,
            Foreground = ink,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(suit, 1);
        grid.Children.Add(suit);

        if (!compact && cosmetic?.VisualVariant is not "card-classic")
        {
            string mark = cosmetic?.VisualVariant switch
            {
                "card-art-deco" => "◇",
                "card-crimson-velvet" => "◆",
                "card-emerald-house" => "♣",
                "card-midnight-blue" => "☾",
                "card-midnight-sovereign" => "✦",
                _ => "",
            };
            if (mark.Length > 0)
            {
                grid.Children.Add(new TextBlock
                {
                    Text = mark,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(palette.DarkFace ? (byte)90 : (byte)65, palette.Ornament.R, palette.Ornament.G, palette.Ornament.B)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                });
            }
        }

        border.Child = grid;
        border.ToolTip = cosmetic?.Name ?? "Classic Cards";
        return border;
    }

    internal static FrameworkElement CreatePreview(CosmeticDefinition cosmetic)
    {
        var fan = new Grid { Width = 108, Height = 68 };
        Card[] cards = [new Card(Rank.Ace, Suit.Spades), new Card(Rank.Queen, Suit.Hearts), new Card(Rank.King, Suit.Clubs)];
        double[] angles = [-8, 0, 8];
        double[] x = [-28, 0, 28];
        for (int i = 0; i < cards.Length; i++)
        {
            Border card = Create(cards[i], faceDown: false, compact: true, cosmetic.Id);
            card.Width = 42;
            card.Height = 60;
            card.Margin = new Thickness(0);
            card.RenderTransformOrigin = new Point(.5, .85);
            card.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new RotateTransform(angles[i]),
                    new TranslateTransform(x[i], i == 1 ? -3 : 2),
                },
            };
            fan.Children.Add(card);
        }
        return fan;
    }

    private static Palette Resolve(string? variant) => variant switch
    {
        "card-art-deco" => new(
            Color.FromRgb(247, 239, 213), Color.FromRgb(228, 216, 181), Color.FromRgb(54, 46, 34),
            Color.FromRgb(28, 28, 26), Color.FromRgb(151, 43, 47), Color.FromRgb(27, 29, 31), Color.FromRgb(10, 12, 15),
            Color.FromRgb(207, 171, 90), Color.FromRgb(179, 143, 67), DoubleFrame: true),
        "card-crimson-velvet" => new(
            Color.FromRgb(250, 239, 224), Color.FromRgb(231, 216, 207), Color.FromRgb(128, 57, 68),
            Color.FromRgb(38, 31, 34), Color.FromRgb(139, 35, 50), Color.FromRgb(102, 25, 39), Color.FromRgb(41, 13, 22),
            Color.FromRgb(216, 192, 185), Color.FromRgb(168, 91, 102), DoubleFrame: true),
        "card-emerald-house" => new(
            Color.FromRgb(244, 241, 221), Color.FromRgb(218, 228, 205), Color.FromRgb(54, 104, 72),
            Color.FromRgb(25, 45, 34), Color.FromRgb(162, 47, 54), Color.FromRgb(18, 75, 49), Color.FromRgb(7, 34, 24),
            Color.FromRgb(225, 192, 99), Color.FromRgb(175, 143, 67), DoubleFrame: true),
        "card-midnight-blue" => new(
            Color.FromRgb(232, 239, 246), Color.FromRgb(203, 216, 232), Color.FromRgb(76, 103, 139),
            Color.FromRgb(27, 43, 67), Color.FromRgb(154, 53, 69), Color.FromRgb(26, 51, 88), Color.FromRgb(9, 20, 42),
            Color.FromRgb(184, 207, 233), Color.FromRgb(126, 158, 197), DoubleFrame: true),
        "card-midnight-sovereign" => new(
            Color.FromRgb(31, 32, 34), Color.FromRgb(13, 15, 18), Color.FromRgb(214, 169, 76),
            Color.FromRgb(239, 217, 166), Color.FromRgb(222, 101, 104), Color.FromRgb(16, 16, 19), Color.FromRgb(5, 7, 10),
            Color.FromRgb(242, 198, 96), Color.FromRgb(224, 176, 76), DoubleFrame: true, DarkFace: true),
        _ => new(
            Color.FromRgb(248, 248, 245), Color.FromRgb(238, 238, 232), Color.FromRgb(216, 216, 210),
            Color.FromRgb(23, 26, 24), Color.FromRgb(184, 41, 49), Color.FromRgb(22, 61, 41), Color.FromRgb(10, 35, 25),
            Color.FromRgb(212, 168, 79), Color.FromRgb(190, 190, 180)),
    };

    private static string RankText(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(CultureInfo.InvariantCulture),
    };

    private static string SuitText(Suit suit) => suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?",
    };
}
