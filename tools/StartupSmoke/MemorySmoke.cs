using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Memory;
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
        ComboBox deckPicker = Find<ComboBox>(window).Single(combo => combo.Name == "MemoryDeckPicker" && combo.IsVisible);
        Require(deckPicker.Items.Count == 4, $"Memory should expose exactly four curated decks; found {deckPicker.Items.Count}.");
        string[] deckIds = deckPicker.Items.Cast<MemoryDeckDefinition>().Select(x => x.Id).ToArray();
        Require(deckIds.SequenceEqual(new[] { "casino-icons", "card-suits", "animals", "fruits" }),
            "Memory deck picker does not expose the four curated decks in the expected order.");
        Require(Find<ContentControl>(window).Any(control => control.Name == "MemoryDeckPreview" && control.IsVisible),
            "Memory hub is missing the curated deck preview strip.");
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible),
            "Memory did not move the Games navigation into the Chill Lounge header.");
        Capture(window, Path.Combine(output, "memory-hub.png"));

        Button easy = Find<Button>(window).Single(button => button.Name == "MemoryStart_Easy" && button.IsVisible);
        Click(easy);
        await Task.Delay(80);
        window.UpdateLayout();

        Button[] cards = VisibleCards(window);
        Require(cards.Length == 16, $"Easy Memory did not render a 4x4 board; found {cards.Length} card buttons.");
        Require(cards.All(card => card.Content?.GetType().Name == "MemoryCardArtwork"),
            "Memory cards are not using the custom card artwork renderer.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("0 / 8 pairs", StringComparison.Ordinal)),
            "Easy Memory did not start with an eight-pair progress counter.");

        Click(cards[0]);
        await Task.Delay(240);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("One card open", StringComparison.Ordinal)),
            "Flipping the first Memory card did not update the table feedback.");
        Require(cards[0].Content?.GetType().Name == "MemoryCardArtwork",
            "Face-up Memory card lost the custom card artwork renderer.");
        Capture(window, Path.Combine(output, "memory-game.png"));

        Click(Find<Button>(window).Single(button => button.Name == "MemoryGameBack" && button.IsVisible));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "MemoryResume" && button.IsVisible),
            "Leaving a Memory table did not preserve a resumable active game.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("0 / 8 pairs", StringComparison.Ordinal)),
            "Memory resume card did not preserve the Easy table progress.");

        Click(Find<Button>(window).Single(button => button.Name == "MemoryResume" && button.IsVisible));
        await Task.Delay(80);
        window.UpdateLayout();
        await CompleteCurrentEasyGame(window);
        await Task.Delay(350);
        window.UpdateLayout();

        Require(Find<Button>(window).Any(button => button.Name == "MemoryResultBack" && button.IsVisible),
            "Winning Memory did not show the completion screen's Back to Memory action.");
        Require(Find<Button>(window).Any(button => button.Name == "MemoryResultAgain" && button.IsVisible),
            "Winning Memory did not show the completion screen's Play Again action.");
        string[] resultLabels = ["MOVES", "ACCURACY", "MISSES", "BEST STREAK", "TIME", "PAIRS"];
        foreach (string label in resultLabels)
        {
            Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == label),
                $"Memory completion screen is missing the {label} statistic.");
        }
        Capture(window, Path.Combine(output, "memory-completion.png"));

        Click(Find<Button>(window).Single(button => button.Name == "MemoryResultBack" && button.IsVisible));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(Find<Button>(window).Count(button => button.IsVisible && button.Name.StartsWith("MemoryStart_", StringComparison.Ordinal)) == 5,
            "Memory completion screen did not return to the Memory hub.");

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible));
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
            "PASS: Memory tile, five difficulty tables, four curated decks, enlarged deck preview, custom card artwork, Easy 4x4 start, card flip feedback, saved resume state, completion statistics screen and header Lounge navigation all passed.");
    }

    private static async Task CompleteCurrentEasyGame(MainWindow window)
    {
        FrameworkElement view = Find<FrameworkElement>(window)
            .Single(element => element.IsVisible && element.GetType().Name == "MemoryView");
        FieldInfo? gameField = view.GetType().GetField("game", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(gameField?.GetValue(view) is MemoryGameState, "Memory smoke could not inspect the active game state.");
        var game = (MemoryGameState)gameField!.GetValue(view)!;
        Dictionary<int, Button> buttons = VisibleCards(window).ToDictionary(button => (int)button.Tag);

        if (game.FaceUpIndices.Count == 1)
        {
            int open = game.FaceUpIndices[0];
            int mate = Enumerable.Range(0, game.Cards.Count)
                .Single(index => index != open && game.Cards[index] == game.Cards[open]);
            Click(buttons[mate]);
            await Task.Delay(280);
        }

        List<int[]> pairs = game.Cards
            .Select((symbol, index) => new { symbol, index })
            .Where(item => !game.MatchedIndices.Contains(item.index))
            .GroupBy(item => item.symbol, StringComparer.Ordinal)
            .Select(group => group.Select(item => item.index).ToArray())
            .ToList();

        foreach (int[] pair in pairs)
        {
            Require(pair.Length == 2, "Memory smoke found a malformed pair while completing the Easy table.");
            Click(buttons[pair[0]]);
            await Task.Delay(240);
            Click(buttons[pair[1]]);
            await Task.Delay(300);
        }
    }

    private static Button[] VisibleCards(MainWindow window) => Find<Button>(window)
        .Where(button => button.IsVisible && button.Name.StartsWith("MemoryCard_", StringComparison.Ordinal))
        .OrderBy(button => button.Name, StringComparer.Ordinal)
        .ToArray();

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
