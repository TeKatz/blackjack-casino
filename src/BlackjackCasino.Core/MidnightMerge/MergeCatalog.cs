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
        };

        return new(chips.Concat(drinks),
            [
                new("chip-case", "Merge.ProducerChip", "chip-1"),
                new("bar-cart", "Merge.ProducerBar", "drink-1", 100),
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
            ]);
    }
}
