namespace BlackjackCasino.Core.Adventure;

public static class AdventureCheckResolver
{
    public static AdventureCheckResult Resolve(
        AdventureCheckDefinition check,
        AdventureCharacterState character,
        int firstRoll,
        int? secondRoll = null)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(character);
        ValidateRoll(firstRoll, nameof(firstRoll));

        if (check.RollMode != AdventureRollMode.Normal)
        {
            if (secondRoll is null)
                throw new ArgumentException("Advantage and disadvantage checks require a second d20 roll.", nameof(secondRoll));
            ValidateRoll(secondRoll.Value, nameof(secondRoll));
        }

        int chosen = check.RollMode switch
        {
            AdventureRollMode.Advantage => Math.Max(firstRoll, secondRoll!.Value),
            AdventureRollMode.Disadvantage => Math.Min(firstRoll, secondRoll!.Value),
            _ => firstRoll,
        };

        int abilityModifier = character.AbilityModifier(check.Ability);
        int skillBonus = character.SkillBonus(check.SkillId);
        int total = chosen + abilityModifier + skillBonus + check.Modifier;
        AdventureCheckOutcome outcome = chosen switch
        {
            20 => AdventureCheckOutcome.CriticalSuccess,
            1 => AdventureCheckOutcome.CriticalFailure,
            _ when total >= check.Difficulty => AdventureCheckOutcome.Success,
            _ => AdventureCheckOutcome.Failure,
        };

        return new AdventureCheckResult(
            firstRoll,
            secondRoll,
            chosen,
            abilityModifier,
            skillBonus,
            check.Modifier,
            total,
            check.Difficulty,
            outcome);
    }

    private static void ValidateRoll(int value, string parameter)
    {
        if (value is < 1 or > 20)
            throw new ArgumentOutOfRangeException(parameter, value, "A d20 roll must be between 1 and 20.");
    }
}

public sealed class AdventureEngine
{
    private readonly IReadOnlyDictionary<string, AdventureSceneDefinition> scenes;
    private readonly IReadOnlyDictionary<string, AdventureItemDefinition> items;
    private readonly IReadOnlyDictionary<string, AdventureStatusDefinition> conditions;

    public AdventureEngine(AdventureDefinition definition, AdventureRunState state)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        State = AdventureRunState.Normalize(state);
        AdventureDefinitionValidator.Validate(definition);
        scenes = definition.Scenes.ToDictionary(scene => scene.Id, StringComparer.OrdinalIgnoreCase);
        items = (definition.Items ?? []).ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        conditions = (definition.Conditions ?? []).ToDictionary(condition => condition.Id, StringComparer.OrdinalIgnoreCase);
        State.CaseBoard = AdventureCaseBoard.Initialize(definition.CaseBoard, State.CaseBoard);

        if (!string.Equals(State.AdventureId, definition.Id, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Run belongs to '{State.AdventureId}', not '{definition.Id}'.", nameof(state));
        if (!scenes.ContainsKey(State.CurrentSceneId))
            throw new ArgumentException($"Run references missing scene '{State.CurrentSceneId}'.", nameof(state));
    }

    public AdventureDefinition Definition { get; }
    public AdventureRunState State { get; }
    public AdventureSceneDefinition CurrentScene => scenes[State.CurrentSceneId];

    public AdventureCheckDefinition? PendingCheck
    {
        get
        {
            if (string.IsNullOrWhiteSpace(State.PendingChoiceId)) return null;
            AdventureCheckDefinition? check = FindChoice(State.PendingChoiceId).Check;
            return check is null ? null : ApplyConditionRollMode(check);
        }
    }

    public static AdventureEngine StartNew(AdventureDefinition definition, AdventureCharacterState character)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(character);
        AdventureDefinitionValidator.Validate(definition);

        var state = new AdventureRunState
        {
            AdventureId = definition.Id,
            AdventureVersion = definition.Version,
            CurrentSceneId = definition.StartSceneId,
            Character = character,
        };
        state.Statistics.ScenesVisited = 1;
        state.Statistics.UniqueSceneIds.Add(definition.StartSceneId);
        var engine = new AdventureEngine(definition, state);
        engine.ApplyEffects(engine.CurrentScene.OnEnterEffects);
        if (engine.CurrentScene.IsEnding) engine.CompleteCurrentScene();
        return engine;
    }

    public IReadOnlyList<AdventureChoiceDefinition> AvailableChoices() =>
        (CurrentScene.Choices ?? []).Where(choice => (choice.Conditions ?? []).All(Evaluate)).ToArray();

    public IReadOnlyList<AdventureFeedbackEvent> DrainFeedback()
    {
        AdventureFeedbackEvent[] feedback = State.PendingFeedback.ToArray();
        State.PendingFeedback.Clear();
        return feedback;
    }

    public AdventureItemDefinition? ItemDefinition(string itemId) =>
        items.TryGetValue(itemId, out AdventureItemDefinition? item) ? item : null;

    public AdventureStatusDefinition? ConditionDefinition(string conditionId) =>
        conditions.TryGetValue(conditionId, out AdventureStatusDefinition? condition) ? condition : null;

    public bool Evaluate(AdventureCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return condition.Kind switch
        {
            AdventureConditionKind.Flag =>
                (State.Flags.TryGetValue(condition.Key, out bool flag) && flag) == condition.Expected,
            AdventureConditionKind.ItemOwned =>
                HasItem(condition.Key) == condition.Expected,
            AdventureConditionKind.AbilityMinimum when condition.Ability is AdventureAbility ability =>
                State.Character.AbilityScore(ability) >= condition.Value,
            AdventureConditionKind.SkillMinimum =>
                State.Character.SkillBonus(condition.Key) >= condition.Value,
            AdventureConditionKind.NpcRelationMinimum =>
                State.Npcs.TryGetValue(condition.Key, out AdventureNpcState? npc) && npc.Relation >= condition.Value,
            AdventureConditionKind.ActiveCondition =>
                State.ActiveConditions.ContainsKey(condition.Key) == condition.Expected,
            AdventureConditionKind.CoinsMinimum => State.Coins >= condition.Value,
            AdventureConditionKind.CounterMinimum =>
                State.Counters.TryGetValue(condition.Key, out int counter) && counter >= condition.Value,
            AdventureConditionKind.CaseNodeKnown =>
                (State.CaseBoard.Nodes.TryGetValue(condition.Key, out CaseNodeState? nodeState) && nodeState.IsKnown) == condition.Expected,
            AdventureConditionKind.CaseConnectionKnown =>
                (State.CaseBoard.Connections.TryGetValue(condition.Key, out CaseConnectionState? connectionState) && connectionState.IsKnown) == condition.Expected,
            AdventureConditionKind.LeadResolved =>
                (State.CaseBoard.Nodes.TryGetValue(condition.Key, out CaseNodeState? leadState) &&
                 leadState.IsKnown && leadState.LeadState == CaseLeadState.Resolved) == condition.Expected,
            AdventureConditionKind.TheoryKnown =>
                (State.CaseBoard.Nodes.TryGetValue(condition.Key, out CaseNodeState? theoryState) && theoryState.IsKnown) == condition.Expected,
            _ => false,
        };
    }

    public void SelectChoice(string choiceId)
    {
        if (State.Completed) throw new InvalidOperationException("This adventure run is complete.");
        if (!string.IsNullOrWhiteSpace(State.PendingChoiceId))
            throw new InvalidOperationException("Resolve the pending check before choosing another action.");

        AdventureChoiceDefinition choice = FindChoice(choiceId);
        if (!(choice.Conditions ?? []).All(Evaluate))
            throw new InvalidOperationException($"Choice '{choiceId}' is not currently available.");

        State.Statistics.ChoicesMade++;
        if (choice.Check is not null)
        {
            State.PendingChoiceId = choice.Id;
            return;
        }

        AdvanceConditions(AdventureDurationUnit.Scene);
        ApplyEffects(choice.Effects);
        MoveTo(choice.NextSceneId ?? throw new InvalidOperationException($"Choice '{choice.Id}' has no destination."));
    }

    public AdventureCheckResult ResolvePendingCheck(int firstRoll, int? secondRoll = null)
    {
        if (string.IsNullOrWhiteSpace(State.PendingChoiceId))
            throw new InvalidOperationException("There is no pending check.");

        AdventureChoiceDefinition choice = FindChoice(State.PendingChoiceId);
        AdventureCheckDefinition baseCheck = choice.Check ??
            throw new InvalidOperationException($"Pending choice '{choice.Id}' does not contain a check.");
        AdventureCheckDefinition check = ApplyConditionRollMode(baseCheck);
        AdventureCheckResult result = AdventureCheckResolver.Resolve(check, State.Character, firstRoll, secondRoll);

        State.RollsMade++;
        if (result.ChosenRoll == 20)
        {
            State.NaturalTwenties++;
            State.Statistics.NaturalTwenties++;
        }
        if (result.ChosenRoll == 1)
        {
            State.NaturalOnes++;
            State.Statistics.NaturalOnes++;
        }
        if (result.Succeeded) State.Statistics.ChecksSucceeded++;
        else State.Statistics.ChecksFailed++;
        State.PendingChoiceId = null;

        AdvanceConditions(AdventureDurationUnit.Scene);
        ApplyEffects(choice.Effects);
        if (result.Succeeded) ApplyEffects(choice.SuccessEffects);
        else ApplyEffects(choice.FailureEffects);

        string destination = result.Outcome switch
        {
            AdventureCheckOutcome.CriticalSuccess when !string.IsNullOrWhiteSpace(baseCheck.CriticalSuccessSceneId) => baseCheck.CriticalSuccessSceneId!,
            AdventureCheckOutcome.CriticalFailure when !string.IsNullOrWhiteSpace(baseCheck.CriticalFailureSceneId) => baseCheck.CriticalFailureSceneId!,
            AdventureCheckOutcome.Success or AdventureCheckOutcome.CriticalSuccess => baseCheck.SuccessSceneId ?? choice.NextSceneId!,
            _ => baseCheck.FailureSceneId ?? choice.NextSceneId!,
        };

        if (string.IsNullOrWhiteSpace(destination))
            throw new InvalidOperationException($"Check '{baseCheck.Id}' has no destination for {result.Outcome}.");

        MoveTo(destination);
        return result;
    }

    public void RecordEncounter(AdventureEncounterType type, AdventureEncounterOutcome outcome)
    {
        State.Statistics.EncountersPlayed++;
        switch (outcome)
        {
            case AdventureEncounterOutcome.Win:
                State.Statistics.EncountersWon++;
                if (type == AdventureEncounterType.Blackjack) State.Statistics.BlackjackWins++;
                if (type == AdventureEncounterType.FiveOfAKind) State.Statistics.FiveOfAKindWins++;
                break;
            case AdventureEncounterOutcome.Loss:
                State.Statistics.EncountersLost++;
                break;
            case AdventureEncounterOutcome.Draw:
                State.Statistics.EncountersDrawn++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }
    }

    public void AdvanceConditions(AdventureDurationUnit unit)
    {
        foreach ((string id, AdventureStatusState state) in State.ActiveConditions.ToArray())
        {
            if (state.Unit != unit || state.Unit == AdventureDurationUnit.Permanent) continue;
            if (conditions.TryGetValue(id, out AdventureStatusDefinition? definition) && definition.PeriodicHitPointDelta != 0)
            {
                ChangeHitPoints(definition.PeriodicHitPointDelta, definition.Name);
            }

            state.Remaining--;
            if (state.Remaining > 0) continue;
            State.ActiveConditions.Remove(id);
            string name = conditions.TryGetValue(id, out AdventureStatusDefinition? removed) ? removed.Name : Humanize(id);
            AddFeedback(AdventureFeedbackKind.ConditionRemoved, id, $"{name} ended", "The condition is no longer affecting you.");
        }
    }

    private AdventureCheckDefinition ApplyConditionRollMode(AdventureCheckDefinition check)
    {
        bool disadvantage = State.ActiveConditions.Keys.Any(id =>
            conditions.TryGetValue(id, out AdventureStatusDefinition? definition) && definition.DisadvantageOnAbilityChecks);
        if (!disadvantage) return check;

        AdventureRollMode rollMode = check.RollMode switch
        {
            AdventureRollMode.Advantage => AdventureRollMode.Normal,
            AdventureRollMode.Normal => AdventureRollMode.Disadvantage,
            _ => AdventureRollMode.Disadvantage,
        };
        return check with { RollMode = rollMode };
    }

    private AdventureChoiceDefinition FindChoice(string choiceId)
    {
        AdventureChoiceDefinition? choice = (CurrentScene.Choices ?? [])
            .FirstOrDefault(candidate => string.Equals(candidate.Id, choiceId, StringComparison.OrdinalIgnoreCase));
        return choice ?? throw new ArgumentException(
            $"Scene '{CurrentScene.Id}' does not contain choice '{choiceId}'.",
            nameof(choiceId));
    }

    private void MoveTo(string sceneId)
    {
        if (!scenes.TryGetValue(sceneId, out AdventureSceneDefinition? scene))
            throw new InvalidOperationException($"Adventure '{Definition.Id}' references missing scene '{sceneId}'.");

        State.CurrentSceneId = scene.Id;
        State.Statistics.ScenesVisited++;
        State.Statistics.UniqueSceneIds.Add(scene.Id);
        ApplyEffects(scene.OnEnterEffects);
        if (scene.IsEnding) CompleteCurrentScene();
    }

    private void CompleteCurrentScene()
    {
        State.Completed = true;
        State.EndingId = CurrentScene.EndingId ?? CurrentScene.Id;
        State.PendingChoiceId = null;
        State.Statistics.CompletedAtUtc ??= DateTimeOffset.UtcNow;
    }

    private bool HasItem(string itemId) =>
        State.ItemStacks.TryGetValue(itemId, out AdventureItemState? state) && state.Quantity > 0;

    private void ApplyEffects(IReadOnlyList<AdventureEffect>? effects)
    {
        foreach (AdventureEffect effect in effects ?? [])
        {
            switch (effect.Kind)
            {
                case AdventureEffectKind.SetFlag:
                    State.Flags[effect.Key] = effect.FlagValue;
                    break;
                case AdventureEffectKind.AddItem:
                    ChangeItem(effect.Key, effect.Value == 0 ? 1 : Math.Abs(effect.Value));
                    break;
                case AdventureEffectKind.RemoveItem:
                    ChangeItem(effect.Key, -(effect.Value == 0 ? 1 : Math.Abs(effect.Value)));
                    break;
                case AdventureEffectKind.ChangeNpcRelation:
                    if (!State.Npcs.TryGetValue(effect.Key, out AdventureNpcState? npc))
                    {
                        npc = new AdventureNpcState();
                        State.Npcs[effect.Key] = npc;
                    }
                    npc.Relation += effect.Value;
                    break;
                case AdventureEffectKind.ChangeHitPoints:
                    ChangeHitPoints(effect.Value, "Story consequence");
                    break;
                case AdventureEffectKind.GainCharacterExperience:
                    State.Character.Experience = Math.Max(0, State.Character.Experience + effect.Value);
                    break;
                case AdventureEffectKind.ChangeCoins:
                    int oldCoins = State.Coins;
                    State.Coins = Math.Max(0, State.Coins + effect.Value);
                    int coinDelta = State.Coins - oldCoins;
                    if (coinDelta > 0) State.Statistics.CoinsEarned += coinDelta;
                    if (coinDelta < 0) State.Statistics.CoinsSpent += -coinDelta;
                    if (coinDelta != 0)
                        AddFeedback(AdventureFeedbackKind.CoinsChanged, "coins", coinDelta > 0 ? "Coins acquired" : "Coins spent", $"{Math.Abs(coinDelta)} coins", coinDelta);
                    break;
                case AdventureEffectKind.ChangeCounter:
                    State.Counters.TryGetValue(effect.Key, out int current);
                    State.Counters[effect.Key] = Math.Max(0, current + effect.Value);
                    break;
                case AdventureEffectKind.ApplyCondition:
                    ApplyCondition(effect.Key, effect.Value);
                    break;
                case AdventureEffectKind.RemoveCondition:
                    RemoveCondition(effect.Key);
                    break;
                case AdventureEffectKind.DiscoverCaseNode:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.DiscoverNode(board, State.CaseBoard, effect.Key, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "New case-board item");
                    break;
                }
                case AdventureEffectKind.RevealCaseConnection:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.RevealConnection(board, State.CaseBoard, effect.Key, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "New connection");
                    break;
                }
                case AdventureEffectKind.ConfirmCaseConnection:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetConnectionStatus(board, State.CaseBoard, effect.Key, CaseConnectionStatus.Confirmed, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Connection confirmed");
                    break;
                }
                case AdventureEffectKind.ContradictCaseConnection:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetConnectionStatus(board, State.CaseBoard, effect.Key, CaseConnectionStatus.Contradicted, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Connection contradicted");
                    break;
                }
                case AdventureEffectKind.ResolveLead:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetLeadState(board, State.CaseBoard, effect.Key, CaseLeadState.Resolved, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Lead resolved");
                    break;
                }
                case AdventureEffectKind.ContradictLead:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetLeadState(board, State.CaseBoard, effect.Key, CaseLeadState.Contradicted, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Lead contradicted");
                    break;
                }
                case AdventureEffectKind.UnlockTheory:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetTheoryState(board, State.CaseBoard, effect.Key, CaseTheoryState.Proposed, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "New theory");
                    break;
                }
                case AdventureEffectKind.SupportTheory:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetTheoryState(board, State.CaseBoard, effect.Key, CaseTheoryState.Supported, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Theory supported");
                    break;
                }
                case AdventureEffectKind.ConfirmTheory:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetTheoryState(board, State.CaseBoard, effect.Key, CaseTheoryState.Confirmed, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Theory confirmed");
                    break;
                }
                case AdventureEffectKind.ContradictTheory:
                {
                    CaseBoardDefinition board = RequireCaseBoard(effect);
                    AdventureCaseBoard.SetTheoryState(board, State.CaseBoard, effect.Key, CaseTheoryState.Contradicted, CurrentScene.Id);
                    AddCaseBoardFeedback(effect.Key, "Theory contradicted");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "Unknown adventure effect.");
            }
        }
    }

    private CaseBoardDefinition RequireCaseBoard(AdventureEffect effect) =>
        Definition.CaseBoard ?? throw new InvalidOperationException(
            $"Adventure '{Definition.Id}' uses Case Board effect '{effect.Kind}' but does not define a Case Board.");

    private void AddCaseBoardFeedback(string key, string title)
    {
        string detail = Definition.CaseBoard?.Nodes.FirstOrDefault(node => string.Equals(node.Id, key, StringComparison.OrdinalIgnoreCase))?.Title
            ?? Definition.CaseBoard?.Connections.FirstOrDefault(connection => string.Equals(connection.Id, key, StringComparison.OrdinalIgnoreCase))?.Label
            ?? Humanize(key);
        AddFeedback(AdventureFeedbackKind.Information, key, title, detail);
    }

    private void ChangeItem(string itemId, int delta)
    {
        if (!State.ItemStacks.TryGetValue(itemId, out AdventureItemState? state))
        {
            state = new AdventureItemState { ItemId = itemId, Quantity = 0 };
            State.ItemStacks[itemId] = state;
        }

        int previous = state.Quantity;
        int maxStack = items.TryGetValue(itemId, out AdventureItemDefinition? definition) ? Math.Max(1, definition.MaxStack) : int.MaxValue;
        state.Quantity = Math.Clamp(state.Quantity + delta, 0, maxStack);
        int applied = state.Quantity - previous;

        if (state.Quantity <= 0)
        {
            State.ItemStacks.Remove(itemId);
            State.Inventory.Remove(itemId);
        }
        else
        {
            State.Inventory.Add(itemId);
        }

        if (applied == 0) return;
        if (applied > 0) State.Statistics.ItemsFound += applied;
        else State.Statistics.ItemsConsumed += -applied;
        string name = definition?.Name ?? Humanize(itemId);
        if (applied > 0)
            AddFeedback(AdventureFeedbackKind.ItemAcquired, itemId, "Item acquired", name + (applied > 1 ? $" ×{applied}" : ""), applied);
        else
            AddFeedback(AdventureFeedbackKind.ItemRemoved, itemId, "Item used", name + (applied < -1 ? $" ×{Math.Abs(applied)}" : ""), applied);
    }

    private void ApplyCondition(string conditionId, int duration)
    {
        AdventureStatusDefinition definition = conditions.TryGetValue(conditionId, out AdventureStatusDefinition? found)
            ? found
            : new AdventureStatusDefinition(conditionId, Humanize(conditionId), "Temporary adventure condition.", "status", AdventureDurationUnit.Scene);
        int normalizedDuration = definition.DurationUnit == AdventureDurationUnit.Permanent ? int.MaxValue : Math.Max(1, duration == 0 ? 1 : duration);

        if (State.ActiveConditions.TryGetValue(conditionId, out AdventureStatusState? existing))
        {
            if (definition.Stackable) existing.Stacks++;
            existing.Remaining = definition.DurationUnit == AdventureDurationUnit.Permanent ? int.MaxValue : Math.Max(existing.Remaining, normalizedDuration);
        }
        else
        {
            State.ActiveConditions[conditionId] = new AdventureStatusState
            {
                ConditionId = conditionId,
                Unit = definition.DurationUnit,
                Remaining = normalizedDuration,
                Stacks = 1,
            };
        }

        State.Statistics.ConditionsReceived++;
        string durationText = definition.DurationUnit == AdventureDurationUnit.Permanent
            ? "Until cleared"
            : $"{normalizedDuration} {definition.DurationUnit.ToString().ToLowerInvariant()}{(normalizedDuration == 1 ? "" : "s")}";
        AddFeedback(AdventureFeedbackKind.ConditionApplied, conditionId, $"{definition.Name} applied", durationText);
    }

    private void RemoveCondition(string conditionId)
    {
        if (!State.ActiveConditions.Remove(conditionId)) return;
        string name = conditions.TryGetValue(conditionId, out AdventureStatusDefinition? definition) ? definition.Name : Humanize(conditionId);
        AddFeedback(AdventureFeedbackKind.ConditionRemoved, conditionId, $"{name} removed", "The condition has been cleared.");
    }

    private void ChangeHitPoints(int delta, string reason)
    {
        int previous = State.Character.CurrentHitPoints;
        State.Character.CurrentHitPoints = Math.Clamp(
            State.Character.CurrentHitPoints + delta,
            0,
            Math.Max(1, State.Character.MaxHitPoints));
        int applied = State.Character.CurrentHitPoints - previous;
        if (applied > 0) State.Statistics.HealingReceived += applied;
        if (applied < 0) State.Statistics.DamageTaken += -applied;
        if (applied != 0)
            AddFeedback(AdventureFeedbackKind.HitPointsChanged, "hp", applied > 0 ? "Health restored" : "You are hurt", $"{Math.Abs(applied)} HP · {reason}", applied);
    }

    private void AddFeedback(AdventureFeedbackKind kind, string key, string title, string detail, int delta = 0)
    {
        State.PendingFeedback.Add(new AdventureFeedbackEvent(kind, key, title, detail, delta));
        if (State.PendingFeedback.Count > 8) State.PendingFeedback.RemoveRange(0, State.PendingFeedback.Count - 8);
    }

    private static string Humanize(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}

public static class AdventureDefinitionValidator
{
    public static void Validate(AdventureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Id)) throw new ArgumentException("Adventure id is required.", nameof(definition));
        if (definition.Version < 1) throw new ArgumentException("Adventure version must be positive.", nameof(definition));
        if (definition.Scenes.Count == 0) throw new ArgumentException("Adventure must contain at least one scene.", nameof(definition));

        if (definition.CharacterCatalog is not null)
            AdventureCharacterCatalog.Validate(definition.CharacterCatalog);
        if (definition.CaseBoard is not null)
            CaseBoardDefinitionValidator.Validate(definition.CaseBoard);

        var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureItemDefinition item in definition.Items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !itemIds.Add(item.Id))
                throw new ArgumentException($"Adventure contains a blank or duplicate item id '{item.Id}'.", nameof(definition));
            if (item.MaxStack < 1) throw new ArgumentException($"Item '{item.Id}' has an invalid max stack.", nameof(definition));
        }

        var conditionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureStatusDefinition condition in definition.Conditions ?? [])
        {
            if (string.IsNullOrWhiteSpace(condition.Id) || !conditionIds.Add(condition.Id))
                throw new ArgumentException($"Adventure contains a blank or duplicate condition id '{condition.Id}'.", nameof(definition));
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureSceneDefinition scene in definition.Scenes)
        {
            if (string.IsNullOrWhiteSpace(scene.Id) || !ids.Add(scene.Id))
                throw new ArgumentException($"Adventure contains a blank or duplicate scene id '{scene.Id}'.", nameof(definition));

            var choiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AdventureChoiceDefinition choice in scene.Choices ?? [])
            {
                if (string.IsNullOrWhiteSpace(choice.Id) || !choiceIds.Add(choice.Id))
                    throw new ArgumentException($"Scene '{scene.Id}' contains a blank or duplicate choice id '{choice.Id}'.", nameof(definition));
                if (choice.Check is null && string.IsNullOrWhiteSpace(choice.NextSceneId))
                    throw new ArgumentException($"Choice '{scene.Id}/{choice.Id}' needs a destination or check.", nameof(definition));
                if (choice.Check is { Difficulty: < 1 })
                    throw new ArgumentException($"Check '{scene.Id}/{choice.Id}' has an invalid difficulty.", nameof(definition));
            }
        }

        if (!ids.Contains(definition.StartSceneId))
            throw new ArgumentException($"Start scene '{definition.StartSceneId}' does not exist.", nameof(definition));

        foreach (AdventureSceneDefinition scene in definition.Scenes)
        {
            foreach (AdventureChoiceDefinition choice in scene.Choices ?? [])
            {
                ValidateRoute(ids, choice.NextSceneId, scene.Id, choice.Id);
                if (choice.Check is AdventureCheckDefinition check)
                {
                    ValidateRoute(ids, check.SuccessSceneId, scene.Id, choice.Id);
                    ValidateRoute(ids, check.FailureSceneId, scene.Id, choice.Id);
                    ValidateRoute(ids, check.CriticalSuccessSceneId, scene.Id, choice.Id);
                    ValidateRoute(ids, check.CriticalFailureSceneId, scene.Id, choice.Id);
                    if (string.IsNullOrWhiteSpace(choice.NextSceneId) &&
                        (string.IsNullOrWhiteSpace(check.SuccessSceneId) || string.IsNullOrWhiteSpace(check.FailureSceneId)))
                    {
                        throw new ArgumentException(
                            $"Checked choice '{scene.Id}/{choice.Id}' needs both success and failure destinations or a fallback destination.",
                            nameof(definition));
                    }
                }
            }
        }
    }

    private static void ValidateRoute(HashSet<string> ids, string? destination, string sceneId, string choiceId)
    {
        if (!string.IsNullOrWhiteSpace(destination) && !ids.Contains(destination))
            throw new ArgumentException($"Choice '{sceneId}/{choiceId}' references missing scene '{destination}'.", nameof(ids));
    }
}
