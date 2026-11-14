using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

internal sealed record BlackjackTableActionResult(bool Success, string Message,
    bool RoundChanged = false, bool CardSound = false,
    bool SettingsChanged = false, bool TableChanged = false);

/// <summary>
/// UI-independent Blackjack session and configuration. The WebView host can be
/// torn down or recreated without re-creating the current hand or its bankroll.
/// Neither WPF widgets nor browser state owns gameplay outcomes.
/// </summary>
internal sealed class BlackjackTableController
{
    private static readonly (string Name, decimal Bet)[] AiRoster =
    [
        ("Alex", 25m), ("Morgan", 50m), ("Casey", 10m),
        ("Jordan", 25m), ("Riley", 100m),
    ];

    private readonly Func<ICardShoe>? shoeFactory;
    internal CasinoSession Session { get; private set; }
    internal PlayModePreference PlayMode { get; private set; }
    internal int AiSeatCount { get; private set; }
    internal decimal SelectedBet { get; set; }
    internal TablePresetId CurrentPreset { get; private set; }
    internal GameRules CurrentRules { get; private set; }
    internal GameRules CustomRules { get; private set; }
    internal TableThemePreference Theme { get; private set; }
    internal bool SoundEnabled { get; private set; }
    internal bool AnimationsEnabled { get; private set; }

    internal BlackjackTableController(AppPreferences preferences, Func<ICardShoe>? shoeFactory = null)
    {
        this.shoeFactory = shoeFactory;
        PlayMode = preferences.PlayMode;
        AiSeatCount = Math.Clamp(preferences.AiSeatCount, 1, AiRoster.Length);
        CurrentPreset = preferences.TablePreset;
        CustomRules = preferences.CustomRules with { };
        CurrentRules = CurrentPreset == TablePresetId.Custom
            ? CustomRules with { } : TablePresets.Get(CurrentPreset).Rules with { };
        Theme = preferences.Theme;
        SoundEnabled = preferences.SoundEnabled;
        AnimationsEnabled = preferences.AnimationsEnabled;
        Session = CreateSession(preferences.Bankroll);
        SelectedBet = Math.Min(preferences.SelectedBet, preferences.Bankroll);
        NormalizeBet();
    }

    private CasinoSession CreateSession(decimal bankroll)
    {
        IReadOnlyList<AiPlayerProfile> players = PlayMode == PlayModePreference.FullTable
            ? AiRoster.Take(AiSeatCount).Select(entry =>
                new AiPlayerProfile(entry.Name, entry.Bet)).ToArray() : [];
        return new CasinoSession(CurrentRules with { }, bankroll, shoeFactory, aiPlayers: players);
    }

    internal void RecreateConfiguredSession()
    {
        if (Session.RoundInProgress)
            throw new InvalidOperationException("Finish the hand before changing table rules.");
        Session = CreateSession(Session.Bankroll);
        NormalizeBet();
    }

    internal void NormalizeBet()
    {
        decimal ceiling = Math.Min(Session.Bankroll, CurrentRules.MaximumBet);
        SelectedBet = Math.Min(SelectedBet, ceiling);
        if (SelectedBet > 0m && SelectedBet < CurrentRules.MinimumBet
            && Session.Bankroll >= CurrentRules.MinimumBet)
            SelectedBet = CurrentRules.MinimumBet;
        if (SelectedBet == 0m && Session.Bankroll >= CurrentRules.MinimumBet)
            SelectedBet = Math.Max(CurrentRules.MinimumBet, Math.Min(25m, ceiling));
    }

    internal BlackjackTableActionResult Apply(BlackjackModernCommand command)
    {
        bool inHand = Session.RoundInProgress;
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                {
                    if (inHand) return Reject("Finish your hand before changing the bet.");
                    decimal amount = decimal.Parse(command.Value!, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    if (amount > Session.Bankroll || amount > CurrentRules.MaximumBet)
                        return Reject("The wager exceeds the available bankroll or limit.");
                    SelectedBet = amount;
                    return Accept("Bet updated.");
                }
                case "bet.clear":
                    if (inHand) return Reject("Finish your hand before clearing the bet.");
                    SelectedBet = 0m;
                    return Accept("Bet cleared.");
                case "deal":
                    if (inHand || SelectedBet < CurrentRules.MinimumBet
                        || SelectedBet > CurrentRules.MaximumBet || SelectedBet > Session.Bankroll)
                        return Reject("Place a valid bet before dealing.");
                    Session.StartRound(SelectedBet);
                    return Accept("Cards dealt.", round: true, card: true);
                case "hit":
                case "stand":
                case "double":
                case "split":
                case "surrender":
                {
                    PlayerAction action = command.Action switch
                    {
                        "hit" => PlayerAction.Hit, "stand" => PlayerAction.Stand,
                        "double" => PlayerAction.DoubleDown, "split" => PlayerAction.Split,
                        _ => PlayerAction.Surrender,
                    };
                    if (!Session.GetAvailableActions().Contains(action))
                        return Reject("This action is not available.");
                    Session.Act(action);
                    return Accept(action.ToString(), round: true,
                        card: action is PlayerAction.Hit or PlayerAction.DoubleDown or PlayerAction.Split);
                }
                case "insurance.take":
                    if (Session.CurrentRound?.Phase != RoundPhase.InsuranceDecision)
                        return Reject("No insurance decision is pending.");
                    Session.TakeInsurance();
                    return Accept("Insurance taken.", round: true);
                case "insurance.decline":
                    if (Session.CurrentRound?.Phase != RoundPhase.InsuranceDecision)
                        return Reject("No insurance decision is pending.");
                    Session.DeclineInsurance();
                    return Accept("No insurance.", round: true);
                case "mode.set":
                    if (inHand) return Reject("Finish the current hand before changing the table.");
                    PlayMode = command.Value == "full" ? PlayModePreference.FullTable : PlayModePreference.Solo;
                    RecreateConfiguredSession();
                    return Accept("Table mode updated.", table: true);
                case "seats.set":
                    if (inHand) return Reject("Finish the current hand before changing AI seats.");
                    AiSeatCount = int.Parse(command.Value!, CultureInfo.InvariantCulture);
                    RecreateConfiguredSession();
                    return Accept("AI seats updated.", table: true);
                case "preset.set":
                    if (inHand) return Reject("Finish the current hand before changing rules.");
                    CurrentPreset = Enum.Parse<TablePresetId>(command.Value!);
                    CurrentRules = CurrentPreset == TablePresetId.Custom ? CustomRules with { }
                        : TablePresets.Get(CurrentPreset).Rules with { };
                    RecreateConfiguredSession();
                    return Accept("Table rules updated.", table: true);
                case "rules.custom.set":
                    if (inHand || command.CustomRules is null)
                        return Reject("Custom rules cannot be changed during a hand.");
                    CustomRules = command.CustomRules with { };
                    CurrentRules = CustomRules with { };
                    CurrentPreset = TablePresetId.Custom;
                    RecreateConfiguredSession();
                    return Accept("Custom rules updated.", table: true);
                case "theme.set":
                    Theme = Enum.Parse<TableThemePreference>(command.Value!);
                    return Accept("Theme updated.", settings: true);
                case "sound.set":
                    SoundEnabled = command.Value == "true";
                    return Accept("Sound updated.", settings: true);
                case "animations.set":
                    AnimationsEnabled = command.Value == "true";
                    return Accept("Animations updated.", settings: true);
                case "bankroll.reset":
                    if (inHand) return Reject("Finish the current hand before resetting the bankroll.");
                    Session.ResetBankroll();
                    SelectedBet = 25m;
                    return Accept("Bankroll reset. Place a bet to begin.");
                default:
                    return Reject("Unsupported table action.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            return Reject(ex.Message);
        }
    }

    private static BlackjackTableActionResult Accept(string message, bool round = false,
        bool card = false, bool settings = false, bool table = false)
        => new(true, message, round, card, settings, table);
    private static BlackjackTableActionResult Reject(string message) => new(false, message);
}
