using System.IO;
using System.Text.Json;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

/// <summary>
/// The only gameplay-command entry point for the modern frontend. All transitions are
/// executed by the existing C# Manor engine and journal. No frontend result or balance
/// is trusted, and an uncertain disk commit locks this window until reopened/recovered.
/// </summary>
internal sealed class ManorModernGameService(ManorController controller)
{
    // Isolated practice uses the EXACT same engine as a wagered run. It is
    // ephemeral and never passed to ManorController.Execute or the journal.
    private ManorRunState? practice;
    private readonly HashSet<string> appliedPracticeIds=new(StringComparer.Ordinal);
    private int practiceStarts;
    internal bool RequiresRestart { get; private set; }

    internal ManorPreviewProtocol.Snapshot Snapshot() =>
        ManorPreviewProtocol.CreateSnapshot(
            controller.State,
            controller.Preferences.Presentation("midnight-manor").AnimationsEnabled,
            controller.Preferences.ManorBet,
            controller.SaveNotice,
            controller.Preferences.Language==UiLanguagePreference.German ? "de" : "en",
            controller.Preferences.SoundEnabled,
            controller.Preferences.MusicEnabled,
            controller.Preferences.MusicVolume,
            practice);

    internal ManorPreviewProtocol.Snapshot Execute(ManorPreviewProtocol.Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if(RequiresRestart)
            throw new InvalidOperationException("Saving was interrupted. Close and reopen Midnight Manor to recover the committed checkpoint.");

        if(command.Type.StartsWith("practice.",StringComparison.Ordinal))
            return ExecutePractice(command);
        if(practice is not null)
            throw new InvalidOperationException("Leave the non-wager practice before starting real play.");

        if(command.ExpectedRevision is not long expected)
            throw new ArgumentException("A state revision is required.",nameof(command));
        if(controller.State.LastModernCommandId==command.RequestId)
            return Snapshot(); // Lost response to the most recently committed command.
        if(controller.State.Revision!=expected)
            throw new InvalidOperationException("Game state changed. The current state has been refreshed; review it before acting again.");

        AppPreferences? nextPreferences=command.Type=="spin.paid"
            ? controller.Preferences with { ManorBet=command.Bet ?? throw new ArgumentException("Missing bet.") }
            : null;

        try
        {
            controller.Execute(machine =>
            {
                Apply(machine,command);
                // Recorded atomically with wallet/profile/feature and revision in the checkpoint.
                machine.State.LastModernCommandId=command.RequestId;
            },nextPreferences);
            return Snapshot();
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            // The durable journal may already contain the action. Never retry it against
            // stale memory. Reopen to recover the authoritative checkpoint.
            RequiresRestart=true;
            throw;
        }
    }

    private ManorPreviewProtocol.Snapshot ExecutePractice(ManorPreviewProtocol.Command command)
    {
        // A practice request never has access to a bankroll, paytable spin,
        // persistence, progression or reward settlement. It shares C# game
        // rules but cannot ever be used to mint play money.
        if(command.ExpectedRevision!=controller.State.Revision)
            throw new InvalidOperationException("Game state changed. Refresh before practising.");
        if(controller.State.CycleActive || controller.State.Run is { Status:ManorRunStatus.Hunting })
            throw new InvalidOperationException("Finish the saved Manor cycle before entering practice.");
        if(appliedPracticeIds.Contains(command.RequestId)) return Snapshot();

        if(command.Type is "practice.start" or "practice.restart")
        {
            practice=ManorRun.Create(3,(5+practiceStarts++)%16).State;
        }
        else if(command.Type=="practice.exit")
        {
            practice=null;
        }
        else
        {
            if(practice is not { Status:ManorRunStatus.Hunting })
                throw new InvalidOperationException("There is no active practice run.");
            var run=new ManorRun(practice);
            switch(command.Type)
            {
                case "practice.move":
                    run.Move(command.Room ?? throw new ArgumentException("Missing practice destination."));
                    break;
                case "practice.candle":
                    run.UseCandle();
                    break;
                case "practice.follow":
                    run.FollowPolicy();
                    break;
                case "practice.cashout":
                    run.CashOut();
                    break;
                case "practice.auto":
                    run.AutoResolve();
                    break;
                default:
                    throw new InvalidOperationException("Unknown practice action.");
            }
        }
        appliedPracticeIds.Add(command.RequestId);
        return Snapshot();
    }

    internal static void Apply(ManorSession machine,ManorPreviewProtocol.Command command)
    {
        switch(command.Type)
        {
            case "spin.paid":
                machine.Start(command.Bet ?? throw new ArgumentException("Missing bet."));
                break;
            case "spin.free":
                machine.SpinFree();
                break;
            case "run.move":
                machine.MarkInteractive();
                machine.Move(command.Room ?? throw new ArgumentException("Missing destination."));
                break;
            case "run.candle":
                machine.MarkInteractive();
                machine.UseCandle();
                break;
            case "run.cashout":
                machine.MarkInteractive();
                machine.CashOut();
                break;
            case "run.follow":
                machine.MarkInteractive();
                machine.FollowPolicy();
                break;
            case "run.auto":
                machine.ResolveAutomatically();
                break;
            default:
                throw new InvalidOperationException("Unknown or read-only command.");
        }
    }
}
