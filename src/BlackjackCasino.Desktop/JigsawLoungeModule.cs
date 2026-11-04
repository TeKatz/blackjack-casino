using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public static BitmapSource Get(string id)
    {
        JigsawMotif motif = JigsawCatalog.Motif(id);
        lock (Images)
        {
            if (Images.TryGetValue(motif.Id, out BitmapSource? cached)) return cached;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 1600;
            image.UriSource = new Uri(
                "pack://application:,,,/BlackjackCasino.Desktop;component/Assets/CollectionArt/" + motif.Asset,
                UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            Images.Add(motif.Id, image);
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
        dc.DrawImage(JigsawImages.Get("enchanted-forest"),
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

        var right = new StackPanel();
        right.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("01  CHOOSE YOUR SCENE", "01  MOTIV AUSWÄHLEN")));
        var motifs = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        foreach (JigsawMotif motif in JigsawCatalog.Motifs)
        {
            bool selected = motif.Id == progress.SelectedMotifId;
            var preview = new StackPanel();
            preview.Children.Add(new Image
            {
                Source = JigsawImages.Get(motif.Id),
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

        right.Children.Add(PremiumLoungeTheme.Heading(JigsawText.T("02  SELECT PIECE COUNT", "02  TEILEANZAHL AUSWÄHLEN")));
        var sizes = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 17) };
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
                primary: selected, width: 175, height: 40, gold: selected);
            option.Margin = new Thickness(3, 3, 3, 4);
            sizes.Children.Add(option);
        }
        right.Children.Add(sizes);

        if (progress.ActiveGame is JigsawGameState active)
        {
            var message = PremiumLoungeTheme.Panel(new StackPanel
            {
                Children =
                {
                    PremiumLoungeTheme.Label("UNFINISHED PUZZLE", 13, PremiumLoungeTheme.Teal, true),
                    PremiumLoungeTheme.Label(
                        JigsawCatalog.Motif(active.MotifId).Name + " · " +
                        active.PieceCount.ToString("N0") + " pieces · " +
                        active.LockedCount.ToString("N0") + " placed",
                        15, PremiumLoungeTheme.Cream),
                },
            });
            message.Margin = new Thickness(0, 0, 0, 10);
            right.Children.Add(message);
            right.Children.Add(PremiumLoungeTheme.Button("RESUME PUZZLE", "JigsawResume",
                () => Open(active), width: 250, height: 47, gold: true));
            var restart = PremiumLoungeTheme.Button("START A NEW PUZZLE", "JigsawReplace",
                () =>
                {
                    if (System.Windows.MessageBox.Show(
                            "Discard the unfinished puzzle and start a new one?",
                            "Jigsaw Puzzle", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        != MessageBoxResult.Yes) return;
                    StartNew();
                }, primary: false, width: 250, height: 43);
            restart.Margin = new Thickness(12, 8, 0, 0);
            right.Children.Add(restart);
        }
        else
        {
            right.Children.Add(PremiumLoungeTheme.Button("START PUZZLE", "JigsawStart",
                StartNew, width: 270, height: 49, gold: true));
        }

        return PremiumLoungeTheme.Build(
            "sky-bar.png", "penthouse-club.png",
            "THE MIDNIGHT CLUB · CHILL LOUNGE",
            "Jigsaw Puzzle",
            "Find your own pace, one piece at a time.",
            "THE PUZZLE STUDIO",
            "A world in pieces.",
            "Twelve curated scenes from the club's illustrated collection. Every scene can be played at any size.",
            left, right,
            "No time limits. No penalties for hints or reference pictures. Progress saves automatically.",
            compactLeft: true);
    }

    private void StartNew()
    {
        progress.ActiveGame = JigsawGame.Start(progress.SelectedMotifId, progress.SelectedPieceCount);
        persist(progress);
        Open(progress.ActiveGame);
    }

    private void Open(JigsawGameState game)
    {
        playView?.Dispose();
        playView = new JigsawPlayView(game, Save, Complete, ReturnToHub);
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
        if (!progress.CompletedGames.Any(c => c.GameId == completion.GameId))
            progress.CompletedGames.Add(completion);
        progress.ActiveGame = null;
        persist(progress);
        try
        {
            ProgressionAward award = recordCompletion(completion);
            return award.PlayerExperience > 0
                ? "Completed! +" + award.PlayerExperience.ToString("N0") +
                  " XP · +" + award.SeasonExperience.ToString("N0") + " Season XP"
                : "Completed! Your reward was already recorded.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Puzzle saved. Reopen the Chill Lounge to retry your reward.";
        }
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
