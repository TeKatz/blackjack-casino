using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

internal sealed record ModernDiceCelebration(long Id, string Player, string Category, int Points, int Bonus, int Face);

/// <summary>Read-only projections; React never computes a legal score, placement or cash settlement.</summary>
internal static class ModernDiceSnapshot
{
    private static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static object Scores(KniffelGame game) => new
    {
        GameId = game.GameId.ToString("D"),
        Dice = game.Dice.ToArray(),
        Held = game.Held.ToArray(),
        game.RollsUsed,
        game.FilledCount,
        game.IsComplete,
        game.CanScore,
        game.CanRoll,
        game.IsAdditionalKniffel,
        game.KniffelsScored,
        game.UpperSubtotal,
        game.UpperBonus,
        game.LowerSubtotal,
        game.AdditionalKniffelBonus,
        game.Total,
        Categories = KniffelScoring.Categories.Select(category => new
        {
            Id = category.ToString(),
            Name = KniffelCategoryLabels.For(category),
            Stored = game.ScoreFor(category),
            Preview = game.CanScore && game.ScoreFor(category) is null ? game.Preview(category) : (int?)null,
            Upper = category <= KniffelCategory.Sixes,
        }).ToArray(),
    };

    internal static object Create(KniffelGame game, KniffelState casual, KniffelTableState? table,
        bool casinoSelected, decimal bankroll, PlayerProfileState? profile,
        string diceVariant, AreaPresentationSettings settings, string notice,
        string completionNotice, bool waiting, IReadOnlyList<ModernDiceCelebration> events, long rollToken,
        int[] rollingIndices, UiLanguagePreference language)
    {
        bool casino = casinoSelected && table is not null;
        DicePaletteTokens appearance = DicePaletteTokens.For(diceVariant);
        return new
        {
            Language = language == UiLanguagePreference.German ? "de" : "en",
            Mode = casino ? "casino" : "casual",
            Bankroll = Money(bankroll),
            StakeOptions = KniffelTable.Stakes.Select(Money).ToArray(),
            StakePrizes = KniffelTable.Stakes.Select(stake => new
            {
                Stake = Money(stake),
                Prizes = Enumerable.Range(1, 4).Select(place => Money(KniffelTable.PrizeForPlace(stake, place))).ToArray(),
            }).ToArray(),
            CasualHasSaved = casual.Entries.Length > 0 || casual.RollsUsed > 0,
            CasualCompleted = casual.Entries.Length == KniffelScoring.Categories.Count,
            Game = Scores(game),
            Table = table is null ? null : new
            {
                Stake = Money(table.Stake),
                table.Settled, table.Forfeited,
                Payout = Money(table.Payout),
                Net = Money(table.Payout - table.Stake),
                table.Placement,
                Waiting = casino && waiting,
                table.LastMoves,
                Tied = table.Opponents.Any(op => KniffelGame.Restore(op).Total == KniffelGame.Restore(table.Player).Total),
                Opponents = table.Opponents.Select((op, i) => new
                {
                    Name = KniffelTable.Names[i],
                    Style = ((KniffelAiStyle)i).ToString(),
                    Game = Scores(KniffelGame.Restore(op)),
                }).ToArray(),
            },
            Profile = profile is null ? null : new { profile.Level, profile.Prestige, profile.Coins },
            DiceVariant = diceVariant,
            DicePalette = new
            {
                appearance.Top,
                appearance.Bottom,
                appearance.Pips,
                appearance.Edge,
                appearance.Glow,
                appearance.Engraved,
            },
            SoundEnabled = settings.SoundEnabled,
            AnimationsEnabled = settings.AnimationsEnabled,
            MusicEnabled = settings.MusicEnabled,
            settings.MusicVolume,
            Notice = notice,
            CompletionNotice = completionNotice,
            Events = events,
            RollToken = rollToken,
            RollingIndices = rollingIndices,
            CompletionXp = KniffelProgression.PlayerExperience(game.Total),
            CompletionSeasonXp = KniffelProgression.SeasonExperience(game.Total),
        };
    }
}
