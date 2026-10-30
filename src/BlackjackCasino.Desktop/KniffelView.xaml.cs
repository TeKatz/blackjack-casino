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

    internal KniffelView(KniffelState state, Action<KniffelState> save, Action back, Func<bool> animations)
    {
        this.save = save;
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
            holdLabels[i] = Label("WÜRFELN", 10, "#B6C7AB");
            holdLabels[i].TextAlignment = TextAlignment.Center;
            holdLabels[i].Margin = new Thickness(0, 12, 0, 0);
            stack.Children.Add(diceViews[i]);
            stack.Children.Add(holdLabels[i]);
            var button = new Button
            {
                Name = "KniffelDie" + i, Content = stack, Padding = new Thickness(0), Margin = new Thickness(2),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                ToolTip = $"Würfel {i + 1} behalten / freigeben (Taste {i + 1})",
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
        NoticeText.Text = "Würfle zuerst. Danach kannst du Würfel behalten und erneut würfeln oder bereits einen Wert eintragen. Deine Partie wird automatisch gespeichert.";
        Refresh();
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
        busy = animations();
        Refresh();
        if (busy)
        {
            NoticeText.Text = "Die Würfel rollen …";
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
            ? "Weiterer Kniffel! Beim Eintragen erhältst du +50 Zusatzpunkte und kannst die Höchstpunktzahl eines freien Felds wählen."
            : Game.RollsUsed == 3
                ? "Dritter Wurf: Wähle jetzt ein freies Feld auf deinem Block und bestätige den Eintrag."
                : "Wähle Würfel zum Behalten oder ein Feld auf dem Block. Du darfst auch schon vor dem dritten Wurf eintragen.";
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
            ? $"{CategoryName(category)}: {points} Punkte. Mit „Eintragen“ bestätigen oder ein anderes Feld wählen."
            : $"{CategoryName(category)} bringt mit diesem Wurf 0 Punkte. Bestätige mit „Streichen“ oder wähle ein anderes Feld.";
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
            ? $"Partie abgeschlossen! {Game.Total} Punkte insgesamt. Oben: {Game.UpperSubtotal} + {Game.UpperBonus} Bonus · Unten: {Game.LowerSubtotal} · Zusatz-Kniffel: {Game.AdditionalKniffelBonus}."
            : $"{CategoryName(category)}: {points} Punkte eingetragen. Weiter mit Runde {Game.FilledCount + 1}.";
        Pulse(rows[category].Button, Color.FromRgb(189, 142, 52));
        RollButton.Focus();
    }

    private void Refresh()
    {
        TurnText.Text = Game.IsComplete ? "Partie abgeschlossen" : $"Runde {Game.FilledCount + 1} von 13";
        TotalText.Text = Game.Total.ToString(CultureInfo.InvariantCulture);
        RollText.Text = Game.IsComplete ? "Alle 13 Felder sind ausgefüllt." : $"Wurf {Game.RollsUsed} / 3";
        RollButton.IsEnabled = !busy && Game.CanRoll;
        RollButton.Content = busy ? "Würfel rollen …" : Game.IsComplete ? "Partie beendet" : Game.RollsUsed == 0 ? "Würfeln" : "Erneut würfeln";
        if (!busy)
        {
            for (int i = 0; i < 5; i++)
            {
                diceViews[i].Value = Game.Dice[i];
                diceViews[i].Held = Game.Held[i];
                holdLabels[i].Text = Game.Held[i] ? "BEHALTEN" : Game.RollsUsed == 3 ? "ERGEBNIS" : "WÜRFELN";
                holdLabels[i].Foreground = Ink(Game.Held[i] ? "#A2EFCD" : "#B6C7AB");
            }
        }
        for (int i = 0; i < 5; i++)
        {
            diceButtons[i].IsEnabled = !busy && Game.CanScore && Game.RollsUsed < 3;
            AutomationProperties.SetName(diceButtons[i], $"Würfel {i + 1}: {Game.Dice[i]}, {(Game.Held[i] ? "behalten" : "nicht behalten")}");
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
            row.Button.ToolTip = stored.HasValue ? $"{CategoryName(category)}: {stored} Punkte eingetragen"
                : available ? $"{CategoryName(category)}: {preview} Punkte möglich. Anklicken zum Auswählen."
                : "Würfle zuerst, um Punkte einzutragen.";
            AutomationProperties.SetName(row.Button, $"{CategoryName(category)}, {row.Score.Text}");
            if (available && preview > 0) Pulse(row.Button, Color.FromRgb(39, 165, 122), chosen ? .7 : .35);
        }
        CommitButton.IsEnabled = !busy && selected.HasValue && Game.CanScore && Game.Preview(selected.Value) > 0;
        ScratchButton.IsEnabled = !busy && selected.HasValue && Game.CanScore;
        CommitButton.Content = CommitButton.IsEnabled ? $"{Game.Preview(selected!.Value)} Punkte eintragen" : "Feld auswählen";
        UpperText.Text = $"Oben: {Game.UpperSubtotal} Punkte · Bonus: {Game.UpperBonus} / 35 (ab 63)";
        BonusText.Text = $"Unten: {Game.LowerSubtotal} Punkte · Bis zum oberen Bonus: {Math.Max(0, 63 - Game.UpperSubtotal)}";
        AdditionalText.Text = $"Zusatz-Kniffel: +{Game.AdditionalKniffelBonus} · Gesamt: {Game.Total}";
        CombinationText.Text = busy ? "" : !Game.CanScore ? Game.IsComplete ? "Dein Block ist vollständig." : "Fünf Würfel. Deine Entscheidung."
            : Game.IsAdditionalKniffel ? "KNIFFEL · +50 Bonus & freie Höchstwertung"
            : CombinationDescription();
    }

    private string CombinationDescription()
    {
        KniffelCategory[] special = [KniffelCategory.Kniffel, KniffelCategory.LargeStraight, KniffelCategory.SmallStraight,
            KniffelCategory.FullHouse, KniffelCategory.FourOfAKind, KniffelCategory.ThreeOfAKind];
        string[] found = special.Where(category => KniffelScoring.Evaluate(category, Game.Dice) > 0)
            .Select(CategoryName).ToArray();
        return found.Length > 0 ? string.Join(" · ", found) : "Augen sammeln oder auf eine Kombination spielen";
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
        NoticeText.Text = "Neue Partie. Viel Erfolg beim Würfeln!";
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
        KniffelCategory.Ones => "Einsen", KniffelCategory.Twos => "Zweien", KniffelCategory.Threes => "Dreien",
        KniffelCategory.Fours => "Vieren", KniffelCategory.Fives => "Fünfen", KniffelCategory.Sixes => "Sechsen",
        KniffelCategory.ThreeOfAKind => "Dreierpasch", KniffelCategory.FourOfAKind => "Viererpasch",
        KniffelCategory.FullHouse => "Full House", KniffelCategory.SmallStraight => "Kleine Straße",
        KniffelCategory.LargeStraight => "Große Straße", KniffelCategory.Kniffel => "Kniffel", _ => "Chance",
    };
    private static TextBlock Label(string text, double size, string color) => new() { Text = text, FontSize = size, Foreground = Ink(color), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
