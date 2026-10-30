using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

public partial class KniffelView : UserControl, IDisposable
{
    private readonly Action<KniffelState> save;
    private readonly Action back;
    private readonly Action? rollSound;
    private readonly Func<KniffelState, string>? completion;
    private readonly Func<bool> animations;
    private readonly Button[] diceButtons = new Button[5];
    private readonly KniffelDie[] diceViews = new KniffelDie[5];
    private readonly TextBlock[] holdLabels = new TextBlock[5];
    private readonly RotateTransform[] rotations = new RotateTransform[5];
    private readonly TranslateTransform[] translations = new TranslateTransform[5];
    private readonly Dictionary<KniffelCategory, (Button Button, TextBlock Score)> rows = [];
    private readonly DispatcherTimer rollTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private readonly Stopwatch rollClock = new();
    private bool[] rolling = new bool[5];
    private bool busy;
    private bool disposed;
    private KniffelCategory? selected;
    internal KniffelGame Game { get; private set; }

    internal KniffelView(KniffelState state, Action<KniffelState> save, Action back, Func<bool> animations, Action? rollSound = null, Func<KniffelState, string>? completion = null)
    {
        this.save = save;
        this.rollSound = rollSound;
        this.completion = completion;
        this.back = back;
        this.animations = animations;
        Game = KniffelGame.Restore(KniffelGame.Normalize(state));
        InitializeComponent();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            var stack = new StackPanel();
            rotations[i] = new RotateTransform();
            translations[i] = new TranslateTransform();
            var transform = new TransformGroup();
            transform.Children.Add(rotations[i]);
            transform.Children.Add(translations[i]);
            diceViews[i] = new KniffelDie { Width = 94, Height = 100, RenderTransformOrigin = new Point(.5, .5), RenderTransform = transform };
            holdLabels[i] = Label("ROLL", 10, "#B6C7AB");
            holdLabels[i].TextAlignment = TextAlignment.Center;
            holdLabels[i].Margin = new Thickness(0, 12, 0, 0);
            stack.Children.Add(diceViews[i]);
            stack.Children.Add(holdLabels[i]);
            var button = new Button
            {
                Name = "KniffelDie" + i, Content = stack, Padding = new Thickness(0), Margin = new Thickness(2),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                ToolTip = $"Hold / release die {i + 1} (key {i + 1})",
            };
            button.Click += (_, _) => ToggleHold(index);
            diceButtons[i] = button;
            DicePanel.Children.Add(button);
        }
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            var title = Label(CategoryName(category), 13, "#2C433B");
            var score = Label("—", 15, "#2C433B");
            score.FontWeight = FontWeights.Bold;
            score.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(score, 1);
            content.Children.Add(title);
            content.Children.Add(score);
            var row = new Button
            {
                Name = "Score_" + category, Content = content, Height = 28,
                Margin = new Thickness(0, category == KniffelCategory.ThreeOfAKind ? 7 : 1, 0, 1),
                Style = (Style)Resources["PaperButton"],
            };
            row.Click += (_, _) => Select(category);
            rows.Add(category, (row, score));
            ScoreRows.Children.Add(row);
        }
        rollTimer.Tick += (_, _) => AnimateRollFrame();
        PreviewKeyDown += OnKeyDown;
        Loaded += (_, _) => { Refresh(); RollButton.Focus(); };
        Unloaded += (_, _) => StopRoll();
        NoticeText.Text = "Roll first, then hold dice and reroll or choose a score. Your game saves automatically.";
        Refresh();
        // Persist migrated IDs before awarding an older completed save.
        save(Game.Snapshot);
        if (Game.IsComplete) NoticeText.Text = $"Game complete! {Game.Total} points. " + completion?.Invoke(Game.Snapshot);
    }

    private void Roll_Click(object sender, RoutedEventArgs e)
    {
        if (disposed || busy || !Game.CanRoll || RestartOverlay.IsVisible) return;
        selected = null;
        for (int i = 0; i < 5; i++) rolling[i] = !Game.Held[i];
        Game.Roll();
        // Save the real result before showing cosmetic rolling faces. Leaving mid-animation
        // therefore resumes exactly this roll and never grants a free reroll.
        save(Game.Snapshot);
        rollSound?.Invoke();
        busy = animations();
        Refresh();
        if (busy)
        {
            NoticeText.Text = "The dice are rolling …";
            rollClock.Restart();
            rollTimer.Start();
        }
        else ShowRollNotice();
    }

    private void AnimateRollFrame()
    {
        if (disposed || !IsVisible || !animations()) { StopRoll(); return; }
        double elapsed = rollClock.Elapsed.TotalMilliseconds;
        for (int i = 0; i < 5; i++)
        {
            if (!rolling[i]) continue;
            double duration = 580 + i * 65;
            if (elapsed < duration)
            {
                double strength = 1 - elapsed / duration;
                diceViews[i].Value = Random.Shared.Next(1, 7);
                rotations[i].Angle = Math.Sin(elapsed / 40 + i) * 24 * strength;
                translations[i].Y = -Math.Abs(Math.Sin(elapsed / 63 + i)) * 35 * strength;
                translations[i].X = Math.Sin(elapsed / 55 + i) * 10 * strength;
            }
            else ResetDie(i);
        }
        if (elapsed >= 840) StopRoll();
    }

    private void StopRoll()
    {
        rollTimer.Stop();
        rollClock.Stop();
        bool wasBusy = busy;
        busy = false;
        for (int i = 0; i < 5; i++) ResetDie(i);
        if (disposed) return;
        Refresh();
        if (wasBusy) ShowRollNotice();
    }

    private void ResetDie(int i)
    {
        diceViews[i].Value = Game.Dice[i];
        rotations[i].Angle = 0;
        translations[i].X = 0;
        translations[i].Y = 0;
    }

    private void ShowRollNotice()
    {
        NoticeText.Text = Game.IsAdditionalKniffel
            ? "Another five of a kind! Scoring grants +50 bonus points and the maximum in any free box."
            : Game.RollsUsed == 3
                ? "Third roll: choose a free box on your sheet and confirm your score."
                : "Choose dice to hold or a box on the sheet. You can score before the third roll.";
    }

    private void ToggleHold(int index)
    {
        if (disposed || busy || !Game.CanScore || Game.RollsUsed == 3 || RestartOverlay.IsVisible) return;
        Game.ToggleHold(index);
        save(Game.Snapshot);
        Refresh();
    }

    private void Select(KniffelCategory category)
    {
        if (disposed || busy || !Game.CanScore || Game.ScoreFor(category).HasValue || RestartOverlay.IsVisible) return;
        selected = category;
        int points = Game.Preview(category);
        NoticeText.Text = points > 0
            ? $"{CategoryName(category)}: {points} points. Confirm with “Record score” or choose another box."
            : $"{CategoryName(category)} scores 0 with this roll. Confirm with “Scratch” or choose another box.";
        Refresh();
    }

    private void Commit_Click(object sender, RoutedEventArgs e) => Commit(false);
    private void Scratch_Click(object sender, RoutedEventArgs e) => Commit(true);
    private void Commit(bool scratch)
    {
        if (disposed || busy || selected is not KniffelCategory category || !Game.CanScore || RestartOverlay.IsVisible) return;
        int points = Game.Commit(category, scratch);
        selected = null;
        save(Game.Snapshot);
        Refresh();
        NoticeText.Text = Game.IsComplete
            ? $"Game complete! {Game.Total} points. Upper: {Game.UpperSubtotal} + {Game.UpperBonus} bonus · Lower: {Game.LowerSubtotal} · Extra five-of-a-kind: {Game.AdditionalKniffelBonus}."
            : $"{CategoryName(category)}: {points} points recorded. Next: turn {Game.FilledCount + 1}.";
        if (Game.IsComplete) NoticeText.Text += " " + completion?.Invoke(Game.Snapshot);
        Pulse(rows[category].Button, Color.FromRgb(189, 142, 52));
        RollButton.Focus();
    }

    private void Refresh()
    {
        TurnText.Text = Game.IsComplete ? "Game complete" : $"Turn {Game.FilledCount + 1} of 13";
        TotalText.Text = Game.Total.ToString(CultureInfo.InvariantCulture);
        RollText.Text = Game.IsComplete ? "All 13 boxes are filled." : $"Roll {Game.RollsUsed} / 3";
        RollButton.IsEnabled = !busy && Game.CanRoll;
        RollButton.Content = busy ? "Rolling dice …" : Game.IsComplete ? "Game finished" : Game.RollsUsed == 0 ? "Roll dice" : "Roll again";
        if (!busy)
        {
            for (int i = 0; i < 5; i++)
            {
                diceViews[i].Value = Game.Dice[i];
                diceViews[i].Held = Game.Held[i];
                holdLabels[i].Text = Game.Held[i] ? "HELD" : Game.RollsUsed == 3 ? "RESULT" : "ROLL";
                holdLabels[i].Foreground = Ink(Game.Held[i] ? "#A2EFCD" : "#B6C7AB");
            }
        }
        for (int i = 0; i < 5; i++)
        {
            diceButtons[i].IsEnabled = !busy && Game.CanScore && Game.RollsUsed < 3;
            AutomationProperties.SetName(diceButtons[i], $"Die {i + 1}: {Game.Dice[i]}, {(Game.Held[i] ? "held" : "not held")}");
        }
        foreach (var (category, row) in rows)
        {
            row.Button.Effect = null;
            int? stored = Game.ScoreFor(category);
            bool available = !busy && Game.CanScore && !stored.HasValue;
            int preview = available ? Game.Preview(category) : 0;
            bool chosen = available && selected == category;
            row.Button.IsEnabled = available;
            row.Score.Text = stored.HasValue ? stored.Value.ToString(CultureInfo.InvariantCulture)
                : available ? preview == 0 ? "0  ×" : "+" + preview : "—";
            row.Score.Foreground = Ink(stored.HasValue ? "#263F3B" : preview > 0 ? "#08775F" : "#958874");
            row.Button.Background = Ink(chosen ? "#BFE4CF" : stored.HasValue ? "#E1D8C5" : available && preview > 0 ? "#DFEEDC" : "#F3ECDD");
            row.Button.BorderBrush = Ink(chosen ? "#16896E" : available && preview > 0 ? "#77AE90" : "#D8CEB9");
            row.Button.ToolTip = stored.HasValue ? $"{CategoryName(category)}: {stored} points recorded"
                : available ? $"{CategoryName(category)}: {preview} possible points. Click to select."
                : "Roll first to record a score.";
            AutomationProperties.SetName(row.Button, $"{CategoryName(category)}, {row.Score.Text}");
            if (available && preview > 0) Pulse(row.Button, Color.FromRgb(39, 165, 122), chosen ? .7 : .35);
        }
        CommitButton.IsEnabled = !busy && selected.HasValue && Game.CanScore && Game.Preview(selected.Value) > 0;
        ScratchButton.IsEnabled = !busy && selected.HasValue && Game.CanScore;
        CommitButton.Content = CommitButton.IsEnabled ? $"Record {Game.Preview(selected!.Value)} points" : "Choose a box";
        UpperText.Text = $"Upper: {Game.UpperSubtotal} points · Bonus: {Game.UpperBonus} / 35 (at 63)";
        BonusText.Text = $"Lower: {Game.LowerSubtotal} points · To upper bonus: {Math.Max(0, 63 - Game.UpperSubtotal)}";
        AdditionalText.Text = $"Extra five-of-a-kind: +{Game.AdditionalKniffelBonus} · Total: {Game.Total}";
        CombinationText.Text = busy ? "" : !Game.CanScore ? Game.IsComplete ? "Your score sheet is complete." : "Five dice. Your decision."
            : Game.IsAdditionalKniffel ? "FIVE OF A KIND · +50 bonus & any box maximum"
            : CombinationDescription();
    }

    private string CombinationDescription()
    {
        KniffelCategory[] special = [KniffelCategory.Kniffel, KniffelCategory.LargeStraight, KniffelCategory.SmallStraight,
            KniffelCategory.FullHouse, KniffelCategory.FourOfAKind, KniffelCategory.ThreeOfAKind];
        string[] found = special.Where(category => KniffelScoring.Evaluate(category, Game.Dice) > 0)
            .Select(CategoryName).ToArray();
        return found.Length > 0 ? string.Join(" · ", found) : "Collect points or aim for a combination";
    }

    private void Pulse(Button row, Color color, double opacity = .6)
    {
        var effect = new DropShadowEffect { Color = color, BlurRadius = 12, ShadowDepth = 0, Opacity = opacity };
        row.Effect = effect;
        if (animations() && IsVisible)
            effect.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(.15, opacity,
                TimeSpan.FromMilliseconds(550)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(2), FillBehavior = FillBehavior.Stop });
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        if (disposed) return;
        if (Game.FilledCount == 0 && Game.RollsUsed == 0 || Game.IsComplete) { Restart(); return; }
        StopRoll();
        RestartOverlay.Visibility = Visibility.Visible;
        DicePanel.IsEnabled = ScoreRows.IsEnabled = RollButton.IsEnabled = CommitButton.IsEnabled = ScratchButton.IsEnabled = false;
        NewGameButton.IsEnabled = KniffelBackButton.IsEnabled = false;
        KeyboardNavigation.SetTabNavigation(RestartOverlay, KeyboardNavigationMode.Cycle);
        CancelRestartButton.Focus();
    }
    private void ConfirmRestart_Click(object sender, RoutedEventArgs e) => Restart();
    private void CancelRestart_Click(object sender, RoutedEventArgs e)
    {
        RestartOverlay.Visibility = Visibility.Collapsed;
        DicePanel.IsEnabled = ScoreRows.IsEnabled = NewGameButton.IsEnabled = KniffelBackButton.IsEnabled = true;
        Refresh();
        RollButton.Focus();
    }
    private void Restart()
    {
        StopRoll();
        Game = new KniffelGame();
        selected = null;
        save(Game.Snapshot);
        CancelRestart_Click(this, new RoutedEventArgs());
        NoticeText.Text = "New game. Good luck with your rolls!";
    }
    private void Back_Click(object sender, RoutedEventArgs e) => back();
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (RestartOverlay.IsVisible) CancelRestart_Click(this, new RoutedEventArgs());
            else back();
            e.Handled = true;
        }
        if (RestartOverlay.IsVisible || e.IsRepeat) return;
        if (e.Key == Key.R) { Roll_Click(this, new RoutedEventArgs()); e.Handled = true; }
        int index = e.Key >= Key.D1 && e.Key <= Key.D5 ? e.Key - Key.D1
            : e.Key >= Key.NumPad1 && e.Key <= Key.NumPad5 ? e.Key - Key.NumPad1 : -1;
        if (index >= 0) { ToggleHold(index); e.Handled = true; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        StopRoll();
        foreach (var row in rows.Values) row.Button.Effect = null;
    }
    internal static string CategoryName(KniffelCategory category) => category switch
    {
        KniffelCategory.Ones => "Ones", KniffelCategory.Twos => "Twos", KniffelCategory.Threes => "Threes",
        KniffelCategory.Fours => "Fours", KniffelCategory.Fives => "Fives", KniffelCategory.Sixes => "Sixes",
        KniffelCategory.ThreeOfAKind => "Three of a kind", KniffelCategory.FourOfAKind => "Four of a kind",
        KniffelCategory.FullHouse => "Full House", KniffelCategory.SmallStraight => "Small straight",
        KniffelCategory.LargeStraight => "Large straight", KniffelCategory.Kniffel => "Five of a kind", _ => "Chance",
    };
    private static TextBlock Label(string text, double size, string color) => new() { Text = text, FontSize = size, Foreground = Ink(color), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
