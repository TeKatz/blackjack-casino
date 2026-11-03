using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Presentation-focused Memory lounge module. The core Memory rules and the in-game board stay
/// shared with the foundation module while this shell owns the roomier hub and result screen.
/// </summary>
internal static class MemoryLoungeModuleVisual
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
                : $"Resume · {progress.ActiveGame.Difficulty} · {MemoryCatalog.GetDeck(progress.ActiveGame.DeckId).Name}");

        return new LoungeHostedGame(
            definition,
            backToGames => new MemoryLoungeHubVisual(progress, persist, stats, recordCompletion, sounds, animations),
            UseShellBackNavigation: true);
    }
}

internal sealed class MemoryLoungeHubVisual : UserControl, IDisposable
{
    private readonly MemoryProgressState progress;
    private readonly Action<MemoryProgressState> persist;
    private readonly Func<MemoryStatistics> stats;
    private readonly Func<MemoryCompletionRecord, ProgressionAward> recordCompletion;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private MemoryView? activeView;
    private TextBlock status = null!;
    private bool disposed;

    internal MemoryLoungeHubVisual(
        MemoryProgressState progress,
        Action<MemoryProgressState> persist,
        Func<MemoryStatistics> stats,
        Func<MemoryCompletionRecord, ProgressionAward> recordCompletion,
        Func<bool> sounds,
        Func<bool> animations)
    {
        this.progress = progress;
        this.persist = persist;
        this.stats = stats;
        this.recordCompletion = recordCompletion;
        this.sounds = sounds;
        this.animations = animations;
        Background = Brush("#0B1715");
        Content = BuildHubViewport();
        Unloaded += (_, _) => Dispose();
    }

    private FrameworkElement BuildHubViewport() => BuildHub();

    private FrameworkElement BuildHub()
    {
        MemoryStatistics snapshot = stats();
        var right = new StackPanel();
        right.VerticalAlignment = VerticalAlignment.Center;
        right.Children.Add(PremiumLoungeTheme.Heading("CHOOSE YOUR TABLE"));

        if (progress.ActiveGame is MemoryGameState active)
        {
            var current = PremiumLoungeTheme.Row(
                "CONTINUE YOUR TABLE",
                $"{active.Difficulty} · {active.PairsFound} / {active.PairCount} pairs · {active.Moves} moves",
                selected: true, height: 74);
            var resume = PremiumLoungeTheme.Button("RESUME TABLE", "MemoryResume",
                () => ShowGame(active), width: 150);
            PremiumLoungeTheme.AddRowAction(current, resume, 160);
            right.Children.Add(current);
        }

        foreach (MemoryDifficulty difficulty in Enum.GetValues<MemoryDifficulty>())
        {
            MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
            snapshot.BestMovesByDifficulty.TryGetValue(difficulty.ToString(), out int bestMoves);
            var row = PremiumLoungeTheme.Row(
                difficulty.ToString(),
                $"{spec.Rows}×{spec.Columns} · {spec.Pairs} pairs · " +
                (bestMoves > 0 ? $"BEST {bestMoves} MOVES" : "NO BEST YET"),
                height: 73);
            var description = (StackPanel)PremiumLoungeTheme.RowContent(row).Children[1];
            var xp = PremiumLoungeTheme.Label(
                $"+{MemoryProgression.PlayerExperience(difficulty):N0} XP", 11, PremiumLoungeTheme.Gold, true);
            xp.Margin = new Thickness(0, 3, 0, 0);
            description.Children.Add(xp);

            MemoryDifficulty selected = difficulty;
            var play = PremiumLoungeTheme.Button("PLAY", "MemoryStart_" + selected,
                () => Start(selected), width: 130);
            PremiumLoungeTheme.AddRowAction(row, play, 140);
            right.Children.Add(row);
        }

        var leftSettings = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        var decks = PremiumLoungeTheme.Panel(new StackPanel(), 12);
        decks.Margin = new Thickness(0, 0, 0, 7);
        var deckContents = (StackPanel)decks.Child;
        deckContents.Children.Add(PremiumLoungeTheme.Heading("CARD DECK"));

        var picker = new ComboBox
        {
            Name = "MemoryDeckPicker",
            ItemsSource = MemoryCatalog.Decks,
            DisplayMemberPath = nameof(MemoryDeckDefinition.Name),
            SelectedItem = MemoryCatalog.GetDeck(progress.PreferredDeckId),
            IsEnabled = progress.ActiveGame is null,
            Height = 36,
        };
        PremiumLoungeTheme.StyleInput(picker);
        deckContents.Children.Add(picker);

        var preview = new ContentControl
        {
            Name = "MemoryDeckPreview",
            Margin = new Thickness(0, 6, 0, 0),
        };
        var descriptionText = PremiumLoungeTheme.Label("", 11, PremiumLoungeTheme.Muted);
        descriptionText.Margin = new Thickness(0, 9, 0, 0);

        void UpdatePreview(MemoryDeckDefinition deck)
        {
            preview.Content = BuildDeckPreview(deck);
            descriptionText.Text = deck.Description;
        }

        status = PremiumLoungeTheme.Label(
            "Choose a table size, or resume your current game.",
            13, PremiumLoungeTheme.Gold);
        status.Margin = new Thickness(4, 9, 0, 6);

        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedItem is not MemoryDeckDefinition deck) return;
            UpdatePreview(deck);
            if (progress.ActiveGame is not null) return;
            progress.PreferredDeckId = deck.Id;
            persist(progress);
            status.Text = $"{deck.Name} selected.";
        };
        UpdatePreview(MemoryCatalog.GetDeck(progress.PreferredDeckId));

        deckContents.Children.Add(preview);
        deckContents.Children.Add(descriptionText);
        leftSettings.Children.Add(decks);

        var guide = PremiumLoungeTheme.Label(
            "Flip two cards. Matching pairs stay open; misses turn back over. " +
            "Time is only a statistic, and mistakes never reduce XP.",
            12, PremiumLoungeTheme.Muted);
        var guidePanel = PremiumLoungeTheme.Panel(guide, 14);
        guidePanel.Margin = new Thickness(0, 3, 0, 4);
        leftSettings.Children.Add(guidePanel);
        leftSettings.Children.Add(status);

        return (FrameworkElement)PremiumLoungeTheme.Build(
            "spa-wellness.png",
            "grand-stage.png",
            "A QUIETER CORNER",
            "A moment to remember.",
            "No clock to beat. Turn over the cards and settle into your own rhythm.",
            "Memory",
            "Find the pairs.",
            "Choose your table, select a card deck and discover each matching pair at your own pace.",
            PremiumLoungeTheme.Stats(
                $"GAMES   {snapshot.CompletedGames:N0}    ·    PAIRS FOUND   {snapshot.PairsFound:N0}",
                $"PERFECT   {snapshot.PerfectGames:N0}    ·    BEST STREAK   {snapshot.BestStreak:N0}"),
            right,
            "Choose a table size and card deck. Your current game is saved, and your completed tables remain in your statistics.",
            leftSecondary: leftSettings,
            compactLeft: true,
            rightScrollable: false);
    }

    private static FrameworkElement BuildDeckPreview(MemoryDeckDefinition deck)
    {
        var strip = new UniformGrid { Columns = 4, Height = 89 };
        foreach (MemorySymbol symbol in deck.Symbols.Take(4))
        {
            var holder = new Border
            {
                Background = Brush("#0A1C18"),
                BorderBrush = Brush("#29483D"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(5),
                Margin = new Thickness(4),
                Child = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    Child = new MemoryCardArtwork(deck.Id, symbol),
                },
            };
            strip.Children.Add(holder);
        }

        return new Border
        {
            Background = Brush("#0D201A"),
            BorderBrush = Brush("#27483B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(7),
            Child = strip,
        };
    }

    private FrameworkElement DifficultyCard(MemoryDifficulty difficulty)
    {
        MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
        MemoryStatistics snapshot = stats();
        snapshot.BestMovesByDifficulty.TryGetValue(difficulty.ToString(), out int bestMoves);

        string accent = difficulty switch
        {
            MemoryDifficulty.Easy => "#83B89A",
            MemoryDifficulty.Normal => "#80B9A2",
            MemoryDifficulty.Hard => "#7FA9B6",
            MemoryDifficulty.Expert => "#D4B66E",
            _ => "#E1C06D",
        };

        var border = new Border
        {
            Background = Gradient("#122A23", "#0E211C"),
            BorderBrush = Brush((int)difficulty >= (int)MemoryDifficulty.Expert ? "#6D6040" : "#315247"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(17, 15, 17, 15),
            Margin = new Thickness(0, 0, 12, 12),
            MinHeight = 168,
        };

        var stack = new StackPanel();
        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition());
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var badge = new Border
        {
            Background = Brush("#17382F"),
            BorderBrush = Brush(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Label(difficulty.ToString().ToUpperInvariant(), 9, accent, FontWeights.Bold),
        };
        top.Children.Add(badge);
        TextBlock best = Label(bestMoves > 0 ? $"BEST {bestMoves} MOVES" : "NO BEST YET", 9, "#779087", FontWeights.SemiBold);
        best.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(best, 1);
        top.Children.Add(best);
        stack.Children.Add(top);

        var boardLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 9, 0, 0) };
        boardLine.Children.Add(new TextBlock
        {
            Text = $"{spec.Rows} × {spec.Columns}",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#F0DFC2"),
            VerticalAlignment = VerticalAlignment.Bottom,
        });
        boardLine.Children.Add(MarginLabel($"  {spec.Pairs} PAIRS", 10, "#9CB0A6", new Thickness(0, 0, 0, 3), FontWeights.SemiBold));
        stack.Children.Add(boardLine);

        var meta = new Grid { Margin = new Thickness(0, 2, 0, 11) };
        meta.ColumnDefinitions.Add(new ColumnDefinition());
        meta.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        meta.Children.Add(Label("Clear every pair", 10, "#82988E"));
        TextBlock xp = Label($"+{MemoryProgression.PlayerExperience(difficulty):N0} XP", 10, accent, FontWeights.Bold);
        Grid.SetColumn(xp, 1);
        meta.Children.Add(xp);
        stack.Children.Add(meta);

        Button play = ActionButton("PLAY", "MemoryStart_" + difficulty);
        play.Height = 38;
        play.BorderBrush = Brush(accent);
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
        ProgressionAward award = recordCompletion(completion);

        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => ShowCompletion(game, award)));
        return award;
    }

    private void ShowCompletion(MemoryGameState completedGame, ProgressionAward award)
    {
        if (disposed) return;
        activeView?.Dispose();
        activeView = null;
        Content = BuildCompletionScreen(completedGame, award);
    }

    private FrameworkElement BuildCompletionScreen(MemoryGameState completedGame, ProgressionAward award)
    {
        MemoryDeckDefinition deck = MemoryCatalog.GetDeck(completedGame.DeckId);
        var root = new Grid { Margin = new Thickness(30) };

        var panel = new Border
        {
            Background = Gradient("#112A23", "#0D1F1A"),
            BorderBrush = Brush(completedGame.Perfect ? "#C7A85F" : "#4F745F"),
            BorderThickness = new Thickness(completedGame.Perfect ? 2 : 1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(32, 28, 32, 30),
            MaxWidth = 920,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        root.Children.Add(panel);

        var stack = new StackPanel();
        TextBlock eyebrow = Label(completedGame.Perfect ? "PERFECT TABLE" : "TABLE CLEARED", 10, completedGame.Perfect ? "#E4C36E" : "#8FC2A3", FontWeights.Bold);
        eyebrow.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(eyebrow);
        stack.Children.Add(new TextBlock
        {
            Text = completedGame.Perfect ? "Perfect Memory" : "Board Cleared",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 38,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#F2E3C5"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4),
        });
        TextBlock subtitle = Label($"{completedGame.Difficulty} · {deck.Name} · {completedGame.PairCount} pairs", 12, "#9EB1A8");
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(subtitle);

        var statsGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 24, 0, 18) };
        statsGrid.Children.Add(ResultStat("MOVES", completedGame.Moves.ToString("N0")));
        statsGrid.Children.Add(ResultStat("ACCURACY", completedGame.Accuracy.ToString("P0")));
        statsGrid.Children.Add(ResultStat("MISSES", completedGame.WrongMatches.ToString("N0")));
        statsGrid.Children.Add(ResultStat("BEST STREAK", $"×{completedGame.BestStreak:N0}"));
        statsGrid.Children.Add(ResultStat("TIME", FormatTime(completedGame.ElapsedSeconds)));
        statsGrid.Children.Add(ResultStat("PAIRS", completedGame.PairCount.ToString("N0")));
        stack.Children.Add(statsGrid);

        var reward = new Border
        {
            Background = Brush("#15342B"),
            BorderBrush = Brush("#496B59"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 13, 18, 13),
        };
        var rewardGrid = new Grid();
        rewardGrid.ColumnDefinitions.Add(new ColumnDefinition());
        rewardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var rewardText = new StackPanel();
        rewardText.Children.Add(Label("COMPLETION REWARD", 9, "#7FA590", FontWeights.Bold));
        rewardText.Children.Add(MarginLabel(completedGame.Perfect ? "No misses. Every guess found its pair." : "Table complete. Your result has been recorded.", 11, "#AABCB3", new Thickness(0, 3, 0, 0)));
        rewardGrid.Children.Add(rewardText);
        var xpText = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        xpText.Children.Add(Label($"+{award.PlayerExperience:N0} XP", 17, "#E4C36E", FontWeights.Bold));
        if (award.SeasonExperience > 0)
            xpText.Children.Add(Label($"+{award.SeasonExperience:N0} Season XP", 9, "#8FC2A3", FontWeights.SemiBold));
        Grid.SetColumn(xpText, 1);
        rewardGrid.Children.Add(xpText);
        reward.Child = rewardGrid;
        stack.Children.Add(reward);

        var actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        Button back = ActionButton("BACK TO MEMORY", "MemoryResultBack");
        back.Height = 42;
        back.Click += (_, _) => BackToHub();
        actions.Children.Add(back);
        Button again = ActionButton("PLAY AGAIN", "MemoryResultAgain");
        again.Height = 42;
        again.BorderBrush = Brush("#C0A55E");
        again.Click += (_, _) => Replay(completedGame);
        Grid.SetColumn(again, 2);
        actions.Children.Add(again);
        stack.Children.Add(actions);

        panel.Child = stack;
        return root;
    }

    private void Replay(MemoryGameState completedGame)
    {
        if (progress.ActiveGame is not null) return;
        progress.PreferredDeckId = completedGame.DeckId;
        progress.ActiveGame = MemoryGame.Start(completedGame.Difficulty, completedGame.DeckId);
        persist(progress);
        ShowGame(progress.ActiveGame);
    }

    private void BackToHub()
    {
        activeView?.Dispose();
        activeView = null;
        Content = BuildHubViewport();
    }

    private static Border ResultStat(string caption, string value)
    {
        Border panel = Panel(new Thickness(14, 12, 14, 12));
        panel.Margin = new Thickness(4);
        var stack = new StackPanel();
        TextBlock valueText = Label(value, 21, "#F0DFC2", FontWeights.SemiBold);
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        TextBlock captionText = MarginLabel(caption, 9, "#7FA590", new Thickness(0, 3, 0, 0), FontWeights.SemiBold);
        captionText.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(valueText);
        stack.Children.Add(captionText);
        panel.Child = stack;
        return panel;
    }

    private static Border Stat(string caption, string value)
    {
        var stack = new StackPanel();
        stack.Children.Add(Label(caption, 9, "#7FA590", FontWeights.SemiBold));
        stack.Children.Add(MarginLabel(value, 20, "#F0DFC2", new Thickness(0, 2, 0, 0), FontWeights.SemiBold));
        Border panel = Panel(new Thickness(15, 12, 15, 12));
        panel.Margin = new Thickness(0, 0, 10, 0);
        panel.Child = stack;
        return panel;
    }

    private static Border Panel(Thickness padding) => new()
    {
        Background = Brush("#10251F"),
        BorderBrush = Brush("#315247"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = padding,
    };

    private static TextBlock Label(string text, double size, string color, FontWeight? weight = null) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = Brush(color),
        FontWeight = weight ?? FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock MarginLabel(string text, double size, string color, Thickness margin, FontWeight? weight = null)
    {
        TextBlock label = Label(text, size, color, weight);
        label.Margin = margin;
        return label;
    }

    private static Button ActionButton(string text, string name) => new()
    {
        Content = text,
        Name = name,
        Padding = new Thickness(12, 8, 12, 8),
        Cursor = CasinoCursors.Hand,
        Background = Brush("#17382F"),
        Foreground = Brush("#F0DFC2"),
        BorderBrush = Brush("#5B765E"),
        BorderThickness = new Thickness(1),
        FontWeight = FontWeights.SemiBold,
    };

    private static LinearGradientBrush Gradient(string start, string end)
    {
        var brush = new LinearGradientBrush(Brush(start).Color, Brush(end).Color, 90);
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static string FormatTime(long seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        activeView?.Dispose();
        activeView = null;
    }
}
