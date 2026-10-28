using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public interface ICardShoe
{
    int Remaining { get; }

    Card Draw();
}

public sealed class Shoe : ICardShoe
{
    private readonly List<Card> cards;
    private int position;

    public Shoe(GameRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        rules.Validate();

        cards = BuildCards(rules.DeckCount);
        Shuffle(cards);
        CutCardIndex = Math.Clamp((int)Math.Floor(cards.Count * rules.Penetration), 1, cards.Count - 1);
    }

    public int TotalCards => cards.Count;

    public int Remaining => cards.Count - position;

    public int Dealt => position;

    public int CutCardIndex { get; }

    public bool ReachedCutCard => Dealt >= CutCardIndex;

    public Card Draw()
    {
        if (position >= cards.Count)
        {
            throw new InvalidOperationException("The shoe is empty.");
        }

        return cards[position++];
    }

    private static List<Card> BuildCards(int deckCount)
    {
        var result = new List<Card>(deckCount * 52);

        for (int deck = 0; deck < deckCount; deck++)
        {
            foreach (Suit suit in Enum.GetValues<Suit>())
            {
                foreach (Rank rank in Enum.GetValues<Rank>())
                {
                    result.Add(new Card(rank, suit));
                }
            }
        }

        return result;
    }

    private static void Shuffle(List<Card> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
