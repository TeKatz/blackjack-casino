using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private PlayerProgressionController? progressionController;
    private Button? profileButton;
    private Grid? profileButtonHost;
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
        progressionController = new PlayerProgressionController();
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

        Grid.SetColumn(versionPanel, 3);

        profileButtonHost = new Grid
        {
            Width = 190,
            Height = 40,
            Margin = new Thickness(0, 0, 22, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        profileButton = new Button
        {
            MinWidth = 190,
            Width = 190,
            Height = 40,
            Padding = new Thickness(13, 6, 13, 6),
            Style = (Style)FindResource("MutedButton"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        profileButton.Click += ProfileButton_Click;
        profileButtonHost.Children.Add(profileButton);

        var sweepClip = new Canvas
        {
            Width = 190,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Background = Brushes.Transparent,
        };

        profileLevelUpSweepTransform = new TranslateTransform(-92, 0);
        profileLevelUpSweep = new Border
        {
            Width = 86,
            Height = 32,
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
                    new GradientStop(Color.FromArgb(92, 225, 71, 91), 0.32),
                    new GradientStop(Color.FromArgb(178, 255, 102, 118), 0.5),
                    new GradientStop(Color.FromArgb(92, 225, 71, 91), 0.68),
                    new GradientStop(Color.FromArgb(0, 214, 63, 82), 1),
                },
            },
        };
        Canvas.SetTop(profileLevelUpSweep, 4);
        sweepClip.Children.Add(profileLevelUpSweep);
        Panel.SetZIndex(sweepClip, 2);
        profileButtonHost.Children.Add(sweepClip);

        Grid.SetColumn(profileButtonHost, 2);
        headerGrid.Children.Add(profileButtonHost);
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
            StatusText.Text += $" · {levels}! +{award.CoinsEarned:N0} coins";
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

        var window = new ProgressionWindow(progressionController)
        {
            Owner = this,
        };
        _ = window.ShowDialog();
        RefreshProfileButton();
    }

    private void RefreshProfileButton()
    {
        if (profileButton is null || progressionController is null)
        {
            return;
        }

        PlayerProfileState profile = progressionController.Profile;
        CosmeticDefinition title = CosmeticCatalog.Get(profile.EquippedTitleId);
        profileButton.Content = $"P{profile.Prestige} · LV {profile.Level}    ◆ {profile.Coins:N0}";
        profileButton.ToolTip = $"{title.Name} · Open profile, stats, season and shop";
        profileButton.BorderBrush = ProfileFrameBrush(profile.EquippedFrameId);
    }

    private static Brush ProfileFrameBrush(string frameId) => frameId switch
    {
        "frame-prestige-2" => Brush("#B77A45"),
        "frame-prestige-5" => Brush("#BFC9D1"),
        "frame-prestige-10" => Brush("#E1C15A"),
        "frame-season-midnight-10" => Brush("#667FD1"),
        "frame-season-midnight-35" => Brush("#B05ECF"),
        "frame-emerald" => Brush("#4D9A72"),
        "frame-midnight" => Brush("#526E9A"),
        "frame-burgundy" => Brush("#9C5B66"),
        "frame-gold-trim" => Brush("#D4A84F"),
        _ => Brush("#52695A"),
    };
}
