using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
        Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion,
        Func<bool>? sounds = null, Func<bool>? animations = null)
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
            backToGames => new WordSearchLoungeHub(progress, persist, stats, recordCompletion, backToGames, sounds ?? (() => true), animations ?? (() => true)));
    }
}

internal sealed class WordSearchLoungeHub : UserControl, IDisposable
{
    private readonly WordSearchProgressState progress;
    private readonly Action<WordSearchProgressState> persist;
    private readonly Func<WordSearchStatistics> stats;
    private readonly Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private WordSearchView? activeView;
    private TextBlock status = null!;

    internal WordSearchLoungeHub(
        WordSearchProgressState progress,
        Action<WordSearchProgressState> persist,
        Func<WordSearchStatistics> stats,
        Func<WordSearchCompletionRecord, ProgressionAward> recordCompletion,
        Action backToGames, Func<bool> sounds, Func<bool> animations)
    {
        this.progress = progress;
        this.persist = persist;
        this.stats = stats;
        this.recordCompletion = recordCompletion;
        _ = backToGames;
        this.sounds = sounds;
        this.animations = animations;
        Unloaded += (_, _) => Dispose();
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
        Grid.SetRow(content, 2);
        root.Children.Add(content);

        var levels = new UniformGrid { Name = "WordSearchDifficulties", Columns = 3, Rows = 2 };
        foreach (WordSearchDifficulty difficulty in Enum.GetValues<WordSearchDifficulty>())
            levels.Children.Add(DifficultyCard(difficulty));
        content.Children.Add(levels);

        var side = new StackPanel { Width = 410, Margin = new Thickness(20, 0, 0, 0) };
        if (progress.ActiveGame is not null)
        {
            var resume = Panel();
            var resumeStack = new DockPanel();
            Button resumeButton = ActionButton("RESUME", "WordSearchResume");
            resumeButton.Margin = new Thickness(12, 0, 0, 0);
            resumeButton.Click += (_, _) => ShowGame(progress.ActiveGame);
            DockPanel.SetDock(resumeButton, Dock.Right); resumeStack.Children.Add(resumeButton);
            var resumeCopy = new StackPanel();
            resumeCopy.Children.Add(Label("CURRENT PUZZLE", 10, "#D5B879"));
            resumeCopy.Children.Add(Label(progress.ActiveGame.Puzzle.Theme, 19, "#F2E3C5", FontWeights.SemiBold));
            resumeCopy.Children.Add(Label($"{progress.ActiveGame.Puzzle.Difficulty} · {progress.ActiveGame.FoundWords.Count} / {progress.ActiveGame.Puzzle.Placements.Count} words found", 12, "#9DB2A8"));
            resumeStack.Children.Add(resumeCopy);
            resume.Child = resumeStack;
            side.Children.Add(resume);
        }

        var info = Panel();
        info.ToolTip = "Drag from the first letter to the last. Words can overlap. Higher difficulties add diagonals and backwards words. Hints reveal a starting letter and never reduce XP.";
        info.Visibility = progress.ActiveGame is null ? Visibility.Visible : Visibility.Collapsed;
        var infoStack = new StackPanel();
        infoStack.Children.Add(Label("HOW IT WORKS", 10, "#D5B879"));
        infoStack.Children.Add(Label("Drag across words. Hints never reduce XP.", 14, "#AABCB3"));
        infoStack.Children.Add(MarginLabel($"{WordSearchCatalog.All.Count:N0} authored puzzle slots · fully offline", 10, "#7F948B", new Thickness(0, 5, 0, 0)));
        info.Child = infoStack;
        side.Children.Add(info);

        status = Label("Choose a difficulty, or resume your current puzzle.", 11, "#C9B98F");
        status.Margin = new Thickness(2, 5, 0, 0);
        side.Children.Add(status);
        Grid.SetColumn(side, 1);
        header.Children.Add(side);
        return root;
    }

    private FrameworkElement DifficultyCard(WordSearchDifficulty difficulty)
    {
        (int size, int wordCount) = WordSearchCatalog.Spec(difficulty);
        IReadOnlyList<WordSearchPuzzleDefinition> pool = WordSearchCatalog.For(difficulty);
        int completed = progress.CompletedGames.Count(x => x.Difficulty == difficulty);
        var border = Panel();
        border.Margin = new Thickness(0, 0, 10, 10);
        border.Padding = new Thickness(18, 12, 18, 12);
        var card = new Grid();
        card.RowDefinitions.Add(new RowDefinition());
        card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        string accent = difficulty >= WordSearchDifficulty.Expert ? "#E0BE73" : "#86BA9D";
        stack.Children.Add(Label(difficulty.ToString().ToUpperInvariant(), 16, accent, FontWeights.Bold));
        stack.Children.Add(Label($"{size} × {size}", 34, "#F0DFC2", FontWeights.SemiBold));
        stack.Children.Add(Label(DirectionText(difficulty), 14, "#A7BDB1"));
        stack.Children.Add(MarginLabel($"{Math.Min(completed, pool.Count)} / {pool.Count} completed", 14, "#91AC9D", new Thickness(0, 5, 0, 0)));
        var meta = new Grid { Name = "WordSearchDifficultyMeta", Margin = new Thickness(0, 5, 0, 3) };
        meta.ColumnDefinitions.Add(new ColumnDefinition());
        meta.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        meta.Children.Add(Label($"{wordCount} WORDS", 14, "#B0C6BA", FontWeights.SemiBold));
        var xp = Label($"+{WordSearchProgression.PlayerExperience(difficulty):N0} XP", 15, accent, FontWeights.Bold);
        Grid.SetColumn(xp, 1); meta.Children.Add(xp); stack.Children.Add(meta);
        stack.Children.Add(new ProgressBar { Minimum = 0, Maximum = pool.Count, Value = Math.Min(completed, pool.Count),
            Height = 6, Margin = new Thickness(0, 4, 0, 8), Foreground = Brush(accent), Background = Brush("#203A34"), BorderThickness = new Thickness(0) });
        // Scale only on smaller windows; the grid still distributes all available card space.
        card.Children.Add(new Viewbox { Child = stack, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Stretch });
        stack.Width = 300;
        border.SizeChanged += (_, _) => stack.Width = Math.Max(180, border.ActualWidth - 38);
        Button play = ActionButton(completed >= pool.Count ? "REPLAY" : "PLAY", "WordSearchStart_" + difficulty);
        play.FontSize = 15; play.Padding = new Thickness(12, 7, 12, 7);
        play.Click += (_, _) => Start(difficulty);
        Grid.SetRow(play, 1); card.Children.Add(play);
        border.Child = card;
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
        activeView = new WordSearchView(game, SaveGame, CompleteGame, BackToHub, sounds, animations);
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
        stack.Children.Add(Label(caption, 13, "#7FA590"));
        stack.Children.Add(Label(value, 30, "#F0DFC2", FontWeights.SemiBold));
        var panel = Panel();
        panel.Margin = new Thickness(0, 0, 10, 0);
        panel.Child = stack;
        return panel;
    }

    internal static Border Panel() => new()
    {
        Background = new LinearGradientBrush(((SolidColorBrush)Brush("#19372C")).Color, ((SolidColorBrush)Brush("#0E211C")).Color, 100),
        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = .26 },
        BorderBrush = Brush("#315247"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(14),
    };

    internal static TextBlock Label(string text, double size, string color, FontWeight? weight = null) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = Brush(color),
        FontWeight = weight ?? FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
    };

    internal static TextBlock MarginLabel(string text, double size, string color, Thickness margin, FontWeight? weight = null)
    {
        TextBlock label = Label(text, size, color, weight);
        label.Margin = margin;
        return label;
    }

    internal static Button ActionButton(string text, string name) => new()
    {
        Content = text,
        Name = name,
        Padding = new Thickness(12, 8, 12, 8),
        Cursor = CasinoCursors.Hand,
        Background = Brush("#17382F"),
        Foreground = Brush("#F0DFC2"),
        BorderBrush = Brush("#5B765E"),
        BorderThickness = new Thickness(1),
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SolidColorBrush> BrushesByColor = new();
    internal static SolidColorBrush Brush(string hex) => BrushesByColor.GetOrAdd(hex, value =>
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    });

    public void Dispose()
    {
        activeView?.Dispose();
        activeView = null;
    }
}

internal sealed partial class WordSearchView : UserControl, IDisposable
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
    private HashSet<WordSearchCell> foundCells = [];
    private WordSearchCell? dragStart;
    private WordSearchCell? dragEnd;
    private WordSearchPlacement? hint;
    private bool disposed;
    private int unsavedSeconds;

    internal WordSearchView(
        WordSearchGameState game,
        Action<WordSearchGameState> save,
        Func<WordSearchGameState, ProgressionAward> complete,
        Action back, Func<bool> sounds, Func<bool> animations)
    {
        this.game = game;
        this.save = save;
        this.complete = complete;
        this.back = back;
        this.sounds = sounds;
        this.animations = animations;
        Background = WordSearchLoungeHub.Brush("#0B1715");
        Content = Build();
        Unloaded += (_, _) => Dispose();
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
        Button games = WordSearchLoungeHub.ActionButton("← Word Search", "WordSearchGameBack");
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

        var side = new Grid();
        side.RowDefinitions.Add(new RowDefinition());
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var wordPanel = WordSearchLoungeHub.Panel();
        var wordStack = new StackPanel();
        wordStack.Children.Add(WordSearchLoungeHub.Label("WORDS TO DISCOVER", 16, "#D5B879"));
        wordStack.Children.Add(words);
        wordStack.Width = 380;
        wordPanel.Child = new Viewbox { Child = wordStack, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, VerticalAlignment = VerticalAlignment.Top };
        side.Children.Add(wordPanel);

        Button hintButton = WordSearchLoungeHub.ActionButton("HINT · reveal a starting letter", "WordSearchHint");
        hintButton.Margin = new Thickness(0, 12, 0, 0);
        hintButton.Click += (_, _) => UseHint();
        Grid.SetRow(hintButton, 1); side.Children.Add(hintButton);
        var hintInfo = WordSearchLoungeHub.MarginLabel(
            "Hints are tracked separately, but they never reduce XP in the Chill Lounge.",
            10,
            "#7F948B",
            new Thickness(2, 8, 0, 0));
        Grid.SetRow(hintInfo, 2); side.Children.Add(hintInfo);
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
                    Text = game.Puzzle.At(row, column).ToString(),
                    FontSize = Math.Max(16, 30 - size),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = WordSearchLoungeHub.Brush("#E7DEC8"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
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
        board.LostMouseCapture += (_, _) =>
        {
            dragStart = null;
            dragEnd = null;
            RefreshCells();
        };
    }

    private void BoardDown(object sender, MouseButtonEventArgs e)
    {
        WordSearchCell? cell = CellAt(e.GetPosition(board));
        if (cell is null || game.Completed || disposed) return;
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
        if (dragStart is null || game.Completed || disposed) return;
        WordSearchCell end = dragEnd ?? dragStart.Value;
        WordSearchCell start = dragStart.Value;
        board.ReleaseMouseCapture();
        dragStart = null;
        dragEnd = null;

        SubmitSelection(start, end);
        e.Handled = true;
    }

    internal void SubmitSelection(WordSearchCell start, WordSearchCell end)
    {
        if (disposed || game.Completed) return;
        if (WordSearchGame.TryFind(game, start, end, out string? word))
        {
            hint = null;
            status.Text = $"Found {word}. Saved.";
            if (!game.Completed) SaveNow();
            if (game.Completed)
            {
                timer.Stop();
                ProgressionAward award = complete(game);
                status.Text = award.PlayerExperience > 0
                    ? $"Puzzle complete · +{award.PlayerExperience:N0} player XP · +{award.SeasonExperience:N0} season XP"
                    : "Puzzle complete · saved.";
                ShowCompletion(award);
            }
        }
        else
        {
            status.Text = "No listed word on that line. Keep looking.";
        }
        Refresh();
        if (word is not null) CelebrateWord(start, end);
    }

    private void UseHint()
    {
        hint = WordSearchGame.Hint(game);
        if (hint is null)
        {
            status.Text = "Every word is already found.";
            return;
        }
        status.Text = $"Hint: {hint.Word} begins at the glowing letter.";
        SaveNow();
        Refresh();
    }

    private void Refresh()
    {
        foundCells = game.Puzzle.Placements.Where(p => game.FoundWords.Contains(p.Word)).SelectMany(p => p.Cells()).ToHashSet();
        progress.Text = $"{game.FoundWords.Count} / {game.Puzzle.Placements.Count} words found · {FormatTime(game.ElapsedSeconds)} · {game.HintsUsed} hint{(game.HintsUsed == 1 ? "" : "s")}";
        words.Children.Clear();
        foreach (WordSearchPlacement placement in game.Puzzle.Placements.OrderBy(x => x.Word, StringComparer.Ordinal))
        {
            bool found = game.FoundWords.Contains(placement.Word);
            words.Children.Add(new TextBlock
            {
                Text = found ? "✓  " + placement.Word : "○  " + placement.Word,
                FontSize = game.Puzzle.Placements.Count > 10 ? 18 : 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = WordSearchLoungeHub.Brush(found ? "#82C6A0" : "#D8D0BC"),
                TextDecorations = found ? TextDecorations.Strikethrough : null,
                Margin = new Thickness(4, 6, 0, 4),
            });
        }
        if (string.IsNullOrWhiteSpace(status.Text))
            status.Text = "Drag across a listed word in either direction.";
        RefreshCells();
    }

    private void RefreshCells()
    {
        HashSet<WordSearchCell> selected = dragStart is { } start && dragEnd is { } end
            ? WordSearchGame.SelectionCells(game.Puzzle, start, end).ToHashSet()
            : [];
        WordSearchCell? hintCell = hint is null ? null : new WordSearchCell(hint.StartRow, hint.StartColumn);

        foreach ((WordSearchCell cell, Border border) in cells)
        {
            bool isSelected = selected.Contains(cell);
            bool isFound = foundCells.Contains(cell);
            bool isHint = hintCell == cell;
            border.Background = WordSearchLoungeHub.Brush(isSelected ? "#7C6738" : isFound ? "#285842" : isHint ? "#725D2F" : "#142E28");
            border.BorderBrush = WordSearchLoungeHub.Brush(isSelected || isHint ? "#E3C376" : isFound ? "#63A47F" : "#28483F");
            border.BorderThickness = new Thickness(isSelected || isHint ? 1.6 : .6);
        }
    }

    private WordSearchCell? CellAt(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= board.ActualWidth || point.Y >= board.ActualHeight)
            return null;
        int size = game.Puzzle.Size;
        int column = Math.Clamp((int)(point.X / board.ActualWidth * size), 0, size - 1);
        int row = Math.Clamp((int)(point.Y / board.ActualHeight * size), 0, size - 1);
        return new(row, column);
    }

    private static WordSearchCell Snap(WordSearchCell start, WordSearchCell raw)
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
        if (game.Completed) return;
        unsavedSeconds = 0;
        save(game);
    }

    private static string FormatTime(long seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        timer.Tick -= TimerTick;
        board.ReleaseMouseCapture();
        StopFeedback();
        CasinoAudio.Mixer.Stop(audioOwner);
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
            var ft = new FormattedText(
                rows[row],
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                13,
                text,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(34, 25 + row * 17));
        }
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 214, 185, 109)), new Pen(gold, 2), new Rect(32, 41, 112, 18), 9, 9);
        dc.DrawEllipse(null, new Pen(gold, 4), new Point(218, 53), 28, 28);
        var handle = new Pen(gold, 7)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        dc.DrawLine(handle, new Point(238, 74), new Point(266, 102));
        var title = new FormattedText(
            "FIND",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Georgia"),
            18,
            gold,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(title, new Point(202, 92));
    }
}
