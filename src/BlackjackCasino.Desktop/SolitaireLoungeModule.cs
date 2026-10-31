using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Desktop;

internal static class SolitaireLoungeModule
{
    internal static LoungeHostedGame Create(
        SolitaireProgressState progress,
        Action<SolitaireProgressState> persist,
        Func<SolitaireStatistics> statistics,
        Action<SolitaireStartRecord> started,
        Action<SolitaireAbandonRecord> abandoned,
        Func<SolitaireCompletionRecord, string> completed,
        Func<string?> cardSetId)
    {
        SolitaireProgressState state = SolitaireProgressState.Normalize(progress);
        return new LoungeHostedGame(
            new LoungeGame(
                "solitaire",
                "Midnight Solitaire",
                "Six ways to let the cards settle.",
                "Klondike, FreeCell, Spider, Pyramid, Golf and TriPeaks. Every deal autosaves; FreeCell deals are numbered and reproducible.",
                () => { },
                Artwork: CreateArtwork,
                SortOrder: 20,
                Status: () =>
                {
                    SolitaireStatistics stats = statistics();
                    int active = state.ActiveGames.Values.Count(g => !g.Won);
                    return $"{stats.Wins:N0} wins · {active} active · {SolitaireAchievementCatalog.Completed(stats)}/{SolitaireAchievementCatalog.All.Count} achievements";
                }),
            back => new SolitaireHubControl(state, persist, statistics, started, abandoned, completed, cardSetId, back));
    }

    private static FrameworkElement CreateArtwork()
    {
        var canvas = new Canvas { Width = 300, Height = 130 };
        Card[] cards =
        [
            new(Rank.Ace, Suit.Spades),
            new(Rank.Queen, Suit.Hearts),
            new(Rank.King, Suit.Clubs),
        ];
        double[] left = [78, 118, 158];
        double[] angle = [-10, 0, 10];
        for (int i = 0; i < cards.Length; i++)
        {
            Border card = PlayingCardPresentation.Create(cards[i], false, compact: false, null);
            card.RenderTransformOrigin = new Point(.5, .8);
            card.RenderTransform = new RotateTransform(angle[i]);
            Canvas.SetLeft(card, left[i]);
            Canvas.SetTop(card, 10);
            canvas.Children.Add(card);
        }
        return canvas;
    }
}

internal sealed class SolitaireHubControl : UserControl, IDisposable
{
    private readonly SolitaireProgressState progress;
    private readonly Action<SolitaireProgressState> persist;
    private readonly Func<SolitaireStatistics> statistics;
    private readonly Action<SolitaireStartRecord> started;
    private readonly Action<SolitaireAbandonRecord> abandoned;
    private readonly Func<SolitaireCompletionRecord, string> completed;
    private readonly Func<string?> cardSetId;
    private readonly Action backToGames;
    private SolitaireView? activeView;
    private TextBlock? status;
    private bool disposed;

    internal SolitaireHubControl(
        SolitaireProgressState progress,
        Action<SolitaireProgressState> persist,
        Func<SolitaireStatistics> statistics,
        Action<SolitaireStartRecord> started,
        Action<SolitaireAbandonRecord> abandoned,
        Func<SolitaireCompletionRecord, string> completed,
        Func<string?> cardSetId,
        Action backToGames)
    {
        this.progress = SolitaireProgressState.Normalize(progress);
        this.persist = persist;
        this.statistics = statistics;
        this.started = started;
        this.abandoned = abandoned;
        this.completed = completed;
        this.cardSetId = cardSetId;
        this.backToGames = backToGames;
        Unloaded += (_, _) => DisposeActiveView();
        ShowHub();
    }

    private void ShowHub()
    {
        DisposeActiveView();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var root = new StackPanel { Margin = new Thickness(42, 28, 42, 34), MaxWidth = 1120, HorizontalAlignment = HorizontalAlignment.Center };

        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(LoungeUi.Eyebrow("MIDNIGHT SOLITAIRE"));
        copy.Children.Add(new TextBlock { Text = "Let the cards settle.", FontFamily = new FontFamily("Georgia"), FontSize = 38, Foreground = LoungeUi.Ink("#F1E6D5"), Margin = new Thickness(0, 5, 0, 7) });
        copy.Children.Add(new TextBlock { Text = "No wagers, no streak requirements and no speed rewards. Time and moves are recorded only as personal statistics.", FontSize = 13, Foreground = LoungeUi.Ink("#9FB5AE"), TextWrapping = TextWrapping.Wrap, MaxWidth = 720 });
        heading.Children.Add(copy);
        Button games = LoungeUi.Button("← LOUNGE GAMES");
        games.VerticalAlignment = VerticalAlignment.Top;
        games.Click += (_, _) => backToGames();
        Grid.SetColumn(games, 1);
        heading.Children.Add(games);
        root.Children.Add(heading);

        SolitaireStatistics stats = statistics();
        var statCard = LoungeUi.Card();
        statCard.Margin = new Thickness(0, 24, 0, 18);
        statCard.Child = new TextBlock
        {
            Text = $"CAREER   {stats.Wins:N0} wins / {stats.GamesStarted:N0} started   ·   {stats.WinRate:N1}% win rate   ·   best streak {stats.BestWinStreak:N0}   ·   achievements {SolitaireAchievementCatalog.Completed(stats)}/{SolitaireAchievementCatalog.All.Count}",
            Foreground = LoungeUi.Ink("#B8CBC4"), FontSize = 12, TextWrapping = TextWrapping.Wrap
        };
        root.Children.Add(statCard);

        status = new TextBlock { Foreground = LoungeUi.Ink("#E7CCA0"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(3, 0, 3, 12) };
        root.Children.Add(status);

        var variants = new WrapPanel();
        variants.Children.Add(VariantCard(SolitaireVariant.Klondike, "Klondike", "The familiar seven-column classic.", "Build alternating colors down; send each suit home from Ace to King."));
        variants.Children.Add(FreeCellCard());
        variants.Children.Add(SpiderCard());
        variants.Children.Add(VariantCard(SolitaireVariant.Pyramid, "Pyramid", "Pairs make thirteen.", "Clear exposed pairs totaling 13; Kings leave on their own."));
        variants.Children.Add(VariantCard(SolitaireVariant.Golf, "Golf", "One rank at a time.", "Clear seven columns by playing one rank above or below the waste."));
        variants.Children.Add(VariantCard(SolitaireVariant.TriPeaks, "TriPeaks", "Three peaks, one quiet run.", "Open the peaks by clearing exposed cards one rank above or below."));
        root.Children.Add(variants);

        root.Children.Add(BuildAchievements(stats));
        scroll.Content = root;
        Content = scroll;
    }

    private Border VariantCard(SolitaireVariant variant, string title, string subtitle, string description)
    {
        var card = LoungeUi.Card();
        card.Width = 330;
        card.Margin = new Thickness(0, 0, 16, 16);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = title, FontFamily = new FontFamily("Georgia"), FontSize = 25, Foreground = LoungeUi.Ink("#F1E6D5") });
        body.Children.Add(new TextBlock { Text = subtitle, FontSize = 12, Foreground = LoungeUi.Ink("#8EC9B0"), Margin = new Thickness(0, 3, 0, 8) });
        body.Children.Add(new TextBlock { Text = description, FontSize = 11, Foreground = LoungeUi.Ink("#9FB5AE"), TextWrapping = TextWrapping.Wrap, MinHeight = 48 });
        AddGameActions(body, variant, 1, null);
        card.Child = body;
        return card;
    }

    private Border FreeCellCard()
    {
        var card = LoungeUi.Card();
        card.Width = 330;
        card.Margin = new Thickness(0, 0, 16, 16);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "FreeCell", FontFamily = new FontFamily("Georgia"), FontSize = 25, Foreground = LoungeUi.Ink("#F1E6D5") });
        body.Children.Add(new TextBlock { Text = "Every deal has a number.", FontSize = 12, Foreground = LoungeUi.Ink("#8EC9B0"), Margin = new Thickness(0, 3, 0, 8) });
        body.Children.Add(new TextBlock { Text = "All cards are visible. Enter a deal number to replay or share the exact same layout.", FontSize = 11, Foreground = LoungeUi.Ink("#9FB5AE"), TextWrapping = TextWrapping.Wrap });
        var deal = new TextBox
        {
            Text = progress.NextDealNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Margin = new Thickness(0, 10, 0, 6), Padding = new Thickness(8, 6, 8, 6),
            Background = LoungeUi.Ink("#0D1D1B"), Foreground = LoungeUi.Ink("#E9E0D1"), BorderBrush = LoungeUi.Ink("#405E57")
        };
        body.Children.Add(deal);
        AddGameActions(body, SolitaireVariant.FreeCell, 1, () => int.TryParse(deal.Text, out int value) && value > 0 ? value : null);
        card.Child = body;
        return card;
    }

    private Border SpiderCard()
    {
        var card = LoungeUi.Card();
        card.Width = 330;
        card.Margin = new Thickness(0, 0, 16, 16);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "Spider", FontFamily = new FontFamily("Georgia"), FontSize = 25, Foreground = LoungeUi.Ink("#F1E6D5") });
        body.Children.Add(new TextBlock { Text = "Eight complete runs.", FontSize = 12, Foreground = LoungeUi.Ink("#8EC9B0"), Margin = new Thickness(0, 3, 0, 8) });
        body.Children.Add(new TextBlock { Text = "Choose one, two or four suits. More suits change difficulty, never the underlying rules.", FontSize = 11, Foreground = LoungeUi.Ink("#9FB5AE"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        foreach (int suits in new[] { 1, 2, 4 })
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            row.Children.Add(new TextBlock { Text = $"{suits} SUIT", Width = 52, Foreground = LoungeUi.Ink("#AFC4BC"), VerticalAlignment = VerticalAlignment.Center, FontSize = 10 });
            AddButtons(row, SolitaireVariant.Spider, suits, null);
            body.Children.Add(row);
        }
        card.Child = body;
        return card;
    }

    private void AddGameActions(StackPanel body, SolitaireVariant variant, int spiderSuits, Func<int?>? requestedDeal)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        AddButtons(row, variant, spiderSuits, requestedDeal);
        body.Children.Add(row);
        if (progress.TryGet(variant, out SolitaireGameState? active, spiderSuits) && active is not null)
        {
            body.Children.Add(new TextBlock
            {
                Text = active.Won ? $"SOLVED · deal #{active.DealNumber:N0} · {active.Moves:N0} moves" : $"IN PROGRESS · deal #{active.DealNumber:N0} · {active.Moves:N0} moves",
                FontSize = 9, Foreground = LoungeUi.Ink(active.Won ? "#E7CCA0" : "#718E86"), Margin = new Thickness(0, 6, 0, 0)
            });
        }
    }

    private void AddButtons(StackPanel row, SolitaireVariant variant, int spiderSuits, Func<int?>? requestedDeal)
    {
        bool has = progress.TryGet(variant, out SolitaireGameState? active, spiderSuits) && active is not null;
        Button resume = LoungeUi.SmallButton(has ? (active!.Won ? "VIEW" : "RESUME") : "RESUME");
        resume.IsEnabled = has;
        resume.Opacity = has ? 1 : .42;
        resume.Margin = new Thickness(0, 0, 6, 0);
        if (has) resume.Click += (_, _) => Open(active!);
        row.Children.Add(resume);

        Button fresh = LoungeUi.SmallButton("NEW DEAL");
        fresh.Click += (_, _) => StartNew(variant, spiderSuits, requestedDeal?.Invoke());
        row.Children.Add(fresh);
    }

    private void StartNew(SolitaireVariant variant, int spiderSuits, int? requestedDeal)
    {
        try
        {
            if (progress.TryGet(variant, out SolitaireGameState? existing, spiderSuits) && existing is not null && !existing.Won)
            {
                abandoned(new SolitaireAbandonRecord(existing.GameId, existing.Variant, existing.SpiderSuits, existing.DealNumber, existing.ElapsedSeconds, existing.Moves));
            }
            int dealNumber = Math.Max(1, requestedDeal ?? progress.NextDealNumber);
            SolitaireGameState game = SolitaireGame.Create(variant, dealNumber, spiderSuits);
            started(new SolitaireStartRecord(game.GameId, game.Variant, game.SpiderSuits, game.DealNumber));
            progress.Save(game);
            persist(progress);
            Open(game);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (status is not null) status.Text = "The new deal could not be saved. Nothing was replaced. " + ex.Message;
        }
    }

    private void Open(SolitaireGameState game)
    {
        DisposeActiveView();
        activeView = new SolitaireView(
            game,
            next => { progress.Save(next); persist(progress); },
            ShowHub,
            completed,
            cardSetId);
        Content = activeView;
        activeView.Focus();
    }

    private Border BuildAchievements(SolitaireStatistics stats)
    {
        var card = LoungeUi.Card();
        card.Margin = new Thickness(0, 4, 0, 0);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "ACHIEVEMENTS", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = LoungeUi.Ink("#8EC9B0") });
        body.Children.Add(new TextBlock { Text = "Milestones only — no daily streaks and no speed requirements.", FontSize = 11, Foreground = LoungeUi.Ink("#718E86"), Margin = new Thickness(0, 4, 0, 10) });
        var wrap = new WrapPanel();
        foreach (SolitaireAchievementDefinition achievement in SolitaireAchievementCatalog.All)
        {
            bool done = achievement.Complete(stats);
            wrap.Children.Add(new Border
            {
                Width = 245, Margin = new Thickness(0, 0, 10, 10), Padding = new Thickness(10), CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), BorderBrush = LoungeUi.Ink(done ? "#628B78" : "#304A47"), Background = LoungeUi.Ink(done ? "#142725" : "#0F1C1D"),
                Child = new TextBlock { Text = $"{(done ? "✓" : "○")}  {achievement.Name}\n{achievement.Description}", FontSize = 10, Foreground = LoungeUi.Ink(done ? "#D9E4D7" : "#718E86"), TextWrapping = TextWrapping.Wrap }
            });
        }
        body.Children.Add(wrap);
        card.Child = body;
        return card;
    }

    private void DisposeActiveView()
    {
        if (activeView is null) return;
        SolitaireView view = activeView;
        activeView = null;
        view.Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DisposeActiveView();
    }
}
