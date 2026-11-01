using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class WordSearchSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();

        Button tile = Find<Button>(window).Single(button => button.Name == "LoungeGame_word_search" && button.IsVisible);
        Require(Find<TextBlock>(tile).Any(text => text.Text.Contains("PUZZLES", StringComparison.Ordinal) && text.Text.Contains("WORDS", StringComparison.Ordinal)),
            "Word Search Lounge tile is missing its compact statistics overview.");
        Require(Find<FrameworkElement>(tile).Any(element => element.GetType().Name == "WordSearchLoungeArtworkUpgrade"),
            "Word Search Lounge tile is not using the upgraded real-grid artwork.");

        Click(tile);
        await Task.Delay(90);
        window.UpdateLayout();

        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Word Search"),
            "Word Search hub did not open from the Chill Lounge menu.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("144 authored puzzle slots", StringComparison.Ordinal)),
            "Word Search hub does not advertise the deterministic offline puzzle catalog.");
        Require(Find<Button>(window).Count(button => button.IsVisible && button.Name.StartsWith("WordSearchStart_", StringComparison.Ordinal)) == 6,
            "Word Search hub does not expose all six difficulty tiers.");
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible && Equals(button.Content, "← GAMES")),
            "Word Search did not move Games navigation into the Chill Lounge header.");
        Require(!Find<Button>(window).Any(button => button.Name == "WordSearchBack" && button.IsVisible),
            "Word Search still shows the redundant page-local Games button.");
        Require(Find<TextBlock>(window).Count(text => text.IsVisible && text.Text.EndsWith(" XP", StringComparison.Ordinal)) >= 6,
            "Word Search difficulty cards are missing their XP presentation.");
        Capture(window, Path.Combine(output, "word-search-hub.png"));

        Button relaxed = Find<Button>(window).Single(button => button.Name == "WordSearchStart_Relaxed" && button.IsVisible);
        Click(relaxed);
        await Task.Delay(90);
        window.UpdateLayout();

        Require(Find<Button>(window).Any(button => button.Name == "WordSearchHint" && button.IsVisible),
            "Starting a Relaxed Word Search did not open the playable puzzle view.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("0 / 6 words found", StringComparison.Ordinal)),
            "Relaxed Word Search did not start with a six-word progress counter.");

        Button hint = Find<Button>(window).Single(button => button.Name == "WordSearchHint" && button.IsVisible);
        Click(hint);
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.StartsWith("Hint:", StringComparison.Ordinal)),
            "Word Search hint action did not reveal a starting-letter hint.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.Contains("1 hint", StringComparison.Ordinal)),
            "Word Search hint usage was not reflected in the puzzle progress UI.");
        Capture(window, Path.Combine(output, "word-search-game.png"));

        Click(Find<Button>(window).Single(button => button.Name == "WordSearchGameBack" && button.IsVisible));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "WordSearchResume" && button.IsVisible),
            "Leaving a Word Search puzzle did not preserve a resumable active game.");

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible));
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGame_word_search" && button.IsVisible),
            "Word Search did not return to the local Chill Lounge game menu.");

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "RETURN TO CLUB")));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "Mode_chill_lounge" && button.IsVisible),
            "Casino lobby was not restored after the Word Search smoke.");
        Require(Find<Border>(window).Any(border => border.Name == "LobbyOverviewHub" && border.IsVisible),
            "Casino lobby is missing the new right-side overview hub.");
        string[] hubActions =
        [
            "LobbyProgression_SeasonPass",
            "LobbyProgression_CosmeticShop",
            "LobbyProgression_Challenges",
            "LobbyProgression_Overview",
        ];
        foreach (string name in hubActions)
        {
            Require(Find<Button>(window).Any(button => button.Name == name && button.IsVisible),
                $"Casino lobby overview hub is missing {name}.");
        }
        string[] retiredFooter = ["Season Pass", "Cosmetic Shop", "Challenges", "Overview"];
        Require(!Find<Button>(window).Any(button => button.IsVisible && button.Content is string label && retiredFooter.Contains(label, StringComparer.Ordinal)),
            "Casino lobby still exposes the retired bottom progression button row.");
        Capture(window, Path.Combine(output, "casino-lobby-overview-hub.png"));

        File.WriteAllText(Path.Combine(output, "word-search-result.txt"),
            "PASS: upgraded Word Search artwork, compact tile statistics, six polished difficulty tiers, header Games navigation, Relaxed puzzle start, hint feedback, autosaved resume state and right-side casino overview hub all passed.");
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
