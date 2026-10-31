using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Desktop;

internal static class WordSearchLoungeModule
{
    internal static LoungeHostedGame Create(
        WordSearchProgressState progress,
        Action<WordSearchProgressState> persist,
        Func<WordSearchStatistics> stats,
        Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion)
    {
        progress ??= new();
        progress.Normalize();
        var definition = new LoungeGame(
            "word-search",
            "Word Search",
            "Find words · Take your time",
            "Sweep through themed letter grids at your own pace. Drag across a word to mark it; there is no score pressure or mandatory timer.",
            () => { },
            Artwork: () => new WordSearchLoungeArtwork(),
            SortOrder: 30,
            Status: () => progress.ActiveGame is null
                ? $"{progress.CompletedGames.Count:N0} puzzles complete"
                : $"Resume · {progress.ActiveGame.Puzzle.Theme}");

        return new LoungeHostedGame(
            definition,
            backToGames => new WordSearchLoungeHub(progress, persist, stats, recordCompletion, backToGames));
    }
}

internal sealed class WordSearchLoungeHub : UserControl, IDisposable
{
    private readonly WordSearchProgressState progress;
    private readonly Action<WordSearchProgressState> persist;
    private readonly Func<WordSearchStatistics> stats;
    private readonly Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion;
    private readonly Action backToGames;
    private WordSearchView? activeView;
    private TextBlock status = null!;

    internal WordSearchLoungeHub(
        WordSearchProgressState progress,
        Action<WordSearchProgressState> persist,
        Func<WordSearchStatistics> stats,
        Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion,
        Action backToGames)
    {
        this.progress = progress;
        this.persist = persist;
        this.stats = stats;
        this.recordCompletion = recordCompletion;
        this.backToGames = backToGames;
        Background = Brush("#0B1715");
        Content = BuildHub();
    }

    private FrameworkElement BuildHub()
    {
        var root = new Grid { Margin = new Thickness(26) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(Label("CHILL LOUNGE · WORD SEARCH", 10, "#82B79D"));
        title.Children.Add(Label("Word Search", 30, "#F2E3C5", FontWeights.SemiBold));
        title.Children.Add(Label("Find the words. Keep the pace yours.", 12, "#AABCB3"));
        header.Children.Add(title);
        Button back = Button("← Games", "WordSearchBack");
        back.Click += (_, _) => backToGames();
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        root.Children.Add(header);

        WordSearchStatistics snapshot = stats();
        var summary = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 18) };
        summary.Children.Add(Stat("PUZZLES", snapshot.CompletedPuzzles.ToString("N0")));
        summary.Children.Add(Stat("WORDS FOUND", snapshot.WordsFound.ToString("N0")));
        summary.Children.Add(Stat("NO HINT", snapshot.NoHintCompletions.ToString("N0")));
        summary.Children.Add(Stat("MASTER", snapshot.MasterCompleted.ToString("N0")));
        Grid.SetRow(summary, 1);
        root.Children.Add(summary);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.65, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(content, 2);
        root.Children.Add(content);

        var levels = new UniformGrid { Columns = 2 };
        foreach (WordSearchDifficulty difficulty in Enum.GetValues<WordSearchDifficulty>())
            levels.Children.Add(DifficultyCard(difficulty));
        content.Children.Add(levels);

        var side = new StackPanel();
        if (progress.ActiveGame is not null)
        {
            var resume = Panel();
            var resumeStack = new StackPanel();
            resumeStack.Children.Add(Label("CURRENT PUZZLE", 10, "#D5B879"));
            resumeStack.Children.Add(Label(progress.ActiveGame.Puzzle.Theme, 19, "#F2E3C5", FontWeights.SemiBold));
            resumeStack.Children.Add(Label($"{progress.ActiveGame.Puzzle.Difficulty} · {progress.ActiveGame.FoundWords.Count} / {progress.ActiveGame.Puzzle.Placements.Count} words found", 11, "#9DB2A8"));
            Button resumeButton = Button("RESUME", "WordSearchResume");
            resumeButton.Margin = new Thickness(0, 12, 0, 0);
            resumeButton.Click += (_, _) => ShowGame(progress.ActiveGame);
            resumeStack.Children.Add(resumeButton);
            resume.Child = resumeStack;
            side.Children.Add(resume);
        }

        var info = Panel();
        info.Margin = new Thickness(0, progress.ActiveGame is null ? 0 : 12, 0, 0);
        var infoStack = new StackPanel();
        infoStack.Children.Add(Label("HOW IT WORKS", 10, "#D5B879"));
        infoStack.Children.Add(Label("Drag from the first letter to the last. Words can overlap. Higher difficulties add diagonals and backwards words.", 11, "#AABCB3"));
        infoStack.Children.Add(Label("Hints reveal a starting letter. They never reduce XP — this is the Chill Lounge.", 11, "#82B79D") { Margin = new Thickness(0, 10, 0, 0) });
        infoStack.Children.Add(Label($"{WordSearchCatalog.All.Count:N0} authored puzzle slots · fully offline", 10, "#7F948B") { Margin = new Thickness(0, 12, 0, 0) });
        info.Child = infoStack;
        side.Children.Add(info);

        status = Label("Choose a difficulty, or resume your current puzzle.", 11, "#C9B98F");
        status.Margin = new Thickness(2, 14, 0, 0);
        side.Children.Add(status);
        Grid.SetColumn(side, 2);
        content.Children.Add(side);
        return root;
    }

    private FrameworkElement DifficultyCard(WordSearchDifficulty difficulty)
    {
        (int size, int wordCount) = WordSearchCatalog.Spec(difficulty);
        IReadOnlyList<WordSearchPuzzleDefinition> pool = WordSearchCatalog.For(difficulty);
        int completed = progress.CompletedGames.Count(x => x.Difficulty == difficulty);
        var border = Panel();
        border.Margin = new Thickness(0, 0, 10, 10);
        var stack = new StackPanel();
        stack.Children.Add(Label(difficulty.ToString().ToUpperInvariant(), 11, difficulty >= WordSearchDifficulty.Expert ? "#E0BE73" : "#86BA9D", FontWeights.Bold));
        stack.Children.Add(Label($"{size} × {size} · {wordCount} words", 17, "#F0DFC2", FontWeights.SemiBold));
        stack.Children.Add(Label(DirectionText(difficulty), 10, "#91A79C"));
        stack.Children.Add(Label($"{Math.Min(completed, pool.Count)} / {pool.Count} completed", 10, "#7F948B") { Margin = new Thickness(0, 7, 0, 0) });
        var bar = new ProgressBar
        {
            Minimum = 0, Maximum = pool.Count, Value = Math.Min(completed, pool.Count), Height = 5,
            Margin = new Thickness(0, 6, 0, 10), Foreground = Brush("#72A990"), Background = Brush("#203A34"),
        };
        stack.Children.Add(bar);
        Button play = Button(completed >= pool.Count ? "REPLAY" : "PLAY", "WordSearchStart_" + difficulty);
        play.Click += (_, _) => Start(difficulty);
        stack.Children.Add(play);
        border.Child = stack;
        return border;
    }

    private void Start(WordSearchDifficulty difficulty)
    {
        if (progress.ActiveGame is not null)
        {
            status.Text = "Resume or finish the current puzzle before starting another one.";
            return;
        }
        WordSearchPuzzleDefinition definition = WordSearchGame.NextPuzzle(progress, difficulty);
        progress.ActiveGame = WordSearchGame.Start(definition);
        persist(progress);
        ShowGame(progress.ActiveGame);
    }

    private void ShowGame(WordSearchGameState game)
    {
        activeView?.Dispose();
        activeView = new WordSearchView(game, SaveGame, CompleteGame, BackToHub);
        Content = activeView;
    }

    private void SaveGame(WordSearchGameState game)
    {
        progress.ActiveGame = game;
        persist(progress);
    }

    private ProgressionAward CompleteGame(WordSearchGameState game)
    {
        WordSearchCompletionRecord completion = WordSearchGame.Complete(game);
        if (!progress.CompletedGames.Any(x => string.Equals(x.PuzzleId, completion.PuzzleId, StringComparison.Ordinal)))
            progress.CompletedGames.Add(completion);
        progress.ActiveGame = null;
        persist(progress);
        return recordCompletion(completion);
    }

    private void BackToHub()
    {
        activeView?.Dispose();
        activeView = null;
        Content = BuildHub();
    }

    private static string DirectionText(WordSearchDifficulty difficulty) => difficulty switch
    {
        WordSearchDifficulty.Relaxed => "Horizontal + vertical",
        WordSearchDifficulty.Easy => "Adds diagonals",
        _ => "All 8 directions · backwards included",
    };

    private static Border Stat(string caption, string value)
    {
        var stack = new StackPanel();
        stack.Children.Add(Label(caption, 9, "#7FA590"));
        stack.Children.Add(Label(value, 20, "#F0DFC2", FontWeights.SemiBold));
        var panel = Panel();
        panel.Margin = new Thickness(0, 0, 10, 0);
        panel.Child = stack;
        return panel;
    }

    internal static Border Panel() => new()
    {
        Background = Brush("#10251F"), BorderBrush = Brush("#315247"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10), Padding = new Thickness(14),
    };

    internal static TextBlock Label(string text, double size, string color, FontWeight? weight = null) => new()
    {
        Text = text, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
    };

    internal static Button Button(string text, string name) => new()
    {
        Content = text, Name = name, Padding = new Thickness(12, 8, 12, 8), Cursor = CasinoCursors.Hand,
        Background = Brush("#17382F"), Foreground = Brush("#F0DFC2"), BorderBrush = Brush("#5B765E"), BorderThickness = new Thickness(1),
    };

    internal static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    public void Dispose()
    {
        activeView?.Dispose();
        activeView = null;
    }
}

internal sealed class WordSearchView : UserControl, IDisposable
{
    private readonly WordSearchGameState game;
    private readonly Action<WordSearchGameState> save;
    private readonly Func<WordSearchGameState, ProgressionAward> complete;
    private readonly Action back;
    private readonly DispatcherTimer timer;
    private readonly Grid board = new();
    private readonly Dictionary<WordSearchCell, Border> cells = [];
    private readonly StackPanel words = new();
    private readonly TextBlock progress = WordSearchLoungeHub.Label("", 11, "#AABCB3");
    private readonly TextBlock status = WordSearchLoungeHub.Label("", 11, "#D7C38F");
    private WordSearchCell? dragStart;
    private WordSearchCell? dragEnd;
    private WordSearchPlacement? hint;
    private bool disposed;
    private int unsavedSeconds;

    internal WordSearchView(
        WordSearchGameState game,
        Action<WordSearchGameState> save,
        Func<WordSearchGameState, ProgressionAward> complete,
        Action back)
    {
        this.game = game;
        this.save = save;
        this.complete = complete;
        this.back = back;
        Background = WordSearchLoungeHub.Brush("#0B1715");
        Content = Build();
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += TimerTick;
        timer.Start();
        Refresh();
    }

    private FrameworkElement Build()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(WordSearchLoungeHub.Label($"WORD SEARCH · {game.Puzzle.Difficulty.ToString().ToUpperInvariant()}", 10, "#82B79D"));
        heading.Children.Add(WordSearchLoungeHub.Label(game.Puzzle.Theme, 26, "#F0DFC2", FontWeights.SemiBold));
        heading.Children.Add(progress);
        header.Children.Add(heading);
        Button games = WordSearchLoungeHub.Button("← Word Search", "WordSearchGameBack");
        games.Click += (_, _) => { SaveNow(); back(); };
        Grid.SetColumn(games, 1);
        header.Children.Add(games);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.75, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        BuildBoard();
        var boardFrame = WordSearchLoungeHub.Panel();
        boardFrame.Padding = new Thickness(12);
        boardFrame.Child = new Viewbox { Child = board, Stretch = Stretch.Uniform };
        body.Children.Add(boardFrame);

        var side = new StackPanel();
        var wordPanel = WordSearchLoungeHub.Panel();
        var wordStack = new StackPanel();
        wordStack.Children.Add(WordSearchLoungeHub.Label("WORDS", 10, "#D5B879"));
        wordStack.Children.Add(words);
        wordPanel.Child = wordStack;
        side.Children.Add(wordPanel);

        Button hintButton = WordSearchLoungeHub.Button("HINT · reveal a starting letter", "WordSearchHint");
        hintButton.Margin = new Thickness(0, 12, 0, 0);
        hintButton.Click += (_, _) => UseHint();
        side.Children.Add(hintButton);
        side.Children.Add(WordSearchLoungeHub.Label("Hints never reduce XP or block challenges except the optional no-hint milestones.", 10, "#7F948B") { Margin = new Thickness(2, 8, 0, 0) });
        Grid.SetColumn(side, 2);
        body.Children.Add(side);

        status.Margin = new Thickness(2, 12, 0, 0);
        Grid.SetRow(status, 2);
        root.Children.Add(status);
        return root;
    }

    private void BuildBoard()
    {
        int size = game.Puzzle.Size;
        board.Width = 600;
        board.Height = 600;
        for (int i = 0; i < size; i++)
        {
            board.RowDefinitions.Add(new RowDefinition());
            board.ColumnDefinitions.Add(new ColumnDefinition());
        }

        for (int row = 0; row < size; row++)
        for (int column = 0; column < size; column++)
        {
            var cell = new WordSearchCell(row, column);
            var border = new Border
            {
                Background = WordSearchLoungeHub.Brush("#142E28"),
                BorderBrush = WordSearchLoungeHub.Brush("#28483F"),
                BorderThickness = new Thickness(.6),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(1),
                Child = new TextBlock
                {
                    Text = game.Puzzle.At(row, column).ToString(), FontSize = Math.Max(16, 30 - size),
                    FontWeight = FontWeights.SemiBold, Foreground = WordSearchLoungeHub.Brush("#E7DEC8"),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            cells[cell] = border;
            board.Children.Add(border);
        }

        board.Background = Brushes.Transparent;
        board.MouseLeftButtonDown += BoardDown;
        board.MouseMove += BoardMove;
        board.MouseLeftButtonUp += BoardUp;
        board.LostMouseCapture += (_, _) => { dragStart = null; dragEnd = null; RefreshCells(); };
    }

    private void BoardDown(object sender, MouseButtonEventArgs e)
    {
        WordSearchCell? cell = CellAt(e.GetPosition(board));
        if (cell is null) return;
        dragStart = cell;
        dragEnd = cell;
        board.CaptureMouse();
        RefreshCells();
        e.Handled = true;
    }

    private void BoardMove(object sender, MouseEventArgs e)
    {
        if (dragStart is null || e.LeftButton != MouseButtonState.Pressed) return;
        WordSearchCell? cell = CellAt(e.GetPosition(board));
        if (cell is null) return;
        dragEnd = Snap(dragStart.Value, cell.Value);
        RefreshCells();
    }

    private void BoardUp(object sender, MouseButtonEventArgs e)
    {
        if (dragStart is null) return;
        WordSearchCell end = dragEnd ?? dragStart.Value;
        WordSearchCell start = dragStart.Value;
        board.ReleaseMouseCapture();
        dragStart = null;
        dragEnd = null;
        if (WordSearchGame.TryFind(game, start, end, out string? word))
        {
            hint = null;
            status.Text = $"Found {word}. Saved.";
            SaveNow();
            if (game.Completed)
            {
                timer.Stop();
                ProgressionAward award = complete(game);
                status.Text = $"Puzzle complete · +{award.PlayerExperience:N0} player XP · +{award.SeasonExperience:N0} season XP";
            }
        }
        else status.Text = "No listed word on that line. Keep looking.";
        Refresh();
        e.Handled = true;
    }

    private void UseHint()
    {
        hint = WordSearchGame.Hint(game);
        if (hint is null) { status.Text = "Every word is already found."; return; }
        status.Text = $"Hint: {hint.Word} begins at the glowing letter.";
        SaveNow();
        RefreshCells();
    }

    private void Refresh()
    {
        progress.Text = $"{game.FoundWords.Count} / {game.Puzzle.Placements.Count} words found · {FormatTime(game.ElapsedSeconds)} · {game.HintsUsed} hint{(game.HintsUsed == 1 ? "" : "s")}";
        words.Children.Clear();
        foreach (WordSearchPlacement placement in game.Puzzle.Placements.OrderBy(x => x.Word, StringComparer.Ordinal))
        {
            bool found = game.FoundWords.Contains(placement.Word);
            words.Children.Add(new TextBlock
            {
                Text = found ? "✓  " + placement.Word : "○  " + placement.Word,
                FontSize = 12,
                Foreground = WordSearchLoungeHub.Brush(found ? "#82C6A0" : "#D8D0BC"),
                TextDecorations = found ? TextDecorations.Strikethrough : null,
                Margin = new Thickness(0, 5, 0, 0),
            });
        }
        if (string.IsNullOrWhiteSpace(status.Text)) status.Text = "Drag across a listed word in either direction.";
        RefreshCells();
    }

    private void RefreshCells()
    {
        HashSet<WordSearchCell> found = game.Puzzle.Placements
            .Where(p => game.FoundWords.Contains(p.Word))
            .SelectMany(p => p.Cells())
            .ToHashSet();
        HashSet<WordSearchCell> selected = dragStart is { } start && dragEnd is { } end
            ? WordSearchGame.SelectionCells(game.Puzzle, start, end).ToHashSet()
            : [];
        WordSearchCell? hintCell = hint is null ? null : new WordSearchCell(hint.StartRow, hint.StartColumn);

        foreach ((WordSearchCell cell, Border border) in cells)
        {
            bool isSelected = selected.Contains(cell);
            bool isFound = found.Contains(cell);
            bool isHint = hintCell == cell;
            border.Background = WordSearchLoungeHub.Brush(isSelected ? "#7C6738" : isFound ? "#285842" : isHint ? "#725D2F" : "#142E28");
            border.BorderBrush = WordSearchLoungeHub.Brush(isSelected || isHint ? "#E3C376" : isFound ? "#63A47F" : "#28483F");
            border.BorderThickness = new Thickness(isSelected || isHint ? 1.6 : .6);
        }
    }

    private WordSearchCell? CellAt(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= board.ActualWidth || point.Y >= board.ActualHeight) return null;
        int size = game.Puzzle.Size;
        int column = Math.Clamp((int)(point.X / board.ActualWidth * size), 0, size - 1);
        int row = Math.Clamp((int)(point.Y / board.ActualHeight * size), 0, size - 1);
        return new(row, column);
    }

    private WordSearchCell Snap(WordSearchCell start, WordSearchCell raw)
    {
        int dr = raw.Row - start.Row;
        int dc = raw.Column - start.Column;
        if (dr == 0 || dc == 0 || Math.Abs(dr) == Math.Abs(dc)) return raw;
        if (Math.Abs(dr) > Math.Abs(dc) * 2) return new(raw.Row, start.Column);
        if (Math.Abs(dc) > Math.Abs(dr) * 2) return new(start.Row, raw.Column);
        int distance = Math.Min(Math.Abs(dr), Math.Abs(dc));
        return new(start.Row + Math.Sign(dr) * distance, start.Column + Math.Sign(dc) * distance);
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        if (game.Completed) return;
        game.ElapsedSeconds = checked(game.ElapsedSeconds + 1);
        unsavedSeconds++;
        progress.Text = $"{game.FoundWords.Count} / {game.Puzzle.Placements.Count} words found · {FormatTime(game.ElapsedSeconds)} · {game.HintsUsed} hint{(game.HintsUsed == 1 ? "" : "s")}";
        if (unsavedSeconds >= 10) SaveNow();
    }

    private void SaveNow()
    {
        unsavedSeconds = 0;
        save(game);
    }

    private static string FormatTime(long seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        if (!game.Completed) SaveNow();
    }
}

internal sealed class WordSearchLoungeArtwork : FrameworkElement
{
    internal WordSearchLoungeArtwork()
    {
        Width = 310;
        Height = 130;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        Brush bg = WordSearchLoungeHub.Brush("#0D241D");
        Brush panel = WordSearchLoungeHub.Brush("#15372C");
        Brush line = WordSearchLoungeHub.Brush("#416D59");
        Brush gold = WordSearchLoungeHub.Brush("#D6B96D");
        Brush text = WordSearchLoungeHub.Brush("#E9DFC8");
        dc.DrawRoundedRectangle(bg, new Pen(line, 1), new Rect(0, 0, 310, 130), 10, 10);
        dc.DrawRoundedRectangle(panel, new Pen(line, 1), new Rect(20, 15, 146, 100), 7, 7);
        string[] rows = ["M O O N N", "J A Z Z E", "C A L M O", "B O O K N", "S T A R S"];
        for (int row = 0; row < rows.Length; row++)
        {
            var ft = new FormattedText(rows[row], System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 13, text, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(34, 25 + row * 17));
        }
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 214, 185, 109)), new Pen(gold, 2), new Rect(32, 41, 112, 18), 9, 9);
        dc.DrawEllipse(null, new Pen(gold, 4), new Point(218, 53), 28, 28);
        dc.DrawLine(new Pen(gold, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, new Point(238, 74), new Point(266, 102));
        var title = new FormattedText("FIND", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia"), 18, gold, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(title, new Point(202, 92));
    }
}
