using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class NeonVaultWindow : Window
{
    private const double PresentationPace = 1.45;
    private static readonly NeonVaultSymbol[] PreviewSymbols = Enum.GetValues<NeonVaultSymbol>();
    private static readonly IReadOnlyDictionary<NeonVaultSymbol, SymbolPalette> Palettes = BuildPalettes();

    private readonly NeonVaultSession machine;
    private readonly PlayerProgressionController progression;
    private readonly Func<NeonVaultSpinResult, ProgressionAward> commitSpin;
    private readonly SlotAudio audio = new();
    private readonly BackgroundMusic music = new(Soundtrack.Reels);
    private readonly DispatcherTimer previewTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Random visualRandom = new(0x4E56_2026);
    private readonly Border[,] cells = new Border[NeonVaultRules.Rows, NeonVaultRules.Reels];
    private readonly SymbolPresenter[,] presenters = new SymbolPresenter[NeonVaultRules.Rows, NeonVaultRules.Reels];
    private readonly Border[] reelFrames = new Border[NeonVaultRules.Reels];
    private readonly TranslateTransform[] reelMotion = new TranslateTransform[NeonVaultRules.Reels];
    private readonly bool[] reelSettled = new bool[NeonVaultRules.Reels];

    private NeonVaultSymbol[] displayGrid = Enumerable.Repeat(NeonVaultSymbol.Pulse, NeonVaultRules.Cells).ToArray();
    private decimal selectedBet;
    private bool spinning;
    private bool initialized;
    private bool closed;
    private NeonVaultNearMiss activeNearMiss;

    private sealed record SymbolPalette(
        string Icon,
        string Label,
        Brush IconBrush,
        Brush BorderBrush,
        Brush Background,
        double IconSize,
        bool Emphasis);

    private sealed class SymbolPresenter(Border card, TextBlock icon, TextBlock label)
    {
        internal Border Card { get; } = card;
        internal TextBlock Icon { get; } = icon;
        internal TextBlock Label { get; } = label;
        internal NeonVaultSymbol? Symbol { get; set; }
        internal bool Compact { get; set; }
    }

    public NeonVaultWindow(
        decimal bankroll,
        decimal bet,
        bool sounds,
        bool animations,
        PlayerProgressionController progression,
        Func<NeonVaultSpinResult, ProgressionAward> commitSpin,
        bool musicEnabled = true,
        double musicVolume = 0.10)
    {
        InitializeComponent();
        machine = new NeonVaultSession(bankroll);
        this.progression = progression;
        this.commitSpin = commitSpin;
        selectedBet = ClosestBet(Math.Clamp(bet, NeonVaultRules.MinimumBet, NeonVaultRules.MaximumBet));
        SoundToggle.IsChecked = sounds;
        AnimationToggle.IsChecked = animations;
        BuildReels();
        SetGrid(displayGrid);
        previewTimer.Tick += PreviewTimer_Tick;
        MenuAudio.Attach(this, () => SoundsEnabled);
        Loaded += (_, _) =>
        {
            StartAmbientMotion();
            music.Start(musicEnabled, musicVolume);
        };
        initialized = true;
        RefreshUi();
    }

    public decimal SelectedBet => selectedBet;
    public bool SoundsEnabled => SoundToggle.IsChecked == true;
    public bool AnimationsEnabled => AnimationToggle.IsChecked == true;

    private void BuildReels()
    {
        ReelsHost.Children.Clear();
        ReelsHost.ColumnDefinitions.Clear();

        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            ReelsHost.ColumnDefinitions.Add(new ColumnDefinition());
            var motion = new TranslateTransform();
            reelMotion[reel] = motion;

            var reelGrid = new Grid
            {
                Margin = new Thickness(5, 0, 5, 0),
                RenderTransform = motion,
                RenderTransformOrigin = new Point(0.5, 0.5),
                SnapsToDevicePixels = true,
            };

            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                reelGrid.RowDefinitions.Add(new RowDefinition());
                SymbolPresenter presenter = CreateSymbolPresenter();
                var frame = new Border
                {
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(4),
                    CornerRadius = new CornerRadius(8),
                    Background = Ink("#A9090B14"),
                    BorderBrush = Ink("#26304E"),
                    BorderThickness = new Thickness(1),
                    ClipToBounds = true,
                    SnapsToDevicePixels = true,
                    Child = presenter.Card,
                };
                Grid.SetRow(frame, row);
                reelGrid.Children.Add(frame);
                cells[row, reel] = frame;
                presenters[row, reel] = presenter;
            }

            var reelBorder = new Border
            {
                Background = Ink("#3A0A0D17"),
                BorderBrush = Ink("#34415F"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(3),
                Child = reelGrid,
                SnapsToDevicePixels = true,
            };
            reelFrames[reel] = reelBorder;
            Grid.SetColumn(reelBorder, reel);
            ReelsHost.Children.Add(reelBorder);
        }
    }

    private SymbolPresenter CreateSymbolPresenter()
    {
        var icon = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var label = new TextBlock
        {
            FontSize = 8,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#D9DCF0"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 5),
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(icon);
        Grid.SetRow(label, 1);
        grid.Children.Add(label);

        var card = new Border
        {
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(7),
            Child = grid,
            SnapsToDevicePixels = true,
        };

        var presenter = new SymbolPresenter(card, icon, label);
        ApplySymbol(presenter, NeonVaultSymbol.Pulse, compact: false);
        return presenter;
    }

    private static IReadOnlyDictionary<NeonVaultSymbol, SymbolPalette> BuildPalettes()
    {
        return new Dictionary<NeonVaultSymbol, SymbolPalette>
        {
            [NeonVaultSymbol.Pulse] = Palette("✦", "PULSE", "#51C9FF", "#1A2038", "#101427", 31, false),
            [NeonVaultSymbol.Prism] = Palette("◆", "PRISM", "#9C72FF", "#251942", "#12102A", 31, false),
            [NeonVaultSymbol.Chip] = Palette("●", "HIGH ROLLER", "#58F0D2", "#142F37", "#0D1923", 31, false),
            [NeonVaultSymbol.Crown] = Palette("♛", "CROWN", "#FFD66E", "#3B2B15", "#1A140D", 31, false),
            [NeonVaultSymbol.Key] = Palette("⌁", "VAULT KEY", "#FFAF66", "#342216", "#171116", 31, false),
            [NeonVaultSymbol.Wild] = Palette("W", "WILD", "#67F3FF", "#0D3B4C", "#15194A", 36, true),
            [NeonVaultSymbol.Scatter] = Palette("▣", "VAULT", "#FFD66E", "#4A2D16", "#1D1320", 33, true),
            [NeonVaultSymbol.VaultKey] = Palette("◇", "ACCESS KEY", "#D49BFF", "#38244B", "#161329", 33, true),
        };
    }

    private static SymbolPalette Palette(
        string icon,
        string label,
        string glow,
        string top,
        string bottom,
        double iconSize,
        bool emphasis)
    {
        return new SymbolPalette(
            icon,
            label,
            FrozenSolid(glow),
            FrozenSolid(glow),
            FrozenGradient(top, bottom),
            iconSize,
            emphasis);
    }

    private static SolidColorBrush FrozenSolid(string hex)
    {
        var brush = new SolidColorBrush(InkColor(hex));
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush FrozenGradient(string top, string bottom)
    {
        var brush = new LinearGradientBrush(InkColor(top), InkColor(bottom), 80);
        brush.Freeze();
        return brush;
    }

    private static void ApplySymbol(SymbolPresenter presenter, NeonVaultSymbol symbol, bool compact)
    {
        if (presenter.Symbol == symbol && presenter.Compact == compact)
        {
            return;
        }

        SymbolPalette palette = Palettes[symbol];
        presenter.Symbol = symbol;
        presenter.Compact = compact;
        presenter.Card.Background = palette.Background;
        presenter.Card.BorderBrush = palette.BorderBrush;
        presenter.Card.BorderThickness = new Thickness(palette.Emphasis ? 1.4 : 0.7);
        presenter.Icon.Text = palette.Icon;
        presenter.Icon.Foreground = palette.IconBrush;
        presenter.Icon.FontSize = compact ? Math.Min(29, palette.IconSize) : palette.IconSize;
        presenter.Label.Text = palette.Label;
        presenter.Label.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void Spin_Click(object sender, RoutedEventArgs e) => await SpinAsync();

    private async Task SpinAsync()
    {
        if (spinning || closed || (!machine.BonusActive && selectedBet > machine.Bankroll))
        {
            return;
        }

        spinning = true;
        Array.Fill(reelSettled, false);
        RefreshUi();

        NeonVaultSpinResult result;
        try
        {
            result = machine.Spin(selectedBet);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            spinning = false;
            ResultText.Text = ex.Message;
            RefreshUi();
            return;
        }

        activeNearMiss = result.NearMiss;
        ProgressionAward award = commitSpin(result);
        BalanceText.Text = Money(result.BankrollBefore - result.Stake);
        SystemStatusText.Text = result.IsFreeSpin ? "VAULT BREAK ACTIVE" : "REELS ENERGIZED";
        AnticipationText.Text = result.IsFreeSpin
            ? $"FREE SPIN · {result.FreeSpinsRemaining + 1} IN SEQUENCE"
            : "OUTCOME LOCKED · PRESENTATION RUNNING";
        ResultText.Text = result.IsFreeSpin ? "Free Spin resolving…" : "Vault reels spinning…";
        RewardText.Text = "Outcome settled. The reels now present the result.";
        audio.Play(SlotSound.Start, SoundsEnabled);

        if (AnimationsEnabled)
        {
            StartReelMotion();
            previewTimer.Start();
            await AnimateReelStops(result);
            previewTimer.Stop();
            if (closed) return;
            await PlayFeatureSequence(result);
        }
        else
        {
            SetGrid(result.FinalGrid);
        }

        if (closed) return;
        await FinishSpin(result, award);
    }

    private async Task AnimateReelStops(NeonVaultSpinResult result)
    {
        int[] pauses =
        [
            Pace(680),
            Pace(330),
            Pace(360),
            Pace(400),
            Pace(result.NearMiss == NeonVaultNearMiss.None ? 500 : 1_100),
        ];

        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            if (reel == NeonVaultRules.Reels - 1 && result.NearMiss != NeonVaultNearMiss.None)
            {
                BeginAnticipation(result.NearMiss);
                audio.Play(SlotSound.Anticipation, SoundsEnabled);
            }

            await Task.Delay(pauses[reel]);
            if (closed) return;

            StopReelMotion(reel);
            SetReel(reel, result.BaseGrid);
            reelSettled[reel] = true;
            FlashReel(reel, result.NearMiss != NeonVaultNearMiss.None && reel == NeonVaultRules.Reels - 1 ? 4 : 2);
            audio.Play(SlotSound.Stop, SoundsEnabled);
        }

        AnticipationText.Text = result.NearMiss switch
        {
            NeonVaultNearMiss.Scatter => "VAULT SIGNAL PASSED THE FINAL REEL",
            NeonVaultNearMiss.Jackpot => "ACCESS KEY MISSED THE UPGRADE",
            NeonVaultNearMiss.Blackout => "POWER GRID UNSTABLE",
            _ => "1,024 WAYS EVALUATING",
        };
        displayGrid = result.BaseGrid.ToArray();
    }

    private void StartReelMotion()
    {
        for (int reel = 0; reel < reelMotion.Length; reel++)
        {
            var animation = new DoubleAnimation(-3.5, 4.5, TimeSpan.FromMilliseconds(Pace(230)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            reelMotion[reel].BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    private void StopReelMotion(int reel)
    {
        reelMotion[reel].BeginAnimation(TranslateTransform.YProperty, null);
        reelMotion[reel].Y = 0;
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            if (reelSettled[reel]) continue;

            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                NeonVaultSymbol symbol = PreviewSymbols[visualRandom.Next(PreviewSymbols.Length)];
                if (reel == NeonVaultRules.Reels - 1 && visualRandom.Next(4) == 0)
                {
                    symbol = activeNearMiss switch
                    {
                        NeonVaultNearMiss.Scatter => NeonVaultSymbol.Scatter,
                        NeonVaultNearMiss.Jackpot => NeonVaultSymbol.VaultKey,
                        _ => symbol,
                    };
                }

                ApplySymbol(presenters[row, reel], symbol, compact: true);
            }
        }
    }

    private async Task PlayFeatureSequence(NeonVaultSpinResult result)
    {
        displayGrid = result.BaseGrid.ToArray();

        if (result.NearMiss == NeonVaultNearMiss.Blackout && result.Blackout == NeonVaultBlackout.None)
        {
            audio.Play(SlotSound.Blackout, SoundsEnabled);
            await ShowFeature("SIGNAL INSTABILITY", "POWER DROP DETECTED · SYSTEM RECOVERING", Ink("#FF5B79"), 620);
            SystemStatusText.Text = "SYSTEM STABLE";
        }

        if (result.SecurityOverrideReel is int overrideReel)
        {
            audio.Play(SlotSound.FreeSpins, SoundsEnabled);
            await ShowFeature("SECURITY OVERRIDE", $"REEL {overrideReel + 1} FORCED WILD", Ink("#65EFFF"), 760);
            ApplyFullReelWild(overrideReel);
            FlashReel(overrideReel, 5);
            await VisualDelay(360);
        }

        if (result.Blackout != NeonVaultBlackout.None)
        {
            audio.Play(SlotSound.Blackout, SoundsEnabled);
            (string title, string subtitle, Brush color) = result.Blackout switch
            {
                NeonVaultBlackout.Total => ("TOTAL BLACKOUT", "ACCESS CONTROL OFFLINE · VAULT SECURITY DISABLED", Ink("#FF3E68")),
                NeonVaultBlackout.Overload => ("BLACKOUT OVERLOAD", "POWER SURGE · MULTIPLE WILD INJECTIONS", Ink("#FF6687")),
                _ => ("BLACKOUT", "EMERGENCY GRID · WILD INJECTION", Ink("#FF718D")),
            };
            await ShowFeature(title, subtitle, color, result.Blackout == NeonVaultBlackout.Total ? 1_000 : 760);

            if (result.BlackoutFullReel is int fullReel)
            {
                FlashReel(fullReel, 6);
            }

            foreach (int index in result.BlackoutWildCells)
            {
                int row = index / NeonVaultRules.Reels;
                int reel = index % NeonVaultRules.Reels;
                if (CanBecomeWild(displayGrid[index]))
                {
                    displayGrid[index] = NeonVaultSymbol.Wild;
                    SetCell(row, reel, NeonVaultSymbol.Wild, true);
                    await VisualDelay(90);
                }
            }
        }

        if (result.ExpandedReels.Count > 0)
        {
            audio.Play(SlotSound.Wild, SoundsEnabled);
            await ShowFeature(
                "WILD EXPANSION",
                result.IsFreeSpin ? "VAULT BREAK GUARANTEED EXPANSION" : "ENERGY CASCADE DETECTED",
                Ink("#65EFFF"),
                650);

            foreach (int reel in result.ExpandedReels)
            {
                ApplyFullReelWild(reel);
                FlashReel(reel, 5);
                await VisualDelay(270);
            }
        }

        SetGrid(result.FinalGrid);

        if (result.Jackpot != NeonVaultJackpot.None)
        {
            string title = result.Jackpot switch
            {
                NeonVaultJackpot.Grand => "GRAND VAULT",
                NeonVaultJackpot.Major => "MAJOR ACCESS",
                NeonVaultJackpot.Minor => "MINOR ACCESS",
                _ => "MINI ACCESS",
            };
            audio.Play(SlotSound.Jackpot, SoundsEnabled);
            await ShowFeature(
                title,
                $"ACCESS GRANTED · {Money(result.JackpotPayout)}",
                Ink("#FFD66E"),
                result.Jackpot == NeonVaultJackpot.Grand ? 1_350 : 950);
        }

        if (result.FreeSpinsAwarded > 0)
        {
            audio.Play(SlotSound.FreeSpins, SoundsEnabled);
            string title = result.IsFreeSpin ? "VAULT BREAK EXTENDED" : "VAULT BREAK";
            await ShowFeature(
                title,
                $"+{result.FreeSpinsAwarded} FREE SPINS · EXPANDING WILDS GUARANTEED",
                Ink("#FFD66E"),
                950);
        }
    }

    private async Task FinishSpin(NeonVaultSpinResult result, ProgressionAward award)
    {
        spinning = false;
        activeNearMiss = NeonVaultNearMiss.None;
        LastWinText.Text = Money(result.Payout);
        LastFeatureText.Text = FeatureSummary(result);

        string multiplier = result.TotalMultiplier >= 1m ? $" · {result.TotalMultiplier:0.##}× BET" : string.Empty;
        ResultText.Text = result.Payout > 0
            ? $"{result.WaysWon:N0} winning ways · {Money(result.Payout)} returned{multiplier}"
            : result.NearMiss == NeonVaultNearMiss.None
                ? "Vault stayed sealed this spin."
                : "Signal lost. The vault stays sealed.";

        string level = award.HasLevelUp ? $" · LEVEL {progression.Profile.Level}!" : string.Empty;
        RewardText.Text = $"+{award.PlayerExperience} XP · +{award.SeasonExperience} Season XP{level}";
        SystemStatusText.Text = result.FreeSpinsRemaining > 0 ? "VAULT BREAK ACTIVE" : "VAULT ONLINE";
        AnticipationText.Text = result.FreeSpinsRemaining > 0 ? "AUTOMATIC FREE SPIN SEQUENCE" : "1,024 WAYS ACTIVE";

        if (result.TotalMultiplier >= 20m)
        {
            audio.Play(result.TotalMultiplier >= 250m ? SlotSound.Jackpot : SlotSound.Win, SoundsEnabled);
            await ShowBigWin(result);
        }
        else
        {
            audio.Play(result.Net > 0 ? SlotSound.Win : result.Net == 0 ? SlotSound.Push : SlotSound.Loss, SoundsEnabled);
        }

        RefreshUi();

        if (machine.FreeSpinsRemaining > 0 && !closed)
        {
            await Task.Delay(AnimationsEnabled ? Pace(850) : 120);
            if (!closed)
            {
                await SpinAsync();
            }
        }
    }

    private async Task ShowBigWin(NeonVaultSpinResult result)
    {
        BigWinTitle.Text = result.TotalMultiplier switch
        {
            >= 1_000m => "VAULT-BREAKING WIN",
            >= 250m => "MEGA WIN",
            >= 50m => "BIG WIN",
            _ => "NICE WIN",
        };
        BigWinAmount.Text = Money(result.Payout);
        BigWinPanel.Opacity = 0;
        BigWinPanel.Visibility = Visibility.Visible;

        var enter = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Pace(210)));
        BigWinPanel.BeginAnimation(OpacityProperty, enter);
        await VisualDelay(1_000);

        var leave = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(Pace(250)));
        BigWinPanel.BeginAnimation(OpacityProperty, leave);
        await VisualDelay(270);

        BigWinPanel.Visibility = Visibility.Collapsed;
        BigWinPanel.BeginAnimation(OpacityProperty, null);
    }

    private async Task ShowFeature(string title, string subtitle, Brush color, int holdMilliseconds)
    {
        FeatureTitle.Text = title;
        FeatureSubtitle.Text = subtitle;
        FeatureTitle.Foreground = color;
        FeatureOverlay.Opacity = 0;
        FeatureOverlay.Visibility = Visibility.Visible;

        var enter = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Pace(170)));
        FeatureOverlay.BeginAnimation(OpacityProperty, enter);
        await VisualDelay(holdMilliseconds);

        var leave = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(Pace(220)));
        FeatureOverlay.BeginAnimation(OpacityProperty, leave);
        await VisualDelay(240);

        FeatureOverlay.Visibility = Visibility.Collapsed;
        FeatureOverlay.BeginAnimation(OpacityProperty, null);
    }

    private void BeginAnticipation(NeonVaultNearMiss nearMiss)
    {
        AnticipationText.Text = nearMiss switch
        {
            NeonVaultNearMiss.Scatter => "2 SCATTERS · FINAL REEL SEARCHING…",
            NeonVaultNearMiss.Jackpot => "ACCESS KEYS LOCKED · FINAL REEL SEARCHING…",
            NeonVaultNearMiss.Blackout => "SIGNAL INSTABILITY · POWER FALLING…",
            _ => "FINAL REEL SEARCHING…",
        };
        SystemStatusText.Text = "ANTICIPATION MODE";
        FlashReel(NeonVaultRules.Reels - 1, 7);
    }

    private void SetGrid(IReadOnlyList<NeonVaultSymbol> grid)
    {
        displayGrid = grid.ToArray();
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
            {
                SetCell(row, reel, displayGrid[NeonVaultRules.Index(row, reel)], false);
            }
        }
    }

    private void SetReel(int reel, IReadOnlyList<NeonVaultSymbol> grid)
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            NeonVaultSymbol symbol = grid[NeonVaultRules.Index(row, reel)];
            SetCell(row, reel, symbol, false);
        }
    }

    private void ApplyFullReelWild(int reel)
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            int index = NeonVaultRules.Index(row, reel);
            if (!CanBecomeWild(displayGrid[index])) continue;
            displayGrid[index] = NeonVaultSymbol.Wild;
            SetCell(row, reel, NeonVaultSymbol.Wild, true);
        }
    }

    private static bool CanBecomeWild(NeonVaultSymbol symbol) =>
        symbol is not NeonVaultSymbol.Scatter and not NeonVaultSymbol.VaultKey;

    private void SetCell(int row, int reel, NeonVaultSymbol symbol, bool flash)
    {
        ApplySymbol(presenters[row, reel], symbol, compact: false);
        if (flash && AnimationsEnabled)
        {
            Border frame = cells[row, reel];
            var pulse = new DoubleAnimation(0.42, 1, TimeSpan.FromMilliseconds(Pace(150)))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                FillBehavior = FillBehavior.Stop,
            };
            frame.BeginAnimation(OpacityProperty, pulse);
        }
    }

    private void FlashReel(int reel, int repeats)
    {
        if (!AnimationsEnabled || reel < 0 || reel >= reelFrames.Length) return;

        var pulse = new DoubleAnimation(1, 0.56, TimeSpan.FromMilliseconds(Pace(110)))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(repeats),
            FillBehavior = FillBehavior.Stop,
        };
        reelFrames[reel].BeginAnimation(OpacityProperty, pulse);
    }

    private void StartAmbientMotion()
    {
        if (!AnimationsEnabled) return;

        AmbientGlow.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0.15, 0.27, TimeSpan.FromSeconds(4.6))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            });

        ScanlineTransform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(0, 610, TimeSpan.FromSeconds(8.2))
            {
                RepeatBehavior = RepeatBehavior.Forever,
            });
    }

    private void RefreshUi()
    {
        BalanceText.Text = Money(machine.Bankroll);
        BetText.Text = Money(selectedBet);
        MiniJackpotText.Text = Money(selectedBet * NeonVaultRules.JackpotMultiplier(NeonVaultJackpot.Mini));
        MinorJackpotText.Text = Money(selectedBet * NeonVaultRules.JackpotMultiplier(NeonVaultJackpot.Minor));
        MajorJackpotText.Text = Money(selectedBet * NeonVaultRules.JackpotMultiplier(NeonVaultJackpot.Major));
        GrandJackpotText.Text = Money(selectedBet * NeonVaultRules.JackpotMultiplier(NeonVaultJackpot.Grand));
        EnergyBar.Value = machine.VaultEnergy;
        EnergyText.Text = $"{machine.VaultEnergy} / {NeonVaultRules.VaultEnergyTarget}";
        FreeSpinsText.Text = machine.FreeSpinsRemaining > 0 ? $"{machine.FreeSpinsRemaining} FREE SPINS" : "NO FREE SPINS";
        ProfileText.Text = $"P{progression.Profile.Prestige} · LV {progression.Profile.Level} · ◆ {progression.Profile.Coins:N0}";

        NeonVaultBetTier tier = NeonVaultRules.BetTier(selectedBet);
        (string tierText, Brush tierTextBrush, Brush tierBorderBrush) = tier switch
        {
            NeonVaultBetTier.MaxExposure => ("MAX VAULT EXPOSURE", Ink("#FF718D"), Ink("#FF526F")),
            NeonVaultBetTier.VaultClass => ("VAULT CLASS", Ink("#FFE18A"), Ink("#C49345")),
            NeonVaultBetTier.HighRoller => ("HIGH ROLLER", Ink("#9DEEFF"), Ink("#4CCDE4")),
            _ => ("STANDARD ACCESS", Ink("#C4CBE2"), Ink("#29304C")),
        };
        HighRollText.Text = tierText;
        HighRollText.Foreground = tierTextBrush;
        HighRollPanel.BorderBrush = tierBorderBrush;

        bool bonus = machine.BonusActive;
        BetDownButton.IsEnabled = !spinning && !bonus && BetIndex(selectedBet) > 0;
        BetUpButton.IsEnabled = !spinning && !bonus && BetIndex(selectedBet) < NeonVaultRules.Bets.Count - 1;
        MaxBetButton.IsEnabled = !spinning && !bonus && selectedBet != NeonVaultRules.MaximumBet;
        SpinButton.IsEnabled = !spinning && !bonus && selectedBet <= machine.Bankroll;
        SpinButton.Content = bonus ? "VAULT BREAK ACTIVE" : spinning ? "RESOLVING…" : "SPIN  [SPACE]";
        BackButton.IsEnabled = !spinning && !bonus;
        AnimationToggle.IsEnabled = !spinning;

        if (!spinning && selectedBet > machine.Bankroll && !bonus)
        {
            RewardText.Text = "Selected exposure exceeds the current play-money bankroll.";
        }
    }

    private void BetDown_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        int index = BetIndex(selectedBet);
        selectedBet = NeonVaultRules.Bets[Math.Max(0, index - 1)];
        RefreshUi();
    }

    private void BetUp_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        int index = BetIndex(selectedBet);
        selectedBet = NeonVaultRules.Bets[Math.Min(NeonVaultRules.Bets.Count - 1, index + 1)];
        RefreshUi();
    }

    private void MaxBet_Click(object sender, RoutedEventArgs e)
    {
        if (spinning || machine.BonusActive) return;
        selectedBet = NeonVaultRules.MaximumBet;
        RefreshUi();
    }

    private void Settings_Changed(object sender, RoutedEventArgs e)
    {
        if (!initialized) return;

        MenuAudio.Enabled = SoundsEnabled;
        if (!SoundsEnabled) audio.Stop();

        if (AnimationsEnabled)
        {
            StartAmbientMotion();
        }
        else
        {
            AmbientGlow.BeginAnimation(OpacityProperty, null);
            AmbientGlow.Opacity = 0.20;
            ScanlineTransform.BeginAnimation(TranslateTransform.YProperty, null);
            ScanlineTransform.Y = 0;
        }
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat && e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && !spinning && !machine.BonusActive)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Space && Keyboard.FocusedElement is not Button && Keyboard.FocusedElement is not CheckBox)
        {
            if (!e.IsRepeat) await SpinAsync();
            e.Handled = true;
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (!spinning && !machine.BonusActive) Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        closed = true;
        previewTimer.Stop();
        previewTimer.Tick -= PreviewTimer_Tick;
        AmbientGlow.BeginAnimation(OpacityProperty, null);
        ScanlineTransform.BeginAnimation(TranslateTransform.YProperty, null);

        for (int reel = 0; reel < reelFrames.Length; reel++)
        {
            reelFrames[reel].BeginAnimation(OpacityProperty, null);
            StopReelMotion(reel);
        }

        audio.Dispose();
        music.Dispose();
    }

    private static string FeatureSummary(NeonVaultSpinResult result)
    {
        var parts = new List<string>();
        if (result.Blackout != NeonVaultBlackout.None) parts.Add(result.Blackout.ToString().ToUpperInvariant());
        if (result.ExpandedReels.Count > 0) parts.Add($"{result.ExpandedReels.Count} expanded reel{(result.ExpandedReels.Count == 1 ? string.Empty : "s")}");
        if (result.Jackpot != NeonVaultJackpot.None) parts.Add($"{result.Jackpot} jackpot");
        if (result.FreeSpinsAwarded > 0) parts.Add($"+{result.FreeSpinsAwarded} Free Spins");
        if (result.SecurityOverrideReel is not null) parts.Add("Security Override");
        if (parts.Count == 0 && result.NearMiss != NeonVaultNearMiss.None) parts.Add($"{result.NearMiss} near miss");
        return parts.Count == 0 ? "No feature event." : string.Join(" · ", parts);
    }

    private Task VisualDelay(int milliseconds) =>
        Task.Delay(AnimationsEnabled ? Pace(milliseconds) : 0);

    private static int Pace(int milliseconds) =>
        (int)Math.Round(milliseconds * PresentationPace, MidpointRounding.AwayFromZero);

    private static decimal ClosestBet(decimal bet) =>
        NeonVaultRules.Bets.OrderBy(value => Math.Abs(value - bet)).First();

    private static int BetIndex(decimal bet)
    {
        for (int i = 0; i < NeonVaultRules.Bets.Count; i++)
        {
            if (NeonVaultRules.Bets[i] == bet) return i;
        }
        return 0;
    }

    private static SolidColorBrush Ink(string hex) => new(InkColor(hex));
    private static Color InkColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
}
