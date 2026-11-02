using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Hosts the ordinary AdventureView and overlays registered encounters whenever the current scene
/// has one. The host watches AdventureView.Content because every story render replaces that content;
/// this keeps encounter detection decoupled from the story engine and from individual adventures.
/// </summary>
internal sealed class AdventureEncounterRunHost : Grid
{
    private readonly AdventureEngine engine;
    private readonly Action<AdventureRunState> persist;
    private readonly Action backToHub;
    private readonly Func<string?> diceSetId;
    private readonly Func<bool> animationsEnabled;
    private readonly Grid overlayLayer = new();
    private readonly DependencyPropertyDescriptor contentDescriptor;
    private AdventureView? storyView;
    private string? activeEncounterId;

    internal AdventureEncounterRunHost(
        AdventureEngine engine,
        Action<AdventureRunState> persist,
        Action backToHub,
        Func<string?> diceSetId,
        Func<bool> animationsEnabled)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.backToHub = backToHub ?? throw new ArgumentNullException(nameof(backToHub));
        this.diceSetId = diceSetId ?? throw new ArgumentNullException(nameof(diceSetId));
        this.animationsEnabled = animationsEnabled ?? throw new ArgumentNullException(nameof(animationsEnabled));
        contentDescriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(AdventureView))
            ?? throw new InvalidOperationException("Adventure encounter host could not observe AdventureView.Content.");

        Children.Add(overlayLayer);
        BuildStoryView();
        Unloaded += (_, _) => DetachStoryView();
    }

    private void BuildStoryView()
    {
        DetachStoryView();
        overlayLayer.Children.Clear();
        activeEncounterId = null;

        storyView = new AdventureView(engine, persist, backToHub, diceSetId, animationsEnabled);
        Children.Insert(0, storyView);
        contentDescriptor.AddValueChanged(storyView, OnStoryContentChanged);
        RefreshEncounter();
    }

    private void DetachStoryView()
    {
        if (storyView is null) return;
        contentDescriptor.RemoveValueChanged(storyView, OnStoryContentChanged);
        Children.Remove(storyView);
        storyView = null;
    }

    private void OnStoryContentChanged(object? sender, EventArgs args) => RefreshEncounter();

    private void RefreshEncounter()
    {
        AdventureSceneDefinition scene = engine.CurrentScene;
        if (!AdventureEncounterCatalog.TryGet(engine.Definition.Id, scene.Id, out AdventureEncounterDefinition? encounter) || encounter is null)
        {
            activeEncounterId = null;
            overlayLayer.Children.Clear();
            return;
        }

        if (string.Equals(activeEncounterId, encounter.Id, StringComparison.OrdinalIgnoreCase) && overlayLayer.Children.Count > 0)
            return;

        if (!AdventureEncounterFramework.TryCreate(
                engine.Definition,
                scene,
                animationsEnabled(),
                ResolveEncounterAsync,
                out FrameworkElement? overlay) || overlay is null)
        {
            return;
        }

        activeEncounterId = encounter.Id;
        overlayLayer.Children.Clear();
        overlayLayer.Children.Add(overlay);
    }

    private async Task ResolveEncounterAsync(AdventureEncounterDefinition encounter, AdventureEncounterOutcome outcome)
    {
        string choiceId = outcome switch
        {
            AdventureEncounterOutcome.Win => encounter.WinChoiceId,
            AdventureEncounterOutcome.Loss => encounter.LossChoiceId,
            AdventureEncounterOutcome.Draw => encounter.DrawChoiceId,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

        engine.SelectChoice(choiceId);
        persist(engine.State);
        await Dispatcher.InvokeAsync(BuildStoryView);
    }
}
