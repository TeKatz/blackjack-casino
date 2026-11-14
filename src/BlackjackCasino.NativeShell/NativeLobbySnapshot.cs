using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Read-only projection of the preview's genuine C# financial and profile state
/// into the existing React lobby wire contract. No mock account balances or
/// synthetic progress; unavailable sub-systems stay disabled.
/// </summary>
internal static class NativeLobbySnapshot
{
    internal static object Create(NativePreviewRuntime session)
    {
        PlayerProfileState player = session.Profile;
        AppPreferences preferences = session.Preferences;
        LobbyProgressSnapshot progress = LobbyProgressSnapshot.From(player);
        CasinoVaultSnapshot vault = CasinoVaultService.Snapshot(player);
        int challenges = ChallengeCatalog.All.Count;
        int complete = ChallengeCatalog.All.Count(challenge =>
            ChallengeCatalog.Progress(player, challenge) >= challenge.Goal);
        DailyLoginStatus daily = DailyLoginProgress.GetStatus(
            preferences.LastDailyLoginClaimDate,
            preferences.DailyLoginClaims,
            DateOnly.FromDateTime(DateTime.Now));
        int cycleStart = Math.Max(0, daily.TotalClaims - daily.CycleCompletedSteps);
        var rewards = Enumerable.Range(0, DailyLoginRewardSchedule.CycleLength)
            .Select(i =>
            {
                DailyLoginRewardDefinition reward = DailyLoginRewardSchedule.GetDefinition(cycleStart + i);
                return new
                {
                    Day = i + 1,
                    Bankroll = Cash(reward.Bankroll),
                    Coins = reward.CosmeticCoins,
                    Pack = reward.PackRarity?.ToString(),
                };
            }).ToArray();

        return new
        {
            NativePreview = true,
            DailyLogin = new
            {
                // The daily claim now commits coins, packs and cash atomically.
                CanClaim = daily.CanClaim,
                daily.ClaimedToday,
                daily.TotalClaims,
                daily.ActiveStep,
                daily.NextStep,
                daily.CycleCompletedSteps,
                ActiveReward = Cash(daily.ActiveReward),
                NextReward = Cash(daily.NextReward),
                Rewards = rewards,
            },
            DisplayName = player.DisplayName,
            Level = player.Level,
            Coins = player.Coins.ToString("N0", CultureInfo.InvariantCulture),
            Prestige = player.Prestige,
            Title = CosmeticCatalog.Get(player.EquippedTitleId).Name,
            TitleVariant = CosmeticCatalog.Get(player.EquippedTitleId).VisualVariant ?? "title-default",
            PortraitVariant = CosmeticCatalog.Get(player.EquippedPortraitId).VisualVariant ?? "portrait-house-guest",
            FrameVariant = CosmeticCatalog.Get(player.EquippedFrameId).VisualVariant ?? "classic",
            Bankroll = Cash(session.Bankroll),
            BankrollValue = session.Bankroll,
            EmpireCredits = session.EmpireStarted
                ? session.EmpireCredits.ToString("N0", CultureInfo.InvariantCulture) + " cr" : "—",
            AlbumOwned = player.Collection.OwnedCardIds.Count,
            AlbumTotal = CollectionCatalog.Cards.Count,
            AlbumPacksReady = CollectionService.UnopenedPackCount(player),
            SeasonTier = progress.SeasonTier,
            SeasonNextTier = progress.SeasonNextTier,
            SeasonExperience = progress.SeasonExperience,
            SeasonRequired = progress.SeasonRequired,
            SeasonFraction = progress.SeasonFraction,
            SeasonComplete = progress.SeasonComplete,
            ChallengeName = progress.ChallengeName,
            ChallengeFraction = progress.ChallengeFraction,
            ChallengeTotal = challenges,
            ChallengeCompleted = complete,
            ChallengeOverallFraction = challenges == 0 ? 1d : (double)complete / challenges,
            ChallengesReady = progress.ReadyCount,
            VaultTier = vault.Tier.Name.Replace(" Vault", "", StringComparison.Ordinal).ToUpperInvariant(),
            VaultAmount = Cash(vault.CurrentAmount),
            VaultCapacity = Cash(vault.Tier.Capacity),
            VaultFraction = Math.Clamp((double)vault.Progress, 0d, 1d),
            AnimationsEnabled = preferences.Presentation("lobby").AnimationsEnabled,
            AlbumSoundEnabled = preferences.Presentation("album").SoundEnabled,
            MusicEnabled = preferences.MusicEnabled,
            MusicVolume = preferences.MusicVolume,
            ShowEmpireBalance = preferences.ShowEmpireBalance,
            Language = preferences.Language == UiLanguagePreference.German ? "de" : "en",
            Modes = HostMigrationCatalog.CasinoRoutes.Select(mode => new
            {
                Id = mode.DesktopModeId,
                Title = mode.DisplayName,
                Category = Category(mode.DesktopModeId),
                Description = Description(mode.DesktopModeId),
                Enabled = mode.HasPreviewBridge || (mode.HasNativeProcess && NativeDescentLaunch.IsPackaged(AppContext.BaseDirectory)),
                Secondary = mode.HasSecondaryWindow && mode.HasPreviewBridge,
            }).ToArray(),
        };
    }

    private static string Cash(decimal money) =>
        money.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

    private static string Category(string id) => id switch
    {
        "blackjack" or "roulette" or "kniffel" => "Table games",
        "midnight-reels" => "Classic slots",
        "neon-vault" or "nine-lives" or "midnight-manor" => "Video slots",
        "casino-empire" => "Idle casino",
        "midnight-merge" => "Casino restoration",
        "chill-lounge" => "Relaxed games",
        _ => "Action Roguelite",
    };

    private static string Description(string id) => id switch
    {
        "blackjack" => "Classic blackjack. Play your hand at the table.",
        "roulette" => "European roulette. Place your bets and spin.",
        "midnight-reels" => "Three reels, timeless symbols and classic wins.",
        "neon-vault" => "Five reels, wilds and electrifying vault bonuses.",
        "nine-lives" => "Find lucky cats and unlock free spins.",
        "midnight-manor" => "Haunted reels, hidden rooms and free spins.",
        "midnight-merge" => "Restore the Midnight Club one room at a time.",
        "kniffel" => "Roll the dice, fill your sheet and beat the house.",
        "casino-empire" => "Build and expand your own casino empire.",
        "chill-lounge" => "Eight relaxing games and a mystery to solve.",
        "midnight-descent" => "Descend into a dangerous roguelite adventure.",
        _ => "Not yet available in the independent native host.",
    };
}
