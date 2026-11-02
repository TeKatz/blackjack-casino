using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Desktop;

internal sealed partial class WordSearchView
{
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private readonly object audioOwner = new();
    private static readonly Lazy<AudioClip> FoundCue = new(() => MemorySoundSynthesis.Build(MemoryCue.Match));
    private static readonly Lazy<AudioClip> CompleteCue = new(() => MemorySoundSynthesis.Build(MemoryCue.Complete));
    private DispatcherTimer? completionDelay;
    private readonly List<Border> wordGlows = [];

    private void CelebrateWord(WordSearchCell start, WordSearchCell end)
    {
        if (sounds()) CasinoAudio.Play(FoundCue.Value, audioOwner, .5);
        if (!animations()) return;
        IReadOnlyList<WordSearchCell> path = WordSearchGame.SelectionCells(game.Puzzle, start, end);
        for (int i = 0; i < path.Count; i++)
        {
            Border cell = cells[path[i]];
            if (cell.Child is not Grid layers)
            {
                UIElement letter = cell.Child;
                cell.Child = null;
                layers = new Grid(); layers.Children.Add(letter); cell.Child = layers;
            }
            var glow = new Border { IsHitTestVisible = false, Opacity = 0, CornerRadius = new CornerRadius(3),
                Background = new LinearGradientBrush(Color.FromArgb(25, 255, 237, 155), Color.FromArgb(210, 177, 255, 210), 25),
                BorderBrush = WordSearchLoungeHub.Brush("#F3D999"), BorderThickness = new Thickness(1.5) };
            layers.Children.Add(glow); wordGlows.Add(glow);
            var sweep = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromMilliseconds(i * 45), FillBehavior = FillBehavior.Stop };
            sweep.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new LinearDoubleKeyFrame(.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
            sweep.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));
            sweep.Completed += (_, _) => { glow.BeginAnimation(OpacityProperty, null); layers.Children.Remove(glow); wordGlows.Remove(glow); };
            glow.BeginAnimation(OpacityProperty, sweep);
        }
    }

    private void ShowCompletion(ProgressionAward award)
    {
        completionDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(animations() ? 950 : 1) };
        completionDelay.Tick += (_, _) =>
        {
            completionDelay.Stop();
            if (disposed || Content is not Grid root) return;
            if (sounds()) CasinoAudio.Play(CompleteCue.Value, audioOwner, .5);
            var overlay = new Grid { Name = "WordSearchCompletionOverlay", Background = new SolidColorBrush(Color.FromArgb(230, 5, 14, 12)) };
            Panel.SetZIndex(overlay, 100);
            KeyboardNavigation.SetTabNavigation(overlay, KeyboardNavigationMode.Cycle);
            var card = WordSearchLoungeHub.Panel();
            card.Width = 560; card.Padding = new Thickness(32); card.HorizontalAlignment = HorizontalAlignment.Center; card.VerticalAlignment = VerticalAlignment.Center;
            var copy = new StackPanel();
            copy.Children.Add(WordSearchLoungeHub.Label("PUZZLE COMPLETE", 15, "#91CFAD", FontWeights.Bold));
            copy.Children.Add(WordSearchLoungeHub.MarginLabel(game.Puzzle.Theme, 32, "#F0DFC2", new Thickness(0, 8, 0, 18), FontWeights.SemiBold));
            copy.Children.Add(WordSearchLoungeHub.Label($"{game.Puzzle.Difficulty} · {game.FoundWords.Count} words found", 20, "#D9DEC9"));
            copy.Children.Add(WordSearchLoungeHub.MarginLabel($"Time  {FormatTime(game.ElapsedSeconds)}    ·    Hints  {game.HintsUsed}", 18, "#AAC4B4", new Thickness(0, 10, 0, 16)));
            copy.Children.Add(WordSearchLoungeHub.Label($"+{award.PlayerExperience:N0} player XP · +{award.SeasonExperience:N0} season XP", 17, "#DFC17D"));
            var buttons = new UniformGrid { Columns = 2, Margin = new Thickness(0, 24, 0, 0) };
            Button club = WordSearchLoungeHub.ActionButton("RETURN TO CLUB", "WordSearchCompletionClub");
            club.Margin = new Thickness(0, 0, 8, 0);
            club.Click += (_, _) => ReturnToClub(club);
            Button hub = WordSearchLoungeHub.ActionButton("WORD SEARCH", "WordSearchCompletionHub");
            hub.Click += (_, _) => back();
            buttons.Children.Add(club); buttons.Children.Add(hub); copy.Children.Add(buttons);
            card.Child = copy;
            overlay.Children.Add(new Viewbox { Child = card, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(16) });
            root.Children.Add(overlay); Grid.SetRowSpan(overlay, root.RowDefinitions.Count);
            AutomationProperties.SetName(overlay, "Word Search puzzle complete");
            hub.Focus();
            if (animations()) card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { FillBehavior = FillBehavior.Stop });
        };
        completionDelay.Start();
    }

    private void ReturnToClub(Button exclude)
    {
        if (Window.GetWindow(this) is not Window window) return;
        Button? shellReturn = Descendants(window).OfType<Button>().FirstOrDefault(button => button != exclude && button.IsVisible
            && button.Content is string text && (text == "RETURN TO CLUB" || text == "ZURÜCK ZUM CLUB"));
        shellReturn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (DependencyObject nested in Descendants(child)) yield return nested;
        }
    }

    private void StopFeedback()
    {
        completionDelay?.Stop();
        foreach (Border glow in wordGlows.ToArray())
        {
            glow.BeginAnimation(OpacityProperty, null);
            if (glow.Parent is Grid layers) layers.Children.Remove(glow);
        }
        wordGlows.Clear();
    }
}
