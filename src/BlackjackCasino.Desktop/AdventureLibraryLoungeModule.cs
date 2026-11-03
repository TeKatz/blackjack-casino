using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Catalog-driven Tales from the Backroom shell. Adding a separate adventure to AdventureCatalog.All
/// automatically gives it a library card, its own saved run, character creation and the shared play surface.
/// </summary>
internal static class AdventureLibraryLoungeModule
{
    // Compatibility overload for the historic AdventureLoungeModule shim and presentation tests.
    // The production MainWindow path uses the overload below so account-wide completion progression is persisted.
    internal static LoungeHostedGame Create(
        AdventureProgressState progress,
        Action<AdventureProgressState> persist,
        Func<string?> diceSetId,
        Func<string?> playerName,
        Func<bool> animationsEnabled,
        Func<AreaPresentationSettings> presentation,
        Action<AreaPresentationSettings> updatePresentation,
        Action<bool> setMusicActive) =>
        Create(
            progress,
            persist,
            (definition, state) => new AdventureCompletionMetaResult(
                false,
                false,
                string.IsNullOrWhiteSpace(state.EndingId)
                    ? string.Empty
                    : AdventureProgressionService.EndingKey(definition.Id, state.EndingId),
                [],
                null),
            diceSetId,
            playerName,
            animationsEnabled,
            presentation,
            updatePresentation,
            setMusicActive);

    internal static LoungeHostedGame Create(
        AdventureProgressState progress,
        Action<AdventureProgressState> persist,
        Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> recordCompletion,
        Func<string?> diceSetId,
        Func<string?> playerName,
        Func<bool> animationsEnabled,
        Func<AreaPresentationSettings> presentation,
        Action<AreaPresentationSettings> updatePresentation,
        Action<bool> setMusicActive)
    {
        AdventureProgressState state = AdventureProgressState.Normalize(progress);
        return new LoungeHostedGame(
            new LoungeGame(
                "tales-backroom",
                "Tales from the Backroom",
                "A tabletop story waits behind the lounge.",
                "Build characters, carry choices between scenes and play separate data-driven adventures through the same tabletop engine.",
                () => { },
                Artwork: CreateArtwork,
                SortOrder: 70,
                Status: () => Status(state)),
            back => new AdventureLibraryHubControl(
                state,
                persist,
                recordCompletion,
                diceSetId,
                playerName,
                animationsEnabled,
                presentation,
                updatePresentation,
                setMusicActive,
                back));
    }

    private static string Status(AdventureProgressState state)
    {
        int total = AdventureCatalog.All.Count;
        int active = state.Runs.Values.Count(run => !run.Completed);
        int completed = state.Runs.Values.Count(run => run.Completed);
        if (active > 0 && state.LastAdventureId is string last && AdventureCatalog.TryGet(last, out AdventureDefinition? adventure) && adventure is not null)
            return $"{adventure.Title} · in progress · {total} adventure{(total == 1 ? "" : "s")}";
        if (completed > 0) return $"{total} adventure{(total == 1 ? "" : "s")} · {completed} completed run{(completed == 1 ? "" : "s")}";
        return $"{total} adventure{(total == 1 ? "" : "s")} · ready to begin";
    }

    private static FrameworkElement CreateArtwork()
    {
        var grid = new Grid { Width = 300, Height = 130 };
        grid.Children.Add(new AdventureD20
        {
            Width = 112,
            Height = 112,
            Value = 20,
            VisualVariant = "dice-obsidian",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return grid;
    }
}

internal sealed class AdventureLibraryHubControl : UserControl
{
    private readonly AdventureProgressState progress;
    private readonly Action<AdventureProgressState> persist;
    private readonly Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> recordCompletion;
    private readonly Func<string?> diceSetId;
    private readonly Func<string?> playerName;
    private readonly Func<bool> animationsEnabled;
    private readonly Func<AreaPresentationSettings> presentation;
    private readonly Action<AreaPresentationSettings> updatePresentation;
    private readonly Action<bool> setMusicActive;
    private string selectedAdventureId;

    internal AdventureLibraryHubControl(
        AdventureProgressState progress,
        Action<AdventureProgressState> persist,
        Func<AdventureDefinition, AdventureRunState, AdventureCompletionMetaResult> recordCompletion,
        Func<string?> diceSetId,
        Func<string?> playerName,
        Func<bool> animationsEnabled,
        Func<AreaPresentationSettings> presentation,
        Action<AreaPresentationSettings> updatePresentation,
        Action<bool> setMusicActive,
        Action backToGames)
    {
        this.progress = AdventureProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.recordCompletion = recordCompletion ?? throw new ArgumentNullException(nameof(recordCompletion));
        this.diceSetId = diceSetId ?? throw new ArgumentNullException(nameof(diceSetId));
        this.playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        this.animationsEnabled = animationsEnabled ?? throw new ArgumentNullException(nameof(animationsEnabled));
        this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        this.updatePresentation = updatePresentation ?? throw new ArgumentNullException(nameof(updatePresentation));
        this.setMusicActive = setMusicActive ?? throw new ArgumentNullException(nameof(setMusicActive));
        _ = backToGames ?? throw new ArgumentNullException(nameof(backToGames));

        selectedAdventureId = ResolveInitialAdventureId();
        Loaded += (_, _) => this.setMusicActive(true);
        Unloaded += (_, _) => this.setMusicActive(false);
        ShowHub();
    }

    private string ResolveInitialAdventureId()
    {
        if (progress.LastAdventureId is string last && AdventureCatalog.TryGet(last, out _)) return last;
        return AdventureCatalog.All.FirstOrDefault()?.Id
            ?? throw new InvalidOperationException("Tales from the Backroom requires at least one registered adventure.");
    }

    private AdventureDefinition SelectedAdventure =>
        AdventureCatalog.TryGet(selectedAdventureId, out AdventureDefinition? adventure) && adventure is not null
            ? adventure
            : AdventureCatalog.All[0];

    private void ShowHub()
    {
        var right = new StackPanel();
        right.Children.Add(PremiumLoungeTheme.Heading("ADVENTURE LIBRARY"));

        foreach (AdventureDefinition tale in AdventureCatalog.All)
            right.Children.Add(BuildAdventureCard(tale));

        right.Children.Add(PremiumLoungeTheme.Heading("CHARACTER & NEW RUN"));
        right.Children.Add(BuildCharacterCreator(SelectedAdventure));
        var musicHeader = PremiumLoungeTheme.Heading("BACKROOM SCORE");
        musicHeader.Margin = new Thickness(0, 18, 0, 9);
        right.Children.Add(musicHeader);
        right.Children.Add(BuildMusicPanel());

        int started = progress.Runs.Count;
        int completed = progress.Runs.Values.Count(run => run.Completed);
        Content = PremiumLoungeTheme.Build(
            "convention-hall.png",
            "penthouse-club.png",
            "BEYOND THE CASINO FLOOR",
            "Every door has a story.",
            "Make your choices, roll the dice and live with what follows.",
            "T A L E S   F R O M   T H E   B A C K R O O M",
            "Choose a tale.\nCarry the consequences.",
            "Each adventure is its own content package. All tales share one character, dice, inventory, condition, encounter and completion blueprint.",
            PremiumLoungeTheme.Stats(
                $"AVAILABLE TALES   {AdventureCatalog.All.Count:N0}",
                $"RUNS STARTED   {started:N0}    ·    COMPLETED   {completed:N0}",
                $"SELECTED   {SelectedAdventure.Title.ToUpperInvariant()}"),
            right,
            "Your investigation board, encounters, choices and character persist between sessions. Each tale can be continued independently.");
    }

    private Border BuildAdventureCard(AdventureDefinition adventure)
    {
        progress.Runs.TryGetValue(adventure.Id, out AdventureRunState? run);
        bool selected = string.Equals(adventure.Id, selectedAdventureId, StringComparison.OrdinalIgnoreCase);
        string state = run is null ? "UNPLAYED" : run.Completed ? "COMPLETED" : "IN PROGRESS";
        var row = PremiumLoungeTheme.Row(
            adventure.Title,
            $"{adventure.Acts?.Count ?? 0} acts · {adventure.Scenes.Count} scenes · {state}",
            selected: selected, height: 93);
        var body = (StackPanel)PremiumLoungeTheme.RowContent(row).Children[1];
        var description = PremiumLoungeTheme.Label(adventure.Subtitle, 11, PremiumLoungeTheme.Muted);
        description.Margin = new Thickness(0, 5, 4, 0);
        body.Children.Add(description);

        if (run is not null && !run.Completed)
        {
            AdventureRunState continuation = run;
            var resume = PremiumLoungeTheme.Button("RESUME", "AdventureResume_" + adventure.Id,
                () => ShowRun(adventure, continuation), width: 100);
            PremiumLoungeTheme.AddRowAction(row, resume, 108);
        }

        var select = PremiumLoungeTheme.Button("SELECT", "AdventureSelect_" + adventure.Id,
            () =>
            {
                selectedAdventureId = adventure.Id;
                progress.LastAdventureId = adventure.Id;
                persist(progress);
                ShowHub();
            }, primary: selected, width: 94, gold: selected);
        PremiumLoungeTheme.AddRowAction(row, select, 104);
        return row;
    }

    private Border BuildCharacterCreator(AdventureDefinition adventure)
    {
        Border panel = PremiumLoungeTheme.Panel(new Grid(), 18);
        panel.Margin = new Thickness(0, 0, 0, 9);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(355) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var chooser = new StackPanel();
        chooser.Children.Add(AdventureUi.Eyebrow("NEW RUN", 10));
        chooser.Children.Add(new TextBlock
        {
            Text = adventure.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            Foreground = AdventureUi.Ink("#F0E0C6"),
            Margin = new Thickness(0, 5, 0, 12),
        });
        var classes = BuildClassSelector(adventure);
        chooser.Children.Add(classes);
        Button start = AdventureUi.Button("BEGIN NEW RUN", accent: true, fontSize: 13, scaleOnHover: false);
        start.HorizontalAlignment = HorizontalAlignment.Left;
        start.Margin = new Thickness(0, 17, 0, 0);
        start.Click += (_, _) =>
        {
            AdventureCharacterState character = AdventureCharacterCatalog.Create(adventure, SelectedClassId(classes), playerName());
            StartFreshRun(adventure, character);
        };
        chooser.Children.Add(start);
        grid.Children.Add(chooser);

        var divider = new Border { Width = 1, Background = AdventureUi.Ink("#4A4936"), Margin = new Thickness(16, 0, 16, 0) };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var preview = new StackPanel { Margin = new Thickness(4, 2, 0, 0) };
        var className = new TextBlock { FontFamily = new FontFamily("Georgia"), FontSize = 24, Foreground = AdventureUi.Ink("#EBD9B7") };
        var description = new TextBlock { Foreground = AdventureUi.Ink("#AEBFB7"), FontFamily = new FontFamily("Georgia"), FontSize = 13.5, LineHeight = 21, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 10) };
        var stats = new TextBlock { Foreground = AdventureUi.Ink("#CDBA8B"), FontFamily = new FontFamily("Georgia"), FontSize = 11.5, LineHeight = 18, TextWrapping = TextWrapping.Wrap };
        preview.Children.Add(className);
        preview.Children.Add(description);
        preview.Children.Add(AdventureUi.OrnamentRule("◇"));
        preview.Children.Add(stats);
        Grid.SetColumn(preview, 2);
        grid.Children.Add(preview);

        void UpdatePreview()
        {
            AdventureClassDefinition definition = AdventureCharacterCatalog.AllFor(adventure)
                .First(item => item.Id == SelectedClassId(classes));
            className.Text = definition.Name;
            description.Text = definition.Description;
            string abilityLine = string.Join(" · ", Enum.GetValues<AdventureAbility>().Select(ability =>
            {
                int score = definition.Abilities.TryGetValue(ability, out int value) ? value : 10;
                return $"{AdventureCharacterCatalog.AbilityShortLabel(adventure, ability)} {score}";
            }));
            string special = string.IsNullOrWhiteSpace(definition.SpecialMechanicName)
                ? string.Empty
                : $"\nSPECIAL · {definition.SpecialMechanicName}";
            stats.Text = $"HP {definition.MaxHitPoints} · Armor {definition.Armor}\n{abilityLine}{special}";
        }
        classes.SelectionChanged += (_, _) => UpdatePreview();
        UpdatePreview();
        panel.Child = grid;
        return panel;
    }

    private static ComboBox BuildClassSelector(AdventureDefinition adventure)
    {
        var classes = new ComboBox
        {
            Width = 320,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 9, 12, 9),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            Background = AdventureUi.Ink("#0B1714"),
            Foreground = AdventureUi.Ink("#F1E4D0"),
            BorderBrush = AdventureUi.Ink("#65755D"),
            BorderThickness = new Thickness(1),
        };
        ComboBoxItem? preferred = null;
        foreach (AdventureClassDefinition definition in AdventureCharacterCatalog.AllFor(adventure))
        {
            var item = new ComboBoxItem
            {
                Content = definition.Name,
                Tag = definition.Id,
                FontFamily = new FontFamily("Georgia"),
                FontSize = 14,
                Padding = new Thickness(8, 6, 8, 6),
                Foreground = AdventureUi.Ink("#EEE2CF"),
            };
            classes.Items.Add(item);
            if (definition.Id == "rogue") preferred = item;
        }
        if (classes.Items.Count == 0)
            throw new InvalidOperationException($"Adventure '{adventure.Id}' does not expose any playable character archetypes.");
        classes.SelectedItem = preferred ?? classes.Items[0];
        return classes;
    }

    private Border BuildMusicPanel()
    {
        AreaPresentationSettings settings = presentation().Sanitize();
        Border panel = PremiumLoungeTheme.Panel(new Grid(), 16);
        panel.Margin = new Thickness(0, 0, 0, 24);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(AdventureUi.Eyebrow("BACKROOM SCORE", 10.5));
        copy.Children.Add(new TextBlock { Text = "Lanterns Beyond the Door", FontFamily = new FontFamily("Georgia"), FontSize = 17, Foreground = AdventureUi.Ink("#E9D8B6"), Margin = new Thickness(0, 3, 0, 1) });
        copy.Children.Add(new TextBlock { Text = "Shared by the Backroom library; future tales can override this at the content layer.", FontFamily = new FontFamily("Georgia"), FontSize = 10.5, Foreground = AdventureUi.Ink("#839B91") });
        grid.Children.Add(copy);

        Button toggle = AdventureUi.Button(settings.MusicEnabled ? "MUSIC · ON" : "MUSIC · OFF", accent: settings.MusicEnabled, fontSize: 11, scaleOnHover: false);
        toggle.VerticalAlignment = VerticalAlignment.Center;
        toggle.Margin = new Thickness(20, 0, 24, 0);
        Grid.SetColumn(toggle, 1);
        grid.Children.Add(toggle);

        var volume = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var label = new TextBlock { Text = $"MUSIC VOLUME · {Math.Round(settings.MusicVolume / .32 * 100):0}%", FontFamily = new FontFamily("Georgia"), FontSize = 10, Foreground = AdventureUi.Ink("#8FB9A6"), Margin = new Thickness(0, 0, 0, 5) };
        volume.Children.Add(label);
        var slider = new Slider { Minimum = 0, Maximum = .32, Value = settings.MusicVolume, Width = 172, Height = 22, IsMoveToPointEnabled = true, Cursor = CasinoCursors.Hand };
        volume.Children.Add(slider);
        Grid.SetColumn(volume, 2);
        grid.Children.Add(volume);

        void Commit(bool enabled, double value)
        {
            settings = settings with { MusicEnabled = enabled, MusicVolume = value };
            updatePresentation(settings);
            toggle.Content = settings.MusicEnabled ? "MUSIC · ON" : "MUSIC · OFF";
            label.Text = $"MUSIC VOLUME · {Math.Round(settings.MusicVolume / .32 * 100):0}%";
        }
        toggle.Click += (_, _) => Commit(!settings.MusicEnabled, slider.Value);
        slider.ValueChanged += (_, args) => Commit(settings.MusicEnabled, args.NewValue);
        panel.Child = grid;
        return panel;
    }

    private void StartFreshRun(AdventureDefinition adventure, AdventureCharacterState character)
    {
        AdventureEngine engine = AdventureEngine.StartNew(adventure, character);
        progress.Runs[adventure.Id] = engine.State;
        progress.LastAdventureId = adventure.Id;
        selectedAdventureId = adventure.Id;
        persist(progress);
        ShowRun(adventure, engine.State);
    }

    private void RestartRun(AdventureDefinition adventure, AdventureRunState previous)
    {
        AdventureCharacterState character = AdventureCharacterCatalog.Create(adventure, previous.Character.ClassId, previous.Character.Name);
        StartFreshRun(adventure, character);
    }

    private void ShowRun(AdventureDefinition adventure, AdventureRunState run)
    {
        var engine = new AdventureEngine(adventure, AdventureRunState.Normalize(run));
        Content = new AdventureEncounterRunHost(
            engine,
            state =>
            {
                progress.Runs[adventure.Id] = state;
                progress.LastAdventureId = adventure.Id;
                persist(progress);
            },
            ShowHub,
            () => RestartRun(adventure, engine.State),
            recordCompletion,
            diceSetId,
            animationsEnabled);
    }

    private static string SelectedClassId(ComboBox classes) => classes.SelectedItem is ComboBoxItem { Tag: string id } ? id : "rogue";
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
