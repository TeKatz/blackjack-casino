using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private void RefreshStatistics()
    {
        PlayerProfileState profile = controller.Profile;
        StatisticsSnapshot blackjack = profile.LifetimeStatistics;
        SlotStatistics reels = profile.Slots;
        SlotStatistics legacy = profile.LegacyPooledSlots;
        NeonVaultStatistics vault = profile.NeonVault;
        NineLivesStatistics nineLives = profile.NineLives;
        ManorStatistics manor = profile.Manor;
        decimal totalWagered = blackjack.TotalWagered + reels.TotalWagered + vault.TotalWagered + nineLives.TotalWagered + manor.TotalWagered + legacy.TotalWagered + profile.Kniffel.TotalWagered;
        decimal totalNet = blackjack.NetProfit + reels.NetProfit + vault.NetProfit + nineLives.NetProfit + manor.NetProfit + legacy.NetProfit + profile.Kniffel.NetProfit;
        OverviewSummaryPanel.Children.Clear();
        OverviewSummaryPanel.Children.Add(SummaryTile("CASINO NET", SignedMoney(totalNet), totalNet >= 0 ? PositiveBrush : NegativeBrush));
        OverviewSummaryPanel.Children.Add(SummaryTile("TOTAL WAGERED", Money(totalWagered), MutedBrush));
        OverviewSummaryPanel.Children.Add(SummaryTile("SLOT SPINS · ALL HISTORY", $"{reels.Spins + vault.Spins + nineLives.Spins + manor.PaidSpins + manor.FreeSpins + legacy.Spins:N0}", CoinBrush));
        OverviewSummaryPanel.Children.Add(SummaryTile("TOTAL PLAYTIME", FormatDuration(controller.TotalPlaytime), MutedBrush));

        GameStatsPanel.Children.Clear();
        decimal winRate = blackjack.HandsPlayed == 0 ? 0 : (decimal)blackjack.HandsWon / blackjack.HandsPlayed;
        var blackjackCard = GameStatisticsCard("♠  BLACKJACK", blackjack.NetProfit,
            ("Rounds", $"{blackjack.RoundsPlayed:N0}"), ("Hands", $"{blackjack.HandsPlayed:N0}"), ("Hand win rate", $"{winRate:P1}"),
            ("Won / lost / push", $"{blackjack.HandsWon:N0} / {blackjack.HandsLost:N0} / {blackjack.Pushes:N0}"),
            ("Total wagered", Money(blackjack.TotalWagered)), ("Biggest round win", Money(blackjack.BiggestWin)),
            ("Blackjacks", $"{blackjack.Blackjacks:N0}"), ("Doubles / splits", $"{blackjack.Doubles:N0} / {blackjack.Splits:N0}"),
            ("Best win streak", $"{blackjack.BestWinStreak:N0}"));
        ((StackPanel)blackjackCard.Child).Children.Add(new Expander
        {
            Header = "Detailed lifetime statistics", Foreground = MutedBrush, Margin = new Thickness(0, 8, 0, 0),
            Content = new TextBlock { Text = BuildStatisticsText(blackjack, includePlaytime: false), Margin = new Thickness(0, 8, 0, 0), LineHeight = 21 },
        });
        GameStatsPanel.Children.Add(blackjackCard);
        var dice = profile.Kniffel;
        GameStatsPanel.Children.Add(GameStatisticsCard("⚄  MIDNIGHT DICE", dice.NetProfit,
            ("Completed games", $"{dice.CompletedGames:N0}"),
            ("Casual / casino", $"{Math.Max(0, dice.CompletedGames - dice.CasinoGamesCompleted):N0} / {dice.CasinoGamesCompleted:N0}"),
            ("Best / average score", dice.CompletedGames == 0 ? "—" : $"{dice.BestScore:N0} / {(double)dice.TotalScore / dice.CompletedGames:0.0}"),
            ("Casino wins / top two", $"{dice.CasinoWins:N0} / {dice.PodiumFinishes:N0}"),
            ("Casino win rate", dice.CasinoGamesCompleted == 0 ? "—" : $"{(decimal)dice.CasinoWins / dice.CasinoGamesCompleted:P1}"),
            ("Entries / forfeits", $"{dice.CasinoGamesStarted:N0} / {dice.Forfeits:N0}"),
            ("Total entry fees", Money(dice.TotalWagered)), ("Total returned", Money(dice.TotalReturned)),
            ("Biggest payout", Money(dice.BiggestPayout)),
            ("Upper bonuses", $"{dice.UpperBonuses:N0}"), ("Five of a kind", $"{dice.FiveOfAKinds:N0}"),
            ("Full house / large straight", $"{dice.FullHouses:N0} / {dice.LargeStraights:N0}")));

        SudokuStatistics sudoku = profile.Sudoku;
        string averageSolve = sudoku.CompletedPuzzles == 0
            ? "—"
            : FormatDuration(TimeSpan.FromSeconds((double)sudoku.TotalSolveSeconds / sudoku.CompletedPuzzles));
        GameStatsPanel.Children.Add(ActivityStatisticsCard("▦  SUDOKU",
            ("Completed puzzles", $"{sudoku.CompletedPuzzles:N0}"),
            ("Free Play / Daily", $"{sudoku.FreePlayCompleted:N0} / {sudoku.DailyCompleted:N0}"),
            ("No-hint solves", $"{sudoku.NoHintCompletions:N0}"),
            ("Hints used", $"{sudoku.HintsUsed:N0}"),
            ("Total solve time", FormatDuration(TimeSpan.FromSeconds(sudoku.TotalSolveSeconds))),
            ("Average solve", averageSolve),
            ("Fastest solve", sudoku.FastestSolveSeconds <= 0 ? "—" : FormatDuration(TimeSpan.FromSeconds(sudoku.FastestSolveSeconds))),
            ("Fastest clean solve", sudoku.FastestNoHintSeconds <= 0 ? "—" : FormatDuration(TimeSpan.FromSeconds(sudoku.FastestNoHintSeconds))),
            ("Relaxed / Easy / Medium", $"{sudoku.RelaxedCompleted:N0} / {sudoku.EasyCompleted:N0} / {sudoku.MediumCompleted:N0}"),
            ("Hard / Expert / Master", $"{sudoku.HardCompleted:N0} / {sudoku.ExpertCompleted:N0} / {sudoku.MasterCompleted:N0}")));

        GameStatsPanel.Children.Add(SlotStatisticsCard("◈  MIDNIGHT REELS", reels));
        GameStatsPanel.Children.Add(GameStatisticsCard("☾  MIDNIGHT MANOR", manor.NetProfit,
            ("Paid spins", $"{manor.PaidSpins:N0}"), ("Free spins", $"{manor.FreeSpins:N0}"), ("Big wins", $"{manor.BigWins:N0}"),
            ("Total wagered", Money(manor.TotalWagered)), ("Total returned", Money(manor.TotalReturned)), ("Biggest win", Money(manor.BiggestWin)),
            ("Free spin triggers", $"{manor.FreeSpinsTriggered:N0}"), ("Manor bonuses", $"{manor.ManorBonuses:N0}"), ("Best run", $"{manor.BestManorMultiplier:N0}×"),
            ("Escapes / max exits", $"{manor.Exits:N0} / {manor.MaximumExits:N0}"), ("Caught / cash out", $"{manor.Catches:N0} / {manor.CashOuts:N0}"),
            ("Interactive / auto", $"{manor.InteractiveRuns:N0} / {manor.AutomaticRuns:N0}")));
        var vaultCard = SlotStatisticsCard("◇  NEON VAULT", vault);
        ((StackPanel)vaultCard.Child).Children.Add(new Expander
        {
            Header = "Vault features & free spins", Foreground = MutedBrush, Margin = new Thickness(0, 8, 0, 0),
            Content = new TextBlock
            {
                Text = $"Paid spins {vault.PaidSpins:N0} · Free spins {vault.FreeSpins:N0}\n"
                    + $"Free spin sessions {vault.FreeSpinSessions:N0} · Spins awarded {vault.FreeSpinsAwarded:N0}\n"
                    + $"Jackpots {vault.Jackpots:N0} · Grand jackpots {vault.GrandJackpots:N0}\n"
                    + $"Blackouts {vault.Blackouts:N0} · Total Blackouts {vault.TotalBlackouts:N0}\n"
                    + $"Security overrides {vault.SecurityOverrides:N0} · Best payout {vault.BestPayoutMultiplier:N0}×",
                TextWrapping = TextWrapping.Wrap, LineHeight = 21, Margin = new Thickness(0, 8, 0, 0),
            },
        });
        GameStatsPanel.Children.Add(vaultCard);

        var nineLivesCard = SlotStatisticsCard("●  NINE LIVES", nineLives);
        ((StackPanel)nineLivesCard.Child).Children.Add(new Expander
        {
            Header = "Nine Lives features & bonus spins", Foreground = MutedBrush, Margin = new Thickness(0, 8, 0, 0),
            Content = new TextBlock
            {
                Text = $"Paid spins {nineLives.PaidSpins:N0} · Bonus spins {nineLives.BonusSpins:N0}\n"
                    + $"Bonus sessions {nineLives.BonusSessions:N0} · Spins awarded {nineLives.BonusSpinsAwarded:N0}\n"
                    + $"Pounce Wilds {nineLives.PounceWilds:N0} · Full Grid Frenzies {nineLives.FullGridFrenzies:N0}\n"
                    + $"Best payout {nineLives.BestPayoutMultiplier:N0.##}×",
                TextWrapping = TextWrapping.Wrap, LineHeight = 21, Margin = new Thickness(0, 8, 0, 0),
            },
        });
        GameStatsPanel.Children.Add(nineLivesCard);

        LegacySlotStatsHost.Content = null;
        if (legacy.Spins != 0 || legacy.Wins != 0 || legacy.Pushes != 0 || legacy.TotalWagered != 0 || legacy.TotalReturned != 0 || legacy.BiggestPayout != 0)
        {
            Border legacyCard = SlotStatisticsCard("Earlier slot history", legacy);
            legacyCard.Name = "LegacySlotHistoryCard";
            ((StackPanel)legacyCard.Child).Children.Insert(1, new TextBlock
            {
                Text = "Game split unavailable · included in casino totals.",
                Foreground = MutedBrush, FontSize = 11, Margin = new Thickness(0, 6, 0, 0),
            });
            LegacySlotStatsHost.Content = legacyCard;
        }

        StatisticsSnapshot session = controller.Service.SessionStatistics;
        SessionStatsText.Text = $"{session.RoundsPlayed:N0} rounds · {session.HandsPlayed:N0} hands · {FormatDuration(controller.SessionElapsed)}\n"
            + $"Won {session.HandsWon:N0} · Lost {session.HandsLost:N0} · Push {session.Pushes:N0}\n"
            + $"Wagered {Money(session.TotalWagered)} · Net {SignedMoney(session.NetProfit)}\n"
            + $"{session.Blackjacks:N0} blackjacks · Best streak {session.BestWinStreak:N0}";
        SessionStatsText.ToolTip = BuildStatisticsText(session, includePlaytime: false);
        TableStatsPanel.Children.Clear();
        if (profile.TableStatistics.Count == 0)
            TableStatsPanel.Children.Add(MutedText("No completed Blackjack rounds yet. Each table appears here after your first round."));
        foreach ((string table, StatisticsSnapshot stats) in profile.TableStatistics.OrderByDescending(pair => pair.Value.RoundsPlayed))
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4), ToolTip = BuildStatisticsText(stats, includePlaytime: false) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.Children.Add(new TextBlock { Text = table, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var hands = new TextBlock { Text = $"{stats.HandsPlayed:N0} hands", Foreground = MutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(hands, 1);
            row.Children.Add(hands);
            var net = new TextBlock { Text = SignedMoney(stats.NetProfit), Foreground = stats.NetProfit >= 0 ? PositiveBrush : NegativeBrush, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(net, 2);
            row.Children.Add(net);
            TableStatsPanel.Children.Add(row);
        }
    }

    private static Border SummaryTile(string caption, string value, Brush color)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = caption, Foreground = MutedBrush, FontSize = 10, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = value, Foreground = color, FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = value });
        var border = PanelBorder();
        border.Margin = new Thickness(6);
        border.Child = stack;
        return border;
    }

    private static Border SlotStatisticsCard(string name, SlotStatistics stats)
    {
        decimal winRate = stats.Spins == 0 ? 0 : (decimal)stats.Wins / stats.Spins;
        return GameStatisticsCard(name, stats.NetProfit,
            ("Spins", $"{stats.Spins:N0}"), ("Winning spins", $"{stats.Wins:N0}"), ("Spin win rate", $"{winRate:P1}"),
            ("Total wagered", Money(stats.TotalWagered)), ("Total returned", Money(stats.TotalReturned)), ("Biggest payout", Money(stats.BiggestPayout)),
            ("Pushes", $"{stats.Pushes:N0}"), ("Losing spins", $"{Math.Max(0, stats.Spins - stats.Wins - stats.Pushes):N0}"),
            ("Return / wagered", stats.TotalWagered == 0 ? "—" : $"{stats.TotalReturned / stats.TotalWagered:P1}"));
    }

    private static Border ActivityStatisticsCard(string name, params (string Caption, string Value)[] metrics)
    {
        var border = PanelBorder();
        border.Margin = new Thickness(6);
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = name,
            Foreground = new SolidColorBrush(Rgb(142, 201, 176)),
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var cells = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3, Margin = new Thickness(0, 10, 0, 0) };
        foreach ((string caption, string value) in metrics)
        {
            var cell = new StackPanel { Margin = new Thickness(0, 4, 10, 4) };
            cell.Children.Add(new TextBlock { Text = caption, Foreground = MutedBrush, FontSize = 10 });
            cell.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = value });
            cells.Children.Add(cell);
        }
        stack.Children.Add(cells);
        border.Child = stack;
        return border;
    }

    private static Border GameStatisticsCard(string name, decimal net, params (string Caption, string Value)[] metrics)
    {
        var border = PanelBorder();
        border.Margin = new Thickness(6);
        var stack = new StackPanel();
        var header = new DockPanel();
        var amount = new TextBlock { Text = SignedMoney(net), FontWeight = FontWeights.Bold, Foreground = net >= 0 ? PositiveBrush : NegativeBrush, FontSize = 17, ToolTip = "Lifetime net profit for this game" };
        DockPanel.SetDock(amount, Dock.Right);
        header.Children.Add(amount);
        header.Children.Add(new TextBlock { Text = name, Foreground = new SolidColorBrush(Rgb(255, 215, 124)), FontWeight = FontWeights.Bold, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        stack.Children.Add(header);
        var cells = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3, Margin = new Thickness(0, 10, 0, 0) };
        foreach ((string caption, string value) in metrics)
        {
            var cell = new StackPanel { Margin = new Thickness(0, 4, 10, 4) };
            cell.Children.Add(new TextBlock { Text = caption, Foreground = MutedBrush, FontSize = 10 });
            cell.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = value });
            cells.Children.Add(cell);
        }
        stack.Children.Add(cells);
        border.Child = stack;
        return border;
    }
}
