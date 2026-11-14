using System.Globalization;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal static class EmpireModernSnapshot
{
    internal static string Decimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    internal static object Create(EmpireService service)
    {
        EmpireState state = service.Snapshot;
        EmpirePropertyState estate = state.Properties[state.ActivePropertyId];
        return new
        {
            service.ActivePropertyId, Credits = Decimal(service.Credits), Dividend = Decimal(service.ClaimableDividend),
            DividendCapacity = Decimal(service.DividendCapacity), Rate = Decimal(service.CreditsPerSecond),
            PortfolioRate = Decimal(service.TotalCreditsPerSecond), CashPerHour = Decimal(service.CashPerHour),
            service.Prestige, service.TotalPrestige, service.Reputation, service.TotalLevels, service.OpenRooms,
            service.LegacyPointsAvailable, service.ManagerSlots, service.AutomationUnlocked,
            Automation = Automation(service.Automation), service.ManagerAssignments,
            service.CanPrestige, PrestigeRequirement = service.PrestigeRequirementText(),
            service.MainFloorLevels, service.PrestigeMainFloorLevelRequirement, PrestigeRunCreditRequirement = Decimal(service.PrestigeRunCreditRequirement),
            OfflineHours = service.OfflineCapacity.TotalHours,
            Floors = EmpireFloorCatalog.All.Select(f => new { f.Id, f.DisplayName, f.Accent, Unlocked = service.FloorUnlocked(f.Id), Requirement = f.Requirement.Description }),
            Rooms = EmpireCatalog.Departments.Select(r => new
            {
                r.Id, r.Name, r.Description, r.Floor, r.Category, Tags = r.Tags.ToString(), r.Accent,
                Level = service.Level(r.Id), Rate = Decimal(service.RoomCreditsPerSecond(r.Id)),
                Requirement = r.UnlockRequirement.Description, Breakdown = Breakdown(service.ProductionBreakdown(r.Id)),
                Quotes = new[] { 1, 5, 10, 25, 50, 100 }.Select(n =>
                {
                    var q = service.QuoteUpgrade(r.Id, n);
                    return new { q.RequestedLevels, q.Levels, q.TargetLevel, Cost = Decimal(q.Cost), q.Unlocked,
                        CanBuy = q.Unlocked && q.Levels > 0 && service.Credits >= q.Cost };
                }).ToArray(),
            }).ToArray(),
            Research = EmpireResearchCatalog.All.Select(r => new { r.Id, r.Name, r.Description, r.Branch, r.MaxRank,
                r.CostPerRank, r.Prerequisite, r.PrerequisiteRank, Rank = service.ResearchRank(r.Id), CanBuy = service.CanResearch(r.Id) }),
            Projects = EmpireProjectCatalog.All.Select(p => new { p.Id, p.Name, p.Description, Cost = Decimal(p.Cost),
                Requirement = p.Requirement.Description, Owned = service.Projects.Contains(p.Id), CanBuy = service.CanBuildProject(p.Id) }),
            Properties = EmpirePropertyCatalog.All.Select(p => new { p.Id, p.Name, p.Description, Cost = Decimal(p.Cost),
                Requirement = p.Requirement.Description, Owned = state.Properties.ContainsKey(p.Id), CanBuy = service.CanBuyProperty(p.Id),
                State = state.Properties.TryGetValue(p.Id, out var owned) ? new { Credits = Decimal(owned.Credits), owned.Prestige,
                    LifetimeCredits = Decimal(owned.LifetimeCredits), TotalLevels = owned.Levels.Values.Sum() } : null }),
            Synergies = service.Synergies.Select(s => new { s.Definition.Id, s.Definition.Name, s.Definition.Description,
                s.Tier, s.MinimumLevel, Bonus = Decimal(s.Bonus), s.NextLevel,
                Members = EmpireCatalog.Departments.Where(r => r.Floor <= s.Definition.MaximumFloor && (r.Tags & s.Definition.Members) != 0)
                    .Select(r => new { r.Id, r.Name, Level = service.Level(r.Id) }) }),
            Managers = EmpireManagerCatalog.All.Select(m => new { m.Id, m.Name, m.Role, m.Description, m.RequiredPrestige,
                Assignment = service.ManagerAssignments.GetValueOrDefault(m.Id),
                Available = service.Prestige >= m.RequiredPrestige && service.ManagerSlots > 0
                    && (service.ManagerAssignments.ContainsKey(m.Id) || service.ManagerAssignments.Count < service.ManagerSlots) }),
            Events = EmpireEventCatalog.All.Select(e => new { e.Id, e.Name, e.Description, Cost = Decimal(e.Cost),
                Requirement = e.Requirement.Description, DurationMinutes = e.Duration.TotalMinutes, e.ActivityGoal, CanBuy = service.CanStartEvent(e.Id) }),
            ActiveEvent = service.ActiveEvent is { } ev ? new { ev.Id, ev.EndsAt, ev.Encore, Progress = service.EventActivityProgress } : null,
            Contracts = EmpireContractCatalog.All.Select(c => new { c.Id, c.Name, c.Description, c.RewardDescription, Goal = Decimal(c.Goal) }),
            ActiveContract = service.ActiveContract?.Id, ContractProgress = Decimal(service.ContractProgress), service.CanClaimContract,
            ContractBuff = service.ContractBuff is { } buff ? new { buff.ContractId, RemainingSeconds = Decimal(buff.RemainingSeconds) } : null,
            Statistics = new
            {
                LifetimeCredits = Decimal(state.LifetimeCredits), RunCredits = Decimal(estate.RunCredits),
                LifetimeCashClaimed = Decimal(state.LifetimeCashClaimed), PropertyLifetimeCredits = Decimal(estate.LifetimeCredits),
                LevelsPurchased = estate.LevelsPurchased.ToString(CultureInfo.InvariantCulture),
                AutomatedLevels = estate.AutomatedLevels.ToString(CultureInfo.InvariantCulture),
                EventsCompleted = estate.EventsCompleted.ToString(CultureInfo.InvariantCulture),
                EventEncores = estate.EventEncores.ToString(CultureInfo.InvariantCulture), ContractsCompleted = estate.ContractsCompleted.ToString(CultureInfo.InvariantCulture),
                ProducedByTag = estate.ProducedByTag.ToDictionary(p => p.Key.ToString(), p => Decimal(p.Value)),
            },
            AchievementBoosts = service.ActiveAchievementBoosts.Select(b => new { b.Name, b.RoomId, Percent = Decimal(b.Percent) }),
        };
    }
    internal static object Automation(EmpireAutomationState a) => new { a.Enabled, Reserve = Decimal(a.Reserve),
        SpendPercent = Decimal(a.SpendPercent), a.TargetLevel, a.CheapestMilestone, a.Priorities };
    private static object Breakdown(EmpireProductionBreakdown b) => new
    {
        Base = Decimal(b.Base), Prestige = Decimal(b.Prestige), Achievements = Decimal(b.Achievements),
        Strategy = Decimal(b.Strategy), Synergies = Decimal(b.Synergies), Projects = Decimal(b.Projects),
        Managers = Decimal(b.Managers), Events = Decimal(b.Events), Contracts = Decimal(b.Contracts),
        StrategicBonus = Decimal(b.StrategicBonus), Final = Decimal(b.Final),
    };
    internal static object Preview(EmpireActionPreview p) => new { p.Success, p.Message,
        BudgetBefore = Decimal(p.BudgetBefore), BudgetAfter = Decimal(p.BudgetAfter),
        PortfolioBefore = Decimal(p.PortfolioBefore), PortfolioAfter = Decimal(p.PortfolioAfter),
        p.LevelBefore, p.LevelAfter, RoomBefore = Breakdown(p.RoomBefore), RoomAfter = Breakdown(p.RoomAfter),
        SynergiesAfter = p.SynergiesAfter.Select(s => new { s.Definition.Id, s.Tier, s.MinimumLevel, Bonus = Decimal(s.Bonus) }),
        AutomationAfter = Automation(p.AutomationAfter) };
}
