namespace BlackjackCasino.Core.MidnightMerge;

public static class MergeOrders
{
    public static IReadOnlyList<MergeItemInstance>? SelectRequired(MergeGameState state, MergeOrderDefinition definition)
    {
        var selected = new List<MergeItemInstance>();
        foreach (var requirement in definition.Requirements.GroupBy(x => x.DefinitionId))
        {
            int needed = requirement.Sum(x => x.Count);
            var matches = state.Items.Where(x => x.DefinitionId == requirement.Key).OrderBy(x => x.Position).Take(needed).ToArray();
            if (matches.Length < needed) return null;
            selected.AddRange(matches);
        }
        return selected;
    }
}
