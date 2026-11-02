namespace BlackjackCasino.Core.Adventure;

public enum AdventureEncounterType
{
    Blackjack,
    FiveOfAKind,
}

public enum AdventureEncounterOutcome
{
    Win,
    Loss,
    Draw,
}

public enum AdventureFiveOfAKindOpponentStyle
{
    Balanced,
    Collector,
    Adventurous,
}

public sealed record AdventureBlackjackEncounterRules(
    int DeckCount = 1,
    bool DealerHitsSoft17 = false,
    bool DealerPeeksForBlackjack = true,
    decimal BlackjackPayout = 1.5m,
    decimal VirtualBet = 1m);

public sealed record AdventureFiveOfAKindEncounterRules(
    AdventureFiveOfAKindOpponentStyle OpponentStyle = AdventureFiveOfAKindOpponentStyle.Adventurous,
    bool AllowEarlyBank = true);

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
    AdventureBlackjackEncounterRules? Blackjack = null,
    AdventureFiveOfAKindEncounterRules? FiveOfAKind = null)
{
    public string ChoiceIdFor(AdventureEncounterOutcome outcome) => outcome switch
    {
        AdventureEncounterOutcome.Win => WinChoiceId,
        AdventureEncounterOutcome.Loss => LossChoiceId,
        AdventureEncounterOutcome.Draw => DrawChoiceId,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };
}

public static class AdventureEncounterCatalog
{
    private static readonly IReadOnlyList<AdventureEncounterDefinition> Encounters =
    [
        // Temporary encounter-framework test tale.
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
        new(
            Id: "ashen-stranger-five-kind",
            AdventureId: "strangers-wager",
            SceneId: "five-kind-table",
            Type: AdventureEncounterType.FiveOfAKind,
            Title: "Bones on the Table",
            OpponentName: "The Ashen Stranger",
            Intro: "The stranger sweeps the cards aside and sets five bone dice on the wood. Three throws each. Keep what you trust, throw the rest, then bank the strongest hand the dice will give you.",
            Stakes: "One turn each · up to 3 rolls · hold any dice · highest scored hand wins",
            WinChoiceId: "dice-encounter-win",
            LossChoiceId: "dice-encounter-loss",
            DrawChoiceId: "dice-encounter-draw",
            FiveOfAKind: new AdventureFiveOfAKindEncounterRules()),

        // The Drowned Bell — the same framework now serves real campaign content.
        new(
            Id: "greyhaven-varro-blackjack",
            AdventureId: "drowned-bell",
            SceneId: "varro-blackjack",
            Type: AdventureEncounterType.Blackjack,
            Title: "One Hand for the Route",
            OpponentName: "Captain Varro",
            Intro: "Mara deals beneath a low whale-oil lamp while Varro keeps one hand beside the folded route chart. One hand decides whether the Lower Choir map changes owners.",
            Stakes: "Win: Lower Choir chart · Lose: Varro keeps the route · Push: play again or walk",
            WinChoiceId: "varro-game-win",
            LossChoiceId: "varro-game-loss",
            DrawChoiceId: "varro-game-draw",
            Blackjack: new AdventureBlackjackEncounterRules()),
        new(
            Id: "greyhaven-bram-five-kind",
            AdventureId: "drowned-bell",
            SceneId: "bram-dice",
            Type: AdventureEncounterType.FiveOfAKind,
            Title: "Five Bones",
            OpponentName: "Bram Kestrel",
            Intro: "Five old bone dice strike the black felt. Bram gives you three throws and the freedom to keep whatever the sea seems willing to leave in your hand.",
            Stakes: "Win: Tomas's oilskin map · one turn each · up to 3 rolls",
            WinChoiceId: "bram-game-win",
            LossChoiceId: "bram-game-loss",
            DrawChoiceId: "bram-game-draw",
            FiveOfAKind: new AdventureFiveOfAKindEncounterRules(AdventureFiveOfAKindOpponentStyle.Collector)),
    ];

    public static IReadOnlyList<AdventureEncounterDefinition> All => Encounters;

    public static bool TryGet(string adventureId, string sceneId, out AdventureEncounterDefinition? encounter)
    {
        encounter = Encounters.FirstOrDefault(candidate =>
            string.Equals(candidate.AdventureId, adventureId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.SceneId, sceneId, StringComparison.OrdinalIgnoreCase));
        return encounter is not null;
    }

    /// <summary>
    /// Resolves whether selecting a story choice immediately enters a registered minigame encounter.
    /// Presentation can use this before the transition to clearly signal Blackjack or Five of a Kind choices.
    /// </summary>
    public static bool TryGetTriggeredByChoice(
        string adventureId,
        AdventureChoiceDefinition choice,
        out AdventureEncounterDefinition? encounter)
    {
        ArgumentNullException.ThrowIfNull(choice);
        if (string.IsNullOrWhiteSpace(choice.NextSceneId))
        {
            encounter = null;
            return false;
        }

        return TryGet(adventureId, choice.NextSceneId, out encounter);
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
            if (encounter.Type == AdventureEncounterType.FiveOfAKind && encounter.FiveOfAKind is null)
                throw new ArgumentException($"Five of a Kind encounter '{encounter.Id}' requires dice rules.", nameof(adventure));
        }
    }
}