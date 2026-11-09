using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Core;

/// <summary>Idempotent structural normalization shared by persistence and progression.</summary>
public static class ProfileNormalization
{
    public static void Normalize(PlayerProfileState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Kniffel ??= new();
        state.Kniffel.RewardedGameIds ??= [];
        state.Kniffel.RewardedGameIds.Remove(Guid.Empty);
        state.Kniffel.EnteredTableIds ??= [];
        state.Kniffel.SettledTableIds ??= [];
        state.Kniffel.EnteredTableIds.Remove(Guid.Empty);
        state.Kniffel.SettledTableIds.Remove(Guid.Empty);
        state.Sudoku ??= new();
        state.Sudoku.RecordedPuzzleIds ??= new HashSet<string>(StringComparer.Ordinal);
        state.Solitaire ??= new();
        state.Solitaire.Normalize();
        state.WordSearch ??= new();
        state.WordSearch.Normalize();
        state.Memory ??= new();
        state.Memory.Normalize();
        state.Minesweeper ??= new();
        state.Minesweeper.Normalize();
        state.Jigsaw ??= new();
        state.Jigsaw.Normalize();
        state.MidnightMerge ??= new();
        state.MidnightMerge.Normalize();
        state.Slots ??= new();
        state.NeonVault ??= new();
        state.NineLives ??= new();
        state.LegacyPooledSlots ??= new();
        state.Adventure = AdventureMetaProgressState.Normalize(state.Adventure);

        if (state.SlotTrackingVersion < 1)
        {
            // Both game types wrote to Slots. No recorded source exists, so neither game
            // can honestly inherit these totals. Keep the exact snapshot separately.
            state.LegacyPooledSlots = state.Slots;
            state.Slots = new();
            state.SlotTrackingVersion = 1;
        }

        state.Manor ??= new();
        state.LifetimeStatistics ??= new();
        state.TableStatistics ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (string key in state.TableStatistics.Keys.ToArray())
        {
            state.TableStatistics[key] ??= new();
        }
        state.Seasons ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (string key in state.Seasons.Keys.ToArray())
        {
            state.Seasons[key] ??= new() { SeasonId = key };
        }
        state.OwnedCosmeticIds ??= [];
        state.ClaimedChallengeIds ??= [];
        state.EquippedCardSetId = string.IsNullOrWhiteSpace(state.EquippedCardSetId)
            ? CosmeticCatalog.DefaultCardSetId
            : state.EquippedCardSetId;
        state.EquippedDiceSetId = string.IsNullOrWhiteSpace(state.EquippedDiceSetId)
            ? CosmeticCatalog.DefaultDiceSetId
            : state.EquippedDiceSetId;

        Collections.CollectionService.Normalize(state);
        state.Vault = CasinoVaultService.Normalize(state, state.Vault);
        state.SchemaVersion = Math.Max(state.SchemaVersion, 6);
    }
}
