using System.Globalization;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.HostShared;

internal static class EmpireModernSnapshot
{
    internal static string Decimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    internal static object Create(EmpireService service)
    {
        var buildings = EmpireCatalog.Buildings.Select(b =>
        {
            int level = service.Level(b.Id);
            string requirement = b.RequiresId is null ? "" :
                EmpireCatalog.Find(b.RequiresId)!.Name + " · Level " + b.RequiresLevel;
            return new
            {
                b.Id, b.Name, b.Description, Level = level, MaxLevel = EmpireCatalog.MaximumLevel,
                Unlocked = service.Unlocked(b), Visible = service.Visible(b),
                Cost = Decimal(service.NextCost(b)), CanBuy = service.CanBuy(b),
                Rate = Decimal(service.Production(b)), VisualTier = EmpireCatalog.VisualTier(level),
                Requirement = requirement,
                NextUnlock = EmpireCatalog.Buildings.FirstOrDefault(next =>
                    next.RequiresId == b.Id && next.RequiresLevel == level + 1)?.Name,
            };
        }).ToArray();

        EmpireBuilding? nextLocked = EmpireCatalog.Buildings.FirstOrDefault(b =>
            service.Visible(b) && !service.Unlocked(b));
        EmpireBuilding? goal = nextLocked is not null
            ? EmpireCatalog.Find(nextLocked.RequiresId)
            : EmpireCatalog.Buildings.Where(b => service.Unlocked(b)
                && service.Level(b.Id) < EmpireCatalog.MaximumLevel)
                .OrderBy(service.NextCost).FirstOrDefault();
        decimal target = goal is null ? 1m : service.NextCost(goal);
        decimal current = Math.Min(target, service.Credits);
        var permanentUpgrades = EmpireLegacyCatalog.Upgrades.Select(upgrade =>
        {
            bool owned = service.OwnsLegacyUpgrade(upgrade.Id);
            bool prerequisite = upgrade.PreviousId is null ||
                service.OwnsLegacyUpgrade(upgrade.PreviousId);
            return new
            {
                upgrade.Id, upgrade.Path, upgrade.Name, upgrade.Description,
                upgrade.Cost, upgrade.MinimumPrestige, upgrade.PreviousId,
                Owned = owned,
                Unlocked = service.Prestige >= upgrade.MinimumPrestige && prerequisite,
                CanBuy = service.CanBuyLegacyUpgrade(upgrade),
            };
        }).ToArray();
        var milestones = EmpireLegacyCatalog.Milestones.Select(milestone =>
        {
            decimal current = Math.Min(service.MilestoneValue(milestone), milestone.Target);
            return new
            {
                milestone.Id, milestone.Name, milestone.Description,
                Category = milestone.Kind, Target = Decimal(milestone.Target),
                Current = Decimal(current),
                Earned = service.CompletedMilestones.Contains(milestone.Id, StringComparer.Ordinal),
                Progress = milestone.Target == 0m ? 1d :
                    (double)(current / milestone.Target),
            };
        }).ToArray();
        return new
        {
            Legacy = new
            {
                Unlocked = service.Prestige >= 1,
                Tokens = service.PrestigeTokens,
                Earned = milestones.Count(m => m.Earned),
                Total = milestones.Length,
                Upgrades = permanentUpgrades, Milestones = milestones,
            },
            Credits = Decimal(service.Credits), Rate = Decimal(service.CreditsPerSecond),
            TapValue = Decimal(service.CreditsPerClick), service.Clicks, service.Prestige,
            service.TotalLevels, RunCredits = Decimal(service.RunCredits),
            PrestigeRequirement = Decimal(service.PrestigeRequirement),
            service.CanPrestige, Buildings = buildings,
            Goal = new
            {
                Id = goal?.Id,
                Title = nextLocked is not null ? "Unlock " + nextLocked.Name :
                    goal is not null ? "Upgrade " + goal.Name : "Casino fully developed",
                Description = nextLocked is not null ? "Reach " +
                    EmpireCatalog.Find(nextLocked.RequiresId)!.Name + " level " + nextLocked.RequiresLevel :
                    goal is not null ? "Next building improvement" : "All buildings at maximum level",
                Current = Decimal(current), Target = Decimal(target),
                Progress = goal is null ? 1d : (double)(target == 0m ? 1m : current / target),
            },
        };
    }
}
