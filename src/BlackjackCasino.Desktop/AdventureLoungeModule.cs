using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        var die = new AdventureD20 { Width = 104, Height = 104, Value = 20, VisualVariant = "dice-obsidian", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        die.RenderTransform = new RotateTransform(-8, 52, 52);
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
    private readonly Action backToGames;

    internal AdventureHubControl(AdventureProgressState progress, Action<AdventureProgressState> persist, Func<string?> diceSetId, Func<string?> playerName, Func<bool> animationsEnabled, Action backToGames)
    {
        this.progress = AdventureProgressState.Normalize(progress);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.diceSetId = diceSetId ?? throw new ArgumentNullException(nameof(diceSetId));
        this.playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        this.animationsEnabled = animationsEnabled ?? throw new ArgumentNullException(nameof(animationsEnabled));
        this.backToGames = backToGames ?? throw new ArgumentNullException(nameof(backToGames));
        ShowHub();
    }

    private void ShowHub()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var root = new StackPanel { Margin = new Thickness(42, 30, 42, 38), MaxWidth = 1120, HorizontalAlignment = HorizontalAlignment.Center };
        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel();
        copy.Children.Add(LoungeUi.Eyebrow("TALES FROM THE BACKROOM"));
        copy.Children.Add(new TextBlock { Text = "Your story. Your roll.", FontFamily = new FontFamily("Georgia"), FontSize = 38, Foreground = LoungeUi.Ink("#F1E6D5"), Margin = new Thickness(0, 5, 0, 7) });
        copy.Children.Add(new TextBlock { Text = "A reusable tabletop adventure engine inside the Chill Lounge. Choices can change the world; checks use your character sheet and the Dice Set equipped in your profile.", FontSize = 13, Foreground = LoungeUi.Ink("#9FB5AE"), TextWrapping = TextWrapping.Wrap, MaxWidth = 760 });
        heading.Children.Add(copy);
        Button games = LoungeUi.Button("← LOUNGE GAMES");
        games.VerticalAlignment = VerticalAlignment.Top;
        games.Click += (_, _) => backToGames();
        Grid.SetColumn(games, 1);
        heading.Children.Add(games);
        root.Children.Add(heading);

        AdventureDefinition adventure = AdventureCatalog.BlackStag;
        var adventureCard = LoungeUi.Card();
        adventureCard.Margin = new Thickness(0, 24, 0, 18);
        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        var cardCopy = new StackPanel { Margin = new Thickness(0, 0, 26, 0) };
        cardCopy.Children.Add(LoungeUi.Eyebrow("ADVENTURE 01"));
        cardCopy.Children.Add(new TextBlock { Text = adventure.Title, FontFamily = new FontFamily("Georgia"), FontSize = 29, Foreground = LoungeUi.Ink("#F1E6D5"), Margin = new Thickness(0, 5, 0, 6) });
        cardCopy.Children.Add(new TextBlock { Text = adventure.Subtitle, Foreground = LoungeUi.Ink("#A9BBB5"), FontSize = 12, TextWrapping = TextWrapping.Wrap });
        progress.Runs.TryGetValue(adventure.Id, out AdventureRunState? existing);
        if (existing is not null)
            cardCopy.Children.Add(new TextBlock { Text = existing.Completed ? $"COMPLETED · {existing.EndingId ?? "ending discovered"} · {existing.RollsMade:N0} rolls" : $"IN PROGRESS · {existing.Character.ClassName} · {existing.RollsMade:N0} rolls", Foreground = LoungeUi.Ink(existing.Completed ? "#E7CCA0" : "#8EC9B0"), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 0) });
        cardGrid.Children.Add(cardCopy);
        var art = new Grid();
        art.Children.Add(new AdventureD20 { Width = 128, Height = 128, Value = 20, VisualVariant = ResolveDiceVariant(diceSetId()), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(art, 1);
        cardGrid.Children.Add(art);
        adventureCard.Child = cardGrid;
        root.Children.Add(adventureCard);

        if (existing is not null && !existing.Completed)
        {
            Button resume = LoungeUi.Button("RESUME THE BLACK STAG");
            resume.HorizontalAlignment = HorizontalAlignment.Left;
            resume.Margin = new Thickness(0, 0, 0, 22);
            resume.Click += (_, _) => ShowRun(existing);
            root.Children.Add(resume);
        }

        root.Children.Add(new TextBlock { Text = existing is null ? "CHOOSE YOUR CHARACTER" : "START A NEW RUN", Foreground = LoungeUi.Ink("#7FB49F"), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(2, 4, 0, 10) });
        var classes = new ComboBox { ItemsSource = AdventureCharacterCatalog.All, DisplayMemberPath = nameof(AdventureClassDefinition.Name), SelectedValuePath = nameof(AdventureClassDefinition.Id), SelectedValue = "rogue", Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 7, 10, 7), Background = LoungeUi.Ink("#10211E"), Foreground = LoungeUi.Ink("#F1E6D5"), BorderBrush = LoungeUi.Ink("#42645E") };
        root.Children.Add(classes);
        var classDescription = new TextBlock { Text = AdventureCharacterCatalog.All.First(item => item.Id == "rogue").Description, Foreground = LoungeUi.Ink("#9FB5AE"), FontSize = 11, TextWrapping = TextWrapping.Wrap, MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2, 8, 0, 12) };
        root.Children.Add(classDescription);
        classes.SelectionChanged += (_, _) => { string selected = classes.SelectedValue as string ?? "rogue"; classDescription.Text = AdventureCharacterCatalog.All.First(item => item.Id == selected).Description; };
        Button start = LoungeUi.Button(existing is null ? "BEGIN ADVENTURE" : "BEGIN NEW RUN");
        start.HorizontalAlignment = HorizontalAlignment.Left;
        start.Click += (_, _) => { string classId = classes.SelectedValue as string ?? "rogue"; AdventureCharacterState character = AdventureCharacterCatalog.Create(classId, playerName()); AdventureEngine engine = AdventureEngine.StartNew(adventure, character); progress.Runs[adventure.Id] = engine.State; progress.LastAdventureId = adventure.Id; persist(progress); ShowRun(engine.State); };
        root.Children.Add(start);
        root.Children.Add(new TextBlock { Text = "The first adventure is a vertical slice for the engine: persistent character state, conditional choices, inventory/world flags, fail-forward checks, critical rolls and multiple endings are data-driven.", Foreground = LoungeUi.Ink("#718E86"), FontSize = 10, TextWrapping = TextWrapping.Wrap, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2, 18, 0, 0) });
        scroll.Content = root;
        Content = scroll;
    }

    private void ShowRun(AdventureRunState run)
    {
        AdventureDefinition adventure = AdventureCatalog.BlackStag;
        var engine = new AdventureEngine(adventure, AdventureRunState.Normalize(run));
        Content = new AdventureView(engine, state => { progress.Runs[adventure.Id] = state; progress.LastAdventureId = adventure.Id; persist(progress); }, ShowHub, diceSetId, animationsEnabled);
    }

    internal static string ResolveDiceVariant(string? id) => CosmeticCatalog.All.FirstOrDefault(item => item.Kind == CosmeticKind.DiceSet && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.VisualVariant ?? "dice-ivory";
    internal static string ResolveDiceName(string? id) => CosmeticCatalog.All.FirstOrDefault(item => item.Kind == CosmeticKind.DiceSet && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))?.Name ?? "House Ivory";
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
    private Button? rollButton;
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
        var root = new Grid { Margin = new Thickness(26, 22, 26, 28) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 520 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        Border story = Panel("#0D1919", "#284641", new Thickness(24));
        story.Margin = new Thickness(0, 0, 18, 0);
        var storyGrid = new Grid();
        storyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        storyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var storyHeader = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        storyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        storyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleStack = new StackPanel();
        titleStack.Children.Add(LoungeUi.Eyebrow(engine.Definition.Title.ToUpperInvariant()));
        titleStack.Children.Add(new TextBlock { Text = scene.Title, FontFamily = new FontFamily("Georgia"), FontSize = 31, Foreground = LoungeUi.Ink("#F2E5D2"), Margin = new Thickness(0, 5, 0, 0) });
        storyHeader.Children.Add(titleStack);
        Button back = LoungeUi.SmallButton("← ADVENTURES");
        back.IsEnabled = !rolling;
        back.Click += (_, _) => { if (rolling) return; persist(engine.State); backToHub(); };
        Grid.SetColumn(back, 1);
        storyHeader.Children.Add(back);
        storyGrid.Children.Add(storyHeader);
        var storyScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var storyBody = new StackPanel { Margin = new Thickness(0, 2, 10, 0) };
        if (!string.IsNullOrWhiteSpace(scene.Speaker)) storyBody.Children.Add(new TextBlock { Text = scene.Speaker.ToUpperInvariant(), Foreground = LoungeUi.Ink("#8EC9B0"), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 7) });
        storyBody.Children.Add(new TextBlock { Text = scene.Body, FontFamily = new FontFamily("Georgia"), FontSize = 17, LineHeight = 28, Foreground = LoungeUi.Ink("#DCCFBC"), TextWrapping = TextWrapping.Wrap });
        if (lastResult is not null) storyBody.Children.Add(BuildResult(lastResult));

        if (engine.State.Completed)
        {
            var ending = Panel("#14231F", "#7A6843", new Thickness(16));
            ending.Margin = new Thickness(0, 24, 0, 0);
            ending.Child = new TextBlock { Text = $"ADVENTURE COMPLETE  ·  {engine.State.EndingId?.Replace('-', ' ').ToUpperInvariant()}\n{engine.State.RollsMade} rolls · {engine.State.NaturalTwenties} natural 20s · {engine.State.NaturalOnes} natural 1s · {engine.State.Character.Experience} character XP", Foreground = LoungeUi.Ink("#E7CCA0"), FontSize = 11, TextWrapping = TextWrapping.Wrap };
            storyBody.Children.Add(ending);
            Button returnButton = LoungeUi.Button("RETURN TO ADVENTURES");
            returnButton.HorizontalAlignment = HorizontalAlignment.Left;
            returnButton.Margin = new Thickness(0, 18, 0, 0);
            returnButton.Click += (_, _) => { persist(engine.State); backToHub(); };
            storyBody.Children.Add(returnButton);
        }
        else if (pending is not null)
        {
            var waiting = Panel("#13221F", "#6D5C38", new Thickness(16));
            waiting.Margin = new Thickness(0, 24, 0, 0);
            waiting.Child = new TextBlock { Text = "THE STORY IS WAITING FOR YOUR ROLL\nThe relevant stat and skill are highlighted on the character sheet. Click your D20 to resolve the check.", Foreground = LoungeUi.Ink("#D8C79E"), FontSize = 11, TextWrapping = TextWrapping.Wrap };
            storyBody.Children.Add(waiting);
        }
        else
        {
            storyBody.Children.Add(new TextBlock { Text = "WHAT DO YOU DO?", Foreground = LoungeUi.Ink("#7FB49F"), FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 25, 0, 10) });
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
        var button = new Button { HorizontalContentAlignment = HorizontalAlignment.Left, Background = LoungeUi.Ink("#10231F"), Foreground = LoungeUi.Ink("#E8DFD0"), BorderBrush = LoungeUi.Ink(choice.Check is null ? "#355A50" : "#7A6843"), BorderThickness = new Thickness(1), Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 0, 0, 8), Cursor = System.Windows.Input.Cursors.Hand };
        var copy = new StackPanel();
        copy.Children.Add(new TextBlock { Text = choice.Text, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        if (choice.Check is AdventureCheckDefinition check) copy.Children.Add(new TextBlock { Text = $"{CheckLabel(check).ToUpperInvariant()} CHECK  ·  DC {check.Difficulty}", Foreground = LoungeUi.Ink("#CDB77E"), FontSize = 9, Margin = new Thickness(0, 5, 0, 0) });
        button.Content = copy;
        button.Click += (_, _) => { lastResult = null; engine.SelectChoice(choice.Id); persist(engine.State); Render(); };
        return button;
    }

    private Border BuildCharacterSheet(AdventureCheckDefinition? pending)
    {
        Border sheet = Panel("#101D1B", "#385650", new Thickness(20));
        var body = new StackPanel();
        AdventureCharacterState character = engine.State.Character;
        body.Children.Add(LoungeUi.Eyebrow("CHARACTER SHEET"));
        body.Children.Add(new TextBlock { Text = character.Name, FontFamily = new FontFamily("Georgia"), FontSize = 27, Foreground = LoungeUi.Ink("#F2E5D2"), Margin = new Thickness(0, 5, 0, 1) });
        body.Children.Add(new TextBlock { Text = $"{character.ClassName} · Level {character.Level}", Foreground = LoungeUi.Ink("#8EC9B0"), FontSize = 11, Margin = new Thickness(0, 0, 0, 14) });
        var vitalGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        for (int i = 0; i < 3; i++) vitalGrid.ColumnDefinitions.Add(new ColumnDefinition());
        vitalGrid.Children.Add(Metric("HP", $"{character.CurrentHitPoints}/{character.MaxHitPoints}", 0));
        vitalGrid.Children.Add(Metric("ARMOR", character.Armor.ToString(System.Globalization.CultureInfo.InvariantCulture), 1));
        vitalGrid.Children.Add(Metric("XP", character.Experience.ToString(System.Globalization.CultureInfo.InvariantCulture), 2));
        body.Children.Add(vitalGrid);
        body.Children.Add(SectionLabel("ABILITIES"));
        var abilities = new UniformGrid { Columns = 3, Margin = new Thickness(0, 4, 0, 13) };
        foreach (AdventureAbility ability in Enum.GetValues<AdventureAbility>())
        {
            bool highlighted = pending?.Ability == ability;
            int score = character.AbilityScore(ability);
            int modifier = character.AbilityModifier(ability);
            var border = Panel(highlighted ? "#253025" : "#0C1716", highlighted ? "#C7A961" : "#29433E", new Thickness(7));
            border.Margin = new Thickness(0, 0, 6, 6);
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = ShortAbility(ability), Foreground = LoungeUi.Ink(highlighted ? "#E7CCA0" : "#7F9D94"), FontSize = 9, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = $"{score}  {Signed(modifier)}", Foreground = LoungeUi.Ink("#E5D9C8"), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center });
            border.Child = stack;
            abilities.Children.Add(border);
        }
        body.Children.Add(abilities);
        body.Children.Add(SectionLabel("TRAINED SKILLS"));
        foreach (AdventureSkillState skill in character.Skills.Values.OrderBy(item => item.Name))
        {
            bool highlighted = string.Equals(pending?.SkillId, skill.Id, StringComparison.OrdinalIgnoreCase);
            body.Children.Add(new TextBlock { Text = $"{skill.Name,-18} {Signed(character.AbilityModifier(skill.Ability) + skill.ProficiencyBonus)}", FontFamily = new FontFamily("Consolas"), Foreground = LoungeUi.Ink(highlighted ? "#E7CCA0" : "#AFC1BA"), FontSize = 10, FontWeight = highlighted ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(2, 2, 0, 2) });
        }
        body.Children.Add(SectionLabel("INVENTORY"));
        body.Children.Add(Muted(engine.State.Inventory.Count == 0 ? "Nothing carried." : string.Join(" · ", engine.State.Inventory.Select(Humanize))));
        body.Children.Add(BuildDiceTray(pending));
        sheet.Child = body;
        return sheet;
    }

    private Border BuildDiceTray(AdventureCheckDefinition? pending)
    {
        var tray = Panel("#0A1413", pending is null ? "#2A423D" : "#7A6843", new Thickness(14));
        tray.Margin = new Thickness(0, 18, 0, 0);
        var body = new StackPanel();
        string? equippedId = diceSetId();
        body.Children.Add(new TextBlock { Text = pending is null ? "D20 TRAY" : $"{CheckLabel(pending).ToUpperInvariant()} · DC {pending.Difficulty}", Foreground = LoungeUi.Ink(pending is null ? "#718E86" : "#E7CCA0"), FontSize = 9, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        if (pending is not null)
        {
            int ability = engine.State.Character.AbilityModifier(pending.Ability);
            int skill = engine.State.Character.SkillBonus(pending.SkillId);
            int total = ability + skill + pending.Modifier;
            body.Children.Add(new TextBlock { Text = $"Modifier {Signed(total)}  ·  {ShortAbility(pending.Ability)} {Signed(ability)}" + (pending.SkillId is null ? "" : $"  ·  training {Signed(skill)}"), Foreground = LoungeUi.Ink("#AFC1BA"), FontSize = 9, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 3) });
        }
        die = new AdventureD20 { Width = 132, Height = 132, Value = pending is null ? 20 : 0, Active = pending is not null, VisualVariant = AdventureHubControl.ResolveDiceVariant(equippedId), RenderTransformOrigin = new Point(.5, .5) };
        rollButton = new Button { Content = die, Width = 150, Height = 150, Margin = new Thickness(0, 7, 0, 4), HorizontalAlignment = HorizontalAlignment.Center, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = pending is null ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand, IsEnabled = pending is not null && !rolling, ToolTip = pending is null ? "The die activates when the story requires a check." : "Roll the check." };
        if (pending is not null) rollButton.Click += async (_, _) => await RollAsync(pending);
        body.Children.Add(rollButton);
        body.Children.Add(new TextBlock { Text = AdventureHubControl.ResolveDiceName(equippedId), Foreground = LoungeUi.Ink("#A79267"), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center });
        body.Children.Add(new TextBlock { Text = pending is null ? "Waiting for a check" : "CLICK THE DIE TO ROLL", Foreground = LoungeUi.Ink(pending is null ? "#617A73" : "#D8C79E"), FontSize = 9, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
        tray.Child = body;
        return tray;
    }

    private async Task RollAsync(AdventureCheckDefinition check)
    {
        if (rolling || rollButton is null || die is null) return;
        rolling = true;
        rollButton.IsEnabled = false;
        int first = Random.Shared.Next(1, 21);
        int? second = check.RollMode == AdventureRollMode.Normal ? null : Random.Shared.Next(1, 21);
        if (animationsEnabled())
        {
            var rotation = new RotateTransform();
            die.RenderTransform = rotation;
            for (int frame = 0; frame < 11; frame++) { die.Value = Random.Shared.Next(1, 21); rotation.Angle += 39 + frame * 2; await Task.Delay(42); }
            die.Value = check.RollMode switch { AdventureRollMode.Advantage => Math.Max(first, second!.Value), AdventureRollMode.Disadvantage => Math.Min(first, second!.Value), _ => first };
            var settle = new DoubleAnimation(rotation.Angle, rotation.Angle + 18, TimeSpan.FromMilliseconds(130)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
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
        string headline = result.Outcome switch { AdventureCheckOutcome.CriticalSuccess => "NATURAL 20 · CRITICAL SUCCESS", AdventureCheckOutcome.CriticalFailure => "NATURAL 1 · CRITICAL FAILURE", AdventureCheckOutcome.Success => "SUCCESS", _ => "FAILURE" };
        var border = Panel(success ? "#13251F" : "#281A1A", success ? "#577A5C" : "#704443", new Thickness(14));
        border.Margin = new Thickness(0, 20, 0, 0);
        border.Child = new TextBlock { Text = $"{headline}\nD20 {result.ChosenRoll}  + modifiers {Signed(result.AbilityModifier + result.SkillBonus + result.AdditionalModifier)}  =  {result.Total}  ·  DC {result.Difficulty}", Foreground = LoungeUi.Ink(success ? "#BFD8B8" : "#D8AAA4"), FontSize = 10, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        return border;
    }

    private static FrameworkElement Metric(string label, string value, int column)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = label, FontSize = 8, Foreground = LoungeUi.Ink("#718E86"), HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = value, FontSize = 13, Foreground = LoungeUi.Ink("#E5D9C8"), FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        Grid.SetColumn(stack, column);
        return stack;
    }

    private static TextBlock SectionLabel(string text) => new() { Text = text, Foreground = LoungeUi.Ink("#7FB49F"), FontSize = 9, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 4) };
    private static TextBlock Muted(string text) => new() { Text = text, Foreground = LoungeUi.Ink("#839A93"), FontSize = 9, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 1, 0, 6) };
    private static Border Panel(string background, string border, Thickness padding) => new() { Background = LoungeUi.Ink(background), BorderBrush = LoungeUi.Ink(border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = padding };
    private static string CheckLabel(AdventureCheckDefinition check) => string.IsNullOrWhiteSpace(check.SkillId) ? check.Ability.ToString() : AdventureCharacterCatalog.Skill(check.SkillId).Name;
    private static string ShortAbility(AdventureAbility ability) => ability switch { AdventureAbility.Strength => "STR", AdventureAbility.Dexterity => "DEX", AdventureAbility.Constitution => "CON", AdventureAbility.Intelligence => "INT", AdventureAbility.Wisdom => "WIS", AdventureAbility.Charisma => "CHA", _ => ability.ToString().ToUpperInvariant() };
    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
