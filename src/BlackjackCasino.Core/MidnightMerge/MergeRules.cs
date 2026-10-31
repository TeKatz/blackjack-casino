namespace BlackjackCasino.Core.MidnightMerge;

public static class MergeRules
{
    public static bool IsPosition(int position) => position >= 0 && position < MergeGameState.Capacity;
    public static MergeOutcome ValidateMerge(MergeItemInstance source, MergeItemInstance target, MergeCatalog catalog)
    {
        if (source.Id == target.Id) return MergeOutcome.Incompatible;
        if (!catalog.Items.TryGetValue(source.DefinitionId, out var a) || !catalog.Items.TryGetValue(target.DefinitionId, out var b))
            return MergeOutcome.UnknownDefinition;
        if (a.Id != b.Id || a.LineId != b.LineId || a.Tier != b.Tier) return MergeOutcome.Incompatible;
        return a.MergeResult is null || a.Tier >= a.MaxTier ? MergeOutcome.MaxTier : MergeOutcome.Merged;
    }
}
