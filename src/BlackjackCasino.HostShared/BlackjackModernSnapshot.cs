using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

// A read-only projection of the Core session. The hidden dealer card is never
// sent to the browser before settlement, not even in a concealed HTML element.
internal static class BlackjackModernSnapshot
{
    private static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static object CardView(Card card, bool hidden = false) => new
    {
        Rank = hidden ? null : card.Rank.ToString(),
        Suit = hidden ? null : card.Suit.ToString(),
        FaceDown = hidden,
    };

    internal static object Create(CasinoSession session, decimal selectedBet, bool fullTable,
        int aiSeatCount, string preset, string theme, string cardSetId, bool soundEnabled,
        bool animationsEnabled, string status, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(session);
        BlackjackRound? round = session.CurrentRound;
        bool conceal = round is { Phase: RoundPhase.InsuranceDecision or RoundPhase.PlayerTurn };
        GameRules rules = session.Rules;
        return new
        {
            Bankroll = Money(session.Bankroll),
            SelectedBet = Money(selectedBet),
            session.ShoeNumber,
            session.CardsRemaining,
            FullTable = fullTable,
            AiSeatCount = aiSeatCount,
            Preset = preset,
            Theme = theme,
            CardSetId = cardSetId,
            CardVariant = CosmeticCatalog.TryGet(cardSetId) is { Kind: CosmeticKind.CardSet } cardSet
                ? cardSet.VisualVariant : "card-classic",
            SoundEnabled = soundEnabled,
            AnimationsEnabled = animationsEnabled,
            Status = status,
            Language = language,
            Rules = new
            {
                rules.DeckCount, rules.DealerHitsSoft17, rules.DealerPeeksForBlackjack,
                rules.DealerTakesHoleCard, rules.AllowInsurance, rules.AllowLateSurrender,
                DoubleRule = rules.DoubleRule.ToString(), rules.DoubleAfterSplit,
                rules.ResplitAces, rules.HitSplitAces, rules.MaxSplitHands,
                rules.Penetration,
                BlackjackPayout = Money(rules.BlackjackPayout),
                MinimumBet = Money(rules.MinimumBet), MaximumBet = Money(rules.MaximumBet),
            },
            Presets = TablePresets.All.Select(p => new { Id = p.Id.ToString(), p.Name, p.Description }).ToArray(),
            CanDeal = !session.RoundInProgress && selectedBet >= rules.MinimumBet
                && selectedBet <= rules.MaximumBet && selectedBet <= session.Bankroll,
            AvailableActions = session.GetAvailableActions().Select(a => a.ToString()).ToArray(),
            Round = round is null ? null : new
            {
                Phase = round.Phase.ToString(),
                round.ActiveHandIndex,
                InitialBet = Money(round.InitialBet),
                InsuranceBet = Money(round.InsuranceBet),
                DealerCards = round.DealerHand.Cards.Select((c, i) => CardView(c, conceal && i > 0)).ToArray(),
                DealerValue = conceal ? null : (int?)round.DealerHand.Value.Total,
                PlayerHands = round.PlayerHands.Select((hand, i) => new
                {
                    Index = i,
                    Bet = Money(hand.Bet),
                    Value = hand.Value.Total,
                    hand.Value.IsSoft, hand.IsSplitHand, hand.IsDoubled, hand.IsSurrendered,
                    Active = round.Phase == RoundPhase.PlayerTurn && round.ActiveHandIndex == i,
                    Cards = hand.Cards.Select(c => CardView(c)).ToArray(),
                    Outcome = round.Result is null ? null : round.Result.Hands[i].Outcome.ToString(),
                    Net = round.Result is null ? null : Money(round.Result.Hands[i].Net),
                }).ToArray(),
                AiSeats = round.AiSeats.Select(seat => new
                {
                    seat.Name,
                    Hands = seat.Hands.Select((hand, i) => new
                    {
                        Index = i,
                        Bet = Money(hand.Bet),
                        Value = hand.Value.Total,
                        Cards = hand.Cards.Select(c => CardView(c)).ToArray(),
                        Outcome = seat.Results.Count > i ? seat.Results[i].Outcome.ToString() : null,
                    }).ToArray(),
                }).ToArray(),
                TotalNet = round.Result is null ? null : Money(round.Result.TotalNet),
                InsuranceNet = round.Result is null ? null : Money(round.Result.InsuranceNet),
            },
        };
    }
}
