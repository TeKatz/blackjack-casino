using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;

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
        reelsMachine = new SlotMachineSession(Bankroll);
        reelsBet = Math.Clamp(Preferences.SlotBet, SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
        if (reelsMachine.Bankroll >= SlotMachineRules.MinimumBet)
            reelsBet = Math.Min(reelsBet, decimal.Floor(reelsMachine.Bankroll * 100) / 100);
        reelsLastSpin = null;
        reelsAward = null;
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
