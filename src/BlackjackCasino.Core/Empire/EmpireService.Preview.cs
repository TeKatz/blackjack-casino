namespace BlackjackCasino.Core.Empire;

/// <summary>The strategic bonuses add together before prestige and achievements multiply them.</summary>
public sealed record EmpireProductionBreakdown(decimal Base, decimal Prestige, decimal Achievements,
    decimal Strategy, decimal Synergies, decimal Projects, decimal Managers, decimal Events, decimal Contracts,
    decimal StrategicBonus, decimal Final);

public sealed record EmpireActionPreview(bool Success, string Message, decimal BudgetBefore, decimal BudgetAfter,
    decimal PortfolioBefore, decimal PortfolioAfter, int LevelBefore, int LevelAfter,
    EmpireProductionBreakdown RoomBefore, EmpireProductionBreakdown RoomAfter,
    IReadOnlyList<EmpireSynergyStatus> SynergiesAfter, EmpireAutomationState AutomationAfter);

public sealed partial class EmpireService
{
    public EmpireProductionBreakdown ProductionBreakdown(string roomId)
        => BreakdownFor(Estate, ActivePropertyId, EmpireCatalog.Find(roomId), state.LastAccruedAt);

    /// <summary>
    /// Runs the real action on an isolated snapshot at the simulation's current instant.
    /// No accrual, wallet callback, persistence, or live-state mutation is involved.
    /// Achievements and activity are carried over, including effects on other rooms.
    /// </summary>
    public EmpireActionPreview Preview(string roomId, Func<EmpireService, DateTimeOffset, EmpireActionResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var candidate = new EmpireService(Snapshot);
        candidate.SetClaimedAchievements(claimedAchievements);
        candidate.activity = activity;
        decimal budget = Credits, portfolio = TotalCreditsPerSecond;
        int level = Level(roomId);
        EmpireProductionBreakdown before = ProductionBreakdown(roomId);
        EmpireActionResult result = action(candidate, state.LastAccruedAt);
        // Failed actions sometimes accrue before validating. Never expose their intermediate state.
        EmpireService after = result.Success ? candidate : this;
        return new(result.Success, result.Message, budget, after.Credits, portfolio, after.TotalCreditsPerSecond,
            level, after.Level(roomId), before, after.ProductionBreakdown(roomId), after.Synergies, after.Automation);
    }

    public EmpireActionPreview PreviewUpgrade(string roomId, int levels) => Preview(roomId, (candidate, at) =>
        new(candidate.UpgradeMany(roomId, levels, at), "Room upgrade"));
}
