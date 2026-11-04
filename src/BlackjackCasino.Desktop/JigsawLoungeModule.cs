using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Jigsaw;

namespace BlackjackCasino.Desktop;

/// <summary>Jigsaw is registered through the same data-driven Chill Lounge catalog as the other games.</summary>
internal static class JigsawLoungeModule
{
    public static LoungeHostedGame Create(
        JigsawProgressState progress,
        Action<JigsawProgressState> persist,
        Func<JigsawStatistics> statistics,
        Func<JigsawCompletionRecord, ProgressionAward> recordCompletion)
    {
        progress ??= new();
        progress.Normalize();
        return new LoungeHostedGame(
            new LoungeGame(
                "jigsaw",
                "Jigsaw Puzzle",
                JigsawText.T("One picture. A thousand little moments.", "Ein Bild. Tausend kleine Momente."),
                JigsawText.T("Choose a scene, then decide how many pieces to put together. No countdown, no pressure.", "Wähle ein Motiv und bestimme die Teileanzahl. Ohne Zeitdruck."),
                () => { },
                Artwork: () => new JigsawTileArtwork(),
                SortOrder: 50,
                Status: () => progress.ActiveGame is null
                    ? progress.CompletedGames.Count.ToString("N0") + JigsawText.T(" puzzles completed", " Puzzle abgeschlossen")
                    : JigsawText.T("Resume ", "Fortsetzen: ") + JigsawText.Motif(progress.ActiveGame.MotifId) +
                      " · " + progress.ActiveGame.LockedCount.ToString("N0") + " / " +
                      progress.ActiveGame.PieceCount.ToString("N0") + JigsawText.T(" pieces", " Teile")),
            back => new JigsawLoungeHub(progress, persist, statistics, recordCompletion, back),
            UseShellBackNavigation: true);
    }
}

internal static class JigsawImages
{
    private static readonly Dictionary<string, BitmapSource> Images = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, BitmapSource> Thumbnails = new(StringComparer.Ordinal);

    public static BitmapSource Get(string id) => Load(id, 1600, Images);
    public static BitmapSource Thumbnail(string id) => Load(id, 320, Thumbnails);

    private static BitmapSource Load(string id, int width, Dictionary<string, BitmapSource> cache)
    {
        JigsawMotif motif = JigsawCatalog.Motif(id);
        lock (cache)
        {
            if (cache.TryGetValue(motif.Id, out BitmapSource? cached)) return cached;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = width;
            image.UriSource = new Uri(
                "pack://application:,,,/BlackjackCasino.Desktop;component/Assets/CollectionArt/" + motif.Asset,
                UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            cache.Add(motif.Id, image);
            return image;
        }
    }
}

internal sealed class JigsawTileArtwork : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth < 1 || ActualHeight < 1) return;
        var area = new Rect((ActualWidth - 242) / 2, (ActualHeight - 108) / 2, 242, 108);
        dc.DrawRoundedRectangle(LoungeUi.Ink("#15342E"), new Pen(LoungeUi.Ink("#BC9C67"), 1.3), area, 9, 9);
        dc.PushClip(new RectangleGeometry(new Rect(area.X + 8, area.Y + 6, area.Width - 16, area.Height - 12), 5, 5));
        dc.DrawImage(JigsawImages.Thumbnail("enchanted-forest"),
            new Rect(area.X + 8, area.Y + 6, area.Width - 16, area.Height - 12));
        for (int c = 1; c < 5; c++)
            dc.DrawLine(new Pen(LoungeUi.Ink("#F5E3C5"), .9),
                new Point(area.X + c * 48, area.Y + 7),
                new Point(area.X + c * 48, area.Bottom - 7));
        for (int r = 1; r < 3; r++)
            dc.DrawLine(new Pen(LoungeUi.Ink("#F5E3C5"), .9),
                new Point(area.X + 8, area.Y + r * 35),
                new Point(area.Right - 8, area.Y + r * 35));
        dc.Pop();
        dc.DrawRoundedRectangle(null, new Pen(LoungeUi.Ink("#D9BC82"), 1.2), area, 9, 9);
    }
}

internal sealed class JigsawLoungeHub : UserControl, IDisposable
{
    private readonly JigsawProgressState progress;
    private readonly Action<JigsawProgressState> persist;
    private readonly Func<JigsawStatistics> statistics;
    private readonly Func<JigsawCompletionRecord, ProgressionAward> recordCompletion;
    private readonly Action backToGames;
    private JigsawPlayView? playView;
    private bool disposed;

    public JigsawLoungeHub(JigsawProgressState progress, Action<JigsawProgressState> persist,
        Func<JigsawStatistics> statistics, Func<JigsawCompletionRecord, ProgressionAward> recordCompletion,
        Action backToGames)
    {
        this.progress = progress;
        this.persist = persist;
        this.statistics = statistics;
        this.recordCompletion = recordCompletion;
        this.backToGames = backToGames;
        Background = LoungeUi.Ink("#071619");
        Content = BuildMenu();
        Unloaded += (_, _) => Dispose();
    }

    private UIElement BuildMenu()
    {
        JigsawStatistics stats = statistics();
        stats.Normalize();
        var left = new StackPanel();
        left.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("A moment of calm", "Ein Moment der Ruhe")));
        left.Children.Add(PremiumLoungeTheme.Label(
            JigsawText.T("Pick the artwork you love. Choose a piece count from 24 to 1,000, then take your time.", "Wähle dein Lieblingsmotiv und zwischen 24 und 1.000 Teilen. Du hast alle Zeit der Welt."),
            15, PremiumLoungeTheme.Cream));
        left.Children.Add(PremiumLoungeTheme.Stats(
            JigsawText.T("COMPLETED   ", "ABGESCHLOSSEN   ") + stats.CompletedGames.ToString("N0"),
            JigsawText.T("PIECES PLACED   ", "TEILE GELEGT   ") + stats.PiecesCompleted.ToString("N0"),
            JigsawText.T("LARGEST PUZZLE   ", "GRÖSSTES PUZZLE   ") + stats.LargestPuzzle.ToString("N0")));

        // Difficulty and options stay below statistics on the left; the right remains an artwork gallery.
        left.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("01  SELECT PIECE COUNT", "01  TEILEANZAHL AUSWÄHLEN")));
        var right = new StackPanel();
        right.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("02  CHOOSE YOUR SCENE", "02  MOTIV AUSWÄHLEN")));
        var motifs = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        foreach (JigsawMotif motif in JigsawCatalog.Motifs)
        {
            bool selected = motif.Id == progress.SelectedMotifId;
            var preview = new StackPanel();
            preview.Children.Add(new Image
            {
                Source = JigsawImages.Thumbnail(motif.Id),
                Height = 85,
                Width = 165,
                Stretch = Stretch.UniformToFill,
            });
            preview.Children.Add(PremiumLoungeTheme.Label(JigsawText.Motif(motif.Id), 13, PremiumLoungeTheme.Cream, true,
                TextAlignment.Center));
            preview.Children.Add(PremiumLoungeTheme.Label(JigsawText.Category(motif.Category), 10, PremiumLoungeTheme.Muted,
                false, TextAlignment.Center));
            var option = new Button
            {
                Name = "JigsawMotif_" + motif.Id.Replace('-', '_'),
                Content = preview,
                Width = 188,
                Margin = new Thickness(0, 0, 9, 9),
                Padding = new Thickness(7),
                BorderThickness = new Thickness(selected ? 2 : 1),
                BorderBrush = LoungeUi.Ink(selected ? "#5CF0D7" : "#60766D"),
                Background = LoungeUi.Ink(selected ? "#154A47" : "#102A2B"),
                Cursor = Cursors.Hand,
                ToolTip = JigsawText.Motif(motif.Id),
            };
            option.Click += (_, _) =>
            {
                progress.SelectedMotifId = motif.Id;
                persist(progress);
                Content = BuildMenu();
            };
            motifs.Children.Add(option);
        }
        right.Children.Add(motifs);

        var sizes = new UniformGrid { Columns = 4, Margin = new Thickness(0, 12, 0, 15) };
        foreach (JigsawSize size in JigsawCatalog.Sizes)
        {
            bool selected = size.Count == progress.SelectedPieceCount;
            var option = PremiumLoungeTheme.Button(
                size.Count.ToString("N0"),
                "JigsawSize_" + size.Count,
                () =>
                {
                    progress.SelectedPieceCount = size.Count;
                    persist(progress);
                    Content = BuildMenu();
                },
                primary: selected, width: 118, height: 38, gold: selected);
            option.Margin = new Thickness(3, 3, 3, 4);
            sizes.Children.Add(option);
        }
        left.Children.Add(sizes);

        var options = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        options.Children.Add(PremiumLoungeTheme.Button(
            progress.TimerEnabled ? JigsawText.T("TIMER: ON", "TIMER: AN") : JigsawText.T("TIMER: OFF", "TIMER: AUS"),
            "JigsawMenuTimer",
            () =>
            {
                progress.TimerEnabled = !progress.TimerEnabled;
                if (progress.ActiveGame is not null)
                {
                    progress.ActiveGame.TimerEnabled = progress.TimerEnabled;
                    if (!progress.TimerEnabled) progress.ActiveGame.ElapsedSeconds = 0;
                }
                persist(progress);
                Content = BuildMenu();
            }, primary: false, width: 175, height: 39));
        options.Children.Add(PremiumLoungeTheme.Button(
            JigsawText.T("ACHIEVEMENTS", "ERFOLGE") + " " + stats.UnlockedAchievementIds.Count + "/30",
            "JigsawAchievementsOpen", () => Content = BuildAchievements(),
            primary: false, width: 275, height: 39, gold: false));
        left.Children.Add(options);

        if (progress.ActiveGame is JigsawGameState active)
        {
            var message = PremiumLoungeTheme.Panel(new StackPanel
            {
                Children =
                {
                    PremiumLoungeTheme.Label(JigsawText.T("UNFINISHED PUZZLE", "UNFERTIGES PUZZLE"), 13, PremiumLoungeTheme.Teal, true),
                    PremiumLoungeTheme.Label(
                        JigsawText.Motif(active.MotifId) + " · " +
                        active.PieceCount.ToString("N0") + JigsawText.T(" pieces · ", " Teile · ") +
                        active.LockedCount.ToString("N0") + JigsawText.T(" placed", " gelegt"),
                        15, PremiumLoungeTheme.Cream),
                },
            });
            message.Margin = new Thickness(0, 0, 0, 10);
            right.Children.Add(message);
            right.Children.Add(PremiumLoungeTheme.Button(JigsawText.T("RESUME PUZZLE", "PUZZLE FORTSETZEN"), "JigsawResume",
                () => Open(active), width: 250, height: 47, gold: true));
            var restart = PremiumLoungeTheme.Button(JigsawText.T("START A NEW PUZZLE", "NEUES PUZZLE STARTEN"), "JigsawReplace",
                () =>
                {
                    if (System.Windows.MessageBox.Show(
                            JigsawText.T("Discard the unfinished puzzle and start a new one?", "Unfertiges Puzzle verwerfen und ein neues starten?"),
                            "Jigsaw Puzzle", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        != MessageBoxResult.Yes) return;
                    StartNew();
                }, primary: false, width: 250, height: 43);
            restart.Margin = new Thickness(12, 8, 0, 0);
            right.Children.Add(restart);
        }
        else
        {
            right.Children.Add(PremiumLoungeTheme.Button(JigsawText.T("START PUZZLE", "PUZZLE STARTEN"), "JigsawStart",
                StartNew, width: 270, height: 49, gold: true));
        }

        return PremiumLoungeTheme.Build(
            "sky-bar.png", "penthouse-club.png",
            "THE MIDNIGHT CLUB · CHILL LOUNGE",
            "Jigsaw Puzzle",
            JigsawText.T("Find your own pace, one piece at a time.", "Finde dein eigenes Tempo, Teil für Teil."),
            JigsawText.T("THE PUZZLE STUDIO", "DAS PUZZLE-ATELIER"),
            JigsawText.T("A world in pieces.", "Eine Welt in Teilen."),
            JigsawText.T("Twelve curated scenes from the club's illustrated collection. Every scene can be played at any size.", "Zwölf ausgewählte Motive aus der illustrierten Sammlung des Clubs. Jedes Motiv ist in allen Größen spielbar."),
            left, right,
            JigsawText.T("No time limits. No penalties for using the reference picture. Progress saves automatically.", "Kein Zeitlimit. Keine Nachteile durch die Bildvorlage. Dein Fortschritt wird automatisch gespeichert."),
            compactLeft: true);
    }

    private void StartNew()
    {
        progress.ActiveGame = JigsawGame.Start(progress.SelectedMotifId, progress.SelectedPieceCount);
        progress.ActiveGame.TimerEnabled = progress.TimerEnabled;
        persist(progress);
        Open(progress.ActiveGame);
    }

    private void Open(JigsawGameState game)
    {
        playView?.Dispose();
        game.TimerEnabled = progress.TimerEnabled;
        playView = new JigsawPlayView(game, Save, Complete, ReturnToHub, enabled =>
        {
            progress.TimerEnabled = enabled;
            persist(progress);
        });
        Content = playView;
    }

    private void Save(JigsawGameState game)
    {
        if (disposed || game.Completed) return;
        progress.ActiveGame = game;
        persist(progress);
    }

    private string Complete(JigsawGameState game)
    {
        if (!game.Completed) return "";
        var completion = new JigsawCompletionRecord(game.GameId, game.MotifId,
            game.PieceCount, game.ElapsedSeconds, game.CompletedAt ?? DateTimeOffset.UtcNow);
        HashSet<string> before = statistics().UnlockedAchievementIds.ToHashSet(StringComparer.Ordinal);
        if (!progress.CompletedGames.Any(c => c.GameId == completion.GameId))
            progress.CompletedGames.Add(completion);
        progress.ActiveGame = null;
        persist(progress);

        ProgressionAward award = ProgressionAward.None;
        string message;
        try
        {
            award = recordCompletion(completion);
            message = award.PlayerExperience > 0
                ? JigsawText.T("Puzzle complete!", "Puzzle abgeschlossen!")
                : JigsawText.T("Puzzle complete! Rewards were already credited.", "Puzzle abgeschlossen! Belohnungen wurden bereits gutgeschrieben.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            message = JigsawText.T("Puzzle saved. Rewards will be retried when you return to the Lounge.",
                "Puzzle gespeichert. Belohnungen werden beim nächsten Besuch der Lounge nachgeholt.");
        }

        JigsawAchievementDefinition[] unlocked = JigsawAchievements.All
            .Where(a => !before.Contains(a.Id) && statistics().UnlockedAchievementIds.Contains(a.Id))
            .ToArray();
        ProgressionAward finalAward = award;
        string finalMessage = message;
        // Transition after the piece-drop event, so the board is not disposed in its own callback.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (disposed) return;
            playView?.Dispose();
            playView = null;
            Content = BuildCompletion(game, finalAward, unlocked, finalMessage);
        }));
        return message;
    }

    private UIElement BuildCompletion(JigsawGameState game, ProgressionAward award,
        IReadOnlyList<JigsawAchievementDefinition> unlocked, string message)
    {
        var left = new StackPanel();
        left.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("THE FINISHED PICTURE", "DAS FERTIGE BILD")));
        left.Children.Add(PremiumLoungeTheme.Panel(new Image
        {
            Source = JigsawImages.Get(game.MotifId),
            Width = 480, Height = 300, Stretch = Stretch.Uniform,
        }, 8));
        left.Children.Add(PremiumLoungeTheme.Label(JigsawText.Motif(game.MotifId), 19,
            PremiumLoungeTheme.Cream, true, TextAlignment.Center));
        left.Children.Add(PremiumLoungeTheme.Label(message, 14, PremiumLoungeTheme.Teal, false,
            TextAlignment.Center));

        var right = new StackPanel();
        right.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("PUZZLE COMPLETED", "PUZZLE ABGESCHLOSSEN"), 22));
        right.Children.Add(PremiumLoungeTheme.Stats(
            JigsawText.T("PIECES   ", "TEILE   ") + game.PieceCount.ToString("N0"),
            JigsawText.T("TIME   ", "ZEIT   ") + (game.TimerEnabled
                ? TimeSpan.FromSeconds(game.ElapsedSeconds).ToString(@"hh\:mm\:ss")
                : JigsawText.T("Not tracked", "Nicht erfasst")),
            JigsawText.T("PLAYER XP   +", "SPIELER-EP   +") + award.PlayerExperience.ToString("N0"),
            JigsawText.T("SEASON XP   +", "SAISON-EP   +") + award.SeasonExperience.ToString("N0"),
            JigsawText.T("COINS   +", "MÜNZEN   +") + award.CoinsEarned.ToString("N0")));
        right.Children.Add(PremiumLoungeTheme.Heading(
            JigsawText.T("ACHIEVEMENTS UNLOCKED", "ERFOLGE FREIGESCHALTET") + " · " + unlocked.Count, 17));
        if (unlocked.Count == 0)
            right.Children.Add(PremiumLoungeTheme.Label(
                JigsawText.T("No new achievements this time. Keep puzzling!", "Diesmal keine neuen Erfolge. Puzzle weiter!"),
                14, PremiumLoungeTheme.Muted));
        else
            foreach (JigsawAchievementDefinition achievement in unlocked)
                right.Children.Add(AchievementRow(achievement, statistics(), true));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
        actions.Children.Add(PremiumLoungeTheme.Button(
            JigsawText.T("PLAY ANOTHER", "WEITERES PUZZLE"), "JigsawResultAgain",
            StartNew, width: 220, height: 48, gold: true));
        var back = PremiumLoungeTheme.Button(JigsawText.T("PUZZLE MENU", "PUZZLE-MENÜ"), "JigsawResultBack",
            ReturnToHub, width: 200, height: 48, primary: false);
        back.Margin = new Thickness(12, 0, 0, 0);
        actions.Children.Add(back);
        right.Children.Add(actions);

        return PremiumLoungeTheme.Build(
            "sky-bar.png", "penthouse-club.png",
            "THE MIDNIGHT CLUB · CHILL LOUNGE", "Jigsaw Puzzle",
            JigsawText.T("Every piece has found its place.", "Jedes Teil hat seinen Platz gefunden."),
            JigsawText.T("YOUR MASTERPIECE", "DEIN MEISTERWERK"),
            JigsawText.T("Picture complete.", "Bild vollendet."),
            JigsawText.T("No rushing. No missed details. Enjoy the result.",
                "Ganz ohne Eile. Genieße dein fertiges Bild."),
            left, right, JigsawText.T("Your completion is saved automatically.", "Dein Abschluss wurde automatisch gespeichert."),
            compactLeft: true);
    }

    private UIElement BuildAchievements()
    {
        JigsawStatistics stats = statistics();
        stats.Normalize();
        var left = new StackPanel();
        left.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("THE ACHIEVEMENT GALLERY", "DIE ERFOLGSGALERIE")));
        left.Children.Add(PremiumLoungeTheme.Label(
            JigsawText.T("Thirty milestones across puzzle sizes, completed pictures, placed pieces and scenes.",
                "Dreißig Meilensteine für Teileanzahl, fertige Bilder, gelegte Teile und Motive."),
            15, PremiumLoungeTheme.Cream));
        left.Children.Add(PremiumLoungeTheme.Stats(
            JigsawText.T("UNLOCKED   ", "FREIGESCHALTET   ") + stats.UnlockedAchievementIds.Count + " / 30",
            JigsawText.T("PIECES PLACED   ", "TEILE GELEGT   ") + stats.PiecesCompleted.ToString("N0")));
        left.Children.Add(PremiumLoungeTheme.Button(JigsawText.T("← PUZZLE MENU", "← PUZZLE-MENÜ"),
            "JigsawAchievementsBack", () => Content = BuildMenu(), width: 250, height: 44));

        var right = new StackPanel();
        foreach (JigsawAchievementDefinition achievement in JigsawAchievements.All)
            right.Children.Add(AchievementRow(achievement, stats,
                stats.UnlockedAchievementIds.Contains(achievement.Id)));
        return PremiumLoungeTheme.Build(
            "sky-bar.png", "penthouse-club.png",
            "THE MIDNIGHT CLUB · CHILL LOUNGE",
            JigsawText.T("Achievements", "Erfolge"),
            JigsawText.T("Thirty achievements. Four reward tiers.", "Dreißig Erfolge. Vier Belohnungsstufen."),
            JigsawText.T("REWARDS", "BELOHNUNGEN"),
            JigsawText.T("Every piece counts.", "Jedes Teil zählt."),
            JigsawText.T("Bronze, Silver, Gold and Platinum grant progressively higher XP and coins.",
                "Bronze, Silber, Gold und Platin gewähren steigende EP- und Münzbelohnungen."),
            left, right, JigsawText.T("Achievements are awarded once per account.", "Erfolge werden pro Profil nur einmal belohnt."),
            compactLeft: true);
    }

    private static FrameworkElement AchievementRow(JigsawAchievementDefinition a, JigsawStatistics stats, bool unlocked)
    {
        var stack = new StackPanel();
        stack.Children.Add(PremiumLoungeTheme.Label(
            (unlocked ? "✓  " : "◇  ") + JigsawText.T(a.Name, a.GermanName) +
            " · " + JigsawText.Tier(a.Tier),
            15, unlocked ? PremiumLoungeTheme.Teal : PremiumLoungeTheme.Cream, true));
        stack.Children.Add(PremiumLoungeTheme.Label(JigsawText.T(a.Description, a.GermanDescription),
            12, PremiumLoungeTheme.Muted));
        stack.Children.Add(PremiumLoungeTheme.Label(
            Math.Min(a.Current(stats), a.Goal).ToString("N0") + " / " + a.Goal.ToString("N0") +
            "   ·   +" + a.ExperienceReward.ToString("N0") + " XP   ·   +" +
            a.CoinReward.ToString("N0") + JigsawText.T(" coins", " Münzen"),
            12, PremiumLoungeTheme.Gold, true));
        var row = PremiumLoungeTheme.Panel(stack, 11);
        row.Margin = new Thickness(0, 0, 0, 8);
        return row;
    }

    private void ReturnToHub()
    {
        playView?.Dispose();
        playView = null;
        Content = BuildMenu();
    }

    public void Dispose()
    {
        if (disposed) return;
        playView?.Dispose();
        playView = null;
        disposed = true;
    }
}
