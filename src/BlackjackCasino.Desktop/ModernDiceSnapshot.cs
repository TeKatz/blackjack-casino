using System.Globalization;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

internal sealed record ModernDiceCelebration(long Id, string Player, string Category, int Points, int Bonus, int Face);

/// <summary>Read-only projections; React never computes a legal score, placement or cash settlement.</summary>
internal static class ModernDiceSnapshot
{
    private static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string ColorHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
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
            Name = KniffelView.CategoryName(category),
            Stored = game.ScoreFor(category),
            Preview = game.CanScore && game.ScoreFor(category) is null ? game.Preview(category) : (int?)null,
            Upper = category <= KniffelCategory.Sixes,
        }).ToArray(),
    };

    internal static object Create(KniffelGame game, KniffelState casual, KniffelTableState? table,
        bool casinoSelected, decimal bankroll, PlayerProfileState? profile,
        string diceVariant, AreaPresentationSettings settings, string notice,
        string completionNotice, bool waiting, IReadOnlyList<ModernDiceCelebration> events, long rollToken,
        int[] rollingIndices)
    {
        bool casino = casinoSelected && table is not null;
        DiceAppearance appearance = DiceAppearance.For(diceVariant);
        return new
        {
            Language = Localization.Language == UiLanguagePreference.German ? "de" : "en",
            Mode = casino ? "casino" : "casual",
            Bankroll = Money(bankroll),
            StakeOptions = KniffelTable.Stakes.Select(Money).ToArray(),
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
                Top = ColorHex(appearance.Top),
                Bottom = ColorHex(appearance.Bottom),
                Pips = ColorHex(appearance.Pips),
                Edge = ColorHex(appearance.Edge),
                Glow = appearance.Glow is { } glow ? ColorHex(glow) : (string?)null,
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
