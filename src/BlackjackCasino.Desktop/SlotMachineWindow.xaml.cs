using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class SlotMachineWindow : Window
{
    private const double CellHeight = 92;
    private readonly SlotMachineSession machine;
    private readonly PlayerProgressionController progression;
    private readonly Func<SlotSpinResult, ProgressionAward> commitSpin;
    private readonly SlotAudio audio = new();
    private readonly BackgroundMusic music = new(Soundtrack.Reels);
    private readonly DispatcherTimer reelTicker = new() { Interval = TimeSpan.FromMilliseconds(75) };
    private readonly List<(StackPanel Strip, TranslateTransform Movement)> reels = [];
    private readonly int[] previousStops = [4, 4, 4];
    private readonly int[] lastTickRows = new int[3];
    private DateTimeOffset nextTickAt;
    private int stoppedReels;
    private bool spinning;
    private bool closed;
    private bool initialized;
    private decimal selectedBet;

    public SlotMachineWindow(decimal bankroll, decimal bet, bool sounds, bool animations,
        PlayerProgressionController progression, Func<SlotSpinResult, ProgressionAward> commitSpin,
        bool musicEnabled = true, double musicVolume = 0.10)
    {
        InitializeComponent();
        machine = new SlotMachineSession(bankroll);
        this.progression = progression;
        this.commitSpin = commitSpin;
        selectedBet = Math.Clamp(bet, SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
        if (bankroll >= SlotMachineRules.MinimumBet)
        {
            selectedBet = Math.Min(selectedBet, decimal.Floor(bankroll * 100) / 100);
        }
        SoundToggle.IsChecked = sounds;
        AnimationToggle.IsChecked = animations;
        for (int i = 0; i < 3; i++)
        {
            var movement = new TranslateTransform();
            var strip = new StackPanel { RenderTransform = movement };
            var canvas = new Canvas { Width = 174, Height = 3 * CellHeight, ClipToBounds = true };
            canvas.Children.Add(strip);
            var frame = new Border
            {
                Background = (Brush)FindResource("SlotReelPaper"),
                Margin = new Thickness(4, 0, 4, 0),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Child = canvas,
            };
            ReelsHost.Children.Add(frame);
            reels.Add((strip, movement));
            DrawStrip(i, previousStops[i], 0);
        }
        foreach (SlotSymbolDefinition symbol in SlotMachineRules.Symbols.Reverse())
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(SymbolImage(symbol.Symbol, 38));
            var name = new TextBlock { Text = symbol.Name + " × 3", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(7, 0, 0, 0) };
            Grid.SetColumn(name, 1);
            row.Children.Add(name);
            var payout = new TextBlock { Text = $"{symbol.TripleMultiplier}×", FontWeight = FontWeights.Bold, Foreground = Brushes.Wheat, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(payout, 2);
            row.Children.Add(payout);
            PaytableHost.Children.Add(row);
        }
        reelTicker.Tick += ReelTicker_Tick;
        initialized = true;
        RefreshUi();
        MenuAudio.Attach(this, () => SoundsEnabled);
        Loaded += (_, _) => music.Start(musicEnabled, musicVolume);
    }

    public decimal SelectedBet => selectedBet;
    public bool SoundsEnabled => SoundToggle.IsChecked == true;
    public bool AnimationsEnabled => AnimationToggle.IsChecked == true;

    private Image SymbolImage(SlotSymbol symbol, double size) => new()
    {
        Source = (ImageSource)FindResource("Slot" + symbol),
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        ToolTip = symbol.ToString(),
    };

    private void DrawStrip(int reel, int start, int travel)
    {
        StackPanel strip = reels[reel].Strip;
        strip.Children.Clear();
        for (int step = -1; step <= travel + 1; step++)
        {
            int stop = (start + step + SlotMachineRules.ReelStops) % SlotMachineRules.ReelStops;
            var cell = new Grid { Width = 174, Height = CellHeight };
            cell.Children.Add(SymbolImage(SlotMachineRules.SymbolAt(stop), 78));
            strip.Children.Add(cell);
        }
    }

    private void Spin_Click(object sender, RoutedEventArgs e) => Spin();

    private void Spin()
    {
        if (spinning || closed || machine.Bankroll < selectedBet)
        {
            return;
        }

        spinning = true;
        stoppedReels = 0;
        ReelFrame.BeginAnimation(OpacityProperty, null);
        RefreshUi();
        SlotSpinResult result;
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

        // The host synchronizes the existing bankroll and profile before any cosmetic work.
        ProgressionAward award = commitSpin(result);
        ResultText.Text = "Reels rolling…";
        RewardText.Text = "One centre payline. Let it roll.";
        BalanceText.Text = Money(result.BankrollBefore - result.Bet);
        audio.Play(SlotSound.Start, SoundsEnabled);

        if (!AnimationsEnabled)
        {
            for (int i = 0; i < 3; i++)
            {
                previousStops[i] = result.Stops[i];
                DrawStrip(i, previousStops[i], 0);
            }
            FinishSpin(result, award);
            return;
        }

        Array.Clear(lastTickRows);
        nextTickAt = DateTimeOffset.UtcNow.AddMilliseconds(140);
        reelTicker.Start();
        for (int i = 0; i < 3; i++)
        {
            int reel = i;
            int target = result.Stops[i];
            int travel = (2 + i) * SlotMachineRules.ReelStops
                + (target - previousStops[i] + SlotMachineRules.ReelStops) % SlotMachineRules.ReelStops;
            DrawStrip(i, previousStops[i], travel);
            var animation = new DoubleAnimation(0, -travel * CellHeight, TimeSpan.FromMilliseconds(2100 + i * 520))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            animation.Completed += (_, _) =>
            {
                if (closed)
                {
                    return;
                }
                reels[reel].Movement.BeginAnimation(TranslateTransform.YProperty, null);
                reels[reel].Movement.Y = 0;
                previousStops[reel] = target;
                DrawStrip(reel, target, 0);
                audio.Play(SlotSound.Stop, SoundsEnabled);
                nextTickAt = DateTimeOffset.UtcNow.AddMilliseconds(165);
                stoppedReels++;
                if (stoppedReels == 3)
                {
                    FinishSpin(result, award);
                }
            };
            reels[i].Movement.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    private void FinishSpin(SlotSpinResult result, ProgressionAward award)
    {
        reelTicker.Stop();
        spinning = false;
        ResultText.Text = result.Multiplier switch
        {
            100 => $"MIDNIGHT JACKPOT · {Money(result.Payout)}",
            > 1 => $"{result.Multiplier}× · {Money(result.Payout)} returned!",
            1 => "Two cherries · stake returned",
            _ => "No match this time.",
        };
        string level = award.HasLevelUp ? $" · Level {progression.Profile.Level}!" : string.Empty;
        RewardText.Text = $"+{award.PlayerExperience} XP · +{award.SeasonExperience} Season XP{level}";
        audio.Play(result.Multiplier == 100 ? SlotSound.Jackpot : result.Net > 0 ? SlotSound.Win : result.Net == 0 ? SlotSound.Push : SlotSound.Loss, SoundsEnabled);
        if (AnimationsEnabled && result.Net > 0)
        {
            var pulse = new DoubleAnimation(1, 0.48, TimeSpan.FromMilliseconds(220))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(result.Multiplier == 100 ? 6 : 3),
                FillBehavior = FillBehavior.Stop,
            };
            ReelFrame.BeginAnimation(OpacityProperty, pulse);
        }
        RefreshUi();
    }

    private void RefreshUi()
    {
        BalanceText.Text = Money(machine.Bankroll);
        BetText.Text = Money(selectedBet);
        BetControls.IsEnabled = !spinning;
        SpinButton.IsEnabled = !spinning && selectedBet <= machine.Bankroll;
        SpinButton.Content = spinning ? "ROLLING…" : "SPIN  [SPACE]";
        AnimationToggle.IsEnabled = !spinning;
        PlayerProfileState profile = progression.Profile;
        ProfileText.Text = $"PRESTIGE {profile.Prestige} · LEVEL {profile.Level} · ◆ {profile.Coins:N0}";
        SlotStatistics stats = profile.Slots;
        StatsText.Text = $"{stats.Spins:N0} spins · {stats.Wins:N0} wins · {stats.Pushes:N0} pushes\nSlot net: {Money(stats.NetProfit)} · Best return: {Money(stats.BiggestPayout)}";
        if (!spinning && machine.Bankroll < SlotMachineRules.MinimumBet)
        {
            RewardText.Text = "Out of play money. Reset your bankroll at the Blackjack table.";
        }
        else if (!spinning && selectedBet > machine.Bankroll)
        {
            RewardText.Text = "Choose a smaller bet to keep playing.";
        }
        foreach (Button button in BetControls.Children.OfType<Button>())
        {
            decimal amount = decimal.Parse((string)button.Tag, CultureInfo.InvariantCulture);
            button.IsEnabled = amount <= machine.Bankroll;
            button.BorderBrush = amount == selectedBet ? Brushes.Wheat : new SolidColorBrush(Color.FromRgb(102, 81, 109));
            button.ToolTip = $"Set bet to {Money(amount)}";
        }
    }

    private void Bet_Click(object sender, RoutedEventArgs e)
    {
        if (!spinning && sender is Button { Tag: string value })
        {
            selectedBet = decimal.Parse(value, CultureInfo.InvariantCulture);
            RefreshUi();
        }
    }

    private void ReelTicker_Tick(object? sender, EventArgs e)
    {
        bool crossedSymbol = false;
        for (int i = 0; i < reels.Count; i++)
        {
            int row = (int)(-reels[i].Movement.Y / CellHeight);
            crossedSymbol |= row != lastTickRows[i];
            lastTickRows[i] = row;
        }
        if (crossedSymbol && DateTimeOffset.UtcNow >= nextTickAt)
        {
            audio.Play(SlotSound.Tick, SoundsEnabled);
        }
    }

    private void Settings_Changed(object sender, RoutedEventArgs e)
    {
        if (initialized) MenuAudio.Enabled = SoundsEnabled;
        if (initialized && !SoundsEnabled)
        {
            audio.Stop();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat && e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Space && Keyboard.FocusedElement is not Button && Keyboard.FocusedElement is not CheckBox)
        {
            if (!e.IsRepeat)
            {
                Spin();
            }
            e.Handled = true;
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closed(object? sender, EventArgs e)
    {
        closed = true;
        reelTicker.Stop();
        reelTicker.Tick -= ReelTicker_Tick;
        foreach ((StackPanel _, TranslateTransform movement) in reels)
        {
            movement.BeginAnimation(TranslateTransform.YProperty, null);
        }
        ReelFrame.BeginAnimation(OpacityProperty, null);
        audio.Dispose();
        music.Dispose();
    }

    private static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
}
