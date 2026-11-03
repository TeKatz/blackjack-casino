using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class LobbyDailyDrawer : Grid
{
    private const double PanelWidth = 350d;
    private const double TabWidth = 54d;

    private readonly Func<DailyLoginStatus> statusProvider;
    private readonly Func<DailyLoginClaim?> claimProvider;
    private readonly Func<bool> animations;
    private readonly Border dismissLayer;
    private readonly Grid drawerStrip;
    private readonly TranslateTransform drawerTransform = new(-PanelWidth, 0d);
    private readonly Button tabButton;
    private readonly TextBlock tabReward;
    private readonly Border tabDot;
    private readonly TextBlock headline;
    private readonly TextBlock subline;
    private readonly Button claimButton;
    private readonly TextBlock nextReward;
    private readonly TextBlock rewardBurst;
    private readonly TranslateTransform rewardBurstTransform = new();
    private readonly List<RewardRow> rewardRows = [];
    private Window? hostWindow;
    private bool isOpen;
    private int lastAutoOpenedTotalClaims = -1;

    private sealed record RewardRow(Border Shell, TextBlock Day, TextBlock Reward, TextBlock State);

    internal LobbyDailyDrawer(
        Func<DailyLoginStatus> statusProvider,
        Func<DailyLoginClaim?> claimProvider,
        Func<bool>? animations = null)
    {
        this.statusProvider = statusProvider;
        this.claimProvider = claimProvider;
        this.animations = animations ?? (() => true);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;

        dismissLayer = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(72, 3, 8, 10)),
            Visibility = Visibility.Collapsed,
            Cursor = Cursors.Arrow,
        };
        dismissLayer.MouseLeftButtonDown += (_, _) => SetOpen(false);
        Children.Add(dismissLayer);

        drawerStrip = new Grid
        {
            Width = PanelWidth + TabWidth,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            RenderTransform = drawerTransform,
        };
        drawerStrip.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PanelWidth) });
        drawerStrip.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(TabWidth) });
        Panel.SetZIndex(drawerStrip, 2);
        Children.Add(drawerStrip);

        var panel = new Border
        {
            Background = new LinearGradientBrush(Ink("#10231F").Color, Ink("#091714").Color, 90),
            BorderBrush = Ink("#3E6558"),
            BorderThickness = new Thickness(0, 1, 1, 1),
            CornerRadius = new CornerRadius(0, 14, 14, 0),
            Padding = new Thickness(22, 22, 18, 18),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 24,
                ShadowDepth = 5,
                Opacity = .42,
            },
        };
        drawerStrip.Children.Add(panel);

        var panelRoot = new Grid();
        panelRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panelRoot.RowDefinitions.Add(new RowDefinition());
        panelRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.Child = panelRoot;

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        header.Children.Add(new TextBlock
        {
            Text = "DAILY REWARDS",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#D8B96F"),
        });
        headline = new TextBlock
        {
            Text = "A little something for stopping by.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 24,
            Foreground = Ink("#F4E8D5"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 4),
        };
        header.Children.Add(headline);
        subline = new TextBlock
        {
            Text = "No streaks. No pressure. Miss a day and your next reward simply waits for you.",
            FontSize = 12,
            Foreground = Ink("#A5B9B0"),
            TextWrapping = TextWrapping.Wrap,
        };
        header.Children.Add(subline);
        panelRoot.Children.Add(header);

        var rewards = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        for (int step = 1; step <= DailyLoginRewardSchedule.CycleLength; step++)
        {
            rewards.Children.Add(BuildRewardRow(step));
        }
        Grid.SetRow(rewards, 1);
        panelRoot.Children.Add(rewards);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        claimButton = new Button
        {
            Height = 46,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Background = Ink("#D8B96F"),
            Foreground = Ink("#102019"),
            BorderBrush = Ink("#F3D895"),
            BorderThickness = new Thickness(1),
            Cursor = CasinoCursors.Hand,
        };
        claimButton.Click += ClaimButton_Click;
        footer.Children.Add(claimButton);
        nextReward = new TextBlock
        {
            FontSize = 11,
            Foreground = Ink("#8FA69B"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };
        Grid.SetRow(nextReward, 1);
        footer.Children.Add(nextReward);
        Grid.SetRow(footer, 2);
        panelRoot.Children.Add(footer);

        rewardBurst = new TextBlock
        {
            FontFamily = new FontFamily("Georgia"),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#FFE7A1"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0,
            IsHitTestVisible = false,
            Effect = new DropShadowEffect { Color = Color.FromRgb(238, 192, 90), BlurRadius = 18, ShadowDepth = 0, Opacity = .72 },
            RenderTransform = rewardBurstTransform,
        };
        Panel.SetZIndex(rewardBurst, 3);
        Grid.SetRowSpan(rewardBurst, 3);
        panelRoot.Children.Add(rewardBurst);

        tabReward = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#F7E5B8"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        tabDot = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = Ink("#72E0AE"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6),
        };
        var tabStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        tabStack.Children.Add(tabDot);
        tabStack.Children.Add(new TextBlock
        {
            Text = "✦",
            FontSize = 18,
            Foreground = Ink("#D8B96F"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        tabStack.Children.Add(new TextBlock
        {
            Text = "D\nA\nI\nL\nY",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#E9DDC8"),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 7, 0, 7),
        });
        tabStack.Children.Add(tabReward);

        tabButton = new Button
        {
            Width = TabWidth,
            MinHeight = 214,
            Padding = new Thickness(4, 10, 4, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Ink("#132A25"),
            BorderBrush = Ink("#4A6C60"),
            BorderThickness = new Thickness(0, 1, 1, 1),
            Content = tabStack,
            Cursor = CasinoCursors.Hand,
            ToolTip = "Daily rewards",
        };
        tabButton.Click += (_, _) => SetOpen(!isOpen);
        Grid.SetColumn(tabButton, 1);
        drawerStrip.Children.Add(tabButton);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
            {
                Refresh(allowAutoOpen: true);
            }
            else
            {
                SetOpen(false, animate: false);
            }
        };

        Refresh();
    }

    internal void Refresh(bool allowAutoOpen = false)
    {
        DailyLoginStatus state = statusProvider();
        int completed = Math.Clamp(state.CycleCompletedSteps, 0, DailyLoginRewardSchedule.CycleLength);

        for (int index = 0; index < rewardRows.Count; index++)
        {
            int step = index + 1;
            RewardRow row = rewardRows[index];
            bool active = step == state.ActiveStep;
            bool claimedToday = state.ClaimedToday && active;
            bool done = step <= completed;

            row.Shell.Background = claimedToday
                ? Ink("#244C3D")
                : active && state.CanClaim
                    ? Ink("#2A3326")
                    : done ? Ink("#163229") : Ink("#0E1D1A");
            row.Shell.BorderBrush = claimedToday
                ? Ink("#72E0AE")
                : active && state.CanClaim
                    ? Ink("#D8B96F")
                    : done ? Ink("#345F4D") : Ink("#273E37");
            row.Shell.BorderThickness = new Thickness(active ? 1.5 : 1);
            row.Day.Foreground = active ? Ink("#F6E6BB") : Ink("#9FB0A8");
            row.Reward.Foreground = step == DailyLoginRewardSchedule.CycleLength ? Ink("#F2CF78") : Ink("#E8E1D3");
            row.State.Text = claimedToday ? "TODAY ✓" : done ? "CLAIMED" : active && state.CanClaim ? "READY" : "UP NEXT";
            row.State.Foreground = claimedToday || done ? Ink("#72E0AE") : active && state.CanClaim ? Ink("#D8B96F") : Ink("#60786E");
        }

        DailyLoginRewardDefinition nextDefinition = DailyLoginRewardSchedule.GetDefinition(state.TotalClaims);
        tabReward.Text = state.CanClaim ? $"${state.ActiveReward:N0}" : "✓";
        tabDot.Visibility = state.CanClaim ? Visibility.Visible : Visibility.Collapsed;
        claimButton.IsEnabled = state.CanClaim;
        claimButton.Content = state.CanClaim ? $"CLAIM DAY {state.ActiveStep} REWARDS" : "CLAIMED TODAY";
        claimButton.Background = state.CanClaim ? Ink("#D8B96F") : Ink("#274238");
        claimButton.Foreground = state.CanClaim ? Ink("#102019") : Ink("#93B2A4");
        nextReward.Text = state.CanClaim
            ? $"Reward {state.ActiveStep}/{DailyLoginRewardSchedule.CycleLength} · {RewardSummary(nextDefinition)}"
            : $"Next visit: reward {state.NextStep}/{DailyLoginRewardSchedule.CycleLength} · {RewardSummary(nextDefinition)}";

        SyncTabPulse(state.CanClaim);
        if (allowAutoOpen && state.CanClaim && lastAutoOpenedTotalClaims != state.TotalClaims)
        {
            lastAutoOpenedTotalClaims = state.TotalClaims;
            Dispatcher.BeginInvoke(() => SetOpen(true));
        }
    }

    internal bool TryClose()
    {
        if (!isOpen)
        {
            return false;
        }

        SetOpen(false);
        return true;
    }

    private Border BuildRewardRow(int step)
    {
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });

        var day = new TextBlock
        {
            Text = $"DAY {step}",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        layout.Children.Add(day);

        DailyLoginRewardDefinition definition = DailyLoginRewardSchedule.GetDefinition(step - 1);
        var reward = new TextBlock
        {
            Text = RewardSummary(definition, multilinePack: true),
            FontFamily = new FontFamily("Georgia"),
            FontSize = step == DailyLoginRewardSchedule.CycleLength ? 15 : 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(reward, 1);
        layout.Children.Add(reward);

        var state = new TextBlock
        {
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(state, 2);
        layout.Children.Add(state);

        var shell = new Border
        {
            Height = definition.PackRarity.HasValue ? 58 : 47,
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(12, 0, 12, 0),
            CornerRadius = new CornerRadius(8),
            Child = layout,
        };
        rewardRows.Add(new RewardRow(shell, day, reward, state));
        return shell;
    }

    private void ClaimButton_Click(object sender, RoutedEventArgs e)
    {
        DailyLoginClaim? claim = claimProvider();
        if (claim is null)
        {
            Refresh();
            return;
        }

        Refresh();
        AnimateRewardBurst(DailyLoginRewardSchedule.GetDefinition(claim.TotalClaims - 1));
    }

    private void AnimateRewardBurst(DailyLoginRewardDefinition reward)
    {
        rewardBurst.Text = $"+${reward.Bankroll:N0} · +{reward.CosmeticCoins:N0} ◆";
        if (reward.PackRarity is { } rarity)
        {
            rewardBurst.Text += $"\n+ {rarity} card pack";
        }

        if (!animations())
        {
            rewardBurst.Opacity = 0;
            return;
        }

        rewardBurstTransform.Y = 18;
        rewardBurst.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
        {
            AutoReverse = true,
            BeginTime = TimeSpan.FromMilliseconds(40),
            Duration = TimeSpan.FromMilliseconds(520),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
        });
        rewardBurstTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, -34, TimeSpan.FromMilliseconds(720))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
    }

    private static string RewardSummary(DailyLoginRewardDefinition reward, bool multilinePack = false)
    {
        string summary = $"${reward.Bankroll:N0} · ◆ {reward.CosmeticCoins:N0}";
        if (reward.PackRarity is { } rarity)
        {
            summary += multilinePack ? $"\n{rarity.ToString().ToUpperInvariant()} PACK" : $" · {rarity} pack";
        }
        return summary;
    }

    private void SetOpen(bool open, bool? animate = null)
    {
        isOpen = open;
        dismissLayer.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        double target = open ? 0d : -PanelWidth;
        bool shouldAnimate = animate ?? animations();
        if (!shouldAnimate)
        {
            drawerTransform.BeginAnimation(TranslateTransform.XProperty, null);
            drawerTransform.X = target;
            return;
        }

        double from = drawerTransform.X;
        var movement = new DoubleAnimation(from, target, TimeSpan.FromMilliseconds(open ? 340 : 280))
        {
            EasingFunction = new QuarticEase { EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop,
        };
        movement.Completed += (_, _) =>
        {
            drawerTransform.BeginAnimation(TranslateTransform.XProperty, null);
            drawerTransform.X = target;
        };
        drawerTransform.BeginAnimation(TranslateTransform.XProperty, movement, HandoffBehavior.SnapshotAndReplace);
    }

    private void SyncTabPulse(bool available)
    {
        tabButton.BeginAnimation(OpacityProperty, null);
        if (!available || !animations())
        {
            tabButton.Opacity = 1;
            return;
        }

        tabButton.BeginAnimation(OpacityProperty, new DoubleAnimation(.82, 1, TimeSpan.FromSeconds(1.35))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        hostWindow = Window.GetWindow(this);
        if (hostWindow is not null)
        {
            hostWindow.PreviewKeyDown -= HostWindow_PreviewKeyDown;
            hostWindow.PreviewKeyDown += HostWindow_PreviewKeyDown;
        }
        Refresh(allowAutoOpen: true);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (hostWindow is not null)
        {
            hostWindow.PreviewKeyDown -= HostWindow_PreviewKeyDown;
            hostWindow = null;
        }
        tabButton.BeginAnimation(OpacityProperty, null);
    }

    private void HostWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && TryClose())
        {
            e.Handled = true;
        }
    }

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
