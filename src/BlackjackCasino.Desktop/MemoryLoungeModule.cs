using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

internal static class MemoryLoungeModule
{
    internal static LoungeHostedGame Create(
        MemoryProgressState progress,
        Action<MemoryProgressState> persist,
        Func<MemoryStatistics> stats,
        Func<MemoryCompletionRecord, ProgressionAward> recordCompletion,
        Func<bool> sounds,
        Func<bool> animations)
    {
        progress ??= new();
        progress.Normalize();
        var definition = new LoungeGame(
            "memory",
            "Memory",
            "Find pairs · Build a streak",
            "Flip two cards at a time and clear the table by finding every pair. No countdown, no score pressure — just your memory and a quiet table.",
            () => { },
            Artwork: () => new MemoryLoungeArtwork(),
            SortOrder: 40,
            Status: () => progress.ActiveGame is null
                ? $"{progress.CompletedGames.Count:N0} games complete"
                : $"Resume · {progress.ActiveGame.Difficulty} · {MemoryTextLocalization.Deck(MemoryCatalog.GetDeck(progress.ActiveGame.DeckId)).Name}");

        return new LoungeHostedGame(
            definition,
            backToGames => new MemoryLoungeHub(progress, persist, stats, recordCompletion, sounds, animations, backToGames));
    }
}

internal sealed class MemoryLoungeHub : UserControl, IDisposable
{
    private readonly MemoryProgressState progress;
    private readonly Action<MemoryProgressState> persist;
    private readonly Func<MemoryStatistics> stats;
    private readonly Func<MemoryCompletionRecord, ProgressionAward> recordCompletion;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private readonly Action backToGames;
    private MemoryView? activeView;
    private TextBlock status = null!;

    internal MemoryLoungeHub(
        MemoryProgressState progress,
        Action<MemoryProgressState> persist,
        Func<MemoryStatistics> stats,
        Func<MemoryCompletionRecord, ProgressionAward> recordCompletion,
        Func<bool> sounds,
        Func<bool> animations,
        Action backToGames)
    {
        this.progress = progress;
        this.persist = persist;
        this.stats = stats;
        this.recordCompletion = recordCompletion;
        this.sounds = sounds;
        this.animations = animations;
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
        title.Children.Add(Label("CHILL LOUNGE · MEMORY", 10, "#82B79D"));
        title.Children.Add(Label("Memory", 30, "#F2E3C5", FontWeights.SemiBold));
        title.Children.Add(Label("Find the pairs. Take all the time you need.", 12, "#AABCB3"));
        header.Children.Add(title);
        Button back = ActionButton("← Games", "MemoryBack");
        back.Click += (_, _) => backToGames();
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        root.Children.Add(header);

        MemoryStatistics snapshot = stats();
        var summary = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 18) };
        summary.Children.Add(Stat("GAMES", snapshot.CompletedGames.ToString("N0")));
        summary.Children.Add(Stat("PAIRS FOUND", snapshot.PairsFound.ToString("N0")));
        summary.Children.Add(Stat("PERFECT", snapshot.PerfectGames.ToString("N0")));
        summary.Children.Add(Stat("BEST STREAK", snapshot.BestStreak.ToString("N0")));
        Grid.SetRow(summary, 1);
        root.Children.Add(summary);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(content, 2);
        root.Children.Add(content);

        var levels = new UniformGrid { Columns = 2 };
        foreach (MemoryDifficulty difficulty in Enum.GetValues<MemoryDifficulty>())
            levels.Children.Add(DifficultyCard(difficulty));
        content.Children.Add(levels);

        var side = new StackPanel();
        if (progress.ActiveGame is not null)
        {
            MemoryGameState active = progress.ActiveGame;
            var resumePanel = Panel();
            var resumeStack = new StackPanel();
            resumeStack.Children.Add(Label("CURRENT TABLE", 10, "#D5B879"));
            resumeStack.Children.Add(Label(MemoryTextLocalization.Deck(MemoryCatalog.GetDeck(active.DeckId)).Name, 19, "#F2E3C5", FontWeights.SemiBold));
            resumeStack.Children.Add(Label($"{active.Difficulty} · {active.PairsFound} / {active.PairCount} pairs · {active.Moves} moves", 11, "#9DB2A8"));
            Button resume = ActionButton("RESUME", "MemoryResume");
            resume.Margin = new Thickness(0, 12, 0, 0);
            resume.Click += (_, _) => ShowGame(active);
            resumeStack.Children.Add(resume);
            resumePanel.Child = resumeStack;
            side.Children.Add(resumePanel);
        }

        var deckPanel = Panel();
        deckPanel.Margin = new Thickness(0, progress.ActiveGame is null ? 0 : 12, 0, 0);
        var deckStack = new StackPanel();
        deckStack.Children.Add(Label("CARD DECK", 10, "#D5B879"));
        MemoryDeckDefinition[] displayDecks = MemoryTextLocalization.Decks();
        var deckPicker = new ComboBox
        {
            Name = "MemoryDeckPicker",
            ItemsSource = displayDecks,
            DisplayMemberPath = nameof(MemoryDeckDefinition.Name),
            SelectedItem = displayDecks.First(deck => deck.Id == progress.PreferredDeckId),
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(8, 6, 8, 6),
            IsEnabled = progress.ActiveGame is null,
        };
        var preview = new ContentControl { Name = "MemoryDeckPreview", Margin = new Thickness(0, 10, 0, 0) };
        var description = MarginLabel("", 10, "#8FA59B", new Thickness(0, 8, 0, 0));

        void RefreshDeckPresentation(MemoryDeckDefinition deck)
        {
            preview.Content = BuildDeckPreview(deck);
            description.Text = deck.Description;
        }

        deckPicker.SelectionChanged += (_, _) =>
        {
            if (deckPicker.SelectedItem is not MemoryDeckDefinition deck) return;
            RefreshDeckPresentation(deck);
            if (progress.ActiveGame is not null) return;
            progress.PreferredDeckId = deck.Id;
            persist(progress);
            status.Text = $"{deck.Name} selected.";
        };
        deckStack.Children.Add(deckPicker);
        deckStack.Children.Add(preview);
        deckStack.Children.Add(description);
        RefreshDeckPresentation(MemoryTextLocalization.Deck(MemoryCatalog.GetDeck(progress.PreferredDeckId)));
        deckPanel.Child = deckStack;
        side.Children.Add(deckPanel);

        var info = Panel();
        info.Margin = new Thickness(0, 12, 0, 0);
        var infoStack = new StackPanel();
        infoStack.Children.Add(Label("HOW IT WORKS", 10, "#D5B879"));
        infoStack.Children.Add(Label("Flip two cards. A matching pair stays open; a miss turns back over. Consecutive matches build your streak.", 11, "#AABCB3"));
        infoStack.Children.Add(MarginLabel("Time is recorded only as a statistic. There is no timer pressure and mistakes never reduce XP.", 10, "#82B79D", new Thickness(0, 10, 0, 0)));
        info.Child = infoStack;
        side.Children.Add(info);

        status = Label("Choose a table size, or resume your current game.", 11, "#C9B98F");
        status.Margin = new Thickness(2, 14, 0, 0);
        side.Children.Add(status);
        Grid.SetColumn(side, 2);
        content.Children.Add(side);
        return root;
    }

    private static FrameworkElement BuildDeckPreview(MemoryDeckDefinition deck)
    {
        var strip = new UniformGrid { Columns = 4, Height = 76 };
        foreach (MemorySymbol symbol in deck.Symbols.Take(4))
        {
            strip.Children.Add(new Viewbox
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2),
                Child = new MemoryCardArtwork(deck.Id, symbol),
            });
        }
        return strip;
    }

    private FrameworkElement DifficultyCard(MemoryDifficulty difficulty)
    {
        MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
        MemoryStatistics snapshot = stats();
        snapshot.BestMovesByDifficulty.TryGetValue(difficulty.ToString(), out int bestMoves);
        var border = Panel();
        border.Margin = new Thickness(0, 0, 10, 10);
        var stack = new StackPanel();
        stack.Children.Add(Label(difficulty.ToString().ToUpperInvariant(), 11, (int)difficulty >= (int)MemoryDifficulty.Expert ? "#E0BE73" : "#86BA9D", FontWeights.Bold));
        stack.Children.Add(Label($"{spec.Rows} × {spec.Columns} · {spec.Pairs} pairs", 17, "#F0DFC2", FontWeights.SemiBold));
        stack.Children.Add(Label($"+{MemoryProgression.PlayerExperience(difficulty):N0} XP on completion", 10, "#91A79C"));
        stack.Children.Add(MarginLabel(bestMoves > 0 ? $"Best: {bestMoves} moves" : "No completed table yet", 10, "#7F948B", new Thickness(0, 7, 0, 10)));
        Button play = ActionButton("PLAY", "MemoryStart_" + difficulty);
        play.Click += (_, _) => Start(difficulty);
        stack.Children.Add(play);
        border.Child = stack;
        return border;
    }

    private void Start(MemoryDifficulty difficulty)
    {
        if (progress.ActiveGame is not null)
        {
            status.Text = "Resume or finish the current table before starting another one.";
            return;
        }
        progress.ActiveGame = MemoryGame.Start(difficulty, progress.PreferredDeckId);
        persist(progress);
        ShowGame(progress.ActiveGame);
    }

    private void ShowGame(MemoryGameState game)
    {
        activeView?.Dispose();
        activeView = new MemoryView(game, SaveGame, CompleteGame, BackToHub, sounds, animations);
        Content = activeView;
    }

    private void SaveGame(MemoryGameState game)
    {
        progress.ActiveGame = game;
        persist(progress);
    }

    private ProgressionAward CompleteGame(MemoryGameState game)
    {
        MemoryCompletionRecord completion = MemoryGame.Complete(game);
        if (!progress.CompletedGames.Any(x => x.GameId == completion.GameId))
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
        Background = Brush("#10251F"),
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

    internal static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    public void Dispose()
    {
        activeView?.Dispose();
        activeView = null;
    }
}

internal sealed class MemoryView : UserControl, IDisposable
{
    private readonly MemoryGameState game;
    private readonly Action<MemoryGameState> save;
    private readonly Func<MemoryGameState, ProgressionAward> complete;
    private readonly Action back;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private readonly DispatcherTimer timer;
    private readonly Dictionary<int, Button> cardButtons = [];
    private readonly TextBlock progress = MemoryLoungeHub.Label("", 11, "#AABCB3");
    private readonly TextBlock status = MemoryLoungeHub.Label("", 13, "#D7C38F", FontWeights.SemiBold);
    private readonly TextBlock streak = MemoryLoungeHub.Label("", 24, "#E4C778", FontWeights.Bold);
    private readonly object audioOwner = new();
    private static readonly IReadOnlyDictionary<MemoryCue, AudioClip> Cues = Enum.GetValues<MemoryCue>().ToDictionary(x => x, MemorySoundSynthesis.Build);
    private bool resolvingMismatch;
    private bool disposed;
    private int unsavedSeconds;

    internal MemoryView(
        MemoryGameState game,
        Action<MemoryGameState> save,
        Func<MemoryGameState, ProgressionAward> complete,
        Action back,
        Func<bool> sounds,
        Func<bool> animations)
    {
        this.game = game;
        this.save = save;
        this.complete = complete;
        this.back = back;
        this.sounds = sounds;
        this.animations = animations;
        Background = MemoryLoungeHub.Brush("#0B1715");
        Content = Build();
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += TimerTick;
        timer.Start();
        RefreshAll();
    }

    private FrameworkElement Build()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        MemoryDeckDefinition deck = MemoryTextLocalization.Deck(MemoryCatalog.GetDeck(game.DeckId));
        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(MemoryLoungeHub.Label($"MEMORY · {game.Difficulty.ToString().ToUpperInvariant()}", 10, "#82B79D"));
        heading.Children.Add(MemoryLoungeHub.Label(deck.Name, 26, "#F0DFC2", FontWeights.SemiBold));
        heading.Children.Add(progress);
        header.Children.Add(heading);
        Button games = MemoryLoungeHub.ActionButton("← Memory", "MemoryGameBack");
        games.Click += (_, _) => { SaveNow(); back(); };
        Grid.SetColumn(games, 1);
        header.Children.Add(games);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.75, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.7, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        FrameworkElement board = BuildBoard();
        var boardFrame = MemoryLoungeHub.Panel();
        boardFrame.Padding = new Thickness(14);
        boardFrame.Child = new Viewbox { Child = board, Stretch = Stretch.Uniform };
        body.Children.Add(boardFrame);

        var side = new StackPanel();
        var streakPanel = MemoryLoungeHub.Panel();
        var streakStack = new StackPanel();
        streakStack.Children.Add(MemoryLoungeHub.Label("MATCH STREAK", 10, "#D5B879"));
        streakStack.Children.Add(streak);
        streakStack.Children.Add(MemoryLoungeHub.Label("Consecutive pairs without a miss.", 10, "#82988E"));
        streakPanel.Child = streakStack;
        side.Children.Add(streakPanel);

        var details = MemoryLoungeHub.Panel();
        details.Margin = new Thickness(0, 12, 0, 0);
        var detailStack = new StackPanel();
        detailStack.Children.Add(MemoryLoungeHub.Label("TABLE NOTES", 10, "#D5B879"));
        detailStack.Children.Add(MemoryLoungeHub.MarginLabel($"Board: {MemoryCatalog.Spec(game.Difficulty).Rows} × {MemoryCatalog.Spec(game.Difficulty).Columns}", 11, "#AABCB3", new Thickness(0, 8, 0, 0)));
        detailStack.Children.Add(MemoryLoungeHub.MarginLabel($"Deck: {deck.Name}", 11, "#AABCB3", new Thickness(0, 4, 0, 0)));
        detailStack.Children.Add(MemoryLoungeHub.MarginLabel("A perfect game means every two-card guess was a match.", 10, "#82B79D", new Thickness(0, 10, 0, 0)));
        details.Child = detailStack;
        side.Children.Add(details);
        Grid.SetColumn(side, 2);
        body.Children.Add(side);

        status.Margin = new Thickness(2, 12, 0, 0);
        Grid.SetRow(status, 2);
        root.Children.Add(status);
        return root;
    }

    private FrameworkElement BuildBoard()
    {
        MemoryDifficultySpec spec = MemoryCatalog.Spec(game.Difficulty);
        var board = new UniformGrid
        {
            Rows = spec.Rows,
            Columns = spec.Columns,
            Width = spec.Columns * 94,
            Height = spec.Rows * 112,
        };
        for (int index = 0; index < game.Cards.Count; index++)
        {
            int cardIndex = index;
            var card = new Button
            {
                Name = $"MemoryCard_{index}",
                Tag = index,
                Margin = new Thickness(5),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = CasinoCursors.Hand,
                RenderTransformOrigin = new Point(.5, .5),
                FocusVisualStyle = null,
            };
            card.Click += async (_, _) => await CardClicked(cardIndex);
            cardButtons[index] = card;
            board.Children.Add(card);
        }
        return board;
    }

    private async Task CardClicked(int index)
    {
        if (resolvingMismatch || game.Completed) return;
        MemoryFlipResult result = MemoryGame.Flip(game, index);
        if (!result.Success) return;

        Play(MemoryCue.Flip);
        await AnimateFlip(index);
        SaveNow();
        RefreshSummary();

        switch (result.Outcome)
        {
            case MemoryFlipOutcome.FirstCard:
                status.Text = "One card open · choose its match.";
                break;
            case MemoryFlipOutcome.Match:
                Play(MemoryCue.Match);
                status.Text = $"PAIR FOUND · ×{result.Streak}";
                RenderCard(result.Index);
                RenderCard(result.OtherIndex);
                Pulse(result.Index);
                Pulse(result.OtherIndex);
                RefreshEnabled();
                break;
            case MemoryFlipOutcome.Mismatch:
                resolvingMismatch = true;
                Play(MemoryCue.Mismatch);
                status.Text = "Not a pair · remember where they were.";
                RefreshEnabled();
                await Task.Delay(760);
                MemoryGame.ResolveMismatch(game);
                SaveNow();
                await Task.WhenAll(AnimateFlip(result.Index), AnimateFlip(result.OtherIndex));
                resolvingMismatch = false;
                RefreshAll();
                break;
            case MemoryFlipOutcome.Completed:
                Play(game.Perfect ? MemoryCue.Perfect : MemoryCue.Complete);
                timer.Stop();
                RenderCard(result.Index);
                RenderCard(result.OtherIndex);
                Pulse(result.Index);
                Pulse(result.OtherIndex);
                ProgressionAward award = complete(game);
                status.Text = game.Perfect
                    ? $"PERFECT MEMORY · {game.PairCount} pairs in {game.Moves} moves · +{award.PlayerExperience:N0} XP"
                    : $"BOARD CLEARED · {game.Moves} moves · {game.WrongMatches} misses · +{award.PlayerExperience:N0} XP";
                status.Foreground = MemoryLoungeHub.Brush(game.Perfect ? "#F0CF77" : "#9DCEB0");
                RefreshAll();
                break;
        }
    }

    private async Task AnimateFlip(int index)
    {
        if (!cardButtons.TryGetValue(index, out Button? card)) return;
        if (!animations())
        {
            RenderCard(index);
            return;
        }

        var scale = new ScaleTransform(1, 1);
        card.RenderTransform = scale;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, .04, TimeSpan.FromMilliseconds(85)));
        await Task.Delay(90);
        RenderCard(index);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.04, 1, TimeSpan.FromMilliseconds(110))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
        await Task.Delay(115);
    }

    private void Pulse(int index)
    {
        if (!animations() || !cardButtons.TryGetValue(index, out Button? card)) return;
        var scale = card.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
        card.RenderTransform = scale;
        var pulse = new DoubleAnimationUsingKeyFrames();
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.09, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130))));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }

    private void RefreshAll()
    {
        foreach (int index in cardButtons.Keys) RenderCard(index);
        RefreshSummary();
        RefreshEnabled();
        if (string.IsNullOrWhiteSpace(status.Text))
            status.Text = "Flip two cards and find every pair.";
    }

    private void RefreshSummary()
    {
        decimal accuracy = game.Moves <= 0 ? 1m : game.Accuracy;
        progress.Text = $"{game.PairsFound} / {game.PairCount} pairs · {game.Moves} moves · {accuracy:P0} accuracy · {FormatTime(game.ElapsedSeconds)}";
        streak.Text = game.CurrentStreak > 0 ? $"×{game.CurrentStreak}" : "—";
        streak.Foreground = MemoryLoungeHub.Brush(game.CurrentStreak >= 5 ? "#F0CF77" : "#E4C778");
    }

    private void RefreshEnabled()
    {
        foreach ((int index, Button card) in cardButtons)
        {
            bool interactive = !resolvingMismatch && !game.Completed && !game.MatchedIndices.Contains(index);
            card.IsEnabled = true;
            card.IsHitTestVisible = interactive;
            card.Focusable = interactive;
            card.Cursor = interactive ? CasinoCursors.Hand : CasinoCursors.Arrow;
        }
    }

    private void RenderCard(int index)
    {
        if (!cardButtons.TryGetValue(index, out Button? card)) return;
        bool matched = game.MatchedIndices.Contains(index);
        bool faceUp = matched || game.FaceUpIndices.Contains(index);
        MemorySymbol? symbol = faceUp ? MemoryTextLocalization.Symbol(MemoryCatalog.Symbol(game.DeckId, game.Cards[index])) : null;
        card.Content = new MemoryCardArtwork(game.DeckId, symbol, matched);
        card.ToolTip = faceUp && symbol is not null ? symbol.Name : "Flip card";
        card.Opacity = 1;
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        if (game.Completed) return;
        game.ElapsedSeconds = checked(game.ElapsedSeconds + 1);
        unsavedSeconds++;
        RefreshSummary();
        if (unsavedSeconds >= 10) SaveNow();
    }

    private void SaveNow()
    {
        unsavedSeconds = 0;
        save(game);
    }

    private void Play(MemoryCue cue)
    {
        if (sounds()) CasinoAudio.Play(Cues[cue], audioOwner, .65);
    }

    private static string FormatTime(long seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        CasinoAudio.Mixer.Stop(audioOwner);
        if (!game.Completed) SaveNow();
    }
}

internal sealed class MemoryLoungeArtwork : FrameworkElement
{
    internal MemoryLoungeArtwork()
    {
        Width = 310;
        Height = 130;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        Brush bg = MemoryLoungeHub.Brush("#0D241D");
        Brush panel = MemoryLoungeHub.Brush("#15372C");
        Brush line = MemoryLoungeHub.Brush("#416D59");
        Brush gold = MemoryLoungeHub.Brush("#D6B96D");
        Brush cream = MemoryLoungeHub.Brush("#E9DFC8");
        dc.DrawRoundedRectangle(bg, new Pen(line, 1), new Rect(0, 0, 310, 130), 10, 10);

        double[,] positions = { { 18, 16 }, { 84, 16 }, { 150, 16 }, { 216, 16 }, { 18, 70 }, { 84, 70 }, { 150, 70 }, { 216, 70 } };
        string[] faces = ["25", "", "7", "", "", "25", "", "7"];
        for (int i = 0; i < 8; i++)
        {
            Rect rect = new(positions[i, 0], positions[i, 1], 50, 43);
            bool open = faces[i].Length > 0;
            dc.DrawRoundedRectangle(open ? panel : MemoryLoungeHub.Brush("#17352E"), new Pen(open ? gold : line, 1.1), rect, 6, 6);
            dc.DrawRoundedRectangle(null, new Pen(open ? MemoryLoungeHub.Brush("#8A7547") : gold, .6), new Rect(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8), 4, 4);
            if (open)
            {
                dc.DrawEllipse(MemoryLoungeHub.Brush("#10251F"), new Pen(gold, .8), new Point(rect.X + 25, rect.Y + 20), 13, 13);
                var text = new FormattedText(faces[i], System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Georgia"), faces[i] == "7" ? 18 : 10, cream, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(text, new Point(rect.X + 25 - text.Width / 2, rect.Y + 20 - text.Height / 2));
            }
            else
            {
                var back = new FormattedText("M", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Georgia"), 13, gold, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(back, new Point(rect.X + 25 - back.Width / 2, rect.Y + 21 - back.Height / 2));
            }
        }
    }
}
