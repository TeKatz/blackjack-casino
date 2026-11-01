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
        "Season 2 · the original 100-tier long-form track. Completed rewards and progress remain available permanently.",
        true,
        true,
        BuildVelvetEclipseTiers());

    private static readonly SeasonDefinition NeonDynasty = new(
        "season-3-neon-dynasty",
        "Neon Dynasty",
        "Season 3 · a 120-tier neon-luxury endurance pass with exclusive Dynasty titles, portraits and animated frames.",
        false,
        true,
        BuildNeonDynastyTiers());

    public static SeasonDefinition Current => NeonDynasty;

    public static IReadOnlyList<SeasonDefinition> All { get; } = [MidnightShift, VelvetEclipse, NeonDynasty];

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
            long required = 300L * tier + 9L * tier * tier;
            string? cosmetic = tier switch
            {
                15 => "portrait-eclipse-raven",
                25 => "frame-eclipse-thread",
                35 => "title-eclipse-afterglow",
                45 => "portrait-eclipse-lady",
                55 => "frame-eclipse-halo",
                65 => "title-eclipse-architect",
                75 => "portrait-eclipse-moon",
                85 => "frame-eclipse-crown",
                95 => "title-eclipse-velvet-hour",
                100 => "portrait-eclipse-sovereign",
                _ => null,
            };

            long coins = tier switch
            {
                100 => 500,
                _ when tier % 25 == 0 => 175,
                _ when tier % 10 == 0 => 80,
                _ when tier % 5 == 0 => 35,
                _ => 12,
            };
            tiers.Add(new SeasonTierDefinition(tier, required, coins, cosmetic));
        }

        return tiers;
    }

    private static IReadOnlyList<SeasonTierDefinition> BuildNeonDynastyTiers()
    {
        var tiers = new List<SeasonTierDefinition>(120);
        for (int tier = 1; tier <= 120; tier++)
        {
            // Cumulative target: 512 XP at tier 1, 6,200 at tier 10,
            // 55,000 at tier 50, 170,000 at tier 100 and 232,800 total XP at tier 120.
            // The reward identity escalates from neon street luxury into full Dynasty regalia.
            long required = 500L * tier + 12L * tier * tier;
            string? cosmetic = tier switch
            {
                10 => "title-neon-initiate",
                20 => "frame-neon-thread",
                30 => "portrait-neon-croupier",
                40 => "title-dynasty-dealer",
                50 => "frame-dynasty-circuit",
                60 => "portrait-dynasty-heiress",
                70 => "title-electric-royalty",
                80 => "frame-neon-pagoda",
                90 => "portrait-neon-overseer",
                100 => "title-neon-sovereign",
                110 => "frame-dynasty-crown",
                120 => "portrait-neon-emperor",
                _ => null,
            };

            long coins = tier switch
            {
                120 => 600,
                _ when tier % 25 == 0 => 180,
                _ when tier % 10 == 0 => 90,
                _ when tier % 5 == 0 => 40,
                _ => 18,
            };
            tiers.Add(new SeasonTierDefinition(tier, required, coins, cosmetic));
        }

        return tiers;
    }
}
