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
        SolitaireStatistics current = statistics();
        var right = new StackPanel();
        right.Children.Add(PremiumLoungeTheme.Heading("CHOOSE A SOLITAIRE GAME"));

        status = PremiumLoungeTheme.Label("", 13, PremiumLoungeTheme.Gold);
        status.Margin = new Thickness(4, 8, 4, 10);

        AddPremiumVariant(SolitaireVariant.Klondike, "KLONDIKE",
            "The familiar seven-column classic.", 1, null);
        AddPremiumVariant(SolitaireVariant.FreeCell, "FREECELL",
            "All cards visible · numbered, reproducible deals.", 1, null);
        // Retain the exact numbered FreeCell-deal entry from the original hub.
        var dealPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(37, 0, 0, 12) };
        dealPanel.Children.Add(PremiumLoungeTheme.Label("FREECELL DEAL #", 12, PremiumLoungeTheme.Muted, true));
        var freeCellDeal = new TextBox
        {
            Name = "SolitaireFreeCellDeal",
            Text = progress.NextDealNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Width = 128, Height = 33,
            Margin = new Thickness(17, 0, 0, 0),
        };
        PremiumLoungeTheme.StyleInput(freeCellDeal);
        dealPanel.Children.Add(freeCellDeal);
        var numbered = PremiumLoungeTheme.Button("START #", "SolitaireFreeCellNumbered",
            () => StartNew(SolitaireVariant.FreeCell, 1,
                int.TryParse(freeCellDeal.Text, out int number) && number > 0 ? number : null),
            width: 110, height: 33, gold: true);
        numbered.Margin = new Thickness(12, 0, 0, 0);
        dealPanel.Children.Add(numbered);
        right.Children.Add(dealPanel);

        foreach (int suits in new[] { 1, 2, 4 })
            AddPremiumVariant(SolitaireVariant.Spider, $"SPIDER · {suits} SUIT{(suits == 1 ? "" : "S")}",
                "Eight completed runs · choose your suit count.", suits, null);
        AddPremiumVariant(SolitaireVariant.Pyramid, "PYRAMID",
            "Clear exposed pairs totaling thirteen.", 1, null);
        AddPremiumVariant(SolitaireVariant.Golf, "GOLF",
            "Clear columns one rank at a time.", 1, null);
        AddPremiumVariant(SolitaireVariant.TriPeaks, "TRIPEAKS",
            "Three peaks. One quiet run.", 1, null);

        right.Children.Add(status);
        var label = PremiumLoungeTheme.Heading("ACHIEVEMENTS");
        label.Margin = new Thickness(0, 9, 0, 7);
        right.Children.Add(label);
        var achievements = BuildAchievements(current);
        achievements.Margin = new Thickness(0, 9, 0, 5);
        achievements.Padding = new Thickness(10);
        achievements.Visibility = Visibility.Collapsed;
        var toggleAchievements = PremiumLoungeTheme.Button(
            $"VIEW ALL · {SolitaireAchievementCatalog.Completed(current)}/{SolitaireAchievementCatalog.All.Count}",
            "SolitaireAchievementsToggle",
            () =>
            {
                bool open = achievements.Visibility != Visibility.Visible;
                achievements.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            },
            width: 710, height: 43, gold: true);
        toggleAchievements.HorizontalAlignment = HorizontalAlignment.Left;
        right.Children.Add(toggleAchievements);
        right.Children.Add(achievements);

        Content = PremiumLoungeTheme.Build(
            "high-limit-slots.png",
            "poker-room.png",
            "A QUIETER CORNER",
            "Let the cards settle.",
            "No wagers, no streak requirements and no speed rewards. Just the deck and your decisions.",
            "MIDNIGHT SOLITAIRE",
            "Six ways to\nplay the night.",
            "Klondike, FreeCell, Spider, Pyramid, Golf and TriPeaks. Every deal is saved automatically; replay favorite numbered FreeCell deals whenever you like.",
            PremiumLoungeTheme.Stats(
                $"CAREER   {current.Wins:N0} WINS / {current.GamesStarted:N0} STARTED",
                $"WIN RATE   {current.WinRate:N1}%    ·    BEST STREAK   {current.BestWinStreak:N0}",
                $"ACHIEVEMENTS   {SolitaireAchievementCatalog.Completed(current)}/{SolitaireAchievementCatalog.All.Count}"),
            right,
            "All six solitaire variants keep their original rules, recorded deals, achievements and persistent progress.");

        void AddPremiumVariant(SolitaireVariant variant, string title, string detail,
            int suits, Func<int?>? dealNumber)
        {
            bool has = progress.TryGet(variant, out SolitaireGameState? active, suits) && active is not null;
            var row = PremiumLoungeTheme.Row(title,
                has
                    ? $"{(active!.Won ? "SOLVED" : "IN PROGRESS")} · #{active.DealNumber:N0} · {active.Moves:N0} MOVES"
                    : detail,
                selected: has && !active!.Won, height: 73);

            SolitaireGameState? resumable = active;
            var resume = PremiumLoungeTheme.Button(
                has && active!.Won ? "VIEW" : "RESUME",
                $"SolitaireResume_{variant}_{suits}",
                () => { if (resumable is not null) Open(resumable); },
                primary: false, enabled: has, width: 98);
            PremiumLoungeTheme.AddRowAction(row, resume, 105);

            var fresh = PremiumLoungeTheme.Button("NEW DEAL",
                $"SolitaireNew_{variant}_{suits}",
                () => StartNew(variant, suits, dealNumber?.Invoke()),
                width: 128);
            PremiumLoungeTheme.AddRowAction(row, fresh, 137);
            right.Children.Add(row);
        }
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
