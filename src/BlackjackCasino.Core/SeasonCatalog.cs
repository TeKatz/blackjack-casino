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
    IReadOnlyList<SeasonTierDefinition> Tiers);

public static class SeasonCatalog
{
    private static readonly SeasonDefinition MidnightShift = new(
        "season-1-midnight-shift",
        "Midnight Shift",
        "Season 1 · the original 50-tier free track. Completed rewards remain yours permanently.",
        true,
        BuildMidnightTiers());

    private static readonly SeasonDefinition VelvetEclipse = new(
        "season-2-velvet-eclipse",
        "Velvet Eclipse",
        "Season 2 · a 100-tier long-form free track for Blackjack and both slot machines. No premium track, no expiry pressure.",
        false,
        BuildVelvetEclipseTiers());

    public static SeasonDefinition Current => VelvetEclipse;

    public static IReadOnlyList<SeasonDefinition> All { get; } = [MidnightShift, VelvetEclipse];

    public static SeasonDefinition Get(string id) => All.First(x => string.Equals(x.Id, id, StringComparison.Ordinal));

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
            // 390 XP at tier 1, 3,900 at tier 10, 37,500 at tier 50 and
            // 120,000 total XP at tier 100. The old pass ended at only 10,000 XP.
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
}
