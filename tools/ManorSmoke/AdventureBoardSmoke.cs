using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Desktop;

internal static class AdventureBoardSmoke
{
    internal static async Task Check(string output)
    {
        AdventureDefinition definition = AdventureCatalog.DeadMansHand;
        AdventureEngine engine = AdventureEngine.StartNew(definition, AdventureCharacterCatalog.Create(definition, "private-eye", "Morgan"));
        engine.State.CaseBoard.Layout.LayoutVersion = 2;
        engine.State.CaseBoard.Layout.Zoom = .28;
        engine.State.CaseBoard.Layout.PanX = -80;
        // Preserve one deliberately arranged node through the viewport repair.
        engine.State.CaseBoard.Layout.NodePositions["arthur-vane"] = new() { X = 180, Y = 150 };
        var view = new AdventureCaseBoardView(engine, _ => { });
        var window = new Window { Content = view, Width = 1440, Height = 900, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        try
        {
            window.Show(); await Task.Delay(90); window.UpdateLayout();
            Require(engine.State.CaseBoard.Layout.NodePositions["arthur-vane"].X == 180, "Viewport repair discarded hand-arranged coordinates");
            Require(engine.State.CaseBoard.Layout.LayoutVersion == 3, "Legacy camera was not repaired");
            foreach ((double width, double height) in new[] { (900d, 620d), (1440d, 900d), (1920d, 1080d) })
            {
                window.Width = width; window.Height = height; window.UpdateLayout();
                Find<Button>(view).Single(b => Equals(b.Content, "FIT")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                window.UpdateLayout();
                Border viewport = Find<Border>(view).Single(b => b.Name == "CaseBoardViewport");
                Grid surface = Find<Grid>(view).Single(g => g.Name == "CaseBoardSurface");
                Canvas cards = Find<Canvas>(view).Single(c => c.Name == "CaseBoardNodes");
                Require(viewport.ActualWidth > view.ActualWidth * .50 && viewport.ActualHeight > view.ActualHeight * .60, "Board viewport wastes the available surface");
                Require(surface.Parent is Canvas { Name: "CaseBoardWorldHost" }, "Oversized board still sits in a clipping layout slot");
                Geometry? layoutClip = LayoutInformation.GetLayoutClip(surface);
                Require(layoutClip is null || layoutClip.Bounds.Contains(new Rect(surface.RenderSize)), "World surface still has a smaller pre-transform layout clip");
                RenderTargetBitmap bitmap = Render(viewport);
                byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
                foreach (Border card in cards.Children.OfType<Border>())
                {
                    Rect bounds = card.TransformToAncestor(viewport).TransformBounds(new Rect(card.RenderSize));
                    Require(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= viewport.ActualWidth && bounds.Bottom <= viewport.ActualHeight, "FIT clipped a known node");
                    Require(bounds.Width >= 95, "Opening evidence is rendered as unreadable thumbnails");
                    Point sample = card.TranslatePoint(new Point(card.ActualWidth * .80, card.ActualHeight * .73), viewport);
                    int offset = ((int)sample.Y * bitmap.PixelWidth + (int)sample.X) * 4;
                    Require(pixels[offset + 2] > 140 && pixels[offset + 1] > 120, "Known card is behind a dark layout layer");
                }
                Save(Render(view), Path.Combine(output, $"case-board-{width:0}x{height:0}.png"));
            }
            var detached = new AdventureCaseBoardWindow(engine, _ => { });
            try
            {
                detached.Show(); await Task.Delay(100);
                engine.SelectChoice("take-case");
                await Task.Delay(700); detached.UpdateLayout();
                Require(Find<Border>(detached).Any(b => Equals(b.Tag, "hotel-mirabelle")), "Detached board failed to synchronize new story evidence");
                Save(Render((FrameworkElement)detached.Content), Path.Combine(output, "case-board-detached-live.png"));
            }
            finally { detached.Close(); }
            foreach (CaseNodeDefinition node in definition.CaseBoard!.Nodes)
                AdventureCaseBoard.DiscoverNode(definition.CaseBoard, engine.State.CaseBoard, node.Id, "qa");
            view.RefreshFromRunState(true);
            Find<Button>(view).Single(b => Equals(b.Content, "FRAME")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout(); Save(Render(view), Path.Combine(output, "case-board-all-evidence.png"));
            File.WriteAllText(Path.Combine(output, "case-board-result.txt"), "PASS: legacy camera repaired without losing card positions; large origin-anchored workspace; no pre-transform layout clipping; every opening card visibly rendered at three sizes; detached board follows story discoveries; full evidence workspace exported.");
        }
        finally { window.Close(); }
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T found) yield return found;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static RenderTargetBitmap Render(FrameworkElement element)
    {
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap(Math.Max(1, (int)element.ActualWidth), Math.Max(1, (int)element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element); return bitmap;
    }
    private static void Save(BitmapSource image, string path)
    {
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image)); using FileStream file = File.Create(path); png.Save(file);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
