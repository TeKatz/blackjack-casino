using System.Diagnostics;
using System.Globalization;
using System.Text;
using BlackjackCasino.Core.Empire;

var output = args.FirstOrDefault() ?? "artifacts/empire-economy.csv";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
bool legacyRequirements = args.Contains("--legacy-requirements");
bool allFloors = args.Contains("--all-floors");
var csv = new StringBuilder("prestige,build,hours_to_next_prestige,levels,production,dividend_per_hour,offline_hours,iterations\n");
DateTimeOffset epoch = new(2026,10,30,0,0,0,TimeSpan.Zero);
foreach (int prestige in new[] { 0,1,5,10,25,50 })
foreach (string build in new[] { "legacy-baseline", "balanced", "operations", "gaming", "hospitality", "achievements", "managed" })
{
    var seed = new EmpireState { Prestige = prestige };
    var research = new Dictionary<string,int>();
    int budget = prestige;
    string[] priorities = build switch
    {
        "operations" or "managed" => ["construction","compound","night-shift","automation","directors"],
        "gaming" => ["tables","mechanical","high-roller","construction"],
        "hospitality" => ["hospitality","destination","guests","construction"],
        _ => [],
    };
    foreach (string id in priorities)
    {
        var node = EmpireResearchCatalog.Find(id)!;
        if (node.Prerequisite is not null && research.GetValueOrDefault(node.Prerequisite) < node.PrerequisiteRank) continue;
        int ranks = Math.Min(node.MaxRank, budget / node.CostPerRank);
        research[id] = ranks; budget -= ranks * node.CostPerRank;
    }
    var service = new EmpireService(seed with { Research = research });
    if (build == "achievements") service.SetClaimedAchievements(EmpireAchievementBoostCatalog.All.Select(boost=>boost.ChallengeId));
    if (build == "managed" && prestige >= 3)
    {
        EmpireState normalized = service.Snapshot;
        EmpirePropertyState property = normalized.Properties[normalized.ActivePropertyId] with
        { Projects = new(["grand-entrance","executive-offices","high-roller-elevator"]), Managers = new() { ["elena"] = "slots" } };
        service = new EmpireService(normalized with { Properties = new(normalized.Properties) { [normalized.ActivePropertyId] = property } });
    }
    service.Start(epoch);
    DateTimeOffset now = epoch;
    int steps = 0;
    decimal elapsed = 0;
    bool legacy = build == "legacy-baseline";
    int levelTarget = legacyRequirements ? Math.Min(1000, 250 + prestige * 25) : service.PrestigeMainFloorLevelRequirement;
    bool Reached() => service.MainFloorLevels >= levelTarget && service.OpenRoomsOnFloor(1) == 5 && service.Snapshot.RunCredits >= service.PrestigeRunCreditRequirement;
    while (!Reached() && steps++ < 5000)
    {
        EmpireState state = service.Snapshot;
        decimal rate = legacy ? EmpireCatalog.Floor(1).Sum(room => EmpireRules.Production(room, service.Level(room.Id)) * service.PrestigeMultiplier) : service.CreditsPerSecond;
        var choices = (allFloors && !legacy ? EmpireCatalog.Departments.Where(room => service.FloorUnlocked(room.Floor)) : EmpireCatalog.Floor(1)).Where(room => service.Level(room.Id) < EmpireRules.MaximumLevel).SelectMany(room => new[] { 1, Math.Min(10 - service.Level(room.Id) % 10, EmpireRules.MaximumLevel - service.Level(room.Id)) }
            .Distinct().Select(count =>
            {
                int level = service.Level(room.Id);
                EmpireUpgradeQuote quote = service.QuoteUpgrade(room.Id,count);
                decimal delta = EmpireRules.Production(room,level + count) - EmpireRules.Production(room,level);
                return (Room: room, Quote: quote, Score: quote.Cost / Math.Max(.001m,delta));
            })).OrderBy(item => item.Score).ThenBy(item => item.Room.Id, StringComparer.Ordinal).ToArray();
        if (service.MainFloorLevels >= levelTarget && service.OpenRoomsOnFloor(1) == 5)
        {
            decimal wait = Math.Max(0, service.PrestigeRunCreditRequirement - state.RunCredits) / Math.Max(.001m,rate);
            Advance(wait + .001m, rate);
            continue;
        }
        var next = choices.First();
        decimal seconds = Math.Max(0, next.Quote.Cost - service.Credits) / Math.Max(.001m,rate);
        Advance(seconds + .001m, rate);
        if (!service.UpgradeMany(next.Room.Id,next.Quote.Levels,now)) throw new InvalidOperationException("Simulation could not fund the selected quote.");
    }
    if (!Reached()) throw new InvalidOperationException("Simulation failed to reach prestige.");
    csv.AppendLine(string.Join(',', prestige,build,(elapsed/3600m).ToString("0.000",CultureInfo.InvariantCulture),service.TotalLevels,
        (legacy ? EmpireCatalog.Floor(1).Sum(room => EmpireRules.Production(room,service.Level(room.Id))*service.PrestigeMultiplier) : service.CreditsPerSecond).ToString("0.00",CultureInfo.InvariantCulture),service.CashPerHour.ToString("0.00",CultureInfo.InvariantCulture),service.OfflineCapacity.TotalHours,steps));

    void Advance(decimal seconds, decimal rate)
    {
        // Counterfactual baseline uses the original curve on the same purchase path.
        // Chunking respects the actual offline cap. All strategic builds use the service.
        while (seconds > 0)
        {
            decimal piece = Math.Min(seconds, 3600);
            now = now.AddSeconds((double)piece);
            if (legacy)
            {
                EmpireState snapshot = service.Snapshot;
                var estate = snapshot.Properties[snapshot.ActivePropertyId];
                estate = estate with { Credits = estate.Credits + rate * piece, RunCredits = estate.RunCredits + rate * piece, LifetimeCredits = estate.LifetimeCredits + rate * piece };
                service = new EmpireService(snapshot with { LastAccruedAt = now, Properties = new(snapshot.Properties) { [snapshot.ActivePropertyId] = estate } });
            }
            else service.Accrue(now);
            elapsed += piece; seconds -= piece;
        }
    }
}
File.WriteAllText(output,csv.ToString());
var stress = new EmpireService(new EmpireState { Prestige = 50, Credits = EmpireRules.MaximumCredits, Levels = EmpireCatalog.Departments.ToDictionary(room=>room.Id,_=>200) });
stress.Start(epoch);
var watch = Stopwatch.StartNew();
for(int tick=1;tick<=10000;tick++) stress.Accrue(epoch.AddMilliseconds(tick*500));
Console.WriteLine($"Saved {output}. 10,000 production ticks: {watch.Elapsed.TotalMilliseconds:0} ms.");

var breakEven = new StringBuilder("department,level,next_level_cost,base_production,marginal_production,repayment_hours\n");
foreach (EmpireDepartment room in EmpireCatalog.Departments)
foreach (int level in new[] { 1,9,25,49,50,99,100,149,150,199 })
{
    decimal cost = EmpireRules.UpgradeCost(room,level);
    decimal production = EmpireRules.Production(room,level);
    decimal delta = EmpireRules.Production(room,level+1)-production;
    breakEven.AppendLine(string.Join(',',room.Id,level,cost.ToString(CultureInfo.InvariantCulture),production.ToString(CultureInfo.InvariantCulture),delta.ToString(CultureInfo.InvariantCulture),(cost/delta/3600).ToString("0.000",CultureInfo.InvariantCulture)));
}
File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"empire-break-even.csv"),breakEven.ToString());
