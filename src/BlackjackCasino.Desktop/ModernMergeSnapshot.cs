using BlackjackCasino.Core;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

/// <summary>Presentation-only projection of the durable MergeBoard and catalog.</summary>
internal static class ModernMergeSnapshot
{
    internal static object Create(MergeBoard board, PlayerProfileState? profile,
        bool soundEnabled, bool animationsEnabled, string feedback,
        long playerXp, long seasonXp, bool progressionSaveFailed, MergeActionResult? lastAction,
        Func<string, string> translate, UiLanguagePreference language)
    {
        ArgumentNullException.ThrowIfNull(translate);
        MergeCatalog catalog = MergeCatalog.Default;
        MergeGameState state = board.Snapshot;
        MergeArea area = MergeProgression.ActiveArea(catalog, state.RenovationPoints);
        MergeMilestone current = MergeProgression.Current(area, state.RenovationPoints);
        MergeMilestone? next = MergeProgression.Next(area, state.RenovationPoints);
        string Text(string key) => translate(key);
        return new
        {
            Language = language == UiLanguagePreference.German ? "de" : "en",
            state.SchemaVersion,
            state.Revision,
            Columns = MergeGameState.Columns,
            Rows = MergeGameState.Rows,
            Capacity = MergeGameState.Capacity,
            RenovationPoints = state.RenovationPoints,
            state.TotalMerges,
            state.HighestTierCreated,
            state.UnlockedMilestones,
            Items = state.Items.Select(item => new
            {
                Id = item.Id.ToString("D"), item.DefinitionId, item.Position, item.Status,
            }).ToArray(),
            Definitions = catalog.Items.Values.OrderBy(x => x.LineId).ThenBy(x => x.Tier)
                .Select(item => new
                {
                    item.Id, Name = Text(item.DisplayName), item.LineId, item.Tier,
                    item.MaxTier, item.MergeResult, item.Visual.Color, item.Visual.Mark,
                    item.Visual.StackCount, item.Tags,
                }).ToArray(),
            Producers = catalog.Producers.Values.OrderBy(x => x.UnlockPoints)
                .Select(producer => new
                {
                    producer.Id, Name = Text(producer.DisplayName),
                    producer.OutputDefinitionId, producer.UnlockPoints,
                    Unlocked = state.RenovationPoints >= producer.UnlockPoints,
                    Available = board.CanSpawn(producer.Id),
                    SpawnCount = state.Producers.FirstOrDefault(p => p.DefinitionId == producer.Id)?.SpawnCount ?? 0,
                }).ToArray(),
            Orders = state.Orders.Select(order =>
            {
                MergeOrderDefinition definition = catalog.Orders.Single(x => x.Id == order.DefinitionId);
                MergeArea orderArea = catalog.Areas.Single(x => x.Id == definition.AreaId);
                bool unlocked = state.RenovationPoints >= orderArea.Milestones[0].Points;
                bool available = unlocked && MergeOrders.SelectRequired(state, definition) is not null;
                return new
                {
                    Id = order.Id.ToString("D"),
                    order.DefinitionId,
                    definition.AreaId,
                    definition.Reward,
                    Available = available,
                    Visible = unlocked && (definition.AreaId == area.Id || available),
                    Requirements = definition.Requirements.Select(requirement => new
                    {
                        requirement.DefinitionId, requirement.Count,
                        Available = state.Items.Count(item => item.DefinitionId == requirement.DefinitionId),
                    }).ToArray(),
                };
            }).ToArray(),
            Restoration = new
            {
                AreaId = area.Id,
                AreaName = Text(area.DisplayName),
                AreaIndex = catalog.Areas.Index().First(x => x.Item.Id == area.Id).Index + 1,
                AreaCount = catalog.Areas.Count,
                Current = new { current.Id, Name = Text(current.DisplayName), current.Points },
                Next = next is null ? null : new { next.Id, Name = Text(next.DisplayName), next.Points },
                PointsUntilNext = MergeProgression.PointsUntilNext(area, state.RenovationPoints),
                UpgradeProgress = MergeProgression.UpgradeProgress(area, state.RenovationPoints),
                Areas = catalog.Areas.Select((room, index) =>
                {
                    MergeMilestone roomCurrent = MergeProgression.Current(room, state.RenovationPoints);
                    MergeMilestone? roomNext = MergeProgression.Next(room, state.RenovationPoints);
                    return new
                    {
                        AreaId = room.Id,
                        AreaName = Text(room.DisplayName),
                        AreaIndex = index + 1,
                        Current = new { roomCurrent.Id, Name = Text(roomCurrent.DisplayName), roomCurrent.Points },
                        Next = roomNext is null ? null : new
                        {
                            roomNext.Id, Name = Text(roomNext.DisplayName), roomNext.Points,
                        },
                        PointsUntilNext = MergeProgression.PointsUntilNext(room, state.RenovationPoints),
                        UpgradeProgress = MergeProgression.UpgradeProgress(room, state.RenovationPoints),
                        Milestones = room.Milestones.Select(milestone => new
                        {
                            milestone.Id, Name = Text(milestone.DisplayName), milestone.Points,
                            Unlocked = state.RenovationPoints >= milestone.Points,
                        }).ToArray(),
                    };
                }).ToArray(),
                Milestones = area.Milestones.Select(milestone => new
                {
                    milestone.Id, Name = Text(milestone.DisplayName), milestone.Points,
                    Unlocked = state.RenovationPoints >= milestone.Points,
                }).ToArray(),
            },
            Profile = profile is null ? null : new
            {
                profile.Level, profile.LevelExperience, profile.Prestige, profile.Coins,
                NextLevelExperience = profile.Level >= ProgressionCurve.MaximumLevel
                    ? 0 : ProgressionCurve.ExperienceForNextLevel(profile.Level),
            },
            soundEnabled, animationsEnabled, feedback,
            playerXp, seasonXp, progressionSaveFailed,
            LastAction = lastAction is null ? null : new
            {
                Outcome = lastAction.Outcome.ToString(),
                lastAction.Position,
                lastAction.FromPosition,
                lastAction.DefinitionId,
                lastAction.SourceDefinitionId,
                lastAction.Reward,
                lastAction.Milestones,
            },
            Labels = new
            {
                Title = Text("Merge.Title"), Eyebrow = Text("Merge.Eyebrow"),
                Back = Text("Merge.Back"), Producer = Text("Merge.Producer"),
                NoEnergy = Text("Merge.NoEnergy"), ProducerHint = Text("Merge.ProducerHint"),
                Orders = Text("Merge.Orders"), Deliver = Text("Merge.Deliver"),
                Waiting = Text("Merge.Waiting"), HowToPlay = Text("Merge.HowToPlay"),
                SelectionHelp = Text("Merge.SelectionHelp"), SelectFirst = Text("Merge.SelectFirst"),
                GlobalProgress = Text("Merge.GlobalProgress"), NextUpgrade = Text("Merge.NextUpgrade"),
                AreaComplete = Text("Merge.AreaComplete"), SaveFailed = Text("Merge.SaveFailed"),
                ProgressionSaveFailed = Text("Merge.ProgressionSaveFailed"),
                XpHint = Text("Merge.XpHint"), ProfileUnavailable = Text("Merge.ProfileUnavailable"),
            },
        };
    }
}
