using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Desktop;

internal static class SudokuSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        Require(window.Modes.Modes.Any(mode => mode.Id == "chill-lounge"), "Chill Lounge is missing from the lobby.");
        window.Modes.Open("chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();

        Require(Application.Current.Windows.Cast<Window>().Count(candidate => candidate.IsVisible) == 1,
            "Chill Lounge opened another visible native window.");
        Require(Find<TextBlock>(window).Any(text => text.Text == "Chill Lounge" && text.IsVisible),
            "Chill Lounge heading is missing from the hosted surface.");
        Require(ButtonByName(window, "SudokuNew_Medium").IsVisible,
            "Chill Lounge does not expose a dedicated Medium New Puzzle action.");
        Require(ButtonByName(window, "SudokuNew_Hard").IsVisible,
            "Chill Lounge does not expose a dedicated Hard New Puzzle action.");
        Require(ButtonByName(window, "SudokuResume_Medium").IsVisible && !ButtonByName(window, "SudokuResume_Medium").IsEnabled,
            "An untouched Medium difficulty should clearly show that there is nothing to resume.");
        Require(Find<Button>(window).Any(button => button.Name == "SudokuDailyButton" && button.IsVisible),
            "Chill Lounge does not expose today's offline Daily Sudoku.");
        Capture(window, Path.Combine(output, "sudoku-lounge.png"));

        Click(ButtonByName(window, "SudokuNew_Medium"));
        await Task.Delay(80);
        window.UpdateLayout();

        SudokuView mediumView = Find<SudokuView>(window).Single();
        string firstMediumId = mediumView.PuzzleId;
        Require(mediumView.Difficulty == SudokuDifficulty.Medium,
            "Medium New Puzzle opened the wrong difficulty.");
        Button[] cells = BoardCells(window);
        Require(cells.Length == 81, $"Sudoku board should expose 81 cells; found {cells.Length}.");
        Require(cells.Max(cell => cell.ActualWidth) >= 58,
            "Sudoku board is still too small for the available hosted surface.");
        Require(cells[1].BorderThickness.Left < cells[3].BorderThickness.Left,
            "Sudoku block boundaries are not visually stronger than inner cell lines.");
        Require(cells[1].BorderThickness.Left > 0,
            "Sudoku inner cell lines disappeared.");
        Require(Find<Button>(window).Count(button => button.IsVisible && button.Content is string value && value.Length == 1 && value[0] is >= '1' and <= '9' && button.Tag is null) >= 9,
            "Sudoku number pad is incomplete.");
        Require(Find<ToggleButton>(window).Any(button => button.IsVisible && Equals(button.Content, "PENCIL  [SPACE]")),
            "Sudoku pencil-mark control is missing.");

        IGrouping<string, Button>? repeatedGiven = cells
            .Where(cell => cell.Content is string value && value.Length == 1 && value[0] is >= '1' and <= '9')
            .GroupBy(cell => cell.Content!.ToString()!)
            .FirstOrDefault(group => group.Count() >= 2);
        Require(repeatedGiven is not null, "Smoke puzzle did not expose a repeated given digit for highlight validation.");
        IGrouping<string, Button> repeated = repeatedGiven!;
        Button selectedGiven = repeated.First();
        Button matchingGiven = repeated.Skip(1).First();
        Click(selectedGiven);
        Color sameNumberColor = ((SolidColorBrush)matchingGiven.Background).Color;
        Button unrelated = cells.First(cell =>
            !ReferenceEquals(cell, selectedGiven) &&
            !ReferenceEquals(cell, matchingGiven) &&
            !Equals(cell.Content, selectedGiven.Content) &&
            !IsRelated((int)cell.Tag, (int)selectedGiven.Tag));
        Color unrelatedColor = ((SolidColorBrush)unrelated.Background).Color;
        Require(sameNumberColor != unrelatedColor,
            "Selecting a number did not highlight matching values across the full Sudoku board.");

        ToggleButton guides = Find<ToggleButton>(mediumView).Single(button =>
            button.IsVisible && button.Content?.ToString()?.StartsWith("GUIDES ·", StringComparison.Ordinal) == true);
        Require(mediumView.GuidesVisible && Equals(guides.Content, "GUIDES · ON  [G]"),
            "Sudoku guide highlighting should start enabled in the smoke profile.");
        Color relatedWithGuides = ((SolidColorBrush)cells.First(cell =>
            !ReferenceEquals(cell, selectedGiven) &&
            !Equals(cell.Content, selectedGiven.Content) &&
            IsRelated((int)cell.Tag, (int)selectedGiven.Tag)).Background).Color;
        Click(guides);
        Require(!mediumView.GuidesVisible && Equals(guides.Content, "GUIDES · OFF  [G]"),
            "Sudoku guide highlighting did not turn off.");
        Color relatedWithoutGuides = ((SolidColorBrush)cells.First(cell =>
            !ReferenceEquals(cell, selectedGiven) &&
            !Equals(cell.Content, selectedGiven.Content) &&
            IsRelated((int)cell.Tag, (int)selectedGiven.Tag)).Background).Color;
        Require(relatedWithGuides != relatedWithoutGuides,
            "Disabling Sudoku guides did not remove row/column/block highlighting.");
        Click(guides);
        Require(mediumView.GuidesVisible, "Sudoku guide highlighting did not turn back on.");

        SudokuPuzzle mediumPuzzle = SudokuLibrary.Get(mediumView.PuzzleId);
        Require(SudokuSolver.TrySolve(mediumPuzzle.Givens, out string mediumSolution),
            "Smoke could not solve the Medium Sudoku fixture.");
        Button editable = cells.First(cell => string.IsNullOrEmpty(cell.Content?.ToString()));
        Click(editable);
        int editableDigit = mediumSolution[(int)editable.Tag] - '0';
        Click(NumberButton(window, editableDigit));
        Require(Equals(editable.Content, editableDigit.ToString()), "Entering a Sudoku digit did not update the selected cell.");

        Button noteCell = BoardCells(window).First(cell => !ReferenceEquals(cell, editable) && string.IsNullOrEmpty(cell.Content?.ToString()));
        Click(noteCell);
        ToggleButton pencil = Find<ToggleButton>(window).Single(button => button.IsVisible && Equals(button.Content, "PENCIL  [SPACE]"));
        Click(pencil);
        Button two = NumberButton(window, 2);
        if (!two.IsEnabled)
        {
            two = Enumerable.Range(1, 9).Select(digit => NumberButton(window, digit)).First(button => button.IsEnabled);
        }
        string noteDigit = two.Content!.ToString()!;
        Click(two);
        Require(noteCell.Content?.ToString()?.Contains(noteDigit, StringComparison.Ordinal) == true,
            "Pencil entry did not appear in the selected Sudoku cell.");

        Button undo = Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "UNDO"));
        Click(undo);
        Require(string.IsNullOrEmpty(noteCell.Content?.ToString()), "Undo did not restore the previous Sudoku board frame.");
        Button redo = Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "REDO"));
        Click(redo);
        Require(noteCell.Content?.ToString()?.Contains(noteDigit, StringComparison.Ordinal) == true,
            "Redo did not restore the Sudoku pencil mark.");
        Capture(window, Path.Combine(output, "sudoku-free-play.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(ButtonByName(window, "SudokuResume_Medium").IsEnabled,
            "Returning from Medium did not mark Medium as an active resumable puzzle.");
        Require(Find<TextBlock>(window).Single(text => text.Name == "SudokuStatus_Medium").Text.StartsWith("IN PROGRESS", StringComparison.Ordinal),
            "Medium status does not communicate that a puzzle is in progress.");

        Click(ButtonByName(window, "SudokuNew_Hard"));
        await Task.Delay(60);
        Require(Find<SudokuView>(window).Single().Difficulty == SudokuDifficulty.Hard,
            "Hard New Puzzle opened the wrong difficulty.");
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(60);
        window.UpdateLayout();
        Require(ButtonByName(window, "SudokuResume_Medium").IsEnabled && ButtonByName(window, "SudokuResume_Hard").IsEnabled,
            "Medium and Hard cannot remain paused independently.");
        Require(Find<TextBlock>(window).Single(text => text.Name == "SudokuStatus_Hard").Text.StartsWith("IN PROGRESS", StringComparison.Ordinal),
            "Hard status does not communicate that a puzzle is in progress.");

        Click(ButtonByName(window, "SudokuNew_Medium"));
        await Task.Delay(60);
        SudokuView replacementMedium = Find<SudokuView>(window).Single();
        Require(replacementMedium.PuzzleId != firstMediumId,
            "New Puzzle reopened the existing Medium save instead of starting a fresh puzzle.");
        Require(replacementMedium.GuidesVisible,
            "Sudoku guide visibility was not persisted when opening another puzzle.");
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(60);
        window.UpdateLayout();

        Button timerToggle = ButtonByName(window, "SudokuTimerToggle");
        Require(Equals(timerToggle.Content, "TIMER · VISIBLE"), "Sudoku timer should default to visible in the smoke profile.");
        Click(timerToggle);
        await Task.Delay(40);
        window.UpdateLayout();
        Require(Equals(ButtonByName(window, "SudokuTimerToggle").Content, "TIMER · HIDDEN"),
            "Sudoku timer visibility option did not switch to hidden.");
        Click(ButtonByName(window, "SudokuNew_Easy"));
        await Task.Delay(60);
        SudokuView hiddenTimerView = Find<SudokuView>(window).Single();
        Require(!hiddenTimerView.TimerVisible, "Hidden timer preference was not applied to the Sudoku game view.");
        Require(!Find<TextBlock>(hiddenTimerView).Any(text => text.IsVisible && text.Text == "TIME"),
            "Timer label is still visible after disabling the Sudoku timer.");

        SudokuPuzzle easyPuzzle = SudokuLibrary.Get(hiddenTimerView.PuzzleId);
        Require(SudokuSolver.TrySolve(easyPuzzle.Givens, out string easySolution),
            "Smoke could not solve the Easy Sudoku fixture.");
        Button[] easyCells = BoardCells(hiddenTimerView);
        int completedDigit = Enumerable.Range(1, 9).First(digit => easyCells.Any(cell =>
            string.IsNullOrEmpty(cell.Content?.ToString()) && easySolution[(int)cell.Tag] - '0' == digit));
        Button completedDigitButton = NumberButton(hiddenTimerView, completedDigit);
        foreach (Button cell in easyCells.Where(cell =>
                     string.IsNullOrEmpty(cell.Content?.ToString()) && easySolution[(int)cell.Tag] - '0' == completedDigit).ToArray())
        {
            Click(cell);
            Click(completedDigitButton);
        }
        Require(!completedDigitButton.IsEnabled && completedDigitButton.Opacity < 0.5d,
            "A fully and correctly placed Sudoku digit was not greyed out and disabled in the number pad.");

        Button wrongCell = BoardCells(hiddenTimerView).First(cell => string.IsNullOrEmpty(cell.Content?.ToString()));
        int correctValue = easySolution[(int)wrongCell.Tag] - '0';
        int wrongValue = Enumerable.Range(1, 9).First(digit =>
            digit != correctValue && NumberButton(hiddenTimerView, digit).IsEnabled);
        Click(wrongCell);
        Click(NumberButton(hiddenTimerView, wrongValue));
        Require(Equals(wrongCell.Content, wrongValue.ToString()), "Smoke could not place an intentionally wrong Sudoku value.");
        Color wrongColor = ((SolidColorBrush)wrongCell.Foreground).Color;
        Color expectedWrongColor = (Color)ColorConverter.ConvertFromString("#F08F84");
        Require(wrongColor == expectedWrongColor,
            "A wrong but otherwise non-conflicting Sudoku value was not identified immediately.");

        Button hint = Find<Button>(hiddenTimerView).Single(button => button.IsVisible && Equals(button.Content, "REVEAL ONE CELL"));
        Click(hint);
        Require(Equals(wrongCell.Content, correctValue.ToString()),
            "Sudoku hint did not repair the selected wrong value.");

        Button secondWrongCell = BoardCells(hiddenTimerView).First(cell => string.IsNullOrEmpty(cell.Content?.ToString()));
        int secondCorrect = easySolution[(int)secondWrongCell.Tag] - '0';
        int secondWrong = Enumerable.Range(1, 9).First(digit =>
            digit != secondCorrect && NumberButton(hiddenTimerView, digit).IsEnabled);
        Click(secondWrongCell);
        Click(NumberButton(hiddenTimerView, secondWrong));
        Require(!hiddenTimerView.Snapshot.Completed, "Intentionally wrong Sudoku state unexpectedly completed the puzzle.");

        int hintGuard = 0;
        while (!hiddenTimerView.Snapshot.Completed && hintGuard < 81)
        {
            Click(hint);
            hintGuard++;
        }
        Require(hiddenTimerView.Snapshot.Completed,
            "Repeated hints softlocked instead of correcting wrong entries and completing the Sudoku.");
        Require(Enumerable.Range(1, 9).All(digit => !NumberButton(hiddenTimerView, digit).IsEnabled),
            "Completed Sudoku still leaves one or more number buttons enabled.");
        Capture(window, Path.Combine(output, "sudoku-hint-recovery.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(50);

        Button daily = ButtonByName(window, "SudokuDailyButton");
        Click(daily);
        await Task.Delay(60);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DAILY SUDOKU"),
            "Daily Sudoku did not open from the Lounge hub.");
        Require(BoardCells(window).Length == 81, "Daily Sudoku did not render a complete 9x9 board.");
        Capture(window, Path.Combine(output, "sudoku-daily.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(50);
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "RETURN TO CLUB")));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(button => button.Name == "Mode_chill_lounge" && button.IsVisible && button.IsEnabled),
            "Casino lobby was not restored after leaving Chill Lounge.");

        File.WriteAllText(Path.Combine(output, "sudoku-result.txt"),
            "PASS: larger board; fine cell grid and strong block boundaries; matching-number highlight; toggleable guides; completed-number disabling; immediate wrong-value validation; hint correction and anti-softlock recovery; independent per-difficulty resume; true New Puzzle replacement; timer hide option; pencil marks; undo/redo; separate Daily entry.");
    }

    private static Button ButtonByName(DependencyObject root, string name) =>
        Find<Button>(root).Single(button => button.Name == name);

    private static Button NumberButton(DependencyObject root, int digit) => Find<Button>(root)
        .Single(button => button.IsVisible && button.Tag is null && Equals(button.Content, digit.ToString()));

    private static Button[] BoardCells(DependencyObject root) => Find<Button>(root)
        .Where(button => button.IsVisible && button.Tag is int)
        .OrderBy(button => (int)button.Tag)
        .ToArray();

    private static bool IsRelated(int left, int right)
    {
        int leftRow = left / 9;
        int leftColumn = left % 9;
        int rightRow = right / 9;
        int rightColumn = right % 9;
        return leftRow == rightRow || leftColumn == rightColumn ||
               (leftRow / 3 == rightRow / 3 && leftColumn / 3 == rightColumn / 3);
    }

    private static void Click(ButtonBase button)
    {
        Require(button.IsVisible && button.IsEnabled, $"Control '{button.Content}' is unavailable.");
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap(
            Math.Max(1, (int)Math.Ceiling(element.ActualWidth)),
            Math.Max(1, (int)Math.Ceiling(element.ActualHeight)),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path);
        encoder.Save(file);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
