using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private PlayerProgressionController? progressionController;
    private Button? profileButton;
    private Border? profileLevelUpSweep;
    private TranslateTransform? profileLevelUpSweepTransform;
    private bool progressionInitialized;

    private void InitializeProgressionFeature()
    {
        if (progressionInitialized)
        {
            return;
        }

        progressionInitialized = true;
        progressionController = new PlayerProgressionController(initialProfile);
        BuildProfileButton();
        AddHandler(Button.ClickEvent, new RoutedEventHandler(ProgressionButtonBubbled), handledEventsToo: true);
        AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(ProgressionKeyObserved), handledEventsToo: true);
        RefreshProfileButton();
    }

    private void ShutdownProgressionFeature()
    {
        progressionController?.Dispose();
        progressionController = null;
    }

    private void BuildProfileButton()
    {
        if (VersionText.Parent is not StackPanel versionPanel || versionPanel.Parent is not Grid headerGrid)
        {
            return;
        }

        if (headerGrid.ColumnDefinitions.Count < 4)
        {
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        Grid.SetColumn(versionPanel, 4);

        // The level-up layer is a sibling, so the cue never changes the button's measured layout.
        profileButton = new Button
        {
            Name = "PlayerProfileButton",
            MinWidth = 275,
            Height = 52,
            Margin = new Thickness(0, 0, 22, 0),
            Padding = new Thickness(9, 5, 9, 5),
            Style = (Style)FindResource("MutedButton"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        profileButton.Click += ProfileButton_Click;
        Grid.SetColumn(profileButton, 2);
        Panel.SetZIndex(profileButton, 1);
        headerGrid.Children.Add(profileButton);

        var sweepClip = new Canvas
        {
            Width = 255,
            Height = 44,
            Margin = new Thickness(0, 0, 22, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Background = Brushes.Transparent,
        };

        profileLevelUpSweepTransform = new TranslateTransform(-125, 0);
        profileLevelUpSweep = new Border
        {
            Width = 118,
            Height = 38,
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransform = profileLevelUpSweepTransform,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 214, 63, 82), 0),
                    new GradientStop(Color.FromArgb(105, 225, 71, 91), 0.28),
                    new GradientStop(Color.FromArgb(215, 255, 176, 98), 0.5),
                    new GradientStop(Color.FromArgb(105, 225, 71, 91), 0.72),
                    new GradientStop(Color.FromArgb(0, 214, 63, 82), 1),
                },
            },
        };
        Canvas.SetTop(profileLevelUpSweep, 3);
        sweepClip.Children.Add(profileLevelUpSweep);
        Grid.SetColumn(sweepClip, 2);
        Panel.SetZIndex(sweepClip, 2);
        headerGrid.Children.Add(sweepClip);
    }

    private void ProgressionButtonBubbled(object sender, RoutedEventArgs e) => QueueProgressionCheck();

    private void ProgressionKeyObserved(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.H or Key.S or Key.D or Key.P or Key.R or Key.Enter)
        {
            QueueProgressionCheck();
        }
    }

    private void QueueProgressionCheck()
    {
        if (progressionController is not null)
        {
            Dispatcher.BeginInvoke(ProcessLatestCompletedRound, DispatcherPriority.ContextIdle);
        }
    }

    private void ProcessLatestCompletedRound()
    {
        if (progressionController is null || session.History.Count == 0)
        {
            return;
        }

        RoundHistoryEntry latest = session.History[^1];
        ProgressionAward award = progressionController.ProcessRound(latest);
        if (award.PlayerExperience <= 0 && award.SeasonExperience <= 0)
        {
            return;
        }

        RefreshProfileButton();

        if (award.HasLevelUp)
        {
            AnimateLevelUpCue();
            string levels = award.LevelsGained.Count == 1
                ? $"Level {award.LevelsGained[0]}"
                : $"Level {award.LevelsGained[0]}–{award.LevelsGained[^1]}";
            StatusText.Text += $" · {levels}! +{award.CoinsEarned:N0} cosmetic coins";
        }
        else if (award.SeasonTiersGained > 0)
        {
            SeasonProgressState season = progressionController.Service.GetOrCreateSeasonProgress(progressionController.Profile.ActiveSeasonId);
            StatusText.Text += $" · Season tier {season.Tier}";
        }
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (progressionController is null)
        {
            return;
        }

        OpenPlayerProfile();
    }

    private void RefreshProfileButton()
    {
        if (profileButton is null || progressionController is null)
        {
            return;
        }

        PlayerProfileState profile = progressionController.Profile;
        CosmeticDefinition title = CosmeticCatalog.Get(profile.EquippedTitleId);
        CosmeticDefinition frame = CosmeticCatalog.Get(profile.EquippedFrameId);

        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(ProgressionWindow.CreatePortraitVisual(CosmeticCatalog.Get(profile.EquippedPortraitId), 38));
        var labels = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock { Text = profile.DisplayName, FontWeight = FontWeights.Bold, FontSize = 14, Width = 132, TextTrimming = TextTrimming.CharacterEllipsis });
        var detail = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(84, 225, 183)) };
        detail.Text = $"LV {profile.Level} · ◆ {profile.Coins:N0}";
        labels.Children.Add(detail);
        labels.Children.Add(new TextBlock { Text = title.Name, FontSize = 10, Foreground = CosmeticAppearance.TitleBrush(title),
            MaxWidth = 132, TextTrimming = TextTrimming.CharacterEllipsis });
        content.Children.Add(labels);
        if (profile.Prestige > 0)
        {
            content.Children.Add(CreatePrestigeBadge(profile.Prestige));
        }
        profileButton.Content = content;
        profileButton.ToolTip = $"{profile.DisplayName} · {title.Name} · Player Prestige {profile.Prestige} · Open profile";
        ApplyHeaderFrame(frame);
    }

    private static FrameworkElement CreatePrestigeBadge(int prestige)
    {
        var outer = new Border
        {
            Width = 43,
            Height = 38,
            Margin = new Thickness(10, 0, 0, 0),
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1.5),
            BorderBrush = new LinearGradientBrush(Color.FromRgb(255, 226, 137), Color.FromRgb(150, 93, 31), 35),
            Background = new LinearGradientBrush(Color.FromRgb(45, 31, 19), Color.FromRgb(18, 20, 25), 65),
            ToolTip = $"Player Prestige {prestige}",
            Effect = new DropShadowEffect { Color = Color.FromRgb(232, 180, 77), BlurRadius = 10, ShadowDepth = 0, Opacity = .42 },
        };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = "✦",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 222, 129)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, -2, 0, -3),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "P" + prestige,
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(240, 207, 133)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        outer.Child = stack;
        return outer;
    }

    private void ApplyHeaderFrame(CosmeticDefinition frame)
    {
        if (profileButton is null)
        {
            return;
        }

        if (frame.VisualVariant.StartsWith("static-", StringComparison.Ordinal))
        {
            profileButton.BorderBrush = new SolidColorBrush(CosmeticAppearance.StandardFrame(frame.VisualVariant));
            profileButton.Effect = null;
            return;
        }
        (Color first, Color second, string motion) = frame.VisualVariant switch
        {
            "emerald-breathe" => (Color.FromRgb(65, 150, 105), Color.FromRgb(112, 220, 156), "pulse"),
            "midnight-sweep" => (Color.FromRgb(60, 86, 142), Color.FromRgb(108, 153, 225), "sweep"),
            "burgundy-ember" => (Color.FromRgb(127, 59, 72), Color.FromRgb(211, 87, 104), "pulse"),
            "gold-shimmer" => (Color.FromRgb(139, 100, 39), Color.FromRgb(238, 198, 91), "sweep"),
            "aurora-drift" => (Color.FromRgb(65, 208, 177), Color.FromRgb(152, 87, 218), "sweep"),
            "crimson-heartbeat" => (Color.FromRgb(117, 34, 49), Color.FromRgb(235, 82, 104), "pulse"),
            "starlight-orbit" => (Color.FromRgb(125, 159, 207), Color.FromRgb(223, 235, 245), "sweep"),
            "royal-violet" => (Color.FromRgb(118, 73, 164), Color.FromRgb(224, 184, 91), "sweep"),
            "obsidian-pulse" => (Color.FromRgb(24, 31, 37), Color.FromRgb(54, 224, 205), "pulse"),
            "prestige-bronze" => (Color.FromRgb(136, 78, 42), Color.FromRgb(208, 142, 82), "pulse"),
            "prestige-silver" => (Color.FromRgb(125, 137, 147), Color.FromRgb(225, 234, 240), "sweep"),
            "prestige-gold" => (Color.FromRgb(151, 113, 36), Color.FromRgb(255, 220, 102), "sweep"),
            "midnight-line" => (Color.FromRgb(67, 87, 157), Color.FromRgb(118, 146, 229), "sweep"),
            "neon-noir" => (Color.FromRgb(59, 210, 188), Color.FromRgb(197, 78, 219), "sweep"),
            "eclipse-thread" => (Color.FromRgb(74, 96, 135), Color.FromRgb(170, 96, 187), "sweep"),
            "eclipse-halo" => (Color.FromRgb(55, 155, 168), Color.FromRgb(206, 107, 178), "pulse"),
            "eclipse-crown" => (Color.FromRgb(191, 137, 55), Color.FromRgb(136, 79, 190), "sweep"),
            _ => (Color.FromRgb(82, 105, 90), Color.FromRgb(82, 105, 90), "none"),
        };

        var brush = new LinearGradientBrush(first, second, 0);
        profileButton.BorderBrush = brush;
        var glow = new DropShadowEffect
        {
            Color = second,
            BlurRadius = motion == "none" ? 0 : 9,
            ShadowDepth = 0,
            Opacity = motion == "none" ? 0 : 0.22,
        };
        profileButton.Effect = motion == "none" ? null : glow;

        if (motion == "pulse")
        {
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.12, 0.38, TimeSpan.FromSeconds(2.2))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });
        }
        else if (motion == "sweep")
        {
            brush.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(0.2, 0.88, TimeSpan.FromSeconds(3.8))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });
        }
    }
}
