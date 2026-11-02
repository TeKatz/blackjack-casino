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
    private bool encounterResolving;

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
        catch (Exception ex)
        {
            AppCrashLog.Write("Adventure completion presentation failed.", ex);
            completionShown = false;
        }
        finally
        {
            completionPending = false;
        }
    }

    private async Task ResolveEncounterAsync(AdventureEncounterDefinition encounter, AdventureEncounterOutcome outcome)
    {
        if (encounterResolving) return;
        encounterResolving = true;
        string originSceneId = engine.State.CurrentSceneId;

        try
        {
            // Route the story first. Only a successfully accepted outcome is counted in run telemetry.
            // This prevents a failed/missing result route from recording the same encounter twice on retry.
            engine.SelectChoice(encounter.ChoiceIdFor(outcome));
            engine.RecordEncounter(encounter.Type, outcome);

            try
            {
                persist(engine.State);
            }
            catch (Exception ex)
            {
                // A transient save problem must not turn a settled card hand into a process-ending WPF exception.
                // The in-memory run remains authoritative and later story transitions will persist it again.
                AppCrashLog.Write($"Adventure encounter '{encounter.Id}' could not be persisted after {outcome}.", ex);
            }

            await Dispatcher.InvokeAsync(BuildStoryView);
        }
        catch (Exception ex)
        {
            AppCrashLog.Write($"Adventure encounter '{encounter.Id}' failed while resolving {outcome}.", ex);

            // If the story transition itself already succeeded, prefer recovering the story view instead of
            // replaying the encounter outcome. That keeps loss/win rewards and telemetry strictly single-shot.
            if (!string.Equals(engine.State.CurrentSceneId, originSceneId, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await Dispatcher.InvokeAsync(BuildStoryView);
                    return;
                }
                catch (Exception recoveryException)
                {
                    AppCrashLog.Write($"Adventure encounter '{encounter.Id}' story-view recovery failed.", recoveryException);
                }
            }

            await Dispatcher.InvokeAsync(() => ShowEncounterRecovery(encounter));
        }
        finally
        {
            encounterResolving = false;
        }
    }

    private void ShowEncounterRecovery(AdventureEncounterDefinition encounter)
    {
        activeEncounterId = null;
        overlayLayer.Children.Clear();

        Border panel = AdventureUi.Panel("#211A18", "#100C0B", "#8E6252", new Thickness(28), radius: 16);
        panel.Width = 620;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;

        var body = new StackPanel();
        body.Children.Add(AdventureUi.Eyebrow("ADVENTURE ENCOUNTER", 10.5));
        body.Children.Add(new TextBlock
        {
            Text = "THE HAND IS SETTLED",
            FontFamily = new System.Windows.Media.FontFamily("Georgia"),
            FontSize = 27,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#E7C8A7"),
            Margin = new Thickness(0, 7, 0, 8),
        });
        body.Children.Add(new TextBlock
        {
            Text = $"{encounter.Title} finished, but the story view could not be restored cleanly. Your result has been kept. You can retry the story view or return to the Adventure Library without closing the game.",
            FontFamily = new System.Windows.Media.FontFamily("Georgia"),
            FontSize = 13,
            LineHeight = 20,
            TextWrapping = TextWrapping.Wrap,
            Foreground = AdventureUi.Ink("#C9B9A8"),
            Margin = new Thickness(0, 0, 0, 18),
        });

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        Button retry = AdventureUi.Button("RETRY STORY VIEW", accent: true, fontSize: 12, scaleOnHover: false);
        retry.Margin = new Thickness(0, 0, 10, 0);
        retry.Click += (_, _) =>
        {
            try
            {
                BuildStoryView();
            }
            catch (Exception ex)
            {
                AppCrashLog.Write($"Adventure encounter '{encounter.Id}' manual story-view recovery failed.", ex);
            }
        };
        actions.Children.Add(retry);

        Button library = AdventureUi.Button("ADVENTURE LIBRARY", fontSize: 12, scaleOnHover: false);
        library.Click += (_, _) =>
        {
            try
            {
                persist(engine.State);
            }
            catch (Exception ex)
            {
                AppCrashLog.Write($"Adventure encounter '{encounter.Id}' recovery save failed.", ex);
            }
            backToHub();
        };
        actions.Children.Add(library);
        body.Children.Add(actions);

        panel.Child = body;
        overlayLayer.Children.Add(panel);
    }
}