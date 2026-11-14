using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

// This parser is deliberately narrower than GameRules.Validate: the UI offers a
// defined, finite rule editor rather than arbitrary JSON deserialization.
internal static class BlackjackModernRules
{
    internal static bool TryParse(string? input, out GameRules? rules)
    {
        rules = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 2048) return false;
        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 15)
                return false;

            bool GetBool(string name) => root.TryGetProperty(name, out JsonElement value)
                && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? value.GetBoolean() : throw new FormatException(name);
            int GetInt(string name) => root.TryGetProperty(name, out JsonElement value)
                && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
                    ? number : throw new FormatException(name);
            string GetText(string name) => root.TryGetProperty(name, out JsonElement value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? throw new FormatException(name) : throw new FormatException(name);
            decimal GetDecimal(string name)
            {
                string value = GetText(name);
                return value.Length <= 32
                    && decimal.TryParse(value, NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out decimal number)
                        ? number : throw new FormatException(name);
            }
            string doubleOption = GetText("doubleRule");
            if (doubleOption is not ("AnyTwo" or "NineToEleven" or "TenToEleven"))
                return false;
            decimal payout = GetDecimal("blackjackPayout");
            decimal penetration = GetDecimal("penetration");
            decimal min = GetDecimal("minimumBet"), max = GetDecimal("maximumBet");
            if (payout is not (1.5m or 1.2m) || penetration is < .50m or > .90m
                || min is < .01m or > 1_000_000m || max is < .01m or > 1_000_000m)
                return false;
            var candidate = new GameRules
            {
                DeckCount = GetInt("deckCount"),
                DealerHitsSoft17 = GetBool("dealerHitsSoft17"),
                DealerPeeksForBlackjack = GetBool("dealerPeeksForBlackjack"),
                DealerTakesHoleCard = GetBool("dealerTakesHoleCard"),
                AllowInsurance = GetBool("allowInsurance"),
                AllowLateSurrender = GetBool("allowLateSurrender"),
                DoubleRule = Enum.Parse<DoubleRule>(doubleOption),
                DoubleAfterSplit = GetBool("doubleAfterSplit"),
                ResplitAces = GetBool("resplitAces"),
                HitSplitAces = GetBool("hitSplitAces"),
                MaxSplitHands = GetInt("maxSplitHands"),
                BlackjackPayout = payout,
                Penetration = (double)penetration,
                MinimumBet = min,
                MaximumBet = max,
            };
            if (candidate.MaxSplitHands is < 2 or > 8
                || !candidate.DealerTakesHoleCard && candidate.DealerPeeksForBlackjack)
                return false;
            candidate.Validate();
            rules = candidate;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException
            or InvalidOperationException or OverflowException or ArgumentException)
        {
            return false;
        }
    }
}
