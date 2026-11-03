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
    private readonly List<LoungeGame> games = [];
    private readonly Func<string>? rewardNotice;
    private Button? gamesHeaderBack;
    private bool showTimer;
    private bool showGuides;

    public LoungeWindow(
        SudokuProgressState progress,
        Action<SudokuProgressState> persist,
        bool showTimer,
        Action<bool> persistTimerVisibility,
        bool showGuides = true,
        Action<bool>? persistGuideVisibility = null,
        Func<string>? rewardNotice = null)
    {
        this.rewardNotice = rewardNotice;
        this.progress = SudokuProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.showTimer = showTimer;
        this.persistTimerVisibility = persistTimerVisibility ?? throw new ArgumentNullException(nameof(persistTimerVisibility));
        this.showGuides = showGuides;
        this.persistGuideVisibility = persistGuideVisibility ?? (_ => { });

        Title = "Chill Lounge";
        Width = 1600;
        Height = 940;
        MinWidth = 1080;
        MinHeight = 700;
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

        games.Add(new LoungeGame("sudoku", "Sudoku", "A little space to think.", "Six difficulties, saved progress and a separate offline Daily library.", ShowHub, () => new LoungeGameArtwork(false)));
        ShowGames();
    }

    private void ShowGames()
    {
        if (sudokuView is not null)
        {
            SudokuView previous = sudokuView;
            sudokuView = null;
            SaveActive(previous);
            previous.Dispose();
        }
        SetShellNavigation(null, null);
        contentHost.Content = new LoungeGameMenu(games, SetShellNavigation);
    }

    private UIElement BuildShell()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new Border
        {
            Background = Ink("#071A1C"),
            BorderBrush = Ink("#C5A368"),
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
            Foreground = Ink("#D7B978"),
        });
        brand.Children.Add(new TextBlock
        {
            Text = "Chill Lounge",
            FontSize = 24,
            FontFamily = new FontFamily("Georgia"),
            Foreground = Ink("#F3E7D5"),
        });
        bar.Children.Add(brand);

        gamesHeaderBack = LoungeButton(LoungeGameMenu.GamesBackLabel());
        gamesHeaderBack.Name = "LoungeGamesHeaderBack";
        gamesHeaderBack.Padding = new Thickness(15, 8, 15, 8);
        gamesHeaderBack.Margin = new Thickness(0, 0, 10, 0);
        gamesHeaderBack.Visibility = Visibility.Collapsed;
        gamesHeaderBack.Click += (_, _) =>
        {
            if (gamesHeaderBack.Tag is Action action) action();
        };

        var close = LoungeButton("RETURN TO CLUB");
        close.Padding = new Thickness(17, 8, 17, 8);
        close.Click += (_, _) => Close();

        var headerActions = new StackPanel { Orientation = Orientation.Horizontal };
        headerActions.Children.Add(gamesHeaderBack);
        headerActions.Children.Add(close);
        Grid.SetColumn(headerActions, 1);
        bar.Children.Add(headerActions);
        top.Child = bar;
        root.Children.Add(top);

        contentHost.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        contentHost.VerticalContentAlignment = VerticalAlignment.Stretch;
        Grid.SetRow(contentHost, 1);
        root.Children.Add(contentHost);
        return root;
    }

    private void SetShellNavigation(string? label, Action? action)
    {
        if (gamesHeaderBack is null) return;
        gamesHeaderBack.Content = label ?? LoungeGameMenu.GamesBackLabel();
        gamesHeaderBack.Tag = action;
        gamesHeaderBack.Visibility = action is null ? Visibility.Collapsed : Visibility.Visible;
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

        SetShellNavigation(LoungeGameMenu.GamesBackLabel(), ShowGames);
        progress = SudokuProgressState.Normalize(progress);

        int normalCompleted = progress.CompletedPuzzleIds.Count(id =>
            SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle.Pool == SudokuPuzzlePool.Normal);
        int dailyCompleted = progress.CompletedPuzzleIds.Count(id =>
            SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle.Pool == SudokuPuzzlePool.Daily);

        var entries = new List<SudokuPremiumMenu.DifficultyEntry>();
        foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
        {
            SudokuDifficulty selected = difficulty;
            bool canResume = TryGetActiveNormal(selected, out SudokuPuzzle? activePuzzle, out SudokuGameState? activeState)
                && activePuzzle is not null && activeState is not null && !activeState.Completed;
            SudokuPuzzle? resumePuzzle = activePuzzle;
            entries.Add(new SudokuPremiumMenu.DifficultyEntry(
                selected,
                DifficultyLabel(selected),
                canResume,
                canResume ? $"IN PROGRESS · {FormatTime(activeState!.ElapsedSeconds)}" : "NO ACTIVE PUZZLE",
                () =>
                {
                    if (resumePuzzle is not null)
                        StartPuzzle(resumePuzzle, null, forceNew: false);
                },
                () => StartNewNormal(selected)));
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        SudokuPuzzle daily = ResolveDaily(today);
        SudokuGameState dailyState = progress.Games.TryGetValue(daily.Id, out SudokuGameState? savedDaily)
            ? SudokuGameState.Normalize(daily, savedDaily)
            : SudokuGameState.Create(daily);

        contentHost.Content = SudokuPremiumMenu.Build(
            entries,
            normalCompleted,
            SudokuLibrary.Normal.Count,
            dailyCompleted,
            showTimer,
            () =>
            {
                showTimer = !showTimer;
                persistTimerVisibility(showTimer);
                ShowHub();
            },
            today,
            daily,
            dailyState.Completed,
            () => StartPuzzle(daily, today, forceNew: false));
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
            }, showBackButton: false);
        SetShellNavigation("← LOUNGE", ShowHub);
        sudokuView.CompletionRewardNotice = rewardNotice;
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
