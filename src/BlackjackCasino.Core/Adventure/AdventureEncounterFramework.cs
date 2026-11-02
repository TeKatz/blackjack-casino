namespace BlackjackCasino.Core.Adventure;

public enum AdventureEncounterType
{
    Blackjack,
}

public enum AdventureEncounterOutcome
{
    Win,
    Loss,
    Draw,
}

public sealed record AdventureBlackjackEncounterRules(
    int DeckCount = 1,
    bool DealerHitsSoft17 = false,
    bool DealerPeeksForBlackjack = true,
    decimal BlackjackPayout = 1.5m,
    decimal VirtualBet = 1m);

public sealed record AdventureEncounterDefinition(
    string Id,
    string AdventureId,
    string SceneId,
    AdventureEncounterType Type,
    string Title,
    string OpponentName,
    string Intro,
    string Stakes,
    string WinChoiceId,
    string LossChoiceId,
    string DrawChoiceId,
    AdventureBlackjackEncounterRules? Blackjack = null);

public static class AdventureEncounterCatalog
{
    private static readonly IReadOnlyList<AdventureEncounterDefinition> Encounters =
    [
        new(
            Id: "ashen-stranger-blackjack",
            AdventureId: "strangers-wager",
            SceneId: "blackjack-table",
            Type: AdventureEncounterType.Blackjack,
            Title: "The Stranger's Wager",
            OpponentName: "The Ashen Stranger",
            Intro: "The stranger clears two mugs aside, cuts a weathered deck once and deals without ceremony. One hand. No second chances unless the cards themselves refuse to choose.",
            Stakes: "Win: 10 coins + Black Iron Token · Lose: 5 coins · Push: rematch or walk away",
            WinChoiceId: "encounter-win",
            LossChoiceId: "encounter-loss",
            DrawChoiceId: "encounter-draw",
            Blackjack: new AdventureBlackjackEncounterRules()),
    ];

    public static IReadOnlyList<AdventureEncounterDefinition> All => Encounters;

    public static bool TryGet(string adventureId, string sceneId, out AdventureEncounterDefinition? encounter)
    {
        encounter = Encounters.FirstOrDefault(candidate =>
            string.Equals(candidate.AdventureId, adventureId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.SceneId, sceneId, StringComparison.OrdinalIgnoreCase));
        return encounter is not null;
    }

    public static void Validate(AdventureDefinition adventure)
    {
        ArgumentNullException.ThrowIfNull(adventure);
        IReadOnlyList<AdventureEncounterDefinition> encounters = Encounters
            .Where(candidate => string.Equals(candidate.AdventureId, adventure.Id, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (encounters.Count == 0) return;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureEncounterDefinition encounter in encounters)
        {
            if (string.IsNullOrWhiteSpace(encounter.Id) || !ids.Add(encounter.Id))
                throw new ArgumentException($"Adventure '{adventure.Id}' contains a blank or duplicate encounter id '{encounter.Id}'.", nameof(adventure));

            AdventureSceneDefinition scene = adventure.Scenes.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, encounter.SceneId, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Encounter '{encounter.Id}' references missing scene '{encounter.SceneId}'.", nameof(adventure));

            var choices = (scene.Choices ?? []).ToDictionary(choice => choice.Id, StringComparer.OrdinalIgnoreCase);
            foreach (string choiceId in new[] { encounter.WinChoiceId, encounter.LossChoiceId, encounter.DrawChoiceId })
            {
                if (!choices.ContainsKey(choiceId))
                    throw new ArgumentException($"Encounter '{encounter.Id}' result choice '{choiceId}' is missing from scene '{scene.Id}'.", nameof(adventure));
            }

            if (encounter.Type == AdventureEncounterType.Blackjack && encounter.Blackjack is null)
                throw new ArgumentException($"Blackjack encounter '{encounter.Id}' requires blackjack rules.", nameof(adventure));
        }
    }
}
