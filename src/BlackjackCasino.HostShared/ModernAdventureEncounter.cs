using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Owns an in-progress, local non-wagering story encounter. All cards, dice, outcomes
/// and opponent decisions are native; the browser only requests an allowed action.
/// As with the original WPF minigames, a fully unfinished hand is restarted on a new app session.
/// </summary>
internal sealed class ModernAdventureEncounter
{
    private readonly AdventureEncounterDefinition definition;
    private readonly BlackjackRound? blackjack;
    private readonly KniffelGame? playerDice;
    private readonly KniffelGame? opponentDice;
    private int[] playerFinal = [];
    private int[] opponentFinal = [];
    private bool[] playerHeld = [];
    private KniffelCategory? playerCategory, opponentCategory;
    private int? playerScore, opponentScore;
    private AdventureEncounterOutcome? outcome;

    internal AdventureEncounterDefinition Definition => definition;
    internal AdventureEncounterOutcome? Outcome => outcome;

    internal ModernAdventureEncounter(AdventureEncounterDefinition definition)
    {
        this.definition = definition;
        if (definition.Type == AdventureEncounterType.Blackjack)
        {
            AdventureBlackjackEncounterRules ruleset = definition.Blackjack
                ?? throw new InvalidOperationException("The story Blackjack encounter has no rules.");
            var rules = new GameRules
            {
                DeckCount = Math.Clamp(ruleset.DeckCount, 1, 8),
                DealerHitsSoft17 = ruleset.DealerHitsSoft17,
                DealerPeeksForBlackjack = ruleset.DealerPeeksForBlackjack,
                DealerTakesHoleCard = true,
                AllowInsurance = false,
                AllowLateSurrender = false,
                DoubleRule = DoubleRule.AnyTwo,
                DoubleAfterSplit = false,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 1,
                BlackjackPayout = ruleset.BlackjackPayout,
                Penetration = .75,
                MinimumBet = ruleset.VirtualBet,
                MaximumBet = ruleset.VirtualBet,
            };
            blackjack = new BlackjackRound(rules, new Shoe(rules), ruleset.VirtualBet);
            blackjack.Start();
            if (blackjack.Phase == RoundPhase.InsuranceDecision) blackjack.DeclineInsurance();
            SetBlackjackOutcome();
        }
        else if (definition.Type == AdventureEncounterType.FiveOfAKind)
        {
            playerDice = new KniffelGame();
            opponentDice = new KniffelGame();
        }
        else throw new ArgumentOutOfRangeException(nameof(definition), "Unsupported adventure encounter.");
    }

    internal object Snapshot()
    {
        object? blackjackState = null;
        object? diceState = null;
        if (blackjack is not null)
        {
            bool finished = blackjack.Phase == RoundPhase.Settled;
            blackjackState = new
            {
                Phase = blackjack.Phase.ToString(),
                Dealer = blackjack.DealerHand.Cards.Select((c, i) => new
                {
                    Hidden = i == 1 && !finished,
                    Rank = i == 1 && !finished ? null : c.Rank.ToString(),
                    Suit = i == 1 && !finished ? null : c.Suit.ToString(),
                }).ToArray(),
                Player = blackjack.PlayerHands[0].Cards.Select(c =>
                    new { Hidden = false, Rank = (string?)c.Rank.ToString(), Suit = (string?)c.Suit.ToString() }).ToArray(),
                PlayerTotal = blackjack.PlayerHands[0].Value.Total,
                DealerTotal = finished ? (int?)blackjack.DealerHand.Value.Total : null,
                CanHit = blackjack.GetAvailableActions().Contains(PlayerAction.Hit),
                CanStand = blackjack.GetAvailableActions().Contains(PlayerAction.Stand),
                Net = finished ? (decimal?)(blackjack.Result?.TotalNet ?? 0m) : null,
            };
        }
        if (playerDice is not null && opponentDice is not null)
        {
            (KniffelCategory Category, int Points)? best = playerDice.CanScore ? Best(playerDice) : null;
            diceState = new
            {
                Player = playerScore is null ? playerDice.Dice.ToArray() : playerFinal,
                Held = playerScore is null ? playerDice.Held.ToArray() : playerHeld,
                Opponent = opponentScore is null ? Array.Empty<int>() : opponentFinal,
                RollsUsed = playerDice.RollsUsed,
                CanRoll = playerScore is null && playerDice.CanRoll,
                CanBank = playerScore is null && playerDice.CanScore &&
                    (definition.FiveOfAKind?.AllowEarlyBank == true || playerDice.RollsUsed == 3),
                BestCategory = best?.Category.ToString(),
                BestPoints = best?.Points,
                PlayerScore = playerScore,
                OpponentScore = opponentScore,
                PlayerCategory = playerCategory?.ToString(),
                OpponentCategory = opponentCategory?.ToString(),
            };
        }
        return new
        {
            definition.Id, Type = definition.Type.ToString(), definition.Title,
            definition.OpponentName, definition.Intro, definition.Stakes,
            Outcome = outcome?.ToString(),
            Blackjack = blackjackState, Dice = diceState,
        };
    }

    internal bool Hit()
    {
        if (blackjack is null || outcome is not null ||
            !blackjack.GetAvailableActions().Contains(PlayerAction.Hit)) return false;
        blackjack.Act(PlayerAction.Hit);
        SetBlackjackOutcome();
        return true;
    }

    internal bool Stand()
    {
        if (blackjack is null || outcome is not null ||
            !blackjack.GetAvailableActions().Contains(PlayerAction.Stand)) return false;
        blackjack.Act(PlayerAction.Stand);
        SetBlackjackOutcome();
        return true;
    }

    internal bool Roll()
    {
        if (playerDice is null || outcome is not null || playerScore is not null ||
            !playerDice.CanRoll) return false;
        playerDice.Roll();
        return true;
    }

    internal bool Hold(int index)
    {
        if (playerDice is null || outcome is not null || playerScore is not null ||
            index is < 0 or >= 5 || !playerDice.CanScore || playerDice.RollsUsed == 3) return false;
        playerDice.ToggleHold(index);
        return true;
    }

    internal bool Bank()
    {
        if (playerDice is null || opponentDice is null || playerScore is not null ||
            !playerDice.CanScore ||
            !(definition.FiveOfAKind?.AllowEarlyBank == true || playerDice.RollsUsed == 3))
            return false;
        (KniffelCategory category, _) = Best(playerDice);
        playerFinal = playerDice.Dice.ToArray();
        playerHeld = playerDice.Held.ToArray();
        playerCategory = category;
        playerScore = playerDice.Commit(category);
        var style = definition.FiveOfAKind?.OpponentStyle switch
        {
            AdventureFiveOfAKindOpponentStyle.Collector => KniffelAiStyle.Collector,
            AdventureFiveOfAKindOpponentStyle.Balanced => KniffelAiStyle.Balanced,
            _ => KniffelAiStyle.Adventurous,
        };
        // Use the same native Kniffel AI choices as the WPF encounter, retaining the
        // final five rolled dice before Commit clears the turn state.
        opponentDice.Roll();
        while (true)
        {
            KniffelAiChoice choice = KniffelAi.Choose(opponentDice, style);
            if (choice.ScoreNow || opponentDice.RollsUsed == 3)
            {
                opponentFinal = opponentDice.Dice.ToArray();
                opponentCategory = choice.Category;
                opponentScore = opponentDice.Commit(choice.Category);
                break;
            }
            for (int i = 0; i < 5; i++)
            {
                bool shouldHold = (choice.HoldMask & (1 << i)) != 0;
                if (opponentDice.Held[i] != shouldHold) opponentDice.ToggleHold(i);
            }
            opponentDice.Roll();
        }
        outcome = playerScore > opponentScore ? AdventureEncounterOutcome.Win
            : playerScore < opponentScore ? AdventureEncounterOutcome.Loss
            : AdventureEncounterOutcome.Draw;
        return true;
    }

    private void SetBlackjackOutcome()
    {
        if (blackjack?.Phase != RoundPhase.Settled) return;
        decimal net = blackjack.Result?.TotalNet ?? 0m;
        outcome = net > 0m ? AdventureEncounterOutcome.Win :
            net < 0m ? AdventureEncounterOutcome.Loss : AdventureEncounterOutcome.Draw;
    }

    private static (KniffelCategory Category, int Points) Best(KniffelGame game) =>
        KniffelScoring.Categories.Select(c => (Category: c, Points: game.Preview(c)))
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.Category == KniffelCategory.Kniffel).First();
}
