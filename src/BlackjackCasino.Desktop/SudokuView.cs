using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

internal sealed class SudokuView : UserControl, IDisposable
{
    private readonly SudokuPuzzle puzzle;
    private readonly SudokuBoard board;
    private readonly Action<SudokuGameState> save;
    private readonly Action back;
    private readonly Button[] cells = new Button[81];
    private readonly Stack<SudokuBoardFrame> undo = new();
    private readonly Stack<SudokuBoardFrame> redo = new();
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch elapsed = new();
    private readonly long elapsedBaseSeconds;
    private readonly TextBlock timeText = new();
    private readonly TextBlock statusText = new();
    private readonly TextBlock hintText = new();
    private readonly ToggleButton pencilButton = new();
    private readonly Button undoButton = new();
    private readonly Button redoButton = new();
    private int selectedIndex = -1;
    private bool disposed;
    private bool completionSaved;

    public SudokuView(SudokuPuzzle puzzle, SudokuGameState state, Action<SudokuGameState> save, Action back)
    {
        this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
        this.back = back ?? throw new ArgumentNullException(nameof(back));
        SudokuGameState clean = SudokuGameState.Normalize(puzzle, state);
        board = new SudokuBoard(puzzle, clean);
        elapsedBaseSeconds = clean.ElapsedSeconds;
        completionSaved = clean.Completed;

        Focusable = true;
        Background = Ink("#0B1517");
        Foreground = Ink("#F0E7D7");
        Content = BuildLayout();

        PreviewKeyDown += OnPreviewKeyDown;
        clock.Tick += ClockOnTick;
        if (!board.IsComplete)
        {
            elapsed.Start();
            clock.Start();
        }

        Refresh();
        Loaded += (_, _) => Focus();
    }

    public SudokuGameState Snapshot => board.Snapshot(CurrentElapsedSeconds);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        clock.Stop();
        elapsed.Stop();
        clock.Tick -= ClockOnTick;
        save(Snapshot);
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(34, 24, 34, 26) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(new TextBlock
        {
            Text = puzzle.Pool == SudokuPuzzlePool.Daily ? "DAILY SUDOKU" : "SUDOKU",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#8EC9B0"),
        });
        title.Children.Add(new TextBlock
        {
            Text = DifficultyName(puzzle.Difficulty),
            FontSize = 31,
            FontFamily = new FontFamily("Georgia"),
            Foreground = Ink("#F5EAD7"),
            Margin = new Thickness(0, 4, 0, 0),
        });
        header.Children.Add(title);

        var leave = MakeButton("← LOUNGE", Ink("#17272A"), Ink("#54706D"));
        leave.Padding = new Thickness(18, 9, 18, 9);
        leave.Click += (_, _) => back();
        Grid.SetColumn(leave, 1);
        header.Children.Add(leave);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 430 });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 290 });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var boardFrame = new Border
        {
            Background = Ink("#111D1E"),
            BorderBrush = Ink("#6A8B81"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 22, 0),
        };
        var boardViewbox = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly };
        var grid = new UniformGrid { Rows = 9, Columns = 9, Width = 594, Height = 594 };
        for (int index = 0; index < 81; index++)
        {
            int cellIndex = index;
            var button = new Button
            {
                Tag = index,
                FontSize = 27,
                FontWeight = FontWeights.SemiBold,
                Background = Ink("#142224"),
                Foreground = Ink("#DFE8E2"),
                BorderBrush = Ink("#46625D"),
                BorderThickness = CellBorder(index),
                Padding = new Thickness(0),
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            button.Click += (_, _) =>
            {
                selectedIndex = cellIndex;
                Refresh();
                Focus();
            };
            cells[index] = button;
            grid.Children.Add(button);
        }
        boardViewbox.Child = grid;
        boardFrame.Child = boardViewbox;
        body.Children.Add(boardFrame);

        var side = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(side, 1);
        body.Children.Add(side);

        var stats = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.Children.Add(StatCard("TIME", timeText));
        hintText.TextAlignment = TextAlignment.Right;
        var hintCard = StatCard("HINTS", hintText);
        Grid.SetColumn(hintCard, 1);
        stats.Children.Add(hintCard);
        side.Children.Add(stats);

        side.Children.Add(new TextBlock
        {
            Text = "NUMBER",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#8EA49D"),
            Margin = new Thickness(0, 0, 0, 8),
        });
        var numbers = new UniformGrid { Rows = 3, Columns = 3, Margin = new Thickness(0, 0, 0, 12) };
        for (int digit = 1; digit <= 9; digit++)
        {
            int value = digit;
            var number = MakeButton(digit.ToString(), Ink("#182B2C"), Ink("#456763"));
            number.FontSize = 20;
            number.Margin = new Thickness(3);
            number.MinHeight = 48;
            number.Click += (_, _) => EnterDigit(value);
            numbers.Children.Add(number);
        }
        side.Children.Add(numbers);

        pencilButton.Content = "PENCIL  [SPACE]";
        pencilButton.Foreground = Ink("#E9E1D2");
        pencilButton.Background = Ink("#17272A");
        pencilButton.BorderBrush = Ink("#54706D");
        pencilButton.Padding = new Thickness(12, 10, 12, 10);
        pencilButton.Margin = new Thickness(3);
        pencilButton.FontWeight = FontWeights.SemiBold;
        pencilButton.Checked += (_, _) => RefreshPencilButton();
        pencilButton.Unchecked += (_, _) => RefreshPencilButton();

        var erase = MakeButton("ERASE  [DEL]", Ink("#17272A"), Ink("#54706D"));
        erase.Margin = new Thickness(3);
        erase.Click += (_, _) => ClearSelected();

        var editRow = new UniformGrid { Rows = 1, Columns = 2 };
        editRow.Children.Add(pencilButton);
        editRow.Children.Add(erase);
        side.Children.Add(editRow);

        undoButton.Content = "UNDO";
        redoButton.Content = "REDO";
        foreach (Button action in new[] { undoButton, redoButton })
        {
            action.Foreground = Ink("#E9E1D2");
            action.Background = Ink("#17272A");
            action.BorderBrush = Ink("#54706D");
            action.Padding = new Thickness(12, 10, 12, 10);
            action.Margin = new Thickness(3);
            action.FontWeight = FontWeights.SemiBold;
        }
        undoButton.Click += (_, _) => Undo();
        redoButton.Click += (_, _) => Redo();
        var historyRow = new UniformGrid { Rows = 1, Columns = 2, Margin = new Thickness(0, 4, 0, 0) };
        historyRow.Children.Add(undoButton);
        historyRow.Children.Add(redoButton);
        side.Children.Add(historyRow);

        var hint = MakeButton("REVEAL ONE CELL", Ink("#2A3326"), Ink("#8A9C70"));
        hint.Margin = new Thickness(3, 12, 3, 3);
        hint.Click += (_, _) => ApplyHint();
        side.Children.Add(hint);

        statusText.TextWrapping = TextWrapping.Wrap;
        statusText.TextAlignment = TextAlignment.Center;
        statusText.Foreground = Ink("#B9CAC3");
        statusText.Margin = new Thickness(4, 18, 4, 0);
        statusText.MinHeight = 42;
        side.Children.Add(statusText);

        side.Children.Add(new TextBlock
        {
            Text = "No rush. The timer is informational only and never changes your reward.",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Foreground = Ink("#718C84"),
            Margin = new Thickness(10, 14, 10, 0),
        });

        return root;
    }

    private void EnterDigit(int digit)
    {
        if (!CanEditSelected())
        {
            return;
        }

        PushUndo();
        bool changed = pencilButton.IsChecked == true
            ? board.ToggleNote(selectedIndex, digit)
            : board.SetValue(selectedIndex, digit);
        if (!changed)
        {
            undo.Pop();
            return;
        }

        AfterMutation();
    }

    private void ClearSelected()
    {
        if (!CanEditSelected() || board.ValueAt(selectedIndex) == 0 && board.NotesAt(selectedIndex) == 0)
        {
            return;
        }

        PushUndo();
        if (board.ValueAt(selectedIndex) != 0)
        {
            board.SetValue(selectedIndex, 0);
        }
        else
        {
            int mask = board.NotesAt(selectedIndex);
            for (int digit = 1; digit <= 9; digit++)
            {
                if ((mask & (1 << digit)) != 0)
                {
                    board.ToggleNote(selectedIndex, digit);
                }
            }
        }
        AfterMutation();
    }

    private void ApplyHint()
    {
        if (board.IsComplete)
        {
            return;
        }

        PushUndo();
        int revealed = board.ApplyHint(selectedIndex);
        if (revealed < 0)
        {
            undo.Pop();
            return;
        }

        selectedIndex = revealed;
        statusText.Text = "One cell was revealed. Hints are recorded, but they never block completion.";
        AfterMutation(preserveStatus: true);
    }

    private void Undo()
    {
        if (undo.Count == 0)
        {
            return;
        }
        redo.Push(board.CaptureFrame());
        board.RestoreFrame(undo.Pop());
        AfterMutation();
    }

    private void Redo()
    {
        if (redo.Count == 0)
        {
            return;
        }
        undo.Push(board.CaptureFrame());
        board.RestoreFrame(redo.Pop());
        AfterMutation();
    }

    private void PushUndo()
    {
        undo.Push(board.CaptureFrame());
        redo.Clear();
    }

    private void AfterMutation(bool preserveStatus = false)
    {
        if (!preserveStatus)
        {
            statusText.Text = string.Empty;
        }

        if (board.IsComplete)
        {
            elapsed.Stop();
            clock.Stop();
            statusText.Text = puzzle.Pool == SudokuPuzzlePool.Daily
                ? "Daily complete. A quiet win for today."
                : "Puzzle complete. Nicely done.";
        }
        else if (!elapsed.IsRunning)
        {
            elapsed.Start();
            clock.Start();
        }

        Refresh();
        save(Snapshot);

        if (board.IsComplete)
        {
            completionSaved = true;
        }
    }

    private void Refresh()
    {
        for (int index = 0; index < cells.Length; index++)
        {
            Button cell = cells[index];
            int value = board.ValueAt(index);
            bool given = board.IsGiven(index);
            bool conflict = board.HasConflictAt(index);
            bool related = selectedIndex >= 0 && IsRelated(index, selectedIndex);
            bool selected = index == selectedIndex;

            cell.Content = value == 0 ? NotesText(board.NotesAt(index)) : value.ToString();
            cell.FontSize = value == 0 ? 10 : 27;
            cell.FontWeight = given ? FontWeights.Bold : FontWeights.SemiBold;
            cell.Foreground = conflict
                ? Ink("#F08F84")
                : given ? Ink("#F2E7D5") : Ink("#91D3BC");
            cell.Background = selected
                ? Ink("#36544F")
                : related ? Ink("#1C3030") : Ink("#142224");
        }

        timeText.Text = FormatTime(CurrentElapsedSeconds);
        hintText.Text = board.HintsUsed.ToString();
        undoButton.IsEnabled = undo.Count > 0;
        redoButton.IsEnabled = redo.Count > 0;
        RefreshPencilButton();

        if (board.IsComplete && !completionSaved)
        {
            completionSaved = true;
        }
    }

    private void RefreshPencilButton()
    {
        pencilButton.Background = pencilButton.IsChecked == true ? Ink("#36544F") : Ink("#17272A");
        pencilButton.BorderBrush = pencilButton.IsChecked == true ? Ink("#8EC9B0") : Ink("#54706D");
    }

    private bool CanEditSelected() => selectedIndex is >= 0 and < 81 && !board.IsGiven(selectedIndex) && !board.IsComplete;

    private void ClockOnTick(object? sender, EventArgs e)
    {
        timeText.Text = FormatTime(CurrentElapsedSeconds);
    }

    private long CurrentElapsedSeconds => elapsedBaseSeconds + (long)Math.Floor(elapsed.Elapsed.TotalSeconds);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        int digit = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            _ => 0,
        };
        if (digit > 0)
        {
            EnterDigit(digit);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
            case Key.D0:
            case Key.NumPad0:
                ClearSelected();
                e.Handled = true;
                break;
            case Key.Space:
                pencilButton.IsChecked = pencilButton.IsChecked != true;
                e.Handled = true;
                break;
            case Key.Z when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                Undo();
                e.Handled = true;
                break;
            case Key.Y when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                Redo();
                e.Handled = true;
                break;
            case Key.Left:
                MoveSelection(0, -1);
                e.Handled = true;
                break;
            case Key.Right:
                MoveSelection(0, 1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1, 0);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1, 0);
                e.Handled = true;
                break;
        }
    }

    private void MoveSelection(int rowDelta, int columnDelta)
    {
        int current = selectedIndex >= 0 ? selectedIndex : 0;
        int row = Math.Clamp(current / 9 + rowDelta, 0, 8);
        int column = Math.Clamp(current % 9 + columnDelta, 0, 8);
        selectedIndex = row * 9 + column;
        Refresh();
    }

    private static bool IsRelated(int left, int right)
    {
        int leftRow = left / 9;
        int leftColumn = left % 9;
        int rightRow = right / 9;
        int rightColumn = right % 9;
        return leftRow == rightRow || leftColumn == rightColumn ||
               (leftRow / 3 == rightRow / 3 && leftColumn / 3 == rightColumn / 3);
    }

    private static string NotesText(int mask)
    {
        if (mask == 0)
        {
            return string.Empty;
        }

        string[] rows = new string[3];
        for (int row = 0; row < 3; row++)
        {
            char[] values = new char[5];
            int cursor = 0;
            for (int column = 0; column < 3; column++)
            {
                int digit = row * 3 + column + 1;
                values[cursor++] = (mask & (1 << digit)) != 0 ? (char)('0' + digit) : '·';
                if (column < 2)
                {
                    values[cursor++] = ' ';
                }
            }
            rows[row] = new string(values);
        }
        return string.Join(Environment.NewLine, rows);
    }

    private static Thickness CellBorder(int index)
    {
        int row = index / 9;
        int column = index % 9;
        double left = column == 0 || column % 3 == 0 ? 2 : 0.5;
        double top = row == 0 || row % 3 == 0 ? 2 : 0.5;
        double right = column == 8 || column % 3 == 2 ? 2 : 0.5;
        double bottom = row == 8 || row % 3 == 2 ? 2 : 0.5;
        return new Thickness(left, top, right, bottom);
    }

    private static Border StatCard(string label, TextBlock value)
    {
        value.FontSize = 20;
        value.FontWeight = FontWeights.SemiBold;
        value.Foreground = Ink("#EDE5D6");
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9,
            Foreground = Ink("#7F9890"),
        });
        stack.Children.Add(value);
        return new Border
        {
            Background = Ink("#121F21"),
            BorderBrush = Ink("#304B48"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(3),
            Child = stack,
        };
    }

    private static Button MakeButton(string text, Brush background, Brush border) => new()
    {
        Content = text,
        Foreground = Ink("#EEE5D6"),
        Background = background,
        BorderBrush = border,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(12, 9, 12, 9),
        FontWeight = FontWeights.SemiBold,
        Cursor = Cursors.Hand,
    };

    private static string DifficultyName(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Relaxed => "Relaxed",
        SudokuDifficulty.Easy => "Easy",
        SudokuDifficulty.Medium => "Medium",
        SudokuDifficulty.Hard => "Hard",
        SudokuDifficulty.Expert => "Expert",
        SudokuDifficulty.Master => "Master",
        _ => difficulty.ToString(),
    };

    private static string FormatTime(long seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0L, seconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
