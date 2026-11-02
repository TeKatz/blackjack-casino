using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Hosts the ordinary AdventureView, overlays registered game encounters, and presents the shared
/// completion screen once a story reaches an ending. Everything remains catalog-driven so later
/// adventures inherit encounter and completion behavior without bespoke navigation code.
/// </summary>
internal sealed class AdventureEncounterRunHost : Grid
{
    private readonly AdventureEngine engine;
    private readonly Action<AdventureRunState> persist;
    private readonly Action backToHub;
    private readonly Action restart;
    private readonly Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> recordCompletion;
    private readonly Func<string?> diceSetId;
    private readonly Func<bool> animationsEnabled;
    private readonly Grid overlayLayer = new();
    private readonly DependencyPropertyDescriptor contentDescriptor;
    private AdventureView? storyView;
    private string? activeEncounterId;
    private bool completionShown;
    private bool completionPending;

    internal AdventureEncounterRunHost(
        AdventureEngine engine,
        Action<AdventureRunState> persist,
        Action backToHub,
        Action restart,
        Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> recordCompletion,
        Func<string?> diceSetId,
        Func<bool> animationsEnabled)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.backToHub = backToHub ?? throw new ArgumentNullException(nameof(backToHub));
        this.restart = restart ?? throw new ArgumentNullException(nameof(restart));
        this.recordCompletion = recordCompletion ?? throw new ArgumentNullException(nameof(recordCompletion));
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
        if (!engine.State.Completed) completionShown = false;

        storyView = new AdventureView(engine, persist, backToHub, diceSetId, animationsEnabled);
        Children.Insert(0, storyView);
        contentDescriptor.AddValueChanged(storyView, OnStoryContentChanged);
        RefreshOverlay();
    }

    private void DetachStoryView()
    {
        if (storyView is null) return;
        contentDescriptor.RemoveValueChanged(storyView, OnStoryContentChanged);
        Children.Remove(storyView);
        storyView = null;
    }

    private void OnStoryContentChanged(object? sender, EventArgs args) => RefreshOverlay();

    private void RefreshOverlay()
    {
        if (engine.State.Completed)
        {
            activeEncounterId = null;
            if (!completionShown && !completionPending)
            {
                completionPending = true;
                _ = ShowCompletionAsync();
            }
            return;
        }

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

    private async Task ShowCompletionAsync()
    {
        try
        {
            // Let the final story scene breathe for a moment before the meta layer arrives.
            await Task.Delay(animationsEnabled() ? 760 : 1);
            if (!engine.State.Completed || !IsLoaded) return;

            persist(engine.State);
            AdventureCompletionMetaResult meta = recordCompletion(engine.Definition, engine.State);
            completionShown = true;
            overlayLayer.Children.Clear();
            overlayLayer.Children.Add(new AdventureCompletionView(
                engine.Definition,
                engine.State,
                meta,
                backToHub,
                restart,
                animationsEnabled()));
        }
        finally
        {
            completionPending = false;
        }
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

        engine.RecordEncounter(encounter.Type, outcome);
        engine.SelectChoice(choiceId);
        persist(engine.State);
        await Dispatcher.InvokeAsync(BuildStoryView);
    }
}
