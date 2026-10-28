using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class PlayerProgressionController : IDisposable
{
    private readonly DateTimeOffset sessionStartedAt = DateTimeOffset.UtcNow;
    private readonly List<RoundHistoryEntry> history;
    private readonly HashSet<Guid> knownRoundIds;
    private bool disposed;
    private readonly bool persist;

    public PlayerProgressionController(PlayerProfileState? initialProfile = null)
    {
        persist = initialProfile is null;
        Service = new ProgressionService(initialProfile ?? PlayerProfileStore.LoadProfile());
        history = initialProfile is null ? PlayerProfileStore.LoadHistory().ToList() : [];
        knownRoundIds = history.Select(x => x.RoundId).ToHashSet();
    }

    public ProgressionService Service { get; }

    public PlayerProfileState Profile => Service.State;

    public IReadOnlyList<RoundHistoryEntry> History => history;

    public TimeSpan SessionElapsed => DateTimeOffset.UtcNow - sessionStartedAt;

    public ProgressionAward ProcessRound(RoundHistoryEntry round)
    {
        if (!knownRoundIds.Add(round.RoundId))
        {
            return ProgressionAward.None;
        }

        ProgressionAward award = Service.ProcessRound(round);
        history.Add(round);
        if (history.Count > 1_000)
        {
            history.RemoveRange(0, history.Count - 1_000);
        }

        SaveAll();
        return award;
    }

    public PurchaseResult PurchaseCosmetic(string cosmeticId)
    {
        PurchaseResult result = Service.PurchaseCosmetic(cosmeticId);
        if (result.Success && persist)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return result;
    }

    public bool EquipCosmetic(string cosmeticId)
    {
        bool equipped = Service.EquipCosmetic(cosmeticId);
        if (equipped && persist)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return equipped;
    }

    public ChallengeClaimResult ClaimChallenge(string challengeId)
    {
        ChallengeClaimResult result = Service.ClaimChallenge(challengeId);
        if (result.Success && persist)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return result;
    }

    public PrestigeResult Prestige()
    {
        PrestigeResult result = Service.Prestige();
        if (result.Success && persist)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return result;
    }

    public void AdoptManorProfile(PlayerProfileState committed)
    {
        // Keep the existing blackjack service, processed-round set and session statistics.
        Profile.Manor = committed.Manor;
        Profile.Level = committed.Level;
        Profile.LevelExperience = committed.LevelExperience;
        Profile.Coins = committed.Coins;
        Profile.LifetimeExperience = committed.LifetimeExperience;
        Profile.LastPlayedAt = committed.LastPlayedAt;
        Profile.Seasons = committed.Seasons;
        Profile.ActiveSeasonId = committed.ActiveSeasonId;
        Profile.OwnedCosmeticIds = committed.OwnedCosmeticIds;
        Profile.ClaimedChallengeIds = committed.ClaimedChallengeIds;
        Profile.EquippedTitleId = committed.EquippedTitleId;
        Profile.EquippedFrameId = committed.EquippedFrameId;
        Profile.EquippedPortraitId = committed.EquippedPortraitId;
    }

    public void SaveAll()
    {
        if (!persist) return;
        PlayerProfileStore.SaveProfile(Profile);
        PlayerProfileStore.SaveHistory(history);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Service.AddPlaytime(SessionElapsed);
        SaveAll();
    }
}
