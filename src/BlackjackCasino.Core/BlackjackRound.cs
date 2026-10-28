namespace BlackjackCasino.Core;

public enum RoundPhase
{
    NotStarted,
    InsuranceDecision,
    PlayerTurn,
    DealerTurn,
    Settled,
}

public enum PlayerAction
{
    Hit,
    Stand,
    DoubleDown,
    Split,
    Surrender,
}

public enum HandOutcome
{
    Win,
    Loss,
    Push,
    Blackjack,
    Surrender,
}

public sealed record HandResult(int HandIndex, HandOutcome Outcome, decimal Net);

public sealed record RoundResult(IReadOnlyList<HandResult> Hands, decimal InsuranceNet)
{
    public decimal TotalNet => Hands.Sum(x => x.Net) + InsuranceNet;
}

public sealed class BlackjackRound
{
    private readonly GameRules rules;
    private readonly ICardShoe shoe;
    private readonly List<BlackjackHand> playerHands = [];
    private readonly List<AiSeatState> aiSeats = [];
    private readonly List<PlayerActionRecord> actionHistory = [];
    private int activeHandIndex;
    private decimal insuranceBet;

    public BlackjackRound(
        GameRules rules,
        ICardShoe shoe,
        decimal initialBet,
        IReadOnlyList<AiPlayerProfile>? aiPlayers = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(shoe);
        rules.Validate();

        if (initialBet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBet), "Initial bet must be positive.");
        }

        if (aiPlayers is { Count: > 5 })
        {
            throw new ArgumentOutOfRangeException(nameof(aiPlayers), "A table supports at most five AI seats.");
        }

        this.rules = rules;
        this.shoe = shoe;
        InitialBet = initialBet;
        DealerHand = new BlackjackHand(initialBet);
        playerHands.Add(new BlackjackHand(initialBet));

        if (aiPlayers is not null)
        {
            foreach (AiPlayerProfile profile in aiPlayers)
            {
                aiSeats.Add(new AiSeatState(profile));
            }
        }
    }

    public decimal InitialBet { get; }

    public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;

    public IReadOnlyList<BlackjackHand> PlayerHands => playerHands;

    public IReadOnlyList<AiSeatState> AiSeats => aiSeats;

    public BlackjackHand DealerHand { get; }

    public int ActiveHandIndex => activeHandIndex;

    public BlackjackHand? ActiveHand => Phase == RoundPhase.PlayerTurn ? playerHands[activeHandIndex] : null;

    public decimal InsuranceBet => insuranceBet;

    public IReadOnlyList<PlayerActionRecord> ActionHistory => actionHistory;

    public RoundResult? Result { get; private set; }

    public void Start()
    {
        EnsurePhase(RoundPhase.NotStarted);

        DealOneCardToOccupiedSeats();
        DealerHand.Add(shoe.Draw());
        DealOneCardToOccupiedSeats();

        if (rules.DealerTakesHoleCard)
        {
            DealerHand.Add(shoe.Draw());
        }

        if (rules.AllowInsurance && DealerHand.Cards[0].Rank == Rank.Ace)
        {
            Phase = RoundPhase.InsuranceDecision;
            return;
        }

        ResolveOpeningState();
    }

    public void TakeInsurance(decimal? amount = null)
    {
        EnsurePhase(RoundPhase.InsuranceDecision);

        decimal requested = amount ?? InitialBet / 2m;
        if (requested <= 0 || requested > InitialBet / 2m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Insurance must be greater than zero and no more than half the initial bet.");
        }

        insuranceBet = requested;
        ResolveOpeningState();
    }

    public void DeclineInsurance()
    {
        EnsurePhase(RoundPhase.InsuranceDecision);
        insuranceBet = 0;
        ResolveOpeningState();
    }

    public IReadOnlySet<PlayerAction> GetAvailableActions()
    {
        if (Phase != RoundPhase.PlayerTurn)
        {
            return new HashSet<PlayerAction>();
        }

        return GetAvailableActionsForHands(playerHands, activeHandIndex);
    }

    public void Act(PlayerAction action)
    {
        EnsurePhase(RoundPhase.PlayerTurn);

        IReadOnlySet<PlayerAction> allowed = GetAvailableActions();
        if (!allowed.Contains(action))
        {
            throw new InvalidOperationException($"Action {action} is not available for the active hand.");
        }

        int handIndex = activeHandIndex;
        actionHistory.Add(new PlayerActionRecord(handIndex, action));
        ExecuteAction(playerHands, activeHandIndex, action);
        AdvanceAfterPlayerAction();
    }

    private void DealOneCardToOccupiedSeats()
    {
        playerHands[0].Add(shoe.Draw());
        foreach (AiSeatState seat in aiSeats)
        {
            seat.MutableHands[0].Add(shoe.Draw());
        }
    }

    private void ResolveOpeningState()
    {
        bool dealerCanPeek = rules.DealerTakesHoleCard
            && rules.DealerPeeksForBlackjack
            && DealerHand.Cards.Count == 2
            && (DealerHand.Cards[0].Rank == Rank.Ace || DealerHand.Cards[0].IsTenValue);

        if (dealerCanPeek && DealerHand.IsNaturalBlackjack)
        {
            Settle();
            return;
        }

        if (playerHands[0].IsNaturalBlackjack)
        {
            playerHands[0].IsComplete = true;
            FinishTableAndSettle();
            return;
        }

        Phase = RoundPhase.PlayerTurn;
        activeHandIndex = 0;
    }

    private IReadOnlySet<PlayerAction> GetAvailableActionsForHands(List<BlackjackHand> hands, int handIndex)
    {
        BlackjackHand hand = hands[handIndex];
        var actions = new HashSet<PlayerAction> { PlayerAction.Stand };

        if (!hand.IsSplitAces || rules.HitSplitAces)
        {
            actions.Add(PlayerAction.Hit);
        }

        if (hand.Cards.Count == 2
            && rules.IsDoubleAllowed(hand.Value.Total)
            && (!hand.IsSplitHand || rules.DoubleAfterSplit)
            && (!hand.IsSplitAces || rules.HitSplitAces))
        {
            actions.Add(PlayerAction.DoubleDown);
        }

        if (CanSplit(hand, hands.Count))
        {
            actions.Add(PlayerAction.Split);
        }

        if (rules.AllowLateSurrender
            && !hand.IsSplitHand
            && hand.Cards.Count == 2
            && CanOfferLateSurrender())
        {
            actions.Add(PlayerAction.Surrender);
        }

        return actions;
    }

    private bool CanOfferLateSurrender()
    {
        Card upCard = DealerHand.Cards[0];
        if (upCard.Rank != Rank.Ace && !upCard.IsTenValue)
        {
            return true;
        }

        return rules.DealerTakesHoleCard && rules.DealerPeeksForBlackjack;
    }

    private bool CanSplit(BlackjackHand hand, int handCount)
    {
        if (hand.Cards.Count != 2 || handCount >= rules.MaxSplitHands)
        {
            return false;
        }

        if (hand.Cards[0].Rank != hand.Cards[1].Rank)
        {
            return false;
        }

        bool splittingAces = hand.Cards[0].Rank == Rank.Ace;
        if (splittingAces && hand.IsSplitAces && !rules.ResplitAces)
        {
            return false;
        }

        return true;
    }

    private void ExecuteAction(List<BlackjackHand> hands, int handIndex, PlayerAction action)
    {
        BlackjackHand hand = hands[handIndex];

        switch (action)
        {
            case PlayerAction.Hit:
                hand.Add(shoe.Draw());
                if (hand.Value.Total >= 21)
                {
                    hand.IsComplete = true;
                }
                break;
            case PlayerAction.Stand:
                hand.IsComplete = true;
                break;
            case PlayerAction.DoubleDown:
                hand.Bet *= 2m;
                hand.IsDoubled = true;
                hand.Add(shoe.Draw());
                hand.IsComplete = true;
                break;
            case PlayerAction.Split:
                SplitHand(hands, handIndex);
                break;
            case PlayerAction.Surrender:
                hand.IsSurrendered = true;
                hand.IsComplete = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private void SplitHand(List<BlackjackHand> hands, int handIndex)
    {
        BlackjackHand original = hands[handIndex];
        Card first = original.Cards[0];
        Card second = original.Cards[1];
        bool splitAces = first.Rank == Rank.Ace;

        var left = new BlackjackHand(original.Bet, isSplitHand: true, isSplitAces: splitAces);
        var right = new BlackjackHand(original.Bet, isSplitHand: true, isSplitAces: splitAces);

        left.Add(first);
        right.Add(second);
        left.Add(shoe.Draw());
        right.Add(shoe.Draw());

        hands[handIndex] = left;
        hands.Insert(handIndex + 1, right);

        if (splitAces && !rules.HitSplitAces)
        {
            left.IsComplete = !CanSplit(left, hands.Count);
            right.IsComplete = !CanSplit(right, hands.Count);
        }
    }

    private void AdvanceAfterPlayerAction()
    {
        if (Phase != RoundPhase.PlayerTurn)
        {
            return;
        }

        int next = playerHands.FindIndex(activeHandIndex, hand => !hand.IsComplete);
        if (next < 0)
        {
            next = playerHands.FindIndex(hand => !hand.IsComplete);
        }

        if (next >= 0)
        {
            activeHandIndex = next;
            return;
        }

        FinishTableAndSettle();
    }

    private void FinishTableAndSettle()
    {
        Phase = RoundPhase.DealerTurn;
        PlayAiSeats();
        EnsureDealerHoleCard();

        if (ShouldDealerPlay())
        {
            while (ShouldDealerHit())
            {
                DealerHand.Add(shoe.Draw());
            }
        }

        Settle();
    }

    private void EnsureDealerHoleCard()
    {
        if (DealerHand.Cards.Count == 1)
        {
            DealerHand.Add(shoe.Draw());
        }
    }

    private void PlayAiSeats()
    {
        foreach (AiSeatState seat in aiSeats)
        {
            List<BlackjackHand> hands = seat.MutableHands;
            int handIndex = 0;

            while (handIndex < hands.Count)
            {
                BlackjackHand hand = hands[handIndex];
                if (hand.IsNaturalBlackjack)
                {
                    hand.IsComplete = true;
                    handIndex++;
                    continue;
                }

                while (!hands[handIndex].IsComplete)
                {
                    IReadOnlySet<PlayerAction> available = GetAvailableActionsForHands(hands, handIndex);
                    PlayerAction action = BasicStrategy.Decide(hands[handIndex], DealerHand.Cards[0], available);
                    ExecuteAction(hands, handIndex, action);
                }

                handIndex++;
            }
        }
    }

    private bool ShouldDealerPlay()
    {
        if (DealerHand.IsNaturalBlackjack)
        {
            return false;
        }

        if (playerHands.Any(NeedsDealerComparison))
        {
            return true;
        }

        return aiSeats.SelectMany(seat => seat.Hands).Any(NeedsDealerComparison);
    }

    private static bool NeedsDealerComparison(BlackjackHand hand) =>
        !hand.IsSurrendered && !hand.Value.IsBust && !hand.IsNaturalBlackjack;

    private bool ShouldDealerHit()
    {
        HandValue value = DealerHand.Value;
        if (value.IsBust)
        {
            return false;
        }

        if (value.Total < 17)
        {
            return true;
        }

        return value.Total == 17 && value.IsSoft && rules.DealerHitsSoft17;
    }

    private void Settle()
    {
        List<HandResult> playerResults = EvaluateHands(playerHands);

        decimal insuranceNet = insuranceBet == 0m
            ? 0m
            : DealerHand.IsNaturalBlackjack ? insuranceBet * 2m : -insuranceBet;

        Result = new RoundResult(playerResults, insuranceNet);

        foreach (AiSeatState seat in aiSeats)
        {
            seat.Results = EvaluateHands(seat.Hands);
        }

        Phase = RoundPhase.Settled;
    }

    private List<HandResult> EvaluateHands(IReadOnlyList<BlackjackHand> hands)
    {
        var results = new List<HandResult>(hands.Count);
        HandValue dealerValue = DealerHand.Value;
        bool dealerBlackjack = DealerHand.IsNaturalBlackjack;

        for (int i = 0; i < hands.Count; i++)
        {
            BlackjackHand hand = hands[i];
            HandResult result;

            if (hand.IsSurrendered)
            {
                result = new HandResult(i, HandOutcome.Surrender, -(hand.Bet / 2m));
            }
            else if (hand.Value.IsBust)
            {
                result = new HandResult(i, HandOutcome.Loss, -hand.Bet);
            }
            else if (dealerBlackjack)
            {
                result = hand.IsNaturalBlackjack
                    ? new HandResult(i, HandOutcome.Push, 0m)
                    : new HandResult(i, HandOutcome.Loss, -hand.Bet);
            }
            else if (hand.IsNaturalBlackjack)
            {
                result = new HandResult(i, HandOutcome.Blackjack, hand.Bet * rules.BlackjackPayout);
            }
            else if (dealerValue.IsBust || hand.Value.Total > dealerValue.Total)
            {
                result = new HandResult(i, HandOutcome.Win, hand.Bet);
            }
            else if (hand.Value.Total < dealerValue.Total)
            {
                result = new HandResult(i, HandOutcome.Loss, -hand.Bet);
            }
            else
            {
                result = new HandResult(i, HandOutcome.Push, 0m);
            }

            results.Add(result);
        }

        return results;
    }

    private void EnsurePhase(RoundPhase expected)
    {
        if (Phase != expected)
        {
            throw new InvalidOperationException($"Expected round phase {expected}, but the current phase is {Phase}.");
        }
    }
}
