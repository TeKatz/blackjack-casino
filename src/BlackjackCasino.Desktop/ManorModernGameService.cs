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
    internal bool RequiresRestart { get; private set; }

    internal ManorPreviewProtocol.Snapshot Snapshot() =>
        ManorPreviewProtocol.CreateSnapshot(
            controller.State,
            controller.Preferences.Presentation("midnight-manor").AnimationsEnabled,
            controller.Preferences.ManorBet,
            controller.SaveNotice,
            controller.Preferences.Language==UiLanguagePreference.German ? "de" : "en");

    internal ManorPreviewProtocol.Snapshot Execute(ManorPreviewProtocol.Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if(RequiresRestart)
            throw new InvalidOperationException("Saving was interrupted. Close and reopen Midnight Manor to recover the committed checkpoint.");

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
