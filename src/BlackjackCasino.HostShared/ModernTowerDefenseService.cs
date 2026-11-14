using BlackjackCasino.Core.TowerDefense;

namespace BlackjackCasino.HostShared;

/// <summary>Native owner of all Tower Defense rules and saves; React is presentation only.</summary>
internal sealed class ModernTowerDefenseService
{
    private TowerDefenseGame game;
    private readonly Action<TdState> persist;
    private int stepsSinceSave;

    internal ModernTowerDefenseService(TdState? saved, Action<TdState> persist)
    {
        this.persist = persist;
        game = new TowerDefenseGame(saved);
    }

    internal object Snapshot() => new
    {
        Screen = game.State.Screen,
        MapPath = TdMap.PathFor(game.State.Stage), Pads = TdMap.PadsFor(game.State.Stage),
        MaxWaves = TdMastery.StageWaves(game.State.Stage),
        State = game.State.Copy(),
    };

    internal (bool Success, string Message) Execute(ModernTowerDefenseCommand command)
    {
        TdState before = game.Save();
        var result = game.Execute(command.Action, command.Pad, command.Kind);
        if (!result.Success) return result;
        try
        {
            persist(game.Save());
            stepsSinceSave = 0;
            return result;
        }
        catch
        {
            game = new TowerDefenseGame(before);
            throw;
        }
    }

    internal void Checkpoint() => persist(game.Save());

    internal bool Tick()
    {
        if (game.State.Phase != "running" || game.State.Paused) return false;
        string oldPhase = game.State.Phase;
        if (!game.Tick()) return false;
        if (++stepsSinceSave >= 20 || game.State.Phase != oldPhase)
        {
            persist(game.Save());
            stepsSinceSave = 0;
        }
        return true;
    }
}
