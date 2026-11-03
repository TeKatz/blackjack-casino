namespace BlackjackCasino.Core.Adventure;

/// <summary>Content upgrades preserve run identity, investigation progress and reward history.</summary>
public static class AdventureRunMigrations
{
    public static bool Apply(AdventureDefinition definition, AdventureRunState state)
    {
        if (definition.Id != DeadMansHandAdventure.AdventureId || state.AdventureId != definition.Id ||
            state.AdventureVersion >= definition.Version || definition.Version < 2) return false;

        if (state.Completed && state.EndingId == "foundation-preview" && state.CurrentSceneId == "foundation-checkpoint")
        {
            state.Completed = false;
            state.EndingId = null;
            state.PendingChoiceId = null;
            state.Statistics.CompletedAtUtc = null;
        }
        // The opening originally pinned these physical objects without adding inventory entries.
        foreach ((string node, string item) in new[]
        {
            ("key-306", "dmh-key"), ("cufflink", "dmh-cufflink"),
            ("torn-photo", "dmh-photo"), ("switchboard-log", "dmh-log"),
        })
        {
            if (state.CaseBoard.Nodes.TryGetValue(node, out CaseNodeState? known) && known.IsKnown)
                RestoreItem(state, item);
        }
        if (state.Statistics.UniqueSceneIds.Contains("phone-detail")) RestoreItem(state, "dmh-bridge");
        state.AdventureVersion = definition.Version;
        return true;
    }

    private static void RestoreItem(AdventureRunState state, string id)
    {
        state.ItemStacks.TryAdd(id, new AdventureItemState { ItemId = id, Quantity = 1 });
        state.Inventory.Add(id);
    }
}
