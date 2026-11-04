using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private ComboBox? seasonPassSelector;
    private Button? seasonPassActivateButton;
    private TextBlock? seasonPassStateText;
    private string? viewedSeasonId;
    private bool syncingSeasonPassSelector;
    private bool seasonPassTabHooked;

    /// <summary>
    /// Initializes the pass library from the loaded progression content. This must be tied to
    /// ProgressionRoot rather than Window.OnContentRendered because the casino embeds the root
    /// without ever displaying this Window instance.
    /// </summary>
    private void InitializeSeasonPassLibrary()
    {
        EnsureSeasonPassSwitcher();

        if (!seasonPassTabHooked)
        {
            ProgressionTabs.SelectionChanged += SeasonPassTabs_SelectionChanged;
            seasonPassTabHooked = true;
        }

        if (SeasonCatalog.TryGet(viewedSeasonId) is null)
        {
            viewedSeasonId = controller.Profile.ActiveSeasonId;
        }

        RefreshSeasonPassSwitcher();
        RenderViewedSeason();
    }

    private void EnsureSeasonPassSwitcher()
    {
        if (seasonPassSelector is not null || SeasonTitleText.Parent is not StackPanel overview)
        {
            return;
        }

        if (overview.Children.Count > 0 && overview.Children[0] is TextBlock caption)
        {
            caption.Text = SeasonTextLocalization.Ui("PassLibrary", "SEASON PASS LIBRARY");
        }

        var selector = new ComboBox
        {
            MinWidth = 280,
            MaxWidth = 390,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Browse any pass without changing your active pass. Use Activate Pass to choose which pass receives new Season XP.",
        };
        System.Windows.Automation.AutomationProperties.SetName(selector, "Season pass library");
        selector.SelectionChanged += SeasonPassSelector_SelectionChanged;

        var activateButton = new Button
        {
            Content = SeasonTextLocalization.Ui("ActivatePass", "ACTIVATE PASS"),
            MinWidth = 132,
            Margin = new Thickness(10, 0, 18, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Make the displayed pass your only active Season XP target.",
        };
        System.Windows.Automation.AutomationProperties.SetName(activateButton, "Activate season pass");
        activateButton.Click += SeasonPassActivateButton_Click;

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 7, 0, 7),
        };
        controls.Children.Add(selector);
        controls.Children.Add(activateButton);

        seasonPassStateText = new TextBlock
        {
            FontSize = 10,
            Foreground = MutedBrush,
            Margin = new Thickness(0, 0, 18, 6),
            TextWrapping = TextWrapping.Wrap,
        };

        overview.Children.Insert(1, controls);
        overview.Children.Insert(2, seasonPassStateText);
        seasonPassSelector = selector;
        seasonPassActivateButton = activateButton;
    }

    private void RefreshSeasonPassSwitcher()
    {
        if (seasonPassSelector is null)
        {
            return;
        }

        string selectedId = SeasonCatalog.TryGet(viewedSeasonId)?.Id ?? controller.Profile.ActiveSeasonId;
        string activeId = controller.Profile.ActiveSeasonId;

        syncingSeasonPassSelector = true;
        try
        {
            seasonPassSelector.Items.Clear();
            foreach (SeasonDefinition season in SeasonCatalog.All)
            {
                SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
                string catalogState = season.Archived
                    ? SeasonTextLocalization.Ui("Legacy", "Legacy")
                    : string.Equals(season.Id, SeasonCatalog.Current.Id, StringComparison.Ordinal)
                        ? SeasonTextLocalization.Ui("Current", "Current")
                        : SeasonTextLocalization.Ui("Available", "Available");
                string activeState = string.Equals(season.Id, activeId, StringComparison.Ordinal) ? " · " + SeasonTextLocalization.Ui("Active", "ACTIVE") : string.Empty;

                seasonPassSelector.Items.Add(new SeasonPassChoice(
                    season.Id,
                    $"{SeasonTextLocalization.Name(season)} · {catalogState} · {SeasonTextLocalization.Ui("Tier", "Tier")} {progress.Tier}/{season.Tiers.Count}{activeState}"));
            }

            seasonPassSelector.SelectedItem = seasonPassSelector.Items
                .OfType<SeasonPassChoice>()
                .FirstOrDefault(item => string.Equals(item.Id, selectedId, StringComparison.Ordinal));
        }
        finally
        {
            syncingSeasonPassSelector = false;
        }

        UpdateSeasonActivationPresentation();
    }

    private void SeasonPassSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncingSeasonPassSelector || seasonPassSelector?.SelectedItem is not SeasonPassChoice choice)
        {
            return;
        }

        viewedSeasonId = choice.Id;
        RenderViewedSeason();
        UpdateSeasonActivationPresentation();
    }

    private void SeasonPassActivateButton_Click(object sender, RoutedEventArgs e)
    {
        if (SeasonCatalog.TryGet(viewedSeasonId) is not { } viewed
            || string.Equals(viewed.Id, controller.Profile.ActiveSeasonId, StringComparison.Ordinal))
        {
            UpdateSeasonActivationPresentation();
            return;
        }

        if (!controller.Service.SelectSeason(viewed.Id))
        {
            RefreshSeasonPassSwitcher();
            return;
        }

        controller.SaveAll();
        RefreshSeasonPassSwitcher();
        RenderViewedSeason();
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
            if (SeasonCatalog.TryGet(viewedSeasonId) is null)
            {
                viewedSeasonId = controller.Profile.ActiveSeasonId;
            }
            RefreshSeasonPassSwitcher();
            RenderViewedSeason();
        }));
    }

    private void UpdateSeasonActivationPresentation()
    {
        if (SeasonCatalog.TryGet(viewedSeasonId) is not { } viewed)
        {
            return;
        }

        bool isActive = string.Equals(viewed.Id, controller.Profile.ActiveSeasonId, StringComparison.Ordinal);
        string passKind = viewed.Archived
            ? SeasonTextLocalization.Ui("LegacyPass", "LEGACY PASS")
            : string.Equals(viewed.Id, SeasonCatalog.Current.Id, StringComparison.Ordinal)
                ? SeasonTextLocalization.Ui("CurrentPass", "CURRENT PASS")
                : SeasonTextLocalization.Ui("AvailablePass", "AVAILABLE PASS");

        if (seasonPassActivateButton is not null)
        {
            seasonPassActivateButton.IsEnabled = !isActive;
            seasonPassActivateButton.Content = isActive ? SeasonTextLocalization.Ui("ActivePass", "ACTIVE PASS") : "ACTIVATE PASS";
            seasonPassActivateButton.ToolTip = isActive
                ? SeasonTextLocalization.Ui("ActiveTip", "This pass currently receives all new Season XP.")
                : $"{SeasonTextLocalization.Ui("ActivateTip", "Activate")} {SeasonTextLocalization.Name(viewed)}. {SeasonTextLocalization.Ui("ActivateTipSuffix", "Your progress on the currently active pass will be preserved.")}";
        }

        if (seasonPassStateText is not null)
        {
            seasonPassStateText.Text = isActive
                ? $"{passKind} · {SeasonTextLocalization.Ui("Active", "ACTIVE")} · {SeasonTextLocalization.Ui("ActiveSeasonXp", "all new Season XP is earned on this pass")}"
                : $"{passKind} · {SeasonTextLocalization.Ui("Viewing", "VIEWING")} · {SeasonTextLocalization.Ui("InactiveSeasonXp", "progress is saved separately · activate this pass to send new Season XP here")}";
            seasonPassStateText.Foreground = isActive ? PositiveBrush : MutedBrush;
        }
    }

    private void RenderViewedSeason()
    {
        if (SeasonCatalog.TryGet(viewedSeasonId) is not { } season)
        {
            season = SeasonCatalog.Get(controller.Profile.ActiveSeasonId);
            viewedSeasonId = season.Id;
        }

        SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
        SeasonTitleText.Text = SeasonTextLocalization.Name(season);
        SeasonDescriptionText.Text = SeasonTextLocalization.Description(season);
        SeasonTierText.Text = $"{SeasonTextLocalization.Ui("Tier", "Tier")} {progress.Tier} / {season.Tiers.Count} · {progress.Experience:N0} {SeasonTextLocalization.Ui("SeasonXp", "Season XP")}";
        SeasonProgress.Maximum = season.Tiers[^1].RequiredExperience;
        SeasonProgress.Value = Math.Min(progress.Experience, season.Tiers[^1].RequiredExperience);

        SetSeasonSectionLabels(season.RewardsEnabled);
        if (season.RewardsEnabled)
        {
            RenderConfiguredRewardTrack(season, progress);
        }
        else
        {
            RenderRewardPlaceholderTrack(season, progress);
        }

        UpdateSeasonActivationPresentation();
    }

    private void SetSeasonSectionLabels(bool rewardsEnabled)
    {
        if (SeasonNextRewardText.Parent is StackPanel nextPanel
            && nextPanel.Children.OfType<TextBlock>().FirstOrDefault() is { } nextCaption)
        {
            nextCaption.Text = rewardsEnabled ? SeasonTextLocalization.Ui("NextReward", "NEXT REWARD") : SeasonTextLocalization.Ui("NextLevel", "NEXT LEVEL");
        }

        if (SeasonRewardsPanel.Parent is ScrollViewer rewardsScroll
            && rewardsScroll.Parent is Grid rewardsGrid
            && rewardsGrid.Children.OfType<TextBlock>().FirstOrDefault() is { } rewardsCaption)
        {
            rewardsCaption.Text = rewardsEnabled ? SeasonTextLocalization.Ui("RewardTrack", "REWARD TRACK") : SeasonTextLocalization.Ui("ProgressionTrack", "PROGRESSION TRACK");
        }

        if (SeasonMilestonesPanel.Parent is ScrollViewer milestonesScroll
            && milestonesScroll.Parent is Grid milestonesGrid
            && milestonesGrid.Children.OfType<StackPanel>().FirstOrDefault() is { } milestoneHeader)
        {
            TextBlock[] labels = milestoneHeader.Children.OfType<TextBlock>().ToArray();
            if (labels.Length > 0)
            {
                labels[0].Text = rewardsEnabled ? SeasonTextLocalization.Ui("MilestoneRewards", "MILESTONE REWARDS") : SeasonTextLocalization.Ui("XpMilestones", "XP MILESTONES");
            }
            if (labels.Length > 1)
            {
                labels[1].Text = rewardsEnabled ? SeasonTextLocalization.Ui("SpecialUnlocks", "Special unlocks at key tiers") : SeasonTextLocalization.Ui("LongForm", "Long-form progression checkpoints");
            }
        }
    }

    private void RenderConfiguredRewardTrack(SeasonDefinition season, SeasonProgressState progress)
    {
        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        SeasonNextRewardText.Inlines.Clear();
        if (next is null)
        {
            SeasonNextRewardText.Text = SeasonTextLocalization.Ui("SeasonComplete", "Season complete");
            SeasonNextRewardXpText.Text = $"{SeasonTextLocalization.Ui("All", "All")} {season.Tiers.Count} {SeasonTextLocalization.Ui("RewardsUnlocked", "rewards unlocked.")}";
        }
        else
        {
            SeasonNextRewardText.Inlines.Add(new Run($"{SeasonTextLocalization.Ui("Tier", "Tier")} {next.Tier} · "));
            SeasonNextRewardText.Inlines.Add(new Run($"◆ {next.Coins:N0} {SeasonTextLocalization.Ui("Coins", "coins")}") { Foreground = CoinBrush, FontWeight = FontWeights.Bold });
            if (next.CosmeticId is not null)
            {
                SeasonNextRewardText.Inlines.Add(new Run($" + {CosmeticCatalog.Get(next.CosmeticId).Name}"));
            }
            if (CollectionCatalog.SeasonPack(next.Tier) is { } nextPack)
            {
                SeasonNextRewardText.Inlines.Add(new Run($" + {nextPack} {SeasonTextLocalization.Ui("Pack", "pack")}"));
            }
            SeasonNextRewardXpText.Text = $"{Math.Max(0, next.RequiredExperience - progress.Experience):N0} {SeasonTextLocalization.Ui("XpToUnlock", "XP to unlock")}";
        }

        SeasonMilestonesPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers.Where(tier => tier.CosmeticId is not null || tier.Tier % 10 == 0))
        {
            bool unlocked = progress.Tier >= tier.Tier;
            var milestone = new Border
            {
                Width = 160,
                MinHeight = 72,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = unlocked ? PositiveBrush : new SolidColorBrush(Color.FromRgb(53, 78, 65)),
                Background = unlocked
                    ? new SolidColorBrush(Color.FromRgb(18, 54, 37))
                    : new SolidColorBrush(Color.FromRgb(9, 29, 20)),
                Opacity = unlocked ? 1 : 0.78,
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"{SeasonTextLocalization.Ui("Tier", "Tier").ToUpperInvariant()} {tier.Tier}", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = unlocked ? PositiveBrush : MutedBrush });
            if (tier.CosmeticId is not null)
            {
                CosmeticDefinition cosmetic = CosmeticCatalog.Get(tier.CosmeticId);
                stack.Children.Add(new TextBlock { Text = cosmetic.Name, Margin = new Thickness(0, 4, 0, 0), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                stack.Children.Add(new TextBlock { Text = cosmetic.Kind.ToString().ToUpperInvariant(), FontSize = 9, Foreground = MutedBrush, Margin = new Thickness(0, 2, 0, 0) });
            }
            stack.Children.Add(CoinText($"◆ {tier.Coins:N0}", 11, FontWeights.SemiBold));
            milestone.Child = stack;
            SeasonMilestonesPanel.Children.Add(milestone);
        }

        SeasonRewardsPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers)
        {
            bool unlocked = progress.Tier >= tier.Tier;
            bool isNext = next?.Tier == tier.Tier;
            CosmeticDefinition? cosmetic = tier.CosmeticId is null ? null : CosmeticCatalog.Get(tier.CosmeticId);
            CollectionRarity? pack = CollectionCatalog.SeasonPack(tier.Tier);

            var border = PanelBorder();
            border.Padding = new Thickness(12, 8, 12, 8);
            border.Margin = new Thickness(2, 3, 2, 3);
            border.MinHeight = 60;
            border.Opacity = unlocked || isNext ? 1 : 0.62;
            if (isNext)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(116, 126, 184));
                border.Background = new SolidColorBrush(Color.FromRgb(14, 31, 31));
            }
            else if (unlocked)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 100, 73));
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(126) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            string state = unlocked ? SeasonTextLocalization.Ui("Unlocked", "UNLOCKED") : isNext ? SeasonTextLocalization.Ui("Next", "NEXT") : SeasonTextLocalization.Ui("Locked", "LOCKED");
            var badge = new Border
            {
                Background = unlocked
                    ? new SolidColorBrush(Color.FromRgb(22, 66, 44))
                    : isNext
                        ? new SolidColorBrush(Color.FromRgb(39, 48, 82))
                        : new SolidColorBrush(Color.FromRgb(13, 38, 26)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = $"T{tier.Tier}",
                    FontWeight = FontWeights.Bold,
                    Foreground = unlocked ? PositiveBrush : isNext ? new SolidColorBrush(Color.FromRgb(166, 177, 232)) : MutedBrush,
                },
            };
            grid.Children.Add(badge);

            FrameworkElement preview = CreateSeasonRewardPreview(tier, cosmetic, pack);
            Grid.SetColumn(preview, 1);
            grid.Children.Add(preview);

            var reward = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = cosmetic is null ? FontWeights.Normal : FontWeights.SemiBold,
                Margin = new Thickness(8, 0, 8, 0),
            };
            reward.Inlines.Add(new Run($"◆ {tier.Coins:N0} {SeasonTextLocalization.Ui("Coins", "coins")}") { Foreground = CoinBrush, FontWeight = FontWeights.SemiBold });
            if (pack is { } tierPack)
            {
                reward.Inlines.Add(new Run($" · {tierPack} {SeasonTextLocalization.Ui("CardPack", "card pack")}"));
            }
            if (cosmetic is not null)
            {
                reward.Inlines.Add(new Run($" · {cosmetic.Name} ({cosmetic.Kind})"));
            }
            Grid.SetColumn(reward, 2);
            grid.Children.Add(reward);

            var xp = new TextBlock
            {
                Text = $"{tier.RequiredExperience:N0} XP",
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(xp, 3);
            grid.Children.Add(xp);

            var status = new TextBlock
            {
                Text = state,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = unlocked ? PositiveBrush : isNext ? new SolidColorBrush(Color.FromRgb(166, 177, 232)) : MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(status, 4);
            grid.Children.Add(status);

            border.Child = grid;
            SeasonRewardsPanel.Children.Add(border);
        }
    }

    private FrameworkElement CreateSeasonRewardPreview(
        SeasonTierDefinition tier,
        CosmeticDefinition? cosmetic,
        CollectionRarity? pack)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        panel.Children.Add(CreateSeasonRewardChip(
            "◆",
            tier.Coins.ToString("N0"),
            CoinBrush,
            $"{tier.Coins:N0} cosmetic coins"));

        if (pack is { } rarity)
        {
            panel.Children.Add(CreateSeasonRewardChip(
                "PACK",
                rarity.ToString().ToUpperInvariant(),
                SeasonPackAccent(rarity),
                $"{rarity} card pack"));
        }

        if (cosmetic is not null)
        {
            var preview = new Viewbox
            {
                Width = 58,
                Height = 42,
                Margin = new Thickness(5, 0, 0, 0),
                Stretch = Stretch.Uniform,
                Child = CreateCosmeticPreview(cosmetic),
                ToolTip = $"{cosmetic.Name} · {cosmetic.Kind}",
            };
            System.Windows.Automation.AutomationProperties.SetName(preview, $"{cosmetic.Name} {SeasonTextLocalization.Ui("Preview", "preview")}");
            panel.Children.Add(preview);
        }

        return panel;
    }

    private static Border CreateSeasonRewardChip(string symbol, string label, Brush accent, string tooltip)
    {
        var content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        content.Children.Add(new TextBlock
        {
            Text = symbol,
            FontSize = symbol == "PACK" ? 8 : 13,
            FontWeight = FontWeights.Bold,
            Foreground = accent,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        content.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 8,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(222, 232, 226)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        return new Border
        {
            Width = 58,
            Height = 42,
            Margin = new Thickness(0, 0, 5, 0),
            Padding = new Thickness(4, 3, 4, 3),
            CornerRadius = new CornerRadius(7),
            BorderThickness = new Thickness(1),
            BorderBrush = accent,
            Background = new SolidColorBrush(Color.FromRgb(8, 27, 19)),
            Child = content,
            ToolTip = tooltip,
        };
    }

    private static Brush SeasonPackAccent(CollectionRarity rarity) => new SolidColorBrush(rarity switch
    {
        CollectionRarity.Common => Color.FromRgb(126, 169, 147),
        CollectionRarity.Uncommon => Color.FromRgb(82, 218, 191),
        CollectionRarity.Rare => Color.FromRgb(103, 169, 245),
        CollectionRarity.Epic => Color.FromRgb(185, 128, 240),
        CollectionRarity.Legendary => Color.FromRgb(244, 199, 92),
        _ => Color.FromRgb(126, 169, 147),
    });

    private void RenderRewardPlaceholderTrack(SeasonDefinition season, SeasonProgressState progress)
    {
        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        SeasonNextRewardText.Inlines.Clear();
        SeasonNextRewardText.Text = next is null ? SeasonTextLocalization.Ui("PassComplete", "Pass complete") : $"Tier {next.Tier}";
        SeasonNextRewardXpText.Text = next is null
            ? $"{season.Tiers.Count} {SeasonTextLocalization.Ui("AllTiersReached", "tiers reached · rewards will be assigned separately.")}"
            : $"{Math.Max(0, next.RequiredExperience - progress.Experience):N0} {SeasonTextLocalization.Ui("XpToNext", "XP to next tier · reward slot reserved")}";

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
                Text = SeasonTextLocalization.Ui("RewardReserved", "Reward slot reserved"),
                Foreground = MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(placeholder, 1);
            grid.Children.Add(placeholder);

            var xp = new TextBlock
            {
                Text = $"+{tierCost:N0} XP · {tier.RequiredExperience:N0} {SeasonTextLocalization.Ui("Total", "total")}",
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(xp, 2);
            grid.Children.Add(xp);

            var status = new TextBlock
            {
                Text = reached ? SeasonTextLocalization.Ui("Reached", "REACHED") : isNext ? SeasonTextLocalization.Ui("Next", "NEXT") : SeasonTextLocalization.Ui("Locked", "LOCKED"),
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
                Text = $"{SeasonTextLocalization.Ui("Tier", "Tier").ToUpperInvariant()} {tier.Tier}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = reached ? PositiveBrush : MutedBrush,
            });
            stack.Children.Add(new TextBlock
            {
                Text = $"{tier.RequiredExperience:N0} XP {SeasonTextLocalization.Ui("Total", "total")}",
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = FontWeights.SemiBold,
            });
            stack.Children.Add(new TextBlock
            {
                Text = reached ? SeasonTextLocalization.Ui("Reached", "REACHED") : SeasonTextLocalization.Ui("Checkpoint", "CHECKPOINT"),
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
