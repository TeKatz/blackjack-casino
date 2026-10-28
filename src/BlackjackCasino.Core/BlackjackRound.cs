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
    private int activeHandIndex;
    private decimal insuranceBet;

    public BlackjackRound(GameRules rules, ICardShoe shoe, decimal initialBet)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(shoe);
        rules.Validate();

        if (initialBet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBet), "Initial bet must be positive.");
        }

        this.rules = rules;
        this.shoe = shoe;
        InitialBet = initialBet;
        DealerHand = new BlackjackHand(initialBet);
        playerHands.Add(new BlackjackHand(initialBet));
    }

    public decimal InitialBet { get; }

    public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;

    public IReadOnlyList<BlackjackHand> PlayerHands => playerHands;

    public BlackjackHand DealerHand { get; }

    public int ActiveHandIndex => activeHandIndex;

    public BlackjackHand? ActiveHand => Phase == RoundPhase.PlayerTurn ? playerHands[activeHandIndex] : null;

    public decimal InsuranceBet => insuranceBet;

    public RoundResult? Result { get; private set; }

    public void Start()
    {
        EnsurePhase(RoundPhase.NotStarted);

        BlackjackHand player = playerHands[0];
        player.Add(shoe.Draw());
        DealerHand.Add(shoe.Draw());
        player.Add(shoe.Draw());
        DealerHand.Add(shoe.Draw());

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

        BlackjackHand hand = playerHands[activeHandIndex];
        var actions = new HashSet<PlayerAction> { PlayerAction.Stand };

        if (!hand.IsSplitAces || rules.HitSplitAces)
        {
            actions.Add(PlayerAction.Hit);
        }

        if (hand.Cards.Count == 2 && (!hand.IsSplitHand || rules.DoubleAfterSplit) && (!hand.IsSplitAces || rules.HitSplitAces))
        {
            actions.Add(PlayerAction.DoubleDown);
        }

        if (CanSplit(hand))
        {
            actions.Add(PlayerAction.Split);
        }

        if (rules.AllowLateSurrender && !hand.IsSplitHand && hand.Cards.Count == 2)
        {
            actions.Add(PlayerAction.Surrender);
        }

        return actions;
    }

    public void Act(PlayerAction action)
    {
        EnsurePhase(RoundPhase.PlayerTurn);

        IReadOnlySet<PlayerAction> allowed = GetAvailableActions();
        if (!allowed.Contains(action))
        {
            throw new InvalidOperationException($"Action {action} is not available for the active hand.");
        }

        BlackjackHand hand = playerHands[activeHandIndex];

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
                SplitActiveHand();
                break;
            case PlayerAction.Surrender:
                hand.IsSurrendered = true;
                hand.IsComplete = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        AdvanceAfterPlayerAction();
    }

    private void ResolveOpeningState()
    {
        bool dealerCanPeek = DealerHand.Cards[0].Rank == Rank.Ace || DealerHand.Cards[0].IsTenValue;
        if (rules.DealerPeeksForBlackjack && dealerCanPeek && DealerHand.IsNaturalBlackjack)
        {
            Settle();
            return;
        }

        if (playerHands[0].IsNaturalBlackjack)
        {
            FinishDealerAndSettle(playDealer: false);
            return;
        }

        Phase = RoundPhase.PlayerTurn;
        activeHandIndex = 0;
    }

    private bool CanSplit(BlackjackHand hand)
    {
        if (hand.Cards.Count != 2 || playerHands.Count >= rules.MaxSplitHands)
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

    private void SplitActiveHand()
    {
        BlackjackHand original = playerHands[activeHandIndex];
        Card first = original.Cards[0];
        Card second = original.Cards[1];
        bool splitAces = first.Rank == Rank.Ace;

        var left = new BlackjackHand(original.Bet, isSplitHand: true, isSplitAces: splitAces);
        var right = new BlackjackHand(original.Bet, isSplitHand: true, isSplitAces: splitAces);

        left.Add(first);
        right.Add(second);
        left.Add(shoe.Draw());
        right.Add(shoe.Draw());

        playerHands[activeHandIndex] = left;
        playerHands.Insert(activeHandIndex + 1, right);

        if (splitAces && !rules.HitSplitAces)
        {
            left.IsComplete = !CanSplit(left);
            right.IsComplete = !CanSplit(right);
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

        FinishDealerAndSettle(playDealer: ShouldDealerPlay());
    }

    private bool ShouldDealerPlay()
    {
        if (DealerHand.IsNaturalBlackjack)
        {
            return false;
        }

        return playerHands.Any(hand => !hand.IsSurrendered && !hand.Value.IsBust && !hand.IsNaturalBlackjack);
    }

    private void FinishDealerAndSettle(bool playDealer)
    {
        Phase = RoundPhase.DealerTurn;

        if (playDealer)
        {
            while (ShouldDealerHit())
            {
                DealerHand.Add(shoe.Draw());
            }
        }

        Settle();
    }

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
        var results = new List<HandResult>(playerHands.Count);
        HandValue dealerValue = DealerHand.Value;
        bool dealerBlackjack = DealerHand.IsNaturalBlackjack;

        for (int i = 0; i < playerHands.Count; i++)
        {
            BlackjackHand hand = playerHands[i];
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

        decimal insuranceNet = insuranceBet == 0m
            ? 0m
            : dealerBlackjack ? insuranceBet * 2m : -insuranceBet;

        Result = new RoundResult(results, insuranceNet);
        Phase = RoundPhase.Settled;
    }

    private void EnsurePhase(RoundPhase expected)
    {
        if (Phase != expected)
        {
            throw new InvalidOperationException($"Expected round phase {expected}, but the current phase is {Phase}.");
        }
    }
}
