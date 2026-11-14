using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.NativeShell;

internal sealed record NativeLobbyOutcome(bool Success, string Message, object? Reward = null);

/// <summary>
/// Authoritative native account/shop/collection/season action adapter.
/// No browser-supplied currency, rewards, level or profile state is accepted.
/// All successful user-data transitions pass through one durable aggregate.
/// </summary>
internal sealed class NativeLobbyController(NativeCasinoRuntime runtime)
{
    private readonly NativeLobbyPanels panels = new(runtime);
    private PlayerProfileState Profile => runtime.Profile;
    private PlayerProgressionController Progress => runtime.Progression;
    internal object Snapshot(string page) => panels.Snapshot(page);
    internal object ShopSnapshot() => panels.Shop();
    internal object? AlbumPending() => panels.AlbumPending();

    private NativeLobbyOutcome Commit(Func<NativeLobbyOutcome> action)
    {
        if (runtime.HandInProgress || runtime.ManorRequiresRestart
            || runtime.RouletteRequiresRestart || runtime.LoungeRequiresRestart)
            return new(false, "Finish the active game or restart after save recovery.");
        try
        {
            NativeLobbyOutcome result = action();
            if (result.Success) runtime.SaveLoungeProfile();
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException or ArgumentException
            or OverflowException)
        {
            runtime.MarkLoungeRecoveryRequired();
            return new(false, "Account save requires recovery: " + ex.Message);
        }
    }

    internal NativeLobbyOutcome PurchaseOrEquip(string id) => Commit(() =>
    {
        CosmeticDefinition? item = CosmeticCatalog.TryGet(id);
        if (item is null) return new(false, "Unknown cosmetic item.");
        if (Profile.OwnedCosmeticIds.Contains(id))
        {
            bool equipped = Progress.EquipCosmetic(id);
            return new(equipped, equipped ? item.Name + " equipped." : "Could not equip cosmetic.");
        }
        PurchaseResult purchase = Progress.PurchaseCosmetic(id);
        if (!purchase.Success) return new(false, purchase.Message);
        bool success = Progress.EquipCosmetic(id);
        return new(true, success
            ? "Equipped " + item.Name + "."
            : item.Name + " purchased. Equip it from the profile.");
    });

    internal NativeLobbyOutcome PurchasePack(string rarity) => Commit(() =>
    {
        if (!Enum.TryParse(rarity, false, out CollectionRarity parsed)
            || !Enum.IsDefined(parsed)
            || !CollectionCatalog.Packs.Any(p => p.Rarity == parsed))
            return new(false, "Unknown card pack rarity.");
        CollectionActionResult outcome = Progress.PurchaseCollectionPack(parsed);
        return new(outcome.Success, outcome.Message);
    });

    internal NativeLobbyOutcome OpenAlbumPack(string rarity) => Commit(() =>
    {
        if (!Enum.TryParse(rarity, false, out CollectionRarity parsed)
            || !Enum.IsDefined(parsed)
            || !CollectionCatalog.Packs.Any(p => p.Rarity == parsed))
            return new(false, "Unknown card pack rarity.");
        CollectionActionResult outcome = Progress.OpenCollectionPack(parsed);
        return new(outcome.Success, outcome.Message);
    });

    internal NativeLobbyOutcome CompleteAlbumOpening(string id) => Commit(() =>
    {
        if (!Guid.TryParseExact(id, "N", out Guid receipt))
            return new(false, "Invalid pack reveal receipt.");
        CollectionActionResult outcome = Progress.CompleteCollectionOpening(receipt);
        return new(outcome.Success, outcome.Message);
    });

    internal NativeLobbyOutcome ClaimAlbumTheme(string id)
    {
        if (runtime.HandInProgress || runtime.ManorRequiresRestart ||
            runtime.RouletteRequiresRestart || runtime.LoungeRequiresRestart)
            return new(false, "Finish the active game first.");
        try
        {
            if (!CollectionCatalog.Themes.Any(theme => theme.Id == id))
                return new(false, "Unknown collection theme.");
            CollectionThemeRewardClaimResult outcome =
                Progress.ClaimCollectionThemeReward(id);
            if (!outcome.Success) return new(false, outcome.Message);
            runtime.CreditLoungeWallet(outcome.Bankroll);
            return new(true, outcome.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException or OverflowException)
        {
            runtime.MarkLoungeRecoveryRequired();
            return new(false, "Collection reward requires recovery: " + ex.Message);
        }
    }

    internal NativeLobbyOutcome Rename(string value) => Commit(() =>
    {
        bool success = Progress.RenameProfile(value, out string message);
        return new(success, message);
    });

    internal NativeLobbyOutcome EquipProfile(string id) => Commit(() =>
    {
        CosmeticDefinition? item = CosmeticCatalog.TryGet(id);
        if (item is null || item.Kind is not (CosmeticKind.Portrait or CosmeticKind.Title)
            || !Profile.OwnedCosmeticIds.Contains(id))
            return new(false, "Profile cosmetic is not owned or eligible.");
        bool equipped = Progress.EquipCosmetic(id);
        return new(equipped, equipped ? item.Name + " equipped." : "Cosmetic could not be equipped.");
    });

    internal NativeLobbyOutcome ActivateSeason(string id) => Commit(() =>
    {
        if (SeasonCatalog.TryGet(id) is not { } season)
            return new(false, "Unknown Season Pass.");
        if (Profile.ActiveSeasonId == id)
            return new(true, "This Season Pass is already active.");
        bool success = Progress.Service.SelectSeason(id);
        return new(success, success ? season.Name + " is now active." : "Season Pass cannot be activated.");
    });

    internal NativeLobbyOutcome ClaimChallenge(string id) => Commit(() =>
    {
        if (!ChallengeCatalog.All.Any(challenge => challenge.Id == id))
            return new(false, "Unknown challenge.");
        ChallengeClaimResult outcome = Progress.ClaimChallenge(id);
        return new(outcome.Success, outcome.Message);
    });

    internal NativeLobbyOutcome ClaimAllChallenges() => Commit(() =>
    {
        ChallengeClaimAllResult result = Progress.ClaimAllChallenges();
        return new(result.Success, result.Message);
    });

    internal NativeLobbyOutcome RecordArcade(string id, int score, int seconds, Guid runId) =>
        Commit(() =>
        {
            if (!ArcadeChallengeCatalog.IsGame(id) || runId == Guid.Empty
                || score is < 0 or > 100_000_000 || seconds is < 0 or > 21_600)
                return new(false, "Invalid terminal Arcade run.");
            return new(Progress.ProcessArcadeRun(new ArcadeRunReceipt(runId, id, score, seconds)),
                "Arcade run recorded.");
        });

    internal NativeLobbyOutcome ClaimDaily()
    {
        if (runtime.HandInProgress || runtime.ManorRequiresRestart ||
            runtime.RouletteRequiresRestart || runtime.LoungeRequiresRestart)
            return new(false, "Finish the active game or recover the save first.");
        try
        {
            DailyLoginClaim? claim = DailyLoginProgress.TryClaim(
                runtime.Preferences.LastDailyLoginClaimDate,
                runtime.Preferences.DailyLoginClaims,
                DateOnly.FromDateTime(DateTime.Now));
            if (claim is null) return new(false, "Daily reward already claimed.");
            DailyLoginRewardDefinition reward =
                DailyLoginRewardSchedule.GetDefinition(claim.TotalClaims - 1);
            PlayerProfileState profile = ManorPersistence.Clone(Profile);
            profile.Coins = checked(profile.Coins + reward.CosmeticCoins);
            if (reward.PackRarity is { } rarity) CollectionService.GrantPack(profile, rarity);
            AppPreferences preferences = runtime.Preferences with
            {
                LastDailyLoginClaimDate = claim.ClaimDate,
                DailyLoginClaims = claim.TotalClaims,
                Bankroll = checked(runtime.Bankroll + reward.Bankroll),
            };
            runtime.CommitLobbyAggregate(preferences, profile);
            return new(true, $"Daily bonus · +{reward.Bankroll:C2} · +{reward.CosmeticCoins:N0} coins.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException or OverflowException)
        {
            runtime.MarkLoungeRecoveryRequired();
            return new(false, "Daily reward requires recovery: " + ex.Message);
        }
    }

    internal NativeLobbyOutcome OpenVault()
    {
        if (runtime.HandInProgress || runtime.ManorRequiresRestart ||
            runtime.RouletteRequiresRestart || runtime.LoungeRequiresRestart)
            return new(false, "Finish the active game before opening the Vault.");
        try
        {
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            CasinoVaultSnapshot before = CasinoVaultService.Snapshot(candidate);
            if (before.PayoutPending) return new(false, "A Vault payout is pending recovery.");
            if (!before.IsFull) return new(false, "The Vault is not full.");
            CasinoVaultOpenResult reward = CasinoVaultService.PrepareOpen(candidate);
            if (!CasinoVaultService.CompletePendingPayout(candidate, reward.PayoutId))
                throw new InvalidOperationException("Vault reward could not be finalized.");
            AppPreferences preferences = runtime.Preferences with
            {
                Bankroll = checked(runtime.Bankroll + reward.TotalPayout),
                LastAppliedVaultPayoutId = reward.PayoutId,
            };
            runtime.CommitLobbyAggregate(preferences, candidate);
            int index = CasinoVaultTierCatalog.All.ToList().FindIndex(t => t.Id == reward.TierId);
            return new(true, "Payout transferred to casino cash.", new
            {
                Tier = before.Tier.Name,
                BaseAmount = Money(reward.BaseAmount),
                BonusAmount = Money(reward.BonusAmount),
                Total = Money(reward.TotalPayout),
                Pack = ((CollectionRarity)Math.Clamp(index, 0, 4)).ToString(),
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or InvalidOperationException or OverflowException)
        {
            runtime.MarkLoungeRecoveryRequired();
            return new(false, "Vault payout requires recovery: " + ex.Message);
        }
    }

    private static string Money(decimal value) =>
        "$" + value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
