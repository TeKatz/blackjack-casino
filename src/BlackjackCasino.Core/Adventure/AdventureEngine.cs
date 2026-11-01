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

    public AdventureEngine(AdventureDefinition definition, AdventureRunState state)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        State = AdventureRunState.Normalize(state);
        AdventureDefinitionValidator.Validate(definition);
        scenes = definition.Scenes.ToDictionary(scene => scene.Id, StringComparer.OrdinalIgnoreCase);

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
            if (string.IsNullOrWhiteSpace(State.PendingChoiceId))
                return null;
            return FindChoice(State.PendingChoiceId).Check;
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
        var engine = new AdventureEngine(definition, state);
        engine.ApplyEffects(engine.CurrentScene.OnEnterEffects);
        if (engine.CurrentScene.IsEnding)
            engine.CompleteCurrentScene();
        return engine;
    }

    public IReadOnlyList<AdventureChoiceDefinition> AvailableChoices() =>
        (CurrentScene.Choices ?? [])
            .Where(choice => (choice.Conditions ?? []).All(Evaluate))
            .ToArray();

    public bool Evaluate(AdventureCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return condition.Kind switch
        {
            AdventureConditionKind.Flag =>
                (State.Flags.TryGetValue(condition.Key, out bool flag) && flag) == condition.Expected,
            AdventureConditionKind.ItemOwned =>
                State.Inventory.Contains(condition.Key) == condition.Expected,
            AdventureConditionKind.AbilityMinimum when condition.Ability is AdventureAbility ability =>
                State.Character.AbilityScore(ability) >= condition.Value,
            AdventureConditionKind.SkillMinimum =>
                State.Character.SkillBonus(condition.Key) >= condition.Value,
            AdventureConditionKind.NpcRelationMinimum =>
                State.Npcs.TryGetValue(condition.Key, out AdventureNpcState? npc) && npc.Relation >= condition.Value,
            _ => false,
        };
    }

    public void SelectChoice(string choiceId)
    {
        if (State.Completed)
            throw new InvalidOperationException("This adventure run is complete.");
        if (!string.IsNullOrWhiteSpace(State.PendingChoiceId))
            throw new InvalidOperationException("Resolve the pending check before choosing another action.");

        AdventureChoiceDefinition choice = FindChoice(choiceId);
        if (!(choice.Conditions ?? []).All(Evaluate))
            throw new InvalidOperationException($"Choice '{choiceId}' is not currently available.");

        if (choice.Check is not null)
        {
            State.PendingChoiceId = choice.Id;
            return;
        }

        ApplyEffects(choice.Effects);
        MoveTo(choice.NextSceneId ?? throw new InvalidOperationException($"Choice '{choice.Id}' has no destination."));
    }

    public AdventureCheckResult ResolvePendingCheck(int firstRoll, int? secondRoll = null)
    {
        if (string.IsNullOrWhiteSpace(State.PendingChoiceId))
            throw new InvalidOperationException("There is no pending check.");

        AdventureChoiceDefinition choice = FindChoice(State.PendingChoiceId);
        AdventureCheckDefinition check = choice.Check ??
            throw new InvalidOperationException($"Pending choice '{choice.Id}' does not contain a check.");
        AdventureCheckResult result = AdventureCheckResolver.Resolve(check, State.Character, firstRoll, secondRoll);

        State.RollsMade++;
        if (result.ChosenRoll == 20) State.NaturalTwenties++;
        if (result.ChosenRoll == 1) State.NaturalOnes++;
        State.PendingChoiceId = null;

        ApplyEffects(choice.Effects);
        if (result.Succeeded)
            ApplyEffects(choice.SuccessEffects);
        else
            ApplyEffects(choice.FailureEffects);

        string destination = result.Outcome switch
        {
            AdventureCheckOutcome.CriticalSuccess when !string.IsNullOrWhiteSpace(check.CriticalSuccessSceneId) => check.CriticalSuccessSceneId!,
            AdventureCheckOutcome.CriticalFailure when !string.IsNullOrWhiteSpace(check.CriticalFailureSceneId) => check.CriticalFailureSceneId!,
            AdventureCheckOutcome.Success or AdventureCheckOutcome.CriticalSuccess => check.SuccessSceneId ?? choice.NextSceneId!,
            _ => check.FailureSceneId ?? choice.NextSceneId!,
        };

        if (string.IsNullOrWhiteSpace(destination))
            throw new InvalidOperationException($"Check '{check.Id}' has no destination for {result.Outcome}.");

        MoveTo(destination);
        return result;
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
        ApplyEffects(scene.OnEnterEffects);
        if (scene.IsEnding)
            CompleteCurrentScene();
    }

    private void CompleteCurrentScene()
    {
        State.Completed = true;
        State.EndingId = CurrentScene.EndingId ?? CurrentScene.Id;
        State.PendingChoiceId = null;
    }

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
                    State.Inventory.Add(effect.Key);
                    break;
                case AdventureEffectKind.RemoveItem:
                    State.Inventory.Remove(effect.Key);
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
                    State.Character.CurrentHitPoints = Math.Clamp(
                        State.Character.CurrentHitPoints + effect.Value,
                        0,
                        Math.Max(1, State.Character.MaxHitPoints));
                    break;
                case AdventureEffectKind.GainCharacterExperience:
                    State.Character.Experience = Math.Max(0, State.Character.Experience + effect.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "Unknown adventure effect.");
            }
        }
    }
}

public static class AdventureDefinitionValidator
{
    public static void Validate(AdventureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Adventure id is required.", nameof(definition));
        if (definition.Version < 1)
            throw new ArgumentException("Adventure version must be positive.", nameof(definition));
        if (definition.Scenes.Count == 0)
            throw new ArgumentException("Adventure must contain at least one scene.", nameof(definition));

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
            throw new ArgumentException(
                $"Choice '{sceneId}/{choiceId}' references missing scene '{destination}'.",
                nameof(ids));
    }
}
