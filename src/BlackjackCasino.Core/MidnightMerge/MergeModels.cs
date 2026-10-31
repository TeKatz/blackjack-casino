namespace BlackjackCasino.Core.MidnightMerge;

public sealed record MergeVisual(string Color, string Mark, int StackCount);
public sealed record MergeProducerDefinition(string Id, string DisplayName, string OutputDefinitionId);
public sealed record MergeItemDefinition(
    string Id, string DisplayName, string LineId, int Tier, int MaxTier,
    MergeVisual Visual, string? MergeResult, IReadOnlyList<string> Tags);
public sealed record MergeItemLine(string Id, IReadOnlyList<string> Tiers);
public sealed record MergeItemInstance(Guid Id, string DefinitionId, int Position, string Status = "ready");
public sealed record MergeProducerState(string DefinitionId, long SpawnCount = 0);
public sealed record MergeRequirement(string DefinitionId, int Count);
public sealed record MergeOrderDefinition(string Id, IReadOnlyList<MergeRequirement> Requirements, long Reward);
public sealed record MergeOrderInstance(Guid Id, string DefinitionId, string Status = "active");
public sealed record MergeCompletionReceipt(Guid OrderId, string DefinitionId, long Reward);
public sealed record MergeMilestone(string Id, string DisplayName, long Points);
public sealed record MergeArea(string Id, string DisplayName, IReadOnlyList<MergeMilestone> Milestones);

public enum MergeOutcome
{
    Spawned, Moved, Merged, OrderCompleted,
    InvalidPosition, MissingItem, Occupied, Incompatible, MaxTier, BoardFull, MissingItems, AlreadyCompleted, UnknownDefinition,
}

public sealed record MergeActionResult(MergeOutcome Outcome, int Position = -1, int FromPosition = -1,
    string? DefinitionId = null, long Reward = 0, IReadOnlyList<string>? Milestones = null, string? SourceDefinitionId = null)
{
    public bool Success => Outcome is MergeOutcome.Spawned or MergeOutcome.Moved or MergeOutcome.Merged or MergeOutcome.OrderCompleted;
}
