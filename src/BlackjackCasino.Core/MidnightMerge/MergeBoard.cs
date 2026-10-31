namespace BlackjackCasino.Core.MidnightMerge;

/// <summary>Serialized transactions. Persist the entire candidate before exposing it; failed writes leave the live state untouched.</summary>
public sealed class MergeBoard
{
    private readonly object gate = new();
    private readonly Action<MergeGameState>? persist;
    private readonly MergeCatalog catalog;
    private MergeGameState state;

    public MergeBoard(MergeGameState? initial = null, Action<MergeGameState>? persist = null, MergeCatalog? catalog = null)
    {
        this.catalog = catalog ?? MergeCatalog.Default;
        this.persist = persist;
        state = MergeGameState.Normalize(initial, this.catalog);
    }

    public MergeGameState Snapshot { get { lock (gate) return Copy(state); } }
    public bool CanSpawn(string producerId) { lock (gate) return catalog.Producers.ContainsKey(producerId) && state.Items.Count < MergeGameState.Capacity; }
    public bool CanMove(Guid itemId, int target) { lock (gate) return MergeRules.IsPosition(target) && state.Items.Any(x => x.Id == itemId) && state.Items.All(x => x.Position != target); }
    public bool CanMerge(Guid itemId, int target)
    {
        lock (gate)
        {
            var source = state.Items.FirstOrDefault(x => x.Id == itemId);
            var destination = state.Items.FirstOrDefault(x => x.Position == target);
            return source is not null && destination is not null && MergeRules.ValidateMerge(source, destination, catalog) == MergeOutcome.Merged;
        }
    }

    public MergeActionResult TrySpawn(string producerId = "chip-case")
    {
        lock (gate)
        {
            if (!catalog.Producers.TryGetValue(producerId, out var producer)) return new(MergeOutcome.UnknownDefinition);
            int position = Enumerable.Range(0, MergeGameState.Capacity).FirstOrDefault(p => state.Items.All(i => i.Position != p), -1);
            if (position < 0) return new(MergeOutcome.BoardFull);
            var next = Copy(state);
            next.Items.Add(new(Guid.NewGuid(), producer.OutputDefinitionId, position));
            int index = next.Producers.FindIndex(x => x.DefinitionId == producerId);
            next.Producers[index] = next.Producers[index] with { SpawnCount = checked(next.Producers[index].SpawnCount + 1) };
            Commit(next);
            return new(MergeOutcome.Spawned, position, DefinitionId: producer.OutputDefinitionId);
        }
    }

    public MergeActionResult TryMove(Guid itemId, int target) => Relocate(itemId, target, false);
    public MergeActionResult TryMerge(Guid itemId, int target) => Relocate(itemId, target, true);
    public MergeActionResult TryDrop(Guid itemId, int target)
    {
        lock (gate) return Relocate(itemId, target, state.Items.Any(x => x.Position == target));
    }

    private MergeActionResult Relocate(Guid itemId, int target, bool merge)
    {
        lock (gate)
        {
            if (!MergeRules.IsPosition(target)) return new(MergeOutcome.InvalidPosition);
            var source = state.Items.FirstOrDefault(x => x.Id == itemId);
            if (source is null) return new(MergeOutcome.MissingItem, target);
            var destination = state.Items.FirstOrDefault(x => x.Position == target);
            string definition = source.DefinitionId;
            if (merge)
            {
                if (destination is null) return new(MergeOutcome.MissingItem, target);
                var validation = MergeRules.ValidateMerge(source, destination, catalog);
                if (validation != MergeOutcome.Merged) return new(validation, target);
                definition = catalog.Items[definition].MergeResult!;
            }
            else if (destination is not null) return new(MergeOutcome.Occupied, target);
            var next = Copy(state);
            next.Items.RemoveAll(x => x.Id == itemId || (merge && x.Id == destination!.Id));
            next.Items.Add(merge ? new(Guid.NewGuid(), definition, target) : source with { Position = target });
            Commit(next);
            return new(merge ? MergeOutcome.Merged : MergeOutcome.Moved, target, source.Position, definition, SourceDefinitionId: source.DefinitionId);
        }
    }

    public MergeActionResult TryComplete(Guid orderId)
    {
        lock (gate)
        {
            if (state.Receipts.Any(x => x.OrderId == orderId)) return new(MergeOutcome.AlreadyCompleted);
            var order = state.Orders.FirstOrDefault(x => x.Id == orderId && x.Status == "active");
            if (order is null) return new(MergeOutcome.UnknownDefinition);
            var definition = catalog.Orders.Single(x => x.Id == order.DefinitionId);
            var required = MergeOrders.SelectRequired(state, definition);
            if (required is null) return new(MergeOutcome.MissingItems);
            var next = Copy(state) with { RenovationPoints = checked(state.RenovationPoints + definition.Reward) };
            var consumed = required.Select(x => x.Id).ToHashSet();
            next.Items.RemoveAll(x => consumed.Contains(x.Id));
            next.Receipts.Add(new(order.Id, definition.Id, definition.Reward));
            next.Orders[next.Orders.FindIndex(x => x.Id == orderId)] = new(Guid.NewGuid(), definition.Id);
            var milestones = MergeProgression.Earned(catalog, next.RenovationPoints).Except(state.UnlockedMilestones).ToArray();
            next.UnlockedMilestones.AddRange(milestones);
            Commit(next);
            return new(MergeOutcome.OrderCompleted, Reward: definition.Reward, Milestones: milestones);
        }
    }

    private void Commit(MergeGameState next)
    {
        next = next with { Revision = checked(state.Revision + 1) };
        persist?.Invoke(Copy(next));
        state = next;
    }
    private static MergeGameState Copy(MergeGameState value) => value with
    {
        Items = [.. value.Items], Producers = [.. value.Producers], Orders = [.. value.Orders],
        Receipts = [.. value.Receipts], UnlockedMilestones = [.. value.UnlockedMilestones],
    };
}
