using BlackjackCasino.Core.Minesweeper;

namespace BlackjackCasino.Desktop;

/// <summary>Transaction boundary for Midnight Sweep; React only receives masked cells.</summary>
internal sealed class ModernMinesweeperService
{
    private MinesweeperGame game;
    private readonly Action<SweepProgress> persist;
    private int ticks;
    internal ModernMinesweeperService(SweepProgress? saved, Action<SweepProgress> persist)
    {
        this.persist = persist;
        game = new MinesweeperGame(saved);
        // Finish a previously persisted terminal run whose profile receipt was interrupted.
        if (game.State.Run.Finished) persist(game.Save());
    }
    internal object Snapshot() => game.Snapshot();
    internal (bool Success, string Message) Execute(ModernMinesweeperCommand command)
    {
        SweepProgress before = game.Save();
        var result = game.Execute(command.Action, command.Index, command.Difficulty);
        if (!result.Success) return result;
        try
        {
            persist(game.Save());
            ticks = 0;
            return result;
        }
        catch
        {
            game = new MinesweeperGame(before);
            throw;
        }
    }
    internal bool Tick()
    {
        if (!game.Tick()) return false;
        if (++ticks >= 10)
        {
            persist(game.Save());
            ticks = 0;
        }
        return true;
    }
    internal void Checkpoint() => persist(game.Save());
}
