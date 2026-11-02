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
    internal static LoungeHostedGame Create(
        AdventureProgressState progress,
        Action<AdventureProgressState> persist,
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
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var root = new StackPanel
        {
            Margin = new Thickness(56, 38, 56, 52),
            MaxWidth = 1320,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var heading = new StackPanel { MaxWidth = 1080, HorizontalAlignment = HorizontalAlignment.Left };
        heading.Children.Add(AdventureUi.Eyebrow("TALES FROM THE BACKROOM", 12));
        heading.Children.Add(new TextBlock
        {
            Text = "Choose a tale. Carry the consequences.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 46,
            Foreground = AdventureUi.Ink("#F4E7D2"),
            Margin = new Thickness(0, 7, 0, 10),
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Each adventure is its own content package, but all of them share the same character, dice, inventory, condition and save-state blueprint.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15.5,
            LineHeight = 24,
            Foreground = AdventureUi.Ink("#AEBFB7"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 980,
        });
        root.Children.Add(heading);
        root.Children.Add(AdventureUi.OrnamentRule());
        root.Children.Add(BuildMusicPanel());

        root.Children.Add(AdventureUi.Eyebrow("ADVENTURE LIBRARY", 11));
        var library = new StackPanel { Margin = new Thickness(0, 10, 0, 24) };
        foreach (AdventureDefinition adventure in AdventureCatalog.All)
            library.Children.Add(BuildAdventureCard(adventure));
        root.Children.Add(library);

        root.Children.Add(BuildCharacterCreator(SelectedAdventure));
        scroll.Content = root;
        Content = scroll;
    }

    private Border BuildAdventureCard(AdventureDefinition adventure)
    {
        progress.Runs.TryGetValue(adventure.Id, out AdventureRunState? run);
        bool selected = string.Equals(adventure.Id, selectedAdventureId, StringComparison.OrdinalIgnoreCase);
        Border card = AdventureUi.Panel(
            selected ? "#172820" : "#14231F",
            "#0D1917",
            selected ? "#9B8149" : "#48645A",
            new Thickness(25),
            radius: 15);
        card.Margin = new Thickness(0, 0, 0, 12);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Margin = new Thickness(0, 0, 22, 0) };
        copy.Children.Add(AdventureUi.Eyebrow($"{(adventure.Acts?.Count ?? 0)} ACTS · {adventure.Scenes.Count} SCENES", 9.5));
        copy.Children.Add(new TextBlock
        {
            Text = adventure.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 29,
            Foreground = AdventureUi.Ink("#F2DFC0"),
            Margin = new Thickness(0, 5, 0, 5),
        });
        copy.Children.Add(new TextBlock
        {
            Text = adventure.Subtitle,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12.5,
            LineHeight = 19,
            Foreground = AdventureUi.Ink("#AABDB4"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 720,
        });
        grid.Children.Add(copy);

        var state = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 20, 0) };
        state.Children.Add(new TextBlock
        {
            Text = run is null ? "UNPLAYED" : run.Completed ? "COMPLETED" : "IN PROGRESS",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink(run?.Completed == true ? "#D7BC75" : run is null ? "#728E84" : "#83C4A8"),
        });
        if (run is not null)
        {
            state.Children.Add(new TextBlock
            {
                Text = run.Completed
                    ? Humanize(run.EndingId ?? "ending discovered")
                    : $"{run.Character.ClassName} · {run.RollsMade} rolls",
                FontFamily = new FontFamily("Georgia"),
                FontSize = 11.5,
                Foreground = AdventureUi.Ink("#AABAB3"),
                Margin = new Thickness(0, 4, 0, 0),
            });
        }
        Grid.SetColumn(state, 1);
        grid.Children.Add(state);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (run is not null && !run.Completed)
        {
            Button resume = AdventureUi.Button("RESUME", accent: true, fontSize: 11.5, scaleOnHover: false);
            resume.Margin = new Thickness(0, 0, 0, 7);
            resume.Click += (_, _) => ShowRun(adventure, run);
            actions.Children.Add(resume);
        }
        Button select = AdventureUi.Button(run is null ? "SELECT" : "NEW RUN", accent: selected, fontSize: 11, scaleOnHover: false);
        select.Click += (_, _) =>
        {
            selectedAdventureId = adventure.Id;
            progress.LastAdventureId = adventure.Id;
            persist(progress);
            ShowHub();
        };
        actions.Children.Add(select);
        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);

        card.Child = grid;
        return card;
    }

    private Border BuildCharacterCreator(AdventureDefinition adventure)
    {
        Border panel = AdventureUi.Panel("#111D1A", "#0A1412", "#3B574F", new Thickness(27), radius: 16);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
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
        var classes = BuildClassSelector();
        chooser.Children.Add(classes);
        Button start = AdventureUi.Button("BEGIN NEW RUN", accent: true, fontSize: 13, scaleOnHover: false);
        start.HorizontalAlignment = HorizontalAlignment.Left;
        start.Margin = new Thickness(0, 17, 0, 0);
        start.Click += (_, _) =>
        {
            AdventureCharacterState character = AdventureCharacterCatalog.Create(SelectedClassId(classes), playerName());
            AdventureEngine engine = AdventureEngine.StartNew(adventure, character);
            progress.Runs[adventure.Id] = engine.State;
            progress.LastAdventureId = adventure.Id;
            persist(progress);
            ShowRun(adventure, engine.State);
        };
        chooser.Children.Add(start);
        grid.Children.Add(chooser);

        var divider = new Border { Width = 1, Background = AdventureUi.Ink("#4A4936"), Margin = new Thickness(16, 0, 16, 0) };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var preview = new StackPanel { Margin = new Thickness(4, 2, 0, 0) };
        var className = new TextBlock { FontFamily = new FontFamily("Georgia"), FontSize = 24, Foreground = AdventureUi.Ink("#EBD9B7") };
        var description = new TextBlock { Foreground = AdventureUi.Ink("#AEBFB7"), FontFamily = new FontFamily("Georgia"), FontSize = 13.5, LineHeight = 21, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 10) };
        var stats = new TextBlock { Foreground = AdventureUi.Ink("#CDBA8B"), FontFamily = new FontFamily("Georgia"), FontSize = 11.5, TextWrapping = TextWrapping.Wrap };
        preview.Children.Add(className);
        preview.Children.Add(description);
        preview.Children.Add(AdventureUi.OrnamentRule("◇"));
        preview.Children.Add(stats);
        Grid.SetColumn(preview, 2);
        grid.Children.Add(preview);

        void UpdatePreview()
        {
            AdventureClassDefinition definition = AdventureCharacterCatalog.All.First(item => item.Id == SelectedClassId(classes));
            className.Text = definition.Name;
            description.Text = definition.Description;
            stats.Text = $"HP {definition.MaxHitPoints} · Armor {definition.Armor} · STR {definition.Abilities[AdventureAbility.Strength]} · DEX {definition.Abilities[AdventureAbility.Dexterity]} · INT {definition.Abilities[AdventureAbility.Intelligence]} · WIS {definition.Abilities[AdventureAbility.Wisdom]}";
        }
        classes.SelectionChanged += (_, _) => UpdatePreview();
        UpdatePreview();
        panel.Child = grid;
        return panel;
    }

    private static ComboBox BuildClassSelector()
    {
        var classes = new ComboBox
        {
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 9, 12, 9),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            Background = AdventureUi.Ink("#0B1714"),
            Foreground = AdventureUi.Ink("#F1E4D0"),
            BorderBrush = AdventureUi.Ink("#65755D"),
            BorderThickness = new Thickness(1),
        };
        ComboBoxItem? rogue = null;
        foreach (AdventureClassDefinition definition in AdventureCharacterCatalog.All)
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
            if (definition.Id == "rogue") rogue = item;
        }
        classes.SelectedItem = rogue ?? classes.Items[0];
        return classes;
    }

    private Border BuildMusicPanel()
    {
        AreaPresentationSettings settings = presentation().Sanitize();
        Border panel = AdventureUi.Panel("#151F1B", "#0B1412", "#5B5942", new Thickness(18, 15, 18, 15), radius: 12, shadow: false);
        panel.Margin = new Thickness(0, 0, 0, 24);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });
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
        var slider = new Slider { Minimum = 0, Maximum = .32, Value = settings.MusicVolume, Width = 280, Height = 22, IsMoveToPointEnabled = true, Cursor = CasinoCursors.Hand };
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

    private void ShowRun(AdventureDefinition adventure, AdventureRunState run)
    {
        var engine = new AdventureEngine(adventure, AdventureRunState.Normalize(run));
        Content = new AdventureView(
            engine,
            state =>
            {
                progress.Runs[adventure.Id] = state;
                progress.LastAdventureId = adventure.Id;
                persist(progress);
            },
            ShowHub,
            diceSetId,
            animationsEnabled);
    }

    private static string SelectedClassId(ComboBox classes) => classes.SelectedItem is ComboBoxItem { Tag: string id } ? id : "rogue";
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
