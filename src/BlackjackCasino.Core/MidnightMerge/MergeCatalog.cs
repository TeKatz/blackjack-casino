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
        if (Items.Values.Any(x => x.Tier < 1 || x.Tier > x.MaxTier || (x.MergeResult is { } next &&
            (!Items.TryGetValue(next, out var result) || result.LineId != x.LineId || result.Tier != x.Tier + 1))) ||
            Producers.Values.Any(x => !Items.ContainsKey(x.OutputDefinitionId)) || Orders.Count == 0 ||
            Orders.Select(x => x.Id).Distinct().Count() != Orders.Count ||
            Orders.Any(x => x.Reward <= 0 || x.Requirements.Count == 0 || x.Requirements.Any(r => r.Count < 1 || !Items.ContainsKey(r.DefinitionId))))
            throw new ArgumentException("Invalid merge catalog references or requirements.");
    }

    private static MergeCatalog CreateDefault()
    {
        string[] colors = ["#BBD0C4", "#7FB59C", "#73ADCE", "#B396D6", "#DBB468", "#F4DE9D"];
        string[] marks = ["I", "II", "III", "IV", "V", "VI"];
        var items = Enumerable.Range(1, 6).Select(t => new MergeItemDefinition(
            $"chip-{t}", $"Merge.Chip{t}", "casino-chips", t, 6,
            new(colors[t - 1], marks[t - 1], Math.Min(3, (t + 1) / 2)), t < 6 ? $"chip-{t + 1}" : null, ["chip"]));
        // Standing supply requests cover every terminal item: a full board can always recover.
        var orders = new List<MergeOrderDefinition>
        {
            new("first-stack", [new("chip-2", 1)], 10),
            new("blue-table", [new("chip-3", 1)], 22),
            new("paired-stacks", [new("chip-2", 2)], 22),
            new("violet-table", [new("chip-4", 1)], 48),
            new("gold-table", [new("chip-5", 1)], 100),
            new("vip-table", [new("chip-6", 1)], 210),
        };
        return new(items, [new("chip-case", "Merge.Producer", "chip-1")], orders,
            [new("lobby", "Merge.Area", [new("dusty", "Merge.StageDusty", 0), new("cleanup", "Merge.StageCleanup", 20),
                new("lights", "Merge.StageLights", 50), new("ready", "Merge.StageReady", 100)])]);
    }
}
