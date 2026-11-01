using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Memory;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Cross-game Lounge presentation glue. This keeps the shared Lounge shell consistent while
/// leaving the individual game/state implementations focused on their own rules.
/// </summary>
internal static class LoungeVisualUpgrade
{
    private static bool installed;
    private static Func<SudokuProgressState>? sudokuProgress;

    internal static void EnsureInstalled(Func<SudokuProgressState> sudokuProgressProvider)
    {
        sudokuProgress = sudokuProgressProvider ?? throw new ArgumentNullException(nameof(sudokuProgressProvider));
        if (installed) return;
        installed = true;

        EventManager.RegisterClassHandler(
            typeof(LoungeGameMenu),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoungeMenuLoaded));
        EventManager.RegisterClassHandler(
            typeof(WordSearchLoungeHub),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWordSearchHubLoaded));
        EventManager.RegisterClassHandler(
            typeof(SolitaireHubControl),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnSolitaireHubLoaded));
    }

    internal static LoungeHostedGame UpgradeSolitaire(LoungeHostedGame module) =>
        module with { UseShellBackNavigation = true };

    internal static LoungeHostedGame UpgradeWordSearch(
        LoungeHostedGame module,
        Func<WordSearchStatistics> statistics,
        Func<WordSearchProgressState> progress)
    {
        return module with
        {
            UseShellBackNavigation = true,
            Definition = module.Definition with
            {
                Artwork = () => new WordSearchLoungeArtworkUpgrade(),
                Status = () =>
                {
                    WordSearchStatistics snapshot = statistics();
                    snapshot.Normalize();
                    int active = progress().ActiveGame is null ? 0 : 1;
                    return $"{snapshot.CompletedPuzzles:N0} puzzles · {snapshot.WordsFound:N0} words · {snapshot.NoHintCompletions:N0} no hint · {active:N0} active";
                },
            },
        };
    }

    internal static LoungeHostedGame UpgradeMemory(
        LoungeHostedGame module,
        Func<MemoryStatistics> statistics)
    {
        return module with
        {
            Definition = module.Definition with
            {
                Status = () =>
                {
                    MemoryStatistics snapshot = statistics();
                    snapshot.Normalize();
                    return $"{snapshot.CompletedGames:N0} games · {snapshot.PairsFound:N0} pairs · {snapshot.PerfectGames:N0} perfect · streak {snapshot.BestStreak:N0}";
                },
            },
        };
    }

    private static void OnLoungeMenuLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not LoungeGameMenu menu) return;
        menu.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => UpdateSudokuTileStatistics(menu)));
    }

    private static void OnWordSearchHubLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not WordSearchLoungeHub hub) return;
        hub.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            Button? localBack = FindDescendant<Button>(hub, button => button.Name == "WordSearchBack");
            if (localBack is not null) localBack.Visibility = Visibility.Collapsed;
            SetHeaderGamesLabel(hub);
            PolishWordSearchDifficultyCards(hub);
        }));
    }

    private static void OnSolitaireHubLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not SolitaireHubControl hub) return;
        hub.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            Button? localBack = FindDescendant<Button>(hub, button =>
                button.Content is string text &&
                (text.Contains("LOUNGE GAMES", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("LOUNGE-SPIELE", StringComparison.OrdinalIgnoreCase)));
            if (localBack is not null) localBack.Visibility = Visibility.Collapsed;
            SetHeaderGamesLabel(hub);
        }));
    }

    private static void SetHeaderGamesLabel(DependencyObject origin)
    {
        Window? window = Window.GetWindow(origin);
        if (window is null) return;
        Button? header = FindDescendant<Button>(window, button => button.Name == "LoungeGamesHeaderBack");
        if (header is not null) header.Content = LoungeGameMenu.GamesBackLabel();
    }

    private static void UpdateSudokuTileStatistics(LoungeGameMenu menu)
    {
        if (sudokuProgress is null) return;
        Button? tile = FindDescendant<Button>(menu, button => button.Name == "LoungeGame_sudoku");
        if (tile?.Content is not StackPanel body) return;

        SudokuProgressState snapshot = SudokuProgressState.Normalize(sudokuProgress());
        int daily = snapshot.CompletedPuzzleIds.Count(id =>
            SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle?.Pool == SudokuPuzzlePool.Daily);
        int active = snapshot.Games.Count(pair =>
            !pair.Value.Completed &&
            SudokuLibrary.TryGet(pair.Key, out SudokuPuzzle? puzzle) &&
            puzzle?.Pool == SudokuPuzzlePool.Normal);

        TextBlock? previous = body.Children.OfType<TextBlock>().FirstOrDefault(text => text.Name == "SudokuTileStats");
        if (previous is not null) body.Children.Remove(previous);
        Border? spacer = body.Children.OfType<Border>().FirstOrDefault(border => Math.Abs(border.Height - 8d) < 0.1);
        if (spacer is not null) body.Children.Remove(spacer);

        var statistics = new TextBlock
        {
            Name = "SudokuTileStats",
            Text = $"{snapshot.CompletedPuzzleIds.Count:N0} SOLVED · {active:N0} ACTIVE · {daily:N0} DAILY",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = LoungeUi.Ink("#718E86"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18),
        };
        body.Children.Insert(Math.Max(0, body.Children.Count - 1), statistics);
    }

    private static void PolishWordSearchDifficultyCards(WordSearchLoungeHub hub)
    {
        foreach (Button play in FindDescendants<Button>(hub)
                     .Where(button => button.Name.StartsWith("WordSearchStart_", StringComparison.Ordinal)))
        {
            if (play.Parent is not StackPanel stack || stack.Parent is not Border card) continue;
            if (!Enum.TryParse(play.Name["WordSearchStart_".Length..], out WordSearchDifficulty difficulty)) continue;

            string accent = difficulty switch
            {
                WordSearchDifficulty.Relaxed => "#86BA9D",
                WordSearchDifficulty.Easy => "#82BCA3",
                WordSearchDifficulty.Medium => "#78AEB0",
                WordSearchDifficulty.Hard => "#7E9FBD",
                WordSearchDifficulty.Expert => "#D7B86A",
                _ => "#E3C16E",
            };
            (int size, int words) = WordSearchCatalog.Spec(difficulty);

            card.MinHeight = 178;
            card.Padding = new Thickness(17, 15, 17, 15);
            card.CornerRadius = new CornerRadius(11);
            card.BorderBrush = WordSearchLoungeHub.Brush(accent);
            card.Background = new LinearGradientBrush(
                ((SolidColorBrush)WordSearchLoungeHub.Brush("#142D26")).Color,
                ((SolidColorBrush)WordSearchLoungeHub.Brush("#0E211C")).Color,
                90);

            TextBlock[] labels = stack.Children.OfType<TextBlock>().ToArray();
            if (labels.Length >= 4)
            {
                TextBlock difficultyLabel = labels[0];
                if (difficultyLabel.Parent == stack)
                {
                    int index = stack.Children.IndexOf(difficultyLabel);
                    stack.Children.Remove(difficultyLabel);
                    difficultyLabel.FontSize = 9;
                    difficultyLabel.Foreground = WordSearchLoungeHub.Brush(accent);
                    var badge = new Border
                    {
                        Name = "WordSearchDifficultyBadge",
                        Background = WordSearchLoungeHub.Brush("#17382F"),
                        BorderBrush = WordSearchLoungeHub.Brush(accent),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 3, 8, 3),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child = difficultyLabel,
                    };
                    stack.Children.Insert(index, badge);
                }

                TextBlock sizeLabel = labels[1];
                sizeLabel.Text = $"{size} × {size}";
                sizeLabel.FontFamily = new FontFamily("Georgia");
                sizeLabel.FontSize = 25;
                sizeLabel.Margin = new Thickness(0, 9, 0, 0);

                TextBlock direction = labels[2];
                direction.Margin = new Thickness(0, 2, 0, 0);
                direction.Foreground = WordSearchLoungeHub.Brush("#91A79C");

                TextBlock completion = labels[3];
                completion.Margin = new Thickness(0, 8, 0, 0);
            }

            if (!stack.Children.OfType<Grid>().Any(grid => grid.Name == "WordSearchDifficultyMeta"))
            {
                var meta = new Grid { Name = "WordSearchDifficultyMeta", Margin = new Thickness(0, 7, 0, 2) };
                meta.ColumnDefinitions.Add(new ColumnDefinition());
                meta.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                meta.Children.Add(new TextBlock
                {
                    Text = $"{words} WORDS",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = WordSearchLoungeHub.Brush("#9CB0A6"),
                });
                var xp = new TextBlock
                {
                    Text = $"+{WordSearchProgression.PlayerExperience(difficulty):N0} XP",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = WordSearchLoungeHub.Brush(accent),
                };
                Grid.SetColumn(xp, 1);
                meta.Children.Add(xp);

                int playIndex = stack.Children.IndexOf(play);
                int progressIndex = stack.Children.OfType<ProgressBar>().Select(stack.Children.IndexOf).DefaultIfEmpty(playIndex).First();
                stack.Children.Insert(Math.Max(0, progressIndex), meta);
            }

            ProgressBar? progressBar = stack.Children.OfType<ProgressBar>().FirstOrDefault();
            if (progressBar is not null)
            {
                progressBar.Height = 6;
                progressBar.Margin = new Thickness(0, 6, 0, 11);
                progressBar.Foreground = WordSearchLoungeHub.Brush(accent);
            }

            play.Height = 38;
            play.FontWeight = FontWeights.SemiBold;
            play.BorderBrush = WordSearchLoungeHub.Brush(accent);
        }
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        return FindDescendants<T>(root).FirstOrDefault(predicate);
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in FindDescendants<T>(child)) yield return nested;
        }
    }
}

/// <summary>A real word-search grid for the Lounge tile instead of pseudo-word rows.</summary>
internal sealed class WordSearchLoungeArtworkUpgrade : FrameworkElement
{
    private static readonly string[] GridRows =
    [
        "MIDNIGHT",
        "CASINOQL",
        "LUCKYXRO",
        "USTARBEU",
        "BMOONPAN",
        "VELVETSG",
        "DEALERHE",
        "QWERTYUI",
    ];

    internal WordSearchLoungeArtworkUpgrade()
    {
        Width = 310;
        Height = 130;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        Brush bg = Ink("#0D241D");
        Brush panel = Ink("#132F27");
        Brush line = Ink("#416D59");
        Brush gold = Ink("#D6B96D");
        Brush text = Ink("#E9DFC8");
        Brush found = new SolidColorBrush(Color.FromArgb(78, 214, 185, 109));

        dc.DrawRoundedRectangle(bg, new Pen(line, 1), new Rect(0, 0, 310, 130), 10, 10);
        dc.DrawRoundedRectangle(panel, new Pen(line, 1), new Rect(16, 10, 126, 110), 7, 7);

        const double cell = 12.5;
        const double x = 29;
        const double y = 15;
        for (int row = 0; row < 8; row++)
        for (int column = 0; column < 8; column++)
        {
            Rect rect = new(x + column * cell, y + row * cell, cell, cell);
            bool moon = row == 4 && column is >= 1 and <= 4;
            if (moon) dc.DrawRoundedRectangle(found, null, new Rect(rect.X + 1, rect.Y + 1, cell - 2, cell - 2), 2, 2);
            dc.DrawRectangle(null, new Pen(Ink("#315448", 0.75), .55), rect);
            DrawCentered(dc, GridRows[row][column].ToString(), 8.5, text,
                new Point(rect.X + cell / 2, rect.Y + cell / 2));
        }
        dc.DrawRoundedRectangle(null, new Pen(gold, 1.6), new Rect(x + cell, y + 4 * cell + 1, cell * 4, cell - 2), 4, 4);

        dc.DrawEllipse(null, new Pen(gold, 4), new Point(218, 48), 25, 25);
        var handle = new Pen(gold, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handle, new Point(236, 66), new Point(261, 91));

        DrawCentered(dc, "FIND", 10, Ink("#83B99F"), new Point(218, 88), FontWeights.SemiBold);
        DrawCentered(dc, "MOON", 18, gold, new Point(218, 105), FontWeights.Bold, "Georgia");
    }

    private void DrawCentered(
        DrawingContext dc,
        string value,
        double size,
        Brush brush,
        Point center,
        FontWeight? weight = null,
        string family = "Segoe UI Semibold")
    {
        var text = new FormattedText(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(family), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private static SolidColorBrush Ink(string hex, double opacity = 1d)
    {
        Color color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(Math.Clamp(opacity, 0d, 1d) * 255d),
            color.R,
            color.G,
            color.B));
        brush.Freeze();
        return brush;
    }
}
