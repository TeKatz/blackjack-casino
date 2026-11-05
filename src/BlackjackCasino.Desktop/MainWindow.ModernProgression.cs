using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Core.Jigsaw;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    // React owns presentation only. The active profile and existing progression
    // services remain the single source of truth for rewards and statistics.
    private static string RecordNumber(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);
    private static string RecordMoney(decimal value) => "$" + value.ToString("N2", CultureInfo.InvariantCulture);
    private static string RecordNet(decimal value) => value < 0
        ? "-$" + Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture)
        : value > 0 ? "+$" + value.ToString("N2", CultureInfo.InvariantCulture) : "$0.00";
    private static string RecordPercent(decimal numerator, decimal denominator) =>
        denominator == 0 ? "—" : (numerator / denominator).ToString("P1", CultureInfo.InvariantCulture);
    private static string RecordDuration(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}h {value.Minutes}m" : $"{Math.Max(0, value.Minutes)}m";
    private static object RecordMetric(string label, string value) => new { Label = label, Value = value };

    private static string RecordCategory(ChallengeCategory category) => category switch
    {
        ChallengeCategory.MidnightDice => "Midnight Dice",
        ChallengeCategory.MidnightReels => "Midnight Reels",
        ChallengeCategory.MidnightManor => "Midnight Manor",
        ChallengeCategory.NeonVault => "Neon Vault",
        ChallengeCategory.NineLives => "Nine Lives",
        ChallengeCategory.MidnightMerge => "Midnight Merge",
        ChallengeCategory.WordSearch => "Word Search",
        _ => category.ToString(),
    };

    private static string RecordChallengeName(ChallengeDefinition challenge)
    {
        if (challenge.Category == ChallengeCategory.Jigsaw)
        {
            JigsawAchievementDefinition? achievement = JigsawAchievements.All.FirstOrDefault(a =>
                challenge.Id == "jigsaw-" + a.Id);
            if (achievement is not null)
                return Localization.Language == UiLanguagePreference.German ? achievement.GermanName : achievement.Name;
        }
        return Localization.Language == UiLanguagePreference.German
            ? Localization.T($"Challenge.{challenge.Id}.Name") : challenge.Name;
    }

    private static string RecordChallengeDescription(ChallengeDefinition challenge)
    {
        if (challenge.Category == ChallengeCategory.Jigsaw)
        {
            JigsawAchievementDefinition? achievement = JigsawAchievements.All.FirstOrDefault(a =>
                challenge.Id == "jigsaw-" + a.Id);
            if (achievement is not null)
                return Localization.Language == UiLanguagePreference.German
                    ? achievement.GermanDescription : achievement.Description;
        }
        return Localization.Language == UiLanguagePreference.German
            ? Localization.T($"Challenge.{challenge.Id}.Description") : challenge.Description;
    }

    private static string RecordChallengeValue(ChallengeDefinition challenge, decimal value)
    {
        string metric = challenge.Metric.ToString();
        if (challenge.Metric == ChallengeMetric.LifetimePlaytimeSeconds)
            return (value / 3600m).ToString("N1", CultureInfo.InvariantCulture) + " h";
        if (metric.EndsWith("Wagered", StringComparison.Ordinal)
            || metric.EndsWith("Returned", StringComparison.Ordinal)
            || metric.EndsWith("NetProfit", StringComparison.Ordinal)
            || metric.EndsWith("BiggestWin", StringComparison.Ordinal)
            || metric.EndsWith("BiggestPayout", StringComparison.Ordinal)
            || metric.EndsWith("HighestBankroll", StringComparison.Ordinal))
            return "$" + RecordNumber(value);
        return RecordNumber(value) + (metric.EndsWith("Multiplier", StringComparison.Ordinal) ? "×" : "");
    }

    private object GetModernChallengesSnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        var entries = ChallengeRegistry.All.Select(challenge =>
        {
            decimal progress = ChallengeRegistry.Progress(profile, challenge);
            bool complete = progress >= challenge.Goal;
            bool claimed = ChallengeRegistry.IsClaimed(profile, challenge);
            EmpireAchievementBoost? empire = EmpireAchievementBoostCatalog.All
                .FirstOrDefault(boost => boost.ChallengeId == challenge.Id);
            return new
            {
                challenge.Id,
                Name = RecordChallengeName(challenge),
                Description = RecordChallengeDescription(challenge),
                Category = RecordCategory(challenge.Category),
                IsAdventure = AdventureChallengeCatalog.IsAdventure(challenge),
                challenge.IsEndgame,
                Progress = (double)Math.Max(0m, progress),
                Goal = (double)challenge.Goal,
                ProgressLabel = RecordChallengeValue(challenge, Math.Min(progress, challenge.Goal)),
                GoalLabel = RecordChallengeValue(challenge, challenge.Goal),
                Fraction = (double)Math.Clamp(challenge.Goal == 0 ? 1m : progress / challenge.Goal, 0m, 1m),
                Complete = complete,
                Claimed = claimed,
                CanClaim = complete && !claimed && challenge.Category != ChallengeCategory.Jigsaw,
                AutomaticallyAwarded = challenge.Category == ChallengeCategory.Jigsaw,
                challenge.CoinReward,
                Pack = challenge.Category == ChallengeCategory.Jigsaw ? null
                    : ProgressionService.ChallengePack(challenge)?.ToString(),
                EmpireBoost = empire is null ? null
                    : $"+{empire.Percent:P1} · "
                      + (empire.RoomId is null ? "All rooms"
                          : EmpireCatalog.Find(empire.RoomId)?.Name ?? empire.RoomId),
            };
        }).ToArray();
        var ready = entries.Where(x => x.CanClaim).ToArray();
        return new
        {
            Total = entries.Length,
            Complete = entries.Count(x => x.Complete),
            Claimed = entries.Count(x => x.Claimed),
            Ready = ready.Length,
            ReadyCoins = ready.Sum(x => x.CoinReward),
            Categories = entries.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray(),
            Entries = entries,
        };
    }

    private (bool Success, string Message) ModernChallengeClaim(string id)
    {
        if (progressionController is null) return (false, "Player profile is not available.");
        if (ChallengeRegistry.All.All(c => c.Id != id)) return (false, "Unknown challenge.");
        ChallengeClaimResult result = progressionController.ClaimChallenge(id);
        if (result.Success)
        {
            RefreshProfileButton();
            RefreshCollectionAlbumHeaderButton();
        }
        return (result.Success, result.Message);
    }

    private (bool Success, string Message) ModernChallengesClaimAll()
    {
        if (progressionController is null) return (false, "Player profile is not available.");
        ChallengeClaimAllResult result = progressionController.ClaimAllChallenges();
        if (result.Success)
        {
            RefreshProfileButton();
            RefreshCollectionAlbumHeaderButton();
        }
        return (result.Success, result.Message);
    }

    private object GetModernStatisticsSnapshot()
    {
        PlayerProgressionController controller = progressionController!;
        PlayerProfileState profile = controller.Profile;
        StatisticsSnapshot blackjack = profile.LifetimeStatistics;
        SlotStatistics reels = profile.Slots;
        SlotStatistics legacy = profile.LegacyPooledSlots;
        NeonVaultStatistics vault = profile.NeonVault;
        NineLivesStatistics nine = profile.NineLives;
        ManorStatistics manor = profile.Manor;
        var dice = profile.Kniffel;
        SudokuStatistics sudoku = profile.Sudoku;
        decimal wagered = blackjack.TotalWagered + reels.TotalWagered + legacy.TotalWagered
            + vault.TotalWagered + nine.TotalWagered + manor.TotalWagered + dice.TotalWagered;
        decimal net = blackjack.NetProfit + reels.NetProfit + legacy.NetProfit
            + vault.NetProfit + nine.NetProfit + manor.NetProfit + dice.NetProfit;
        long spins = reels.Spins + legacy.Spins + vault.Spins + nine.Spins + manor.PaidSpins + manor.FreeSpins;
        object Slot(string id, string title, SlotStatistics item) => new
        {
            Id = id, Title = title, Category = "slots",
            Net = RecordNet(item.NetProfit), Positive = item.NetProfit >= 0,
            Metrics = new[]
            {
                RecordMetric("Spins", RecordNumber(item.Spins)),
                RecordMetric("Winning spins", RecordNumber(item.Wins)),
                RecordMetric("Spin win rate", RecordPercent(item.Wins, item.Spins)),
                RecordMetric("Total wagered", RecordMoney(item.TotalWagered)),
                RecordMetric("Total returned", RecordMoney(item.TotalReturned)),
                RecordMetric("Biggest payout", RecordMoney(item.BiggestPayout)),
                RecordMetric("Pushes", RecordNumber(item.Pushes)),
                RecordMetric("Losing spins", RecordNumber(Math.Max(0, item.Spins - item.Wins - item.Pushes))),
                RecordMetric("Return / wagered", RecordPercent(item.TotalReturned, item.TotalWagered)),
            },
        };
        object Game(string id, string title, string category, decimal gameNet, params object[] metrics) => new
        {
            Id = id, Title = title, Category = category,
            Net = RecordNet(gameNet), Positive = gameNet >= 0, Metrics = metrics,
        };
        object Activity(string id, string title, params object[] metrics) => new
        {
            Id = id, Title = title, Category = "relaxed", Net = (string?)null,
            Positive = true, Metrics = metrics,
        };

        var games = new List<object>
        {
            Game("blackjack", "Blackjack", "tables", blackjack.NetProfit,
                RecordMetric("Rounds", RecordNumber(blackjack.RoundsPlayed)),
                RecordMetric("Hands", RecordNumber(blackjack.HandsPlayed)),
                RecordMetric("Hand win rate", RecordPercent(blackjack.HandsWon, blackjack.HandsPlayed)),
                RecordMetric("Won / lost / push", $"{blackjack.HandsWon:N0} / {blackjack.HandsLost:N0} / {blackjack.Pushes:N0}"),
                RecordMetric("Total wagered", RecordMoney(blackjack.TotalWagered)),
                RecordMetric("Biggest round win", RecordMoney(blackjack.BiggestWin)),
                RecordMetric("Blackjacks", RecordNumber(blackjack.Blackjacks)),
                RecordMetric("Doubles / splits", $"{blackjack.Doubles:N0} / {blackjack.Splits:N0}"),
                RecordMetric("Best win streak", RecordNumber(blackjack.BestWinStreak)),
                RecordMetric("Surrenders / insurance", $"{blackjack.Surrenders:N0} / {blackjack.InsuranceTaken:N0}"),
                RecordMetric("Double wins", RecordNumber(blackjack.DoubleWins))),
            Game("midnight-dice", "Midnight Dice", "tables", dice.NetProfit,
                RecordMetric("Completed games", RecordNumber(dice.CompletedGames)),
                RecordMetric("Casual / casino", $"{Math.Max(0, dice.CompletedGames - dice.CasinoGamesCompleted):N0} / {dice.CasinoGamesCompleted:N0}"),
                RecordMetric("Best / average score", dice.CompletedGames == 0 ? "—" : $"{dice.BestScore:N0} / {(double)dice.TotalScore / dice.CompletedGames:0.0}"),
                RecordMetric("Casino wins / top two", $"{dice.CasinoWins:N0} / {dice.PodiumFinishes:N0}"),
                RecordMetric("Casino win rate", RecordPercent(dice.CasinoWins, dice.CasinoGamesCompleted)),
                RecordMetric("Entries / forfeits", $"{dice.CasinoGamesStarted:N0} / {dice.Forfeits:N0}"),
                RecordMetric("Total entry fees", RecordMoney(dice.TotalWagered)),
                RecordMetric("Total returned", RecordMoney(dice.TotalReturned)),
                RecordMetric("Biggest payout", RecordMoney(dice.BiggestPayout)),
                RecordMetric("Upper bonuses", RecordNumber(dice.UpperBonuses)),
                RecordMetric("Five of a kind", RecordNumber(dice.FiveOfAKinds)),
                RecordMetric("Full house / large straight", $"{dice.FullHouses:N0} / {dice.LargeStraights:N0}")),
            Activity("sudoku", "Sudoku",
                RecordMetric("Completed puzzles", RecordNumber(sudoku.CompletedPuzzles)),
                RecordMetric("Free Play / Daily", $"{sudoku.FreePlayCompleted:N0} / {sudoku.DailyCompleted:N0}"),
                RecordMetric("No-hint solves", RecordNumber(sudoku.NoHintCompletions)),
                RecordMetric("Hints used", RecordNumber(sudoku.HintsUsed)),
                RecordMetric("Total solve time", RecordDuration(TimeSpan.FromSeconds(sudoku.TotalSolveSeconds))),
                RecordMetric("Average solve", sudoku.CompletedPuzzles == 0 ? "—" : RecordDuration(TimeSpan.FromSeconds((double)sudoku.TotalSolveSeconds / sudoku.CompletedPuzzles))),
                RecordMetric("Fastest solve", sudoku.FastestSolveSeconds <= 0 ? "—" : RecordDuration(TimeSpan.FromSeconds(sudoku.FastestSolveSeconds))),
                RecordMetric("Fastest clean solve", sudoku.FastestNoHintSeconds <= 0 ? "—" : RecordDuration(TimeSpan.FromSeconds(sudoku.FastestNoHintSeconds))),
                RecordMetric("Relaxed / Easy / Medium", $"{sudoku.RelaxedCompleted:N0} / {sudoku.EasyCompleted:N0} / {sudoku.MediumCompleted:N0}"),
                RecordMetric("Hard / Expert / Master", $"{sudoku.HardCompleted:N0} / {sudoku.ExpertCompleted:N0} / {sudoku.MasterCompleted:N0}")),
            Slot("midnight-reels", "Midnight Reels", reels),
            Game("midnight-manor", "Midnight Manor", "slots", manor.NetProfit,
                RecordMetric("Paid spins", RecordNumber(manor.PaidSpins)),
                RecordMetric("Free spins", RecordNumber(manor.FreeSpins)),
                RecordMetric("Big wins", RecordNumber(manor.BigWins)),
                RecordMetric("Total wagered", RecordMoney(manor.TotalWagered)),
                RecordMetric("Total returned", RecordMoney(manor.TotalReturned)),
                RecordMetric("Biggest win", RecordMoney(manor.BiggestWin)),
                RecordMetric("Free spin triggers", RecordNumber(manor.FreeSpinsTriggered)),
                RecordMetric("Manor bonuses", RecordNumber(manor.ManorBonuses)),
                RecordMetric("Best run", $"{manor.BestManorMultiplier:N0}×"),
                RecordMetric("Escapes / max exits", $"{manor.Exits:N0} / {manor.MaximumExits:N0}"),
                RecordMetric("Caught / cash out", $"{manor.Catches:N0} / {manor.CashOuts:N0}"),
                RecordMetric("Interactive / auto", $"{manor.InteractiveRuns:N0} / {manor.AutomaticRuns:N0}")),
            Slot("neon-vault", "Neon Vault", vault),
            Slot("nine-lives", "Nine Lives", nine),
        };
        // Preserve all special-mode statistics from the WPF Overview; never
        // reclassify old pooled slot data into any modern machine.
        games.Add(Game("neon-features", "Neon Vault · Features", "slots", vault.NetProfit,
            RecordMetric("Paid / free spins", $"{vault.PaidSpins:N0} / {vault.FreeSpins:N0}"),
            RecordMetric("Free spin sessions", RecordNumber(vault.FreeSpinSessions)),
            RecordMetric("Spins awarded", RecordNumber(vault.FreeSpinsAwarded)),
            RecordMetric("Jackpots / grand", $"{vault.Jackpots:N0} / {vault.GrandJackpots:N0}"),
            RecordMetric("Blackouts / total", $"{vault.Blackouts:N0} / {vault.TotalBlackouts:N0}"),
            RecordMetric("Security overrides", RecordNumber(vault.SecurityOverrides)),
            RecordMetric("Best payout multiplier", $"{vault.BestPayoutMultiplier:N0}×")));
        games.Add(Game("nine-features", "Nine Lives · Features", "slots", nine.NetProfit,
            RecordMetric("Paid / bonus spins", $"{nine.PaidSpins:N0} / {nine.BonusSpins:N0}"),
            RecordMetric("Bonus sessions", RecordNumber(nine.BonusSessions)),
            RecordMetric("Spins awarded", RecordNumber(nine.BonusSpinsAwarded)),
            RecordMetric("Pounce Wilds", RecordNumber(nine.PounceWilds)),
            RecordMetric("Full Grid Frenzies", RecordNumber(nine.FullGridFrenzies)),
            RecordMetric("Best payout multiplier", $"{nine.BestPayoutMultiplier:N0.##}×")));
        if (legacy.Spins != 0 || legacy.Wins != 0 || legacy.Pushes != 0
            || legacy.TotalWagered != 0 || legacy.TotalReturned != 0 || legacy.BiggestPayout != 0)
            games.Add(Slot("legacy-slots", "Earlier slot history · game split unavailable", legacy));

        StatisticsSnapshot current = controller.Service.SessionStatistics;
        return new
        {
            Overview = new
            {
                Net = RecordNet(net), NetPositive = net >= 0,
                Wagered = RecordMoney(wagered),
                SlotSpins = RecordNumber(spins),
                Playtime = RecordDuration(controller.TotalPlaytime),
                Rounds = RecordNumber(blackjack.RoundsPlayed),
                GamesTracked = games.Count,
            },
            Games = games,
            Session = new
            {
                Rounds = RecordNumber(current.RoundsPlayed),
                Hands = RecordNumber(current.HandsPlayed),
                Playtime = RecordDuration(controller.SessionElapsed),
                Won = RecordNumber(current.HandsWon),
                Lost = RecordNumber(current.HandsLost),
                Pushes = RecordNumber(current.Pushes),
                Wagered = RecordMoney(current.TotalWagered),
                Net = RecordNet(current.NetProfit),
                Blackjacks = RecordNumber(current.Blackjacks),
                BestStreak = RecordNumber(current.BestWinStreak),
            },
            Tables = profile.TableStatistics
                .OrderByDescending(t => t.Value.RoundsPlayed)
                .Select(t => new
                {
                    Name = t.Key,
                    Hands = RecordNumber(t.Value.HandsPlayed),
                    Net = RecordNet(t.Value.NetProfit),
                    Positive = t.Value.NetProfit >= 0,
                    Rounds = RecordNumber(t.Value.RoundsPlayed),
                    WinRate = RecordPercent(t.Value.HandsWon, t.Value.HandsPlayed),
                }).ToArray(),
        };
    }


    // Native services retain ownership of progression and season selection.
    // The browser receives display-only snapshots and can only request a catalog-approved selection.
    private object GetModernAccountSnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        return new
        {
            profile.DisplayName,
            profile.Level,
            profile.Prestige,
            Coins = RecordNumber(profile.Coins),
            Title = CosmeticCatalog.Get(profile.EquippedTitleId).Name,
            PortraitId = profile.EquippedPortraitId,
            ProfileExperience = RecordNumber(profile.LifetimeExperience),
            Playtime = RecordDuration(progressionController.TotalPlaytime),
            OwnedCosmetics = profile.OwnedCosmeticIds.Count,
            OwnedCards = profile.Collection.OwnedCardIds.Count,
            TotalCards = CollectionCatalog.Cards.Count,
            Season = SeasonTextLocalization.Name(SeasonCatalog.Get(profile.ActiveSeasonId)),
            Created = profile.CreatedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture),
        };
    }

    private object GetModernSeasonsSnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        return new
        {
            profile.ActiveSeasonId,
            Seasons = SeasonCatalog.All.Select(season =>
            {
                profile.Seasons.TryGetValue(season.Id, out SeasonProgressState? progress);
                long xp = Math.Max(0, progress?.Experience ?? 0);
                return new
                {
                    season.Id,
                    Name = SeasonTextLocalization.Name(season),
                    Description = SeasonTextLocalization.Description(season),
                    season.Archived,
                    season.RewardsEnabled,
                    Tier = Math.Max(0, progress?.Tier ?? 0),
                    Experience = xp,
                    TotalTiers = season.Tiers.Count,
                    MaxExperience = season.Tiers[^1].RequiredExperience,
                    Tiers = season.Tiers.Select(tier => new
                    {
                        tier.Tier,
                        tier.RequiredExperience,
                        tier.Coins,
                        tier.CosmeticId,
                        tier.BankrollReward,
                    }).ToArray(),
                };
            }).ToArray(),
        };
    }

    private (bool Success, string Message) ActivateModernSeason(string id)
    {
        if (progressionController is null) return (false, "Player profile is not available.");
        if (SeasonCatalog.TryGet(id) is not { } season) return (false, "Unknown Season Pass.");
        if (progressionController.Profile.ActiveSeasonId == id) return (true, "This Season Pass is already active.");
        if (!progressionController.Service.SelectSeason(season.Id)) return (false, "Season Pass could not be activated.");
        progressionController.SaveAll();
        RefreshProfileButton();
        return (true, SeasonTextLocalization.Name(season) + " is now active.");
    }

    private void OpenModernProgression(string page)
    {
        if (modernLobby is not { Ready: true } || modernLobby.Failed)
        {
            if (page == "profile") OpenNativePlayerProfile();
            else if (page == "statistics") OpenProfileStatistics("Statistics");
            else OpenProgressionPage(page == "season" ? "Season Pass" : "Challenges");
            return;
        }
        if (progressionController is null) return;
        progressionController.AccruePlaytime();
        ProcessLatestCompletedRound();
        // Prepare React's destination while the native overlay is still opaque.
        // This prevents a brief exposure of the previous React lobby page.
        modernLobby.ShowProgression(page);
        while (MetaPageOpen) CloseMetaPage();
        modernLobby.SetLobbyVisible(true);
        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
    }

    private void OnModernProgressionClosed()
    {
        if (lobby?.Visibility != System.Windows.Visibility.Visible)
            modernLobby?.SetLobbyVisible(false);
        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
    }
}
