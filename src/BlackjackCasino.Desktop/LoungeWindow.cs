using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

internal sealed class LoungeWindow : Window
{
    private readonly Action<SudokuProgressState> persist;
    private readonly Action<bool> persistTimerVisibility;
    private readonly Action<bool> persistGuideVisibility;
    private readonly ContentControl contentHost = new();
    private SudokuProgressState progress;
    private SudokuView? sudokuView;
    private bool showTimer;
    private bool showGuides;

    public LoungeWindow(
        SudokuProgressState progress,
        Action<SudokuProgressState> persist,
        bool showTimer,
        Action<bool> persistTimerVisibility,
        bool showGuides = true,
        Action<bool>? persistGuideVisibility = null)
    {
        this.progress = SudokuProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.showTimer = showTimer;
        this.persistTimerVisibility = persistTimerVisibility ?? throw new ArgumentNullException(nameof(persistTimerVisibility));
        this.showGuides = showGuides;
        this.persistGuideVisibility = persistGuideVisibility ?? (_ => { });

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
            Margin = new Thickness(42, 28, 42, 34),
            MaxWidth = 1180,
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
            Margin = new Thickness(0, 0, 0, 24),
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
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });

        var copy = new StackPanel { Margin = new Thickness(0, 0, 34, 0) };
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
            Text = "Pencil marks, undo/redo, optional hints and autosave. Each Free Play difficulty keeps its own paused puzzle, while Daily puzzles remain in a completely separate library.",
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
        copy.Children.Add(new TextBlock
        {
            Text = "NEW PUZZLE replaces only the paused puzzle on that difficulty. Other difficulties remain untouched.",
            FontSize = 11,
            Foreground = Ink("#718E86"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
        });
        card.Children.Add(copy);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actions, 1);
        actions.Children.Add(new TextBlock
        {
            Text = "FREE PLAY",
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#809B93"),
            Margin = new Thickness(0, 0, 0, 7),
        });

        foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
        {
            actions.Children.Add(BuildDifficultyRow(difficulty));
        }

        var timerToggle = LoungeButton(showTimer ? "TIMER · VISIBLE" : "TIMER · HIDDEN");
        timerToggle.Name = "SudokuTimerToggle";
        timerToggle.Margin = new Thickness(0, 10, 0, 0);
        timerToggle.BorderBrush = Ink(showTimer ? "#668C7E" : "#4A6661");
        timerToggle.Click += (_, _) =>
        {
            showTimer = !showTimer;
            persistTimerVisibility(showTimer);
            ShowHub();
        };
        actions.Children.Add(timerToggle);

        actions.Children.Add(new TextBlock
        {
            Text = "The timer can be hidden completely. Your elapsed time is still preserved in the save.",
            FontSize = 10,
            Foreground = Ink("#718E86"),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 6, 8, 0),
        });

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        SudokuPuzzle daily = ResolveDaily(today);
        SudokuGameState dailyState = progress.Games.TryGetValue(daily.Id, out SudokuGameState? existingDaily)
            ? SudokuGameState.Normalize(daily, existingDaily)
            : SudokuGameState.Create(daily);
        Button dailyButton = LoungeButton(dailyState.Completed
            ? "TODAY'S DAILY · COMPLETE"
            : $"TODAY'S DAILY · {DifficultyLabel(daily.Difficulty).ToUpperInvariant()}");
        dailyButton.Name = "SudokuDailyButton";
        dailyButton.Margin = new Thickness(0, 14, 0, 0);
        dailyButton.BorderBrush = Ink("#8A9B6F");
        dailyButton.Background = Ink("#283126");
        dailyButton.Click += (_, _) => StartPuzzle(daily, today, forceNew: false);
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
            Text = "The bundled catalog is data-driven and validated before release. Free Play and Daily content never share the same puzzle pool.",
            FontSize = 11,
            Foreground = Ink("#617A74"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 18, 8, 0),
            TextAlignment = TextAlignment.Center,
        });

        scroll.Content = root;
        contentHost.Content = scroll;
    }

    private Border BuildDifficultyRow(SudokuDifficulty difficulty)
    {
        bool hasActive = TryGetActiveNormal(difficulty, out SudokuPuzzle? activePuzzle, out SudokuGameState? activeState) &&
            activePuzzle is not null && activeState is not null && !activeState.Completed;

        var row = new Border
        {
            Background = Ink(hasActive ? "#142827" : "#111D1F"),
            BorderBrush = Ink(hasActive ? "#4E756A" : "#304A47"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 7, 8, 7),
            Margin = new Thickness(0, 0, 0, 6),
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock
        {
            Text = DifficultyLabel(difficulty).ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#E9E0D1"),
        });
        var status = new TextBlock
        {
            Name = $"SudokuStatus_{difficulty}",
            Text = hasActive ? $"IN PROGRESS · {FormatTime(activeState!.ElapsedSeconds)}" : "NO ACTIVE PUZZLE",
            FontSize = 9,
            Foreground = Ink(hasActive ? "#8EC9B0" : "#6F8881"),
            Margin = new Thickness(0, 2, 8, 0),
        };
        labels.Children.Add(status);
        grid.Children.Add(labels);

        var resume = SmallActionButton("RESUME");
        resume.Name = $"SudokuResume_{difficulty}";
        resume.IsEnabled = hasActive;
        resume.Opacity = hasActive ? 1d : 0.42d;
        resume.Margin = new Thickness(6, 0, 4, 0);
        if (hasActive)
        {
            SudokuPuzzle puzzleToResume = activePuzzle!;
            resume.Click += (_, _) => StartPuzzle(puzzleToResume, null, forceNew: false);
        }
        Grid.SetColumn(resume, 1);
        grid.Children.Add(resume);

        var startNew = SmallActionButton("NEW PUZZLE");
        startNew.Name = $"SudokuNew_{difficulty}";
        startNew.Margin = new Thickness(4, 0, 0, 0);
        startNew.Click += (_, _) => StartNewNormal(difficulty);
        Grid.SetColumn(startNew, 2);
        grid.Children.Add(startNew);

        row.Child = grid;
        return row;
    }

    private void StartNewNormal(SudokuDifficulty difficulty)
    {
        IReadOnlyList<SudokuPuzzle> candidates = SudokuLibrary.ForDifficulty(difficulty);
        if (candidates.Count == 0)
        {
            return;
        }

        var completed = new HashSet<string>(progress.CompletedPuzzleIds, StringComparer.Ordinal);
        var seen = new HashSet<string>(progress.Games.Keys, StringComparer.Ordinal);
        SudokuPuzzle? current = null;
        if (TryGetActiveNormal(difficulty, out SudokuPuzzle? active, out _) && active is not null)
        {
            current = active;
        }

        SudokuPuzzle puzzle = candidates.FirstOrDefault(candidate => !completed.Contains(candidate.Id) && !seen.Contains(candidate.Id))
            ?? candidates.FirstOrDefault(candidate => !completed.Contains(candidate.Id) && candidate.Id != current?.Id)
            ?? candidates.FirstOrDefault(candidate => !completed.Contains(candidate.Id))
            ?? candidates.First();

        var games = new Dictionary<string, SudokuGameState>(progress.Games, StringComparer.Ordinal);
        var abandoned = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string id, SudokuGameState raw) in progress.Games)
        {
            if (!SudokuLibrary.TryGet(id, out SudokuPuzzle? existing) ||
                existing.Pool != SudokuPuzzlePool.Normal ||
                existing.Difficulty != difficulty)
            {
                continue;
            }

            SudokuGameState clean = SudokuGameState.Normalize(existing, raw);
            if (!clean.Completed)
            {
                games.Remove(id);
                abandoned.Add(id);
            }
        }

        string? activeNormalId = progress.ActiveNormalPuzzleId;
        if (activeNormalId is not null && abandoned.Contains(activeNormalId))
        {
            activeNormalId = null;
        }
        progress = progress with
        {
            Games = games,
            ActiveNormalPuzzleId = activeNormalId,
        };
        persist(progress);
        StartPuzzle(puzzle, null, forceNew: true);
    }

    private void StartPuzzle(SudokuPuzzle puzzle, DateOnly? dailyDate, bool forceNew)
    {
        SudokuGameState state = !forceNew && progress.Games.TryGetValue(puzzle.Id, out SudokuGameState? saved)
            ? SudokuGameState.Normalize(puzzle, saved)
            : SudokuGameState.Create(puzzle);

        sudokuView = new SudokuView(
            puzzle,
            state,
            showTimer,
            next =>
            {
                progress = progress.SaveGame(puzzle, next, dailyDate);
                persist(progress);
            },
            ShowHub,
            showGuides,
            visible =>
            {
                showGuides = visible;
                persistGuideVisibility(visible);
            });
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

    private bool TryGetActiveNormal(
        SudokuDifficulty difficulty,
        out SudokuPuzzle? puzzle,
        out SudokuGameState? state)
    {
        puzzle = null;
        state = null;

        if (progress.ActiveNormalPuzzleId is string preferredId &&
            TryRestoreActive(preferredId, difficulty, out puzzle, out state))
        {
            return true;
        }

        foreach ((string id, _) in progress.Games.Reverse())
        {
            if (TryRestoreActive(id, difficulty, out puzzle, out state))
            {
                return true;
            }
        }

        puzzle = null;
        state = null;
        return false;
    }

    private bool TryRestoreActive(
        string id,
        SudokuDifficulty difficulty,
        out SudokuPuzzle? puzzle,
        out SudokuGameState? state)
    {
        puzzle = null;
        state = null;
        if (!SudokuLibrary.TryGet(id, out SudokuPuzzle? found) ||
            found.Pool != SudokuPuzzlePool.Normal ||
            found.Difficulty != difficulty ||
            !progress.Games.TryGetValue(id, out SudokuGameState? saved))
        {
            return false;
        }

        SudokuGameState clean = SudokuGameState.Normalize(found, saved);
        if (clean.Completed)
        {
            return false;
        }

        puzzle = found;
        state = clean;
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

    private static Button SmallActionButton(string text) => new()
    {
        Content = text,
        Foreground = Ink("#EEE4D4"),
        Background = Ink("#17282A"),
        BorderBrush = Ink("#4A6661"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(10, 6, 10, 6),
        FontSize = 9,
        FontWeight = FontWeights.SemiBold,
        Cursor = Cursors.Hand,
        MinWidth = 82,
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

    private static string FormatTime(long seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0L, seconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
