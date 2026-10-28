namespace BlackjackCasino.Core;

public sealed record PlayerActionRecord(int HandIndex, PlayerAction Action);

public sealed record HistoricalHand(
    decimal Bet,
    bool IsSplitHand,
    bool IsDoubled,
    bool IsSurrendered,
    IReadOnlyList<Card> Cards,
    HandOutcome Outcome,
    decimal Net);

public sealed record HistoricalAiSeat(
    string Name,
    IReadOnlyList<HistoricalHand> Hands);

public sealed record RoundHistoryEntry(
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    decimal BankrollBefore,
    decimal BankrollAfter,
    decimal InitialBet,
    decimal TotalNet,
    GameRules Rules,
    IReadOnlyList<Card> DealerCards,
    IReadOnlyList<HistoricalHand> PlayerHands,
    IReadOnlyList<PlayerActionRecord> Actions,
    IReadOnlyList<HistoricalAiSeat> AiSeats);
