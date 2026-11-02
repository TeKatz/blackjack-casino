namespace BlackjackCasino.Core.Adventure;

public static class StrangersWagerAdventure
{
    public static AdventureDefinition Build()
    {
        var scenes = new List<AdventureSceneDefinition>
        {
            new(
                "rainy-tavern",
                "The Bent Lantern",
                "Rain has driven half the road into the Bent Lantern. Near the hearth, a stranger in a charcoal coat sits alone with a deck of cards squared neatly between his hands. He has watched you since you entered, but only now does he tap the empty chair opposite him.",
                Location: "The Bent Lantern",
                Choices:
                [
                    new("take-seat", "Take the empty seat opposite the stranger.", "the-offer"),
                    new("ignore-stranger", "Keep your drink, keep your distance and leave him to his cards.", "ending-walk-away"),
                ]),
            new(
                "the-offer",
                "A Game for Something Real",
                "He turns the top card face down without looking at it. \"Coin is dull. Information is better. But tonight I'll settle for proof that you know when to draw and when to stop. One hand of blackjack. Beat me and the token on this table is yours. Lose, and I take five coins.\"",
                "The Ashen Stranger",
                [
                    new("accept-now", "Accept the wager.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("ask-stakes", "Ask why the black iron token matters.", "stakes-explained"),
                    new("decline", "Decline. Some games cost more than their stakes.", "ending-walk-away"),
                ],
                Location: "The Bent Lantern"),
            new(
                "stakes-explained",
                "The Black Iron Token",
                "The token is no common tavern marker. A crowned raven has been stamped into one side; the other is smooth from years of handling. The stranger covers it with two fingers. \"Win first. Questions are cheaper after victory.\"",
                "The Ashen Stranger",
                [
                    new("accept-informed", "Play the hand.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("leave-informed", "Leave the token and the stranger behind.", "ending-walk-away"),
                ],
                Location: "The Bent Lantern"),
            new(
                "blackjack-table",
                "The Stranger's Wager",
                "The tavern seems to recede as the cards hit the wood. For the next few moments there is only the stranger, the deck and the decision of whether one more card will save you or ruin you.",
                Choices:
                [
                    new("encounter-win", "[Encounter result: win]", "ending-win",
                        Effects:
                        [
                            new(AdventureEffectKind.ChangeCoins, Value: 10),
                            new(AdventureEffectKind.AddItem, "black-iron-token"),
                            new(AdventureEffectKind.GainCharacterExperience, Value: 50),
                        ]),
                    new("encounter-loss", "[Encounter result: loss]", "ending-loss",
                        Effects:
                        [
                            new(AdventureEffectKind.ChangeCoins, Value: -5),
                            new(AdventureEffectKind.GainCharacterExperience, Value: 20),
                        ]),
                    new("encounter-draw", "[Encounter result: draw]", "push"),
                ],
                Location: "The Bent Lantern"),
            new(
                "push",
                "No Winner",
                "The stranger turns both hands over, studies the equal totals and gives a quiet laugh. \"The deck dislikes certainty. Again?\" He leaves the token where it is.",
                "The Ashen Stranger",
                [
                    new("rematch", "Play another hand.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("leave-push", "Call the push fair and walk away.", "ending-walk-away"),
                ],
                Location: "The Bent Lantern"),
            new(
                "ending-win",
                "Ending — The Raven Token",
                "The stranger stares at the final cards, then slides the black iron token across the table. \"Keep it. If someone shows you the same raven and asks where you earned yours, tell them the Bent Lantern. Nothing more.\" By the time you look up from the token, his chair is empty.",
                IsEnding: true,
                EndingId: "raven-token",
                Location: "The Bent Lantern"),
            new(
                "ending-loss",
                "Ending — House Lesson",
                "He collects five coins with no triumph in his expression. \"You played the cards honestly. That's rarer than winning.\" The black token disappears beneath his coat, and he returns to his drink as though the wager never happened.",
                IsEnding: true,
                EndingId: "house-lesson",
                Location: "The Bent Lantern"),
            new(
                "ending-walk-away",
                "Ending — An Unplayed Hand",
                "You leave the stranger to his deck. Behind you, cards whisper across wood and someone else takes the empty chair. Whatever the black token meant, it remains their problem tonight.",
                IsEnding: true,
                EndingId: "unplayed-hand",
                Location: "The Bent Lantern"),
        };

        var adventure = new AdventureDefinition(
            "strangers-wager",
            1,
            "The Stranger's Wager",
            "Encounter framework test · A short tavern story with a real embedded blackjack hand.",
            "rainy-tavern",
            scenes,
            Items:
            [
                new("black-iron-token", "Black Iron Token", "A heavy raven-stamped token won from the Ashen Stranger.", "token", AdventureItemKind.Relic),
            ],
            Acts: new Dictionary<string, string>());

        AdventureDefinitionValidator.Validate(adventure);
        AdventureEncounterCatalog.Validate(adventure);
        return adventure;
    }
}
