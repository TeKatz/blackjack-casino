using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow : Window
{
    private readonly PlayerProgressionController controller;

    public ProgressionWindow(PlayerProgressionController controller)
    {
        this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        InitializeComponent();
        StateChanged += (_, _) => RefreshMaximizeGlyph();
        RefreshAll();
        RefreshMaximizeGlyph();
    }

    private void RefreshAll()
    {
        RefreshProfileHeader();
        RefreshStatistics();
        RefreshHistory();
        RefreshSeason();
        RefreshShop();
    }

    private void RefreshProfileHeader()
    {
        PlayerProfileState profile = controller.Profile;
        CosmeticDefinition title = CosmeticCatalog.Get(profile.EquippedTitleId);
        TitleText.Text = title.Name;
        LevelText.Text = $"Prestige {profile.Prestige} · Level {profile.Level} / {ProgressionCurve.MaximumLevel} · Lifetime XP {profile.LifetimeExperience:N0}";
        CoinsText.Text = $"◆ {profile.Coins:N0} coins";
        ProfileFrame.BorderBrush = FrameBrush(profile.EquippedFrameId);
        PrestigeButton.IsEnabled = controller.Service.CanPrestige;

        if (profile.Level >= ProgressionCurve.MaximumLevel)
        {
            LevelProgress.Maximum = 1;
            LevelProgress.Value = 1;
            LevelProgressText.Text = "LEVEL 100 — PRESTIGE AVAILABLE";
        }
        else
        {
            long required = controller.Service.ExperienceToNextLevel;
            LevelProgress.Maximum = Math.Max(1, required);
            LevelProgress.Value = Math.Min(profile.LevelExperience, required);
            LevelProgressText.Text = $"{profile.LevelExperience:N0} / {required:N0} XP";
        }
    }

    private void RefreshStatistics()
    {
        LifetimeStatsText.Text = BuildStatisticsText(controller.Profile.LifetimeStatistics, includePlaytime: true);
        SessionStatsText.Text = BuildStatisticsText(controller.Service.SessionStatistics, includePlaytime: false)
            + $"\nSession time: {FormatDuration(controller.SessionElapsed)}";

        TableStatsPanel.Children.Clear();
        if (controller.Profile.TableStatistics.Count == 0)
        {
            TableStatsPanel.Children.Add(MutedText("No completed hands yet."));
            return;
        }

        foreach ((string table, StatisticsSnapshot stats) in controller.Profile.TableStatistics.OrderByDescending(x => x.Value.RoundsPlayed))
        {
            decimal winRate = stats.HandsPlayed == 0 ? 0 : (decimal)stats.HandsWon / stats.HandsPlayed;
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.Children.Add(new TextBlock { Text = table, FontWeight = FontWeights.SemiBold });
            var hands = new TextBlock { Text = $"{stats.HandsPlayed:N0} hands", Foreground = MutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(hands, 1);
            row.Children.Add(hands);
            var net = new TextBlock { Text = SignedMoney(stats.NetProfit), Foreground = stats.NetProfit >= 0 ? PositiveBrush : NegativeBrush, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(net, 2);
            row.Children.Add(net);
            row.ToolTip = $"Win rate {winRate:P1}";
            TableStatsPanel.Children.Add(row);
        }
    }

    private void RefreshHistory()
    {
        HistoryPanel.Children.Clear();
        RoundHistoryEntry[] entries = controller.History.OrderByDescending(x => x.CompletedAt).Take(150).ToArray();
        if (entries.Length == 0)
        {
            HistoryPanel.Children.Add(MutedText("No hand history yet. Finish a hand and it will appear here."));
            return;
        }

        foreach (RoundHistoryEntry entry in entries)
        {
            string player = string.Join(" | ", entry.PlayerHands.Select((hand, index) => $"H{index + 1} {Cards(hand.Cards)} → {Outcome(hand.Outcome)}"));
            var border = PanelBorder();
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"{entry.CompletedAt.ToLocalTime():g}    {SignedMoney(entry.TotalNet)}",
                FontWeight = FontWeights.Bold,
                Foreground = entry.TotalNet >= 0 ? PositiveBrush : NegativeBrush,
            });
            stack.Children.Add(new TextBlock { Text = $"Bet {Money(entry.InitialBet)} · Dealer {Cards(entry.DealerCards)}", Margin = new Thickness(0, 5, 0, 0), Foreground = MutedBrush });
            stack.Children.Add(new TextBlock { Text = player, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            border.Child = stack;
            HistoryPanel.Children.Add(border);
        }
    }

    private void RefreshSeason()
    {
        SeasonDefinition season = SeasonCatalog.Get(controller.Profile.ActiveSeasonId);
        SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
        SeasonTitleText.Text = season.Name;
        SeasonDescriptionText.Text = season.Description;
        SeasonTierText.Text = $"Tier {progress.Tier} / {season.Tiers.Count} · {progress.Experience:N0} Season XP";
        SeasonProgress.Maximum = season.Tiers[^1].RequiredExperience;
        SeasonProgress.Value = Math.Min(progress.Experience, season.Tiers[^1].RequiredExperience);

        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        if (next is null)
        {
            SeasonNextRewardText.Text = "Season complete";
            SeasonNextRewardXpText.Text = "All 50 rewards unlocked.";
        }
        else
        {
            string cosmetic = next.CosmeticId is null ? string.Empty : $" + {CosmeticCatalog.Get(next.CosmeticId).Name}";
            SeasonNextRewardText.Text = $"Tier {next.Tier} · ◆ {next.Coins:N0} coins{cosmetic}";
            SeasonNextRewardXpText.Text = $"{Math.Max(0, next.RequiredExperience - progress.Experience):N0} XP to unlock";
        }

        SeasonMilestonesPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers.Where(tier => tier.CosmeticId is not null || tier.Tier % 10 == 0))
        {
            bool unlocked = progress.Tier >= tier.Tier;
            string reward = tier.CosmeticId is null
                ? $"◆ {tier.Coins:N0}"
                : CosmeticCatalog.Get(tier.CosmeticId).Name;

            var milestone = new Border
            {
                Width = 150,
                MinHeight = 58,
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
            milestone.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = $"TIER {tier.Tier}", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = unlocked ? PositiveBrush : MutedBrush },
                    new TextBlock { Text = reward, Margin = new Thickness(0, 4, 0, 0), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                },
            };
            SeasonMilestonesPanel.Children.Add(milestone);
        }

        SeasonRewardsPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers)
        {
            bool unlocked = progress.Tier >= tier.Tier;
            bool isNext = next?.Tier == tier.Tier;
            string cosmetic = tier.CosmeticId is null ? string.Empty : $" · {CosmeticCatalog.Get(tier.CosmeticId).Name}";

            var border = PanelBorder();
            border.Padding = new Thickness(12, 9, 12, 9);
            border.Margin = new Thickness(2, 3, 2, 3);
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
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(126) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            string state = unlocked ? "UNLOCKED" : isNext ? "NEXT" : "LOCKED";
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
            };
            badge.Child = new TextBlock
            {
                Text = $"T{tier.Tier}",
                FontWeight = FontWeights.Bold,
                Foreground = unlocked ? PositiveBrush : isNext ? new SolidColorBrush(Color.FromRgb(166, 177, 232)) : MutedBrush,
            };
            grid.Children.Add(badge);

            var reward = new TextBlock
            {
                Text = $"◆ {tier.Coins:N0} coins{cosmetic}",
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = tier.CosmeticId is null ? FontWeights.Normal : FontWeights.SemiBold,
            };
            Grid.SetColumn(reward, 1);
            grid.Children.Add(reward);

            var xp = new TextBlock
            {
                Text = $"{tier.RequiredExperience:N0} XP",
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(xp, 2);
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
            Grid.SetColumn(status, 3);
            grid.Children.Add(status);

            border.Child = grid;
            SeasonRewardsPanel.Children.Add(border);
        }
    }

    private void RefreshShop()
    {
        PlayerProfileState profile = controller.Profile;
        ShopCoinsText.Text = $"Balance: ◆ {profile.Coins:N0}";
        ShopPanel.Children.Clear();

        foreach (CosmeticDefinition cosmetic in CosmeticCatalog.ShopItems)
        {
            bool owned = profile.OwnedCosmeticIds.Contains(cosmetic.Id, StringComparer.Ordinal);
            bool equipped = cosmetic.Kind switch
            {
                CosmeticKind.Title => profile.EquippedTitleId == cosmetic.Id,
                CosmeticKind.Frame => profile.EquippedFrameId == cosmetic.Id,
                _ => false,
            };
            bool canAfford = profile.Coins >= cosmetic.Price;

            var border = PanelBorder();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = $"{cosmetic.Name} · {cosmetic.Kind}", FontWeight = FontWeights.Bold, FontSize = 15 });
            text.Children.Add(new TextBlock { Text = cosmetic.Description, Foreground = MutedBrush, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            grid.Children.Add(text);

            var button = new Button
            {
                Tag = cosmetic.Id,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = equipped ? "Equipped" : owned ? "Equip" : $"Buy · ◆ {cosmetic.Price:N0}",
                IsEnabled = !equipped && (owned || canAfford),
                ToolTip = !owned && !canAfford
                    ? $"You need ◆ {cosmetic.Price - profile.Coins:N0} more coins."
                    : null,
            };
            button.Click += ShopItemButton_Click;
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);
            border.Child = grid;
            ShopPanel.Children.Add(border);
        }
    }

    private void ShopItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id })
        {
            return;
        }

        PlayerProfileState profile = controller.Profile;
        bool owned = profile.OwnedCosmeticIds.Contains(id, StringComparer.Ordinal);
        if (!owned)
        {
            PurchaseResult result = controller.PurchaseCosmetic(id);
            ShopStatusText.Text = result.Message;
            if (!result.Success)
            {
                RefreshShop();
                return;
            }
        }

        _ = controller.EquipCosmetic(id);
        ShopStatusText.Text = $"Equipped {CosmeticCatalog.Get(id).Name}.";
        RefreshAll();
    }

    private void PrestigeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!controller.Service.CanPrestige)
        {
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            this,
            "Enter Prestige? Your level and current level XP reset to 0. Coins, cosmetics, statistics, bankroll and Season Pass progress stay exactly as they are.",
            "Enter Prestige",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        PrestigeResult result = controller.Prestige();
        ShopStatusText.Text = result.Message;
        RefreshAll();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        RefreshMaximizeGlyph();
    }

    private void RefreshMaximizeGlyph()
    {
        if (MaximizeButton is not null)
        {
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }
    }

    private string BuildStatisticsText(StatisticsSnapshot stats, bool includePlaytime)
    {
        decimal winRate = stats.HandsPlayed == 0 ? 0m : (decimal)stats.HandsWon / stats.HandsPlayed;
        string text =
            $"Rounds: {stats.RoundsPlayed:N0}\n" +
            $"Hands: {stats.HandsPlayed:N0}\n" +
            $"W / L / Push: {stats.HandsWon:N0} / {stats.HandsLost:N0} / {stats.Pushes:N0}\n" +
            $"Win rate: {winRate:P1}\n" +
            $"Blackjacks: {stats.Blackjacks:N0}\n" +
            $"Doubles: {stats.Doubles:N0} ({stats.DoubleWins:N0} won)\n" +
            $"Splits: {stats.Splits:N0}\n" +
            $"Surrenders: {stats.Surrenders:N0}\n" +
            $"Insurance taken: {stats.InsuranceTaken:N0}\n" +
            $"Total wagered: {Money(stats.TotalWagered)}\n" +
            $"Net: {SignedMoney(stats.NetProfit)}\n" +
            $"Biggest win: {Money(stats.BiggestWin)}\n" +
            $"Biggest loss: {Money(stats.BiggestLoss)}\n" +
            $"Best win streak: {stats.BestWinStreak:N0}\n" +
            $"Worst loss streak: {stats.WorstLossStreak:N0}";

        if (includePlaytime)
        {
            text += $"\nPlaytime: {FormatDuration(TimeSpan.FromSeconds(controller.Profile.TotalPlaytimeSeconds) + controller.SessionElapsed)}";
        }

        return text;
    }

    private static Border PanelBorder() => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(180, 11, 33, 23)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(49, 90, 67)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(2, 4, 2, 4),
    };

    private static TextBlock MutedText(string text) => new() { Text = text, Foreground = MutedBrush, Margin = new Thickness(4) };

    private static Brush FrameBrush(string id) => id switch
    {
        "frame-prestige-2" => new SolidColorBrush(Color.FromRgb(183, 122, 69)),
        "frame-prestige-5" => new SolidColorBrush(Color.FromRgb(191, 201, 209)),
        "frame-prestige-10" => new SolidColorBrush(Color.FromRgb(225, 193, 90)),
        "frame-season-midnight-10" => new SolidColorBrush(Color.FromRgb(102, 127, 209)),
        "frame-season-midnight-35" => new SolidColorBrush(Color.FromRgb(176, 94, 207)),
        "frame-emerald" => new SolidColorBrush(Color.FromRgb(77, 154, 114)),
        "frame-midnight" => new SolidColorBrush(Color.FromRgb(82, 110, 154)),
        "frame-burgundy" => new SolidColorBrush(Color.FromRgb(156, 91, 102)),
        "frame-gold-trim" => new SolidColorBrush(Color.FromRgb(212, 168, 79)),
        _ => new SolidColorBrush(Color.FromRgb(82, 105, 90)),
    };

    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(166, 187, 174));
    private static readonly Brush PositiveBrush = new SolidColorBrush(Color.FromRgb(142, 199, 129));
    private static readonly Brush NegativeBrush = new SolidColorBrush(Color.FromRgb(217, 126, 126));

    private static string Cards(IEnumerable<Card> cards) => string.Join(" ", cards.Select(card => $"{Rank(card.Rank)}{Suit(card.Suit)}"));

    private static string Rank(Rank rank) => rank switch
    {
        BlackjackCasino.Core.Rank.Ace => "A",
        BlackjackCasino.Core.Rank.Jack => "J",
        BlackjackCasino.Core.Rank.Queen => "Q",
        BlackjackCasino.Core.Rank.King => "K",
        _ => ((int)rank).ToString(CultureInfo.InvariantCulture),
    };

    private static string Suit(Suit suit) => suit switch
    {
        BlackjackCasino.Core.Suit.Clubs => "♣",
        BlackjackCasino.Core.Suit.Diamonds => "♦",
        BlackjackCasino.Core.Suit.Hearts => "♥",
        BlackjackCasino.Core.Suit.Spades => "♠",
        _ => "?",
    };

    private static string Outcome(HandOutcome outcome) => outcome switch
    {
        HandOutcome.Blackjack => "Blackjack",
        HandOutcome.Win => "Win",
        HandOutcome.Loss => "Loss",
        HandOutcome.Push => "Push",
        HandOutcome.Surrender => "Surrender",
        _ => outcome.ToString(),
    };

    private static string Money(decimal amount) => $"${amount:N2}";
    private static string SignedMoney(decimal amount) => amount switch
    {
        > 0 => $"+${amount:N2}",
        < 0 => $"-${Math.Abs(amount):N2}",
        _ => "$0.00",
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }

        return $"{Math.Max(0, duration.Minutes)}m";
    }
}
