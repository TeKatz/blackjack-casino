namespace BlackjackCasino.Core.Empire;

/// <summary>Small, finite permanent upgrade tracks. Purchase IDs, costs and
/// prerequisites are authoritative here, never in the React frontend.</summary>
public sealed record EmpireLegacyUpgrade(string Id, string Path, string Name, string Description,
    int Cost, int MinimumPrestige, string? PreviousId);

public sealed record EmpireLegacyMilestone(string Id, string Name, string Description,
    string Kind, decimal Target);

public static class EmpireLegacyCatalog
{
    public const int PrestigeTokensPerReset = 2;
    public const int MaximumTokens = 1_000;

    // The first node in every track costs just one token so Prestige I
    // immediately introduces a meaningful choice. Later nodes stay hidden
    // until their predecessor is purchased.
    public static IReadOnlyList<EmpireLegacyUpgrade> Upgrades { get; } =
        Array.AsReadOnly<EmpireLegacyUpgrade>(
        [
            new("active-1", "active", "Polished Chips", "+15% permanent click power.", 1, 1, null),
            new("active-2", "active", "Lucky Twenty-Five", "Critical clicks pay four times instead of three.", 2, 1, "active-1"),
            new("active-3", "active", "High-Stakes Hands", "+25% permanent click power.", 3, 2, "active-2"),
            new("active-4", "active", "Quick Jackpot", "Every 20th click is critical instead of every 25th.", 4, 3, "active-3"),
            new("active-5", "active", "Golden Touch", "+50% permanent click power.", 6, 5, "active-4"),

            new("passive-1", "passive", "Reliable Floor", "+15% permanent building production.", 1, 1, null),
            new("passive-2", "passive", "Experienced Staff", "+10% permanent building production.", 2, 1, "passive-1"),
            new("passive-3", "passive", "Premium Tables", "+22% permanent building production.", 3, 2, "passive-2"),
            new("passive-4", "passive", "Luxury Operations", "+25% permanent building production.", 4, 3, "passive-3"),
            new("passive-5", "passive", "Empire Franchise", "+40% permanent building production.", 6, 5, "passive-4"),

            new("hybrid-1", "hybrid", "House Advantage", "+8% to clicks and building production.", 1, 1, null),
            new("hybrid-2", "hybrid", "Connected Floors", "+12% to clicks and building production.", 2, 1, "hybrid-1"),
            new("hybrid-3", "hybrid", "Grand Partnership", "+15% to clicks and building production.", 3, 2, "hybrid-2"),
            new("hybrid-4", "hybrid", "Private Syndicate", "+18% to clicks and building production.", 4, 3, "hybrid-3"),
            new("hybrid-5", "hybrid", "Midnight Legacy", "+25% to clicks and building production.", 6, 5, "hybrid-4"),
        ]);

    // Every milestone pays one permanent legacy token, only once across
    // all runs. This provides frequent intermediate goals without a second
    // economy that must be managed before Prestige I.
    public static IReadOnlyList<EmpireLegacyMilestone> Milestones { get; } =
        Array.AsReadOnly<EmpireLegacyMilestone>(
        [
            new("click-50", "First Stack", "Make 50 clicks.", "clicks", 50m),
            new("click-250", "Warm Hands", "Make 250 clicks.", "clicks", 250m),
            new("click-1000", "In the Rhythm", "Make 1,000 clicks.", "clicks", 1_000m),
            new("click-5000", "Chip Master", "Make 5,000 clicks.", "clicks", 5_000m),
            new("click-20000", "Endless Stack", "Make 20,000 clicks.", "clicks", 20_000m),

            new("wealth-1000", "First Earnings", "Earn 1,000 credits across all runs.", "credits", 1_000m),
            new("wealth-50000", "Profitable Night", "Earn 50,000 lifetime credits.", "credits", 50_000m),
            new("wealth-1000000", "Millionaire", "Earn 1 million lifetime credits.", "credits", 1_000_000m),
            new("wealth-50000000", "Casino Fortune", "Earn 50 million lifetime credits.", "credits", 50_000_000m),
            new("wealth-2000000000", "Empire Fortune", "Earn 2 billion lifetime credits.", "credits", 2_000_000_000m),

            new("build-5", "Foundation", "Reach 5 building levels in a run.", "levels", 5m),
            new("build-12", "Growing Casino", "Reach 12 building levels in a run.", "levels", 12m),
            new("build-20", "Established Casino", "Reach 20 building levels in a run.", "levels", 20m),
            new("build-32", "Luxury Complex", "Reach 32 building levels in a run.", "levels", 32m),
            new("build-48", "Midnight Landmark", "Reach 48 building levels in a run.", "levels", 48m),

            new("prestige-1", "New Beginnings", "Reach Prestige I.", "prestige", 1m),
            new("prestige-2", "Second Opening", "Reach Prestige II.", "prestige", 2m),
            new("prestige-3", "Rising Reputation", "Reach Prestige III.", "prestige", 3m),
            new("prestige-5", "Casino Dynasty", "Reach Prestige V.", "prestige", 5m),
            new("prestige-10", "Midnight Legend", "Reach Prestige X.", "prestige", 10m),
        ]);

    public static EmpireLegacyUpgrade? FindUpgrade(string? id) =>
        Upgrades.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    public static bool IsKnownMilestone(string? id) =>
        Milestones.Any(x => string.Equals(x.Id, id, StringComparison.Ordinal));
}
