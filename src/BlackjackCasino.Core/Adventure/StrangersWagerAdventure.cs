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
                "Rain has driven half the road into the Bent Lantern. Near the hearth, a stranger in a charcoal coat sits alone with a deck of cards squared neatly between his hands. Five bone dice rest beside the deck. He has watched you since you entered, but only now does he tap the empty chair opposite him.",
                Location: "The Bent Lantern",
                Choices:
                [
                    new("take-seat", "Take the empty seat opposite the stranger.", "the-offer"),
                    new("ignore-stranger", "Keep your drink, keep your distance and leave him to his games.", "ending-walk-away"),
                ]),
            new(
                "the-offer",
                "A Game for Something Real",
                "He turns the top card face down without looking at it. \"Coin is dull. Information is better. Tonight I only need proof that you know when chance has given you enough. Cards or dice. Your choice. Beat me and the token on this table is yours. Lose, and I take five coins.\"",
                "The Ashen Stranger",
                [
                    new("accept-now", "Choose the cards. Play one hand of blackjack.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("choose-dice", "Choose the bone dice. Three throws, best scored hand.", "five-kind-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("ask-stakes", "Ask why the black iron token matters.", "stakes-explained"),
                    new("decline", "Decline. Some games cost more than their stakes.", "ending-walk-away"),
                ],
                Location: "The Bent Lantern"),
            new(
                "stakes-explained",
                "The Black Iron Token",
                "The token is no common tavern marker. A crowned raven has been stamped into one side; the other is smooth from years of handling. The stranger covers it with two fingers. \"Win first. Questions are cheaper after victory. Cards or dice; the token does not care.\"",
                "The Ashen Stranger",
                [
                    new("accept-informed", "Play the blackjack hand.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("dice-informed", "Take the five bone dice instead.", "five-kind-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
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
                "five-kind-table",
                "Bones on the Table",
                "The deck disappears beneath the stranger's sleeve. Five pale dice take its place. You each get one turn and no more than three throws. A die you trust can stay. When you bank the hand, its strongest real Five of a Kind score becomes the mark to beat.",
                Choices:
                [
                    new("dice-encounter-win", "[Encounter result: win]", "ending-dice-win",
                        Effects:
                        [
                            new(AdventureEffectKind.ChangeCoins, Value: 10),
                            new(AdventureEffectKind.AddItem, "black-iron-token"),
                            new(AdventureEffectKind.GainCharacterExperience, Value: 50),
                        ]),
                    new("dice-encounter-loss", "[Encounter result: loss]", "ending-dice-loss",
                        Effects:
                        [
                            new(AdventureEffectKind.ChangeCoins, Value: -5),
                            new(AdventureEffectKind.GainCharacterExperience, Value: 20),
                        ]),
                    new("dice-encounter-draw", "[Encounter result: draw]", "dice-push"),
                ],
                Location: "The Bent Lantern"),
            new(
                "push",
                "No Winner",
                "The stranger turns both hands over, studies the equal totals and gives a quiet laugh. \"The deck dislikes certainty. Again?\" He leaves the token where it is.",
                "The Ashen Stranger",
                [
                    new("rematch", "Play another hand.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("switch-to-dice", "Let the cards rest. Settle it with the dice instead.", "five-kind-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("leave-push", "Call the push fair and walk away.", "ending-walk-away"),
                ],
                Location: "The Bent Lantern"),
            new(
                "dice-push",
                "The Same Number",
                "For a moment the stranger simply looks from your dice to his. Then one corner of his mouth moves. \"Equal marks. The bones have a sense of humor. Again?\"",
                "The Ashen Stranger",
                [
                    new("dice-rematch", "Roll another showdown.", "five-kind-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("switch-to-cards", "Change the wager. Deal the cards.", "blackjack-table", Conditions: [new(AdventureConditionKind.CoinsMinimum, Value: 5)]),
                    new("leave-dice-push", "Accept the draw and leave the table.", "ending-walk-away"),
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
                "ending-dice-win",
                "Ending — Five Bones, One Raven",
                "Your final dice stop moving. The stranger studies the score, then the pattern on the table, and pushes the raven token to you with one finger. \"Chance is only noise until someone knows what to keep.\" When the next burst of tavern laughter turns your head, his chair is empty.",
                IsEnding: true,
                EndingId: "raven-token-dice",
                Location: "The Bent Lantern"),
            new(
                "ending-dice-loss",
                "Ending — The Bones Remember",
                "The stranger's final die settles and leaves his mark above yours. He takes five coins, gathers the bone dice and nods once. \"You kept the right ones. The wrong ones simply answered louder.\" The raven token vanishes beneath his coat.",
                IsEnding: true,
                EndingId: "bones-remember",
                Location: "The Bent Lantern"),
            new(
                "ending-walk-away",
                "Ending — An Unplayed Wager",
                "You leave the stranger to his cards and bone dice. Behind you, something rattles across wood and someone else takes the empty chair. Whatever the black token meant, it remains their problem tonight.",
                IsEnding: true,
                EndingId: "unplayed-wager",
                Location: "The Bent Lantern"),
        };

        var adventure = new AdventureDefinition(
            "strangers-wager",
            2,
            "The Stranger's Wager",
            "Encounter framework test · A short tavern story with real embedded blackjack and Five of a Kind encounters.",
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
