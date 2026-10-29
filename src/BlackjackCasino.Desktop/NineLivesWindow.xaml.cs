using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class NineLivesWindow : Window
{
    private static readonly double[] ReelX = [366, 639, 912];
    private static readonly double[] ReelY = [185, 399, 609];
    private static readonly Brush Gold = FrozenBrush("#FFD56A");
    private static readonly Brush Violet = FrozenBrush("#B34DFF");
    private static readonly Brush DimGold = FrozenBrush("#9D6B2C");
    private static readonly Brush SoftText = FrozenBrush("#EEDFF4");
    private static readonly NineLivesSymbol[] PreviewSymbols = Enum.GetValues<NineLivesSymbol>();

    private readonly NineLivesSession machine;
    private readonly PlayerProgressionController progression;
    private readonly Func<NineLivesSpinResult, ProgressionAward> commitSpin;
    private readonly SlotAudio audio = new();
    private readonly BackgroundMusic music = new(Soundtrack.Manor);
    private readonly Random visualRandom = new(0x9_1_2026);
    private readonly Border[] cells = new Border[NineLivesRules.Cells];
    private readonly Image[] symbolImages = new Image[NineLivesRules.Cells];
    private readonly ScaleTransform[] symbolScale = new ScaleTransform[NineLivesRules.Cells];

    private decimal selectedBet;
    private decimal lastWin;
    private bool soundsEnabled;
    private bool animationsEnabled;
    private readonly bool musicEnabled;
    private readonly double musicVolume;
    private bool spinning;
    private bool autoSpinning;
    private bool closed;

    public NineLivesWindow(
        decimal bankroll,
        decimal bet,
        bool sounds,
        bool animations,
        PlayerProgressionController progression,
        Func<NineLivesSpinResult, ProgressionAward> commitSpin,
        bool musicEnabled = true,
        double musicVolume = 0.10)
    {
        InitializeComponent();
        machine = new NineLivesSession(bankroll);
        this.progression = progression;
        this.commitSpin = commitSpin;
        soundsEnabled = sounds;
        animationsEnabled = animations;
        this.musicEnabled = musicEnabled;
        this.musicVolume = musicVolume;
        selectedBet = ClosestBet(Math.Clamp(bet, NineLivesRules.MinimumBet, NineLivesRules.MaximumBet));

        BackgroundArt.Source = NineLivesArtwork.Background;
        BuildReels();
        SetGrid(
        [
            NineLivesSymbol.BlackCat, NineLivesSymbol.Yarn, NineLivesSymbol.Fishbone,
            NineLivesSymbol.WhiteCat, NineLivesSymbol.Bonus, NineLivesSymbol.Bell,
            NineLivesSymbol.Fishbone, NineLivesSymbol.TabbyCat, NineLivesSymbol.Wild,
        ]);

        MenuAudio.Attach(this, () => SoundsEnabled);
        Loaded += (_, _) =>
        {
            music.Start(this.musicEnabled, this.musicVolume);
            RefreshUi();
        };
        RefreshUi();
    }

    public decimal SelectedBet => selectedBet;
    public bool SoundsEnabled => soundsEnabled;
    public bool AnimationsEnabled => animationsEnabled;

    private void BuildReels()
    {
        ReelCanvas.Children.Clear();
        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            for (int reel = 0; reel < NineLivesRules.Reels; reel++)
            {
                int index = NineLivesRules.Index(row, reel);
                var scale = new ScaleTransform(1, 1);
                symbolScale[index] = scale;
                var image = new Image
                {
                    Stretch = Stretch.UniformToFill,
                    RenderTransform = scale,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    SnapsToDevicePixels = true,
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                symbolImages[index] = image;

                var frame = new Border
                {
                    Width = 257,
                    Height = 199,
                    CornerRadius = new CornerRadius(13),
                    BorderThickness = new Thickness(2),
                    BorderBrush = DimGold,
                    Background = new SolidColorBrush(Color.FromArgb(245, 8, 6, 15)),
                    ClipToBounds = true,
                    Child = image,
                    SnapsToDevicePixels = true,
                };
                cells[index] = frame;
                Canvas.SetLeft(frame, ReelX[reel]);
                Canvas.SetTop(frame, ReelY[row]);
                ReelCanvas.Children.Add(frame);
            }
        }
    }

    private void SetGrid(IReadOnlyList<NineLivesSymbol> grid)
    {
        for (int i = 0; i < NineLivesRules.Cells; i++)
        {
            SetSymbol(i, grid[i]);
        }
    }

    private void SetSymbol(int index, NineLivesSymbol symbol)
    {
        symbolImages[index].Source = NineLivesArtwork.Symbol(symbol);
        cells[index].Tag = symbol;
    }

    private async void SpinButton_Click(object sender, RoutedEventArgs e) => await SpinAsync();

    private async Task SpinAsync()
    {
        if (spinning || closed)
        {
            return;
        }
        if (!machine.BonusActive && selectedBet > machine.Bankroll)
        {
            StatusText.Text = "Not enough bankroll for that bet.";
            return;
        }

        spinning = true;
        ResetPresentation();
        RefreshUi();
        audio.Play(SlotSound.Start, SoundsEnabled);

        NineLivesSpinResult result;
        try
        {
            result = machine.Spin(selectedBet);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            StatusText.Text = ex.Message;
            spinning = false;
            RefreshUi();
            return;
        }

        if (AnimationsEnabled)
        {
            await AnimateReelsAsync(result);
        }
        else
        {
            SetGrid(result.Grid);
        }

        if (result.PounceWildCell.HasValue)
        {
            await PresentPounceAsync(result.PounceWildCell.Value);
        }

        lastWin = result.Payout;
        RefreshUi();
        await PresentResultAsync(result);

        ProgressionAward award = commitSpin(result);
        if (award.HasLevelUp)
        {
            StatusText.Text += $"  ·  LEVEL {progression.Profile.Level}";
        }

        spinning = false;
        RefreshUi();
    }

    private async Task AnimateReelsAsync(NineLivesSpinResult result)
    {
        bool[] stopped = new bool[NineLivesRules.Reels];
        int[] stopTick = [10, 17, 24];
        int bonusSeen = 0;

        for (int tick = 0; tick <= stopTick[^1]; tick++)
        {
            for (int reel = 0; reel < NineLivesRules.Reels; reel++)
            {
                if (stopped[reel])
                {
                    continue;
                }

                if (tick >= stopTick[reel])
                {
                    stopped[reel] = true;
                    for (int row = 0; row < NineLivesRules.Rows; row++)
                    {
                        int index = NineLivesRules.Index(row, reel);
                        SetSymbol(index, result.Grid[index]);
                        RenderOptions.SetBitmapScalingMode(symbolImages[index], BitmapScalingMode.HighQuality);
                        if (result.Grid[index] == NineLivesSymbol.Bonus)
                        {
                            bonusSeen++;
                        }
                    }

                    audio.Play(SlotSound.Stop, SoundsEnabled);
                    await PresentReelStopAsync(reel);

                    if (bonusSeen >= 2 && reel < NineLivesRules.Reels - 1)
                    {
                        audio.Play(SlotSound.Anticipation, SoundsEnabled);
                        StatusText.Text = "Two lives are glowing. One more can open the night…";
                        await Task.Delay(AnimationsEnabled ? 520 : 0);
                    }
                    continue;
                }

                for (int row = 0; row < NineLivesRules.Rows; row++)
                {
                    int index = NineLivesRules.Index(row, reel);
                    SetSymbol(index, PreviewSymbols[visualRandom.Next(PreviewSymbols.Length)]);
                    RenderOptions.SetBitmapScalingMode(symbolImages[index], BitmapScalingMode.LowQuality);
                }
            }

            await Task.Delay(72);
        }

        for (int i = 0; i < symbolImages.Length; i++)
        {
            RenderOptions.SetBitmapScalingMode(symbolImages[i], BitmapScalingMode.HighQuality);
        }
    }

    private async Task PresentReelStopAsync(int reel)
    {
        if (!AnimationsEnabled)
        {
            return;
        }

        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            int index = NineLivesRules.Index(row, reel);
            cells[index].BorderBrush = Gold;
            cells[index].BorderThickness = new Thickness(3);

            var settle = new DoubleAnimation(0.92, 1.0, TimeSpan.FromMilliseconds(190))
            {
                EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut },
            };
            symbolScale[index].BeginAnimation(ScaleTransform.ScaleXProperty, settle);
            symbolScale[index].BeginAnimation(ScaleTransform.ScaleYProperty, settle);

            var fade = new DoubleAnimation(0.62, 1.0, TimeSpan.FromMilliseconds(180));
            symbolImages[index].BeginAnimation(OpacityProperty, fade);
        }

        await Task.Delay(210);
        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            int index = NineLivesRules.Index(row, reel);
            cells[index].BorderBrush = DimGold;
            cells[index].BorderThickness = new Thickness(2);
        }
    }

    private async Task PresentPounceAsync(int index)
    {
        cells[index].BorderBrush = Violet;
        cells[index].BorderThickness = new Thickness(5);
        cells[index].Effect = new DropShadowEffect
        {
            Color = Color.FromRgb(190, 77, 255),
            BlurRadius = 28,
            ShadowDepth = 0,
            Opacity = 0.88,
        };
        audio.Play(SlotSound.Wild, SoundsEnabled);
        StatusText.Text = "POUNCE WILD — the black cat struck another cell.";
        if (!AnimationsEnabled)
        {
            return;
        }

        var pounce = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(620) };
        pounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0)));
        pounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.18, KeyTime.FromPercent(0.24), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        pounce.KeyFrames.Add(new EasingDoubleKeyFrame(0.94, KeyTime.FromPercent(0.52)));
        pounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.08, KeyTime.FromPercent(0.76)));
        pounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1)));
        symbolScale[index].BeginAnimation(ScaleTransform.ScaleXProperty, pounce);
        symbolScale[index].BeginAnimation(ScaleTransform.ScaleYProperty, pounce);

        var flash = new DoubleAnimation(0.58, 1.0, TimeSpan.FromMilliseconds(210))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2),
        };
        cells[index].BeginAnimation(OpacityProperty, flash);
        await Task.Delay(660);
        cells[index].BeginAnimation(OpacityProperty, null);
    }

    private async Task PresentResultAsync(NineLivesSpinResult result)
    {
        WinLineCanvas.Children.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            bool winner = result.FullGridFrenzy || result.WinningCells.Contains(i);
            cells[i].Opacity = result.WinningCells.Count == 0 && !result.FullGridFrenzy ? 1 : winner ? 1 : 0.36;
            cells[i].BorderBrush = winner ? Gold : DimGold;
            cells[i].BorderThickness = winner ? new Thickness(5) : new Thickness(2);
            cells[i].Effect = winner
                ? new DropShadowEffect { Color = Color.FromRgb(255, 213, 106), BlurRadius = 18, ShadowDepth = 0, Opacity = 0.60 }
                : null;

            if (winner && AnimationsEnabled)
            {
                var pulse = new DoubleAnimation(1, 1.075, TimeSpan.FromMilliseconds(260))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                };
                symbolScale[i].BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
                symbolScale[i].BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
            }
        }

        foreach (int lineIndex in result.WinningLines)
        {
            DrawPayline(lineIndex);
        }

        if (result.FullGridFrenzy)
        {
            audio.Play(SlotSound.Jackpot, SoundsEnabled);
            StatusText.Text = $"FULL GRID FRENZY · {NineLivesRules.FullGridMultiplier:N0}× · WIN {FormatMoney(result.Payout)}";
            StatusText.FontSize = 20;
            StatusText.Foreground = Gold;
            await PulseWinAmountAsync(1450);
        }
        else if (result.BonusSpinsAwarded > 0)
        {
            audio.Play(SlotSound.FreeSpins, SoundsEnabled);
            StatusText.Text = $"NINE LIVES AWAKENED · {result.BonusSpinsAwarded} BONUS SPINS";
            StatusText.FontSize = 18;
            StatusText.Foreground = Violet;
            await PulseWinAmountAsync(1050);
        }
        else if (result.Payout > 0)
        {
            audio.Play(SlotSound.Win, SoundsEnabled);
            StatusText.Text = $"WIN {FormatMoney(result.Payout)}  ·  {result.WinningLines.Count} LINE{(result.WinningLines.Count == 1 ? "" : "S")}  ·  {result.TotalMultiplier:0.##}×";
            StatusText.FontSize = 19;
            StatusText.Foreground = Gold;
            await PulseWinAmountAsync(820);
        }
        else
        {
            audio.Play(SlotSound.Loss, SoundsEnabled);
            StatusText.Text = result.BonusCount == 2
                ? "Two lives. The third stayed in the shadows."
                : "The rooftop goes quiet.";
            await Task.Delay(AnimationsEnabled ? 360 : 80);
        }

        if (result.BonusSpinsRemaining > 0)
        {
            BonusBadge.Visibility = Visibility.Visible;
            BonusText.Text = $"NINE LIVES · {result.BonusSpinsRemaining} SPINS LEFT";
        }
        else
        {
            BonusBadge.Visibility = Visibility.Collapsed;
        }
    }

    private async Task PulseWinAmountAsync(int holdMilliseconds)
    {
        if (!AnimationsEnabled)
        {
            return;
        }

        WinText.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = new ScaleTransform(1, 1);
        WinText.RenderTransform = scale;
        var pulse = new DoubleAnimation(1, 1.30, TimeSpan.FromMilliseconds(230))
        {
            AutoReverse = true,
            EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut },
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        await Task.Delay(holdMilliseconds);
    }

    private void DrawPayline(int lineIndex)
    {
        IReadOnlyList<int> line = NineLivesRules.Lines[lineIndex];
        Point start = Center(line[0]);
        Point end = Center(line[^1]);
        var glow = new System.Windows.Shapes.Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = new SolidColorBrush(Color.FromArgb(135, 185, 69, 255)),
            StrokeThickness = 12,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
            Opacity = AnimationsEnabled ? 0 : 1,
        };
        var core = new System.Windows.Shapes.Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = Gold,
            StrokeThickness = 4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
            Opacity = AnimationsEnabled ? 0 : 1,
        };
        WinLineCanvas.Children.Add(glow);
        WinLineCanvas.Children.Add(core);

        if (AnimationsEnabled)
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
            };
            glow.BeginAnimation(OpacityProperty, fade);
            core.BeginAnimation(OpacityProperty, fade);
        }
    }

    private static Point Center(int index)
    {
        int row = index / NineLivesRules.Reels;
        int reel = index % NineLivesRules.Reels;
        return new Point(ReelX[reel] + 128.5, ReelY[row] + 99.5);
    }

    private void ResetPresentation()
    {
        WinLineCanvas.Children.Clear();
        BonusBadge.Visibility = machine.BonusActive ? Visibility.Visible : Visibility.Collapsed;
        StatusText.FontSize = 14;
        StatusText.Foreground = SoftText;
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Opacity = 1;
            cells[i].Effect = null;
            cells[i].BorderBrush = DimGold;
            cells[i].BorderThickness = new Thickness(2);
            symbolScale[i].BeginAnimation(ScaleTransform.ScaleXProperty, null);
            symbolScale[i].BeginAnimation(ScaleTransform.ScaleYProperty, null);
            symbolScale[i].ScaleX = 1;
            symbolScale[i].ScaleY = 1;
            symbolImages[i].BeginAnimation(OpacityProperty, null);
            symbolImages[i].Opacity = 1;
        }
    }

    private void BetDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        int index = Math.Max(0, NineLivesRules.Bets.IndexOf(selectedBet) - 1);
        selectedBet = NineLivesRules.Bets[index];
        RefreshUi();
    }

    private void BetUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        int current = NineLivesRules.Bets.IndexOf(selectedBet);
        int index = Math.Min(NineLivesRules.Bets.Count - 1, current + 1);
        decimal candidate = NineLivesRules.Bets[index];
        if (candidate <= machine.Bankroll)
        {
            selectedBet = candidate;
        }
        RefreshUi();
    }

    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        selectedBet = NineLivesRules.Bets.LastOrDefault(bet => bet <= machine.Bankroll);
        if (selectedBet < NineLivesRules.MinimumBet)
        {
            selectedBet = NineLivesRules.MinimumBet;
        }
        RefreshUi();
    }

    private async void AutoButton_Click(object sender, RoutedEventArgs e)
    {
        if (autoSpinning)
        {
            autoSpinning = false;
            StatusText.Text = "Auto spin stopped.";
            RefreshUi();
            return;
        }

        autoSpinning = true;
        RefreshUi();
        int remaining = 20;
        while (autoSpinning && !closed && remaining-- > 0)
        {
            if (!machine.BonusActive && machine.Bankroll < selectedBet)
            {
                break;
            }
            await SpinAsync();
            if (!autoSpinning || closed) break;
            await Task.Delay(AnimationsEnabled ? 620 : 180);
        }
        autoSpinning = false;
        RefreshUi();
    }

    private void CasinoButton_Click(object sender, RoutedEventArgs e)
    {
        autoSpinning = false;
        Close();
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (PaytableFlyout.Visibility == Visibility.Visible || SettingsFlyout.Visibility == Visibility.Visible)
            {
                PaytableFlyout.Visibility = Visibility.Collapsed;
                SettingsFlyout.Visibility = Visibility.Collapsed;
            }
            else
            {
                autoSpinning = false;
                Close();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            await SpinAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.A)
        {
            AutoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
        }
        else if (e.Key is Key.Add or Key.OemPlus)
        {
            BetUpButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key is Key.Subtract or Key.OemMinus)
        {
            BetDownButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void RefreshUi()
    {
        BankrollText.Text = machine.Bankroll.ToString("#,##0.##", CultureInfo.InvariantCulture);
        LevelText.Text = $"Lvl {Math.Max(1, progression.Profile.Level)}";
        BetText.Text = machine.BonusActive ? "FREE" : selectedBet.ToString("#,##0.##", CultureInfo.InvariantCulture);
        WinText.Text = lastWin > 0 ? FormatMoney(lastWin) : "—";
        BetDownButton.IsEnabled = !spinning && !machine.BonusActive && NineLivesRules.Bets.IndexOf(selectedBet) > 0;
        BetUpButton.IsEnabled = !spinning && !machine.BonusActive && NineLivesRules.Bets.IndexOf(selectedBet) < NineLivesRules.Bets.Count - 1;
        MaxButton.IsEnabled = !spinning && !machine.BonusActive;
        SpinButton.IsEnabled = !spinning && (machine.BonusActive || machine.Bankroll >= selectedBet);
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
        AutoButton.IsEnabled = !spinning || autoSpinning;
        if (machine.BonusActive)
        {
            BonusBadge.Visibility = Visibility.Visible;
            BonusText.Text = $"NINE LIVES · {machine.BonusSpinsRemaining} SPINS LEFT";
        }
    }

    private static string FormatMoney(decimal amount)
        => amount.ToString("#,##0.##", CultureInfo.InvariantCulture);

    private static decimal ClosestBet(decimal target) => NineLivesRules.Bets.OrderBy(value => Math.Abs(value - target)).First();

    private static SolidColorBrush FrozenBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        closed = true;
        autoSpinning = false;
        audio.Dispose();
        music.Dispose();
    }
}

internal static class NineLivesListExtensions
{
    internal static int IndexOf(this IReadOnlyList<decimal> values, decimal target)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == target) return i;
        }
        return -1;
    }
}
