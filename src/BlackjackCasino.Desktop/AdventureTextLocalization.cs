using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Localizes authored campaign copy by stable adventure/scene/choice identifiers.
/// The story engine, routing and serialized save state retain their language-neutral IDs.
/// Missing translations fall back to the original English text instead of leaking key tokens.
/// </summary>
internal static class AdventureTextLocalization
{
    private static string L(string key, string original)
    {
        if (Localization.Language != UiLanguagePreference.German) return original;
        string translated = Localization.T(key);
        return translated.StartsWith("⟦", StringComparison.Ordinal) ? original : translated;
    }

    internal static string Ui(string key, string original) => L("Adventure.Ui." + key, original);
    internal static string Raw(string original) => Localization.TranslateSource(original);

    internal static string Title(AdventureDefinition adventure) =>
        L($"Adventure.{adventure.Id}.Title", adventure.Title);

    internal static string Subtitle(AdventureDefinition adventure) =>
        L($"Adventure.{adventure.Id}.Subtitle", adventure.Subtitle);

    internal static string SceneTitle(AdventureDefinition adventure, AdventureSceneDefinition scene) =>
        L(scene.TitleLocalizationKey ?? $"Adventure.{adventure.Id}.Scene.{scene.Id}.Title", scene.Title);

    internal static string SceneBody(AdventureDefinition adventure, AdventureSceneDefinition scene) =>
        L(scene.BodyLocalizationKey ?? $"Adventure.{adventure.Id}.Scene.{scene.Id}.Body", scene.Body);

    internal static string SceneLocation(AdventureDefinition adventure, AdventureSceneDefinition scene) =>
        L($"Adventure.{adventure.Id}.Scene.{scene.Id}.Location", scene.Location ?? "");

    internal static string Act(AdventureDefinition adventure, AdventureSceneDefinition scene, string original) =>
        scene.ActId is { } id ? L($"Adventure.{adventure.Id}.Act.{id}", original) : Title(adventure);

    internal static string BoardTitle(AdventureDefinition adventure, CaseNodeDefinition node) =>
        L($"Adventure.{adventure.Id}.Board.{node.Id}.Title", node.Title);

    internal static string BoardDescription(AdventureDefinition adventure, CaseNodeDefinition node) =>
        L($"Adventure.{adventure.Id}.Board.{node.Id}.Description", node.Description);

    internal static string BoardSubtitle(AdventureDefinition adventure, CaseNodeDefinition node) =>
        L($"Adventure.{adventure.Id}.Board.{node.Id}.Subtitle", node.Subtitle ?? "");

    internal static string ItemName(AdventureDefinition adventure, AdventureItemDefinition item) =>
        L($"Adventure.{adventure.Id}.Item.{item.Id}.Name", item.Name);

    internal static string ItemDescription(AdventureDefinition adventure, AdventureItemDefinition item) =>
        L($"Adventure.{adventure.Id}.Item.{item.Id}.Description", item.Description);

    internal static string ConditionName(AdventureDefinition adventure, AdventureStatusDefinition condition) =>
        L($"Adventure.{adventure.Id}.Condition.{condition.Id}.Name", condition.Name);

    internal static string ConditionDescription(AdventureDefinition adventure, AdventureStatusDefinition condition) =>
        L($"Adventure.{adventure.Id}.Condition.{condition.Id}.Description", condition.Description);

    internal static string Choice(AdventureDefinition adventure, AdventureChoiceDefinition choice) =>
        L(choice.LocalizationKey ?? $"Adventure.{adventure.Id}.Choice.{choice.Id}", choice.Text);
}
