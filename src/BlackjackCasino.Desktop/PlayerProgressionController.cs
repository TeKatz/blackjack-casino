using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class PlayerProgressionController : IDisposable
{
    private readonly TimeProvider timeProvider;
    private readonly long sessionStartedAt;
    private long accountedSessionSeconds;
    private TimeSpan? finalSessionElapsed;
    private readonly List<RoundHistoryEntry> history;
    private readonly HashSet<Guid> knownRoundIds;
    private bool disposed;
    private readonly bool persist;
    private readonly bool persistChallengeClaims;
    private readonly Action<PlayerProfileState> saveChallengeProfile;
    private readonly Action<PlayerProfileState> saveKniffelProfile;
    private readonly bool persistKniffelRewards;

    public PlayerProgressionController(PlayerProfileState? initialProfile = null, Action<PlayerProfileState>? challengeProfileSaver = null,
        TimeProvider? timeProvider = null, Action<PlayerProfileState>? kniffelProfileSaver = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        sessionStartedAt = this.timeProvider.GetTimestamp();
        persist = initialProfile is null;
        persistChallengeClaims = persist || challengeProfileSaver is not null;
        saveChallengeProfile = challengeProfileSaver ?? PlayerProfileStore.SaveProfileOrThrow;
        persistKniffelRewards = persist || kniffelProfileSaver is not null;
        saveKniffelProfile = kniffelProfileSaver ?? PlayerProfileStore.SaveProfileOrThrow;
        Service = new ProgressionService(initialProfile ?? PlayerProfileStore.LoadProfile());
        history = initialProfile is null ? PlayerProfileStore.LoadHistory().ToList() : [];
        knownRoundIds = history.Select(x => x.RoundId).ToHashSet();
    }

    public ProgressionService Service { get; }
    public PlayerProfileState Profile => Service.State;
    public IReadOnlyList<RoundHistoryEntry> History => history;
    public TimeSpan SessionElapsed => finalSessionElapsed ?? timeProvider.GetElapsedTime(sessionStartedAt);

    public TimeSpan UnrecordedSessionElapsed
    {
        get
        {
            lock (Profile) { return UnrecordedElapsed(); }
        }
    }

    public TimeSpan TotalPlaytime
    {
        get
        {
            lock (Profile) { return TimeSpan.FromSeconds(Profile.TotalPlaytimeSeconds) + UnrecordedElapsed(); }
        }
    }

    public void AccruePlaytime()
    {
        lock (Profile)
        {
            if (disposed) return;
            long elapsedSeconds = SessionElapsed.Ticks / TimeSpan.TicksPerSecond;
            long unrecordedSeconds = elapsedSeconds - accountedSessionSeconds;
            if (unrecordedSeconds <= 0) return;
            Service.AddPlaytime(TimeSpan.FromSeconds(unrecordedSeconds));
            accountedSessionSeconds = elapsedSeconds;
        }
    }

    private TimeSpan UnrecordedElapsed()
    {
        TimeSpan remaining = SessionElapsed - TimeSpan.FromSeconds(accountedSessionSeconds);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public ProgressionAward ProcessRound(RoundHistoryEntry round)
    {
        if (!knownRoundIds.Add(round.RoundId)) return ProgressionAward.None;
        ProgressionAward award = Service.ProcessRound(round);
        history.Add(round);
        if (history.Count > 1_000) history.RemoveRange(0, history.Count - 1_000);
        SaveAll();
        return award;
    }

    public ProgressionAward ProcessKniffel(BlackjackCasino.Core.Kniffel.KniffelState checkpoint)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            ProgressionAward award = new BlackjackCasino.Core.Kniffel.KniffelProgression(new ProgressionService(candidate)).Process(checkpoint);
            if (award == ProgressionAward.None) return award;
            CasinoVaultService.SyncFromStatistics(candidate);
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return award;
        }
    }

    public ProgressionAward ProcessKniffelTable(BlackjackCasino.Core.Kniffel.KniffelTableState table)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            ProgressionAward award = new BlackjackCasino.Core.Kniffel.KniffelProgression(new ProgressionService(candidate)).ProcessTable(table);
            CasinoVaultService.SyncFromStatistics(candidate);
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return award;
        }
    }

    public ProgressionAward ProcessSudoku(BlackjackCasino.Core.Sudoku.SudokuCompletionRecord completion)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            ProgressionAward award = new BlackjackCasino.Core.Sudoku.SudokuProgression(new ProgressionService(candidate)).Process(completion);
            if (award == ProgressionAward.None) return award;
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return award;
        }
    }

    public bool ProcessSolitaireStart(BlackjackCasino.Core.Solitaire.SolitaireStartRecord start)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            bool changed = new BlackjackCasino.Core.Solitaire.SolitaireProgression(new ProgressionService(candidate)).ProcessStart(start);
            if (!changed) return false;
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return true;
        }
    }

    public bool ProcessSolitaireAbandon(BlackjackCasino.Core.Solitaire.SolitaireAbandonRecord abandon)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            bool changed = new BlackjackCasino.Core.Solitaire.SolitaireProgression(new ProgressionService(candidate)).ProcessAbandon(abandon);
            if (!changed) return false;
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return true;
        }
    }

    public ProgressionAward ProcessSolitaire(BlackjackCasino.Core.Solitaire.SolitaireCompletionRecord completion)
    {
        lock (Profile)
        {
            AccruePlaytime();
            PlayerProfileState candidate = ManorPersistence.Clone(Profile);
            ProgressionAward award = new BlackjackCasino.Core.Solitaire.SolitaireProgression(new ProgressionService(candidate)).ProcessWin(completion);
            if (award == ProgressionAward.None) return award;
            if (persistKniffelRewards) saveKniffelProfile(candidate);
            AdoptManorProfile(candidate);
            return award;
        }
    }

    public PurchaseResult PurchaseCosmetic(string cosmeticId)
    {
        PurchaseResult result = Service.PurchaseCosmetic(cosmeticId);
        if (result.Success && persist) PlayerProfileStore.SaveProfile(Profile);
        return result;
    }

    public bool EquipCosmetic(string cosmeticId)
    {
        bool equipped = Service.EquipCosmetic(cosmeticId);
        if (equipped && persist) PlayerProfileStore.SaveProfile(Profile);
        return equipped;
    }

    public bool RenameProfile(string name, out string message)
    {
        string previous = Profile.DisplayName;
        try
        {
            Profile.DisplayName = ProfileIdentity.Validate(name);
            if (persist) PlayerProfileStore.SaveProfileOrThrow(Profile);
            message = "Profile name saved.";
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Profile.DisplayName = previous;
            message = ex.Message;
            return false;
        }
    }

    public ChallengeClaimResult ClaimChallenge(string challengeId)
    {
        lock (Profile)
        {
            AccruePlaytime();
            ChallengeClaimResult result = Service.ClaimChallenge(challengeId);
            if (result.Success && persist) PlayerProfileStore.SaveProfile(Profile);
            return result;
        }
    }

    public ChallengeClaimAllResult ClaimAllChallenges()
    {
        lock (Profile)
        {
            AccruePlaytime();
            long previousCoins = Profile.Coins;
            List<string> previousClaims = Profile.ClaimedChallengeIds;
            ChallengeClaimAllResult result = Service.ClaimAllChallenges();
            if (!result.Success || !persistChallengeClaims) return result;
            try
            {
                saveChallengeProfile(Profile);
                return result;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Profile.Coins = previousCoins;
                Profile.ClaimedChallengeIds = previousClaims;
                return new(false, "Challenge rewards could not be saved. Please try again. " + ex.Message, []);
            }
        }
    }

    public PrestigeResult Prestige()
    {
        PrestigeResult result = Service.Prestige();
        if (result.Success && persist) PlayerProfileStore.SaveProfile(Profile);
        return result;
    }

    public void AdoptManorProfile(PlayerProfileState committed)
    {
        Profile.Kniffel = committed.Kniffel;
        Profile.Sudoku = committed.Sudoku;
        Profile.Solitaire = committed.Solitaire;
        Profile.Manor = committed.Manor;
        Profile.Vault = committed.Vault;
        Profile.SchemaVersion = committed.SchemaVersion;
        Profile.SlotTrackingVersion = committed.SlotTrackingVersion;
        Profile.Slots = committed.Slots;
        Profile.NeonVault = committed.NeonVault;
        Profile.NineLives = committed.NineLives;
        Profile.LegacyPooledSlots = committed.LegacyPooledSlots;
        Profile.DisplayName = committed.DisplayName;
        Profile.Level = committed.Level;
        Profile.LevelExperience = committed.LevelExperience;
        Profile.Prestige = committed.Prestige;
        Profile.Coins = committed.Coins;
        Profile.LifetimeExperience = committed.LifetimeExperience;
        Profile.LastPlayedAt = committed.LastPlayedAt;
        Profile.Seasons = committed.Seasons;
        Profile.ActiveSeasonId = committed.ActiveSeasonId;
        Profile.OwnedCosmeticIds = committed.OwnedCosmeticIds;
        Profile.ClaimedChallengeIds = committed.ClaimedChallengeIds;
        Profile.EquippedTitleId = committed.EquippedTitleId;
        Profile.EquippedFrameId = committed.EquippedFrameId;
        Profile.EquippedPortraitId = committed.EquippedPortraitId;
        Profile.EquippedCardSetId = committed.EquippedCardSetId;
        Profile.EquippedDiceSetId = committed.EquippedDiceSetId;
    }

    public void SaveAll()
    {
        AccruePlaytime();
        CasinoVaultService.SyncFromStatistics(Profile);
        if (!persist) return;
        PlayerProfileStore.SaveProfile(Profile);
        PlayerProfileStore.SaveHistory(history);
    }

    public void Dispose()
    {
        lock (Profile)
        {
            if (disposed) return;
            finalSessionElapsed = SessionElapsed;
            AccruePlaytime();
            disposed = true;
            SaveAll();
        }
    }
}
