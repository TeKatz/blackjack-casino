using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Button? challengeHeaderButton;
    private TextBlock? challengeHeaderStatus;
    private ProgressBar? challengeHeaderProgress;
    private Border? challengeHeaderBadge;
    private TextBlock? challengeHeaderBadgeText;
    private DropShadowEffect? challengeHeaderGlow;
    private int lastChallengeReadyCount = -1;

    private void BuildChallengeHeaderButton(Panel headerActions)
    {
        if (challengeHeaderButton is not null) return;

        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock
        {
            Text = "CHALLENGES",
            FontSize = 8.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(190, 167, 116)),
            HorizontalAlignment = HorizontalAlignment.Left,
        });
        challengeHeaderStatus = new TextBlock
        {
            Text = "VIEW",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(225, 232, 224)),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        labels.Children.Add(challengeHeaderStatus);
        challengeHeaderProgress = new ProgressBar { Name = "HeaderChallengeProgress", Minimum = 0, Maximum = 1, Height = 3, Margin = new Thickness(0, 3, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(113, 221, 176)), Background = new SolidColorBrush(Color.FromRgb(34, 56, 45)) };
        labels.Children.Add(challengeHeaderProgress);

        challengeHeaderBadgeText = new TextBlock
        {
            Text = string.Empty,
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(27, 23, 14)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        challengeHeaderBadge = new Border
        {
            Width = 21,
            Height = 21,
            Margin = new Thickness(7, 0, 0, 0),
            CornerRadius = new CornerRadius(11),
            Background = new SolidColorBrush(Color.FromRgb(227, 188, 93)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(255, 224, 146)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = challengeHeaderBadgeText,
            Visibility = Visibility.Collapsed,
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        content.Children.Add(ProgressionArtwork.Create("Challenges", 32));
        labels.Margin = new Thickness(5, 0, 0, 0);
        content.Children.Add(labels);
        content.Children.Add(challengeHeaderBadge);

        challengeHeaderButton = new Button
        {
            Name = "HeaderChallengesButton",
            Height = 52,
            MinWidth = 88,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(9, 5, 9, 5),
            Style = (Style)FindResource("MutedButton"),
            Content = content,
            VerticalAlignment = VerticalAlignment.Center,
        };
        challengeHeaderButton.Click += ChallengeHeaderButton_Click;
        headerActions.Children.Add(challengeHeaderButton);
        RefreshChallengeHeaderButton();
    }

    private void ChallengeHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (progressionController is null || MetaPageOpen) return;
        OpenProgressionPage("Challenges");
    }

    private void RefreshChallengeHeaderButton()
    {
        if (challengeHeaderButton is null || challengeHeaderStatus is null || challengeHeaderBadge is null
            || challengeHeaderBadgeText is null || progressionController is null)
            return;

        LobbyProgressSnapshot snapshot = LobbyProgressSnapshot.From(progressionController.Profile);
        lobby?.SetProgress(snapshot);
        int readyCount = snapshot.ReadyCount;
        long readyCoins = snapshot.ReadyCoins;
        if (challengeHeaderProgress is not null) challengeHeaderProgress.Value = snapshot.ChallengeFraction;
        challengeHeaderStatus.Text = readyCount > 0 ? $"{readyCount} READY" : snapshot.ChallengeId is null ? "COMPLETE" : snapshot.ChallengeFraction.ToString("P0");
        challengeHeaderStatus.Foreground = new SolidColorBrush(readyCount > 0
            ? Color.FromRgb(246, 213, 133)
            : Color.FromRgb(225, 232, 224));
        challengeHeaderBadge.Visibility = readyCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        challengeHeaderBadgeText.Text = readyCount > 99 ? "99+" : readyCount.ToString();
        challengeHeaderButton.ToolTip = readyCount > 0
            ? $"{readyCount:N0} challenge reward{(readyCount == 1 ? string.Empty : "s")} ready · ◆ {readyCoins:N0} cosmetic coins · Open Challenges"
            : snapshot.ChallengeId is null ? "All challenges complete" : $"{snapshot.ChallengeName}: {snapshot.ChallengeProgress:N0} / {snapshot.ChallengeGoal:N0} · Open Challenges";

        if (readyCount == lastChallengeReadyCount) return;
        bool hadReady = lastChallengeReadyCount > 0;
        lastChallengeReadyCount = readyCount;

        if (readyCount <= 0)
        {
            if (challengeHeaderGlow is not null)
            {
                challengeHeaderGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
                challengeHeaderGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
            }
            challengeHeaderGlow = null;
            challengeHeaderButton.BeginAnimation(OpacityProperty, null);
            challengeHeaderButton.ClearValue(BorderBrushProperty);
            challengeHeaderButton.ClearValue(EffectProperty);
            return;
        }

        Color gold = Color.FromRgb(229, 188, 91);
        challengeHeaderButton.BorderBrush = new SolidColorBrush(gold);
        challengeHeaderGlow = new DropShadowEffect
        {
            Color = gold,
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = 0.28,
        };
        challengeHeaderButton.Effect = challengeHeaderGlow;

        if (!AnimationsEnabled) return;

        challengeHeaderGlow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.16, 0.48, TimeSpan.FromMilliseconds(1050))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });


        // A newly completed challenge gets one gentle opacity cue; afterwards only the glow remains.
        if (!hadReady)
        {
            challengeHeaderButton.BeginAnimation(OpacityProperty, new DoubleAnimation(0.78, 1.0, TimeSpan.FromMilliseconds(420))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        }
    }
}
