using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Core.Empire;
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
    // Prevent two native processes from modifying the same candidate journal,
    // profile or browser database concurrently. The OS releases this handle
    // after a crash; the file itself may safely persist across sessions.
    private readonly FileStream writerLease;
    private readonly PlayerProgressionController progression;
    private readonly NativeRouletteJournal rouletteJournal;
    private readonly EmpireService empire;
    private readonly NativePlayerAggregateJournal loungeJournal;
    private NativeLoungeRuntime? lounge;
    private bool loungeRequiresRestart;
    private bool rouletteRequiresRestart;
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
    private KniffelGame? diceGame;
    private string diceNotice = "";
    private string diceReward = "";
    private readonly List<ModernDiceCelebration> diceCelebrations = [];
    private long diceEventCounter, diceRollToken;
    private int[] diceRollingIndices = [];
    private bool diceAdvancing;
    private ManorController? manorController;
    private ManorModernGameService? manorGame;
    private bool disposed;
    private string status = "Choose a game from the native shell.";

    internal AppPreferences Preferences { get; private set; }
    internal decimal Bankroll => table.Session.Bankroll;
    internal bool HandInProgress => table.Session.RoundInProgress;
    internal PlayerProfileState Profile => progression.Profile;
    internal PlayerProgressionController Progression => progression;
    internal bool LoungeRequiresRestart => loungeRequiresRestart;
    internal void MarkLoungeRecoveryRequired() => loungeRequiresRestart = true;
    internal decimal EmpireCredits => empire.Started ? empire.Credits : 0m;
    internal bool EmpireStarted => empire.Started;
    internal string Status => status;

    internal NativePreviewRuntime(string previewDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previewDirectory);
        directory = HostPreviewStorage.RequireIsolatedDirectory(previewDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Directory.CreateDirectory(directory);
        try
        {
            writerLease = new FileStream(Path.Combine(directory, ".native-host-writer.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new InvalidOperationException(
                "This NativeShell player profile is already in use. Close the other casino window first.",
                error);
        }
        try
        {
            InitializeAfterLease();
        }
        catch
        {
            writerLease.Dispose();
            throw;
        }
    }

    private void InitializeAfterLease()
    {
        // Recover any interrupted aggregate Manor journal BEFORE loading either
        // player preference or profile mirrors, exactly as the regular app does.
        new ManorPersistence(directory).Recover();
        loungeJournal = new NativePlayerAggregateJournal(directory);
        loungeJournal.Recover();
        rouletteJournal = new NativeRouletteJournal(directory);
        rouletteJournal.Recover();
        Preferences = PlayerStateStore.Load(directory);
        progression = new PlayerProgressionController(LoadPreviewProfile(directory),
            initialHistory: PlayerProfileStore.LoadHistory(directory),
            historySaver: entries => SavePreviewHistory(directory, entries));
        empire = new EmpireService(Preferences.Empire);
        empire.SetClaimedAchievements(Profile.ClaimedChallengeIds);
        empire.SetActivitySnapshot(EmpireActivitySnapshot.FromProfile(Profile));
        table = new BlackjackTableController(Preferences);
        reelsBet = Math.Clamp(Preferences.SlotBet, SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
        neonBet = NeonVaultRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NeonVaultBet)).First();
        nineBet = NineLivesRules.Bets.OrderBy(bet => Math.Abs(bet - Preferences.NineLivesBet)).First();
    }

    internal void OpenLounge()
    {
        if (disposed || HandInProgress || ManorRequiresRestart
            || rouletteRequiresRestart || loungeRequiresRestart)
            throw new InvalidOperationException("Finish the active game or recover the save first.");
        lounge ??= new NativeLoungeRuntime(this);
    }

    internal void CloseLounge()
    {
        if (lounge is null) return;
        try { lounge.Close(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException)
        {
            loungeRequiresRestart = true;
            status = "Lounge save interrupted: " + ex.Message;
        }
        finally { lounge = null; }
    }

    internal void OpenLoungeGame(string gameId) =>
        (lounge ?? throw new InvalidOperationException("Lounge not started.")).Open(gameId);

    internal object LoungeSnapshot(bool boardOnly = false) =>
        (lounge ?? throw new InvalidOperationException("Lounge not started.")).Snapshot(boardOnly);

    internal NativeLoungeReply? ApplyLounge(string type, string json, bool boardOnly = false) =>
        lounge?.Execute(type, json, boardOnly);

    internal bool LoungeCanOpenCaseBoard => lounge?.CanOpenBoard ?? false;
    internal void SetLoungeBoardWindow(bool open) => lounge?.SetBoardWindow(open);

    internal bool TickLounge() => lounge?.Tick() ?? false;
    internal bool TickLoungeTower() => lounge?.TickTower() ?? false;

    internal void UpdateLoungePreferences(Func<AppPreferences, AppPreferences> change)
    {
        if (disposed || loungeRequiresRestart) throw new InvalidOperationException("Lounge recovery required.");
        AppPreferences previous = Preferences;
        try
        {
            Preferences = change(Preferences);
            Persist();
        }
        catch
        {
            Preferences = previous;
            loungeRequiresRestart = true;
            throw;
        }
    }

    internal void SaveLoungeProfile()
    {
        if (disposed || loungeRequiresRestart) throw new InvalidOperationException("Lounge recovery required.");
        try
        {
            loungeJournal.Commit(Preferences with { Bankroll = Bankroll }, Profile);
        }
        catch
        {
            loungeRequiresRestart = true;
            throw;
        }
    }

    internal void CommitLobbyAggregate(AppPreferences preferences, PlayerProfileState profile)
    {
        if (disposed || ManorRequiresRestart || rouletteRequiresRestart || loungeRequiresRestart)
            throw new InvalidOperationException("Player transaction recovery requires restart.");
        if (preferences.Bankroll < 0m)
            throw new ArgumentOutOfRangeException(nameof(preferences));
        try
        {
            loungeJournal.Commit(preferences, profile);
            progression.AdoptManorProfile(profile);
            table.Session.ResetBankroll(preferences.Bankroll);
            Preferences = preferences;
        }
        catch
        {
            loungeRequiresRestart = true;
            throw;
        }
    }

    internal void CreditLoungeWallet(decimal amount)
    {
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        AppPreferences candidate = Preferences with { Bankroll = checked(Bankroll + amount) };
        CommitLobbyAggregate(candidate, Profile);
    }

    internal void SaveLoungeAward(ProgressionAward award)
    {
        if (award == ProgressionAward.None) return;
        if (disposed || loungeRequiresRestart) throw new InvalidOperationException("Lounge recovery required.");
        try
        {
            decimal newBankroll = checked(Bankroll + award.BankrollEarned);
            AppPreferences candidate = Preferences with { Bankroll = newBankroll };
            // Wallet and all reward receipt IDs live in the SAME durable aggregate.
            loungeJournal.Commit(candidate, Profile);
            table.Session.ResetBankroll(newBankroll);
            Preferences = candidate;
        }
        catch
        {
            loungeRequiresRestart = true;
            throw;
        }
    }

    internal void OpenEmpire()
    {
        if (disposed || HandInProgress || ManorRequiresRestart || rouletteRequiresRestart)
            throw new InvalidOperationException("Finish the active game before opening Empire.");
        empire.SetClaimedAchievements(Profile.ClaimedChallengeIds);
        empire.SetActivitySnapshot(EmpireActivitySnapshot.FromProfile(Profile));
        empire.Start(DateTimeOffset.UtcNow);
        empire.Accrue(DateTimeOffset.UtcNow);
        Persist();
        status = "Casino Empire management ready.";
    }

    internal void CloseEmpire()
    {
        if (disposed || !empire.Started) return;
        empire.Accrue(DateTimeOffset.UtcNow);
        Persist();
    }

    internal object EmpireSnapshot(bool detached = false) => new
    {
        Language = Preferences.Language == UiLanguagePreference.German ? "de" : "en",
        AnimationsEnabled = Preferences.Presentation("casino-empire").AnimationsEnabled,
        SoundEnabled = Preferences.Presentation("casino-empire").SoundEnabled,
        Detached = detached,
        CanClaimDividend = !HandInProgress && !ManorRequiresRestart && !rouletteRequiresRestart,
        Economy = EmpireModernSnapshot.Create(empire),
    };

    internal object EmpirePreview(EmpireModernCommand command) =>
        EmpireModernSnapshot.Preview(empire.Preview(
            command.InspectRoomId ?? command.RoomId
                ?? EmpireCatalog.Find(command.Id ?? "")?.Id ?? "slots",
            (candidate, at) => EmpireModernProtocol.Apply(candidate, command, at)));

    internal EmpireActionResult CommitEmpire(EmpireModernCommand command)
    {
        if (disposed || ManorRequiresRestart || rouletteRequiresRestart || HandInProgress)
            return new(false, "Finish the active game or recover the save before managing Empire.");
        try
        {
            EmpireActionResult result;
            if (command.Action == "dividend")
            {
                decimal paid = empire.ClaimDividend(DateTimeOffset.UtcNow, (state, cash) =>
                {
                    var candidate = Preferences with
                    {
                        Empire = state,
                        Bankroll = checked(Bankroll + cash),
                    };
                    // A single durable snapshot owns both the owner reserve and cash.
                    PlayerStateStore.SaveOrThrow(candidate, directory);
                    Preferences = candidate;
                    table.Session.ResetBankroll(candidate.Bankroll);
                });
                result = paid > 0m
                    ? new(true, $"Owner dividend: {paid:C2} added to casino cash.")
                    : new(false, "No owner dividend is available.");
            }
            else
            {
                result = empire.Transact(candidate =>
                {
                    candidate.ObserveActivity(
                        EmpireActivitySnapshot.FromProfile(Profile), DateTimeOffset.UtcNow);
                    return EmpireModernProtocol.Apply(candidate, command, DateTimeOffset.UtcNow);
                }, state =>
                {
                    var candidate = Preferences with { Empire = state, Bankroll = Bankroll };
                    PlayerStateStore.SaveOrThrow(candidate, directory);
                    Preferences = candidate;
                });
            }
            status = result.Message;
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException
            or InvalidOperationException)
        {
            status = "Empire change was not saved. Verify the stored state and retry.";
            return new(false, status + " " + ex.Message);
        }
    }

    internal void TickEmpire()
    {
        if (disposed || !empire.Started || ManorRequiresRestart || rouletteRequiresRestart)
            return;
        empire.Accrue(DateTimeOffset.UtcNow);
    }

    internal bool RouletteRequiresRestart => rouletteRequiresRestart;

    internal object LobbySnapshot() => NativeLobbySnapshot.Create(this);

    internal void SetLobbyLanguage(UiLanguagePreference language)
    {
        if (disposed || rouletteRequiresRestart || ManorRequiresRestart) return;
        Preferences = Preferences with { Language = language };
        Persist();
    }

    internal object RouletteSnapshot()
    {
        AreaPresentationSettings area = Preferences.Presentation("roulette");
        return new
        {
            Bankroll = Bankroll.ToString("C2", CultureInfo.GetCultureInfo("en-US")),
            BankrollValue = Bankroll,
            Language = Preferences.Language == UiLanguagePreference.German ? "de" : "en",
            Motion = area.AnimationsEnabled,
            Sound = area.SoundEnabled,
        };
    }

    internal object ApplyRoulette(Guid roundId, JsonElement rawBets)
    {
        object Reject(string message) => new { RoundId = roundId, Success = false, Message = message };
        if (disposed || rouletteRequiresRestart || ManorRequiresRestart)
            return Reject("Preview save recovery requires restart.");
        if (roundId == Guid.Empty) return Reject("Invalid round ID.");
        if (rouletteJournal.TryReceipt(roundId, out NativeRouletteReceipt? replay))
            return replay!;
        if (HandInProgress || rawBets.ValueKind != JsonValueKind.Array
            || rawBets.GetArrayLength() is < 1 or > 100)
            return Reject("Finish the current game or use valid roulette bets.");

        var bets = new List<RouletteBet>();
        foreach (JsonElement bet in rawBets.EnumerateArray())
        {
            if (bet.ValueKind != JsonValueKind.Object
                || !bet.TryGetProperty("key", out JsonElement key)
                || key.ValueKind != JsonValueKind.String
                || !bet.TryGetProperty("amount", out JsonElement amount)
                || amount.ValueKind != JsonValueKind.Number
                || !amount.TryGetDecimal(out decimal stake))
                return Reject("Invalid roulette bet.");
            bets.Add(new RouletteBet(key.GetString()!, stake));
        }

        RouletteResult result;
        try { result = Roulette.Settle(bets, Bankroll, Roulette.Draw()); }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { return Reject(ex.Message); }

        AppPreferences candidate = Preferences with { Bankroll = result.Bankroll };
        try
        {
            NativeRouletteReceipt receipt = rouletteJournal.Commit(roundId, result, candidate);
            table.Session.ResetBankroll(result.Bankroll);
            table.NormalizeBet();
            Preferences = candidate;
            status = "Roulette settlement committed.";
            Persist();
            return receipt;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or InvalidDataException or InvalidOperationException)
        {
            rouletteRequiresRestart = true;
            status = "Roulette checkpoint uncertain. Restart this preview before another wager.";
            return Reject(status + " " + ex.Message);
        }
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
    internal bool DiceCasinoMode =>
        Preferences.KniffelCasinoSelected && Preferences.KniffelTable is not null;

    internal bool DiceWaiting =>
        DiceCasinoMode && Preferences.KniffelTable is { Settled: false } table
        && KniffelTable.NeedsOpponents(table);

    internal void OpenDice()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before Midnight Dice.");
        AccountCompletedRound();
        if (diceGame is null)
        {
            KniffelState saved = DiceCasinoMode ? Preferences.KniffelTable!.Player : Preferences.Kniffel;
            KniffelState normalized = KniffelGame.Normalize(saved);
            diceGame = KniffelGame.Restore(normalized);
            if (!DiceCasinoMode)
                SaveDicePreferences(Preferences with { Kniffel = normalized, Bankroll = Bankroll });
            diceNotice = diceGame.IsComplete
                ? $"Game complete · {diceGame.Total} points."
                : "Roll the dice, hold what you want, and record your scores. Progress saves automatically.";
            diceReward = DiceCasinoMode && Preferences.KniffelTable is { Settled: true } completed
                ? RecordDiceTable(completed)
                : !DiceCasinoMode && diceGame.IsComplete
                    ? RecordDiceCompletion(diceGame.Snapshot) : "";
        }
        status = "Midnight Dice ready.";
    }

    internal object DiceSnapshot()
    {
        KniffelGame game = diceGame ?? throw new InvalidOperationException("Midnight Dice is not active.");
        string variant = CosmeticCatalog.Get(Profile.EquippedDiceSetId
            ?? CosmeticCatalog.DefaultDiceSetId).VisualVariant;
        return ModernDiceSnapshot.Create(game, Preferences.Kniffel,
            Preferences.KniffelTable, Preferences.KniffelCasinoSelected,
            Bankroll, Profile, variant, Preferences.Presentation("kniffel"),
            diceNotice, diceReward, DiceWaiting, diceCelebrations, diceRollToken,
            diceRollingIndices, Preferences.Language);
    }

    private void SaveDicePreferences(AppPreferences next)
    {
        PlayerStateStore.SaveOrThrow(next, directory);
        Preferences = next;
    }

    private void CommitDiceCash(KniffelCashChange change)
    {
        SaveDicePreferences(Preferences with
        {
            Bankroll = change.Bankroll,
            KniffelTable = change.Table,
            KniffelCasinoSelected = true,
        });
        table.Session.ResetBankroll(change.Bankroll);
    }

    private string RecordDiceTable(KniffelTableState checkpoint)
    {
        ProgressionAward reward = progression.ProcessKniffelTable(checkpoint);
        Persist();
        return reward.PlayerExperience > 0
            ? $"+{reward.PlayerExperience} XP · +{reward.SeasonExperience} Season XP."
            : checkpoint.Forfeited
                ? "Forfeited tables do not award completion XP."
                : "Completion rewards already credited.";
    }

    private string RecordDiceCompletion(KniffelState checkpoint)
    {
        ProgressionAward reward = progression.ProcessKniffel(checkpoint);
        Persist();
        return reward.PlayerExperience > 0
            ? $"+{reward.PlayerExperience} XP · +{reward.SeasonExperience} Season XP."
            : "Completion rewards already credited.";
    }

    internal (bool Success, string Message) ApplyDice(ModernDiceCommand command)
    {
        if (disposed || diceGame is null)
            return (false, "Midnight Dice is not active.");
        try
        {
            switch (command.Action)
            {
                case "sound.set":
                case "animations.set":
                case "music.set":
                {
                    AreaPresentationSettings settings = Preferences.Presentation("kniffel");
                    bool enabled = command.Value == "true";
                    settings = command.Action switch
                    {
                        "sound.set" => settings with { SoundEnabled = enabled },
                        "animations.set" => settings with { AnimationsEnabled = enabled },
                        _ => settings with { MusicEnabled = enabled },
                    };
                    Preferences = Preferences.WithPresentation("kniffel", settings);
                    Persist();
                    status = "Presentation settings saved.";
                    return (true, status);
                }
                case "casual.start":
                case "casual.resume":
                {
                    KniffelState casual = command.Action == "casual.start"
                        ? new KniffelGame().Snapshot : KniffelGame.Normalize(Preferences.Kniffel);
                    SaveDicePreferences(Preferences with
                    {
                        KniffelCasinoSelected = false, Kniffel = casual, Bankroll = Bankroll,
                    });
                    diceGame = KniffelGame.Restore(casual);
                    diceCelebrations.Clear();
                    diceRollToken = 0;
                    diceRollingIndices = [];
                    diceReward = diceGame.IsComplete
                        ? RecordDiceCompletion(diceGame.Snapshot) : "";
                    diceNotice = "Casual game loaded. No entry fee.";
                    status = "Casual dice table ready.";
                    return (true, status);
                }
                case "casino.start":
                {
                    if (DiceWaiting || diceAdvancing)
                        return (false, "Wait for the opponents to finish.");
                    if (Preferences.KniffelTable is { Settled: true } previous)
                        _ = progression.ProcessKniffelTable(previous);
                    decimal stake = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    KniffelCash.Start(Bankroll, Preferences.KniffelTable, stake, CommitDiceCash);
                    diceGame = KniffelGame.Restore(Preferences.KniffelTable!.Player);
                    diceCelebrations.Clear();
                    diceRollToken = 0;
                    diceRollingIndices = [];
                    diceNotice = "Casino table joined. Entry paid and saved.";
                    diceReward = "";
                    Persist();
                    status = "Casino entry committed once.";
                    return (true, status);
                }
                case "casino.resume":
                {
                    if (Preferences.KniffelTable is not { } saved)
                        return (false, "No paid table is saved.");
                    SaveDicePreferences(Preferences with
                    {
                        KniffelCasinoSelected = true, Bankroll = Bankroll,
                    });
                    diceGame = KniffelGame.Restore(saved.Player);
                    diceCelebrations.Clear();
                    diceRollingIndices = [];
                    diceNotice = saved.Settled
                        ? "Previous casino table restored."
                        : "Casino table resumed without another entry fee.";
                    diceReward = saved.Settled ? RecordDiceTable(saved) : "";
                    status = "Paid table restored.";
                    return (true, status);
                }
                case "casino.forfeit":
                {
                    if (!DiceCasinoMode || Preferences.KniffelTable is not { Settled: false } current
                        || DiceWaiting || diceAdvancing)
                        return (false, "This casino table cannot be forfeited now.");
                    KniffelCash.Apply(Bankroll, current, KniffelTable.Forfeit(current), CommitDiceCash);
                    diceReward = RecordDiceTable(Preferences.KniffelTable!);
                    diceNotice = "Table forfeited. The entry fee remains spent.";
                    status = "Casino forfeit persisted.";
                    return (true, status);
                }
                case "roll":
                case "hold":
                case "score":
                case "scratch":
                    return ApplyDiceTurn(command);
                default:
                    return (false, "Unsupported dice action.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or OverflowException or FormatException)
        {
            diceNotice = ex is IOException or UnauthorizedAccessException
                ? "Could not save. No dice operation was applied; try again."
                : ex.Message;
            return (false, diceNotice);
        }
    }

    private (bool Success, string Message) ApplyDiceTurn(ModernDiceCommand command)
    {
        if (diceGame is null || diceAdvancing || DiceWaiting
            || DiceCasinoMode && Preferences.KniffelTable is { Settled: true })
            return (false, "Wait for the current table before playing.");

        KniffelState before = diceGame.Snapshot;
        KniffelGame candidate = KniffelGame.Restore(before);
        int points = 0;
        KniffelCategory category = default;
        switch (command.Action)
        {
            case "roll": candidate.Roll(); break;
            case "hold": candidate.ToggleHold(command.Index!.Value); break;
            case "score":
            case "scratch":
                category = Enum.Parse<KniffelCategory>(command.Category!);
                points = candidate.Commit(category, command.Action == "scratch");
                break;
            default:
                return (false, "Unsupported dice action.");
        }
        KniffelState after = candidate.Snapshot;
        if (DiceCasinoMode)
        {
            KniffelTableState tableState = Preferences.KniffelTable!;
            KniffelTableState updated = KniffelTable.WithPlayer(tableState, after);
            SaveDicePreferences(Preferences with { KniffelTable = updated, Bankroll = Bankroll });
        }
        else
        {
            SaveDicePreferences(Preferences with { Kniffel = after, Bankroll = Bankroll });
        }
        diceGame = candidate;

        switch (command.Action)
        {
            case "roll":
                diceRollToken++;
                diceRollingIndices = Enumerable.Range(0, 5).Where(i => !before.Held[i]).ToArray();
                diceNotice = candidate.IsAdditionalKniffel
                    ? "Additional five of a kind! Commit an open box for +50 bonus."
                    : candidate.RollsUsed == 3
                        ? "Third roll: choose a box and record its score."
                        : "Hold dice, roll again or record a score.";
                break;
            case "hold":
                diceNotice = $"Die {command.Index!.Value + 1} " +
                    (after.Held[command.Index.Value] ? "held." : "released.");
                break;
            default:
                diceNotice = $"{KniffelCategoryLabels.For(category)}: {points} points recorded."
                    + (candidate.IsComplete
                        ? $" Sheet complete with {candidate.Total} points."
                        : $" Turn {candidate.FilledCount + 1} next.");
                AddDiceCelebration("You", before, after);
                if (candidate.IsComplete)
                    diceReward = DiceCasinoMode
                        ? "Opponents are finishing the table."
                        : RecordDiceCompletion(after);
                break;
        }
        status = "Dice state saved.";
        return (true, status);
    }

    private void AddDiceCelebration(string player, KniffelState before, KniffelState after)
    {
        if (after.KniffelsScored <= before.KniffelsScored) return;
        KniffelEntry? entry = after.Entries.FirstOrDefault(item =>
            before.Entries.All(previous => previous.Category != item.Category));
        if (entry is null) return;
        int bonus = (Math.Max(0, after.KniffelsScored - 1) -
            Math.Max(0, before.KniffelsScored - 1)) * 50;
        diceCelebrations.Add(new(++diceEventCounter, player, entry.Category.ToString(),
            entry.Points, bonus, before.RollsUsed > 0 ? before.Dice[0] : 0));
        if (diceCelebrations.Count > 24) diceCelebrations.RemoveAt(0);
    }

    internal async Task<bool> AdvanceDiceOpponentsAsync()
    {
        if (disposed || diceAdvancing || !DiceWaiting || diceGame is null
            || HandInProgress || Preferences.KniffelTable is not { } snapshot)
            return false;
        diceAdvancing = true;
        try
        {
            KniffelTableState next = await Task.Run(() => KniffelTable.Advance(snapshot));
            KniffelTableState? current = Preferences.KniffelTable;
            if (disposed || !DiceCasinoMode || HandInProgress || current is null
                || current.Player.GameId != snapshot.Player.GameId || current.Settled)
                return false;

            KniffelCash.Apply(Bankroll, current, next, CommitDiceCash);
            for (int i = 0; i < 3; i++)
                AddDiceCelebration(KniffelTable.Names[i],
                    snapshot.Opponents[i], next.Opponents[i]);
            diceReward = next.Settled ? RecordDiceTable(next) : "";
            diceNotice = next.Settled
                ? $"Casino match complete · Place #{next.Placement}. Prize {next.Payout:N2}."
                : string.Join(" · ", next.LastMoves) + ". Your turn.";
            status = diceNotice;
            Persist();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidOperationException or OverflowException)
        {
            diceNotice = "Opponents could not be saved. " + ex.Message;
            return false;
        }
        finally
        {
            diceAdvancing = false;
        }
    }

    internal void CloseDice()
    {
        if (diceGame is null) return;
        Persist();
        diceGame = null;
        diceCelebrations.Clear();
        diceRollingIndices = [];
    }

    internal bool ManorRequiresRestart => manorGame?.RequiresRestart == true;

    internal void OpenManor()
    {
        if (disposed || HandInProgress)
            throw new InvalidOperationException("Finish the Blackjack hand before Midnight Manor.");
        AccountCompletedRound();
        if (manorGame?.RequiresRestart == true)
            throw new InvalidOperationException(
                "Manor journal recovery requires restarting the native shell.");
        if (manorController is null)
        {
            manorController = new ManorController(Preferences, Bankroll, progression, committed =>
            {
                // The Manor journal holds the wallet and profile together before
                // either native mirror or visible bankroll can be replaced.
                table.Session.ResetBankroll(committed.Bankroll);
                Preferences = committed;
            }, new ManorPersistence(directory));
            manorGame = new ManorModernGameService(manorController);
        }
        status = "Midnight Manor ready.";
    }

    internal ManorPreviewProtocol.Snapshot ManorSnapshot()
        => manorGame?.Snapshot()
            ?? throw new InvalidOperationException("Midnight Manor is not active.");

    internal ManorPreviewProtocol.Snapshot ApplyManor(ManorPreviewProtocol.Command command)
    {
        if (disposed || manorGame is null)
            throw new InvalidOperationException("Midnight Manor is not active.");
        ManorPreviewProtocol.Snapshot reply = manorGame.Execute(command);
        status = "Midnight Manor checkpoint committed.";
        return reply;
    }

    internal void CloseManor(bool interrupted = false)
    {
        if (manorController is null) return;
        // Do not override a potentially newer committed Manor journal with
        // a stale ordinary player-state checkpoint when saving was uncertain.
        if (manorGame?.RequiresRestart == true) return;
        Preferences = manorController.Preferences;
        if (!interrupted)
        {
            manorController = null;
            manorGame = null;
        }
        Persist();
    }

    internal void Persist()
    {
        // A possibly committed Manor aggregate journal is authoritative. Do
        // not overwrite either mirror with stale in-memory state before restart.
        if (disposed || ManorRequiresRestart || rouletteRequiresRestart || loungeRequiresRestart) return;
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
            Empire = empire.Snapshot,
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

    private static void SavePreviewHistory(string directory, IReadOnlyList<RoundHistoryEntry> history)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "hand-history.json");
        string temp = path + ".native.tmp";
        RoundHistoryEntry[] snapshot = PlayerProfileStore.CreateHistorySnapshot(history);
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, snapshot, ProfileJson);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
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
            if (diceGame is not null) CloseDice();
            if (manorController is not null) CloseManor();
            if (lounge is not null) CloseLounge();
            if (empire.Started && !ManorRequiresRestart && !rouletteRequiresRestart)
                empire.Accrue(DateTimeOffset.UtcNow);
            if (!ManorRequiresRestart && !rouletteRequiresRestart && !loungeRequiresRestart)
            {
                AccountCompletedRound();
                Persist();
            }
        }
        finally
        {
            disposed = true;
            try { progression.Dispose(); }
            finally { writerLease.Dispose(); }
        }
    }
}
