using System.Security.Cryptography;
using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal sealed record ModernAdventureCopy(
    Func<AdventureDefinition, string> Title,
    Func<AdventureDefinition, string> Subtitle,
    Func<AdventureDefinition, AdventureSceneDefinition, string> SceneTitle,
    Func<AdventureDefinition, AdventureSceneDefinition, string> SceneBody,
    Func<AdventureDefinition, AdventureSceneDefinition, string> SceneLocation,
    Func<AdventureDefinition, AdventureChoiceDefinition, string> Choice,
    Func<AdventureDefinition, AdventureClassDefinition, string> ClassName,
    Func<AdventureDefinition, AdventureClassDefinition, string> ClassDescription,
    Func<AdventureDefinition, CaseNodeDefinition, string> BoardTitle,
    Func<AdventureDefinition, CaseNodeDefinition, string> BoardDescription,
    Func<AdventureDefinition, AdventureItemDefinition, string> ItemName,
    Func<AdventureDefinition, AdventureItemDefinition, string> ItemDescription,
    Func<AdventureDefinition, AdventureStatusDefinition, string> ConditionName)
{
    internal static ModernAdventureCopy English { get; } = new(
        a => a.Title, a => a.Subtitle, (_, s) => s.Title, (_, s) => s.Body,
        (_, s) => s.Location ?? "", (_, c) => c.Text, (_, c) => c.Name,
        (_, c) => c.Description, (_, n) => n.Title, (_, n) => n.Description,
        (_, i) => i.Name, (_, i) => i.Description, (_, c) => c.Name);
}

/// <summary>
/// Story, character, inventory, clues and encounters are committed through the established
/// AdventureProgressState writer. The React UI only receives a read-only snapshot.
/// </summary>
internal sealed class ModernAdventureService
{
    private AdventureProgressState progress;
    private readonly Action<AdventureProgressState> persist;
    private readonly Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> award;
    private readonly Func<string?> playerName;
    private readonly ModernAdventureCopy text;
    private readonly Func<string, string> localize;
    private readonly Func<(bool Enabled, double Volume)> musicSettings;
    private readonly Action<bool, double> saveMusic;
    private string selectedId, screen = "library";
    private string? playingId;
    private ModernAdventureEncounter? encounter;
    private AdventureCheckResult? lastRoll;
    private string notice = "", metaNotice = "";

    internal ModernAdventureService(AdventureProgressState saved,
        Action<AdventureProgressState> persist,
        Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> award,
        Func<string?>? playerName = null, ModernAdventureCopy? text = null,
        Func<string, string>? localize = null,
        Func<(bool Enabled, double Volume)>? musicSettings = null,
        Action<bool, double>? saveMusic = null)
    {
        this.progress = Clone(saved);
        this.persist = persist;
        this.award = award;
        this.playerName = playerName ?? (() => "Player");
        this.text = text ?? ModernAdventureCopy.English;
        this.localize = localize ?? (value => value);
        this.musicSettings = musicSettings ?? (() => (true, .24));
        this.saveMusic = saveMusic ?? ((_, _) => { });
        selectedId = AdventureCatalog.All.Any(a => a.Id == progress.LastAdventureId)
            ? progress.LastAdventureId! : AdventureCatalog.All[0].Id;
    }

    internal string Screen => screen;
    internal bool DetachedBoardOpen { get; set; }
    internal bool HasCaseBoard => playingId is not null && Definition(playingId).CaseBoard is not null;
    internal AdventureRunState? ActiveRun => playingId is not null &&
        progress.Runs.TryGetValue(playingId, out AdventureRunState? run) ? run : null;
    internal string SelectedId => selectedId;

    internal object Snapshot()
    {
        AdventureDefinition selected = Definition(selectedId);
        object? run = null;
        if (playingId is not null && progress.Runs.TryGetValue(playingId, out AdventureRunState? state))
        {
            AdventureDefinition definition = Definition(playingId);
            var engine = new AdventureEngine(definition, CloneState(state));
            var scene = engine.CurrentScene;
            AdventureCheckDefinition? check = engine.PendingCheck;
            var conditions = engine.State.ActiveConditions.Values
                .Where(s => s.Remaining > 0).Select(s => new
                {
                    s.ConditionId, s.Remaining, s.Stacks,
                    Name = engine.ConditionDefinition(s.ConditionId) is { } entry
                        ? text.ConditionName(definition, entry) : s.ConditionId,
                }).ToArray();
            run = new
            {
                engine.State.RunId, engine.State.AdventureId, engine.State.Completed,
                engine.State.EndingId, engine.State.RollsMade,
                Scene = new
                {
                    scene.Id, Title = text.SceneTitle(definition, scene),
                    Body = text.SceneBody(definition, scene), scene.Speaker,
                    Location = text.SceneLocation(definition, scene),
                    Act = scene.ActId is { } act && definition.Acts?.TryGetValue(act, out string? label) == true
                        ? localize(label) : text.Title(definition),
                },
                Choices = engine.State.Completed || check is not null
                    ? Array.Empty<object>()
                    : engine.AvailableChoices().Select(choice =>
                    {
                        AdventureEncounterCatalog.TryGetTriggeredByChoice(
                            definition.Id, choice, out AdventureEncounterDefinition? triggered);
                        return (object)new
                        {
                            choice.Id, Text = text.Choice(definition, choice),
                            Check = choice.Check is null ? null : new
                            {
                                Ability = AdventureCharacterCatalog.AbilityLabel(definition, choice.Check.Ability),
                                choice.Check.Difficulty, choice.Check.SkillId,
                            },
                            Encounter = triggered?.Type.ToString(),
                            Events = (choice.Effects ?? []).Concat(choice.SuccessEffects ?? [])
                                .Concat(choice.FailureEffects ?? []).Select(e => e.Kind.ToString()).Distinct().ToArray(),
                        };
                    }).ToArray(),
                Pending = check is null ? null : new
                {
                    Ability = AdventureCharacterCatalog.AbilityLabel(definition, check.Ability),
                    check.Difficulty, check.SkillId, Mode = check.RollMode.ToString(),
                    Modifier = engine.State.Character.AbilityModifier(check.Ability) +
                        engine.State.Character.SkillBonus(check.SkillId) + check.Modifier,
                },
                LastRoll = lastRoll is null ? null : new
                {
                    lastRoll.FirstRoll, lastRoll.SecondRoll, lastRoll.ChosenRoll,
                    lastRoll.AbilityModifier, lastRoll.SkillBonus, lastRoll.AdditionalModifier,
                    lastRoll.Total, lastRoll.Difficulty, Outcome = lastRoll.Outcome.ToString(),
                },
                Character = new
                {
                    engine.State.Character.Name, engine.State.Character.ClassId,
                    engine.State.Character.Level, engine.State.Character.Experience,
                    engine.State.Character.MaxHitPoints, engine.State.Character.CurrentHitPoints,
                    engine.State.Character.Armor, engine.State.Coins,
                    Abilities = Enum.GetValues<AdventureAbility>().Select(ability => new
                    {
                        Id = ability.ToString(),
                        Name = AdventureCharacterCatalog.AbilityShortLabel(definition, ability),
                        Score = engine.State.Character.AbilityScore(ability),
                        Modifier = engine.State.Character.AbilityModifier(ability),
                    }).ToArray(),
                    Skills = engine.State.Character.Skills.Values.OrderBy(v => v.Name)
                        .Select(skill => new
                        {
                            skill.Id, Name = localize(skill.Name),
                            Bonus = engine.State.Character.AbilityModifier(skill.Ability) +
                                skill.ProficiencyBonus,
                        }).ToArray(),
                },
                Inventory = engine.State.ItemStacks.Values.Where(i => i.Quantity > 0)
                    .OrderBy(i => i.ItemId).Select(item =>
                    {
                        AdventureItemDefinition? def = engine.ItemDefinition(item.ItemId);
                        return new
                        {
                            item.ItemId, item.Quantity,
                            Name = def is null ? item.ItemId : text.ItemName(definition, def),
                            Description = def is null ? item.ItemId : text.ItemDescription(definition, def),
                            Kind = def?.Kind.ToString() ?? "Item",
                            UseChoices = check is null && !engine.State.Completed
                                ? engine.AvailableChoices().Where(choice => (choice.Conditions ?? [])
                                    .Any(c => c.Kind == AdventureConditionKind.ItemOwned &&
                                        c.Expected && c.Key == item.ItemId))
                                    .Select(choice => new
                                    { choice.Id, Text = text.Choice(definition, choice) }).ToArray()
                                : Array.Empty<object>(),
                        };
                    }).ToArray(),
                Conditions = conditions,
                Feedback = engine.State.PendingFeedback.TakeLast(8).Select(f => new
                {
                    Type = f.Kind.ToString(), f.Title, Description = f.Detail,
                }).ToArray(),
                Board = BuildBoard(definition, engine.State.CaseBoard),
                Statistics = new
                {
                    engine.State.Statistics.ScenesVisited, engine.State.Statistics.ChoicesMade,
                    engine.State.Statistics.ChecksSucceeded, engine.State.Statistics.ChecksFailed,
                    engine.State.Statistics.EncountersWon, engine.State.Statistics.EncountersLost,
                },
            };
        }

        return new
        {
            Screen = screen, SelectedAdventureId = selectedId,
            Adventures = AdventureCatalog.All.Select(a =>
            {
                progress.Runs.TryGetValue(a.Id, out AdventureRunState? saved);
                return new
                {
                    a.Id, Title = text.Title(a), Subtitle = text.Subtitle(a),
                    Acts = a.Acts?.Count ?? 0, Scenes = a.Scenes.Count,
                    Status = saved is null ? "unplayed" : saved.Completed ? "completed" : "active",
                    RunId = saved?.RunId, LastSceneId = saved?.CurrentSceneId,
                };
            }).ToArray(),
            Classes = AdventureCharacterCatalog.AllFor(selected).Select(c => new
            {
                c.Id, Name = text.ClassName(selected, c),
                Description = text.ClassDescription(selected, c),
                c.MaxHitPoints, c.Armor, Special = c.SpecialMechanicName ?? "",
                Abilities = Enum.GetValues<AdventureAbility>().Select(ability => new
                {
                    Id = ability.ToString(),
                    Name = AdventureCharacterCatalog.AbilityShortLabel(selected, ability),
                    Score = c.Abilities.GetValueOrDefault(ability, 10),
                }).ToArray(),
            }).ToArray(),
            DefaultPlayerName = playerName() ?? "Player",
            MusicEnabled = musicSettings().Enabled,
            MusicVolume = musicSettings().Volume,
            Started = progress.Runs.Count,
            Completed = progress.Runs.Values.Count(r => r.Completed),
            Run = run, Encounter = encounter?.Snapshot(),
            Notice = notice, MetaNotice = metaNotice,
        };
    }

    private object? BuildBoard(AdventureDefinition definition, CaseBoardState state)
    {
        if (definition.CaseBoard is not { } board) return null;
        var known = board.Nodes.Where(n => state.Nodes.TryGetValue(n.Id, out CaseNodeState? s) &&
            s.IsKnown).ToArray();
        var visible = known.Select((n, i) =>
        {
            CaseNodeState s = state.Nodes[n.Id];
            var position = state.Layout.NodePositions.GetValueOrDefault(n.Id);
            return new
            {
                n.Id, Category = n.Category.ToString(),
                Title = text.BoardTitle(definition, n),
                Description = text.BoardDescription(definition, n),
                Subtitle = localize(n.Subtitle ?? ""),
                Reliability = s.Reliability.ToString(),
                Lead = s.LeadState.ToString(), Theory = s.TheoryState.ToString(),
                Facts = (n.KnownFacts ?? []).Select(localize).ToArray(),
                Quotes = (n.StoryQuotes ?? []).Select(localize).ToArray(),
                X = position?.X ?? n.X ?? 120d + (i % 4) * 325d,
                Y = position?.Y ?? n.Y ?? 120d + (i / 4) * 200d,
            };
        }).ToArray();
        var keys = known.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var edges = board.Connections.Where(e =>
            keys.Contains(e.FromNodeId) && keys.Contains(e.ToNodeId) &&
            state.Connections.TryGetValue(e.Id, out CaseConnectionState? saved) && saved.IsKnown)
            .Select(e => new
            {
                e.Id, From = e.FromNodeId, To = e.ToNodeId,
                Type = e.Type.ToString(),
                Status = state.Connections[e.Id].Status.ToString(),
                Label = localize(e.Label ?? ""),
            }).ToArray();
        return new
        {
            Title = localize(board.Title), Nodes = visible, Connections = edges,
            state.Layout.Zoom, state.Layout.PanX, state.Layout.PanY,
        };
    }

    internal (bool Success, string Message) Execute(ModernAdventureCommand command)
    {
        switch (command.Action)
        {
            case "adventure.music":
                if (screen != "library") return (false, "Music controls are only available in the library.");
                saveMusic(command.Enabled!.Value, command.Volume!.Value);
                return (true, "Music settings saved.");
            case "adventure.select":
                selectedId = command.AdventureId!;
                var selected = Clone(progress);
                selected.LastAdventureId = selectedId;
                persist(selected);
                progress = selected;
                return (true, "Adventure selected.");
            case "adventure.start":
                return Start(command.AdventureId!, command.ClassId!, command.ConfirmReplace == true);
            case "adventure.restart":
            {
                var current = progress.Runs.GetValueOrDefault(command.AdventureId!);
                if (current is null) return (false, "Start an adventure first.");
                if (!current.Completed && command.ConfirmReplace != true)
                    return (false, "Confirm replacement of the unfinished story.");
                return Start(command.AdventureId!, current.Character.ClassId, true);
            }
            case "adventure.resume":
                return Resume(command.AdventureId!);
            case "adventure.choice":
                if (screen != "story" || playingId is null ||
                    !progress.Runs.TryGetValue(playingId, out AdventureRunState? choiceRun) ||
                    choiceRun.Completed) return (false, "No active story choice.");
                var choiceEngine = new AdventureEngine(Definition(playingId), CloneState(choiceRun));
                if (choiceEngine.PendingCheck is not null ||
                    !choiceEngine.AvailableChoices().Any(c => c.Id == command.ChoiceId))
                    return (false, "Choice is not available in this scene.");
                return WithEngine(e => e.SelectChoice(command.ChoiceId!));
            case "adventure.roll":
                return Roll();
            case "adventure.board.open":
                if (screen != "story" || playingId is null ||
                    Definition(playingId).CaseBoard is null)
                    return (false, "No investigation board in this scene.");
                screen = "board";
                return (true, "Board opened.");
            case "adventure.board.close":
                if (screen != "board") return (false, "Board is not open.");
                screen = "story";
                return (true, "Story restored.");
            case "adventure.board.move":
                return MoveBoard(command.NodeId!, command.X!.Value, command.Y!.Value);
            case "adventure.board.view":
                return ViewBoard(command.Zoom!.Value, command.PanX!.Value, command.PanY!.Value);
            case "adventure.encounter.hit":
                return EncounterAction(e => e.Hit());
            case "adventure.encounter.stand":
                return EncounterAction(e => e.Stand());
            case "adventure.encounter.roll":
                return EncounterAction(e => e.Roll());
            case "adventure.encounter.hold":
                return EncounterAction(e => e.Hold(command.Index!.Value));
            case "adventure.encounter.bank":
                return EncounterAction(e => e.Bank());
            case "adventure.encounter.finish":
                return FinishEncounter();
            case "adventure.back":
                return Back();
            case "adventure.exit":
                if (screen != "library") return (false, "Return to the adventure library first.");
                return (true, "Adventure closed.");
            default:
                return (false, "Unsupported adventure command.");
        }
    }

    private (bool, string) Start(string adventureId, string classId, bool replace)
    {
        AdventureDefinition definition = Definition(adventureId);
        if (!AdventureCharacterCatalog.AllFor(definition).Any(c => c.Id == classId))
            return (false, "Unknown character class.");
        if (progress.Runs.TryGetValue(adventureId, out AdventureRunState? previous) &&
            !previous.Completed && !replace)
            return (false, "Confirm replacement of your unfinished story.");
        AdventureCharacterState character = AdventureCharacterCatalog.Create(
            definition, classId, playerName());
        AdventureEngine engine = AdventureEngine.StartNew(definition, character);
        var updated = Clone(progress);
        updated.Runs[adventureId] = engine.State;
        updated.LastAdventureId = adventureId;
        persist(updated);
        progress = updated;
        selectedId = playingId = adventureId;
        lastRoll = null;
        metaNotice = notice = "";
        OpenScene(engine);
        CompleteIfNeeded(definition, engine.State);
        return (true, "Adventure started.");
    }

    private (bool, string) Resume(string adventureId)
    {
        if (!progress.Runs.TryGetValue(adventureId, out AdventureRunState? saved))
            return (false, "No saved adventure.");
        AdventureDefinition definition = Definition(adventureId);
        AdventureRunState state = CloneState(saved);
        var engine = new AdventureEngine(definition, state);
        var updated = Clone(progress);
        updated.Runs[adventureId] = engine.State;
        updated.LastAdventureId = adventureId;
        persist(updated);
        progress = updated;
        selectedId = playingId = adventureId;
        lastRoll = null;
        notice = "";
        OpenScene(engine);
        CompleteIfNeeded(definition, engine.State);
        return (true, "Saved adventure resumed.");
    }

    private (bool, string) WithEngine(Action<AdventureEngine> apply, bool requirePending = false)
    {
        if (screen != "story" || playingId is null || !progress.Runs.TryGetValue(playingId, out AdventureRunState? saved))
            return (false, "No active story scene.");
        AdventureDefinition definition = Definition(playingId);
        var updated = Clone(progress);
        var engine = new AdventureEngine(definition, updated.Runs[playingId]);
        if (AdventureEncounterCatalog.TryGet(definition.Id, engine.CurrentScene.Id, out _))
            return (false, "Finish the encounter first.");
        if (engine.State.Completed) return (false, "Story already complete.");
        if (requirePending && engine.PendingCheck is null) return (false, "No pending dice check.");
        apply(engine);
        updated.Runs[playingId] = engine.State;
        persist(updated);
        progress = updated;
        OpenScene(engine);
        CompleteIfNeeded(definition, engine.State);
        return (true, "Story choice saved.");
    }

    private (bool, string) Roll()
    {
        AdventureCheckResult? result = null;
        var response = WithEngine(engine =>
        {
            AdventureCheckDefinition check = engine.PendingCheck
                ?? throw new InvalidOperationException("No pending check.");
            int first = RandomNumberGenerator.GetInt32(1, 21);
            int? second = check.RollMode == AdventureRollMode.Normal
                ? null : RandomNumberGenerator.GetInt32(1, 21);
            result = engine.ResolvePendingCheck(first, second);
        }, requirePending: true);
        if (response.Item1 && result is not null) lastRoll = result;
        return response;
    }

    private (bool, string) MoveBoard(string nodeId, double x, double y)
    {
        if ((screen != "board" && !(screen == "story" && DetachedBoardOpen)) || playingId is null || !HasCaseBoard)
            return (false, "Board not open.");
        AdventureDefinition definition = Definition(playingId);
        if (definition.CaseBoard is not { } board ||
            !board.Nodes.Any(n => n.Id == nodeId)) return (false, "Unknown clue.");
        var updated = Clone(progress);
        AdventureRunState state = updated.Runs[playingId];
        state.CaseBoard = AdventureCaseBoard.Initialize(board, state.CaseBoard);
        if (!state.CaseBoard.Nodes.TryGetValue(nodeId, out CaseNodeState? item) || !item.IsKnown)
            return (false, "Undiscovered clue.");
        state.CaseBoard.Layout.NodePositions[nodeId] = new CaseBoardNodePosition { X = x, Y = y };
        state.CaseBoard.Layout.LayoutVersion = Math.Max(3, state.CaseBoard.Layout.LayoutVersion);
        persist(updated);
        progress = updated;
        return (true, "Clue position saved.");
    }

    private (bool, string) ViewBoard(double zoom, double x, double y)
    {
        if ((screen != "board" && !(screen == "story" && DetachedBoardOpen)) || playingId is null || !HasCaseBoard)
            return (false, "Board not open.");
        var updated = Clone(progress);
        AdventureRunState run = updated.Runs[playingId];
        run.CaseBoard = AdventureCaseBoard.Initialize(Definition(playingId).CaseBoard, run.CaseBoard);
        BoardLayoutState layout = run.CaseBoard.Layout;
        layout.Zoom = zoom;
        layout.PanX = x;
        layout.PanY = y;
        persist(updated);
        progress = updated;
        return (true, "Board view saved.");
    }

    private (bool, string) EncounterAction(Func<ModernAdventureEncounter, bool> apply)
    {
        if (screen != "encounter" || encounter is null)
            return (false, "No active tabletop encounter.");
        return apply(encounter) ? (true, "Encounter move accepted.") :
            (false, "This encounter move is unavailable.");
    }

    private (bool, string) FinishEncounter()
    {
        if (screen != "encounter" || encounter?.Outcome is not { } outcome || playingId is null)
            return (false, "The encounter must be settled first.");
        AdventureDefinition definition = Definition(playingId);
        var updated = Clone(progress);
        var engine = new AdventureEngine(definition, updated.Runs[playingId]);
        if (!AdventureEncounterCatalog.TryGet(definition.Id, engine.CurrentScene.Id,
            out AdventureEncounterDefinition? current) ||
            current is null || current.Id != encounter.Definition.Id)
            return (false, "Encounter scene changed.");
        engine.SelectChoice(current.ChoiceIdFor(outcome));
        engine.RecordEncounter(current.Type, outcome);
        updated.Runs[playingId] = engine.State;
        persist(updated);
        progress = updated;
        encounter = null;
        OpenScene(engine);
        CompleteIfNeeded(definition, engine.State);
        return (true, "Encounter result saved.");
    }

    private (bool, string) Back()
    {
        if (screen == "encounter") return (false, "Finish the encounter before leaving.");
        if (screen == "board") { screen = "story"; return (true, "Back to story."); }
        if (screen is "story" or "complete")
        {
            playingId = null; lastRoll = null; encounter = null;
            screen = "library";
            return (true, "Back to adventure library.");
        }
        return (false, "Already in the adventure library.");
    }

    private void OpenScene(AdventureEngine engine)
    {
        if (engine.State.Completed)
        {
            encounter = null;
            screen = "complete";
            return;
        }
        if (AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id,
            out AdventureEncounterDefinition? definition) && definition is not null)
        {
            if (encounter?.Definition.Id != definition.Id)
                encounter = new ModernAdventureEncounter(definition);
            screen = "encounter";
        }
        else
        {
            encounter = null;
            screen = "story";
        }
    }

    private void CompleteIfNeeded(AdventureDefinition definition, AdventureRunState state)
    {
        if (!state.Completed) return;
        try
        {
            AdventureCompletionMetaResult result = award(definition, state);
            metaNotice = result.Recorded
                ? "Adventure completion and progression recorded."
                : "Completion already recorded for this character run.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            metaNotice = "Adventure saved. Progression will retry when this run is reopened.";
        }
    }

    private static AdventureDefinition Definition(string id) =>
        AdventureCatalog.TryGet(id, out AdventureDefinition? definition) && definition is not null
            ? definition : throw new ArgumentException("Unknown adventure.", nameof(id));

    internal static AdventureProgressState Clone(AdventureProgressState source)
    {
        // Complete native JSON round-trip retains the existing v4 save schema,
        // caseboard dictionaries, conditions, character and scene statistics.
        string json = JsonSerializer.Serialize(AdventureProgressState.Normalize(source));
        return AdventureProgressState.Normalize(JsonSerializer.Deserialize<AdventureProgressState>(json));
    }

    internal static AdventureRunState CloneState(AdventureRunState source)
        => AdventureRunState.Normalize(JsonSerializer.Deserialize<AdventureRunState>(
            JsonSerializer.Serialize(source)));
}
