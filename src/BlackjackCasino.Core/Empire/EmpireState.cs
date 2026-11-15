namespace BlackjackCasino.Core.Empire;

/// <summary>Version 4 is an intentional fresh start, not a projection of the old idle economy.</summary>
public sealed record EmpireState
{
    public const int CurrentSchemaVersion = 4;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public bool Started { get; init; }
    public decimal Credits { get; init; }
    public decimal LifetimeCredits { get; init; }
    public decimal RunCredits { get; init; }
    public int Prestige { get; init; }
    public long Clicks { get; init; }
    public DateTimeOffset LastAccruedAt { get; init; }
    public Dictionary<string, int> Buildings { get; init; } = new(StringComparer.Ordinal);

    public static EmpireState Normalize(EmpireState? source)
    {
        // Old saves are deliberately not coerced into a radically different economy.
        // The old JSON is backed up by PlayerStateStore before this state is persisted.
        if (source is null || source.SchemaVersion != CurrentSchemaVersion) return new();
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (EmpireBuilding building in EmpireCatalog.Buildings)
        {
            int level = Math.Clamp(source.Buildings?.GetValueOrDefault(building.Id) ?? 0, 0, EmpireCatalog.MaximumLevel);
            levels[building.Id] = level;
        }
        // Repairs malformed or externally edited saves without opening locked areas.
        foreach (EmpireBuilding building in EmpireCatalog.Buildings)
        {
            if (building.RequiresId is not null && levels.GetValueOrDefault(building.RequiresId) < building.RequiresLevel)
                levels[building.Id] = 0;
        }
        return source with
        {
            SchemaVersion = CurrentSchemaVersion,
            Credits = Math.Clamp(source.Credits, 0m, EmpireCatalog.MaximumCredits),
            LifetimeCredits = Math.Clamp(source.LifetimeCredits, 0m, EmpireCatalog.MaximumCredits),
            RunCredits = Math.Clamp(source.RunCredits, 0m, EmpireCatalog.MaximumCredits),
            Prestige = Math.Clamp(source.Prestige, 0, EmpireCatalog.MaximumPrestige),
            Clicks = Math.Max(0, source.Clicks),
            Buildings = levels,
        };
    }
}
