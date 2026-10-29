using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class NeonVaultWindow
{
    private static readonly NeonVaultSymbol[] EnhancedPayingSymbols =
    [
        NeonVaultSymbol.Pulse,
        NeonVaultSymbol.Prism,
        NeonVaultSymbol.Chip,
        NeonVaultSymbol.Crown,
        NeonVaultSymbol.Key,
    ];

    private readonly DispatcherTimer enhancedFeedbackTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(33),
    };

    private readonly Image?[,] enhancedArtwork = new Image?[NeonVaultRules.Rows, NeonVaultRules.Reels];
    private readonly NeonVaultSymbol?[,] enhancedArtworkSymbols = new NeonVaultSymbol?[NeonVaultRules.Rows, NeonVaultRules.Reels];
    private readonly bool[] enhancedObservedSettled = new bool[NeonVaultRules.Reels];
    private Canvas? enhancedPathCanvas;
    private bool enhancedPresentationInitialized;
    private bool enhancedWasSpinning;
    private bool enhancedNearMissShown;
    private bool enhancedLowQualityScaling;

    private sealed record EnhancedWinGroup(
        NeonVaultSymbol Symbol,
        int ReelCount,
        IReadOnlyList<int> Cells,
        IReadOnlyList<int> PathCells);

    static NeonVaultWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(NeonVaultWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(EnhancedPresentation_Loaded));
    }

    private static void EnhancedPresentation_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NeonVaultWindow window)
        {
            window.InitializeEnhancedPresentation();
        }
    }

    private void InitializeEnhancedPresentation()
    {
        if (enhancedPresentationInitialized || ReelFrame.Child is not Grid reelRoot)
        {
            return;
        }

        enhancedPresentationInitialized = true;
        enhancedPathCanvas = new Canvas
        {
            Margin = new Thickness(13),
            IsHitTestVisible = false,
            ClipToBounds = true,
            Opacity = 0.92,
        };
        Panel.SetZIndex(enhancedPathCanvas, 8);
        reelRoot.Children.Add(enhancedPathCanvas);

        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                SymbolPresenter presenter = presenters[row, reel];
                if (presenter.Card.Child is not Grid symbolGrid)
                {
                    continue;
                }

                var image = new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(2, 2, 2, 1),
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true,
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                Grid.SetRow(image, 0);
                Panel.SetZIndex(image, 3);
                symbolGrid.Children.Add(image);
                enhancedArtwork[row, reel] = image;

                // Keep the original lightweight TextBlock alive because ApplySymbol updates it,
                // but replace its visible output with the generated artwork above.
                presenter.Icon.Visibility = Visibility.Collapsed;
                presenter.Label.FontSize = 7.5;
                presenter.Label.FontWeight = FontWeights.Bold;
                presenter.Label.Foreground = Ink("#E8EBF8");
            }
        }

        enhancedWasSpinning = spinning;
        Array.Copy(reelSettled, enhancedObservedSettled, reelSettled.Length);
        SyncGeneratedArtwork();
        ResetEnhancedFeedback(clearPaths: true);

        enhancedFeedbackTimer.Tick += EnhancedFeedbackTimer_Tick;
        enhancedFeedbackTimer.Start();
        Closed += (_, _) => enhancedFeedbackTimer.Stop();
    }

    private void EnhancedFeedbackTimer_Tick(object? sender, EventArgs e)
    {
        if (closed)
        {
            enhancedFeedbackTimer.Stop();
            return;
        }

        SyncGeneratedArtwork();

        if (spinning != enhancedWasSpinning)
        {
            enhancedWasSpinning = spinning;
            SetArtworkScalingMode(spinning);
            if (spinning)
            {
                enhancedNearMissShown = false;
                ResetEnhancedFeedback(clearPaths: true);
                Array.Copy(reelSettled, enhancedObservedSettled, reelSettled.Length);
            }
            else
            {
                ShowResolvedWinFeedback();
            }
        }

        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            bool settled = reelSettled[reel];
            if (settled == enhancedObservedSettled[reel])
            {
                continue;
            }

            enhancedObservedSettled[reel] = settled;
            if (!settled)
            {
                continue;
            }

            CancelGenericReelFlash(reel);
            UpdateCandidateFeedback(reel);
        }

        if (spinning &&
            !enhancedNearMissShown &&
            activeNearMiss != NeonVaultNearMiss.None &&
            reelSettled[NeonVaultRules.Reels - 2] &&
            !reelSettled[NeonVaultRules.Reels - 1])
        {
            enhancedNearMissShown = true;
            ShowNearMissFeedback(activeNearMiss);
        }
    }

    private void SyncGeneratedArtwork()
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                SymbolPresenter presenter = presenters[row, reel];
                NeonVaultSymbol symbol = presenter.Symbol ?? NeonVaultSymbol.Pulse;
                Image? image = enhancedArtwork[row, reel];
                if (image is not null && enhancedArtworkSymbols[row, reel] != symbol)
                {
                    image.Source = NeonVaultSymbolAssets.Get(symbol);
                    enhancedArtworkSymbols[row, reel] = symbol;
                }

                string label = EnhancedLabel(symbol);
                if (!string.Equals(presenter.Label.Text, label, StringComparison.Ordinal))
                {
                    presenter.Label.Text = label;
                }
            }
        }
    }

    private void SetArtworkScalingMode(bool lowQuality)
    {
        if (enhancedLowQualityScaling == lowQuality)
        {
            return;
        }

        enhancedLowQualityScaling = lowQuality;
        BitmapScalingMode mode = lowQuality ? BitmapScalingMode.LowQuality : BitmapScalingMode.HighQuality;
        foreach (Image? image in enhancedArtwork)
        {
            if (image is not null)
            {
                RenderOptions.SetBitmapScalingMode(image, mode);
            }
        }
    }

    private static string EnhancedLabel(NeonVaultSymbol symbol) => symbol switch
    {
        NeonVaultSymbol.Pulse => "PULSE CORE",
        NeonVaultSymbol.Prism => "QUEEN",
        NeonVaultSymbol.Chip => "NEON CHIP",
        NeonVaultSymbol.Crown => "KING",
        NeonVaultSymbol.Key => "SECURITY EYE",
        NeonVaultSymbol.Wild => "WILD",
        NeonVaultSymbol.Scatter => "VAULT BREAK",
        NeonVaultSymbol.VaultKey => "ACCESS KEY",
        _ => symbol.ToString().ToUpperInvariant(),
    };

    private void UpdateCandidateFeedback(int settledReel)
    {
        int settledThrough = HighestConsecutiveSettledReel();
        if (settledThrough < 1)
        {
            ResetSettledCellFeedback(settledThrough);
            ClearEnhancedPaths();
            return;
        }

        NeonVaultSymbol[] grid = CapturePresentedGrid();
        var candidateCells = new HashSet<int>();
        var groups = new List<EnhancedWinGroup>();

        foreach (NeonVaultSymbol symbol in EnhancedPayingSymbols)
        {
            EnhancedWinGroup? group = BuildConsecutiveGroup(grid, symbol, settledThrough + 1, requireAllReels: true);
            if (group is null)
            {
                continue;
            }

            groups.Add(group);
            foreach (int index in group.Cells)
            {
                candidateCells.Add(index);
            }
        }

        AddSpecialCandidates(grid, NeonVaultSymbol.Scatter, settledThrough, candidateCells);
        AddSpecialCandidates(grid, NeonVaultSymbol.VaultKey, settledThrough, candidateCells);

        if (candidateCells.Count == 0)
        {
            ResetSettledCellFeedback(settledThrough);
            ClearEnhancedPaths();
            return;
        }

        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                int index = NeonVaultRules.Index(row, reel);
                if (reel > settledThrough)
                {
                    ResetCellFeedback(row, reel);
                    continue;
                }

                if (candidateCells.Contains(index))
                {
                    ApplyCandidateCell(row, reel, grid[index]);
                }
                else
                {
                    ApplyDimmedCell(row, reel, 0.26);
                }
            }
        }

        RenderEnhancedPaths(groups, winning: false);
    }

    private void AddSpecialCandidates(
        IReadOnlyList<NeonVaultSymbol> grid,
        NeonVaultSymbol special,
        int settledThrough,
        ISet<int> candidateCells)
    {
        var matches = new List<int>();
        for (int reel = 0; reel <= settledThrough; reel++)
        {
            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                int index = NeonVaultRules.Index(row, reel);
                if (grid[index] == special)
                {
                    matches.Add(index);
                }
            }
        }

        if (matches.Count < 2)
        {
            return;
        }

        foreach (int index in matches)
        {
            candidateCells.Add(index);
        }
    }

    private void ShowNearMissFeedback(NeonVaultNearMiss nearMiss)
    {
        Brush accent = nearMiss switch
        {
            NeonVaultNearMiss.Scatter => Ink("#FFD66E"),
            NeonVaultNearMiss.Jackpot => Ink("#D49BFF"),
            NeonVaultNearMiss.Blackout => Ink("#FF5B79"),
            _ => Ink("#65EFFF"),
        };

        int finalReel = NeonVaultRules.Reels - 1;
        Border frame = reelFrames[finalReel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = 1;
        frame.BorderBrush = accent;
        frame.BorderThickness = new Thickness(2.5);
        frame.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = new ScaleTransform(1, 1);
        frame.RenderTransform = scale;

        if (AnimationsEnabled)
        {
            var pulse = new DoubleAnimation(1, 1.018, TimeSpan.FromMilliseconds(Pace(260)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        NeonVaultSymbol? focus = nearMiss switch
        {
            NeonVaultNearMiss.Scatter => NeonVaultSymbol.Scatter,
            NeonVaultNearMiss.Jackpot => NeonVaultSymbol.VaultKey,
            _ => null,
        };

        if (focus is not NeonVaultSymbol focusedSymbol)
        {
            return;
        }

        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < finalReel; reel++)
            {
                NeonVaultSymbol symbol = presenters[row, reel].Symbol ?? NeonVaultSymbol.Pulse;
                if (symbol == focusedSymbol)
                {
                    ApplyCandidateCell(row, reel, symbol, strong: true);
                }
                else
                {
                    ApplyDimmedCell(row, reel, 0.20);
                }
            }
        }
    }

    private void ShowResolvedWinFeedback()
    {
        NeonVaultSymbol[] grid = CapturePresentedGrid();
        var winningCells = new HashSet<int>();
        var groups = new List<EnhancedWinGroup>();

        foreach (NeonVaultSymbol symbol in EnhancedPayingSymbols)
        {
            EnhancedWinGroup? group = BuildConsecutiveGroup(grid, symbol, NeonVaultRules.Reels, requireAllReels: false);
            if (group is null || group.ReelCount < 3)
            {
                continue;
            }

            groups.Add(group);
            foreach (int index in group.Cells)
            {
                winningCells.Add(index);
            }
        }

        AddResolvedSpecial(grid, NeonVaultSymbol.Scatter, 3, winningCells);
        AddResolvedSpecial(grid, NeonVaultSymbol.VaultKey, 3, winningCells);

        ResetReelFrameFeedback();
        if (winningCells.Count == 0)
        {
            ResetEnhancedFeedback(clearPaths: true);
            return;
        }

        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                int index = NeonVaultRules.Index(row, reel);
                if (winningCells.Contains(index))
                {
                    ApplyWinningCell(row, reel, grid[index]);
                }
                else
                {
                    ApplyDimmedCell(row, reel, 0.18);
                }
            }
        }

        RenderEnhancedPaths(groups, winning: true);
    }

    private static void AddResolvedSpecial(
        IReadOnlyList<NeonVaultSymbol> grid,
        NeonVaultSymbol special,
        int minimum,
        ISet<int> winningCells)
    {
        int count = grid.Count(x => x == special);
        if (count < minimum)
        {
            return;
        }

        for (int index = 0; index < grid.Count; index++)
        {
            if (grid[index] == special)
            {
                winningCells.Add(index);
            }
        }
    }

    private EnhancedWinGroup? BuildConsecutiveGroup(
        IReadOnlyList<NeonVaultSymbol> grid,
        NeonVaultSymbol target,
        int maximumReels,
        bool requireAllReels)
    {
        var cellsInGroup = new List<int>();
        var path = new List<int>();
        int previousRow = NeonVaultRules.Rows / 2;
        int consecutive = 0;

        for (int reel = 0; reel < Math.Min(maximumReels, NeonVaultRules.Reels); reel++)
        {
            var matches = new List<int>();
            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                int index = NeonVaultRules.Index(row, reel);
                NeonVaultSymbol cell = grid[index];
                if (cell == target || cell == NeonVaultSymbol.Wild)
                {
                    matches.Add(index);
                }
            }

            if (matches.Count == 0)
            {
                if (requireAllReels)
                {
                    return null;
                }
                break;
            }

            consecutive++;
            cellsInGroup.AddRange(matches);
            int representative = matches
                .OrderBy(index => grid[index] == target ? 0 : 1)
                .ThenBy(index => Math.Abs(IndexRow(index) - previousRow))
                .First();
            path.Add(representative);
            previousRow = IndexRow(representative);
        }

        if (consecutive == 0 || (requireAllReels && consecutive < maximumReels))
        {
            return null;
        }

        return new EnhancedWinGroup(target, consecutive, cellsInGroup, path);
    }

    private void RenderEnhancedPaths(IEnumerable<EnhancedWinGroup> groups, bool winning)
    {
        if (enhancedPathCanvas is null)
        {
            return;
        }

        enhancedPathCanvas.Children.Clear();
        double width = enhancedPathCanvas.ActualWidth;
        double height = enhancedPathCanvas.ActualHeight;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        IEnumerable<EnhancedWinGroup> selected = groups
            .Where(group => group.PathCells.Count >= 2)
            .OrderByDescending(group => group.ReelCount)
            .Take(winning ? 4 : 3);

        foreach (EnhancedWinGroup group in selected)
        {
            var line = new Polyline
            {
                Stroke = Palettes[group.Symbol].BorderBrush,
                StrokeThickness = winning ? 3.2 : 2.0,
                Opacity = winning ? 0.92 : 0.62,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
            };
            if (!winning)
            {
                line.StrokeDashArray = new DoubleCollection { 2.5, 2.0 };
            }

            foreach (int index in group.PathCells)
            {
                int row = IndexRow(index);
                int reel = IndexReel(index);
                line.Points.Add(new Point(
                    (reel + 0.5) * width / NeonVaultRules.Reels,
                    (row + 0.5) * height / NeonVaultRules.Rows));
            }

            enhancedPathCanvas.Children.Add(line);
        }
    }

    private NeonVaultSymbol[] CapturePresentedGrid()
    {
        var grid = new NeonVaultSymbol[NeonVaultRules.Cells];
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                int index = NeonVaultRules.Index(row, reel);
                grid[index] = presenters[row, reel].Symbol ?? displayGrid[index];
            }
        }
        return grid;
    }

    private int HighestConsecutiveSettledReel()
    {
        int result = -1;
        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            if (!reelSettled[reel])
            {
                break;
            }
            result = reel;
        }
        return result;
    }

    private void ResetSettledCellFeedback(int settledThrough)
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel <= settledThrough && reel < NeonVaultRules.Reels; reel++)
            {
                ResetCellFeedback(row, reel);
            }
        }
    }

    private void ResetEnhancedFeedback(bool clearPaths)
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                ResetCellFeedback(row, reel);
            }
        }

        ResetReelFrameFeedback();
        if (clearPaths)
        {
            ClearEnhancedPaths();
        }
    }

    private void ResetCellFeedback(int row, int reel)
    {
        Border frame = cells[row, reel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = 1;
        frame.BorderBrush = Ink("#26304E");
        frame.BorderThickness = new Thickness(1);
        frame.Background = Ink("#A9090B14");
        ClearScaleAnimation(frame);
    }

    private void ApplyDimmedCell(int row, int reel, double opacity)
    {
        Border frame = cells[row, reel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = opacity;
        frame.BorderBrush = Ink("#1B2238");
        frame.BorderThickness = new Thickness(0.8);
        ClearScaleAnimation(frame);
    }

    private void ApplyCandidateCell(int row, int reel, NeonVaultSymbol symbol, bool strong = false)
    {
        Border frame = cells[row, reel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = 1;
        frame.Background = Ink(strong ? "#E3161A2A" : "#CA111827");
        frame.BorderBrush = Palettes[symbol].BorderBrush;
        frame.BorderThickness = new Thickness(strong ? 3.0 : 2.2);
        SetStaticScale(frame, strong ? 1.045 : 1.025);
    }

    private void ApplyWinningCell(int row, int reel, NeonVaultSymbol symbol)
    {
        Border frame = cells[row, reel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = 1;
        frame.Background = Ink("#F0182036");
        frame.BorderBrush = Palettes[symbol].BorderBrush;
        frame.BorderThickness = new Thickness(3.0);
        frame.RenderTransformOrigin = new Point(0.5, 0.5);

        var scale = new ScaleTransform(1.025, 1.025);
        frame.RenderTransform = scale;
        if (!AnimationsEnabled)
        {
            return;
        }

        var pulse = new DoubleAnimation(1.025, 1.065, TimeSpan.FromMilliseconds(Pace(260)))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3),
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }

    private static void SetStaticScale(Border frame, double value)
    {
        frame.RenderTransformOrigin = new Point(0.5, 0.5);
        frame.RenderTransform = new ScaleTransform(value, value);
    }

    private static void ClearScaleAnimation(Border frame)
    {
        if (frame.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
        frame.RenderTransform = Transform.Identity;
    }

    private void CancelGenericReelFlash(int reel)
    {
        Border frame = reelFrames[reel];
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = 1;
        frame.BorderBrush = Ink("#34415F");
        frame.BorderThickness = new Thickness(1);
        ClearScaleAnimation(frame);
    }

    private void ResetReelFrameFeedback()
    {
        for (int reel = 0; reel < reelFrames.Length; reel++)
        {
            CancelGenericReelFlash(reel);
        }
    }

    private void ClearEnhancedPaths() => enhancedPathCanvas?.Children.Clear();

    private static int IndexRow(int index) => index / NeonVaultRules.Reels;
    private static int IndexReel(int index) => index % NeonVaultRules.Reels;
}
