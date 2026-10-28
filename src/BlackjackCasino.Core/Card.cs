namespace BlackjackCasino.Core;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades,
}

public enum Rank
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
}

public readonly record struct Card(Rank Rank, Suit Suit)
{
    public int HardValue => Rank switch
    {
        Rank.Ace => 1,
        Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)Rank,
    };

    public bool IsTenValue => HardValue == 10;

    public override string ToString() => $"{Rank} of {Suit}";
}
