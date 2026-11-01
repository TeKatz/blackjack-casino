namespace BlackjackCasino.Core;

public sealed record SeasonTierDefinition(
    int Tier,
    long RequiredExperience,
    long Coins,
    string? CosmeticId);

public sealed record SeasonDefinition(
    string Id,
    string Name,
    string Description,
    bool Archived,
    bool RewardsEnabled,
    IReadOnlyList<SeasonTierDefinition> Tiers);

public static class SeasonCatalog
{
    private static readonly SeasonDefinition MidnightShift = new(
        "season-1-midnight-shift",
        "Midnight Shift",
        "Season 1 · the original 50-tier free track. Completed rewards and progress remain available permanently.",
        true,
        true,
        BuildMidnightTiers());

    private static readonly SeasonDefinition VelvetEclipse = new(
        "season-2-velvet-eclipse",
        "Velvet Eclipse",
        "Season 2 · a 100-tier long-form pass with a steeper XP curve. Reward slots are intentionally unassigned for now.",
        false,
        false,
        BuildVelvetEclipseTiers());

    public static SeasonDefinition Current => VelvetEclipse;

    public static IReadOnlyList<SeasonDefinition> All { get; } = [MidnightShift, VelvetEclipse];

    public static SeasonDefinition Get(string id) => TryGet(id)
        ?? throw new KeyNotFoundException($"Unknown season '{id}'.");

    public static SeasonDefinition? TryGet(string? id) => All.FirstOrDefault(
        season => string.Equals(season.Id, id, StringComparison.Ordinal));

    private static IReadOnlyList<SeasonTierDefinition> BuildMidnightTiers()
    {
        var tiers = new List<SeasonTierDefinition>(50);
        for (int tier = 1; tier <= 50; tier++)
        {
            string? cosmetic = tier switch
            {
                5 => "title-season-midnight-5",
                10 => "frame-season-midnight-10",
                20 => "title-season-midnight-20",
                35 => "frame-season-midnight-35",
                50 => "title-season-midnight-50",
                _ => null,
            };

            long coins = tier % 10 == 0 ? 75 : tier % 5 == 0 ? 40 : 15;
            tiers.Add(new SeasonTierDefinition(tier, tier * 200L, coins, cosmetic));
        }

        return tiers;
    }

    private static IReadOnlyList<SeasonTierDefinition> BuildVelvetEclipseTiers()
    {
        var tiers = new List<SeasonTierDefinition>(100);
        for (int tier = 1; tier <= 100; tier++)
        {
            // Cumulative target: 309 XP at tier 1, 3,900 at tier 10,
            // 37,500 at tier 50 and 120,000 total XP at tier 100.
            // Every individual tier costs more than Midnight Shift's flat 200 XP.
            long required = 300L * tier + 9L * tier * tier;
            tiers.Add(new SeasonTierDefinition(tier, required, 0, null));
        }

        return tiers;
    }
}
