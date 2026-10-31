using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class MemorySmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();

        Button tile = Find<Button>(window).Single(button => button.Name == "LoungeGame_memory" && button.IsVisible);
        Click(tile);
        await Task.Delay(70);
        window.UpdateLayout();

        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Memory"),
            "Memory hub did not open from the Chill Lounge menu.");
        Require(Find<Button>(window).Count(button => button.IsVisible && button.Name.StartsWith("MemoryStart_", StringComparison.Ordinal)) == 5,
            "Memory hub does not expose all five difficulty tables.");
        Require(Find<ComboBox>(window).Any(combo => combo.Name == "MemoryDeckPicker" && combo.IsVisible),
            "Memory hub is missing the card deck selector.");
        Capture(window, Path.Combine(output, "memory-hub.png"));

        Button easy = Find<Button>(window).Single(button => button.Name == "MemoryStart_Easy" && button.IsVisible);
        Click(easy);
        await Task.Delay(80);
        window.UpdateLayout();

        Button[] cards = Find<Button>(window)
            .Where(button => button.IsVisible && button.Name.StartsWith("MemoryCard_", StringComparison.Ordinal))
            .OrderBy(button => button.Name, StringComparer.Ordinal)
            .ToArray();
        Require(cards.Length == 16, $"Easy Memory did not render a 4x4 board; found {cards.Length} card buttons.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("0 / 8 pairs", StringComparison.Ordinal)),
            "Easy Memory did not start with an eight-pair progress counter.");

        Click(cards[0]);
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("One card open", StringComparison.Ordinal)),
            "Flipping the first Memory card did not update the table feedback.");
        Capture(window, Path.Combine(output, "memory-game.png"));

        Click(Find<Button>(window).Single(button => button.Name == "MemoryGameBack" && button.IsVisible));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "MemoryResume" && button.IsVisible),
            "Leaving a Memory table did not preserve a resumable active game.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("0 / 8 pairs", StringComparison.Ordinal)),
            "Memory resume card did not preserve the Easy table progress.");

        Click(Find<Button>(window).Single(button => button.Name == "MemoryBack" && button.IsVisible));
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGame_memory" && button.IsVisible),
            "Memory did not return to the local Chill Lounge game menu.");

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "RETURN TO CLUB")));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "Mode_chill_lounge" && button.IsVisible),
            "Casino lobby was not restored after the Memory smoke.");

        File.WriteAllText(Path.Combine(output, "memory-result.txt"),
            "PASS: Memory tile, five difficulty tables, deck picker, Easy 4x4 start, card flip feedback, saved resume state and local Lounge navigation all passed.");
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
