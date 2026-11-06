using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.NativeShell;

namespace BlackjackCasino.Desktop;

/// <summary>
/// First WPF-independent composition root. Isolated preview storage deliberately
/// prevents an incomplete host from overwriting the installed casino's saves.
/// It uses the very same validated blackjack/reels protocols and Core engines.
/// </summary>
internal sealed class NativePreviewRuntime : IDisposable
{
    private readonly string directory;
    private readonly PlayerProgressionController progression;
    private readonly BlackjackTableController table;
    private SlotMachineSession? reelsMachine;
    private SlotSpinResult? reelsLastSpin;
    private ProgressionAward? reelsAward;
    private decimal reelsBet;
    private NeonVaultSession? neonMachine;
    private NeonVaultSpinResult? neonLastSpin;
    private ProgressionAward? neonAward;
    private decimal neonBet;
    private NineLivesSession? nineMachine;
    private NineLivesSpinResult? nineLastSpin;
    private ProgressionAward? nineAward;
    private decimal nineBet;
    private MergeBoard? mergeBoard;
    private MergeActionResult? mergeLastAction;
    private string mergeFeedback = "Select a producer and restore the Midnight Club.";
    private long mergePlayerXp, mergeSeasonXp;
    private bool mergeProgressionSaveFailed;
    private bool disposed;
    private string status = "Choose a game from the native shell.";

    internal AppPreferences Preferences { get; private set; }
    internal decimal Bankroll => table.Session.Bankroll;
    internal bool HandInProgress => table.Session.RoundInProgress;
    internal PlayerProfileState Profile => progression.Profile;
    internal string Status => status;

    internal NativePreviewRuntime(string previewDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previewDirectory);
        directory = Path.GetFullPath(previewDirectory);
        Preferences = PlayerStateStore.Load(directory);
        progression = new PlayerProgressionController(LoadPreviewProfile(directory));
        table = new BlackjackTableController(Preferences);
        reelsBet = Math.Clamp(Preferences.SlotBet, SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
        neonBet = NeonVaultRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NeonVaultBet)).First();
        nineBet = NineLivesRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NineLivesBet)).First();
    }

    internal object BlackjackSnapshot() => BlackjackModernSnapshot.Create(
        table.Session, table.SelectedBet, table.PlayMode == PlayModePreference.FullTable,
        table.AiSeatCount, table.CurrentPreset.ToString(), table.Theme.ToString(),
        Profile.EquippedCardSetId, table.SoundEnabled, table.AnimationsEnabled,
        status, Preferences.Language == UiLanguagePreference.German ? "de" : "en");

    internal (bool Success, string Message) ApplyBlackjack(BlackjackModernCommand command)
    {
        if (disposed) return (false, "Casino session is closed.");
        if (command.Action is "mode.set" or "seats.set" or "preset.set" or "rules.custom.set")
            AccountCompletedRound();

        BlackjackTableActionResult result = table.Apply(command);
        if (!result.Success) return (false, result.Message);
        status = result.Message;
        if (result.RoundChanged && table.Session.CurrentRound is { } round)
        {
            status = round.Phase switch
            {
                RoundPhase.InsuranceDecision => "Insurance decision required.",
                RoundPhase.PlayerTurn => result.Message,
                RoundPhase.Settled => $"Round settled: {round.Result!.TotalNet.ToString(CultureInfo.InvariantCulture)}.",
                _ => result.Message,
            };
        }
        AccountCompletedRound();
        Persist();
        return (true, status);
    }

    private void AccountCompletedRound()
    {
        if (table.Session.History.Count == 0) return;
        ProgressionAward award = progression.ProcessRound(table.Session.History[^1]);
        if (award.BankrollEarned > 0m)
            table.Session.CreditBankroll(award.BankrollEarned);
    }

    internal void OpenReels()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before visiting Reels.");
        AccountCompletedRound();
        // Recreating the WebView2 page after a browser fault must not reset
        // a completed native spin, its award or the authoritative balance.
        if (reelsMachine is null)
        {
            reelsMachine = new SlotMachineSession(Bankroll);
            reelsBet = Math.Clamp(Preferences.SlotBet,
                SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
            if (reelsMachine.Bankroll >= SlotMachineRules.MinimumBet)
                reelsBet = Math.Min(reelsBet, decimal.Floor(reelsMachine.Bankroll * 100) / 100);
            reelsLastSpin = null;
            reelsAward = null;
        }
        status = "Midnight Reels ready.";
    }

    internal object ReelsSnapshot()
    {
        if (reelsMachine is null) throw new InvalidOperationException("Reels are not active.");
        AreaPresentationSettings presentation = Preferences.Presentation("midnight-reels");
        return ModernReelsSnapshot.Create(reelsMachine, reelsBet, reelsLastSpin,
            reelsAward, Profile, presentation.SoundEnabled, presentation.AnimationsEnabled,
            Preferences.Language == UiLanguagePreference.German ? "de" : "en");
    }

    internal (bool Success, string Message) ApplyReels(ModernReelsCommand command)
    {
        if (disposed || reelsMachine is null)
            return (false, "Reels are not active.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                {
                    decimal value = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (value > reelsMachine.Bankroll) return (false, "Insufficient balance.");
                    reelsBet = value;
                    Preferences = Preferences with { SlotBet = value };
                    status = "Wager updated.";
                    break;
                }
                case "sound.set":
                case "animations.set":
                {
                    AreaPresentationSettings settings = Preferences.Presentation("midnight-reels");
                    bool enabled = command.Value == "true";
                    settings = command.Action == "sound.set"
                        ? settings with { SoundEnabled = enabled }
                        : settings with { AnimationsEnabled = enabled };
                    Preferences = Preferences.WithPresentation("midnight-reels", settings);
                    status = "Presentation updated.";
                    break;
                }
                case "spin":
                {
                    if (reelsBet < SlotMachineRules.MinimumBet ||
                        reelsBet > SlotMachineRules.MaximumBet || reelsBet > reelsMachine.Bankroll)
                        return (false, "Select an affordable wager.");
                    SlotSpinResult spin = reelsMachine.Spin(reelsBet);
                    table.Session.ResetBankroll(spin.BankrollAfter);
                    reelsAward = new SlotProgression(progression.Service).Process(spin);
                    reelsLastSpin = spin;
                    status = "Spin settled.";
                    break;
                }
                default:
                    return (false, "Unsupported Reels command.");
            }

            Persist();
            return (true, status);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or FormatException or OverflowException)
        {
            return (false, ex.Message);
        }
    }

    internal void CloseReels()
    {
        if (reelsMachine is null) return;
        Preferences = Preferences with { SlotBet = reelsBet };
        reelsMachine = null;
        reelsLastSpin = null;
        reelsAward = null;
        Persist();
    }


    internal void OpenNeonVault()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before Neon Vault.");
        AccountCompletedRound();
        if (neonMachine is null)
        {
            neonMachine = new NeonVaultSession(Bankroll);
            neonBet = NeonVaultRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NeonVaultBet)).First();
            neonLastSpin = null;
            neonAward = null;
        }
        else
        {
            neonMachine.SynchronizeBankroll(Bankroll);
        }
        status = "Neon Vault ready.";
    }

    internal object NeonVaultSnapshot()
    {
        if (neonMachine is null) throw new InvalidOperationException("Neon Vault is not active.");
        AreaPresentationSettings presentation = Preferences.Presentation("neon-vault");
        return ModernNeonVaultSnapshot.Create(neonMachine, neonBet, neonLastSpin, neonAward,
            Profile, presentation.SoundEnabled, presentation.AnimationsEnabled,
            Preferences.Language == UiLanguagePreference.German ? "de" : "en");
    }

    internal (bool Success, string Message) ApplyNeonVault(ModernNeonVaultCommand command)
    {
        if (disposed || neonMachine is null) return (false, "Neon Vault is not active.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                    if (neonMachine.BonusActive)
                        return (false, "Finish free spins before changing the wager.");
                    decimal wager = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (wager > neonMachine.Bankroll) return (false, "Not enough play money.");
                    neonBet = wager;
                    status = "Wager selected.";
                    break;
                case "sound.set":
                case "animations.set":
                    AreaPresentationSettings area = Preferences.Presentation("neon-vault");
                    bool enabled = command.Value == "true";
                    area = command.Action == "sound.set"
                        ? area with { SoundEnabled = enabled }
                        : area with { AnimationsEnabled = enabled };
                    Preferences = Preferences.WithPresentation("neon-vault", area);
                    status = "Presentation updated.";
                    break;
                case "spin":
                    if (!neonMachine.BonusActive && neonBet > neonMachine.Bankroll)
                        return (false, "Place an affordable wager.");
                    NeonVaultSpinResult result = neonMachine.Spin(neonBet);
                    table.Session.ResetBankroll(result.BankrollAfter);
                    neonAward = new SlotProgression(progression.Service).Process(result);
                    neonLastSpin = result;
                    status = "Neon Vault spin settled.";
                    break;
                default:
                    return (false, "Unsupported Neon Vault action.");
            }
            Persist();
            return (true, status);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            return (false, ex.Message);
        }
    }

    internal void CloseNeonVault(bool interrupted = false)
    {
        if (neonMachine is null) return;
        Preferences = Preferences with { NeonVaultBet = neonBet };
        if (!interrupted)
        {
            neonMachine = null;
            neonLastSpin = null;
            neonAward = null;
        }
        Persist();
    }

    internal void OpenNineLives()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before Nine Lives.");
        AccountCompletedRound();
        if (nineMachine is null)
        {
            nineMachine = new NineLivesSession(Bankroll);
            nineBet = NineLivesRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NineLivesBet)).First();
            nineLastSpin = null;
            nineAward = null;
        }
        else
        {
            nineMachine.SynchronizeBankroll(Bankroll);
        }
        status = "Nine Lives ready.";
    }

    internal object NineLivesSnapshot()
    {
        if (nineMachine is null) throw new InvalidOperationException("Nine Lives is not active.");
        AreaPresentationSettings presentation = Preferences.Presentation("nine-lives");
        return ModernNineLivesSnapshot.Create(nineMachine, nineBet, nineLastSpin, nineAward,
            Profile, presentation.SoundEnabled, presentation.AnimationsEnabled,
            Preferences.Language == UiLanguagePreference.German ? "de" : "en");
    }

    internal (bool Success, string Message) ApplyNineLives(ModernNineLivesCommand command)
    {
        if (disposed || nineMachine is null) return (false, "Nine Lives is not active.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                    if (nineMachine.BonusActive)
                        return (false, "Finish bonus spins before changing the wager.");
                    decimal wager = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (wager > nineMachine.Bankroll) return (false, "Not enough play money.");
                    nineBet = wager;
                    status = "Wager selected.";
                    break;
                case "sound.set":
                case "animations.set":
                    AreaPresentationSettings area = Preferences.Presentation("nine-lives");
                    bool enabled = command.Value == "true";
                    area = command.Action == "sound.set"
                        ? area with { SoundEnabled = enabled }
                        : area with { AnimationsEnabled = enabled };
                    Preferences = Preferences.WithPresentation("nine-lives", area);
                    status = "Presentation updated.";
                    break;
                case "spin":
                    if (!nineMachine.BonusActive && nineBet > nineMachine.Bankroll)
                        return (false, "Place an affordable wager.");
                    NineLivesSpinResult result = nineMachine.Spin(nineBet);
                    table.Session.ResetBankroll(result.BankrollAfter);
                    nineAward = new SlotProgression(progression.Service).Process(result);
                    nineLastSpin = result;
                    status = "Nine Lives spin settled.";
                    break;
                default:
                    return (false, "Unsupported Nine Lives action.");
            }
            Persist();
            return (true, status);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            return (false, ex.Message);
        }
    }

    internal void CloseNineLives(bool interrupted = false)
    {
        if (nineMachine is null) return;
        Preferences = Preferences with { NineLivesBet = nineBet };
        if (!interrupted)
        {
            nineMachine = null;
            nineLastSpin = null;
            nineAward = null;
        }
        Persist();
    }


    internal void OpenMerge()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before Midnight Merge.");
        AccountCompletedRound();
        if (mergeBoard is null)
        {
            void SaveCandidate(MergeGameState candidate)
            {
                // MergeBoard applies its new state ONLY AFTER this durable write.
                var updated = Preferences with { MidnightMerge = candidate, Bankroll = Bankroll };
                PlayerStateStore.SaveOrThrow(updated, directory);
                Preferences = updated;
            }
            mergeBoard = new MergeBoard(Preferences.MidnightMerge, SaveCandidate);
            SaveCandidate(mergeBoard.Snapshot);
            mergeLastAction = null;
            mergeFeedback = NativeMergeText.Translate("Merge.Help", Preferences.Language);
            mergePlayerXp = mergeSeasonXp = 0;
            mergeProgressionSaveFailed = false;
        }
        SyncMergeProgression();
        status = "Midnight Merge ready.";
    }

    internal long MergeRevision => mergeBoard?.Snapshot.Revision ?? 0;

    internal object MergeSnapshot()
    {
        MergeBoard board = mergeBoard ??
            throw new InvalidOperationException("Midnight Merge is not active.");
        AreaPresentationSettings presentation = Preferences.Presentation("midnight-merge");
        return ModernMergeSnapshot.Create(board, Profile,
            presentation.SoundEnabled, presentation.AnimationsEnabled,
            mergeFeedback, mergePlayerXp, mergeSeasonXp,
            mergeProgressionSaveFailed, mergeLastAction,
            key => NativeMergeText.Translate(key, Preferences.Language), Preferences.Language);
    }

    private void SyncMergeProgression()
    {
        if (mergeBoard is null) return;
        try
        {
            MergeProfileSyncResult sync = progression.ProcessMidnightMerge(mergeBoard.Snapshot);
            if (sync.Award.BankrollEarned > 0m)
                table.Session.CreditBankroll(sync.Award.BankrollEarned);
            mergeProgressionSaveFailed = false;
            if (sync.Award.PlayerExperience > 0 || sync.Award.SeasonExperience > 0)
            {
                mergePlayerXp = sync.Award.PlayerExperience;
                mergeSeasonXp = sync.Award.SeasonExperience;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The board checkpoint survives; the idempotent Core ledger will
            // reconcile a profile save interruption when the game reopens.
            mergeProgressionSaveFailed = true;
        }
    }

    internal (bool Success, string Message, long Revision) ApplyMerge(ModernMergeCommand command)
    {
        if (disposed || mergeBoard is null)
            return (false, "Midnight Merge is not active.", MergeRevision);
        try
        {
            if (command.Action is "sound.set" or "animations.set")
            {
                AreaPresentationSettings area = Preferences.Presentation("midnight-merge");
                bool enabled = command.Value == "true";
                area = command.Action == "sound.set" ? area with { SoundEnabled = enabled }
                    : area with { AnimationsEnabled = enabled };
                Preferences = Preferences.WithPresentation("midnight-merge", area);
                Persist();
                return (true, "Presentation updated.", MergeRevision);
            }

            MergeActionResult result = command.Action switch
            {
                "spawn" => mergeBoard.TrySpawn(command.ProducerId!),
                "drop" => mergeBoard.TryDrop(Guid.Parse(command.ItemId!), command.Target!.Value),
                "deliver" => mergeBoard.TryComplete(Guid.Parse(command.OrderId!)),
                _ => new MergeActionResult(MergeOutcome.UnknownDefinition),
            };
            mergeFeedback = NativeMergeText.Translate("Merge.Feedback." + result.Outcome,
                Preferences.Language);
            if (!result.Success) return (false, mergeFeedback, MergeRevision);

            mergeLastAction = result;
            SyncMergeProgression();
            if (result.Outcome == MergeOutcome.OrderCompleted)
                mergeFeedback = string.Format(
                    Preferences.Language == UiLanguagePreference.German ? CultureInfo.GetCultureInfo("de")
                        : CultureInfo.GetCultureInfo("en"),
                    NativeMergeText.Translate("Merge.Reward", Preferences.Language), result.Reward);
            if (result.Milestones is { Count: > 0 })
            {
                MergeArea area = MergeProgression.ActiveArea(
                    MergeCatalog.Default, mergeBoard.Snapshot.RenovationPoints);
                mergeFeedback = string.Format(
                    Preferences.Language == UiLanguagePreference.German ? CultureInfo.GetCultureInfo("de")
                        : CultureInfo.GetCultureInfo("en"),
                    NativeMergeText.Translate("Merge.Milestone", Preferences.Language),
                    NativeMergeText.Translate(
                        MergeProgression.Current(area, mergeBoard.Snapshot.RenovationPoints).DisplayName,
                        Preferences.Language),
                    result.Reward);
            }

            Persist();
            return (true, mergeFeedback, MergeRevision);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            mergeFeedback = NativeMergeText.Translate("Merge.SaveFailed", Preferences.Language);
            return (false, mergeFeedback, MergeRevision);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            return (false, ex.Message, MergeRevision);
        }
    }

    internal void CloseMerge(bool interrupted = false)
    {
        if (mergeBoard is null) return;
        Persist();
        if (!interrupted)
        {
            mergeBoard = null;
            mergeLastAction = null;
        }
    }
    internal void Persist()
    {
        if (disposed) return;
        table.NormalizeBet();
        Preferences = Preferences with
        {
            Bankroll = Bankroll,
            SelectedBet = table.SelectedBet,
            PlayMode = table.PlayMode,
            AiSeatCount = table.AiSeatCount,
            TablePreset = table.CurrentPreset,
            CustomRules = table.CustomRules with { },
            Theme = table.Theme,
            SoundEnabled = table.SoundEnabled,
            AnimationsEnabled = table.AnimationsEnabled,
            SlotBet = reelsBet,
            NeonVaultBet = neonBet,
            NineLivesBet = nineBet,
            MidnightMerge = mergeBoard?.Snapshot ?? Preferences.MidnightMerge,
        };
        // This isolated preview directory is NEVER the production save folder.
        PlayerStateStore.SaveOrThrow(Preferences, directory);
        SavePreviewProfile(directory, Profile);
    }

    private static readonly JsonSerializerOptions ProfileJson = CreateProfileJson();

    private static JsonSerializerOptions CreateProfileJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static PlayerProfileState LoadPreviewProfile(string directory)
    {
        string path = Path.Combine(directory, "profile.json");
        try
        {
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<PlayerProfileState>(
                    File.ReadAllText(path), ProfileJson);
                return PlayerProfileStore.SanitizeProfile(loaded ?? new PlayerProfileState());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // The preview must not touch the installed casino's profile.
        }
        return PlayerProfileStore.SanitizeProfile(new PlayerProfileState());
    }

    private static void SavePreviewProfile(string directory, PlayerProfileState profile)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "profile.json");
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, profile, ProfileJson);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }

    public void Dispose()
    {
        if (disposed) return;
        try
        {
            if (reelsMachine is not null) CloseReels();
            if (neonMachine is not null) CloseNeonVault();
            if (nineMachine is not null) CloseNineLives();
            if (mergeBoard is not null) CloseMerge();
            AccountCompletedRound();
            Persist();
        }
        finally
        {
            disposed = true;
            progression.Dispose();
        }
    }
}
