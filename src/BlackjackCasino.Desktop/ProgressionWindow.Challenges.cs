using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private void RefreshChallenges()
    {
        if (!collectionsInitialized) return;
        PlayerProfileState profile = controller.Profile;
        var claimedIds = profile.ClaimedChallengeIds.ToHashSet(StringComparer.Ordinal);
        var entries = ChallengeCatalog.All.Select(challenge => new
        {
            Challenge = challenge,
            Complete = ChallengeCatalog.Complete(profile, challenge),
            Claimed = claimedIds.Contains(challenge.Id),
        }).ToArray();
        var ready = entries.Where(entry => entry.Complete && !entry.Claimed).ToArray();
        long readyCoins = ready.Sum(entry => entry.Challenge.CoinReward);
        ChallengeSummaryText.Text = $"{entries.Count(entry => entry.Complete):N0} / {entries.Length:N0} complete · {entries.Count(entry => entry.Claimed):N0} claimed · {ready.Length:N0} ready · ◆ {readyCoins:N0} available";
        ClaimAllButton.Content = ready.Length == 0 ? "Claim All · none ready" : $"Claim All · ◆ {readyCoins:N0}";
        ClaimAllButton.IsEnabled = ready.Length > 0;
        if (string.IsNullOrWhiteSpace(ChallengeStatusText.Text))
            ChallengeStatusText.Text = "All ready rewards can be claimed together, across every filter and page.";

        ChallengeCategory? category = (ChallengeCategoryFilter.SelectedItem as FilterChoice)?.Value is ChallengeCategory selected ? selected : null;
        var filtered = entries.Where(entry => (!category.HasValue || entry.Challenge.Category == category)
            && MatchesSearch(ChallengeSearchBox.Text, entry.Challenge.Name, entry.Challenge.Description, CategoryName(entry.Challenge.Category), entry.Challenge.IsEndgame ? "Endgame" : "Milestone")
            && (ChallengeStatusFilter.SelectedIndex switch
            {
                1 => entry.Complete && !entry.Claimed,
                2 => !entry.Complete && !entry.Claimed,
                3 => entry.Claimed,
                4 => !entry.Claimed,
                _ => true,
            }))
            .OrderBy(entry => entry.Claimed ? 2 : entry.Complete ? 0 : 1)
            .ThenBy(entry => entry.Challenge.Category)
            .ToArray();
        challengePage = Math.Clamp(challengePage, 0, LastPage(filtered.Length, ChallengePageSize));
        ChallengesPanel.Children.Clear();
        foreach (var entry in filtered.Skip(challengePage * ChallengePageSize).Take(ChallengePageSize))
            ChallengesPanel.Children.Add(BuildChallengeCard(entry.Challenge));
        if (filtered.Length == 0)
            ChallengesPanel.Children.Add(MutedText("No challenges match these filters. Try another status, game, or search, or choose Reset."));
        ChallengePageText.Text = PageDescription(filtered.Length, challengePage, ChallengePageSize, "challenges");
        ChallengePreviousButton.IsEnabled = challengePage > 0;
        ChallengeNextButton.IsEnabled = challengePage < LastPage(filtered.Length, ChallengePageSize);
    }

    private FrameworkElement BuildChallengeCard(ChallengeDefinition challenge)
    {
        decimal progress = ChallengeCatalog.Progress(controller.Profile, challenge);
        bool complete = progress >= challenge.Goal;
        bool claimed = controller.Profile.ClaimedChallengeIds.Contains(challenge.Id, StringComparer.Ordinal);
        var border = PanelBorder();
        border.Padding = new Thickness(12, 8, 12, 8);
        border.Margin = new Thickness(0, 0, 4, 6);
        border.BorderBrush = claimed ? new SolidColorBrush(Rgb(48, 83, 65)) : complete ? CoinMutedBrush : new SolidColorBrush(Rgb(49, 90, 67));
        var grid = new Grid { MinHeight = 48 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
        text.Children.Add(new TextBlock { Text = $"{CategoryName(challenge.Category).ToUpperInvariant()} · {(challenge.IsEndgame ? "ENDGAME · " : "")}{challenge.Name}", FontWeight = FontWeights.SemiBold, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = challenge.Name });
        text.Children.Add(new TextBlock { Text = challenge.Description, Foreground = MutedBrush, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
        grid.Children.Add(text);
        var progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
        progressStack.Children.Add(new TextBlock { Text = $"{FormatChallengeValue(challenge, Math.Min(progress, challenge.Goal))} / {FormatChallengeValue(challenge, challenge.Goal)}", FontSize = 11, Foreground = complete ? PositiveBrush : MutedBrush });
        progressStack.Children.Add(new ProgressBar
        {
            Minimum = 0, Maximum = Math.Max(1d, (double)challenge.Goal),
            Value = Math.Clamp((double)progress, 0, Math.Max(1d, (double)challenge.Goal)),
            Height = 6, Margin = new Thickness(0, 6, 0, 0), Foreground = complete ? CoinBrush : PositiveBrush,
            Background = new SolidColorBrush(Rgb(18, 48, 34)),
        });
        Grid.SetColumn(progressStack, 1);
        grid.Children.Add(progressStack);
        var reward = CoinText($"◆ {challenge.CoinReward:N0}", 12, FontWeights.Bold);
        reward.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(reward, 2);
        grid.Children.Add(reward);
        var button = new Button { Tag = challenge.Id, Content = claimed ? "Claimed" : complete ? "Claim" : "In progress", IsEnabled = complete && !claimed, Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 6, 8, 6), VerticalAlignment = VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(button, $"{button.Content}: {challenge.Name}");
        button.Click += ChallengeClaim_Click;
        Grid.SetColumn(button, 3);
        grid.Children.Add(button);
        border.Child = grid;
        return border;
    }

    private void ChallengeClaim_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id }) return;
        ChallengeClaimResult result = controller.ClaimChallenge(id);
        ChallengeStatusText.Text = result.Message;
        RefreshAll();
    }

    private void ClaimAllChallenges_Click(object sender, RoutedEventArgs e)
    {
        ChallengeClaimAllResult result = controller.ClaimAllChallenges();
        ChallengeStatusText.Text = result.Message;
        RefreshAll();
    }
}
