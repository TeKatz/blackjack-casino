using System.Security.Cryptography;

namespace BlackjackCasino.Core.Scratchcards;

/// <summary>
/// Immutable, pre-drawn play-money ticket. Older receipts without GameId
/// deserialize as Lucky Sevens, keeping the original on-disk schema readable.
/// </summary>
public sealed record ScratchcardTicket(
    Guid TicketId, int[] Symbols, decimal Prize, bool Collected, decimal BankrollAfter)
{
    public string GameId { get; init; } = ScratchcardRules.LuckySevensId;
}

/// <summary>A single source of truth for ticket prices and prize multipliers.</summary>
public sealed record ScratchcardGame(string Id, string Name, decimal Price, int PrizeMultiplier);

public static class ScratchcardRules
{
    public const string LuckySevensId = "lucky-sevens";
    public const string GoldRushId = "gold-rush";
    public const string PlatinumVaultId = "platinum-vault";
    public const string DiamondRoyaleId = "diamond-royale";
    public const decimal TicketPrice = 20m; // Backward-compatible original ticket price.
    public const int SymbolCount = 5;
    public const int FieldCount = 9;
    public const int DrawRange = 1_000;

    public static IReadOnlyList<ScratchcardGame> Games { get; } =
        Array.AsReadOnly(new[]
        {
            new ScratchcardGame(LuckySevensId, "Lucky Sevens", TicketPrice, 1),
            new ScratchcardGame(GoldRushId, "Gold Rush", 100m, 5),
            new ScratchcardGame(PlatinumVaultId, "Platinum Vault", 500m, 25),
            new ScratchcardGame(DiamondRoyaleId, "Diamond Royale", 2_500m, 125),
        });

    public static ScratchcardGame? FindGame(string? id) =>
        Games.FirstOrDefault(game => string.Equals(game.Id, id, StringComparison.Ordinal));

    public static ScratchcardGame GetGame(string? id) =>
        FindGame(id) ?? throw new ArgumentException("Unknown scratchcard game.", nameof(id));

    // Shared probability bands; each tier scales stake and payouts by the same
    // factor. All four series therefore retain an 86.25% theoretical return.
    public static decimal PrizeForRoll(int roll, string gameId = LuckySevensId)
    {
        if (roll is < 0 or >= DrawRange)
            throw new ArgumentOutOfRangeException(nameof(roll));
        int multiplier = GetGame(gameId).PrizeMultiplier;
        decimal basePrize = roll switch
        {
            < 500 => 0m,
            < 700 => 10m,
            < 850 => 20m,
            < 950 => 50m,
            < 990 => 100m,
            < 999 => 250m,
            _ => 1_000m,
        };
        return basePrize * multiplier;
    }

    public static bool IsPrize(decimal value, string gameId = LuckySevensId) =>
        FindGame(gameId) is { } game &&
        (value == 0m || value == 10m * game.PrizeMultiplier ||
         value == 20m * game.PrizeMultiplier ||
         value == 50m * game.PrizeMultiplier ||
         value == 100m * game.PrizeMultiplier ||
         value == 250m * game.PrizeMultiplier ||
         value == 1_000m * game.PrizeMultiplier);

    public static ScratchcardTicket Draw(Guid id, decimal bankrollAfter,
        Func<int, int>? random = null, string gameId = LuckySevensId)
    {
        if (id == Guid.Empty) throw new ArgumentException("Ticket ID is required.", nameof(id));
        if (bankrollAfter < 0m) throw new ArgumentOutOfRangeException(nameof(bankrollAfter));
        ScratchcardGame game = GetGame(gameId);
        random ??= RandomNumberGenerator.GetInt32;
        int Next(int ceiling)
        {
            int result = random(ceiling);
            if (result < 0 || result >= ceiling)
                throw new InvalidOperationException("Invalid scratchcard random source.");
            return result;
        }

        decimal prize = PrizeForRoll(Next(DrawRange), game.Id);
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
        return new ScratchcardTicket(id, symbols.ToArray(), prize, false, bankrollAfter)
        {
            GameId = game.Id,
        };
    }

    public static bool Valid(ScratchcardTicket ticket)
    {
        if (ticket.TicketId == Guid.Empty || ticket.BankrollAfter < 0m ||
            !IsPrize(ticket.Prize, ticket.GameId) ||
            ticket.Symbols is not { Length: FieldCount } ||
            ticket.Symbols.Any(s => s < 0 || s >= SymbolCount))
            return false;
        int matches = Enumerable.Range(0, SymbolCount)
            .Count(symbol => ticket.Symbols.Count(s => s == symbol) == 3);
        return ticket.Prize == 0m
            ? matches == 0 && ticket.Symbols.All(s => ticket.Symbols.Count(other => other == s) <= 2)
            : matches == 1 && ticket.Symbols.All(s => ticket.Symbols.Count(other => other == s) <= 3);
    }
}
