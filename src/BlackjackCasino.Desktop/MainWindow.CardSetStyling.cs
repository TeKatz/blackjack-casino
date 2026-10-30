using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private bool cardSetStyleBusy;
    private bool cardSetStylingInitialized;

    private void InitializeCardSetStyling()
    {
        if (cardSetStylingInitialized) return;
        cardSetStylingInitialized = true;
        LayoutUpdated += (_, _) => ApplyEquippedCardSetToRenderedCards();
    }

    private void ApplyEquippedCardSetToRenderedCards()
    {
        if (cardSetStyleBusy || progressionController is null)
        {
            return;
        }

        string cardSetId = progressionController.Profile.EquippedCardSetId;
        CosmeticDefinition cosmetic = CosmeticCatalog.TryGet(cardSetId) is { Kind: CosmeticKind.CardSet } valid
            ? valid
            : CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId);

        cardSetStyleBusy = true;
        try
        {
            foreach (Border card in EnumerateVisuals<Border>(this).Where(IsPlayingCardBorder))
            {
                if (card.Tag is string applied && string.Equals(applied, cardSetId, StringComparison.Ordinal))
                {
                    continue;
                }

                RestyleCard(card, cosmetic);
                card.Tag = cardSetId;
            }
        }
        finally
        {
            cardSetStyleBusy = false;
        }
    }

    private static bool IsPlayingCardBorder(Border border)
    {
        bool normal = Math.Abs(border.Width - 76) < .1 && Math.Abs(border.Height - 108) < .1;
        bool compact = Math.Abs(border.Width - 48) < .1 && Math.Abs(border.Height - 68) < .1;
        if (!normal && !compact) return false;
        return border.Child is Grid || border.Child is TextBlock;
    }

    private static void RestyleCard(Border card, CosmeticDefinition cosmetic)
    {
        CardTheme theme = CardTheme.For(cosmetic.VisualVariant);
        bool compact = card.Width < 60;
        card.BorderBrush = new SolidColorBrush(theme.Border);
        card.BorderThickness = new Thickness(theme.DoubleFrame ? 2 : 1);
        card.Background = new LinearGradientBrush(theme.FaceA, theme.FaceB, 125);
        card.ToolTip = cosmetic.Name;

        if (card.Child is TextBlock backGlyph)
        {
            card.Background = new LinearGradientBrush(theme.BackA, theme.BackB, 35);
            backGlyph.Text = cosmetic.VisualVariant == "card-midnight-sovereign" ? "✦" : cosmetic.VisualVariant == "card-midnight-blue" ? "☾" : "♠";
            backGlyph.Foreground = new SolidColorBrush(theme.BackAccent);
            backGlyph.FontSize = compact ? 24 : 36;
            return;
        }

        if (card.Child is not Grid face)
        {
            return;
        }

        TextBlock[] text = face.Children.OfType<TextBlock>().ToArray();
        if (text.Length < 2)
        {
            return;
        }

        string suit = text[1].Text;
        bool red = suit is "♥" or "♦";
        var ink = new SolidColorBrush(red ? theme.RedInk : theme.BlackInk);
        text[0].Foreground = ink;
        text[1].Foreground = ink;

        foreach (FrameworkElement decoration in face.Children.OfType<FrameworkElement>().Where(x => Equals(x.Uid, "CardSetOrnament")).ToArray())
        {
            face.Children.Remove(decoration);
        }

        if (cosmetic.VisualVariant == "card-classic")
        {
            return;
        }

        if (theme.DoubleFrame)
        {
            var frame = new Border
            {
                Uid = "CardSetOrnament",
                BorderBrush = new SolidColorBrush(Color.FromArgb(115, theme.Ornament.R, theme.Ornament.G, theme.Ornament.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(compact ? 2 : 4),
                Margin = new Thickness(compact ? -2 : -4),
                IsHitTestVisible = false,
            };
            Panel.SetZIndex(frame, -1);
            face.Children.Add(frame);
        }

        if (!compact)
        {
            string mark = cosmetic.VisualVariant switch
            {
                "card-art-deco" => "◇",
                "card-crimson-velvet" => "◆",
                "card-emerald-house" => "♣",
                "card-midnight-blue" => "☾",
                "card-midnight-sovereign" => "✦",
                _ => string.Empty,
            };
            if (mark.Length > 0)
            {
                face.Children.Add(new TextBlock
                {
                    Uid = "CardSetOrnament",
                    Text = mark,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(theme.DarkFace ? (byte)120 : (byte)80, theme.Ornament.R, theme.Ornament.G, theme.Ornament.B)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    IsHitTestVisible = false,
                });
            }
        }
    }

    private static IEnumerable<T> EnumerateVisuals<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in EnumerateVisuals<T>(child)) yield return nested;
        }
    }

    private sealed record CardTheme(
        Color FaceA, Color FaceB, Color Border, Color BlackInk, Color RedInk,
        Color BackA, Color BackB, Color BackAccent, Color Ornament,
        bool DoubleFrame = false, bool DarkFace = false)
    {
        internal static CardTheme For(string variant) => variant switch
        {
            "card-art-deco" => new(Color.FromRgb(247,239,213),Color.FromRgb(228,216,181),Color.FromRgb(54,46,34),Color.FromRgb(28,28,26),Color.FromRgb(151,43,47),Color.FromRgb(27,29,31),Color.FromRgb(10,12,15),Color.FromRgb(207,171,90),Color.FromRgb(179,143,67),true),
            "card-crimson-velvet" => new(Color.FromRgb(250,239,224),Color.FromRgb(231,216,207),Color.FromRgb(128,57,68),Color.FromRgb(38,31,34),Color.FromRgb(139,35,50),Color.FromRgb(102,25,39),Color.FromRgb(41,13,22),Color.FromRgb(216,192,185),Color.FromRgb(168,91,102),true),
            "card-emerald-house" => new(Color.FromRgb(244,241,221),Color.FromRgb(218,228,205),Color.FromRgb(54,104,72),Color.FromRgb(25,45,34),Color.FromRgb(162,47,54),Color.FromRgb(18,75,49),Color.FromRgb(7,34,24),Color.FromRgb(225,192,99),Color.FromRgb(175,143,67),true),
            "card-midnight-blue" => new(Color.FromRgb(232,239,246),Color.FromRgb(203,216,232),Color.FromRgb(76,103,139),Color.FromRgb(27,43,67),Color.FromRgb(154,53,69),Color.FromRgb(26,51,88),Color.FromRgb(9,20,42),Color.FromRgb(184,207,233),Color.FromRgb(126,158,197),true),
            "card-midnight-sovereign" => new(Color.FromRgb(31,32,34),Color.FromRgb(13,15,18),Color.FromRgb(214,169,76),Color.FromRgb(239,217,166),Color.FromRgb(222,101,104),Color.FromRgb(16,16,19),Color.FromRgb(5,7,10),Color.FromRgb(242,198,96),Color.FromRgb(224,176,76),true,true),
            _ => new(Color.FromRgb(248,248,245),Color.FromRgb(238,238,232),Color.FromRgb(216,216,210),Color.FromRgb(23,26,24),Color.FromRgb(184,41,49),Color.FromRgb(22,61,41),Color.FromRgb(10,35,25),Color.FromRgb(212,168,79),Color.FromRgb(190,190,180)),
        };
    }
}
