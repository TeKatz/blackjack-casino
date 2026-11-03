using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class BackroomLoungeSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        NavigationSmoke.OpenCovered(window, "chill-lounge");
        await Task.Delay(130);
        window.UpdateLayout();

        Button tile = Find<Button>(window).Single(button =>
            button.Name == "LoungeGame_tales_backroom" && button.IsVisible);
        Click(tile);
        await Task.Delay(120);
        window.UpdateLayout();

        FrameworkElement hub = Find<FrameworkElement>(window).Single(element =>
            element.IsVisible && element.GetType().Name == "AdventureLibraryHubControl");
        Button start = Find<Button>(hub).Single(button =>
            button.Name == "AdventureBeginNewRun" && button.IsVisible);
        Button music = Find<Button>(hub).Single(button =>
            button.Name == "AdventureMusicToggle" && button.IsVisible);
        Button selection = Find<Button>(hub).First(button =>
            button.Name.StartsWith("AdventureSelect_", StringComparison.Ordinal) && button.IsVisible);

        Require(Find<ComboBox>(hub).Any(combo => combo.IsVisible && combo.IsEnabled),
            "The Tales hub lost its working character-class selector.");
        Require(Find<Slider>(hub).Any(slider => slider.IsVisible),
            "The Tales hub lost its live soundtrack volume slider.");

        double startX = start.TranslatePoint(new Point(0, 0), hub).X;
        double musicX = music.TranslatePoint(new Point(0, 0), hub).X;
        double selectedX = selection.TranslatePoint(new Point(0, 0), hub).X;
        Require(startX < selectedX && musicX < selectedX,
            $"Tales character/music controls are not left of the adventure library: {startX}, {musicX}, {selectedX}.");

        Require(!Find<ScrollViewer>(hub).Any(scroll =>
            scroll.IsVisible &&
            scroll.ActualHeight > 200 &&
            scroll.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled),
            "The Tales menu still needs a vertical scrollbar at the desktop smoke resolution.");

        foreach (FrameworkElement control in new FrameworkElement[] { start, music, selection })
        {
            Point top = control.TranslatePoint(new Point(0, 0), hub);
            Require(top.Y >= -1 && top.Y + control.ActualHeight <= hub.ActualHeight + 2,
                $"The Tales menu clipped {control.Name} vertically at the desktop smoke resolution.");
        }

        hub.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (FileStream file = File.Create(Path.Combine(output, "backroom-hub.png")))
        {
            encoder.Save(file);
        }

        Button games = Find<Button>(window).Single(button =>
            button.Name == "LoungeGamesHeaderBack" && button.IsVisible);
        Click(games);
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button =>
            button.Name == "LoungeGame_tales_backroom" && button.IsVisible),
            "The Tales hub failed to return to the Chill Lounge games screen.");

        Click(Find<Button>(window).Single(button =>
            button.IsVisible && Equals(button.Content, "RETURN TO CLUB")));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "Mode_chill_lounge" && button.IsVisible),
            "Returning from Tales did not restore the Casino lobby.");
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            foreach (T nested in Find<T>(VisualTreeHelper.GetChild(root, index)))
                yield return nested;
        }
    }

    private static void Click(Button button)
        => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
