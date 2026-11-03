using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class ShopCosmeticsSmoke
{
    internal static void Check(string output)
    {
        var gallery = new StackPanel { Background = new SolidColorBrush(Color.FromRgb(12, 23, 29)), Margin = new Thickness(12) };
        var backHashes = new HashSet<string>();
        foreach (CosmeticDefinition deck in CosmeticCatalog.All.Where(c => c.Kind == CosmeticKind.CardSet))
        {
            var row = Row(deck.Name);
            foreach (bool compact in new[] { false, true })
            {
                Border front = PlayingCardPresentation.Create(new Card(Rank.Queen, Suit.Hearts), false, compact, deck.Id);
                Border back = PlayingCardPresentation.Create(new Card(Rank.Ace, Suit.Spades), true, compact, deck.Id);
                row.Children.Add(front); row.Children.Add(back);
                int decorations = ((Grid)front.Child).Children.Count;
                PlayingCardPresentation.ApplyTheme(front, CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId));
                PlayingCardPresentation.ApplyTheme(front, deck);
                Require(((Grid)front.Child).Children.Count == decorations, "Deck restyling accumulated ornaments: " + deck.Id);
                Require(CardBackArtwork.Get(deck.VisualVariant).IsFrozen, "Card back brush was not frozen");
                Require(ReferenceEquals(CardBackArtwork.Get(deck.VisualVariant), CardBackArtwork.Get(deck.VisualVariant)), "Card back brush cache missed");
                PlayingCardPresentation.ApplyTheme(back, CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId));
                PlayingCardPresentation.ApplyTheme(back, deck);
                Require(((Grid)back.Child).Uid == "PlayingCardBack", "Deck restyling exposed a hidden card");
                if (!compact)
                {
                    RenderTargetBitmap bitmap = Render(back, 76, 108);
                    byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
                    bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
                    Require(backHashes.Add(Convert.ToHexString(SHA256.HashData(pixels))), "Deck backs reused the same appearance: " + deck.Id);
                }
            }
            gallery.Children.Add(row);
        }
        Save(Render(gallery, 700, 1450), Path.Combine(output, "shop-cardsets-gallery.png"));

        gallery.Children.Clear();
        var diceHashes = new HashSet<string>();
        foreach (CosmeticDefinition dice in CosmeticCatalog.All.Where(c => c.Kind == CosmeticKind.DiceSet))
        {
            var row = Row(dice.Name);
            row.Children.Add(new KniffelDie { Width = 80, Height = 80, Value = 5, VisualVariant = dice.VisualVariant });
            row.Children.Add(new KniffelDie { Width = 64, Height = 64, Value = 6, Held = true, VisualVariant = dice.VisualVariant });
            var d20 = new AdventureD20 { Width = 88, Height = 88, Value = 20, VisualVariant = dice.VisualVariant };
            row.Children.Add(d20);
            RenderTargetBitmap bitmap = Render(d20, 88, 88);
            byte[] pixels = new byte[88 * 88 * 4]; bitmap.CopyPixels(pixels, 88 * 4, 0);
            Require(diceHashes.Add(Convert.ToHexString(SHA256.HashData(pixels))), "D20 palette fell back to another set: " + dice.Id);
            gallery.Children.Add(row);
        }
        Save(Render(gallery, 700, 1350), Path.Combine(output, "shop-dicesets-gallery.png"));
        foreach (string variant in new[] { "dice-ruby", "dice-aurora", "dice-astral-opal", "dice-solar-crown" })
            Require(DiceAppearance.For(variant).Glow is not null, "Missing premium dice glow: " + variant);
        File.WriteAllText(Path.Combine(output, "shop-cosmetics-result.txt"), "PASS: all card fronts/backs render at regular and compact sizes; restyling preserves hidden cards and does not accumulate ornaments; back brushes are frozen and cached; every D20 palette is distinct; premium glows present; gallery exports captured.");
    }

    private static StackPanel Row(string name)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6) };
        row.Children.Add(new TextBlock { Text = name, Foreground = Brushes.AntiqueWhite, Width = 200, FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    private static RenderTargetBitmap Render(FrameworkElement element, int width, int height)
    {
        element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height)); element.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element); return bitmap;
    }
    private static void Save(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path); encoder.Save(stream);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
