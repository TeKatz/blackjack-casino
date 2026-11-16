using System.Security.Cryptography;

namespace BlackjackCasino.Core.Scratchcards;

/// <summary>
/// A play-money scratch ticket. Outcomes are fixed before any visual scratching.
/// Three identical symbols anywhere among the nine fields win the fixed prize.
/// </summary>
public sealed record ScratchcardTicket(
    Guid TicketId, int[] Symbols, decimal Prize, bool Collected, decimal BankrollAfter);

public static class ScratchcardRules
{
    public const decimal TicketPrice = 20m;
    public const int SymbolCount = 5;
    public const int FieldCount = 9;
    public const int DrawRange = 1_000;

    // 50% blank, 20% $10, 15% $20, 10% $50, 4% $100,
    // 0.9% $250, 0.1% $1,000. Expected return: $17.25 / $20 (86.25%).
    public static decimal PrizeForRoll(int roll) => roll switch
    {
        < 0 or >= DrawRange => throw new ArgumentOutOfRangeException(nameof(roll)),
        < 500 => 0m,
        < 700 => 10m,
        < 850 => 20m,
        < 950 => 50m,
        < 990 => 100m,
        < 999 => 250m,
        _ => 1_000m,
    };

    public static bool IsPrize(decimal value) =>
        value is 0m or 10m or 20m or 50m or 100m or 250m or 1_000m;

    public static ScratchcardTicket Draw(Guid id, decimal bankrollAfter,
        Func<int, int>? random = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Ticket ID is required.", nameof(id));
        if (bankrollAfter < 0m) throw new ArgumentOutOfRangeException(nameof(bankrollAfter));
        random ??= RandomNumberGenerator.GetInt32;
        int Next(int ceiling)
        {
            int result = random(ceiling);
            if (result < 0 || result >= ceiling)
                throw new InvalidOperationException("Invalid scratchcard random source.");
            return result;
        }

        decimal prize = PrizeForRoll(Next(DrawRange));
        var symbols = new List<int>(FieldCount);
        if (prize == 0m)
        {
            // Never accidentally produce three matching symbols on a losing ticket.
            for (int symbol = 0; symbol < 4; symbol++)
            {
                symbols.Add(symbol);
                symbols.Add(symbol);
            }
            symbols.Add(4);
        }
        else
        {
            int winner = Next(SymbolCount);
            symbols.AddRange([winner, winner, winner]);
            var others = Enumerable.Range(0, SymbolCount).Where(s => s != winner).ToList();
            for (int i = others.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (others[i], others[j]) = (others[j], others[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                symbols.Add(others[i]);
                symbols.Add(others[i]);
            }
        }
        for (int i = symbols.Count - 1; i > 0; i--)
        {
            int j = Next(i + 1);
            (symbols[i], symbols[j]) = (symbols[j], symbols[i]);
        }
        return new ScratchcardTicket(id, symbols.ToArray(), prize, false, bankrollAfter);
    }

    public static bool Valid(ScratchcardTicket ticket)
    {
        if (ticket.TicketId == Guid.Empty || ticket.BankrollAfter < 0m ||
            !IsPrize(ticket.Prize) || ticket.Symbols is not { Length: FieldCount } ||
            ticket.Symbols.Any(s => s < 0 || s >= SymbolCount))
            return false;
        int matches = Enumerable.Range(0, SymbolCount)
            .Count(symbol => ticket.Symbols.Count(s => s == symbol) == 3);
        return ticket.Prize == 0m
            ? matches == 0 && ticket.Symbols.All(s => ticket.Symbols.Count(other => other == s) <= 2)
            : matches == 1 && ticket.Symbols.All(s => ticket.Symbols.Count(other => other == s) <= 3);
    }
}
