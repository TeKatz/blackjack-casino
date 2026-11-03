using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.WordSearch;

internal static class WordSearchSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        NavigationSmoke.OpenCovered(window, "chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();

        Button wordSearchTile = Find<Button>(window).Single(button => button.Name == "LoungeGame_word_search" && button.IsVisible);
        Button memoryTile = Find<Button>(window).Single(button => button.Name == "LoungeGame_memory" && button.IsVisible);
        Button sudokuTile = Find<Button>(window).Single(button => button.Name == "LoungeGame_sudoku" && button.IsVisible);
        Require(Find<TextBlock>(wordSearchTile).Any(text => text.Text.Contains("PUZZLES", StringComparison.Ordinal) && text.Text.Contains("WORDS", StringComparison.Ordinal)),
            "Word Search Lounge tile is missing its compact statistics overview.");
        Require(Find<TextBlock>(memoryTile).Any(text => text.Text.Contains("GAMES", StringComparison.Ordinal) && text.Text.Contains("PAIRS", StringComparison.Ordinal)),
            "Memory Lounge tile is missing its compact statistics overview.");
        Require(Find<TextBlock>(sudokuTile).Any(text => text.Name == "SudokuTileStats" && text.Text.Contains("SOLVED", StringComparison.Ordinal)),
            "Sudoku Lounge tile is missing its compact statistics overview.");
        Require(Find<FrameworkElement>(wordSearchTile).Any(element => element.GetType().Name == "WordSearchLoungeArtworkUpgrade"),
            "Word Search Lounge tile is not using the upgraded real-grid artwork.");

        Click(sudokuTile);
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible && Equals(button.Content, "← GAMES")),
            "Sudoku did not move Games navigation into the Chill Lounge header.");
        Require(!Find<Button>(window).Any(button => button.Name == "SudokuBackToLoungeGames" && button.IsVisible),
            "Sudoku still shows the old page-local Lounge Games button.");
        Click(Find<Button>(window).Single(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible));
        await Task.Delay(70);
        window.UpdateLayout();

        wordSearchTile = Find<Button>(window).Single(button => button.Name == "LoungeGame_word_search" && button.IsVisible);
        Click(wordSearchTile);
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
        // Verify the 1080p hub keeps every difficulty and its guidance visible
        // without scrolling or magnifying a thumbnail into the large art pane.
        FrameworkElement wordSearchHub = Find<FrameworkElement>(window).Single(element =>
            element.IsVisible && element.GetType().Name == "WordSearchLoungeHub");
        Require(!Find<ScrollViewer>(wordSearchHub).Any(scroll => scroll.IsVisible &&
            scroll.ActualHeight > 200 && scroll.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled),
            "Word Search hub still requires a vertical scrollbar.");
        FrameworkElement help = Find<FrameworkElement>(wordSearchHub).Single(element =>
            element.Name == "WordSearchHubHelp" && element.IsVisible);
        FrameworkElement hubStatus = Find<FrameworkElement>(wordSearchHub).Single(element =>
            element.Name == "WordSearchHubStatus" && element.IsVisible);
        foreach (FrameworkElement visible in new FrameworkElement[] { help, hubStatus })
        {
            Point location = visible.TranslatePoint(new Point(0, 0), wordSearchHub);
            Require(location.Y >= -1 && location.Y + visible.ActualHeight <= wordSearchHub.ActualHeight + 2,
                $"Word Search hub clipped {visible.Name} below the viewport.");
        }
        Require(Find<Image>(wordSearchHub).Where(art => art.IsVisible && art.Source is BitmapSource).All(art =>
            ((BitmapSource)art.Source).PixelWidth >= 600),
            "Word Search hub enlarged a low-resolution illustration in its backdrop.");
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

        Click(Find<Button>(window).Single(button => button.Name == "WordSearchResume" && button.IsVisible));
        await Task.Delay(60); window.UpdateLayout();
        WordSearchView puzzleView = Find<WordSearchView>(window).Single();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        WordSearchGameState game = (WordSearchGameState)typeof(WordSearchView).GetField("game", flags)!.GetValue(puzzleView)!;
        var preferencesField = typeof(MainWindow).GetField("preferences", flags)!;
        AppPreferences currentPreferences = (AppPreferences)preferencesField.GetValue(window)!;
        AreaPresentationSettings previousPresentation = currentPreferences.Presentation("chill-lounge");
        preferencesField.SetValue(window, currentPreferences.WithPresentation("chill-lounge", previousPresentation with { AnimationsEnabled = true, SoundEnabled = false }));
        WordSearchPlacement firstWord = game.Puzzle.Placements[0];
        puzzleView.SubmitSelection(new(firstWord.StartRow, firstWord.StartColumn), new(firstWord.EndRow, firstWord.EndColumn));
        await Task.Delay(80); window.UpdateLayout();
        var glows = (System.Collections.ICollection)typeof(WordSearchView).GetField("wordGlows", flags)!.GetValue(puzzleView)!;
        Require(glows.Count > 0, "Finding a word did not start the letter glow sweep.");
        Capture(window, Path.Combine(output, "word-search-found-glow.png"));
        foreach (WordSearchPlacement placement in game.Puzzle.Placements.Skip(1))
            puzzleView.SubmitSelection(new(placement.StartRow, placement.StartColumn), new(placement.EndRow, placement.EndColumn));
        await Task.Delay(1400); window.UpdateLayout();
        currentPreferences = (AppPreferences)preferencesField.GetValue(window)!;
        preferencesField.SetValue(window, currentPreferences.WithPresentation("chill-lounge", previousPresentation));
        Require(Find<Grid>(window).Any(grid => grid.Name == "WordSearchCompletionOverlay" && grid.IsVisible), "Completing a Word Search did not show its result window.");
        Require(Find<Button>(window).Any(button => button.Name == "WordSearchCompletionClub" && button.IsVisible), "Word Search result has no Return to Club action.");
        int found = game.FoundWords.Count;
        puzzleView.SubmitSelection(new(0, 0), new(0, 1));
        Require(game.FoundWords.Count == found, "Completed Word Search still accepts selections.");
        Capture(window, Path.Combine(output, "word-search-complete.png"));
        Click(Find<Button>(window).Single(button => button.Name == "WordSearchCompletionHub" && button.IsVisible));
        await Task.Delay(60); window.UpdateLayout();
        Require(!Find<Button>(window).Any(button => button.Name == "WordSearchResume" && button.IsVisible), "A completed puzzle returned as an active resumable game.");

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGamesHeaderBack" && button.IsVisible));
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "LoungeGame_word_search" && button.IsVisible),
            "Word Search did not return to the local Chill Lounge game menu.");

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_word_search" && button.IsVisible));
        await Task.Delay(40); window.UpdateLayout();
        Click(Find<Button>(window).Single(button => button.Name == "WordSearchStart_Easy" && button.IsVisible));
        await Task.Delay(40); window.UpdateLayout();
        puzzleView = Find<WordSearchView>(window).Single();
        game = (WordSearchGameState)typeof(WordSearchView).GetField("game", flags)!.GetValue(puzzleView)!;
        foreach (WordSearchPlacement placement in game.Puzzle.Placements)
            puzzleView.SubmitSelection(new(placement.StartRow, placement.StartColumn), new(placement.EndRow, placement.EndColumn));
        await Task.Delay(1100); window.UpdateLayout();
        Click(Find<Button>(window).Single(button => button.Name == "WordSearchCompletionClub" && button.IsVisible));
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
            "PASS: upgraded Word Search artwork; Word Search, Memory and Sudoku tile statistics; Sudoku and Word Search header Games navigation; six polished difficulty tiers; autosaved Word Search state; and right-side casino overview hub all passed.");
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
