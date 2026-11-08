using System.Collections.ObjectModel;

namespace BlackjackCasino.Core.MidnightMerge;

/// <summary>Content definitions contain stable IDs and resource keys, never localized rule inputs.</summary>
public sealed class MergeCatalog
{
    public IReadOnlyDictionary<string, MergeItemDefinition> Items { get; }
    public IReadOnlyDictionary<string, MergeProducerDefinition> Producers { get; }
    public IReadOnlyList<MergeItemLine> Lines { get; }
    public IReadOnlyList<MergeOrderDefinition> Orders { get; }
    public IReadOnlyList<MergeArea> Areas { get; }
    public static MergeCatalog Default { get; } = CreateDefault();

    public MergeCatalog(IEnumerable<MergeItemDefinition> items, IEnumerable<MergeProducerDefinition> producers,
        IEnumerable<MergeOrderDefinition> orders, IEnumerable<MergeArea> areas)
    {
        Items = new ReadOnlyDictionary<string, MergeItemDefinition>(items.ToDictionary(x => x.Id, x => x with { Tags = Array.AsReadOnly(x.Tags.ToArray()) }));
        Producers = new ReadOnlyDictionary<string, MergeProducerDefinition>(producers.ToDictionary(x => x.Id));
        Orders = Array.AsReadOnly(orders.Select(x => x with { Requirements = Array.AsReadOnly(x.Requirements.ToArray()) }).ToArray());
        Areas = Array.AsReadOnly(areas.Select(x => x with { Milestones = Array.AsReadOnly(x.Milestones.ToArray()) }).ToArray());
        Lines = Array.AsReadOnly(Items.Values.GroupBy(x => x.LineId)
            .Select(x => new MergeItemLine(x.Key, Array.AsReadOnly(x.OrderBy(i => i.Tier).Select(i => i.Id).ToArray()))).ToArray());

        bool invalidAreas = Areas.Count == 0
            || Areas.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != Areas.Count
            || Areas.Any(area => area.Milestones.Count == 0
                || area.Milestones.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != area.Milestones.Count
                || !area.Milestones.Select(x => x.Points).SequenceEqual(area.Milestones.Select(x => x.Points).OrderBy(x => x)));
        bool invalidAreaOrder = Areas.Zip(Areas.Skip(1), (a, b) => a.Milestones[0].Points >= b.Milestones[0].Points).Any(x => x);

        if (Items.Values.Any(x => x.Tier < 1 || x.Tier > x.MaxTier || (x.MergeResult is { } next &&
            (!Items.TryGetValue(next, out var result) || result.LineId != x.LineId || result.Tier != x.Tier + 1))) ||
            Producers.Values.Any(x => x.UnlockPoints < 0 || !Items.ContainsKey(x.OutputDefinitionId)) || Orders.Count == 0 ||
            Orders.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != Orders.Count || invalidAreas || invalidAreaOrder ||
            Orders.Any(x => x.Reward <= 0 || !Areas.Any(a => a.Id == x.AreaId) || x.Requirements.Count == 0 ||
                x.Requirements.Any(r => r.Count < 1 || !Items.ContainsKey(r.DefinitionId))))
            throw new ArgumentException("Invalid merge catalog references or requirements.");
    }

    private static MergeCatalog CreateDefault()
    {
        string[] chipColors = ["#BBD0C4", "#7FB59C", "#73ADCE", "#B396D6", "#DBB468", "#F4DE9D"];
        string[] chipMarks = ["I", "II", "III", "IV", "V", "VI"];
        var chips = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"chip-{t}", $"Merge.Chip{t}", "casino-chips", t, 6,
            new(chipColors[t - 1], chipMarks[t - 1], Math.Min(3, (t + 1) / 2)), t < 6 ? $"chip-{t + 1}" : null, ["chip"]));

        string[] drinkColors = ["#C6D6D4", "#A9D5C0", "#E0C47A", "#E39A77", "#C17B9E", "#A188CE", "#E7C970", "#F4E4A6"];
        string[] drinkMarks = ["G", "I", "H", "C", "S", "R", "CH", "M"];
        var drinks = Enumerable.Range(1, 8).Select(t => new MergeItemDefinition(
            $"drink-{t}", $"Merge.Drink{t}", "bar-drinks", t, 8,
            new(drinkColors[t - 1], drinkMarks[t - 1], Math.Min(3, (t + 1) / 3)), t < 8 ? $"drink-{t + 1}" : null, ["drink", "bar"]));

        var decor = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"decor-{t}", $"Merge.Decor{t}", "decor-supplies", t, 6,
            new(new[] { "#C5B59A", "#CFAF6E", "#B79BC5", "#D0A76B", "#E5D4A2", "#F5E3AF" }[t - 1],
                new[] { "D1", "D2", "D3", "D4", "D5", "D6" }[t - 1], Math.Min(3, (t + 1) / 2)),
            t < 6 ? $"decor-{t + 1}" : null, ["decor", "restoration"]));

        var garden = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"garden-{t}", $"Merge.Garden{t}", "garden-supplies", t, 6,
            new(new[] { "#8CC4A3", "#73B68E", "#78BE8E", "#A5CF8F", "#D4DC91", "#E2F5AF" }[t - 1],
                new[] { "G1", "G2", "G3", "G4", "G5", "G6" }[t - 1], Math.Min(3, (t + 1) / 2)),
            t < 6 ? $"garden-{t + 1}" : null, ["garden", "restoration"]));

        var stage = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"stage-{t}", $"Merge.Stage{t}", "stage-supplies", t, 6,
            new(new[] { "#C59BB8", "#A784C4", "#B395D5", "#E0A6A1", "#E4C27D", "#FFE4A9" }[t - 1],
                new[] { "S1", "S2", "S3", "S4", "S5", "S6" }[t - 1], Math.Min(3, (t + 1) / 2)),
            t < 6 ? $"stage-{t + 1}" : null, ["stage", "restoration"]));

        var sky = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"sky-{t}", $"Merge.Sky{t}", "sky-supplies", t, 6,
            new(new[] { "#8ABEC9", "#87ABDE", "#9F9AE1", "#C6A8E9", "#B6DFF0", "#FBE2B5" }[t - 1],
                new[] { "O1", "O2", "O3", "O4", "O5", "O6" }[t - 1], Math.Min(3, (t + 1) / 2)),
            t < 6 ? $"sky-{t + 1}" : null, ["sky", "restoration"]));

        var orders = new List<MergeOrderDefinition>
        {
            new("first-stack", [new("chip-2", 1)], 10),
            new("blue-table", [new("chip-3", 1)], 22),
            new("paired-stacks", [new("chip-2", 2)], 22),
            new("violet-table", [new("chip-4", 1)], 48),
            new("gold-table", [new("chip-5", 1)], 100),
            new("vip-table", [new("chip-6", 1)], 210),

            new("bar-glassware", [new("drink-2", 1), new("chip-2", 1)], 18, "bar"),
            new("bar-house-pour", [new("drink-3", 1), new("chip-2", 1)], 32, "bar"),
            new("bar-cocktail-pair", [new("drink-4", 1), new("chip-3", 1)], 72, "bar"),
            new("bar-signature-tray", [new("drink-5", 1), new("chip-3", 1)], 150, "bar"),
            new("bar-reserve-service", [new("drink-6", 1), new("chip-4", 1)], 310, "bar"),
            new("bar-champagne", [new("drink-7", 1), new("chip-5", 1)], 620, "bar"),
            new("bar-midnight-reserve", [new("drink-8", 1), new("chip-6", 1)], 900, "bar"),
            new("bar-cellar-clearance", [new("drink-8", 1)], 650, "bar"),

            new("floor-brass", [new("decor-3", 1), new("chip-3", 1)], 340, "gaming-floor"),
            new("floor-seating", [new("decor-4", 1), new("drink-3", 1)], 550, "gaming-floor"),
            new("floor-tables", [new("decor-5", 1), new("chip-4", 1)], 750, "gaming-floor"),
            new("floor-centerpiece", [new("decor-6", 1)], 900, "gaming-floor"),
            new("garden-planters", [new("garden-3", 1), new("drink-3", 1)], 410, "conservatory"),
            new("garden-benches", [new("garden-4", 1), new("decor-3", 1)], 650, "conservatory"),
            new("garden-arch", [new("garden-5", 1), new("decor-4", 1)], 900, "conservatory"),
            new("garden-living-wall", [new("garden-6", 1)], 1000, "conservatory"),
            new("lounge-service", [new("drink-4", 1), new("decor-3", 1)], 500, "lounge"),
            new("lounge-upholstery", [new("decor-4", 1), new("chip-4", 1)], 700, "lounge"),
            new("lounge-greenery", [new("garden-5", 1), new("decor-4", 1)], 950, "lounge"),
            new("lounge-signature", [new("decor-6", 1), new("drink-5", 1)], 1250, "lounge"),
            new("gallery-frames", [new("decor-4", 1), new("garden-3", 1)], 650, "gallery"),
            new("gallery-collection", [new("chip-5", 1), new("decor-4", 1)], 900, "gallery"),
            new("gallery-installation", [new("decor-5", 1), new("garden-4", 1)], 1150, "gallery"),
            new("gallery-opening", [new("decor-6", 1), new("drink-5", 1)], 1450, "gallery"),
            new("theater-props", [new("stage-3", 1), new("decor-3", 1)], 700, "theater"),
            new("theater-lighting", [new("stage-4", 1), new("drink-4", 1)], 1000, "theater"),
            new("theater-set", [new("stage-5", 1), new("garden-4", 1)], 1400, "theater"),
            new("theater-curtain", [new("stage-6", 1)], 1800, "theater"),
            new("terrace-planters", [new("garden-4", 1), new("stage-3", 1)], 750, "terrace"),
            new("terrace-greenery", [new("garden-5", 1), new("decor-3", 1)], 1100, "terrace"),
            new("terrace-canopy", [new("garden-6", 1), new("drink-5", 1)], 1450, "terrace"),
            new("terrace-illumination", [new("stage-5", 1), new("decor-5", 1)], 1700, "terrace"),
            new("suite-amenities", [new("sky-3", 1), new("decor-4", 1)], 900, "vip-suites"),
            new("suite-observation", [new("sky-4", 1), new("drink-5", 1)], 1250, "vip-suites"),
            new("suite-interiors", [new("sky-5", 1), new("garden-4", 1)], 1600, "vip-suites"),
            new("suite-luxury", [new("decor-6", 1), new("sky-6", 1)], 2000, "vip-suites"),
            new("observatory-mechanics", [new("sky-3", 1), new("stage-4", 1)], 1000, "observatory"),
            new("observatory-lenses", [new("sky-4", 1), new("garden-5", 1)], 1450, "observatory"),
            new("observatory-calibration", [new("sky-5", 1), new("decor-5", 1)], 1800, "observatory"),
            new("observatory-unveiling", [new("sky-6", 1)], 2300, "observatory"),
        };

        return new(chips.Concat(drinks).Concat(decor).Concat(garden).Concat(stage).Concat(sky),
            [
                new("chip-case", "Merge.ProducerChip", "chip-1"),
                new("bar-cart", "Merge.ProducerBar", "drink-1", 100),
                new("decor-cabinet", "Merge.ProducerDecor", "decor-1", 2500),
                new("garden-chest", "Merge.ProducerGarden", "garden-1", 5600),
                new("stage-trunk", "Merge.ProducerStage", "stage-1", 21800),
                new("sky-kit", "Merge.ProducerSky", "sky-1", 38800),
            ],
            orders,
            [
                new("lobby", "Merge.Area", [
                    new("dusty", "Merge.StageDusty", 0),
                    new("cleanup", "Merge.StageCleanup", 20),
                    new("lights", "Merge.StageLights", 50),
                    new("ready", "Merge.StageReady", 100),
                ]),
                new("bar", "Merge.AreaBar", [
                    new("closed", "Merge.StageBarClosed", 100),
                    new("cleanup", "Merge.StageBarCleanup", 500),
                    new("stocked", "Merge.StageBarStocked", 1200),
                    new("open", "Merge.StageBarOpen", 2500),
                ]),
                new("gaming-floor", "Merge.AreaFloor", [
                    new("closed", "Merge.StageFloor1", 2500),
                    new("cleared", "Merge.StageFloor2", 3200),
                    new("furnished", "Merge.StageFloor3", 4200),
                    new("ready", "Merge.StageFloor4", 5600),
                ]),
                new("conservatory", "Merge.AreaConservatory", [
                    new("closed", "Merge.StageConservatory1", 5600),
                    new("cleared", "Merge.StageConservatory2", 6500),
                    new("furnished", "Merge.StageConservatory3", 8000),
                    new("ready", "Merge.StageConservatory4", 9900),
                ]),
                new("lounge", "Merge.AreaLounge", [
                    new("closed", "Merge.StageLounge1", 9900),
                    new("cleared", "Merge.StageLounge2", 11100),
                    new("furnished", "Merge.StageLounge3", 12800),
                    new("ready", "Merge.StageLounge4", 15200),
                ]),
                new("gallery", "Merge.AreaGallery", [
                    new("closed", "Merge.StageGallery1", 15200),
                    new("cleared", "Merge.StageGallery2", 16700),
                    new("furnished", "Merge.StageGallery3", 19000),
                    new("ready", "Merge.StageGallery4", 21800),
                ]),
                new("theater", "Merge.AreaTheater", [
                    new("closed", "Merge.StageTheater1", 21800),
                    new("cleared", "Merge.StageTheater2", 23700),
                    new("furnished", "Merge.StageTheater3", 26300),
                    new("ready", "Merge.StageTheater4", 29500),
                ]),
                new("terrace", "Merge.AreaTerrace", [
                    new("closed", "Merge.StageTerrace1", 29500),
                    new("cleared", "Merge.StageTerrace2", 31700),
                    new("furnished", "Merge.StageTerrace3", 35000),
                    new("ready", "Merge.StageTerrace4", 38800),
                ]),
                new("vip-suites", "Merge.AreaSuites", [
                    new("closed", "Merge.StageSuites1", 38800),
                    new("cleared", "Merge.StageSuites2", 41500),
                    new("furnished", "Merge.StageSuites3", 45300),
                    new("ready", "Merge.StageSuites4", 49800),
                ]),
                new("observatory", "Merge.AreaObservatory", [
                    new("closed", "Merge.StageObservatory1", 49800),
                    new("cleared", "Merge.StageObservatory2", 53200),
                    new("furnished", "Merge.StageObservatory3", 57800),
                    new("ready", "Merge.StageObservatory4", 63600),
                ]),
            ]);
    }
}
