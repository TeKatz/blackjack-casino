namespace BlackjackCasino.Core;

/// <summary>
/// Owns the long-lived casino state around individual blackjack rounds: bankroll,
/// wager reservation and the shared shoe. UI layers should use this class instead
/// of changing a player's balance directly.
/// </summary>
public sealed class CasinoSession
{
    private readonly GameRules rules;
    private readonly Func<ICardShoe> shoeFactory;
    private readonly IReadOnlyList<AiPlayerProfile> aiPlayers;
    private ICardShoe shoe;
    private bool settlementApplied;

    public CasinoSession(
        GameRules rules,
        decimal startingBankroll = 1_000m,
        Func<ICardShoe>? shoeFactory = null,
        IReadOnlyList<AiPlayerProfile>? aiPlayers = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        rules.Validate();

        if (startingBankroll < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startingBankroll));
        }

        if (aiPlayers is { Count: > 5 })
        {
            throw new ArgumentOutOfRangeException(nameof(aiPlayers), "A table supports at most five AI seats.");
        }

        this.rules = rules;
        this.shoeFactory = shoeFactory ?? (() => new Shoe(rules));
        this.aiPlayers = aiPlayers?.ToArray() ?? [];
        shoe = this.shoeFactory();
        Bankroll = startingBankroll;
    }

    public decimal Bankroll { get; private set; }

    public BlackjackRound? CurrentRound { get; private set; }

    public int ShoeNumber { get; private set; } = 1;

    public int CardsRemaining => shoe.Remaining;

    public int AiPlayerCount => aiPlayers.Count;

    public bool RoundInProgress => CurrentRound is { Phase: not RoundPhase.Settled };

    public IReadOnlySet<PlayerAction> GetAvailableActions()
    {
        if (CurrentRound is not { Phase: RoundPhase.PlayerTurn } round)
        {
            return new HashSet<PlayerAction>();
        }

        var actions = new HashSet<PlayerAction>(round.GetAvailableActions());
        BlackjackHand hand = round.ActiveHand!;

        if (Bankroll < hand.Bet)
        {
            actions.Remove(PlayerAction.DoubleDown);
            actions.Remove(PlayerAction.Split);
        }

        return actions;
    }

    public void StartRound(decimal bet)
    {
        if (RoundInProgress)
        {
            throw new InvalidOperationException("The current round must finish before a new round can start.");
        }

        if (bet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Bet must be positive.");
        }

        EnsureFunds(bet);
        EnsurePlayableShoe();

        Bankroll -= bet;
        settlementApplied = false;
        CurrentRound = new BlackjackRound(rules, shoe, bet, aiPlayers);

        try
        {
            CurrentRound.Start();
            ApplySettlementIfNeeded();
        }
        catch
        {
            CurrentRound = null;
            Bankroll += bet;
            throw;
        }
    }

    public void Act(PlayerAction action)
    {
        BlackjackRound round = RequireRound(RoundPhase.PlayerTurn);
        IReadOnlySet<PlayerAction> available = GetAvailableActions();
        if (!available.Contains(action))
        {
            throw new InvalidOperationException($"Action {action} is not currently available.");
        }

        decimal additionalWager = action switch
        {
            PlayerAction.DoubleDown => round.ActiveHand!.Bet,
            PlayerAction.Split => round.ActiveHand!.Bet,
            _ => 0m,
        };

        if (additionalWager > 0)
        {
            EnsureFunds(additionalWager);
            Bankroll -= additionalWager;
        }

        try
        {
            round.Act(action);
            ApplySettlementIfNeeded();
        }
        catch
        {
            Bankroll += additionalWager;
            throw;
        }
    }

    public void TakeInsurance(decimal? amount = null)
    {
        BlackjackRound round = RequireRound(RoundPhase.InsuranceDecision);
        decimal wager = amount ?? round.InitialBet / 2m;
        EnsureFunds(wager);
        Bankroll -= wager;

        try
        {
            round.TakeInsurance(wager);
            ApplySettlementIfNeeded();
        }
        catch
        {
            Bankroll += wager;
            throw;
        }
    }

    public void DeclineInsurance()
    {
        BlackjackRound round = RequireRound(RoundPhase.InsuranceDecision);
        round.DeclineInsurance();
        ApplySettlementIfNeeded();
    }

    public void ResetBankroll(decimal amount = 1_000m)
    {
        if (RoundInProgress)
        {
            throw new InvalidOperationException("Bankroll cannot be reset while a round is in progress.");
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Bankroll = amount;
        CurrentRound = null;
        settlementApplied = false;
    }

    private BlackjackRound RequireRound(RoundPhase phase)
    {
        if (CurrentRound is null)
        {
            throw new InvalidOperationException("No blackjack round exists.");
        }

        if (CurrentRound.Phase != phase)
        {
            throw new InvalidOperationException($"Expected round phase {phase}, but the current phase is {CurrentRound.Phase}.");
        }

        return CurrentRound;
    }

    private void ApplySettlementIfNeeded()
    {
        if (settlementApplied || CurrentRound is not { Phase: RoundPhase.Settled, Result: not null } round)
        {
            return;
        }

        decimal returnedStakesAndWinnings = 0m;
        foreach (HandResult result in round.Result.Hands)
        {
            BlackjackHand hand = round.PlayerHands[result.HandIndex];
            returnedStakesAndWinnings += hand.Bet + result.Net;
        }

        if (round.InsuranceBet > 0)
        {
            returnedStakesAndWinnings += round.InsuranceBet + round.Result.InsuranceNet;
        }

        Bankroll += returnedStakesAndWinnings;
        settlementApplied = true;
    }

    private void EnsurePlayableShoe()
    {
        bool reachedCutCard = shoe is Shoe concrete && concrete.ReachedCutCard;
        if (reachedCutCard || shoe.Remaining < 20)
        {
            shoe = shoeFactory();
            ShoeNumber++;
        }
    }

    private void EnsureFunds(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (Bankroll < amount)
        {
            throw new InvalidOperationException($"Insufficient bankroll. Required {amount:0.##}, available {Bankroll:0.##}.");
        }
    }
}
