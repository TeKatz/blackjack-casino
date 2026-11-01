using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal static class AdventureLoungeModule
{
    internal static LoungeHostedGame Create(AdventureProgressState progress, Action<AdventureProgressState> persist, Func<string?> diceSetId, Func<string?> playerName, Func<bool> animationsEnabled)
    {
        AdventureProgressState state = AdventureProgressState.Normalize(progress);
        return new LoungeHostedGame(
            new LoungeGame("tales-backroom", "Tales from the Backroom", "A tabletop story waits behind the lounge.",
                "Build a character, make consequential choices and roll your equipped Midnight Dice cosmetic when the story calls for a check.",
                () => { }, Artwork: CreateArtwork, SortOrder: 70, Status: () => Status(state)),
            back => new AdventureHubControl(state, persist, diceSetId, playerName, animationsEnabled, back));
    }

    private static string Status(AdventureProgressState state)
    {
        if (!state.Runs.TryGetValue(AdventureCatalog.BlackStag.Id, out AdventureRunState? run)) return "1 adventure · ready to begin";
        return run.Completed ? $"The Black Stag · ending: {run.EndingId ?? "complete"}" : $"The Black Stag · {run.Character.ClassName} · in progress";
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

    internal AdventureHubControl(AdventureProgressState progress, Action<AdventureProgressState> persist, Func<string?> diceSetId, Func<string?> playerName, Func<bool> animationsEnabled, Action backToGames)
    {
        this.progress = AdventureProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.diceSetId = diceSetId ?? throw new ArgumentNullException(nameof(diceSetId));
        this.playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        this.animationsEnabled = animationsEnabled ?? throw new ArgumentNullException(nameof(animationsEnabled));
        _ = backToGames ?? throw new ArgumentNullException(nameof(backToGames));
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
            Text = "A tabletop adventure corner inside the Chill Lounge. Build a character, make consequential choices and let the story call for your equipped D20 when fate has something to say.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 16,
            LineHeight = 24,
            Foreground = AdventureUi.Ink("#AEBFB7"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 980,
        });
        root.Children.Add(heading);
        root.Children.Add(AdventureUi.OrnamentRule());

        AdventureDefinition adventure = AdventureCatalog.BlackStag;
        progress.Runs.TryGetValue(adventure.Id, out AdventureRunState? existing);

        Border adventureCard = AdventureUi.Panel("#14231F", "#0D1917", "#48645A", new Thickness(32), radius: 17);
        adventureCard.Margin = new Thickness(0, 0, 0, 24);
        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

        var cardCopy = new StackPanel { Margin = new Thickness(0, 2, 34, 2) };
        cardCopy.Children.Add(AdventureUi.Eyebrow("ADVENTURE I", 11));
        cardCopy.Children.Add(new TextBlock
        {
            Text = adventure.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 38,
            Foreground = AdventureUi.Ink("#F5E5C8"),
            Margin = new Thickness(0, 7, 0, 8),
        });
        cardCopy.Children.Add(new TextBlock
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
            cardCopy.Children.Add(new TextBlock
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
        cardGrid.Children.Add(cardCopy);

        var art = new Grid();
        var halo = new Border
        {
            Width = 218,
            Height = 218,
            CornerRadius = new CornerRadius(109),
            BorderBrush = AdventureUi.Ink("#6E5E3D"),
            BorderThickness = new Thickness(1),
            Background = new RadialGradientBrush(
                (Color)ColorConverter.ConvertFromString("#2B3324"),
                (Color)ColorConverter.ConvertFromString("#0E1715")),
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Opacity = .25, Color = (Color)ColorConverter.ConvertFromString("#A98E53") },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        art.Children.Add(halo);
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
            Button resume = AdventureUi.Button("RESUME THE BLACK STAG", accent: true, fontSize: 13.5);
            resume.HorizontalAlignment = HorizontalAlignment.Left;
            resume.Margin = new Thickness(0, 0, 0, 24);
            resume.Click += (_, _) => ShowRun(existing);
            root.Children.Add(resume);
        }

        Border characterPanel = AdventureUi.Panel("#111D1A", "#0A1412", "#3B574F", new Thickness(28), radius: 16);
        var characterGrid = new Grid();
        characterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        characterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        characterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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
        ComboBoxItem? rogueItem = null;
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
            if (string.Equals(definition.Id, "rogue", StringComparison.OrdinalIgnoreCase)) rogueItem = item;
        }
        classes.SelectedItem = rogueItem ?? classes.Items[0];
        chooser.Children.Add(classes);

        var start = AdventureUi.Button(existing is null ? "BEGIN ADVENTURE" : "BEGIN NEW RUN", accent: true, fontSize: 13.5);
        start.HorizontalAlignment = HorizontalAlignment.Left;
        start.Margin = new Thickness(0, 18, 0, 0);
        chooser.Children.Add(start);
        characterGrid.Children.Add(chooser);

        Border divider = new()
        {
            Width = 1,
            Background = AdventureUi.Ink("#4A4936"),
            Margin = new Thickness(16, 0, 16, 0),
        };
        Grid.SetColumn(divider, 1);
        characterGrid.Children.Add(divider);

        var details = new StackPanel { Margin = new Thickness(4, 2, 0, 0) };
        var className = new TextBlock
        {
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            Foreground = AdventureUi.Ink("#EBD9B7"),
        };
        var classDescription = new TextBlock
        {
            Foreground = AdventureUi.Ink("#AEBFB7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 14,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12),
        };
        var classStats = new TextBlock
        {
            Foreground = AdventureUi.Ink("#CDBA8B"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };
        details.Children.Add(className);
        details.Children.Add(classDescription);
        details.Children.Add(AdventureUi.OrnamentRule("◇"));
        details.Children.Add(classStats);
        Grid.SetColumn(details, 2);
        characterGrid.Children.Add(details);

        void UpdateClassPreview()
        {
            string classId = SelectedClassId(classes);
            AdventureClassDefinition definition = AdventureCharacterCatalog.All.First(item => item.Id == classId);
            className.Text = definition.Name;
            classDescription.Text = definition.Description;
            classStats.Text = $"HP {definition.MaxHitPoints}   ·   Armor {definition.Armor}   ·   " +
                $"STR {definition.Abilities[AdventureAbility.Strength]}   DEX {definition.Abilities[AdventureAbility.Dexterity]}   " +
                $"INT {definition.Abilities[AdventureAbility.Intelligence]}   WIS {definition.Abilities[AdventureAbility.Wisdom]}";
        }

        classes.SelectionChanged += (_, _) => UpdateClassPreview();
        UpdateClassPreview();
        start.Click += (_, _) =>
        {
            string classId = SelectedClassId(classes);
            AdventureCharacterState character = AdventureCharacterCatalog.Create(classId, playerName());
            AdventureEngine engine = AdventureEngine.StartNew(adventure, character);
            progress.Runs[adventure.Id] = engine.State;
            progress.LastAdventureId = adventure.Id;
            persist(progress);
            ShowRun(engine.State);
        };

        characterPanel.Child = characterGrid;
        root.Children.Add(characterPanel);
        scroll.Content = root;
        Content = scroll;
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

    private static string SelectedClassId(ComboBox classes) =>
        classes.SelectedItem is ComboBoxItem { Tag: string id } ? id : "rogue";

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
        Render();
    }

    private void Render()
    {
        AdventureSceneDefinition scene = engine.CurrentScene;
        AdventureCheckDefinition? pending = engine.PendingCheck;

        var root = new Grid
        {
            Margin = new Thickness(28, 24, 28, 30),
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 620 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });

        Border story = AdventureUi.Panel("#101B18", "#091311", "#345047", new Thickness(32), radius: 17);
        story.Margin = new Thickness(0, 0, 20, 0);
        var storyGrid = new Grid();
        storyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        storyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var storyHeader = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        storyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        storyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleStack = new StackPanel();
        titleStack.Children.Add(AdventureUi.Eyebrow(engine.Definition.Title.ToUpperInvariant(), 11));
        titleStack.Children.Add(new TextBlock
        {
            Text = scene.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 40,
            Foreground = AdventureUi.Ink("#F4E3C8"),
            Margin = new Thickness(0, 7, 0, 0),
        });
        storyHeader.Children.Add(titleStack);

        Button back = AdventureUi.Button("← ADVENTURES", fontSize: 11.5);
        back.VerticalAlignment = VerticalAlignment.Top;
        back.IsEnabled = !rolling;
        back.Click += (_, _) =>
        {
            if (rolling) return;
            persist(engine.State);
            backToHub();
        };
        Grid.SetColumn(back, 1);
        storyHeader.Children.Add(back);
        storyGrid.Children.Add(storyHeader);

        var storyScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var storyBody = new StackPanel { Margin = new Thickness(0, 2, 12, 0) };
        storyBody.Children.Add(AdventureUi.OrnamentRule());

        if (!string.IsNullOrWhiteSpace(scene.Speaker))
        {
            storyBody.Children.Add(new TextBlock
            {
                Text = scene.Speaker.ToUpperInvariant(),
                Foreground = AdventureUi.Ink("#8FD0B5"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        storyBody.Children.Add(new TextBlock
        {
            Text = scene.Body,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 20,
            LineHeight = 32,
            Foreground = AdventureUi.Ink("#E1D3BF"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 1080,
        });

        if (lastResult is not null) storyBody.Children.Add(BuildResult(lastResult));

        if (engine.State.Completed)
        {
            Border ending = AdventureUi.Panel("#1B2B22", "#101A16", "#8D7646", new Thickness(20), radius: 12, shadow: false);
            ending.Margin = new Thickness(0, 28, 0, 0);
            ending.Child = new TextBlock
            {
                Text = $"ADVENTURE COMPLETE  ·  {Humanize(engine.State.EndingId ?? "ending").ToUpperInvariant()}\n{engine.State.RollsMade} rolls · {engine.State.NaturalTwenties} natural 20s · {engine.State.NaturalOnes} natural 1s · {engine.State.Character.Experience} character XP",
                Foreground = AdventureUi.Ink("#E8CF8E"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 13,
                LineHeight = 21,
                TextWrapping = TextWrapping.Wrap,
            };
            storyBody.Children.Add(ending);

            Button returnButton = AdventureUi.Button("RETURN TO ADVENTURES", accent: true, fontSize: 13);
            returnButton.HorizontalAlignment = HorizontalAlignment.Left;
            returnButton.Margin = new Thickness(0, 20, 0, 0);
            returnButton.Click += (_, _) =>
            {
                persist(engine.State);
                backToHub();
            };
            storyBody.Children.Add(returnButton);
        }
        else if (pending is not null)
        {
            Border waiting = AdventureUi.Panel("#1C271E", "#101914", "#8F7847", new Thickness(19), radius: 12, shadow: false);
            waiting.Margin = new Thickness(0, 28, 0, 0);
            var waitingCopy = new StackPanel();
            waitingCopy.Children.Add(new TextBlock
            {
                Text = "FATE IS WAITING FOR YOUR ROLL",
                Foreground = AdventureUi.Ink("#E7CB85"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
            });
            waitingCopy.Children.Add(new TextBlock
            {
                Text = "The relevant ability and skill are highlighted on your character sheet. Roll the D20 to let the story continue.",
                Foreground = AdventureUi.Ink("#C7BA9C"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
            });
            waiting.Child = waitingCopy;
            storyBody.Children.Add(waiting);
        }
        else
        {
            storyBody.Children.Add(new TextBlock
            {
                Text = "WHAT DO YOU DO?",
                Foreground = AdventureUi.Ink("#8FD0B5"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 30, 0, 12),
            });
            foreach (AdventureChoiceDefinition choice in engine.AvailableChoices()) storyBody.Children.Add(BuildChoice(choice));
        }

        storyScroll.Content = storyBody;
        Grid.SetRow(storyScroll, 1);
        storyGrid.Children.Add(storyScroll);
        story.Child = storyGrid;
        root.Children.Add(story);

        Border sheet = BuildCharacterSheet(pending);
        Grid.SetColumn(sheet, 1);
        root.Children.Add(sheet);
        Content = root;
    }

    private FrameworkElement BuildChoice(AdventureChoiceDefinition choice)
    {
        Button button = AdventureUi.Button("", accent: choice.Check is not null, fontSize: 14.5);
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Padding = new Thickness(18, 14, 18, 14);
        button.Margin = new Thickness(0, 0, 0, 10);
        button.MinHeight = 58;

        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = choice.Text,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            LineHeight = 22,
            Foreground = AdventureUi.Ink("#EEE1CD"),
        });
        if (choice.Check is AdventureCheckDefinition check)
        {
            copy.Children.Add(new TextBlock
            {
                Text = $"{CheckLabel(check).ToUpperInvariant()} CHECK  ·  DC {check.Difficulty}",
                Foreground = AdventureUi.Ink("#D5BB77"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 7, 0, 0),
            });
        }
        button.Content = copy;
        button.Click += (_, _) =>
        {
            lastResult = null;
            engine.SelectChoice(choice.Id);
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
        body.Children.Add(new TextBlock
        {
            Text = character.Name,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 32,
            Foreground = AdventureUi.Ink("#F4E2C5"),
            Margin = new Thickness(0, 6, 0, 2),
        });
        body.Children.Add(new TextBlock
        {
            Text = $"{character.ClassName} · Level {character.Level}",
            Foreground = AdventureUi.Ink("#90D0B5"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 7),
        });
        body.Children.Add(AdventureUi.OrnamentRule("◇"));

        var vitalGrid = new Grid { Margin = new Thickness(0, 0, 0, 13) };
        for (int i = 0; i < 3; i++) vitalGrid.ColumnDefinitions.Add(new ColumnDefinition());
        Border hp = AdventureUi.StatTile("HP", $"{character.CurrentHitPoints}/{character.MaxHitPoints}");
        Border armor = AdventureUi.StatTile("ARMOR", character.Armor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Border xp = AdventureUi.StatTile("XP", character.Experience.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Grid.SetColumn(hp, 0);
        Grid.SetColumn(armor, 1);
        Grid.SetColumn(xp, 2);
        vitalGrid.Children.Add(hp);
        vitalGrid.Children.Add(armor);
        vitalGrid.Children.Add(xp);
        body.Children.Add(vitalGrid);

        body.Children.Add(AdventureUi.SectionLabel("ABILITIES"));
        var abilities = new UniformGrid { Columns = 3, Margin = new Thickness(0, 2, 0, 13) };
        foreach (AdventureAbility ability in Enum.GetValues<AdventureAbility>())
        {
            bool highlighted = pending?.Ability == ability;
            int score = character.AbilityScore(ability);
            int modifier = character.AbilityModifier(ability);
            Border border = AdventureUi.StatTile(ShortAbility(ability), $"{score}   {Signed(modifier)}", highlighted);
            abilities.Children.Add(border);
        }
        body.Children.Add(abilities);

        body.Children.Add(AdventureUi.SectionLabel("TRAINED SKILLS"));
        foreach (AdventureSkillState skill in character.Skills.Values.OrderBy(item => item.Name))
        {
            bool highlighted = string.Equals(pending?.SkillId, skill.Id, StringComparison.OrdinalIgnoreCase);
            var skillLine = new Grid { Margin = new Thickness(2, 3, 2, 3) };
            skillLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            skillLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            skillLine.Children.Add(new TextBlock
            {
                Text = skill.Name,
                FontFamily = new FontFamily("Georgia"),
                Foreground = AdventureUi.Ink(highlighted ? "#E8CE89" : "#B6C8C0"),
                FontSize = 12,
                FontWeight = highlighted ? FontWeights.Bold : FontWeights.Normal,
            });
            var bonus = new TextBlock
            {
                Text = Signed(character.AbilityModifier(skill.Ability) + skill.ProficiencyBonus),
                FontFamily = new FontFamily("Georgia"),
                Foreground = AdventureUi.Ink(highlighted ? "#E8CE89" : "#8EA59C"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
            };
            Grid.SetColumn(bonus, 1);
            skillLine.Children.Add(bonus);
            body.Children.Add(skillLine);
        }

        body.Children.Add(AdventureUi.SectionLabel("INVENTORY"));
        body.Children.Add(Muted(engine.State.Inventory.Count == 0 ? "Nothing carried." : string.Join(" · ", engine.State.Inventory.Select(Humanize))));
        body.Children.Add(BuildDiceTray(pending));
        sheet.Child = body;
        return sheet;
    }

    private Border BuildDiceTray(AdventureCheckDefinition? pending)
    {
        Border tray = AdventureUi.Panel("#0D1815", "#07100E", pending is null ? "#314A43" : "#8F7847", new Thickness(17), radius: 14, shadow: false);
        tray.Margin = new Thickness(0, 20, 0, 0);
        var body = new StackPanel();
        string? equippedId = diceSetId();

        body.Children.Add(new TextBlock
        {
            Text = pending is null ? "D20 TRAY" : $"{CheckLabel(pending).ToUpperInvariant()} · DC {pending.Difficulty}",
            Foreground = AdventureUi.Ink(pending is null ? "#7F9B91" : "#E8CD87"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = pending is null ? 11 : 12,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        if (pending is not null)
        {
            int ability = engine.State.Character.AbilityModifier(pending.Ability);
            int skill = engine.State.Character.SkillBonus(pending.SkillId);
            int total = ability + skill + pending.Modifier;
            body.Children.Add(new TextBlock
            {
                Text = $"Modifier {Signed(total)}  ·  {ShortAbility(pending.Ability)} {Signed(ability)}" +
                    (pending.SkillId is null ? "" : $"  ·  training {Signed(skill)}"),
                Foreground = AdventureUi.Ink("#B9C6BD"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 7),
            });
        }

        die = new AdventureD20
        {
            Width = 166,
            Height = 166,
            Value = pending is null ? 20 : 0,
            Active = pending is not null,
            VisualVariant = AdventureHubControl.ResolveDiceVariant(equippedId),
        };

        rollSurface = AdventureUi.Panel(
            pending is null ? "#101B18" : "#17221A",
            "#08100E",
            pending is null ? "#2B433D" : "#9A814B",
            new Thickness(8),
            radius: 15,
            shadow: false);
        rollSurface.Width = 212;
        rollSurface.Height = 190;
        rollSurface.HorizontalAlignment = HorizontalAlignment.Center;
        rollSurface.Margin = new Thickness(0, 9, 0, 7);
        rollSurface.Cursor = pending is null ? Cursors.Arrow : Cursors.Hand;
        rollSurface.RenderTransformOrigin = new Point(.5, .5);
        rollSurface.RenderTransform = new ScaleTransform(1, 1);
        rollSurface.Child = die;

        Brush idleBorder = rollSurface.BorderBrush;
        Brush idleBackground = rollSurface.Background;
        if (pending is not null)
        {
            rollSurface.MouseEnter += (_, _) =>
            {
                if (rolling || rollSurface is null || die is null) return;
                rollSurface.BorderBrush = AdventureUi.Ink("#D8BA6D");
                rollSurface.Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#253224"),
                    (Color)ColorConverter.ConvertFromString("#0B1511"),
                    90);
                rollSurface.Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 0,
                    Opacity = .3,
                    Color = (Color)ColorConverter.ConvertFromString("#B39250"),
                };
                if (rollSurface.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1.025;
                    scale.ScaleY = 1.025;
                }
                die.Active = true;
            };
            rollSurface.MouseLeave += (_, _) =>
            {
                if (rollSurface is null || die is null) return;
                rollSurface.BorderBrush = idleBorder;
                rollSurface.Background = idleBackground;
                rollSurface.Effect = null;
                if (rollSurface.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1;
                    scale.ScaleY = 1;
                }
                die.Active = true;
            };
            rollSurface.MouseLeftButtonUp += async (_, _) => await RollAsync(pending);
        }
        body.Children.Add(rollSurface);

        body.Children.Add(new TextBlock
        {
            Text = AdventureHubControl.ResolveDiceName(equippedId),
            Foreground = AdventureUi.Ink("#B49D68"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        body.Children.Add(new TextBlock
        {
            Text = pending is null ? "Waiting for the story" : "CLICK THE D20 TO ROLL",
            Foreground = AdventureUi.Ink(pending is null ? "#718A82" : "#E0C77F"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        });
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
        if (animationsEnabled())
        {
            var rotation = new RotateTransform();
            die.RenderTransform = rotation;
            for (int frame = 0; frame < 11; frame++)
            {
                die.Value = Random.Shared.Next(1, 21);
                rotation.Angle += 39 + frame * 2;
                await Task.Delay(42);
            }
            die.Value = check.RollMode switch
            {
                AdventureRollMode.Advantage => Math.Max(first, second!.Value),
                AdventureRollMode.Disadvantage => Math.Min(first, second!.Value),
                _ => first,
            };
            var settle = new DoubleAnimation(rotation.Angle, rotation.Angle + 18, TimeSpan.FromMilliseconds(130))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };
            rotation.BeginAnimation(RotateTransform.AngleProperty, settle);
            await Task.Delay(145);
        }

        lastResult = engine.ResolvePendingCheck(first, second);
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
        Border border = AdventureUi.Panel(
            success ? "#173024" : "#301D1B",
            success ? "#0D1C16" : "#1A1110",
            success ? "#648A65" : "#7B4A45",
            new Thickness(18),
            radius: 12,
            shadow: false);
        border.Margin = new Thickness(0, 23, 0, 0);
        border.Child = new TextBlock
        {
            Text = $"{headline}\nD20 {result.ChosenRoll}  + modifiers {Signed(result.AbilityModifier + result.SkillBonus + result.AdditionalModifier)}  =  {result.Total}  ·  DC {result.Difficulty}",
            Foreground = AdventureUi.Ink(success ? "#C6DEB9" : "#E0B0A7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            LineHeight = 21,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        return border;
    }

    private static TextBlock Muted(string text) => new()
    {
        Text = text,
        Foreground = AdventureUi.Ink("#8EA49B"),
        FontFamily = new FontFamily("Georgia"),
        FontSize = 11,
        LineHeight = 18,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(2, 1, 0, 6),
    };

    private static string CheckLabel(AdventureCheckDefinition check) => string.IsNullOrWhiteSpace(check.SkillId) ? check.Ability.ToString() : AdventureCharacterCatalog.Skill(check.SkillId).Name;
    private static string ShortAbility(AdventureAbility ability) => ability switch
    {
        AdventureAbility.Strength => "STR",
        AdventureAbility.Dexterity => "DEX",
        AdventureAbility.Constitution => "CON",
        AdventureAbility.Intelligence => "INT",
        AdventureAbility.Wisdom => "WIS",
        AdventureAbility.Charisma => "CHA",
        _ => ability.ToString().ToUpperInvariant(),
    };
    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
