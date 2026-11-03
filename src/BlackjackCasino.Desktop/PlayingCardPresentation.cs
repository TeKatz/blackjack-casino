using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class PlayingCardPresentation
{
    internal static Border Create(Card card, bool faceDown, bool compact, string? cardSetId)
    {
        CosmeticDefinition cosmetic = CosmeticCatalog.TryGet(cardSetId ?? CosmeticCatalog.DefaultCardSetId) is { Kind: CosmeticKind.CardSet } valid
            ? valid : CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId);
        var border = new Border
        {
            Width = compact ? 48 : 76, Height = compact ? 68 : 108,
            Margin = new Thickness(compact ? 2 : 4),
            CornerRadius = new CornerRadius(compact ? 5 : 7),
            ClipToBounds = true,
        };
        var grid = new Grid { Uid = faceDown ? "PlayingCardBack" : "PlayingCardFace", Margin = new Thickness(compact ? 5 : 8) };
        if (!faceDown)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.Children.Add(new TextBlock { Text = RankText(card.Rank), FontSize = compact ? 13 : 20, FontWeight = FontWeights.Bold });
            var suit = new TextBlock { Text = SuitText(card.Suit), FontSize = compact ? 22 : 34,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(suit, 1); grid.Children.Add(suit);
        }
        border.Child = grid;
        ApplyTheme(border, cosmetic);
        border.Tag = cosmetic.Id;
        MainWindow.RegisterCardStyling(border);
        return border;
    }

    // Shared by Blackjack, Solitaire and shop previews, including cards already on the table.
    internal static void ApplyTheme(Border card, CosmeticDefinition cosmetic)
    {
        PlayingCardTheme theme = PlayingCardTheme.Resolve(cosmetic.VisualVariant);
        bool compact = card.Width < 60;
        bool faceDown = card.Child is Grid { Uid: "PlayingCardBack" };
        card.BorderThickness = new Thickness(theme.DoubleFrame ? 2 : 1);
        card.BorderBrush = theme.Premium ? new LinearGradientBrush(new GradientStopCollection
        {
            new(theme.BackAccent, 0), new(theme.Border, .35), new(Colors.White, .47),
            new(theme.Ornament, .6), new(theme.Border, 1),
        }, 35) : new SolidColorBrush(theme.Border);
        card.Background = new LinearGradientBrush(faceDown ? theme.BackA : theme.FaceA, faceDown ? theme.BackB : theme.FaceB, faceDown ? 35 : 125);
        card.ToolTip = cosmetic.Name;
        if (card.Child is not Grid grid) return;
        foreach (FrameworkElement item in grid.Children.OfType<FrameworkElement>().Where(x => x.Uid == "CardSetOrnament").ToArray())
            grid.Children.Remove(item);
        if (faceDown)
        {
            AddDecoration(grid, new Border { Background = CardBackArtwork.Get(cosmetic.VisualVariant), CornerRadius = new CornerRadius(3), Opacity = .7 });
            AddDecoration(grid, new Border { BorderBrush = new SolidColorBrush(theme.BackAccent), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3) });
            AddDecoration(grid, new Border { BorderBrush = new SolidColorBrush(theme.BackAccent), BorderThickness = new Thickness(.6), Margin = new Thickness(compact ? 3 : 5), Opacity = .5, CornerRadius = new CornerRadius(2) });
            if (theme.Pattern == "eclipse")
            {
                AddDecoration(grid, new System.Windows.Shapes.Ellipse { Width = compact ? 19 : 30, Height = compact ? 19 : 30,
                    Fill = new SolidColorBrush(theme.BackB), Stroke = new SolidColorBrush(theme.BackAccent), StrokeThickness = 1.5,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            }
            else AddDecoration(grid, new TextBlock { Text = theme.Mark, FontSize = compact ? 20 : 31, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(theme.BackAccent), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        }
        else
        {
            TextBlock[] text = grid.Children.OfType<TextBlock>().ToArray();
            if (text.Length >= 2)
            {
                Brush ink = new SolidColorBrush(text[1].Text is "♥" or "♦" ? theme.RedInk : theme.BlackInk);
                text[0].Foreground = ink; text[1].Foreground = ink;
            }
            if (theme.DoubleFrame)
                AddDecoration(grid, new Border { BorderBrush = new SolidColorBrush(theme.Ornament), Opacity = .45,
                    BorderThickness = new Thickness(.6), CornerRadius = new CornerRadius(3), Margin = new Thickness(compact ? -2 : -4) }, behind: true);
            if (!compact && cosmetic.VisualVariant != "card-classic")
                AddDecoration(grid, new TextBlock { Text = theme.Mark, FontSize = 11, Foreground = new SolidColorBrush(theme.Ornament),
                    Opacity = .65, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom });
        }
        if (theme.Premium)
        {
            // Static reflected light: no per-frame redraws, blur effects or animation clocks per card.
            var light = new RadialGradientBrush(Color.FromArgb(55, theme.BackAccent.R, theme.BackAccent.G, theme.BackAccent.B), Colors.Transparent)
            { Center = new Point(.1, .05), GradientOrigin = new Point(.1, .05), RadiusX = 1.1, RadiusY = .8 };
            light.Freeze();
            AddDecoration(grid, new Border { Background = light, Margin = new Thickness(-3), CornerRadius = new CornerRadius(3) }, behind: true);
        }
    }

    private static void AddDecoration(Grid grid, FrameworkElement item, bool behind = false)
    {
        item.Uid = "CardSetOrnament"; item.IsHitTestVisible = false;
        Grid.SetRowSpan(item, Math.Max(1, grid.RowDefinitions.Count));
        if (behind) Panel.SetZIndex(item, -1);
        grid.Children.Add(item);
    }

    internal static FrameworkElement CreatePreview(CosmeticDefinition cosmetic)
    {
        var fan = new Grid { Width = 108, Height = 68 };
        Card[] cards = [new Card(Rank.Ace, Suit.Spades), new Card(Rank.Queen, Suit.Hearts), new Card(Rank.King, Suit.Clubs)];
        double[] angles = [-8, 0, 8];
        double[] x = [-28, 0, 28];
        for (int i = 0; i < cards.Length; i++)
        {
            Border card = Create(cards[i], faceDown: i == 2, compact: true, cosmetic.Id);
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
