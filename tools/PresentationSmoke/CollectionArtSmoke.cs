using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Ellipse = System.Windows.Shapes.Ellipse;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckCollectionArt(MainWindow window, PlayerProfileState profile)
    {
        var pixels = new HashSet<string>(StringComparer.Ordinal);
        var perceptual = new List<(string Id, ulong Hash)>();
        foreach (CollectionCard card in CollectionCatalog.Cards)
        {
            BitmapSource image = CollectionIllustrations.ImageFor(card);
            Require(image.IsFrozen && image.PixelWidth >= 200 && image.PixelHeight >= 300, "Missing or undersized illustration: " + card.Id);
            var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            int stride = converted.PixelWidth * 4;
            var buffer = new byte[stride * converted.PixelHeight];
            converted.CopyPixels(buffer, stride, 0);
            Require(pixels.Add(Convert.ToHexString(SHA256.HashData(buffer))), "Duplicate illustration pixels: " + card.Id);
            ulong hash = IllustrationHash(image);
            foreach (var other in perceptual)
                Require(BitOperations.PopCount(hash ^ other.Hash) > 3, "Near-duplicate illustration composition: " + card.Id + " / " + other.Id);
            perceptual.Add((card.Id, hash));
        }
        Require(pixels.Count == 136 && CollectionIllustrations.All.Count == 136, "Not every card has unique artwork");

        // Exercise the real dedicated album inspector without mutating ownership.
        string ownedId = profile.Collection.OwnedCardIds.First();
        CollectionCard owned = CollectionCatalog.Find(ownedId)!;
        Find<Button>(Find<StackPanel>(window).Single(p => p.Name == "AlbumThemes"))
            .Single(b => Equals(b.Tag, owned.ThemeId)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Button selected = Find<Button>(window).Single(b => Equals(b.Tag, "inspect:" + ownedId));
        selected.Focus();
        selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout(); await Task.Delay(50);
        Border inspector = Find<Border>(window).Single(b => b.Name == "AlbumArtOverlay");
        Require(inspector.IsVisible, "Collected card did not open its artwork inspector");
        Image inspectorImage = Find<Image>(inspector).Single(i => i.Name == "CollectionCardArt");
        Require(inspectorImage.Tag as string == ownedId, "Inspector displays another card");
        Require(RenderOptions.GetBitmapScalingMode(inspectorImage) == BitmapScalingMode.HighQuality, "Inspector illustration is not using high-quality bitmap scaling");
        Require(!Find<Viewbox>(inspector).Any(), "Inspector still enlarges the small card through a Viewbox");
        Border inspectorCard = Find<Border>(inspector).Single(b => b.Name == "CollectionCard" && Equals(b.Tag, ownedId));
        Require(inspectorCard.Width >= 320 && inspectorCard.Height >= 490, "Inspector did not render a native large card");

        // Large blurred ellipses caused the inspector and the rest of the WPF menu to fall onto an
        // expensive effect render path. The background is now panel-aligned radial gradients instead.
        Require(!Find<Ellipse>(inspector).Any(e => e.Effect is BlurEffect), "Artwork inspector reintroduced a large BlurEffect glow");
        Grid browse = Find<Grid>(window).Single(g => g.Name == "AlbumBrowseSurface");
        Require(browse.Visibility == Visibility.Collapsed, "Album browser is still rendering underneath the artwork inspector");

        foreach (var size in new[] { (1440d, 900d), (1920d, 1080d) })
        {
            window.Width = size.Item1; window.Height = size.Item2; window.UpdateLayout(); await Task.Delay(30);
            RequireWithin(window, Find<Button>(inspector).Single(b => b.Name == "AlbumArtClose"), "Artwork inspector close");
            RequireWithin(window, inspectorImage, "Artwork inspector illustration");
            RequireWithin(window, inspectorCard, "Artwork inspector card");
            Capture(window, "album-art-inspector-" + (int)size.Item1);
        }
        Button close = Find<Button>(inspector).Single(b => b.Name == "AlbumArtClose");
        close.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        window.UpdateLayout();
        await Task.Delay(35);
        Require(!inspector.IsVisible, "Escape did not close the artwork inspector");
        Require(browse.Visibility == Visibility.Visible, "Album browser did not resume after closing the inspector");
        Require(selected.IsVisible && selected.IsEnabled && selected.IsKeyboardFocused,
            $"Escape focus state invalid: visible={selected.IsVisible}, enabled={selected.IsEnabled}, keyboardFocused={selected.IsKeyboardFocused}, focusedElement={Keyboard.FocusedElement?.GetType().Name ?? "null"}");
        Require(Find<Grid>(window).Any(g => g.Name == "CollectionAlbumRoot" && g.IsVisible), "Escape closed the dedicated album page");

        var root = (Grid)window.Content;
        var host = new Border { Name = "CollectionArtQA", Background = new SolidColorBrush(Color.FromRgb(6, 12, 20)), Padding = new Thickness(18) };
        Grid.SetRowSpan(host, 2); Grid.SetColumnSpan(host, 2); Panel.SetZIndex(host, int.MaxValue); root.Children.Add(host);
        try
        {
            window.Width = 1440; window.Height = 900;
            foreach (CollectionTheme theme in CollectionCatalog.Themes)
            {
                var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                stack.Children.Add(new TextBlock { Text = theme.Name + " · ILLUSTRATED EDITION", Foreground = Brushes.WhiteSmoke, FontFamily = new FontFamily("Georgia"), FontSize = 25, Margin = new Thickness(8, 0, 0, 18) });
                var grid = new UniformGrid { Columns = theme.UltraRare ? 4 : 5, Width = (theme.UltraRare ? 4 : 5) * 180 };
                foreach (CollectionCard card in theme.Cards)
                    grid.Children.Add(CollectionArtwork.Card(card, true, animationsEnabled: false));
                stack.Children.Add(grid); host.Child = stack;
                window.UpdateLayout(); await Task.Delay(30);
                Require(!Find<Border>(host).Where(b => b.Name == "CollectionFoil").Any(b => b.RenderTransform.HasAnimatedProperties), "Reduced-motion cards still animate");
                Capture(window, "album-art-" + theme.Id);
            }
            var editions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            foreach (CollectionRarity rarity in Enum.GetValues<CollectionRarity>())
                editions.Children.Add(CollectionArtwork.Card(CollectionCatalog.Themes[0].Cards.First(c => c.Rarity == rarity), true, animationsEnabled: false));
            editions.Children.Add(CollectionArtwork.Card(CollectionCatalog.Themes.First(t => t.UltraRare).Cards[0], true, animationsEnabled: false));
            host.Child = editions; window.UpdateLayout(); await Task.Delay(40); Capture(window, "album-art-rarity-editions");

            foreach (CollectionRarity rarity in new[] { CollectionRarity.Rare, CollectionRarity.Epic, CollectionRarity.Legendary })
            foreach (double scale in new[] { 1d, 2.08d })
            {
                CollectionCard edition = CollectionCatalog.Themes[0].Cards.First(c => c.Rarity == rarity);
                Border testCard = CollectionArtwork.Card(edition, true, animationsEnabled: false, scale: scale);
                host.Child = testCard; window.UpdateLayout();
                Grid surface = (Grid)testCard.Child;
                Border[] inlays = Find<Border>(surface).Where(b => b.Name.StartsWith("CollectionCorner", StringComparison.Ordinal)).ToArray();
                Require(inlays.Length == 4, "Premium card lost its four corner inlays.");
                Rect[] bounds = inlays.Select(b => b.TransformToAncestor(surface).TransformBounds(new Rect(b.RenderSize))).ToArray();
                Require(Math.Abs(bounds[0].Left - bounds[2].Left) < 1 && Math.Abs(bounds[1].Right - bounds[3].Right) < 1, "Corner inlays have mismatched horizontal anchors.");
                Require(Math.Abs(bounds[0].Left - (surface.ActualWidth - bounds[1].Right)) < 1, "Premium corner insets are asymmetric.");
                Require(Math.Abs(bounds[0].Top - (surface.ActualHeight - bounds[2].Bottom)) < 1, "Premium vertical corner insets are asymmetric.");
            }

            CollectionCard legendary = CollectionCatalog.Themes[0].Cards[^1];
            Border animated = CollectionArtwork.Card(legendary, true, animationsEnabled: true);
            host.Child = animated; window.UpdateLayout(); await Task.Delay(50);
            Border foil = Find<Border>(animated).Single(b => b.Name == "CollectionFoil");
            Require(foil.RenderTransform.HasAnimatedProperties, "Visible Legendary card has no foil animation");
            animated.Visibility = Visibility.Collapsed; await Task.Delay(30);
            Require(!foil.RenderTransform.HasAnimatedProperties, "Hidden card kept an animation clock");
            animated.Visibility = Visibility.Visible; await Task.Delay(30);
            Require(foil.RenderTransform.HasAnimatedProperties, "Visible card did not resume foil animation");
            host.Child = null; window.UpdateLayout(); await Task.Delay(40);
            Require(!foil.RenderTransform.HasAnimatedProperties, "Unloaded card kept an animation clock");
            File.WriteAllText(Path.Combine(output, "album-art-result.txt"), "PASS: 136 distinct decoded illustrations and perceptual compositions; full catalog mapping; all 16 theme galleries; all rarity finishes; dedicated high-quality inspector and Escape; no expensive blur glows behind the inspector; hidden album browser; reduced motion; visible/hidden/unloaded foil lifecycle.");
        }
        finally { host.Child = null; root.Children.Remove(host); window.UpdateLayout(); }
    }

    private static ulong IllustrationHash(BitmapSource image)
    {
        var drawing = new DrawingVisual();
        using (DrawingContext dc = drawing.RenderOpen()) dc.DrawImage(image, new Rect(0, 0, 9, 8));
        var thumb = new RenderTargetBitmap(9, 8, 96, 96, PixelFormats.Pbgra32); thumb.Render(drawing);
        var data = new byte[9 * 8 * 4]; thumb.CopyPixels(data, 9 * 4, 0);
        int Luma(int x, int y) { int i = (y * 9 + x) * 4; return data[i] * 114 + data[i+1] * 587 + data[i+2] * 299; }
        ulong result = 0;
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (Luma(x, y) > Luma(x+1, y)) result |= 1UL << (y*8+x);
        return result;
    }
}
