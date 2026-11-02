using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

// Legacy entry point kept as a compatibility shim. The actual Backroom hub is catalog-driven.
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
        Action<bool> setMusicActive) =>
        AdventureLibraryLoungeModule.Create(
            progress,
            persist,
            diceSetId,
            playerName,
            animationsEnabled,
            presentation,
            updatePresentation,
            setMusicActive);
}

// Kept under the historic type name so existing presentation code can continue resolving dice cosmetics.
internal static class AdventureHubControl
{
    internal static string ResolveDiceVariant(string? id) =>
        CosmeticCatalog.All.FirstOrDefault(item =>
            item.Kind == CosmeticKind.DiceSet &&
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.VisualVariant ?? "dice-ivory";

    internal static string ResolveDiceName(string? id) =>
        CosmeticCatalog.All.FirstOrDefault(item =>
            item.Kind == CosmeticKind.DiceSet &&
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.Name ?? "House Ivory";
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
    private FrameworkElement? storyTransitionSurface;
    private bool rolling;
    private bool transitioning;

    internal AdventureView(
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
        feedback = engine.DrainFeedback();
        Render();
    }

    private void Render(bool animateStoryIn = false)
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
        Grid.SetRowSpan(sheet, 2);
        root.Children.Add(sheet);

        Border hud = AdventureInventoryUi.Build(engine);
        Grid.SetRow(hud, 1);
        Grid.SetColumn(hud, 0);
        root.Children.Add(hud);

        Content = root;
        if (animateStoryIn) BeginStoryFadeIn();
    }

    private Border BuildStory(AdventureSceneDefinition scene, AdventureCheckDefinition? pending)
    {
        Border story = AdventureUi.Panel("#101B18", "#091311", "#345047", new Thickness(32), radius: 17);
        var grid = new Grid();
        storyTransitionSurface = grid;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        string act = scene.ActId is not null && engine.Definition.Acts?.TryGetValue(scene.ActId, out string? actName) == true
            ? actName
            : engine.Definition.Title;
        title.Children.Add(AdventureUi.Eyebrow(act.ToUpperInvariant(), 11));
        title.Children.Add(new TextBlock
        {
            Text = scene.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 40,
            Foreground = AdventureUi.Ink("#F4E3C8"),
            Margin = new Thickness(0, 7, 0, 0),
        });
        if (!string.IsNullOrWhiteSpace(scene.Location))
        {
            title.Children.Add(new TextBlock
            {
                Text = scene.Location.ToUpperInvariant(),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10,
                Foreground = AdventureUi.Ink("#708E83"),
                Margin = new Thickness(1, 5, 0, 0),
            });
        }
        header.Children.Add(title);

        Button back = AdventureUi.Button("← ADVENTURES", fontSize: 11.5, scaleOnHover: false);
        back.VerticalAlignment = VerticalAlignment.Top;
        back.IsEnabled = !rolling;
        back.Click += (_, _) =>
        {
            if (rolling || transitioning) return;
            persist(engine.State);
            backToHub();
        };
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        grid.Children.Add(header);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var body = new StackPanel { Margin = new Thickness(0, 2, 12, 0) };
        body.Children.Add(AdventureUi.OrnamentRule());
        if (!string.IsNullOrWhiteSpace(scene.Speaker))
        {
            body.Children.Add(new TextBlock
            {
                Text = scene.Speaker.ToUpperInvariant(),
                Foreground = AdventureUi.Ink("#8FD0B5"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }
        body.Children.Add(new TextBlock
        {
            Text = scene.Body,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 20,
            LineHeight = 32,
            Foreground = AdventureUi.Ink("#E1D3BF"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 1080,
        });
        if (lastResult is not null) body.Children.Add(BuildResult(lastResult));

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
            body.Children.Add(ending);

            Button returnButton = AdventureUi.Button("RETURN TO ADVENTURES", accent: true, fontSize: 13, scaleOnHover: false);
            returnButton.HorizontalAlignment = HorizontalAlignment.Left;
            returnButton.Margin = new Thickness(0, 20, 0, 0);
            returnButton.Click += (_, _) =>
            {
                if (transitioning) return;
                persist(engine.State);
                backToHub();
            };
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
                Foreground = AdventureUi.Ink("#DCC793"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 13,
                LineHeight = 21,
                TextWrapping = TextWrapping.Wrap,
            };
            body.Children.Add(waiting);
        }
        else
        {
            body.Children.Add(new TextBlock
            {
                Text = "WHAT DO YOU DO?",
                Foreground = AdventureUi.Ink("#8FD0B5"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 30, 0, 12),
            });
            foreach (AdventureChoiceDefinition choice in engine.AvailableChoices())
                body.Children.Add(BuildChoice(choice));
        }

        scroll.Content = body;
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        story.Child = grid;
        return story;
    }

    private FrameworkElement BuildChoice(AdventureChoiceDefinition choice)
    {
        bool launchesEncounter = AdventureEncounterCatalog.TryGetTriggeredByChoice(
            engine.Definition.Id,
            choice,
            out AdventureEncounterDefinition? encounter);
        Button button = AdventureUi.Button("", accent: choice.Check is not null && !launchesEncounter, fontSize: 14.5, scaleOnHover: false);
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Padding = new Thickness(18, 14, 18, 14);
        button.Margin = new Thickness(0, 0, 0, 10);
        button.MinHeight = 58;
        if (encounter is not null) StyleEncounterChoice(button);

        var content = new Grid();
        if (encounter is not null)
        {
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            FrameworkElement icon = BuildEncounterIcon(encounter.Type);
            icon.Margin = new Thickness(0, 1, 14, 0);
            content.Children.Add(icon);
        }

        var copy = new StackPanel();
        copy.Children.Add(new TextBlock
        {
            Text = choice.Text,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            LineHeight = 22,
            Foreground = AdventureUi.Ink(encounter is null ? "#EEE1CD" : "#E7F5EB"),
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
        if (encounter is not null)
        {
            copy.Children.Add(new TextBlock
            {
                Text = encounter.Type == AdventureEncounterType.Blackjack
                    ? "BLACKJACK ENCOUNTER"
                    : "DICE ENCOUNTER · FIVE OF A KIND",
                Foreground = AdventureUi.Ink("#72D39B"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 7, 0, 0),
            });
            Grid.SetColumn(copy, 1);
        }
        content.Children.Add(copy);
        button.Content = content;
        button.Click += async (_, _) => await AdvanceChoiceAsync(choice.Id);
        return button;
    }

    private static void StyleEncounterChoice(Button button)
    {
        Brush normal = AdventureUi.Ink("#123025");
        Brush hover = AdventureUi.Ink("#194632");
        Brush border = AdventureUi.Ink("#4FAE78");
        Brush hoverBorder = AdventureUi.Ink("#78D79F");
        button.Background = normal;
        button.BorderBrush = border;
        button.Foreground = AdventureUi.Ink("#E5F7EA");
        button.BorderThickness = new Thickness(1.25);

        button.MouseEnter += (_, _) =>
        {
            button.Background = hover;
            button.BorderBrush = hoverBorder;
            button.Effect = new DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = .28,
                Color = (Color)ColorConverter.ConvertFromString("#4FAE78"),
            };
        };
        button.MouseLeave += (_, _) =>
        {
            button.Background = normal;
            button.BorderBrush = border;
            button.Effect = null;
        };
    }

    private static FrameworkElement BuildEncounterIcon(AdventureEncounterType type)
    {
        Brush accent = AdventureUi.Ink("#78D79F");
        Brush surface = AdventureUi.Ink("#0C2119");
        if (type == AdventureEncounterType.Blackjack)
        {
            var host = new Grid { Width = 34, Height = 30 };
            var rear = new Border
            {
                Width = 17,
                Height = 23,
                CornerRadius = new CornerRadius(2.5),
                BorderBrush = accent,
                BorderThickness = new Thickness(1.2),
                Background = surface,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform(-10),
                Margin = new Thickness(2, 0, 0, 0),
            };
            var front = new Border
            {
                Width = 17,
                Height = 23,
                CornerRadius = new CornerRadius(2.5),
                BorderBrush = accent,
                BorderThickness = new Thickness(1.2),
                Background = AdventureUi.Ink("#123326"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform(8),
                Margin = new Thickness(0, 0, 2, 0),
                Child = new TextBlock
                {
                    Text = "♠",
                    FontFamily = new FontFamily("Georgia"),
                    FontSize = 11,
                    Foreground = accent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            host.Children.Add(rear);
            host.Children.Add(front);
            return host;
        }

        var pipGrid = new Grid { Margin = new Thickness(4) };
        for (int i = 0; i < 3; i++)
        {
            pipGrid.RowDefinitions.Add(new RowDefinition());
            pipGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }
        foreach ((int row, int column) in new[] { (0, 0), (0, 2), (1, 1), (2, 0), (2, 2) })
        {
            var pip = new System.Windows.Shapes.Ellipse
            {
                Width = 3.4,
                Height = 3.4,
                Fill = accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(pip, row);
            Grid.SetColumn(pip, column);
            pipGrid.Children.Add(pip);
        }
        return new Border
        {
            Width = 27,
            Height = 27,
            CornerRadius = new CornerRadius(5),
            BorderBrush = accent,
            BorderThickness = new Thickness(1.3),
            Background = surface,
            Child = pipGrid,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    private Border BuildCharacterSheet(AdventureCheckDefinition? pending)
    {
        Border sheet = AdventureUi.Panel("#13201D", "#091310", "#3C5A51", new Thickness(24), radius: 17);
        var body = new StackPanel();
        AdventureCharacterState character = engine.State.Character;

        body.Children.Add(AdventureUi.Eyebrow("CHARACTER SHEET", 11));

        var identityRow = new Grid { Margin = new Thickness(0, 5, 0, 6) };
        identityRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        identityRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(214) });

        var identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        identity.Children.Add(new TextBlock
        {
            Text = character.Name,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 30,
            Foreground = AdventureUi.Ink("#F4E2C5"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        identity.Children.Add(new TextBlock
        {
            Text = $"{character.ClassName} · Level {character.Level}",
            Foreground = AdventureUi.Ink("#90D0B5"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12.5,
            Margin = new Thickness(0, 1, 0, 0),
        });
        identityRow.Children.Add(identity);

        Border eventFeed = AdventureInventoryUi.BuildEventFeed(feedback, animationsEnabled());
        Grid.SetColumn(eventFeed, 1);
        identityRow.Children.Add(eventFeed);
        body.Children.Add(identityRow);
        body.Children.Add(AdventureUi.OrnamentRule("◇"));

        var vitals = new Grid { Margin = new Thickness(0, 0, 0, 13) };
        for (int i = 0; i < 3; i++) vitals.ColumnDefinitions.Add(new ColumnDefinition());
        Border hp = AdventureUi.StatTile("HP", $"{character.CurrentHitPoints}/{character.MaxHitPoints}");
        Border armor = AdventureUi.StatTile("ARMOR", character.Armor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Border xp = AdventureUi.StatTile("XP", character.Experience.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Grid.SetColumn(hp, 0);
        Grid.SetColumn(armor, 1);
        Grid.SetColumn(xp, 2);
        vitals.Children.Add(hp);
        vitals.Children.Add(armor);
        vitals.Children.Add(xp);
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
            line.Children.Add(new TextBlock
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
            line.Children.Add(bonus);
            body.Children.Add(line);
        }

        if (engine.State.ActiveConditions.Count > 0)
        {
            body.Children.Add(AdventureUi.SectionLabel("ACTIVE CONDITIONS"));
            body.Children.Add(new TextBlock
            {
                Text = string.Join(" · ", engine.State.ActiveConditions.Values.Select(state =>
                    engine.ConditionDefinition(state.ConditionId)?.Name ?? Humanize(state.ConditionId))),
                Foreground = AdventureUi.Ink("#C9A978"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 1, 0, 2),
            });
        }

        body.Children.Add(BuildDiceTray(pending));
        sheet.Child = body;
        return sheet;
    }

    private Border BuildDiceTray(AdventureCheckDefinition? pending)
    {
        Border tray = AdventureUi.Panel(
            "#0D1815",
            "#07100E",
            pending is null ? "#314A43" : "#8F7847",
            new Thickness(16),
            radius: 14,
            shadow: false);
        tray.Margin = new Thickness(0, 10, 0, 0);

        var body = new StackPanel();
        string? equipped = diceSetId();
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
            body.Children.Add(new TextBlock
            {
                Text = $"Modifier {Signed(ability + skill + pending.Modifier)} · {ShortAbility(pending.Ability)} {Signed(ability)}" +
                       (pending.SkillId is null ? "" : $" · training {Signed(skill)}"),
                Foreground = AdventureUi.Ink("#B9C6BD"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 6),
            });
        }

        die = new AdventureD20
        {
            Width = 166,
            Height = 166,
            Value = pending is null ? lastResult?.ChosenRoll ?? 20 : 0,
            Active = pending is not null,
            VisualVariant = AdventureHubControl.ResolveDiceVariant(equipped),
        };
        rollSurface = AdventureUi.Panel(
            pending is null ? "#101B18" : "#17221A",
            "#08100E",
            pending is null ? "#2B433D" : "#9A814B",
            new Thickness(8),
            radius: 15,
            shadow: false);
        rollSurface.Width = 212;
        rollSurface.Height = 188;
        rollSurface.HorizontalAlignment = HorizontalAlignment.Center;
        rollSurface.Margin = new Thickness(0, 6, 0, 4);
        rollSurface.Cursor = pending is null ? CasinoCursors.Arrow : CasinoCursors.Hand;
        rollSurface.Child = die;

        if (pending is not null)
        {
            Brush border = rollSurface.BorderBrush;
            rollSurface.MouseEnter += (_, _) =>
            {
                if (!rolling && !transitioning && rollSurface is not null)
                {
                    rollSurface.BorderBrush = AdventureUi.Ink("#D8BA6D");
                    rollSurface.Effect = new DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Opacity = .25,
                        Color = (Color)ColorConverter.ConvertFromString("#B39250"),
                    };
                }
            };
            rollSurface.MouseLeave += (_, _) =>
            {
                if (rollSurface is not null)
                {
                    rollSurface.BorderBrush = border;
                    rollSurface.Effect = null;
                }
            };
            rollSurface.MouseLeftButtonUp += async (_, _) => await RollAsync(pending);
        }

        body.Children.Add(rollSurface);
        body.Children.Add(new TextBlock
        {
            Text = AdventureHubControl.ResolveDiceName(equipped),
            Foreground = AdventureUi.Ink("#B49D68"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        body.Children.Add(new TextBlock
        {
            Text = pending is not null
                ? "CLICK THE D20 TO ROLL"
                : lastResult is not null
                    ? $"LAST ROLL · {lastResult.ChosenRoll}"
                    : "Waiting for the story",
            Foreground = AdventureUi.Ink(pending is not null ? "#E0C77F" : lastResult is not null ? "#CBB579" : "#718A82"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        });
        tray.Child = body;
        return tray;
    }

    private async Task AdvanceChoiceAsync(string choiceId)
    {
        if (rolling || transitioning) return;
        transitioning = true;
        try
        {
            await FadeStoryOutAsync();
            lastResult = null;
            engine.SelectChoice(choiceId);
            feedback = engine.DrainFeedback();
            persist(engine.State);
            Render(animateStoryIn: animationsEnabled());
        }
        finally
        {
            transitioning = false;
        }
    }

    private async Task RollAsync(AdventureCheckDefinition check)
    {
        if (rolling || transitioning || rollSurface is null || die is null) return;
        rolling = true;
        rollSurface.IsHitTestVisible = false;
        try
        {
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
                foreach (int pause in pauses)
                {
                    die.Value = Random.Shared.Next(1, 21);
                    await Task.Delay(pause);
                }
                die.Value = chosen;
                await Task.Delay(230);
                await FadeStoryOutAsync();
            }
            else
            {
                die.Value = chosen;
            }

            lastResult = engine.ResolvePendingCheck(first, second);
            feedback = engine.DrainFeedback();
            persist(engine.State);
            rolling = false;
            Render(animateStoryIn: animationsEnabled());
        }
        finally
        {
            rolling = false;
        }
    }

    private async Task FadeStoryOutAsync()
    {
        if (!animationsEnabled() || storyTransitionSurface is null) return;

        storyTransitionSurface.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(storyTransitionSurface.Opacity, 0, TimeSpan.FromMilliseconds(145))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
            });
        await Task.Delay(155);
    }

    private void BeginStoryFadeIn()
    {
        if (!animationsEnabled() || storyTransitionSurface is null) return;

        storyTransitionSurface.Opacity = 0;
        var translate = new TranslateTransform(0, 5);
        storyTransitionSurface.RenderTransform = translate;
        storyTransitionSurface.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(235))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        translate.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(5, 0, TimeSpan.FromMilliseconds(255))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
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
            Text = $"{headline}\nD20 {result.ChosenRoll} + modifiers {Signed(result.AbilityModifier + result.SkillBonus + result.AdditionalModifier)} = {result.Total} · DC {result.Difficulty}",
            Foreground = AdventureUi.Ink(success ? "#C6DEB9" : "#E0B0A7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            LineHeight = 21,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        return border;
    }

    private static string CheckLabel(AdventureCheckDefinition check) =>
        string.IsNullOrWhiteSpace(check.SkillId)
            ? check.Ability.ToString()
            : AdventureCharacterCatalog.Skill(check.SkillId).Name;

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

    private static string Signed(int value) =>
        value >= 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string Humanize(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
