using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private void RefreshChallenges()
    {
        if (!collectionsInitialized) return;
        PlayerProfileState profile = controller.Profile;
        var claimedIds = profile.ClaimedChallengeIds.ToHashSet(StringComparer.Ordinal);
        var entries = ChallengeRegistry.All.Select(challenge => new
        {
            Challenge = challenge,
            Complete = ChallengeRegistry.Complete(profile, challenge),
            Claimed = claimedIds.Contains(challenge.Id),
        }).ToArray();
        var ready = entries.Where(entry => entry.Complete && !entry.Claimed).ToArray();
        long readyCoins = ready.Sum(entry => entry.Challenge.CoinReward);
        ChallengeSummaryText.Text = $"{entries.Count(entry => entry.Complete):N0} / {entries.Length:N0} complete · {entries.Count(entry => entry.Claimed):N0} claimed · {ready.Length:N0} ready · ◆ {readyCoins:N0} available";
        ClaimAllButton.Content = ready.Length == 0 ? "Claim All · none ready" : $"Claim All · ◆ {readyCoins:N0}";
        ClaimAllButton.IsEnabled = ready.Length > 0;
        if (string.IsNullOrWhiteSpace(ChallengeStatusText.Text))
            ChallengeStatusText.Text = "All ready rewards can be claimed together, including Adventure achievements.";

        ChallengeCategory? category = (ChallengeCategoryFilter.SelectedItem as FilterChoice)?.Value is ChallengeCategory selected ? selected : null;
        var filtered = entries.Where(entry => (!category.HasValue || entry.Challenge.Category == category)
            && MatchesSearch(ChallengeSearchBox.Text, entry.Challenge.Name, entry.Challenge.Description, ChallengeCategoryLabel(entry.Challenge), entry.Challenge.IsEndgame ? "Endgame" : "Milestone")
            && (ChallengeStatusFilter.SelectedIndex switch
            {
                1 => entry.Complete && !entry.Claimed,
                2 => !entry.Complete && !entry.Claimed,
                3 => entry.Claimed,
                4 => !entry.Claimed,
                _ => true,
            }))
            .OrderBy(entry => entry.Claimed ? 2 : entry.Complete ? 0 : 1)
            .ThenBy(entry => AdventureChallengeCatalog.IsAdventure(entry.Challenge) ? "Adventure" : entry.Challenge.Category.ToString())
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
        decimal progress = ChallengeRegistry.Progress(controller.Profile, challenge);
        bool complete = progress >= challenge.Goal;
        bool claimed = controller.Profile.ClaimedChallengeIds.Contains(challenge.Id, StringComparer.Ordinal);
        EmpireAchievementBoost? empireReward = EmpireAchievementBoostCatalog.All.FirstOrDefault(boost => boost.ChallengeId == challenge.Id);
        var border = PanelBorder();
        border.Padding = new Thickness(18, 15, 18, 15);
        border.Margin = new Thickness(0, 0, 12, 12);
        border.BorderBrush = claimed ? new SolidColorBrush(Rgb(48, 83, 65)) : complete ? CoinBrush : new SolidColorBrush(Rgb(49, 90, 67));
        var grid = new Grid { MinHeight = 140 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = ChallengeCategoryLabel(challenge).ToUpperInvariant() + (challenge.IsEndgame ? " · ENDGAME" : ""), FontSize = 9, Foreground = CoinMutedBrush, FontWeight = FontWeights.Bold });
        text.Children.Add(new TextBlock { Text = challenge.Name, FontFamily = new FontFamily("Georgia"), FontSize = 19, Foreground = complete ? CoinBrush : new SolidColorBrush(Rgb(244, 225, 184)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 7) });
        grid.Children.Add(text);
        var description = new TextBlock { Text = challenge.Description, Foreground = MutedBrush, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(description, 1); grid.Children.Add(description);
        var progressStack = new StackPanel { Margin = new Thickness(0, 0, 0, 13) };
        progressStack.Children.Add(new TextBlock { Text = $"{FormatChallengeValue(challenge, Math.Min(progress, challenge.Goal))} / {FormatChallengeValue(challenge, challenge.Goal)}", FontSize = 11, Foreground = complete ? PositiveBrush : MutedBrush });
        progressStack.Children.Add(new ProgressBar
        {
            Minimum = 0, Maximum = Math.Max(1d, (double)challenge.Goal),
            Value = Math.Clamp((double)progress, 0, Math.Max(1d, (double)challenge.Goal)),
            Height = 7, Margin = new Thickness(0, 6, 0, 0), Foreground = complete ? CoinBrush : PositiveBrush,
            Background = new SolidColorBrush(Rgb(18, 48, 34)),
        });
        Grid.SetRow(progressStack, 2); grid.Children.Add(progressStack);
        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(footer, 3); grid.Children.Add(footer);

        var rewardStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        rewardStack.Children.Add(CoinText($"◆ {challenge.CoinReward:N0}", 12, FontWeights.Bold));
        rewardStack.Children.Add(new TextBlock { Text = $"+ {ProgressionService.ChallengePack(challenge)} pack", FontSize = 10, Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap });
        if (empireReward is not null)
        {
            string target = empireReward.RoomId is null
                ? "ALL ROOMS"
                : EmpireCatalog.Find(empireReward.RoomId)?.Name.ToUpperInvariant() ?? empireReward.RoomId.ToUpperInvariant();
            rewardStack.Children.Add(new TextBlock
            {
                Text = $"EMPIRE +{empireReward.Percent:P1}\n{target}",
                FontSize = 9,
                Foreground = PositiveBrush,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        footer.Children.Add(rewardStack);

        var button = new Button { Tag = challenge.Id, Content = claimed ? "Claimed" : complete ? "Claim" : "In progress", IsEnabled = complete && !claimed, Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 6, 8, 6), VerticalAlignment = VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(button, $"{button.Content}: {challenge.Name}");
        button.Click += ChallengeClaim_Click;
        Grid.SetColumn(button, 1);
        footer.Children.Add(button);
        border.Child = grid;
        return border;
    }

    private static string ChallengeCategoryLabel(ChallengeDefinition challenge) =>
        AdventureChallengeCatalog.IsAdventure(challenge) ? "Adventure" : CategoryName(challenge.Category);

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
