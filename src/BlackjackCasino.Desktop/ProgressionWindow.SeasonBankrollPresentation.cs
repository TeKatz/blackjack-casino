using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private bool seasonBankrollPresentationInitialized;
    private bool seasonBankrollSelectorHooked;
    private bool seasonBankrollActivateHooked;

    private void InitializeSeasonBankrollPresentation()
    {
        if (!seasonBankrollPresentationInitialized)
        {
            ProgressionTabs.SelectionChanged += SeasonBankrollTabs_SelectionChanged;
            seasonBankrollPresentationInitialized = true;
        }

        HookSeasonBankrollControls();
        ApplySeasonBankrollPresentation();
    }

    private void SeasonBankrollTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, ProgressionTabs)
            || ProgressionTabs.SelectedItem is not TabItem tab
            || !string.Equals(tab.Tag as string, "Season Pass", StringComparison.Ordinal))
        {
            return;
        }

        // The season-library handler also queues its render. This handler is registered after it,
        // so our queued pass annotates the freshly rebuilt controls rather than stale visuals.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            HookSeasonBankrollControls();
            ApplySeasonBankrollPresentation();
        }));
    }

    private void HookSeasonBankrollControls()
    {
        if (seasonPassSelector is not null && !seasonBankrollSelectorHooked)
        {
            seasonPassSelector.SelectionChanged += SeasonBankrollSelector_SelectionChanged;
            seasonBankrollSelectorHooked = true;
        }

        if (seasonPassActivateButton is not null && !seasonBankrollActivateHooked)
        {
            seasonPassActivateButton.Click += SeasonBankrollActivateButton_Click;
            seasonBankrollActivateHooked = true;
        }
    }

    private void SeasonBankrollSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ApplySeasonBankrollPresentation();

    private void SeasonBankrollActivateButton_Click(object sender, RoutedEventArgs e)
        => ApplySeasonBankrollPresentation();

    private void ApplySeasonBankrollPresentation()
    {
        if (ProgressionTabs.SelectedItem is not TabItem tab
            || !string.Equals(tab.Tag as string, "Season Pass", StringComparison.Ordinal))
        {
            return;
        }

        SeasonDefinition? season = SeasonCatalog.TryGet(viewedSeasonId)
            ?? SeasonCatalog.TryGet(controller.Profile.ActiveSeasonId);
        if (season is null || !season.RewardsEnabled)
        {
            return;
        }

        SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        if (next is { BankrollReward: > 0m })
        {
            AppendBankrollInline(SeasonNextRewardText, next.BankrollReward, " + ");
        }

        SeasonTierDefinition[] milestoneTiers = season.Tiers
            .Where(tier => tier.CosmeticId is not null || tier.Tier % 10 == 0)
            .ToArray();
        int milestoneCount = Math.Min(milestoneTiers.Length, SeasonMilestonesPanel.Children.Count);
        for (int index = 0; index < milestoneCount; index++)
        {
            SeasonTierDefinition tier = milestoneTiers[index];
            if (tier.BankrollReward <= 0m
                || SeasonMilestonesPanel.Children[index] is not Border { Child: StackPanel stack }
                || stack.Children.OfType<TextBlock>().Any(text => ContainsBankroll(text.Text)))
            {
                continue;
            }

            stack.Children.Add(new TextBlock
            {
                Text = $"+${tier.BankrollReward:N0} bankroll",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = PositiveBrush,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        int tierCount = Math.Min(season.Tiers.Count, SeasonRewardsPanel.Children.Count);
        for (int index = 0; index < tierCount; index++)
        {
            SeasonTierDefinition tier = season.Tiers[index];
            if (tier.BankrollReward <= 0m
                || SeasonRewardsPanel.Children[index] is not Border { Child: Grid grid })
            {
                continue;
            }

            TextBlock? rewardText = grid.Children
                .OfType<TextBlock>()
                .FirstOrDefault(text => Grid.GetColumn(text) == 2);
            if (rewardText is not null)
            {
                AppendBankrollInline(rewardText, tier.BankrollReward, " · ");
            }

            StackPanel? preview = grid.Children
                .OfType<StackPanel>()
                .FirstOrDefault(panel => Grid.GetColumn(panel) == 1);
            if (preview is not null
                && !preview.Children.OfType<Border>().Any(IsBankrollChip))
            {
                preview.Children.Insert(
                    Math.Min(1, preview.Children.Count),
                    CreateSeasonRewardChip(
                        "$",
                        tier.BankrollReward.ToString("N0"),
                        PositiveBrush,
                        $"${tier.BankrollReward:N0} bankroll"));
            }
        }
    }

    private static void AppendBankrollInline(TextBlock target, decimal amount, string prefix)
    {
        if (amount <= 0m || ContainsBankroll(target.Text))
        {
            return;
        }

        target.Inlines.Add(new Run($"{prefix}${amount:N0} bankroll")
        {
            Foreground = PositiveBrush,
            FontWeight = FontWeights.SemiBold,
        });
    }

    private static bool IsBankrollChip(Border chip)
        => chip.ToolTip is string tooltip && ContainsBankroll(tooltip);

    private static bool ContainsBankroll(string? text)
        => text?.Contains("bankroll", StringComparison.OrdinalIgnoreCase) == true;
}
