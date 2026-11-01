using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private ComboBox? seasonPassSelector;
    private TextBlock? seasonPassStateText;
    private bool syncingSeasonPassSelector;
    private bool seasonPassTabHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        EnsureSeasonPassSwitcher();
        RefreshSeasonPassSwitcher();
        ApplySeasonPassPresentation();

        if (!seasonPassTabHooked)
        {
            ProgressionTabs.SelectionChanged += SeasonPassTabs_SelectionChanged;
            seasonPassTabHooked = true;
        }
    }

    private void EnsureSeasonPassSwitcher()
    {
        if (seasonPassSelector is not null || SeasonTitleText.Parent is not StackPanel overview)
        {
            return;
        }

        if (overview.Children.Count > 0 && overview.Children[0] is TextBlock caption)
        {
            caption.Text = "SEASON PASS LIBRARY";
        }

        var selector = new ComboBox
        {
            MinWidth = 270,
            MaxWidth = 360,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 7, 18, 7),
            ToolTip = "Choose which pass is displayed and receives Season XP. Progress is saved separately for every pass.",
        };
        System.Windows.Automation.AutomationProperties.SetName(selector, "Season pass");
        selector.SelectionChanged += SeasonPassSelector_SelectionChanged;

        seasonPassStateText = new TextBlock
        {
            FontSize = 10,
            Foreground = MutedBrush,
            Margin = new Thickness(0, 0, 18, 6),
            TextWrapping = TextWrapping.Wrap,
        };

        overview.Children.Insert(1, selector);
        overview.Children.Insert(2, seasonPassStateText);
        seasonPassSelector = selector;
    }

    private void RefreshSeasonPassSwitcher()
    {
        if (seasonPassSelector is null)
        {
            return;
        }

        syncingSeasonPassSelector = true;
        try
        {
            seasonPassSelector.Items.Clear();
            foreach (SeasonDefinition season in SeasonCatalog.All)
            {
                SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
                string state = season.Archived
                    ? "Legacy"
                    : string.Equals(season.Id, SeasonCatalog.Current.Id, StringComparison.Ordinal)
                        ? "Current"
                        : "Available";
                seasonPassSelector.Items.Add(new SeasonPassChoice(
                    season.Id,
                    $"{season.Name} · {state} · Tier {progress.Tier}/{season.Tiers.Count}"));
            }

            seasonPassSelector.SelectedItem = seasonPassSelector.Items
                .OfType<SeasonPassChoice>()
                .FirstOrDefault(item => string.Equals(item.Id, controller.Profile.ActiveSeasonId, StringComparison.Ordinal));
        }
        finally
        {
            syncingSeasonPassSelector = false;
        }
    }

    private void SeasonPassSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncingSeasonPassSelector || seasonPassSelector?.SelectedItem is not SeasonPassChoice choice)
        {
            return;
        }

        if (!controller.Service.SelectSeason(choice.Id))
        {
            RefreshSeasonPassSwitcher();
            return;
        }

        controller.SaveAll();
        RefreshSeason();
        RefreshSeasonPassSwitcher();
        ApplySeasonPassPresentation();
    }

    private void SeasonPassTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, ProgressionTabs)
            || ProgressionTabs.SelectedItem is not TabItem tab
            || !string.Equals(tab.Tag as string, "Season Pass", StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            EnsureSeasonPassSwitcher();
            RefreshSeasonPassSwitcher();
            ApplySeasonPassPresentation();
        }));
    }

    private void ApplySeasonPassPresentation()
    {
        if (SeasonCatalog.TryGet(controller.Profile.ActiveSeasonId) is not { } season)
        {
            return;
        }

        SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
        if (seasonPassStateText is not null)
        {
            seasonPassStateText.Text = season.Archived
                ? "LEGACY PASS · PLAYABLE · progress remains independent"
                : "CURRENT PASS · active Season XP target · progress remains independent";
        }

        SetSeasonSectionLabels(season.RewardsEnabled);
        if (season.RewardsEnabled)
        {
            return;
        }

        RenderRewardPlaceholderTrack(season, progress);
    }

    private void SetSeasonSectionLabels(bool rewardsEnabled)
    {
        if (SeasonNextRewardText.Parent is StackPanel nextPanel
            && nextPanel.Children.OfType<TextBlock>().FirstOrDefault() is { } nextCaption)
        {
            nextCaption.Text = rewardsEnabled ? "NEXT REWARD" : "NEXT LEVEL";
        }

        if (SeasonRewardsPanel.Parent is ScrollViewer rewardsScroll
            && rewardsScroll.Parent is Grid rewardsGrid
            && rewardsGrid.Children.OfType<TextBlock>().FirstOrDefault() is { } rewardsCaption)
        {
            rewardsCaption.Text = rewardsEnabled ? "REWARD TRACK" : "PROGRESSION TRACK";
        }

        if (SeasonMilestonesPanel.Parent is ScrollViewer milestonesScroll
            && milestonesScroll.Parent is Grid milestonesGrid
            && milestonesGrid.Children.OfType<StackPanel>().FirstOrDefault() is { } milestoneHeader)
        {
            TextBlock[] labels = milestoneHeader.Children.OfType<TextBlock>().ToArray();
            if (labels.Length > 0)
            {
                labels[0].Text = rewardsEnabled ? "MILESTONE REWARDS" : "XP MILESTONES";
            }
            if (labels.Length > 1)
            {
                labels[1].Text = rewardsEnabled ? "Special unlocks at key tiers" : "Long-form progression checkpoints";
            }
        }
    }

    private void RenderRewardPlaceholderTrack(SeasonDefinition season, SeasonProgressState progress)
    {
        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        SeasonNextRewardText.Inlines.Clear();
        SeasonNextRewardText.Text = next is null ? "Pass complete" : $"Tier {next.Tier}";
        SeasonNextRewardXpText.Text = next is null
            ? $"All {season.Tiers.Count} tiers reached · rewards will be assigned separately."
            : $"{Math.Max(0, next.RequiredExperience - progress.Experience):N0} XP to next tier · reward slot reserved";

        SeasonRewardsPanel.Children.Clear();
        long previousRequirement = 0;
        foreach (SeasonTierDefinition tier in season.Tiers)
        {
            bool reached = progress.Tier >= tier.Tier;
            bool isNext = next?.Tier == tier.Tier;
            long tierCost = tier.RequiredExperience - previousRequirement;
            previousRequirement = tier.RequiredExperience;

            var border = new Border
            {
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(2, 3, 2, 3),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = reached
                    ? new SolidColorBrush(Color.FromRgb(58, 100, 73))
                    : isNext
                        ? new SolidColorBrush(Color.FromRgb(116, 126, 184))
                        : new SolidColorBrush(Color.FromRgb(47, 81, 64)),
                Background = isNext
                    ? new SolidColorBrush(Color.FromRgb(14, 31, 31))
                    : new SolidColorBrush(Color.FromRgb(8, 26, 18)),
                Opacity = reached || isNext ? 1 : 0.68,
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var badge = new Border
            {
                Background = reached
                    ? new SolidColorBrush(Color.FromRgb(22, 66, 44))
                    : isNext
                        ? new SolidColorBrush(Color.FromRgb(39, 48, 82))
                        : new SolidColorBrush(Color.FromRgb(13, 38, 26)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = $"T{tier.Tier}",
                    FontWeight = FontWeights.Bold,
                    Foreground = reached
                        ? PositiveBrush
                        : isNext
                            ? new SolidColorBrush(Color.FromRgb(166, 177, 232))
                            : MutedBrush,
                },
            };
            grid.Children.Add(badge);

            var placeholder = new TextBlock
            {
                Text = "Reward slot reserved",
                Foreground = MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(placeholder, 1);
            grid.Children.Add(placeholder);

            var xp = new TextBlock
            {
                Text = $"+{tierCost:N0} XP · {tier.RequiredExperience:N0} total",
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(xp, 2);
            grid.Children.Add(xp);

            var status = new TextBlock
            {
                Text = reached ? "REACHED" : isNext ? "NEXT" : "LOCKED",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = reached
                    ? PositiveBrush
                    : isNext
                        ? new SolidColorBrush(Color.FromRgb(166, 177, 232))
                        : MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(status, 3);
            grid.Children.Add(status);

            border.Child = grid;
            SeasonRewardsPanel.Children.Add(border);
        }

        SeasonMilestonesPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers.Where(tier => tier.Tier % 10 == 0))
        {
            bool reached = progress.Tier >= tier.Tier;
            var milestone = new Border
            {
                Width = 160,
                MinHeight = 76,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = reached ? PositiveBrush : new SolidColorBrush(Color.FromRgb(53, 78, 65)),
                Background = reached
                    ? new SolidColorBrush(Color.FromRgb(18, 54, 37))
                    : new SolidColorBrush(Color.FromRgb(9, 29, 20)),
                Opacity = reached ? 1 : 0.78,
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"TIER {tier.Tier}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = reached ? PositiveBrush : MutedBrush,
            });
            stack.Children.Add(new TextBlock
            {
                Text = $"{tier.RequiredExperience:N0} XP total",
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = FontWeights.SemiBold,
            });
            stack.Children.Add(new TextBlock
            {
                Text = reached ? "REACHED" : "CHECKPOINT",
                FontSize = 9,
                Foreground = MutedBrush,
                Margin = new Thickness(0, 3, 0, 0),
            });
            milestone.Child = stack;
            SeasonMilestonesPanel.Children.Add(milestone);
        }
    }

    private sealed record SeasonPassChoice(string Id, string Label)
    {
        public override string ToString() => Label;
    }
}
