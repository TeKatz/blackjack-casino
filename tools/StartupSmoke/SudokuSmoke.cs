using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        Require(Find<Button>(window).Any(button => Equals(button.Content, "START NEXT PUZZLE") && button.IsVisible),
            "Chill Lounge does not expose the Free Play Sudoku action.");
        Require(Find<Button>(window).Any(button => button.Content?.ToString()?.StartsWith("TODAY'S DAILY", StringComparison.Ordinal) == true && button.IsVisible),
            "Chill Lounge does not expose today's offline Daily Sudoku.");
        Capture(window, Path.Combine(output, "sudoku-lounge.png"));

        Click(Find<Button>(window).Single(button => Equals(button.Content, "START NEXT PUZZLE") && button.IsVisible));
        await Task.Delay(80);
        window.UpdateLayout();

        Button[] cells = BoardCells(window);
        Require(cells.Length == 81, $"Sudoku board should expose 81 cells; found {cells.Length}.");
        Require(Find<Button>(window).Count(button => button.IsVisible && button.Content is string value && value.Length == 1 && value[0] is >= '1' and <= '9' && button.Tag is null) >= 9,
            "Sudoku number pad is incomplete.");
        Require(Find<ToggleButton>(window).Any(button => button.IsVisible && Equals(button.Content, "PENCIL  [SPACE]")),
            "Sudoku pencil-mark control is missing.");
        Require(Find<Button>(window).Any(button => button.IsVisible && Equals(button.Content, "UNDO")),
            "Sudoku Undo control is missing.");
        Require(Find<Button>(window).Any(button => button.IsVisible && Equals(button.Content, "REDO")),
            "Sudoku Redo control is missing.");

        Button editable = cells.First(cell => string.IsNullOrEmpty(cell.Content?.ToString()));
        Click(editable);
        Button one = Find<Button>(window).First(button => button.IsVisible && Equals(button.Content, "1") && button.Tag is null);
        Click(one);
        Require(Equals(editable.Content, "1"), "Entering a Sudoku digit did not update the selected cell.");

        Button noteCell = BoardCells(window).First(cell => !ReferenceEquals(cell, editable) && string.IsNullOrEmpty(cell.Content?.ToString()));
        Click(noteCell);
        ToggleButton pencil = Find<ToggleButton>(window).Single(button => button.IsVisible && Equals(button.Content, "PENCIL  [SPACE]"));
        Click(pencil);
        Button two = Find<Button>(window).First(button => button.IsVisible && Equals(button.Content, "2") && button.Tag is null);
        Click(two);
        Require(noteCell.Content?.ToString()?.Contains('2') == true, "Pencil entry did not appear in the selected Sudoku cell.");

        Button undo = Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "UNDO"));
        Click(undo);
        Require(string.IsNullOrEmpty(noteCell.Content?.ToString()), "Undo did not restore the previous Sudoku board frame.");
        Button redo = Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "REDO"));
        Click(redo);
        Require(noteCell.Content?.ToString()?.Contains('2') == true, "Redo did not restore the Sudoku pencil mark.");
        Capture(window, Path.Combine(output, "sudoku-free-play.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(60);
        window.UpdateLayout();
        Button daily = Find<Button>(window).Single(button => button.IsVisible && button.Content?.ToString()?.StartsWith("TODAY'S DAILY", StringComparison.Ordinal) == true);
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
            "PASS: embedded Chill Lounge; Free Play Sudoku; 81-cell board; number entry; pencil marks; undo/redo; separate offline Daily entry; return to lobby.");
    }

    private static Button[] BoardCells(DependencyObject root) => Find<Button>(root)
        .Where(button => button.IsVisible && button.Tag is int)
        .OrderBy(button => (int)button.Tag)
        .ToArray();

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
