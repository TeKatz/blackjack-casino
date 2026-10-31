namespace BlackjackCasino.Core.MidnightMerge;

public sealed record MergeGameState
{
    public const int CurrentSchemaVersion = 1;
    public const int Columns = 7;
    public const int Rows = 6;
    public const int Capacity = Columns * Rows;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public List<MergeItemInstance> Items { get; init; } = [];
    public List<MergeProducerState> Producers { get; init; } = [];
    public List<MergeOrderInstance> Orders { get; init; } = [];
    public List<MergeCompletionReceipt> Receipts { get; init; } = [];
    public long RenovationPoints { get; init; }
    public List<string> UnlockedMilestones { get; init; } = [];
    public long Revision { get; init; }

    public static MergeGameState Normalize(MergeGameState? state, MergeCatalog? catalog = null)
    {
        catalog ??= MergeCatalog.Default;
        state ??= new();
        if (state.SchemaVersion > CurrentSchemaVersion)
            throw new NotSupportedException("This Midnight Merge save requires a newer application.");
        var ids = new HashSet<Guid>();
        var occupied = new HashSet<int>();
        var items = new List<MergeItemInstance>();
        foreach (var item in state.Items ?? [])
        {
            if (item is null || string.IsNullOrWhiteSpace(item.DefinitionId) || !catalog.Items.ContainsKey(item.DefinitionId) || item.Id == Guid.Empty || !ids.Add(item.Id)) continue;
            int position = item.Position;
            if (position < 0 || position >= Capacity || occupied.Contains(position))
                position = Enumerable.Range(0, Capacity).FirstOrDefault(p => !occupied.Contains(p), -1);
            if (position < 0) continue;
            occupied.Add(position);
            items.Add(item with { Position = position, Status = "ready" });
        }
        var receipts = (state.Receipts ?? []).Where(x => x is not null && x.OrderId != Guid.Empty && x.Reward > 0)
            .DistinctBy(x => x.OrderId).ToList();
        var completed = receipts.Select(x => x.OrderId).ToHashSet();
        var orderIds = new HashSet<Guid>();
        var orders = new List<MergeOrderInstance>();
        foreach (var definition in catalog.Orders)
        {
            var existing = (state.Orders ?? []).FirstOrDefault(x => x is not null && x.DefinitionId == definition.Id &&
                x.Status == "active" && x.Id != Guid.Empty && !completed.Contains(x.Id) && !orderIds.Contains(x.Id));
            var active = existing ?? new MergeOrderInstance(Guid.NewGuid(), definition.Id);
            orderIds.Add(active.Id);
            orders.Add(active);
        }
        long points = Math.Max(0, state.RenovationPoints);
        var unlocked = (state.UnlockedMilestones ?? []).Where(id => catalog.Areas.Any(a => a.Milestones.Any(m => $"{a.Id}/{m.Id}" == id)))
            .Concat(MergeProgression.Earned(catalog, points)).Distinct(StringComparer.Ordinal).ToList();
        return state with
        {
            SchemaVersion = CurrentSchemaVersion, Items = items, Orders = orders, Receipts = receipts,
            Producers = catalog.Producers.Keys.Select(id => new MergeProducerState(id,
                Math.Max(0, (state.Producers ?? []).FirstOrDefault(x => x is not null && x.DefinitionId == id)?.SpawnCount ?? 0))).ToList(),
            RenovationPoints = points, UnlockedMilestones = unlocked, Revision = Math.Max(0, state.Revision),
        };
    }
}
