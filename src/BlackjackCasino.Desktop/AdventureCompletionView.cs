using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>A shared end-of-tale presentation for every Adventure content package.</summary>
internal sealed class AdventureCompletionView : Grid
{
    private readonly bool animationsEnabled;
    private readonly Border panel;
    private readonly List<FrameworkElement> revealElements = [];
    private bool leaving;

    internal AdventureCompletionView(
        AdventureDefinition definition,
        AdventureRunState state,
        AdventureCompletionMetaResult meta,
        Action backToLibrary,
        Action restart,
        bool animationsEnabled)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);
        _ = backToLibrary ?? throw new ArgumentNullException(nameof(backToLibrary));
        _ = restart ?? throw new ArgumentNullException(nameof(restart));
        this.animationsEnabled = animationsEnabled;

        Background = new SolidColorBrush(Color.FromArgb(218, 3, 8, 7));
        panel = AdventureUi.Panel("#14231E", "#080E0C", "#9A814A", new Thickness(32, 28, 32, 28), radius: 20);
        panel.Width = 920;
        panel.MaxHeight = 790;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.Effect = new DropShadowEffect { BlurRadius = 42, ShadowDepth = 9, Opacity = .62, Color = Colors.Black };
        Children.Add(panel);

        AdventureSceneDefinition? endingScene = definition.Scenes.FirstOrDefault(scene =>
            scene.IsEnding && string.Equals(scene.EndingId, state.EndingId, StringComparison.OrdinalIgnoreCase));
        string endingName = endingScene?.Title.Replace("Ending — ", "", StringComparison.OrdinalIgnoreCase)
            ?? Humanize(state.EndingId ?? "Unknown Ending");

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 730,
        };
        var root = new StackPanel();
        scroll.Content = root;
        panel.Child = scroll;

        root.Children.Add(AdventureUi.Eyebrow("TALE COMPLETE", 11.5));
        var title = new TextBlock
        {
            Text = definition.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 35,
            FontWeight = FontWeights.SemiBold,
            Foreground = AdventureUi.Ink("#F2E2C7"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 2),
        };
        root.Children.Add(title);

        var achieved = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 7, 0, 7) };
        achieved.Children.Add(new TextBlock
        {
            Text = "ENDING ACHIEVED",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#8CB6A3"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        achieved.Children.Add(new TextBlock
        {
            Text = endingName,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Foreground = AdventureUi.Ink("#E0C177"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0),
        });
        root.Children.Add(achieved);
        revealElements.Add(achieved);

        root.Children.Add(AdventureUi.OrnamentRule("✦"));
        root.Children.Add(BuildStatistics(state));

        if (meta.NewlyCompletedChallenges.Count > 0)
        {
            FrameworkElement challengeReveal = BuildChallengeReveal(meta.NewlyCompletedChallenges);
            root.Children.Add(challengeReveal);
            revealElements.Add(challengeReveal);
        }

        if (meta.UnlockedTitle is not null)
        {
            FrameworkElement titleReveal = BuildTitleReveal(meta.UnlockedTitle);
            root.Children.Add(titleReveal);
            revealElements.Add(titleReveal);
        }

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 22, 0, 0),
        };
        Button library = AdventureUi.Button("ADVENTURE LIBRARY", accent: false, fontSize: 12.5, scaleOnHover: false);
        library.MinWidth = 205;
        library.Margin = new Thickness(6, 0, 6, 0);
        library.Click += async (_, _) => await LeaveAsync(backToLibrary);
        actions.Children.Add(library);
        Button again = AdventureUi.Button("BEGIN AGAIN", accent: true, fontSize: 12.5, scaleOnHover: false);
        again.MinWidth = 190;
        again.Margin = new Thickness(6, 0, 6, 0);
        again.Click += async (_, _) => await LeaveAsync(restart);
        actions.Children.Add(again);
        root.Children.Add(actions);

        if (animationsEnabled)
        {
            Opacity = 0;
            panel.RenderTransformOrigin = new Point(.5, .5);
            panel.RenderTransform = new ScaleTransform(.965, .965);
            foreach (FrameworkElement element in revealElements) element.Opacity = 0;
            Loaded += async (_, _) => await AnimateInAsync();
        }
    }

    private static FrameworkElement BuildStatistics(AdventureRunState state)
    {
        AdventureRunStatistics stats = state.Statistics;
        TimeSpan duration = (stats.CompletedAtUtc ?? DateTimeOffset.UtcNow) - stats.StartedAtUtc;
        if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

        Border box = AdventureUi.Panel("#111D19", "#0A1311", "#40594F", new Thickness(17, 14, 17, 14), radius: 12, shadow: false);
        box.Margin = new Thickness(0, 4, 0, 11);
        var root = new StackPanel();
        root.Children.Add(AdventureUi.Eyebrow("RUN STATISTICS", 9.5));

        var grid = new UniformGrid { Columns = 4, Margin = new Thickness(0, 9, 0, 0) };
        grid.Children.Add(Stat("TIME", FormatDuration(duration)));
        grid.Children.Add(Stat("SCENES", $"{stats.UniqueSceneIds.Count:N0} / {stats.ScenesVisited:N0}"));
        grid.Children.Add(Stat("CHOICES", stats.ChoicesMade.ToString("N0")));
        grid.Children.Add(Stat("D20 ROLLS", state.RollsMade.ToString("N0")));
        grid.Children.Add(Stat("CHECKS WON", stats.ChecksSucceeded.ToString("N0")));
        grid.Children.Add(Stat("CHECKS LOST", stats.ChecksFailed.ToString("N0")));
        grid.Children.Add(Stat("NAT 20", stats.NaturalTwenties.ToString("N0")));
        grid.Children.Add(Stat("NAT 1", stats.NaturalOnes.ToString("N0")));
        grid.Children.Add(Stat("ITEMS FOUND", stats.ItemsFound.ToString("N0")));
        grid.Children.Add(Stat("DAMAGE", stats.DamageTaken.ToString("N0")));
        grid.Children.Add(Stat("ENCOUNTERS", $"{stats.EncountersWon:N0}W · {stats.EncountersLost:N0}L"));
        grid.Children.Add(Stat("COINS LEFT", state.Coins.ToString("N0")));
        root.Children.Add(grid);
        box.Child = root;
        return box;
    }

    private static FrameworkElement BuildChallengeReveal(IReadOnlyList<ChallengeDefinition> challenges)
    {
        Border box = AdventureUi.Panel("#19251D", "#0E1712", "#7B6941", new Thickness(17, 13, 17, 13), radius: 12, shadow: false);
        box.Margin = new Thickness(0, 5, 0, 8);
        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = challenges.Count == 1 ? "ACHIEVEMENT UNLOCKED" : $"{challenges.Count} ACHIEVEMENTS UNLOCKED",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#D1B56D"),
        });
        foreach (ChallengeDefinition challenge in challenges.Take(5))
        {
            root.Children.Add(new TextBlock
            {
                Text = "✦  " + challenge.Name,
                FontFamily = new FontFamily("Georgia"),
                FontSize = 14,
                Foreground = AdventureUi.Ink("#E8D9BB"),
                Margin = new Thickness(0, 5, 0, 0),
            });
        }
        if (challenges.Count > 5)
            root.Children.Add(new TextBlock { Text = $"+ {challenges.Count - 5} more in Challenges", FontSize = 10.5, Foreground = AdventureUi.Ink("#8EA399"), Margin = new Thickness(22, 4, 0, 0) });
        box.Child = root;
        return box;
    }

    private static FrameworkElement BuildTitleReveal(CosmeticDefinition title)
    {
        Border box = AdventureUi.Panel("#162522", "#091412", "#8FAF83", new Thickness(19, 15, 19, 15), radius: 13, shadow: false);
        box.Margin = new Thickness(0, 5, 0, 8);
        box.Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 0, Opacity = .35, Color = Color.FromRgb(92, 210, 195) };
        var root = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        root.Children.Add(new TextBlock
        {
            Text = "PRESTIGE TITLE UNLOCKED",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#A6C7B7"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        root.Children.Add(new TextBlock
        {
            Text = "✦  " + title.Name + "  ✦",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            FontWeight = FontWeights.SemiBold,
            Foreground = CosmeticAppearance.TitleBrush(title),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 2),
        });
        root.Children.Add(new TextBlock
        {
            Text = "Added permanently to your profile titles.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            Foreground = AdventureUi.Ink("#839B91"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        box.Child = root;
        return box;
    }

    private static FrameworkElement Stat(string label, string value)
    {
        var stack = new StackPanel { Margin = new Thickness(5, 5, 5, 7) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 8.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#728E83"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15.5,
            Foreground = AdventureUi.Ink("#E1D1B3"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        });
        return stack;
    }

    private async Task AnimateInAsync()
    {
        BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(330)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        if (panel.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(.965, 1, TimeSpan.FromMilliseconds(380)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(.965, 1, TimeSpan.FromMilliseconds(380)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        }

        await Task.Delay(260);
        foreach (FrameworkElement element in revealElements)
        {
            element.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            await Task.Delay(180);
        }
    }

    private async Task LeaveAsync(Action destination)
    {
        if (leaving) return;
        leaving = true;
        try
        {
            if (animationsEnabled)
            {
                BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
                await Task.Delay(250);
            }
            destination();
        }
        finally
        {
            leaving = false;
        }
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{duration.Minutes}:{duration.Seconds:00}";

    private static string Humanize(string value) => string.Join(' ',
        value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
