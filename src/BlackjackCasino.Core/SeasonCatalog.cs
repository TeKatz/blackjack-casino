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
        "A free 50-tier cosmetic track built for late-night blackjack. It does not expire from your library.",
        false,
        BuildMidnightTiers());

    public static SeasonDefinition Current => MidnightShift;

    public static IReadOnlyList<SeasonDefinition> All { get; } = [MidnightShift];

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
}
