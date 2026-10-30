using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

internal sealed class LoungeWindow : Window
{
    private readonly Action<SudokuProgressState> persist;
    private readonly ContentControl contentHost = new();
    private SudokuProgressState progress;
    private SudokuView? sudokuView;
    private ComboBox? difficultyPicker;

    public LoungeWindow(SudokuProgressState progress, Action<SudokuProgressState> persist)
    {
        this.progress = SudokuProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));

        Title = "Chill Lounge";
        Width = 1180;
        Height = 780;
        MinWidth = 980;
        MinHeight = 680;
        Background = Ink("#0A1315");
        Foreground = Ink("#F0E7D8");
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = BuildShell();

        Closed += (_, _) =>
        {
            if (sudokuView is not null)
            {
                SudokuView active = sudokuView;
                sudokuView = null;
                SaveActive(active);
                active.Dispose();
            }
        };

        ShowHub();
    }

    private UIElement BuildShell()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new Border
        {
            Background = Ink("#0E1C1E"),
            BorderBrush = Ink("#2C4846"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(28, 15, 28, 15),
        };
        var bar = new Grid();
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var brand = new StackPanel();
        brand.Children.Add(new TextBlock
        {
            Text = "THE MIDNIGHT CLUB",
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#7FB49F"),
        });
        brand.Children.Add(new TextBlock
        {
            Text = "Chill Lounge",
            FontSize = 24,
            FontFamily = new FontFamily("Georgia"),
            Foreground = Ink("#F3E7D5"),
        });
        bar.Children.Add(brand);

        var close = LoungeButton("RETURN TO CLUB");
        close.Padding = new Thickness(17, 8, 17, 8);
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        bar.Children.Add(close);
        top.Child = bar;
        root.Children.Add(top);

        contentHost.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        contentHost.VerticalContentAlignment = VerticalAlignment.Stretch;
        Grid.SetRow(contentHost, 1);
        root.Children.Add(contentHost);
        return root;
    }

    private void ShowHub()
    {
        if (sudokuView is not null)
        {
            SudokuView previous = sudokuView;
            sudokuView = null;
            SaveActive(previous);
            previous.Dispose();
        }

        progress = SudokuProgressState.Normalize(progress);
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var root = new StackPanel
        {
            Margin = new Thickness(52, 36, 52, 40),
            MaxWidth = 1040,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        root.Children.Add(new TextBlock
        {
            Text = "A QUIETER CORNER",
            Foreground = Ink("#7FB49F"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
        });
        root.Children.Add(new TextBlock
        {
            Text = "Take your time.",
            Foreground = Ink("#F4E8D7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 40,
            Margin = new Thickness(0, 7, 0, 8),
        });
        root.Children.Add(new TextBlock
        {
            Text = "No wagers, no pressure. Pick something small, settle in and leave whenever you want — progress is saved locally.",
            Foreground = Ink("#A9BBB5"),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 28),
        });

        var sudokuCard = new Border
        {
            Background = Ink("#101F21"),
            BorderBrush = Ink("#42645E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(26),
        };
        var card = new Grid();
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340 });
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });

        var copy = new StackPanel { Margin = new Thickness(0, 0, 30, 0) };
        copy.Children.Add(new TextBlock
        {
            Text = "SUDOKU",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#8EC9B0"),
        });
        copy.Children.Add(new TextBlock
        {
            Text = "One grid. No clock to beat.",
            FontSize = 28,
            FontFamily = new FontFamily("Georgia"),
            Foreground = Ink("#F2E5D3"),
            Margin = new Thickness(0, 6, 0, 12),
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Pencil marks, undo/redo, optional hints and autosave. Normal puzzles and Daily puzzles live in separate libraries, so a Daily never reappears during free play.",
            FontSize = 13,
            Foreground = Ink("#ABC0B9"),
            TextWrapping = TextWrapping.Wrap,
        });
        int normalCompleted = progress.CompletedPuzzleIds.Count(id =>
            SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle.Pool == SudokuPuzzlePool.Normal);
        int dailyCompleted = progress.CompletedPuzzleIds.Count(id =>
            SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle.Pool == SudokuPuzzlePool.Daily);
        copy.Children.Add(new TextBlock
        {
            Text = $"FREE PLAY  {normalCompleted}/{SudokuLibrary.Normal.Count}   ·   DAILY  {dailyCompleted} completed",
            FontSize = 11,
            Foreground = Ink("#718E86"),
            Margin = new Thickness(0, 18, 0, 0),
        });
        card.Children.Add(copy);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actions, 1);
        actions.Children.Add(new TextBlock
        {
            Text = "FREE PLAY DIFFICULTY",
            FontSize = 9,
            Foreground = Ink("#809B93"),
            Margin = new Thickness(0, 0, 0, 6),
        });
        difficultyPicker = new ComboBox
        {
            ItemsSource = Enum.GetValues<SudokuDifficulty>(),
            SelectedItem = SudokuDifficulty.Medium,
            MinHeight = 38,
            Margin = new Thickness(0, 0, 0, 10),
        };
        actions.Children.Add(difficultyPicker);

        Button start = LoungeButton("START NEXT PUZZLE");
        start.Click += (_, _) => StartNextNormal();
        actions.Children.Add(start);

        if (TryGetActiveNormal(out SudokuPuzzle? activeNormal, out SudokuGameState? activeState) && !activeState.Completed)
        {
            Button resume = LoungeButton($"CONTINUE {DifficultyLabel(activeNormal.Difficulty).ToUpperInvariant()}");
            resume.Margin = new Thickness(0, 8, 0, 0);
            resume.Click += (_, _) => StartPuzzle(activeNormal, null);
            actions.Children.Add(resume);
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        SudokuPuzzle daily = ResolveDaily(today);
        SudokuGameState dailyState = progress.Games.TryGetValue(daily.Id, out SudokuGameState? existingDaily)
            ? SudokuGameState.Normalize(daily, existingDaily)
            : SudokuGameState.Create(daily);
        Button dailyButton = LoungeButton(dailyState.Completed
            ? "TODAY'S DAILY · COMPLETE"
            : $"TODAY'S DAILY · {DifficultyLabel(daily.Difficulty).ToUpperInvariant()}");
        dailyButton.Margin = new Thickness(0, 14, 0, 0);
        dailyButton.BorderBrush = Ink("#8A9B6F");
        dailyButton.Background = Ink("#283126");
        dailyButton.Click += (_, _) => StartPuzzle(daily, today);
        actions.Children.Add(dailyButton);

        actions.Children.Add(new TextBlock
        {
            Text = today.ToString("dddd, dd MMMM", System.Globalization.CultureInfo.CurrentCulture),
            FontSize = 10,
            Foreground = Ink("#718E86"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 7, 0, 0),
        });

        card.Children.Add(actions);
        sudokuCard.Child = card;
        root.Children.Add(sudokuCard);

        root.Children.Add(new TextBlock
        {
            Text = "The bundled catalog is intentionally data-driven. This first slice proves the systems; the long-term target is thousands of separately validated free-play and Daily puzzles.",
            FontSize = 11,
            Foreground = Ink("#617A74"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 18, 8, 0),
            TextAlignment = TextAlignment.Center,
        });

        scroll.Content = root;
        contentHost.Content = scroll;
    }

    private void StartNextNormal()
    {
        SudokuDifficulty difficulty = difficultyPicker?.SelectedItem is SudokuDifficulty selected
            ? selected
            : SudokuDifficulty.Medium;
        IReadOnlyList<SudokuPuzzle> candidates = SudokuLibrary.ForDifficulty(difficulty);
        if (candidates.Count == 0)
        {
            return;
        }

        var completed = new HashSet<string>(progress.CompletedPuzzleIds, StringComparer.Ordinal);
        SudokuPuzzle puzzle = candidates.FirstOrDefault(candidate => !completed.Contains(candidate.Id))
            ?? candidates[0];
        StartPuzzle(puzzle, null);
    }

    private void StartPuzzle(SudokuPuzzle puzzle, DateOnly? dailyDate)
    {
        SudokuGameState state = progress.Games.TryGetValue(puzzle.Id, out SudokuGameState? saved)
            ? SudokuGameState.Normalize(puzzle, saved)
            : SudokuGameState.Create(puzzle);

        sudokuView = new SudokuView(
            puzzle,
            state,
            next =>
            {
                progress = progress.SaveGame(puzzle, next, dailyDate);
                persist(progress);
            },
            ShowHub);
        contentHost.Content = sudokuView;
        sudokuView.Focus();
    }

    private void SaveActive(SudokuView view)
    {
        SudokuGameState snapshot = view.Snapshot;
        SudokuPuzzle puzzle = SudokuLibrary.Get(snapshot.PuzzleId);
        DateOnly? date = puzzle.Pool == SudokuPuzzlePool.Daily
            ? FindDailyDate(puzzle.Id)
            : null;
        progress = progress.SaveGame(puzzle, snapshot, date);
        persist(progress);
    }

    private SudokuPuzzle ResolveDaily(DateOnly date)
    {
        if (progress.TryGetDaily(date, out SudokuPuzzle? assigned) && assigned is not null)
        {
            return assigned;
        }

        SudokuPuzzle puzzle = SudokuDailySchedule.ForDate(date);
        progress = progress.AssignDaily(date, puzzle);
        persist(progress);
        return puzzle;
    }

    private DateOnly? FindDailyDate(string puzzleId)
    {
        foreach ((string rawDate, string id) in progress.DailyAssignments)
        {
            if (!string.Equals(id, puzzleId, StringComparison.Ordinal))
            {
                continue;
            }
            if (DateOnly.TryParseExact(
                rawDate,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateOnly date))
            {
                return date;
            }
        }
        return null;
    }

    private bool TryGetActiveNormal(out SudokuPuzzle? puzzle, out SudokuGameState? state)
    {
        puzzle = null;
        state = null;
        if (progress.ActiveNormalPuzzleId is not string id ||
            !SudokuLibrary.TryGet(id, out SudokuPuzzle? found) ||
            found.Pool != SudokuPuzzlePool.Normal)
        {
            return false;
        }

        puzzle = found;
        state = progress.Games.TryGetValue(id, out SudokuGameState? saved)
            ? SudokuGameState.Normalize(found, saved)
            : SudokuGameState.Create(found);
        return true;
    }

    private static Button LoungeButton(string text) => new()
    {
        Content = text,
        Foreground = Ink("#EEE4D4"),
        Background = Ink("#17282A"),
        BorderBrush = Ink("#4A6661"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(15, 10, 15, 10),
        FontWeight = FontWeights.SemiBold,
        Cursor = Cursors.Hand,
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };

    private static string DifficultyLabel(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Relaxed => "Relaxed",
        SudokuDifficulty.Easy => "Easy",
        SudokuDifficulty.Medium => "Medium",
        SudokuDifficulty.Hard => "Hard",
        SudokuDifficulty.Expert => "Expert",
        SudokuDifficulty.Master => "Master",
        _ => difficulty.ToString(),
    };

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
