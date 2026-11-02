using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal static class AdventureLoungeModule
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
                "Build a character, make consequential choices and roll your equipped Midnight Dice cosmetic when the story calls for a check.",
                () => { },
                Artwork: CreateArtwork,
                SortOrder: 70,
                Status: () => Status(state)),
            back => new AdventureHubControl(
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
        if (!state.Runs.TryGetValue(AdventureCatalog.BlackStag.Id, out AdventureRunState? run))
            return "1 adventure · ready to begin";
        return run.Completed
            ? $"The Black Stag · ending: {run.EndingId ?? "complete"}"
            : $"The Black Stag · {run.Character.ClassName} · in progress";
    }

    private static FrameworkElement CreateArtwork()
    {
        var grid = new Grid { Width = 300, Height = 130 };
        var die = new AdventureD20
        {
            Width = 112,
            Height = 112,
            Value = 20,
            VisualVariant = "dice-obsidian",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        die.RenderTransform = new RotateTransform(-8, 56, 56);
        grid.Children.Add(die);
        return grid;
    }
}

internal sealed class AdventureHubControl : UserControl
{
    private readonly AdventureProgressState progress;
    private readonly Action<AdventureProgressState> persist;
    private readonly Func<string?> diceSetId;
    private readonly Func<string?> playerName;
    private readonly Func<bool> animationsEnabled;
    private readonly Func<AreaPresentationSettings> presentation;
    private readonly Action<AreaPresentationSettings> updatePresentation;
    private readonly Action<bool> setMusicActive;

    internal AdventureHubControl(
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

        Loaded += (_, _) => this.setMusicActive(true);
        Unloaded += (_, _) => this.setMusicActive(false);
        ShowHub();
    }

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

        var heading = new StackPanel { MaxWidth = 1040, HorizontalAlignment = HorizontalAlignment.Left };
        heading.Children.Add(AdventureUi.Eyebrow("TALES FROM THE BACKROOM", 12));
        heading.Children.Add(new TextBlock
        {
            Text = "Your story. Your roll.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 48,
            Foreground = AdventureUi.Ink("#F4E7D2"),
            Margin = new Thickness(0, 7, 0, 10),
        });
        heading.Children.Add(new TextBlock
        {
            Text = "A dark tabletop-adventure corner built to support separate campaigns, persistent choices, inventory, conditions and dice-driven consequences.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 16,
            LineHeight = 24,
            Foreground = AdventureUi.Ink("#AEBFB7"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 980,
        });
        root.Children.Add(heading);
        root.Children.Add(AdventureUi.OrnamentRule());
        root.Children.Add(BuildMusicPanel());

        AdventureDefinition adventure = AdventureCatalog.BlackStag;
        progress.Runs.TryGetValue(adventure.Id, out AdventureRunState? existing);

        Border adventureCard = AdventureUi.Panel("#14231F", "#0D1917", "#48645A", new Thickness(32), radius: 17);
        adventureCard.Margin = new Thickness(0, 0, 0, 24);
        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        var copy = new StackPanel { Margin = new Thickness(0, 2, 34, 2) };
        copy.Children.Add(AdventureUi.Eyebrow("ADVENTURE I · FIVE ACTS", 11));
        copy.Children.Add(new TextBlock
        {
            Text = adventure.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 38,
            Foreground = AdventureUi.Ink("#F5E5C8"),
            Margin = new Thickness(0, 7, 0, 8),
        });
        copy.Children.Add(new TextBlock
        {
            Text = adventure.Subtitle,
            Foreground = AdventureUi.Ink("#B7C7C0"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 760,
        });
        if (existing is not null)
        {
            copy.Children.Add(new TextBlock
            {
                Text = existing.Completed
                    ? $"COMPLETED · {Humanize(existing.EndingId ?? "ending discovered")} · {existing.RollsMade:N0} rolls"
                    : $"IN PROGRESS · {existing.Character.ClassName} · {existing.RollsMade:N0} rolls",
                Foreground = AdventureUi.Ink(existing.Completed ? "#E5C77C" : "#8FD1B4"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 17, 0, 0),
            });
        }
        cardGrid.Children.Add(copy);

        var art = new Grid();
        art.Children.Add(new Border
        {
            Width = 218,
            Height = 218,
            CornerRadius = new CornerRadius(109),
            BorderBrush = AdventureUi.Ink("#6E5E3D"),
            BorderThickness = new Thickness(1),
            Background = new RadialGradientBrush(
                (Color)ColorConverter.ConvertFromString("#2B3324"),
                (Color)ColorConverter.ConvertFromString("#0E1715")),
            Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = .25,
                Color = (Color)ColorConverter.ConvertFromString("#A98E53"),
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        art.Children.Add(new AdventureD20
        {
            Width = 172,
            Height = 172,
            Value = 20,
            VisualVariant = ResolveDiceVariant(diceSetId()),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(art, 1);
        cardGrid.Children.Add(art);
        adventureCard.Child = cardGrid;
        root.Children.Add(adventureCard);

        if (existing is not null && !existing.Completed)
        {
            Button resume = AdventureUi.Button("RESUME THE BLACK STAG", accent: true, fontSize: 13.5, scaleOnHover: false);
            resume.HorizontalAlignment = HorizontalAlignment.Left;
            resume.Margin = new Thickness(0, 0, 0, 24);
            resume.Click += (_, _) => ShowRun(existing);
            root.Children.Add(resume);
        }

        root.Children.Add(BuildCharacterCreator(existing, adventure));
        scroll.Content = root;
        Content = scroll;
    }

    private Border BuildCharacterCreator(AdventureRunState? existing, AdventureDefinition adventure)
    {
        Border panel = AdventureUi.Panel("#111D1A", "#0A1412", "#3B574F", new Thickness(28), radius: 16);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var chooser = new StackPanel();
        chooser.Children.Add(AdventureUi.Eyebrow(existing is null ? "CHOOSE YOUR CHARACTER" : "START A NEW RUN", 11));
        chooser.Children.Add(new TextBlock
        {
            Text = "Choose your path",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 28,
            Foreground = AdventureUi.Ink("#F0E0C6"),
            Margin = new Thickness(0, 6, 0, 13),
        });

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
        chooser.Children.Add(classes);

        Button start = AdventureUi.Button(existing is null ? "BEGIN ADVENTURE" : "BEGIN NEW RUN", accent: true, fontSize: 13.5, scaleOnHover: false);
        start.HorizontalAlignment = HorizontalAlignment.Left;
        start.Margin = new Thickness(0, 18, 0, 0);
        chooser.Children.Add(start);
        grid.Children.Add(chooser);

        var divider = new Border { Width = 1, Background = AdventureUi.Ink("#4A4936"), Margin = new Thickness(16, 0, 16, 0) };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var details = new StackPanel { Margin = new Thickness(4, 2, 0, 0) };
        var className = new TextBlock { FontFamily = new FontFamily("Georgia"), FontSize = 25, Foreground = AdventureUi.Ink("#EBD9B7") };
        var description = new TextBlock
        {
            Foreground = AdventureUi.Ink("#AEBFB7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 14,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12),
        };
        var stats = new TextBlock { Foreground = AdventureUi.Ink("#CDBA8B"), FontFamily = new FontFamily("Georgia"), FontSize = 12, TextWrapping = TextWrapping.Wrap };
        details.Children.Add(className);
        details.Children.Add(description);
        details.Children.Add(AdventureUi.OrnamentRule("◇"));
        details.Children.Add(stats);
        Grid.SetColumn(details, 2);
        grid.Children.Add(details);

        void UpdatePreview()
        {
            AdventureClassDefinition selected = AdventureCharacterCatalog.All.First(item => item.Id == SelectedClassId(classes));
            className.Text = selected.Name;
            description.Text = selected.Description;
            stats.Text = $"HP {selected.MaxHitPoints}   ·   Armor {selected.Armor}   ·   STR {selected.Abilities[AdventureAbility.Strength]}   DEX {selected.Abilities[AdventureAbility.Dexterity]}   INT {selected.Abilities[AdventureAbility.Intelligence]}   WIS {selected.Abilities[AdventureAbility.Wisdom]}";
        }
        classes.SelectionChanged += (_, _) => UpdatePreview();
        UpdatePreview();
        start.Click += (_, _) =>
        {
            AdventureCharacterState character = AdventureCharacterCatalog.Create(SelectedClassId(classes), playerName());
            AdventureEngine engine = AdventureEngine.StartNew(adventure, character);
            progress.Runs[adventure.Id] = engine.State;
            progress.LastAdventureId = adventure.Id;
            persist(progress);
            ShowRun(engine.State);
        };

        panel.Child = grid;
        return panel;
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
        copy.Children.Add(new TextBlock { Text = "A quiet fantasy score used only inside Tales from the Backroom.", FontFamily = new FontFamily("Georgia"), FontSize = 10.5, Foreground = AdventureUi.Ink("#839B91") });
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

    private void ShowRun(AdventureRunState run)
    {
        AdventureDefinition adventure = AdventureCatalog.BlackStag;
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
    internal static string ResolveDiceVariant(string? id) => CosmeticCatalog.All.FirstOrDefault(item => item.Kind == CosmeticKind.DiceSet && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.VisualVariant ?? "dice-ivory";
    internal static string ResolveDiceName(string? id) => CosmeticCatalog.All.FirstOrDefault(item => item.Kind == CosmeticKind.DiceSet && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.Name ?? "House Ivory";
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}

internal sealed class AdventureView : UserControl
{
    private readonly AdventureEngine engine;
    private readonly Action<AdventureRunState> persist;
    private readonly Action backToHub;
    private readonly Func<string?> diceSetId;
    private readonly Func<bool> animationsEnabled;
    private AdventureCheckResult? lastResult;
    private IReadOnlyList<AdventureFeedbackEvent> feedback = [];
    private AdventureD20? die;
    private Border? rollSurface;
    private bool rolling;

    internal AdventureView(AdventureEngine engine, Action<AdventureRunState> persist, Action backToHub, Func<string?> diceSetId, Func<bool> animationsEnabled)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.backToHub = backToHub ?? throw new ArgumentNullException(nameof(backToHub));
        this.diceSetId = diceSetId ?? throw new ArgumentNullException(nameof(diceSetId));
        this.animationsEnabled = animationsEnabled ?? throw new ArgumentNullException(nameof(animationsEnabled));
        feedback = engine.DrainFeedback();
        Render();
    }

    private void Render()
    {
        AdventureSceneDefinition scene = engine.CurrentScene;
        AdventureCheckDefinition? pending = engine.PendingCheck;
        var root = new Grid { Margin = new Thickness(28, 24, 28, 26) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 620 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Border story = BuildStory(scene, pending);
        story.Margin = new Thickness(0, 0, 20, 0);
        root.Children.Add(story);

        Border sheet = BuildCharacterSheet(pending);
        Grid.SetColumn(sheet, 1);
        root.Children.Add(sheet);

        Border hud = AdventureInventoryUi.Build(engine, feedback, animationsEnabled());
        Grid.SetRow(hud, 1);
        Grid.SetColumnSpan(hud, 2);
        root.Children.Add(hud);
        Content = root;
    }

    private Border BuildStory(AdventureSceneDefinition scene, AdventureCheckDefinition? pending)
    {
        Border story = AdventureUi.Panel("#101B18", "#091311", "#345047", new Thickness(32), radius: 17);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        string act = scene.ActId is not null && engine.Definition.Acts?.TryGetValue(scene.ActId, out string? actName) == true ? actName : engine.Definition.Title;
        title.Children.Add(AdventureUi.Eyebrow(act.ToUpperInvariant(), 11));
        title.Children.Add(new TextBlock { Text = scene.Title, FontFamily = new FontFamily("Georgia"), FontSize = 40, Foreground = AdventureUi.Ink("#F4E3C8"), Margin = new Thickness(0, 7, 0, 0) });
        if (!string.IsNullOrWhiteSpace(scene.Location))
            title.Children.Add(new TextBlock { Text = scene.Location.ToUpperInvariant(), FontFamily = new FontFamily("Georgia"), FontSize = 10, Foreground = AdventureUi.Ink("#708E83"), Margin = new Thickness(1, 5, 0, 0) });
        header.Children.Add(title);

        Button back = AdventureUi.Button("← ADVENTURES", fontSize: 11.5, scaleOnHover: false);
        back.VerticalAlignment = VerticalAlignment.Top;
        back.IsEnabled = !rolling;
        back.Click += (_, _) =>
        {
            if (rolling) return;
            persist(engine.State);
            backToHub();
        };
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        grid.Children.Add(header);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var body = new StackPanel { Margin = new Thickness(0, 2, 12, 0) };
        body.Children.Add(AdventureUi.OrnamentRule());
        if (!string.IsNullOrWhiteSpace(scene.Speaker))
            body.Children.Add(new TextBlock { Text = scene.Speaker.ToUpperInvariant(), Foreground = AdventureUi.Ink("#8FD0B5"), FontFamily = new FontFamily("Georgia"), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        body.Children.Add(new TextBlock { Text = scene.Body, FontFamily = new FontFamily("Georgia"), FontSize = 20, LineHeight = 32, Foreground = AdventureUi.Ink("#E1D3BF"), TextWrapping = TextWrapping.Wrap, MaxWidth = 1080 });
        if (lastResult is not null) body.Children.Add(BuildResult(lastResult));

        if (engine.State.Completed)
        {
            Border ending = AdventureUi.Panel("#1B2B22", "#101A16", "#8D7646", new Thickness(20), radius: 12, shadow: false);
            ending.Margin = new Thickness(0, 28, 0, 0);
            ending.Child = new TextBlock
            {
                Text = $"ADVENTURE COMPLETE  ·  {Humanize(engine.State.EndingId ?? "ending").ToUpperInvariant()}\n{engine.State.RollsMade} rolls · {engine.State.NaturalTwenties} natural 20s · {engine.State.NaturalOnes} natural 1s · {engine.State.Character.Experience} character XP",
                Foreground = AdventureUi.Ink("#E8CF8E"), FontFamily = new FontFamily("Georgia"), FontSize = 13, LineHeight = 21, TextWrapping = TextWrapping.Wrap,
            };
            body.Children.Add(ending);
            Button returnButton = AdventureUi.Button("RETURN TO ADVENTURES", accent: true, fontSize: 13, scaleOnHover: false);
            returnButton.HorizontalAlignment = HorizontalAlignment.Left;
            returnButton.Margin = new Thickness(0, 20, 0, 0);
            returnButton.Click += (_, _) => { persist(engine.State); backToHub(); };
            body.Children.Add(returnButton);
        }
        else if (pending is not null)
        {
            Border waiting = AdventureUi.Panel("#1C271E", "#101914", "#8F7847", new Thickness(19), radius: 12, shadow: false);
            waiting.Margin = new Thickness(0, 28, 0, 0);
            waiting.Child = new TextBlock
            {
                Text = pending.RollMode == AdventureRollMode.Disadvantage
                    ? "FATE IS WAITING · DISADVANTAGE\nAn active condition is making this check harder. Roll the D20 to continue."
                    : pending.RollMode == AdventureRollMode.Advantage
                        ? "FATE IS WAITING · ADVANTAGE\nRoll the D20 to continue."
                        : "FATE IS WAITING FOR YOUR ROLL\nThe relevant ability and skill are highlighted on your character sheet.",
                Foreground = AdventureUi.Ink("#DCC793"), FontFamily = new FontFamily("Georgia"), FontSize = 13, LineHeight = 21, TextWrapping = TextWrapping.Wrap,
            };
            body.Children.Add(waiting);
        }
        else
        {
            body.Children.Add(new TextBlock { Text = "WHAT DO YOU DO?", Foreground = AdventureUi.Ink("#8FD0B5"), FontFamily = new FontFamily("Georgia"), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 30, 0, 12) });
            foreach (AdventureChoiceDefinition choice in engine.AvailableChoices()) body.Children.Add(BuildChoice(choice));
        }

        scroll.Content = body;
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        story.Child = grid;
        return story;
    }

    private FrameworkElement BuildChoice(AdventureChoiceDefinition choice)
    {
        Button button = AdventureUi.Button("", accent: choice.Check is not null, fontSize: 14.5, scaleOnHover: false);
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Padding = new Thickness(18, 14, 18, 14);
        button.Margin = new Thickness(0, 0, 0, 10);
        button.MinHeight = 58;
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock { Text = choice.Text, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Georgia"), FontSize = 15, LineHeight = 22, Foreground = AdventureUi.Ink("#EEE1CD") });
        if (choice.Check is AdventureCheckDefinition check)
            copy.Children.Add(new TextBlock { Text = $"{CheckLabel(check).ToUpperInvariant()} CHECK  ·  DC {check.Difficulty}", Foreground = AdventureUi.Ink("#D5BB77"), FontFamily = new FontFamily("Georgia"), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 7, 0, 0) });
        button.Content = copy;
        button.Click += (_, _) =>
        {
            lastResult = null;
            engine.SelectChoice(choice.Id);
            feedback = engine.DrainFeedback();
            persist(engine.State);
            Render();
        };
        return button;
    }

    private Border BuildCharacterSheet(AdventureCheckDefinition? pending)
    {
        Border sheet = AdventureUi.Panel("#13201D", "#091310", "#3C5A51", new Thickness(24), radius: 17);
        var body = new StackPanel();
        AdventureCharacterState character = engine.State.Character;
        body.Children.Add(AdventureUi.Eyebrow("CHARACTER SHEET", 11));
        body.Children.Add(new TextBlock { Text = character.Name, FontFamily = new FontFamily("Georgia"), FontSize = 32, Foreground = AdventureUi.Ink("#F4E2C5"), Margin = new Thickness(0, 6, 0, 2) });
        body.Children.Add(new TextBlock { Text = $"{character.ClassName} · Level {character.Level}", Foreground = AdventureUi.Ink("#90D0B5"), FontFamily = new FontFamily("Georgia"), FontSize = 13, Margin = new Thickness(0, 0, 0, 7) });
        body.Children.Add(AdventureUi.OrnamentRule("◇"));

        var vitals = new Grid { Margin = new Thickness(0, 0, 0, 13) };
        for (int i = 0; i < 3; i++) vitals.ColumnDefinitions.Add(new ColumnDefinition());
        Border hp = AdventureUi.StatTile("HP", $"{character.CurrentHitPoints}/{character.MaxHitPoints}");
        Border armor = AdventureUi.StatTile("ARMOR", character.Armor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Border xp = AdventureUi.StatTile("XP", character.Experience.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Grid.SetColumn(hp, 0); Grid.SetColumn(armor, 1); Grid.SetColumn(xp, 2);
        vitals.Children.Add(hp); vitals.Children.Add(armor); vitals.Children.Add(xp);
        body.Children.Add(vitals);

        body.Children.Add(AdventureUi.SectionLabel("ABILITIES"));
        var abilities = new UniformGrid { Columns = 3, Margin = new Thickness(0, 2, 0, 13) };
        foreach (AdventureAbility ability in Enum.GetValues<AdventureAbility>())
        {
            int score = character.AbilityScore(ability);
            int modifier = character.AbilityModifier(ability);
            abilities.Children.Add(AdventureUi.StatTile(ShortAbility(ability), $"{score}   {Signed(modifier)}", pending?.Ability == ability));
        }
        body.Children.Add(abilities);

        body.Children.Add(AdventureUi.SectionLabel("TRAINED SKILLS"));
        foreach (AdventureSkillState skill in character.Skills.Values.OrderBy(item => item.Name))
        {
            bool highlighted = string.Equals(pending?.SkillId, skill.Id, StringComparison.OrdinalIgnoreCase);
            var line = new Grid { Margin = new Thickness(2, 3, 2, 3) };
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            line.Children.Add(new TextBlock { Text = skill.Name, FontFamily = new FontFamily("Georgia"), Foreground = AdventureUi.Ink(highlighted ? "#E8CE89" : "#B6C8C0"), FontSize = 12, FontWeight = highlighted ? FontWeights.Bold : FontWeights.Normal });
            var bonus = new TextBlock { Text = Signed(character.AbilityModifier(skill.Ability) + skill.ProficiencyBonus), FontFamily = new FontFamily("Georgia"), Foreground = AdventureUi.Ink(highlighted ? "#E8CE89" : "#8EA59C"), FontSize = 12, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(bonus, 1);
            line.Children.Add(bonus);
            body.Children.Add(line);
        }

        if (engine.State.ActiveConditions.Count > 0)
        {
            body.Children.Add(AdventureUi.SectionLabel("ACTIVE CONDITIONS"));
            body.Children.Add(new TextBlock
            {
                Text = string.Join(" · ", engine.State.ActiveConditions.Values.Select(state => engine.ConditionDefinition(state.ConditionId)?.Name ?? Humanize(state.ConditionId))),
                Foreground = AdventureUi.Ink("#C9A978"), FontFamily = new FontFamily("Georgia"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 1, 0, 2),
            });
        }

        body.Children.Add(BuildDiceTray(pending));
        sheet.Child = body;
        return sheet;
    }

    private Border BuildDiceTray(AdventureCheckDefinition? pending)
    {
        Border tray = AdventureUi.Panel("#0D1815", "#07100E", pending is null ? "#314A43" : "#8F7847", new Thickness(16), radius: 14, shadow: false);
        tray.Margin = new Thickness(0, 10, 0, 0);
        var body = new StackPanel();
        string? equipped = diceSetId();
        body.Children.Add(new TextBlock { Text = pending is null ? "D20 TRAY" : $"{CheckLabel(pending).ToUpperInvariant()} · DC {pending.Difficulty}", Foreground = AdventureUi.Ink(pending is null ? "#7F9B91" : "#E8CD87"), FontFamily = new FontFamily("Georgia"), FontSize = pending is null ? 11 : 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        if (pending is not null)
        {
            int ability = engine.State.Character.AbilityModifier(pending.Ability);
            int skill = engine.State.Character.SkillBonus(pending.SkillId);
            body.Children.Add(new TextBlock { Text = $"Modifier {Signed(ability + skill + pending.Modifier)} · {ShortAbility(pending.Ability)} {Signed(ability)}" + (pending.SkillId is null ? "" : $" · training {Signed(skill)}"), Foreground = AdventureUi.Ink("#B9C6BD"), FontFamily = new FontFamily("Georgia"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 6) });
        }

        die = new AdventureD20 { Width = 166, Height = 166, Value = pending is null ? lastResult?.ChosenRoll ?? 20 : 0, Active = pending is not null, VisualVariant = AdventureHubControl.ResolveDiceVariant(equipped) };
        rollSurface = AdventureUi.Panel(pending is null ? "#101B18" : "#17221A", "#08100E", pending is null ? "#2B433D" : "#9A814B", new Thickness(8), radius: 15, shadow: false);
        rollSurface.Width = 212;
        rollSurface.Height = 188;
        rollSurface.HorizontalAlignment = HorizontalAlignment.Center;
        rollSurface.Margin = new Thickness(0, 6, 0, 4);
        rollSurface.Cursor = pending is null ? CasinoCursors.Arrow : CasinoCursors.Hand;
        rollSurface.Child = die;
        if (pending is not null)
        {
            Brush border = rollSurface.BorderBrush;
            rollSurface.MouseEnter += (_, _) => { if (!rolling && rollSurface is not null) { rollSurface.BorderBrush = AdventureUi.Ink("#D8BA6D"); rollSurface.Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Opacity = .25, Color = (Color)ColorConverter.ConvertFromString("#B39250") }; } };
            rollSurface.MouseLeave += (_, _) => { if (rollSurface is not null) { rollSurface.BorderBrush = border; rollSurface.Effect = null; } };
            rollSurface.MouseLeftButtonUp += async (_, _) => await RollAsync(pending);
        }
        body.Children.Add(rollSurface);
        body.Children.Add(new TextBlock { Text = AdventureHubControl.ResolveDiceName(equipped), Foreground = AdventureUi.Ink("#B49D68"), FontFamily = new FontFamily("Georgia"), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
        body.Children.Add(new TextBlock { Text = pending is not null ? "CLICK THE D20 TO ROLL" : lastResult is not null ? $"LAST ROLL · {lastResult.ChosenRoll}" : "Waiting for the story", Foreground = AdventureUi.Ink(pending is not null ? "#E0C77F" : lastResult is not null ? "#CBB579" : "#718A82"), FontFamily = new FontFamily("Georgia"), FontSize = 10.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) });
        tray.Child = body;
        return tray;
    }

    private async Task RollAsync(AdventureCheckDefinition check)
    {
        if (rolling || rollSurface is null || die is null) return;
        rolling = true;
        rollSurface.IsHitTestVisible = false;
        int first = Random.Shared.Next(1, 21);
        int? second = check.RollMode == AdventureRollMode.Normal ? null : Random.Shared.Next(1, 21);
        int chosen = check.RollMode switch
        {
            AdventureRollMode.Advantage => Math.Max(first, second!.Value),
            AdventureRollMode.Disadvantage => Math.Min(first, second!.Value),
            _ => first,
        };
        if (animationsEnabled())
        {
            die.PlayRollMotion();
            int[] pauses = [90, 95, 100, 110, 120, 130, 145, 160, 180, 200];
            foreach (int pause in pauses) { die.Value = Random.Shared.Next(1, 21); await Task.Delay(pause); }
            die.Value = chosen;
            await Task.Delay(230);
        }
        else die.Value = chosen;

        lastResult = engine.ResolvePendingCheck(first, second);
        feedback = engine.DrainFeedback();
        persist(engine.State);
        rolling = false;
        Render();
    }

    private static Border BuildResult(AdventureCheckResult result)
    {
        bool success = result.Succeeded;
        string headline = result.Outcome switch
        {
            AdventureCheckOutcome.CriticalSuccess => "NATURAL 20 · CRITICAL SUCCESS",
            AdventureCheckOutcome.CriticalFailure => "NATURAL 1 · CRITICAL FAILURE",
            AdventureCheckOutcome.Success => "SUCCESS",
            _ => "FAILURE",
        };
        Border border = AdventureUi.Panel(success ? "#173024" : "#301D1B", success ? "#0D1C16" : "#1A1110", success ? "#648A65" : "#7B4A45", new Thickness(18), radius: 12, shadow: false);
        border.Margin = new Thickness(0, 23, 0, 0);
        border.Child = new TextBlock { Text = $"{headline}\nD20 {result.ChosenRoll} + modifiers {Signed(result.AbilityModifier + result.SkillBonus + result.AdditionalModifier)} = {result.Total} · DC {result.Difficulty}", Foreground = AdventureUi.Ink(success ? "#C6DEB9" : "#E0B0A7"), FontFamily = new FontFamily("Georgia"), FontSize = 13, LineHeight = 21, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        return border;
    }

    private static string CheckLabel(AdventureCheckDefinition check) => string.IsNullOrWhiteSpace(check.SkillId) ? check.Ability.ToString() : AdventureCharacterCatalog.Skill(check.SkillId).Name;
    private static string ShortAbility(AdventureAbility ability) => ability switch
    {
        AdventureAbility.Strength => "STR", AdventureAbility.Dexterity => "DEX", AdventureAbility.Constitution => "CON",
        AdventureAbility.Intelligence => "INT", AdventureAbility.Wisdom => "WIS", AdventureAbility.Charisma => "CHA", _ => ability.ToString().ToUpperInvariant(),
    };
    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
