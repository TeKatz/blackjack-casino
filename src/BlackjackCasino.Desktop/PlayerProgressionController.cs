using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class PlayerProgressionController : IDisposable
{
    private readonly DateTimeOffset sessionStartedAt = DateTimeOffset.UtcNow;
    private readonly List<RoundHistoryEntry> history;
    private readonly HashSet<Guid> knownRoundIds;
    private bool disposed;

    public PlayerProgressionController()
    {
        Service = new ProgressionService(PlayerProfileStore.LoadProfile());
        history = PlayerProfileStore.LoadHistory().ToList();
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
        if (result.Success)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return result;
    }

    public bool EquipCosmetic(string cosmeticId)
    {
        bool equipped = Service.EquipCosmetic(cosmeticId);
        if (equipped)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return equipped;
    }

    public PrestigeResult Prestige()
    {
        PrestigeResult result = Service.Prestige();
        if (result.Success)
        {
            PlayerProfileStore.SaveProfile(Profile);
        }
        return result;
    }

    public void SaveAll()
    {
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
