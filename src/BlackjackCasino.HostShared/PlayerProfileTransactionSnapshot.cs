using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal enum RoutineProfileTransactionArea
{
    Kniffel,
    Sudoku,
    MidnightMerge,
    Solitaire,
    WordSearch,
    Memory,
    Jigsaw,
    Manor,
    Arcade,
}

/// <summary>
/// Creates an isolated transaction candidate for high-frequency game progression without cloning
/// the entire profile through JSON. Only state that routine progression may mutate is copied;
/// untouched heavyweight state remains shared and read-only for the lifetime of the transaction.
/// </summary>
internal static class PlayerProfileTransactionSnapshot
{
    public static PlayerProfileState Create(PlayerProfileState source, RoutineProfileTransactionArea area)
    {
        ArgumentNullException.ThrowIfNull(source);

        var candidate = new PlayerProfileState
        {
            SchemaVersion = source.SchemaVersion,
            DisplayName = source.DisplayName,
            Level = source.Level,
            LevelExperience = source.LevelExperience,
            Prestige = source.Prestige,
            Coins = source.Coins,
            LifetimeExperience = source.LifetimeExperience,
            TotalPlaytimeSeconds = source.TotalPlaytimeSeconds,
            CreatedAt = source.CreatedAt,
            LastPlayedAt = source.LastPlayedAt,
            EquippedTitleId = source.EquippedTitleId,
            EquippedFrameId = source.EquippedFrameId,
            EquippedPortraitId = source.EquippedPortraitId,
            EquippedCardSetId = source.EquippedCardSetId,
            EquippedDiceSetId = source.EquippedDiceSetId,
            OwnedCosmeticIds = [.. source.OwnedCosmeticIds],
            ClaimedChallengeIds = [.. source.ClaimedChallengeIds],
            Adventure = source.Adventure,
            TowerDefense = source.TowerDefense,
            Minesweeper = source.Minesweeper,
            Kniffel = source.Kniffel,
            Sudoku = source.Sudoku,
            SudokuCareer = source.SudokuCareer,
            SudokuVariants = source.SudokuVariants,
            Solitaire = source.Solitaire,
            WordSearch = source.WordSearch,
            Memory = source.Memory,
            Jigsaw = source.Jigsaw,
            Arcade = source.Arcade,
            MidnightMerge = source.MidnightMerge,
            Manor = source.Manor,
            // Season rewards can grant card packs, so collection state must be isolated for every
            // routine progression transaction even when the game itself does not touch the album.
            Collection = ManorPersistence.Clone(source.Collection),
            Vault = source.Vault,
            SlotTrackingVersion = source.SlotTrackingVersion,
            Slots = source.Slots,
            NeonVault = source.NeonVault,
            NineLives = source.NineLives,
            LegacyPooledSlots = source.LegacyPooledSlots,
            LifetimeStatistics = source.LifetimeStatistics,
            TableStatistics = source.TableStatistics,
            Seasons = CloneSeasons(source.Seasons),
            ActiveSeasonId = source.ActiveSeasonId,
        };

        switch (area)
        {
            case RoutineProfileTransactionArea.Kniffel:
                candidate.Kniffel = ManorPersistence.Clone(source.Kniffel);
                // SyncFromStatistics writes the vault after Kniffel progression.
                candidate.Vault = ManorPersistence.Clone(source.Vault);
                break;
            case RoutineProfileTransactionArea.Sudoku:
                candidate.Sudoku = ManorPersistence.Clone(source.Sudoku);
                candidate.SudokuCareer = ManorPersistence.Clone(source.SudokuCareer);
                candidate.SudokuVariants = ManorPersistence.Clone(source.SudokuVariants);
                break;
            case RoutineProfileTransactionArea.MidnightMerge:
                candidate.MidnightMerge = ManorPersistence.Clone(source.MidnightMerge);
                break;
            case RoutineProfileTransactionArea.Solitaire:
                candidate.Solitaire = ManorPersistence.Clone(source.Solitaire);
                break;
            case RoutineProfileTransactionArea.WordSearch:
                candidate.WordSearch = ManorPersistence.Clone(source.WordSearch);
                break;
            case RoutineProfileTransactionArea.Memory:
                candidate.Memory = ManorPersistence.Clone(source.Memory);
                break;
            case RoutineProfileTransactionArea.Jigsaw:
                candidate.Jigsaw = ManorPersistence.Clone(source.Jigsaw);
                break;
            case RoutineProfileTransactionArea.Arcade:
                candidate.Arcade = ManorPersistence.Clone(source.Arcade);
                break;
            case RoutineProfileTransactionArea.Manor:
                candidate.Manor = ManorPersistence.Clone(source.Manor);
                // Manor syncs Casino Vault progress from its durable statistics before committing.
                candidate.Vault = ManorPersistence.Clone(source.Vault);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(area), area, null);
        }

        return candidate;
    }

    private static Dictionary<string, SeasonProgressState> CloneSeasons(
        IReadOnlyDictionary<string, SeasonProgressState> source)
    {
        var clone = new Dictionary<string, SeasonProgressState>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, SeasonProgressState value) in source)
        {
            clone[key] = new SeasonProgressState
            {
                SeasonId = value.SeasonId,
                Experience = value.Experience,
                Tier = value.Tier,
            };
        }

        return clone;
    }
}
