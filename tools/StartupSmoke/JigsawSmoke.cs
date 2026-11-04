using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class JigsawSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        NavigationSmoke.OpenCovered(window, "chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();
        Button tile = Button(window, "LoungeGame_jigsaw");
        Require(tile.IsVisible, "Jigsaw tile is missing from Chill Lounge.");
        Click(tile);
        await Task.Delay(120);
        window.UpdateLayout();

        Require(Find<Button>(window).Count(b => b.Name.StartsWith("JigsawMotif_", StringComparison.Ordinal)) == 12,
            "The Jigsaw gallery did not offer all twelve themes.");
        Require(Find<Button>(window).Count(b => b.Name.StartsWith("JigsawSize_", StringComparison.Ordinal)) == 8,
            "The Jigsaw setup is missing one of the eight piece counts.");
        Capture(window, Path.Combine(output, "jigsaw-gallery.png"));

        Click(Button(window, "JigsawSize_1000"));
        await Task.Delay(90);
        window.UpdateLayout();
        Click(Button(window, "JigsawStart"));
        await Task.Delay(150);
        window.UpdateLayout();
        Require(Find<FrameworkElement>(window).Any(b => b.IsVisible && b.GetType().Name == "JigsawBoardSurface"),
            "Jigsaw did not open the interactive puzzle surface.");
        Require(Find<Button>(window).Any(b => b.Name == "JigsawNextTray"),
            "Jigsaw's paginated piece tray is missing.");
        Capture(window, Path.Combine(output, "jigsaw-board-1000.png"));

        Click(Button(window, "JigsawBackToMenu"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Button(window, "JigsawResume").IsVisible,
            "The puzzle was not resumable after leaving the board.");
        Click(Button(window, "JigsawResume"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<FrameworkElement>(window).Any(b => b.IsVisible && b.GetType().Name == "JigsawBoardSurface"),
            "Resuming a puzzle did not restore the board.");
        Click(Button(window, "LoungeGamesHeaderBack"));
        await Task.Delay(70);
        window.UpdateLayout();
        Require(Button(window, "LoungeGame_jigsaw").IsVisible,
            "Back-to-games did not restore the Chill Lounge menu.");
        Click(Find<Button>(window).Single(b => b.IsVisible && Equals(b.Content, "RETURN TO CLUB")));
        await Task.Delay(70);
        window.UpdateLayout();
        File.WriteAllText(Path.Combine(output, "jigsaw-result.txt"),
            "PASS: Jigsaw tile, gallery, eight piece counts, 1000-piece board, tray and resume.");
    }

    private static Button Button(MainWindow window, string name) =>
        Find<Button>(window).Single(x => x.Name == name && x.IsVisible);

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Capture(FrameworkElement element, string file)
    {
        element.UpdateLayout();
        int w = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int h = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T found) yield return found;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
            foreach (T element in Find<T>(VisualTreeHelper.GetChild(root, i)))
                yield return element;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
