using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Adds the per-theme completion reward surface to the collector album without coupling the
/// collection-domain reward transaction to the album's card rendering code. The album rebuilds its
/// card grid after theme switches and pack reveals, so this presenter reattaches the reward surface
/// to the named card area after those layout passes.
/// </summary>
internal sealed class CollectionThemeRewardPresenter : IDisposable
{
    private readonly CollectionAlbumPage page;
    private readonly PlayerProgressionController controller;
    private readonly FrameworkElement root;
    private readonly bool animationsEnabled;
    private string activeThemeId = CollectionCatalog.Themes[0].Id;
    private Border? rewardSurface;
    private bool renderScheduled;
    private bool disposed;

    private CollectionThemeRewardPresenter(
        CollectionAlbumPage page,
        PlayerProgressionController controller,
        FrameworkElement root,
        bool animationsEnabled)
    {
        this.page = page;
        this.controller = controller;
        this.root = root;
        this.animationsEnabled = animationsEnabled;

        root.AddHandler(Button.ClickEvent, new RoutedEventHandler(AnyButton_Click), true);
        root.LayoutUpdated += Root_LayoutUpdated;
        page.Closed += Page_Closed;
        ScheduleRender();
    }

    internal static void Attach(CollectionAlbumPage page, PlayerProgressionController controller, bool animationsEnabled)
    {
        if (page.Content is FrameworkElement root)
            _ = new CollectionThemeRewardPresenter(page, controller, root, animationsEnabled);
    }

    private void AnyButton_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;
        Button? button = FindAncestor<Button>(source);
        if (button?.Tag is not string tag) return;
        if (CollectionCatalog.Themes.Any(theme => string.Equals(theme.Id, tag, StringComparison.Ordinal)))
        {
            activeThemeId = tag;
            ScheduleRender();
        }
    }

    private void Root_LayoutUpdated(object? sender, EventArgs e)
    {
        if (disposed) return;
        Grid? cards = VisualDescendants<Grid>(root).FirstOrDefault(grid => grid.Name == "AlbumCards");
        if (cards is null) return;
        if (rewardSurface?.Parent != cards) ScheduleRender();
    }

    private void ScheduleRender()
    {
        if (disposed || renderScheduled) return;
        renderScheduled = true;
        root.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            renderScheduled = false;
            if (!disposed) Render();
        }));
    }

    private void Render()
    {
        Grid? cards = VisualDescendants<Grid>(root).FirstOrDefault(grid => grid.Name == "AlbumCards");
        if (cards is null) return;
        CollectionTheme theme = CollectionCatalog.Themes.FirstOrDefault(candidate => candidate.Id == activeThemeId)
            ?? CollectionCatalog.Themes[0];
        activeThemeId = theme.Id;

        if (rewardSurface?.Parent is Panel oldParent) oldParent.Children.Remove(rewardSurface);

        CollectionState state = controller.Profile.Collection;
        int owned = theme.Cards.Count(card => state.OwnedCardIds.Contains(card.Id));
        bool complete = owned == theme.Cards.Count;
        bool claimed = state.ClaimedThemeRewardIds.Contains(theme.Id);
        CollectionThemeCompletionReward reward = CollectionService.ThemeCompletionReward(theme);
        rewardSurface = BuildRewardSurface(theme, reward, owned, complete, claimed);

        cards.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
        int rewardRow = cards.RowDefinitions.Count;
        cards.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(rewardSurface, rewardRow);
        cards.Children.Add(rewardSurface);
    }

    private Border BuildRewardSurface(
        CollectionTheme theme,
        CollectionThemeCompletionReward reward,
        int owned,
        bool complete,
        bool claimed)
    {
        Color accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
        var accentBrush = new SolidColorBrush(accent);
        var panel = new Border
        {
            Name = "AlbumThemeCompletionReward",
            Tag = theme.Id,
            Margin = new Thickness(4, 0, 4, 4),
            Padding = new Thickness(18, 14, 18, 14),
            MinHeight = 112,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(complete ? 1.5 : 1),
            BorderBrush = complete ? accentBrush : new SolidColorBrush(Color.FromRgb(48, 76, 63)),
            Background = complete
                ? new LinearGradientBrush(Color.FromRgb(11, 34, 29), Color.FromRgb(11, 25, 28), 12)
                : new LinearGradientBrush(Color.FromRgb(10, 28, 22), Color.FromRgb(9, 24, 21), 12),
        };
        System.Windows.Automation.AutomationProperties.SetName(panel, $"{theme.Name} completion reward");

        if (complete && !claimed)
        {
            var glow = new DropShadowEffect { Color = accent, ShadowDepth = 0, BlurRadius = 13, Opacity = .28 };
            panel.Effect = glow;
            if (animationsEnabled)
            {
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(.20, .52, TimeSpan.FromSeconds(1.8))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
            }
        }

        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(178) });

        layout.Children.Add(new Border
        {
            Width = 58,
            Height = 58,
            CornerRadius = new CornerRadius(29),
            BorderThickness = new Thickness(1.5),
            BorderBrush = accentBrush,
            Background = new SolidColorBrush(Color.FromArgb(55, accent.R, accent.G, accent.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = claimed ? "✓" : theme.Symbol,
                FontSize = claimed ? 25 : 27,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });

        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 0, 14, 0) };
        copy.Children.Add(new TextBlock
        {
            Text = claimed ? "THEME REWARD CLAIMED" : complete ? "THEME COMPLETE" : "THEME COMPLETION REWARD",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = complete ? accentBrush : new SolidColorBrush(Color.FromRgb(150, 174, 160)),
        });
        copy.Children.Add(new TextBlock
        {
            Text = theme.Name,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(244, 230, 207)),
            Margin = new Thickness(0, 2, 0, 2),
        });
        copy.Children.Add(new TextBlock
        {
            Text = claimed
                ? $"All {theme.Cards.Count} cards collected · completion reward secured."
                : complete
                    ? $"All {theme.Cards.Count} cards collected. Your completion reward is ready."
                    : $"Collect {theme.Cards.Count - owned} more card{(theme.Cards.Count - owned == 1 ? string.Empty : "s")} · {owned}/{theme.Cards.Count} collected.",
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.FromRgb(157, 177, 166)),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(copy, 1);
        layout.Children.Add(copy);

        var rewards = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        rewards.Children.Add(RewardBadge("◆", $"{reward.Coins:N0}", "COSMETIC COINS", Color.FromRgb(84, 225, 183)));
        rewards.Children.Add(RewardBadge("✦", $"{reward.PackCount}× {reward.PackRarity}", "CARD PACK", CollectionArtwork.RarityColor(reward.PackRarity)));
        Grid.SetColumn(rewards, 2);
        layout.Children.Add(rewards);

        var claim = new Button
        {
            Name = "AlbumThemeRewardClaim",
            Tag = "theme-reward:" + theme.Id,
            Content = claimed ? "CLAIMED" : complete ? "CLAIM REWARD" : "LOCKED",
            Width = 156,
            Height = 42,
            Margin = new Thickness(16, 0, 0, 0),
            Padding = new Thickness(12, 7, 12, 7),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = complete && !claimed
                ? new SolidColorBrush(Color.FromArgb(90, accent.R, accent.G, accent.B))
                : new SolidColorBrush(Color.FromRgb(18, 43, 33)),
            BorderBrush = complete && !claimed ? accentBrush : new SolidColorBrush(Color.FromRgb(55, 76, 66)),
            BorderThickness = new Thickness(1),
            Foreground = complete && !claimed
                ? new SolidColorBrush(Color.FromRgb(245, 240, 222))
                : new SolidColorBrush(Color.FromRgb(112, 137, 123)),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            IsEnabled = complete && !claimed,
            Cursor = complete && !claimed ? CasinoCursors.Hand : System.Windows.Input.Cursors.Arrow,
            ToolTip = claimed
                ? "This completion reward has already been claimed."
                : complete
                    ? $"Claim the {theme.Name} completion reward."
                    : $"Collect all {theme.Cards.Count} cards in this theme to unlock the reward.",
        };
        System.Windows.Automation.AutomationProperties.SetName(claim,
            claimed ? $"{theme.Name} reward claimed" : complete ? $"Claim {theme.Name} reward" : $"{theme.Name} reward locked");
        claim.Click += (_, _) => Claim(theme);
        Grid.SetColumn(claim, 3);
        layout.Children.Add(claim);

        panel.Child = layout;
        return panel;
    }

    private static Border RewardBadge(string symbol, string value, string caption, Color accent)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = symbol,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(accent),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = value.ToUpperInvariant(),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(234, 236, 226)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        });
        stack.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 8,
            Foreground = new SolidColorBrush(Color.FromRgb(135, 157, 146)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        return new Border
        {
            Width = 142,
            Height = 70,
            Margin = new Thickness(4, 0, 4, 0),
            Padding = new Thickness(7, 5, 7, 5),
            Background = new SolidColorBrush(Color.FromRgb(8, 27, 22)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, accent.R, accent.G, accent.B)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = stack,
        };
    }

    private void Claim(CollectionTheme theme)
    {
        CollectionThemeRewardClaimResult result = controller.ClaimCollectionThemeReward(theme.Id);
        SetAlbumStatus(result.Message);
        if (!result.Success)
        {
            ScheduleRender();
            return;
        }

        RefreshAlbumTheme(theme.Id);
        ScheduleRender();

        var celebration = new CollectionThemeRewardWindow(theme, result, animationsEnabled);
        Window? owner = Window.GetWindow(root);
        if (owner is { IsVisible: true }) celebration.Owner = owner;
        celebration.ShowDialog();
    }

    private void RefreshAlbumTheme(string themeId)
    {
        Button? themeButton = VisualDescendants<Button>(root)
            .FirstOrDefault(button => string.Equals(button.Tag as string, themeId, StringComparison.Ordinal));
        themeButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, themeButton));
    }

    private void SetAlbumStatus(string message)
    {
        TextBlock? status = VisualDescendants<TextBlock>(root)
            .FirstOrDefault(text => text.Name == "AlbumCollectionStatus");
        if (status is not null) status.Text = message;
    }

    private static T? FindAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static IEnumerable<T> VisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in VisualDescendants<T>(child)) yield return nested;
        }
    }

    private void Page_Closed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        root.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(AnyButton_Click));
        root.LayoutUpdated -= Root_LayoutUpdated;
        page.Closed -= Page_Closed;
    }
}
