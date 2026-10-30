using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

public partial class KniffelView
{
    private sealed record DiceCelebration(string Player, int Face, string Detail);
    private readonly Queue<DiceCelebration> celebrations = new();
    private readonly HashSet<(Guid Game, int Count)> celebratedScores = [];
    private readonly DispatcherTimer celebrationTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };
    private bool celebrationShowing;
    private bool resultPending;
    private bool resultPresented;
    private decimal tableBankroll;
    private string completionNotice = "";
    internal string CompletionNotice => completionNotice;
    internal void SetCompletionNotice(string value) => completionNotice = value;

    // Called only after the entry/checkpoint has been saved. Comparing counters also
    // catches the extra-five bonus when it is written into a different category.
    internal void CelebrateScore(string player, KniffelState before, KniffelState after)
    {
        if (disposed || before.GameId != after.GameId || after.KniffelsScored <= before.KniffelsScored
            || !celebratedScores.Add((after.GameId, after.KniffelsScored))) return;
        KniffelEntry? entry = after.Entries.FirstOrDefault(e => before.Entries.All(old => old.Category != e.Category));
        if (entry is null) return;
        int bonus = (Math.Max(0, after.KniffelsScored - 1) - Math.Max(0, before.KniffelsScored - 1)) * 50;
        string detail = $"{CategoryName(entry.Category)} · {entry.Points} points" + (bonus > 0 ? $" · +{bonus} bonus" : "");
        int face = before.RollsUsed > 0 ? before.Dice[0] : 0;
        celebrations.Enqueue(new(player, face, detail));
        PresentNextFeedback();
    }

    internal void CelebrateOpponents(KniffelTableState before, KniffelTableState after)
    {
        if (disposed || Game.GameId != after.Player.GameId || before.Player.GameId != after.Player.GameId) return;
        for (int i = 0; i < after.Opponents.Length; i++)
            CelebrateScore(KniffelTable.Names[i], before.Opponents[i], after.Opponents[i]);
    }

    private void PresentNextFeedback()
    {
        if (disposed || !IsLoaded || !IsVisible || celebrationShowing || RestartOverlay.Visibility == Visibility.Visible) return;
        if (!celebrations.TryDequeue(out DiceCelebration? celebration)) { TryShowMatchResult(); return; }
        celebrationShowing = true;
        var body = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        body.Children.Add(new TextBlock { Name = "FiveOfAKindPlayer", Text = celebration.Player + " scored", Foreground = Ink("#C6E9D5"), FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center });
        body.Children.Add(new TextBlock { Text = "FIVE OF A KIND", Foreground = Ink("#FFE3A5"), FontFamily = new FontFamily("Georgia"), FontSize = 30, Margin = new Thickness(0, 4, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });
        var dice = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        for (int i = 0; i < 5; i++)
        {
            // AI checkpoints clear their dice after committing; matching gold dice
            // are symbolic rather than inventing an opponent's rolled face.
            if (celebration.Face > 0) dice.Children.Add(new KniffelDie { Value = celebration.Face, Width = 35, Height = 38, Margin = new Thickness(3) });
            else dice.Children.Add(new Border { Width = 27, Height = 27, Margin = new Thickness(4), Background = Ink("#E7CD97"), CornerRadius = new CornerRadius(5), Child = new TextBlock { Text = "★", FontSize = 15, Foreground = Ink("#294638"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        }
        body.Children.Add(dice);
        body.Children.Add(new TextBlock { Text = celebration.Detail, Foreground = Ink("#E4D7BA"), FontSize = 14, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Center });
        var motion = new TranslateTransform();
        var scale = new ScaleTransform(1, 1);
        var transforms = new TransformGroup(); transforms.Children.Add(scale); transforms.Children.Add(motion);
        var card = new Border
        {
            Name = "FiveOfAKindBanner", Width = 510, Padding = new Thickness(24, 18, 24, 18),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 145, 0, 0), CornerRadius = new CornerRadius(18),
            Background = Ink("#F219352B"), BorderBrush = Ink("#E8C77F"), BorderThickness = new Thickness(2),
            Effect = new DropShadowEffect { Color = Color.FromRgb(236, 194, 103), BlurRadius = 32, ShadowDepth = 0, Opacity = .7 },
            RenderTransform = transforms, RenderTransformOrigin = new Point(.5, .5), Child = body,
        };
        DiceFeedbackLayer.Children.Clear();
        DiceFeedbackLayer.Children.Add(card);
        if (animations())
        {
            card.BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = { new LinearDoubleKeyFrame(0, TimeSpan.Zero), new LinearDoubleKeyFrame(1, TimeSpan.FromSeconds(.2)),
                    new LinearDoubleKeyFrame(1, TimeSpan.FromSeconds(2.1)), new LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(2.5)) },
            });
            motion.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(24, 0, TimeSpan.FromSeconds(.35)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.9, 1, TimeSpan.FromSeconds(.35)));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.9, 1, TimeSpan.FromSeconds(.35)));
            AddCelebrationSparks();
        }
        fiveOfAKindSound?.Invoke();
        celebrationTimer.Tick -= FinishCelebration;
        celebrationTimer.Tick += FinishCelebration;
        celebrationTimer.Start();
    }

    private void AddCelebrationSparks()
    {
        var canvas = new Canvas { Width = 1160, Height = 748, ClipToBounds = true };
        DiceFeedbackLayer.Children.Insert(0, canvas);
        for (int i = 0; i < 24; i++)
        {
            double angle = i * Math.PI * 2 / 24;
            var move = new TranslateTransform();
            var spark = new Ellipse { Width = i % 3 == 0 ? 6 : 3, Height = i % 3 == 0 ? 6 : 3, Fill = Ink(i % 2 == 0 ? "#F7D792" : "#A6E4CA"), RenderTransform = move };
            Canvas.SetLeft(spark, 580); Canvas.SetTop(spark, 240); canvas.Children.Add(spark);
            var duration = TimeSpan.FromSeconds(.85 + i % 4 * .12);
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, Math.Cos(angle) * (265 + i % 4 * 18), duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, Math.Sin(angle) * 135, duration));
            spark.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, duration));
        }
    }

    private void FinishCelebration(object? sender, EventArgs e)
    {
        StopFeedback();
        PresentNextFeedback();
    }
    private void StopFeedback()
    {
        celebrationTimer.Stop();
        celebrationTimer.Tick -= FinishCelebration;
        celebrationShowing = false;
        DiceFeedbackLayer.Children.Clear();
    }
    private void ResetMatchFeedback()
    {
        StopFeedback();
        celebrations.Clear(); celebratedScores.Clear();
        resultPending = resultPresented = false;
        completionNotice = "";
        MatchResultButton.Visibility = Visibility.Collapsed;
    }
    private void RequestMatchResult()
    {
        MatchResultButton.Visibility = Visibility.Visible;
        if (resultPresented) return;
        resultPending = true;
        // Commit finishes updating focus and the host's persisted settlement first.
        Dispatcher.BeginInvoke(new Action(TryShowMatchResult), DispatcherPriority.Background);
    }
    private void TryShowMatchResult()
    {
        if (disposed || !IsLoaded || !IsVisible || !resultPending || celebrationShowing || celebrations.Count > 0
            || RestartOverlay.Visibility == Visibility.Visible) return;
        ShowMatchResult();
    }
    private void MatchResult_Click(object sender, RoutedEventArgs e)
    {
        if (!celebrationShowing && celebrations.Count == 0) ShowMatchResult();
    }
    private void ShowMatchResult()
    {
        if (disposed || tableState is { Settled: false } || tableState is null && !Game.IsComplete) return;
        resultPending = false;
        resultPresented = true;
        ShowGameSetup(new KniffelMatchResultView(Game.Snapshot, tableState, tableBankroll, completionNotice,
            HideGameSetup, () => { HideGameSetup(); gameSetup?.Invoke(); }, back));
    }
}
