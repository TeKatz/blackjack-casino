namespace BlackjackCasino.Core.Solitaire;

public static class SolitaireGame
{
    private static readonly Dictionary<int, int[]> TriPeaksCovers = new()
    {
        [0] = [3, 4], [1] = [5, 6], [2] = [7, 8],
        [3] = [9, 10], [4] = [10, 11], [5] = [12, 13], [6] = [13, 14], [7] = [15, 16], [8] = [16, 17],
        [9] = [18, 19], [10] = [19, 20], [11] = [20, 21], [12] = [21, 22], [13] = [22, 23], [14] = [23, 24], [15] = [24, 25], [16] = [25, 26], [17] = [26, 27],
    };

    public static SolitaireGameState Create(SolitaireVariant variant, int dealNumber, int spiderSuits = 1)
    {
        dealNumber = Math.Max(1, dealNumber);
        spiderSuits = spiderSuits is 1 or 2 or 4 ? spiderSuits : 1;
        var state = new SolitaireGameState { Variant = variant, DealNumber = dealNumber, SpiderSuits = spiderSuits };
        switch (variant)
        {
            case SolitaireVariant.Klondike: DealKlondike(state); break;
            case SolitaireVariant.FreeCell: DealFreeCell(state); break;
            case SolitaireVariant.Spider: DealSpider(state); break;
            case SolitaireVariant.Pyramid: DealPyramid(state); break;
            case SolitaireVariant.Golf: DealGolf(state); break;
            case SolitaireVariant.TriPeaks: DealTriPeaks(state); break;
            default: throw new ArgumentOutOfRangeException(nameof(variant));
        }
        return state;
    }

    public static IReadOnlyList<SolitaireCardState> Pile(SolitaireGameState state, string id) => GetPile(state, id);

    public static bool TryDraw(SolitaireGameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Won) return false;
        return state.Variant switch
        {
            SolitaireVariant.Klondike => DrawKlondike(state),
            SolitaireVariant.Spider => DealSpiderRow(state),
            SolitaireVariant.Pyramid or SolitaireVariant.Golf or SolitaireVariant.TriPeaks => DrawSingle(state),
            _ => false,
        };
    }

    public static bool TryActivateCard(SolitaireGameState state, string pileId, int index)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Won || !state.Piles.TryGetValue(pileId, out List<SolitaireCardState>? pile) || index < 0 || index >= pile.Count) return false;
        SolitaireCardState card = pile[index];

        if (state.Variant is SolitaireVariant.Klondike or SolitaireVariant.Spider)
        {
            if (index == pile.Count - 1 && !card.FaceUp && (pileId.StartsWith("T", StringComparison.Ordinal)))
            {
                card.FaceUp = true;
                FinishAction(state);
                return true;
            }
            return false;
        }

        if (state.Variant == SolitaireVariant.Pyramid)
        {
            if (!IsPyramidSelectable(state, pileId, index) || card.Card.Rank != Rank.King) return false;
            RemoveSelectableCard(state, pileId);
            FinishAction(state);
            return true;
        }

        if (state.Variant is SolitaireVariant.Golf or SolitaireVariant.TriPeaks)
        {
            if (!IsSelectable(state, pileId, index)) return false;
            List<SolitaireCardState> waste = GetPile(state, "Waste");
            if (waste.Count == 0 || !Adjacent(card.Card.Rank, waste[^1].Card.Rank)) return false;
            SolitaireCardState moved = pile[index];
            pile.RemoveAt(index);
            moved.FaceUp = true;
            waste.Add(moved);
            FinishAction(state);
            return true;
        }

        return false;
    }

    public static bool TryMove(SolitaireGameState state, string sourceId, int sourceIndex, string targetId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Won || !state.Piles.TryGetValue(sourceId, out List<SolitaireCardState>? source) ||
            !state.Piles.TryGetValue(targetId, out List<SolitaireCardState>? target) || sourceIndex < 0 || sourceIndex >= source.Count)
            return false;

        bool moved = state.Variant switch
        {
            SolitaireVariant.Klondike => MoveKlondike(state, sourceId, sourceIndex, targetId),
            SolitaireVariant.FreeCell => MoveFreeCell(state, sourceId, sourceIndex, targetId),
            SolitaireVariant.Spider => MoveSpider(state, sourceId, sourceIndex, targetId),
            SolitaireVariant.Pyramid => MovePyramid(state, sourceId, sourceIndex, targetId),
            _ => false,
        };
        if (moved) FinishAction(state);
        return moved;
    }

    public static bool TryAutoFoundation(SolitaireGameState state, string sourceId, int sourceIndex)
    {
        if (state.Variant is not (SolitaireVariant.Klondike or SolitaireVariant.FreeCell)) return false;
        if (!state.Piles.TryGetValue(sourceId, out List<SolitaireCardState>? source) || sourceIndex < 0 || sourceIndex >= source.Count) return false;
        SolitaireCardState card = source[sourceIndex];
        return card.FaceUp && TryMove(state, sourceId, sourceIndex, FoundationId(card.Card.Suit));
    }

    public static bool IsSelectable(SolitaireGameState state, string pileId, int index)
    {
        if (!state.Piles.TryGetValue(pileId, out List<SolitaireCardState>? pile) || index < 0 || index >= pile.Count) return false;
        SolitaireCardState card = pile[index];
        if (!card.FaceUp) return false;
        return state.Variant switch
        {
            SolitaireVariant.Klondike => pileId.StartsWith("T", StringComparison.Ordinal) ? ValidAlternatingRun(pile, index) : index == pile.Count - 1,
            SolitaireVariant.FreeCell => pileId.StartsWith("C", StringComparison.Ordinal) ? ValidAlternatingRun(pile, index) : index == pile.Count - 1,
            SolitaireVariant.Spider => pileId.StartsWith("T", StringComparison.Ordinal) && ValidSameSuitRun(pile, index),
            SolitaireVariant.Pyramid => IsPyramidSelectable(state, pileId, index),
            SolitaireVariant.Golf => pileId.StartsWith("G", StringComparison.Ordinal) && index == pile.Count - 1,
            SolitaireVariant.TriPeaks => IsTriPeaksExposed(state, pileId) && index == pile.Count - 1,
            _ => false,
        };
    }

    public static bool IsWon(SolitaireGameState state) => state.Variant switch
    {
        SolitaireVariant.Klondike or SolitaireVariant.FreeCell => FoundationIds.Sum(id => GetPile(state, id).Count) == 52,
        SolitaireVariant.Spider => state.CompletedRuns >= 8,
        SolitaireVariant.Pyramid => Enumerable.Range(0, 28).All(i => GetPile(state, $"P{i}").Count == 0),
        SolitaireVariant.Golf => Enumerable.Range(0, 7).All(i => GetPile(state, $"G{i}").Count == 0),
        SolitaireVariant.TriPeaks => Enumerable.Range(0, 28).All(i => GetPile(state, $"P{i}").Count == 0),
        _ => false,
    };

    private static readonly string[] FoundationIds = ["F-Clubs", "F-Diamonds", "F-Hearts", "F-Spades"];

    private static void DealKlondike(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(StandardDeck(), state.DealNumber);
        int cursor = 0;
        for (int c = 0; c < 7; c++)
        {
            List<SolitaireCardState> pile = state.Piles[$"T{c}"] = [];
            for (int i = 0; i <= c; i++) pile.Add(new(deck[cursor++], i == c));
        }
        state.Piles["Stock"] = deck.Skip(cursor).Select(c => new SolitaireCardState(c, false)).ToList();
        state.Piles["Waste"] = [];
        foreach (string id in FoundationIds) state.Piles[id] = [];
    }

    private static void DealFreeCell(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(StandardDeck(), state.DealNumber);
        for (int i = 0; i < 8; i++) state.Piles[$"C{i}"] = [];
        for (int i = 0; i < deck.Count; i++) state.Piles[$"C{i % 8}"].Add(new(deck[i], true));
        for (int i = 0; i < 4; i++) state.Piles[$"Cell{i}"] = [];
        foreach (string id in FoundationIds) state.Piles[id] = [];
    }

    private static void DealSpider(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(SpiderDeck(state.SpiderSuits), state.DealNumber);
        for (int i = 0; i < 10; i++) state.Piles[$"T{i}"] = [];
        for (int i = 0; i < 54; i++) state.Piles[$"T{i % 10}"].Add(new(deck[i], false));
        for (int i = 0; i < 10; i++) state.Piles[$"T{i}"][^1].FaceUp = true;
        state.Piles["Stock"] = deck.Skip(54).Select(c => new SolitaireCardState(c, false)).ToList();
    }

    private static void DealPyramid(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(StandardDeck(), state.DealNumber);
        for (int i = 0; i < 28; i++) state.Piles[$"P{i}"] = [new(deck[i], true)];
        state.Piles["Stock"] = deck.Skip(28).Select(c => new SolitaireCardState(c, false)).ToList();
        state.Piles["Waste"] = [];
    }

    private static void DealGolf(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(StandardDeck(), state.DealNumber);
        int cursor = 0;
        for (int c = 0; c < 7; c++)
        {
            List<SolitaireCardState> pile = state.Piles[$"G{c}"] = [];
            for (int i = 0; i < 5; i++) pile.Add(new(deck[cursor++], true));
        }
        state.Piles["Stock"] = deck.Skip(cursor).Select(c => new SolitaireCardState(c, false)).ToList();
        state.Piles["Waste"] = [];
        SolitaireCardState first = state.Piles["Stock"][^1];
        state.Piles["Stock"].RemoveAt(state.Piles["Stock"].Count - 1);
        first.FaceUp = true;
        state.Piles["Waste"].Add(first);
    }

    private static void DealTriPeaks(SolitaireGameState state)
    {
        List<Card> deck = Shuffle(StandardDeck(), state.DealNumber);
        for (int i = 0; i < 28; i++) state.Piles[$"P{i}"] = [new(deck[i], true)];
        state.Piles["Stock"] = deck.Skip(28).Select(c => new SolitaireCardState(c, false)).ToList();
        state.Piles["Waste"] = [];
        SolitaireCardState first = state.Piles["Stock"][^1];
        state.Piles["Stock"].RemoveAt(state.Piles["Stock"].Count - 1);
        first.FaceUp = true;
        state.Piles["Waste"].Add(first);
    }

    private static bool DrawKlondike(SolitaireGameState state)
    {
        List<SolitaireCardState> stock = GetPile(state, "Stock");
        List<SolitaireCardState> waste = GetPile(state, "Waste");
        if (stock.Count > 0)
        {
            SolitaireCardState card = stock[^1];
            stock.RemoveAt(stock.Count - 1);
            card.FaceUp = true;
            waste.Add(card);
            FinishAction(state);
            return true;
        }
        if (waste.Count == 0) return false;
        for (int i = waste.Count - 1; i >= 0; i--)
        {
            SolitaireCardState card = waste[i];
            card.FaceUp = false;
            stock.Add(card);
        }
        waste.Clear();
        FinishAction(state);
        return true;
    }

    private static bool DrawSingle(SolitaireGameState state)
    {
        List<SolitaireCardState> stock = GetPile(state, "Stock");
        if (stock.Count == 0) return false;
        SolitaireCardState card = stock[^1];
        stock.RemoveAt(stock.Count - 1);
        card.FaceUp = true;
        GetPile(state, "Waste").Add(card);
        FinishAction(state);
        return true;
    }

    private static bool DealSpiderRow(SolitaireGameState state)
    {
        List<SolitaireCardState> stock = GetPile(state, "Stock");
        if (stock.Count < 10 || Enumerable.Range(0, 10).Any(i => GetPile(state, $"T{i}").Count == 0)) return false;
        for (int i = 0; i < 10; i++)
        {
            SolitaireCardState card = stock[^1];
            stock.RemoveAt(stock.Count - 1);
            card.FaceUp = true;
            GetPile(state, $"T{i}").Add(card);
        }
        for (int i = 0; i < 10; i++) RemoveSpiderRuns(state, GetPile(state, $"T{i}"));
        FinishAction(state);
        return true;
    }

    private static bool MoveKlondike(SolitaireGameState state, string sourceId, int sourceIndex, string targetId)
    {
        List<SolitaireCardState> source = GetPile(state, sourceId);
        List<SolitaireCardState> target = GetPile(state, targetId);
        SolitaireCardState first = source[sourceIndex];
        if (!first.FaceUp) return false;
        int count = source.Count - sourceIndex;

        if (targetId.StartsWith("F-", StringComparison.Ordinal))
        {
            if (count != 1 || sourceIndex != source.Count - 1 || !CanFoundation(first.Card, targetId, target)) return false;
        }
        else if (targetId.StartsWith("T", StringComparison.Ordinal))
        {
            if (!ValidAlternatingRun(source, sourceIndex) || !CanTableau(first.Card, target)) return false;
        }
        else return false;

        MoveRange(source, sourceIndex, target);
        FlipExposed(sourceId, source);
        return true;
    }

    private static bool MoveFreeCell(SolitaireGameState state, string sourceId, int sourceIndex, string targetId)
    {
        List<SolitaireCardState> source = GetPile(state, sourceId);
        List<SolitaireCardState> target = GetPile(state, targetId);
        SolitaireCardState first = source[sourceIndex];
        if (!first.FaceUp) return false;
        int count = source.Count - sourceIndex;

        if (targetId.StartsWith("F-", StringComparison.Ordinal))
        {
            if (count != 1 || sourceIndex != source.Count - 1 || !CanFoundation(first.Card, targetId, target)) return false;
        }
        else if (targetId.StartsWith("Cell", StringComparison.Ordinal))
        {
            if (count != 1 || sourceIndex != source.Count - 1 || target.Count != 0) return false;
        }
        else if (targetId.StartsWith("C", StringComparison.Ordinal))
        {
            if (!sourceId.StartsWith("C", StringComparison.Ordinal) && count != 1) return false;
            if (sourceId.StartsWith("C", StringComparison.Ordinal) && !ValidAlternatingRun(source, sourceIndex)) return false;
            if (!CanFreeCellTableau(first.Card, target)) return false;
            if (count > FreeCellMoveCapacity(state, targetId, target.Count == 0)) return false;
        }
        else return false;

        MoveRange(source, sourceIndex, target);
        return true;
    }

    private static bool MoveSpider(SolitaireGameState state, string sourceId, int sourceIndex, string targetId)
    {
        if (!sourceId.StartsWith("T", StringComparison.Ordinal) || !targetId.StartsWith("T", StringComparison.Ordinal) || sourceId == targetId) return false;
        List<SolitaireCardState> source = GetPile(state, sourceId);
        List<SolitaireCardState> target = GetPile(state, targetId);
        if (!ValidSameSuitRun(source, sourceIndex)) return false;
        Card first = source[sourceIndex].Card;
        if (target.Count > 0 && (int)target[^1].Card.Rank != (int)first.Rank + 1) return false;
        MoveRange(source, sourceIndex, target);
        FlipExposed(sourceId, source);
        RemoveSpiderRuns(state, target);
        return true;
    }

    private static bool MovePyramid(SolitaireGameState state, string sourceId, int sourceIndex, string targetId)
    {
        if (sourceId == targetId || !IsPyramidSelectable(state, sourceId, sourceIndex)) return false;
        List<SolitaireCardState> target = GetPile(state, targetId);
        if (target.Count == 0 || !IsPyramidSelectable(state, targetId, target.Count - 1)) return false;
        Card a = GetPile(state, sourceId)[sourceIndex].Card;
        Card b = target[^1].Card;
        if ((int)a.Rank + (int)b.Rank != 13) return false;
        RemoveSelectableCard(state, sourceId);
        RemoveSelectableCard(state, targetId);
        return true;
    }

    private static bool IsPyramidSelectable(SolitaireGameState state, string pileId, int index)
    {
        if (pileId == "Waste") return GetPile(state, "Waste").Count > 0 && index == GetPile(state, "Waste").Count - 1;
        if (!pileId.StartsWith("P", StringComparison.Ordinal) || !int.TryParse(pileId.AsSpan(1), out int position) || position < 0 || position >= 28) return false;
        List<SolitaireCardState> pile = GetPile(state, pileId);
        if (pile.Count == 0 || index != pile.Count - 1) return false;
        (int row, int col) = PyramidRowCol(position);
        if (row == 6) return true;
        int nextStart = Triangle(row + 1);
        return GetPile(state, $"P{nextStart + col}").Count == 0 && GetPile(state, $"P{nextStart + col + 1}").Count == 0;
    }

    private static bool IsTriPeaksExposed(SolitaireGameState state, string pileId)
    {
        if (!pileId.StartsWith("P", StringComparison.Ordinal) || !int.TryParse(pileId.AsSpan(1), out int position) || position < 0 || position >= 28) return false;
        if (GetPile(state, pileId).Count == 0) return false;
        return !TriPeaksCovers.TryGetValue(position, out int[]? covers) || covers.All(i => GetPile(state, $"P{i}").Count == 0);
    }

    private static void RemoveSelectableCard(SolitaireGameState state, string pileId)
    {
        List<SolitaireCardState> pile = GetPile(state, pileId);
        if (pile.Count > 0) pile.RemoveAt(pile.Count - 1);
    }

    private static int FreeCellMoveCapacity(SolitaireGameState state, string targetId, bool targetEmpty)
    {
        int emptyCells = Enumerable.Range(0, 4).Count(i => GetPile(state, $"Cell{i}").Count == 0);
        int emptyColumns = Enumerable.Range(0, 8).Count(i => GetPile(state, $"C{i}").Count == 0);
        if (targetEmpty && targetId.StartsWith("C", StringComparison.Ordinal)) emptyColumns = Math.Max(0, emptyColumns - 1);
        return (emptyCells + 1) * (1 << Math.Min(emptyColumns, 20));
    }

    private static bool CanFoundation(Card card, string targetId, List<SolitaireCardState> target)
    {
        if (!string.Equals(targetId, FoundationId(card.Suit), StringComparison.Ordinal)) return false;
        if (target.Count == 0) return card.Rank == Rank.Ace;
        Card top = target[^1].Card;
        return top.Suit == card.Suit && (int)card.Rank == (int)top.Rank + 1;
    }

    private static bool CanTableau(Card card, List<SolitaireCardState> target)
    {
        if (target.Count == 0) return card.Rank == Rank.King;
        Card top = target[^1].Card;
        return OppositeColor(card, top) && (int)card.Rank + 1 == (int)top.Rank;
    }

    private static bool CanFreeCellTableau(Card card, List<SolitaireCardState> target)
    {
        if (target.Count == 0) return true;
        Card top = target[^1].Card;
        return OppositeColor(card, top) && (int)card.Rank + 1 == (int)top.Rank;
    }

    private static bool ValidAlternatingRun(List<SolitaireCardState> pile, int start)
    {
        for (int i = start; i < pile.Count; i++) if (!pile[i].FaceUp) return false;
        for (int i = start; i < pile.Count - 1; i++)
        {
            if (!OppositeColor(pile[i].Card, pile[i + 1].Card) || (int)pile[i].Card.Rank != (int)pile[i + 1].Card.Rank + 1) return false;
        }
        return true;
    }

    private static bool ValidSameSuitRun(List<SolitaireCardState> pile, int start)
    {
        if (start < 0 || start >= pile.Count) return false;
        for (int i = start; i < pile.Count; i++) if (!pile[i].FaceUp) return false;
        for (int i = start; i < pile.Count - 1; i++)
        {
            if (pile[i].Card.Suit != pile[i + 1].Card.Suit || (int)pile[i].Card.Rank != (int)pile[i + 1].Card.Rank + 1) return false;
        }
        return true;
    }

    private static void RemoveSpiderRuns(SolitaireGameState state, List<SolitaireCardState> pile)
    {
        while (pile.Count >= 13)
        {
            int start = pile.Count - 13;
            if (!ValidSameSuitRun(pile, start) || pile[start].Card.Rank != Rank.King || pile[^1].Card.Rank != Rank.Ace) return;
            pile.RemoveRange(start, 13);
            state.CompletedRuns++;
            if (pile.Count > 0 && !pile[^1].FaceUp) pile[^1].FaceUp = true;
        }
    }

    private static void FlipExposed(string sourceId, List<SolitaireCardState> source)
    {
        if (sourceId.StartsWith("T", StringComparison.Ordinal) && source.Count > 0 && !source[^1].FaceUp) source[^1].FaceUp = true;
    }

    private static void MoveRange(List<SolitaireCardState> source, int start, List<SolitaireCardState> target)
    {
        List<SolitaireCardState> moving = source.GetRange(start, source.Count - start);
        source.RemoveRange(start, source.Count - start);
        target.AddRange(moving);
    }

    private static void FinishAction(SolitaireGameState state)
    {
        state.Moves++;
        state.Won = IsWon(state);
    }

    private static bool Adjacent(Rank a, Rank b) => Math.Abs((int)a - (int)b) == 1;
    private static bool OppositeColor(Card a, Card b) => IsRed(a.Suit) != IsRed(b.Suit);
    private static bool IsRed(Suit suit) => suit is Suit.Hearts or Suit.Diamonds;
    private static string FoundationId(Suit suit) => $"F-{suit}";

    private static List<SolitaireCardState> GetPile(SolitaireGameState state, string id)
    {
        if (!state.Piles.TryGetValue(id, out List<SolitaireCardState>? pile)) state.Piles[id] = pile = [];
        return pile;
    }

    private static (int Row, int Col) PyramidRowCol(int position)
    {
        int row = 0;
        while (row < 6 && Triangle(row + 1) <= position) row++;
        return (row, position - Triangle(row));
    }

    private static int Triangle(int row) => row * (row + 1) / 2;

    private static List<Card> StandardDeck() =>
        Enum.GetValues<Suit>().SelectMany(s => Enum.GetValues<Rank>().Select(r => new Card(r, s))).ToList();

    private static List<Card> SpiderDeck(int suits)
    {
        Suit[] available = suits switch
        {
            1 => [Suit.Spades],
            2 => [Suit.Spades, Suit.Hearts],
            _ => [Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades],
        };
        int copies = 8 / available.Length;
        var cards = new List<Card>(104);
        foreach (Suit suit in available)
            for (int copy = 0; copy < copies; copy++)
                foreach (Rank rank in Enum.GetValues<Rank>()) cards.Add(new Card(rank, suit));
        return cards;
    }

    private static List<Card> Shuffle(List<Card> cards, int seed)
    {
        var random = new DealRandom(seed);
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        return cards;
    }

    private sealed class DealRandom
    {
        private ulong state;
        public DealRandom(int seed) => state = unchecked((ulong)(uint)seed) + 0x9E3779B97F4A7C15UL;
        public int Next(int maxExclusive)
        {
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            ulong value = state * 2685821657736338717UL;
            return (int)(value % (uint)maxExclusive);
        }
    }
}
