using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class PortraitSmoke
{
    internal static async Task Check(string output)
    {
        using var controller = new PlayerProgressionController(new PlayerProfileState { Coins = 20_000 });
        var window = new ProgressionWindow(controller);
        try
        {
            window.Show();
            await Task.Delay(100);
            TabControl tabs = Descendants<TabControl>(window).First();
            tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (string)t.Header == "Cosmetic Shop");
            window.UpdateLayout();
            StackPanel shop = (StackPanel)window.FindName("ShopPanel");
            CosmeticDefinition[] portraits = CosmeticCatalog.ShopItems.Where(c => c.Kind == CosmeticKind.Portrait).OrderBy(c => c.Price).ToArray();
            Require(portraits.Length == 8, "Expected the eight premium shop portraits");
            var sources = new HashSet<ImageSource>();
            long spent = 0;
            foreach (CosmeticDefinition portrait in portraits)
            {
                BitmapSource image = PortraitArtwork.Get(portrait.Id) ?? throw new Exception("Missing bundled image: " + portrait.Id);
                Require(image.IsFrozen && image.PixelWidth == 512 && image.PixelHeight == 512, "Invalid decoded portrait " + portrait.Id);
                Require(ReferenceEquals(image, PortraitArtwork.Get(portrait.Id)), "Portrait decode cache was bypassed");
                Require(sources.Add(image), "Different portraits reused the same artwork");
                Border row = ShopRow(shop, portrait.Id);
                Ellipse preview = Descendants<Ellipse>(row).Single(e => e.Fill is ImageBrush);
                Require(ReferenceEquals(((ImageBrush)preview.Fill).ImageSource, image), "Shop preview uses the wrong image");
                CheckCircle(preview);
                Save(preview, System.IO.Path.Combine(output, portrait.Id + "-circle.png"), 3);
                if (portrait == portraits[0] || portrait == portraits[^1])
                {
                    row.BringIntoView(); await Task.Delay(70);
                    Save(window, System.IO.Path.Combine(output, portrait == portraits[0] ? "portraits-shop-first.png" : "portraits-shop-last.png"));
                }
                Button buy = Descendants<Button>(row).Single(b => Equals(b.Tag, portrait.Id));
                Require(buy.IsEnabled, "Portrait purchase is unexpectedly disabled");
                buy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                spent += portrait.Price;
                Require(controller.Profile.Coins == 20_000 - spent, "Purchase charged an incorrect amount");
                Require(controller.Profile.EquippedPortraitId == portrait.Id, "Purchased portrait did not equip");
                window.UpdateLayout();
            }
            using var profileController=new PlayerProgressionController(controller.Profile);
            var profileWindow=new PlayerProfileWindow(profileController);
            profileWindow.Show();profileWindow.UpdateLayout();await Task.Delay(70);
            Ellipse equipped = Descendants<Ellipse>(profileWindow).Single(e => e.Width == 112 && e.Fill is ImageBrush);
            Require(ReferenceEquals(((ImageBrush)equipped.Fill).ImageSource, PortraitArtwork.Get("portrait-velvet-king")), "Equipped portrait differs from its shop image");
            CheckCircle(equipped);
            Save(profileWindow, System.IO.Path.Combine(output, "portraits-player.png"));
            profileWindow.Width = profileWindow.MinWidth; profileWindow.Height = profileWindow.MinHeight;
            profileWindow.UpdateLayout(); Save(profileWindow, System.IO.Path.Combine(output, "portraits-player-minimum.png"));
            profileWindow.Close();
            Require(PortraitArtwork.Get(CosmeticCatalog.DefaultPortraitId) is null, "Default/season artwork was unexpectedly replaced");
            File.WriteAllText(System.IO.Path.Combine(output, "portraits-result.txt"), "PASS: all 8 embedded images loaded, distinct and cached; true circular clipping with transparent corners; actual shop previews, purchase/equip and Player portrait; regular/minimum window captures. No live profile was used.");
        }
        finally { window.Close(); }
    }

    private static Border ShopRow(StackPanel shop, string id) => shop.Children.OfType<Border>()
        .Single(row => Descendants<Button>(row).Any(button => Equals(button.Tag, id)));

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T found) yield return found;
            foreach (T nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static RenderTargetBitmap Render(FrameworkElement target, double scale = 1)
    {
        target.UpdateLayout();
        var drawing = new DrawingVisual();
        using (DrawingContext d = drawing.RenderOpen())
            d.DrawRectangle(new VisualBrush(target), null, new Rect(0, 0, target.ActualWidth, target.ActualHeight));
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(target.ActualWidth * scale), (int)Math.Ceiling(target.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        return bitmap;
    }

    private static void CheckCircle(FrameworkElement portrait)
    {
        RenderTargetBitmap image = Render(portrait);
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        foreach (int index in new[] { 0, image.PixelWidth - 1, (image.PixelHeight - 1) * image.PixelWidth, image.PixelWidth * image.PixelHeight - 1 })
            Require(pixels[index * 4 + 3] == 0, "Portrait corners are not circularly clipped");
        int centre = ((image.PixelHeight / 2) * image.PixelWidth + image.PixelWidth / 2) * 4;
        Require(pixels[centre + 3] == 255, "Portrait centre did not render");
    }

    private static void Save(FrameworkElement target, string path, double scale = 1)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(Render(target, scale)));
        using FileStream file = File.Create(path); encoder.Save(file);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
