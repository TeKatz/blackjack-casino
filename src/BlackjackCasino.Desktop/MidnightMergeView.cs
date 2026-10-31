using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

internal sealed class MidnightMergeView : UserControl, IDisposable
{
    private const double Pitch = 70;
    private const double CellSize = 64;
    private const double BoardWidth = 526;
    private const double BoardHeight = 456;

    private readonly MergeBoard board;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private readonly Action back;
    private readonly Action<UiLanguagePreference> setLanguage;
    private readonly Func<MergeGameState, MergeProfileSyncResult> syncProgression;
    private readonly Func<PlayerProfileState?> profile;
    private readonly object audioOwner = new();
    private static readonly IReadOnlyDictionary<MergeCue, AudioClip> Cues = Enum.GetValues<MergeCue>().ToDictionary(x => x, MergeSoundSynthesis.Build);
    private readonly Dictionary<int, Button> cells = [];
    private readonly Dictionary<int, Guid> displayed = [];
    private readonly Dictionary<string, Border> orderCards = [];
    private readonly Dictionary<string, Button> producerButtons = [];

    private Canvas table = null!;
    private Canvas effects = null!;
    private StackPanel orders = null!;
    private StackPanel renovation = null!;
    private StackPanel challenges = null!;
    private TextBlock feedback = null!;
    private TextBlock selection = null!;
    private TextBlock pointsLabel = null!;
    private TextBlock spaceLabel = null!;
    private TextBlock profileLabel = null!;
    private TextBlock progressionFeedback = null!;
    private Guid? selected;
    private Point dragStart;
    private Guid? dragItem;
    private bool suppressClick;
    private bool disposed;
    private bool progressionSaveFailed;
    private long lastPlayerXp;
    private long lastSeasonXp;
    private string statusKey = "Merge.Help";
    private object?[] statusArgs = [];

    internal MergeGameState Snapshot => board.Snapshot;

    internal MidnightMergeView(
        MergeBoard board,
        Func<bool> sounds,
        Func<bool> animations,
        Action back,
        Action<UiLanguagePreference>? setLanguage = null,
        Func<MergeGameState, MergeProfileSyncResult>? syncProgression = null,
        Func<PlayerProfileState?>? profile = null)
    {
        this.board = board;
        this.sounds = sounds;
        this.animations = animations;
        this.back = back;
        this.setLanguage = setLanguage ?? (language => Localization.SetLanguage(language));
        this.syncProgression = syncProgression ?? (_ => MergeProfileSyncResult.None);
        this.profile = profile ?? (() => null);
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(this, KeyboardNavigationMode.Cycle);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            back();
        };
        Background = Ink("#0B141A");
        Build();
        Localization.LanguageChanged += LanguageChanged;
    }

    private void LanguageChanged(object? sender, EventArgs e) { if (!disposed) Build(); }
    internal static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock Text(string text, double size = 13, string color = "#B6C4C5") => new()
    { Text = text, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap };
    private static Border Card(UIElement child, string border = "#4C4A3A", Thickness? padding = null) => new()
    { Child = child, Background = Ink("#121F26"), BorderBrush = Ink(border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = padding ?? new Thickness(16) };

    private static Button ActionButton(string text, string name)
    {
        var button = new Button
        {
            Content = Text(text, 12, "#F2E2C5"), Name = name, Padding = new Thickness(14, 10, 14, 10),
            Background = Ink("#23383B"), Foreground = Ink("#F2E2C5"), BorderBrush = Ink("#8B7954"),
            BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
        };
        var style = new Style(typeof(Button));
        var template = new ControlTemplate(typeof(Button));
        var chrome = new FrameworkElementFactory(typeof(Border));
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        chrome.AppendChild(presenter);
        template.VisualTree = chrome;
        style.Setters.Add(new Setter(TemplateProperty, template));
        var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(BackgroundProperty, Ink("#344D4D")));
        var focus = new Trigger { Property = IsKeyboardFocusedProperty, Value = true };
        focus.Setters.Add(new Setter(BorderBrushProperty, Ink("#F1CD7A")));
        style.Triggers.Add(hover);
        style.Triggers.Add(focus);
        button.Style = style;
        AutomationProperties.SetName(button, text);
        return button;
    }

    private void Build()
    {
        cells.Clear();
        displayed.Clear();
        orderCards.Clear();
        producerButtons.Clear();

        var root = new Grid
        {
            Width = 1520,
            Height = 900,
            Background = new LinearGradientBrush(Color.FromRgb(17, 31, 37), Color.FromRgb(8, 17, 23), 70),
        };
        var body = new Grid { Margin = new Thickness(24) };
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(260) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(472) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        root.Children.Add(body);

        BuildHeader(body);
        BuildRestoration(body);
        BuildWorkspace(body);
        BuildFooter(body);

        Content = new Viewbox { Child = root, Stretch = Stretch.Uniform };
        Refresh();
    }

    private void BuildHeader(Grid body)
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(370) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(Text(Localization.T("Merge.Eyebrow"), 10, "#C5AB74"));
        title.Children.Add(new TextBlock
        {
            Text = Localization.T("Merge.Title"), FontFamily = new FontFamily("Georgia"), FontSize = 34,
            Foreground = Ink("#F2E2C5"), Margin = new Thickness(0, 2, 0, 1),
        });
        pointsLabel = Text("", 12, "#CCBC98");
        pointsLabel.Name = "MergePoints";
        title.Children.Add(pointsLabel);
        header.Children.Add(title);

        var profileStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 20, 0) };
        profileStack.Children.Add(Text(Localization.T("Merge.GlobalProgress"), 9, "#89BBAA"));
        profileLabel = Text("", 12, "#E5D2A9");
        profileStack.Children.Add(profileLabel);
        progressionFeedback = Text("", 10, "#9CCEB7");
        progressionFeedback.Margin = new Thickness(0, 3, 0, 0);
        profileStack.Children.Add(progressionFeedback);
        Grid.SetColumn(profileStack, 1);
        header.Children.Add(profileStack);

        var navigation = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Button language = ActionButton(Localization.Language == UiLanguagePreference.English ? "Deutsch" : "English", "MergeLanguage");
        language.Margin = new Thickness(0, 0, 12, 0);
        language.Click += (_, _) => setLanguage(Localization.Language == UiLanguagePreference.English ? UiLanguagePreference.German : UiLanguagePreference.English);
        navigation.Children.Add(language);
        Button leave = ActionButton(Localization.T("Merge.Back"), "MergeBack");
        leave.Click += (_, _) => back();
        navigation.Children.Add(leave);
        Grid.SetColumn(navigation, 2);
        header.Children.Add(navigation);
        body.Children.Add(header);
    }

    private void BuildRestoration(Grid body)
    {
        renovation = new StackPanel();
        var panorama = Card(renovation, "#8E7851", new Thickness(14, 10, 14, 10));
        panorama.Margin = new Thickness(0, 4, 0, 8);
        Grid.SetRow(panorama, 1);
        body.Children.Add(panorama);
    }

    private void BuildWorkspace(Grid body)
    {
        var main = new Grid();
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(780) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        main.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(main, 2);
        body.Children.Add(main);

        var left = new Grid();
        left.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(560) });
        left.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        left.ColumnDefinitions.Add(new ColumnDefinition());

        table = new Canvas
        {
            Width = BoardWidth,
            Height = BoardHeight,
            Background = new LinearGradientBrush(Color.FromRgb(26, 52, 53), Color.FromRgb(15, 35, 39), 90),
            ClipToBounds = false,
        };
        BuildCells();
        effects = new Canvas { Width = BoardWidth, Height = BoardHeight, IsHitTestVisible = false };
        Panel.SetZIndex(effects, 100);
        table.Children.Add(effects);
        var tableFrame = Card(table, "#8E7851", new Thickness(8));
        tableFrame.HorizontalAlignment = HorizontalAlignment.Left;
        left.Children.Add(tableFrame);

        var tools = new StackPanel { VerticalAlignment = VerticalAlignment.Stretch };
        tools.Children.Add(Text(Localization.T("Merge.Producer"), 11, "#D5B879"));
        foreach (MergeProducerDefinition definition in MergeCatalog.Default.Producers.Values.OrderBy(x => x.UnlockPoints))
        {
            string name = definition.Id == "chip-case" ? "MergeProducer" : "MergeProducer_" + definition.Id.Replace('-', '_');
            Button producer = ActionButton(Localization.T(definition.DisplayName), name);
            producer.Margin = new Thickness(0, 8, 0, 2);
            producer.Click += (_, _) => Execute(() => board.TrySpawn(definition.Id), producer);
            producerButtons[definition.Id] = producer;
            tools.Children.Add(producer);
        }
        var noEnergy = Text(Localization.T("Merge.NoEnergy"), 11, "#C6B78F");
        noEnergy.Margin = new Thickness(0, 8, 0, 0);
        tools.Children.Add(noEnergy);
        spaceLabel = Text("", 10);
        spaceLabel.Margin = new Thickness(0, 4, 0, 0);
        tools.Children.Add(spaceLabel);
        tools.Children.Add(new Border { Height = 1, Background = Ink("#34494D"), Margin = new Thickness(0, 14, 0, 12) });
        tools.Children.Add(Text(Localization.T("Merge.HowToPlay"), 10, "#89BBAA"));
        tools.Children.Add(Text(Localization.T("Merge.SelectionHelp"), 10, "#AEBBBC"));
        var toolsCard = Card(tools, "#3C5554", new Thickness(14));
        Grid.SetColumn(toolsCard, 2);
        left.Children.Add(toolsCard);
        main.Children.Add(left);

        var side = new Grid();
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(285) });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        side.RowDefinitions.Add(new RowDefinition());

        var orderTitle = Text(Localization.T("Merge.Orders"), 11, "#D5B879");
        orderTitle.VerticalAlignment = VerticalAlignment.Center;
        side.Children.Add(orderTitle);
        orders = new StackPanel();
        var orderScroll = new ScrollViewer
        {
            Content = orders,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Grid.SetRow(orderScroll, 1);
        side.Children.Add(orderScroll);

        var challengeTitle = Text(Localization.T("Merge.Challenges"), 11, "#D5B879");
        challengeTitle.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(challengeTitle, 2);
        side.Children.Add(challengeTitle);
        challenges = new StackPanel();
        var challengeCard = Card(challenges, "#34565A", new Thickness(10));
        Grid.SetRow(challengeCard, 3);
        side.Children.Add(challengeCard);

        Grid.SetColumn(side, 2);
        main.Children.Add(side);
    }

    private void BuildCells()
    {
        for (int position = 0; position < MergeGameState.Capacity; position++)
        {
            int index = position;
            var cell = ActionButton("", $"MergeCell_{index}");
            cell.Tag = index;
            cell.Width = CellSize;
            cell.Height = CellSize;
            cell.Padding = new Thickness(0);
            cell.Background = Ink("#172E32");
            cell.BorderBrush = Ink("#345052");
            cell.AllowDrop = true;
            Canvas.SetLeft(cell, 18 + index % MergeGameState.Columns * Pitch);
            Canvas.SetTop(cell, 18 + index / MergeGameState.Columns * Pitch);
            cell.Click += (_, _) => ClickCell(index);
            cell.PreviewMouseLeftButtonDown += (_, e) =>
            {
                dragStart = e.GetPosition(this);
                dragItem = board.Snapshot.Items.FirstOrDefault(x => x.Position == index)?.Id;
            };
            cell.PreviewMouseMove += (_, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed || dragItem is not { } id) return;
                Point now = e.GetPosition(this);
                if (Math.Abs(now.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(now.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
                Play(MergeCue.PickUp);
                dragItem = null;
                selected = id;
                Refresh();
                suppressClick = true;
                try { DragDrop.DoDragDrop(cell, new DataObject("MidnightMergeItem", id), DragDropEffects.Move); }
                finally { selected = null; Refresh(); Dispatcher.BeginInvoke(() => suppressClick = false); }
                e.Handled = true;
            };
            cell.DragOver += (_, e) =>
            {
                if (e.Data.GetData("MidnightMergeItem") is Guid id)
                {
                    bool allowed = board.CanMove(id, index) || board.CanMerge(id, index);
                    e.Effects = allowed ? DragDropEffects.Move : DragDropEffects.None;
                    cell.BorderBrush = Ink(allowed ? "#EAC77D" : "#CF7C7C");
                }
                e.Handled = true;
            };
            cell.DragLeave += (_, _) => RefreshHighlights();
            cell.Drop += (_, e) =>
            {
                if (e.Data.GetData("MidnightMergeItem") is Guid id)
                {
                    selected = null;
                    Execute(() => board.TryDrop(id, index), cell);
                }
                e.Handled = true;
            };
            cells.Add(index, cell);
            table.Children.Add(cell);
        }
    }

    private void BuildFooter(Grid body)
    {
        var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(520) });
        feedback = Text("", 11, "#E4D0A5");
        feedback.Name = "MergeFeedback";
        footer.Children.Add(feedback);
        selection = Text("", 10, "#89BBAA");
        selection.TextAlignment = TextAlignment.Right;
        Grid.SetColumn(selection, 1);
        footer.Children.Add(selection);
        Grid.SetRow(footer, 3);
        body.Children.Add(footer);
    }

    private void ClickCell(int position)
    {
        if (suppressClick) return;
        var item = board.Snapshot.Items.FirstOrDefault(x => x.Position == position);
        if (selected is { } id)
        {
            if (item?.Id == id) { selected = null; Refresh(); return; }
            selected = null;
            Execute(() => board.TryDrop(id, position), cells[position]);
        }
        else if (item is not null)
        {
            selected = item.Id;
            Play(MergeCue.PickUp);
            Refresh();
        }
        else
        {
            SetStatus("Merge.SelectFirst");
            Refresh();
        }
    }

    private void Execute(Func<MergeActionResult> action, FrameworkElement origin, string? orderDefinition = null)
    {
        MergeActionResult result;
        try
        {
            result = action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("Merge.SaveFailed");
            Refresh();
            Play(MergeCue.Invalid);
            Shake(origin);
            return;
        }

        if (!result.Success)
        {
            SetStatus("Merge.Feedback." + result.Outcome);
            Play(MergeCue.Invalid);
            Refresh();
            Shake(origin);
            return;
        }

        if (result.Outcome == MergeOutcome.OrderCompleted) SetStatus("Merge.Reward", result.Reward);
        else SetStatus("Merge.Feedback." + result.Outcome);

        SyncProgression();
        Refresh();

        switch (result.Outcome)
        {
            case MergeOutcome.Spawned:
                Play(MergeCue.Spawn);
                Pop(cells[result.Position].Content as FrameworkElement);
                Pop(origin);
                break;
            case MergeOutcome.Moved:
                Play(MergeCue.Drop);
                Travel(result, false);
                break;
            case MergeOutcome.Merged:
                bool high = MergeCatalog.Default.Items[result.DefinitionId!].Tier >= 4;
                Play(high ? MergeCue.TierUp : MergeCue.Merge);
                Travel(result, true);
                break;
            case MergeOutcome.OrderCompleted:
                Play(MergeCue.Order);
                if (orderDefinition is not null && orderCards.TryGetValue(orderDefinition, out var card)) Pop(card);
                Pop(pointsLabel);
                if (result.Milestones is { Count: > 0 })
                {
                    Play(MergeCue.Milestone);
                    Pop(renovation);
                    MergeArea area = MergeProgression.ActiveArea(MergeCatalog.Default, board.Snapshot.RenovationPoints);
                    SetStatus("Merge.Milestone", MergeProgression.Current(area, board.Snapshot.RenovationPoints).DisplayName, result.Reward);
                    feedback.Text = FormatStatus();
                }
                break;
        }
    }

    private void SyncProgression()
    {
        try
        {
            MergeProfileSyncResult result = syncProgression(board.Snapshot);
            progressionSaveFailed = false;
            if (result.Award.PlayerExperience > 0 || result.Award.SeasonExperience > 0)
            {
                lastPlayerXp = result.Award.PlayerExperience;
                lastSeasonXp = result.Award.SeasonExperience;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            progressionSaveFailed = true;
        }
    }

    private string FormatStatus() => statusKey == "Merge.Milestone"
        ? Localization.F(statusKey, Localization.T((string)statusArgs[0]!), statusArgs[1])
        : Localization.F(statusKey, statusArgs);

    private void SetStatus(string key, params object?[] args)
    {
        statusKey = key;
        statusArgs = args;
    }

    private void Refresh()
    {
        MergeGameState state = board.Snapshot;
        if (selected is { } id && state.Items.All(x => x.Id != id)) selected = null;
        pointsLabel.Text = Localization.F("Merge.Points", state.RenovationPoints);
        spaceLabel.Text = Localization.F("Merge.Space", MergeGameState.Capacity - state.Items.Count, MergeGameState.Capacity);
        feedback.Text = FormatStatus();
        selection.Text = selected is { } selectedId && state.Items.FirstOrDefault(x => x.Id == selectedId) is { } selectedItem
            ? Localization.F("Merge.Selected", Localization.T(MergeCatalog.Default.Items[selectedItem.DefinitionId].DisplayName))
            : Localization.T("Merge.SelectionHelp");

        RefreshProfile();
        RefreshProducers(state);
        RefreshBoard(state);
        RefreshHighlights();
        RefreshOrders(state);
        RefreshRestoration(state);
        RefreshChallenges();
    }

    private void RefreshProfile()
    {
        PlayerProfileState? value = profile();
        if (value is null)
        {
            profileLabel.Text = Localization.T("Merge.ProfileUnavailable");
            progressionFeedback.Text = string.Empty;
            return;
        }
        if (value.Level >= ProgressionCurve.MaximumLevel)
            profileLabel.Text = Localization.F("Merge.ProfileMax", value.Level);
        else
            profileLabel.Text = Localization.F("Merge.ProfileProgress", value.Level, value.LevelExperience, ProgressionCurve.ExperienceForNextLevel(value.Level));

        progressionFeedback.Text = progressionSaveFailed
            ? Localization.T("Merge.ProgressionSaveFailed")
            : lastPlayerXp > 0 || lastSeasonXp > 0
                ? Localization.F("Merge.XpAward", lastPlayerXp, lastSeasonXp)
                : Localization.T("Merge.XpHint");
        progressionFeedback.Foreground = Ink(progressionSaveFailed ? "#CF7C7C" : "#9CCEB7");
    }

    private void RefreshProducers(MergeGameState state)
    {
        foreach (MergeProducerDefinition definition in MergeCatalog.Default.Producers.Values)
        {
            if (!producerButtons.TryGetValue(definition.Id, out Button? button)) continue;
            bool unlocked = state.RenovationPoints >= definition.UnlockPoints;
            bool space = state.Items.Count < MergeGameState.Capacity;
            button.IsEnabled = unlocked && space;
            button.Opacity = unlocked ? 1 : .5;
            button.ToolTip = unlocked
                ? Localization.T("Merge.ProducerHint")
                : Localization.F("Merge.ProducerLocked", definition.UnlockPoints);
        }
    }

    private void RefreshBoard(MergeGameState state)
    {
        foreach (var (position, cell) in cells)
        {
            var item = state.Items.FirstOrDefault(x => x.Position == position);
            if (item is null)
            {
                if (displayed.Remove(position)) cell.Content = null;
                AutomationProperties.SetName(cell, Localization.F("Merge.EmptyCell", position / MergeGameState.Columns + 1, position % MergeGameState.Columns + 1));
            }
            else
            {
                var definition = MergeCatalog.Default.Items[item.DefinitionId];
                if (!displayed.TryGetValue(position, out Guid previous) || previous != item.Id)
                {
                    cell.Content = new MergeChipArtwork(definition, 52);
                    displayed[position] = item.Id;
                }
                AutomationProperties.SetName(cell, Localization.F("Merge.ItemCell", Localization.T(definition.DisplayName), position / MergeGameState.Columns + 1, position % MergeGameState.Columns + 1));
            }
            cell.ToolTip = AutomationProperties.GetName(cell);
        }
    }

    private void RefreshOrders(MergeGameState state)
    {
        orders.Children.Clear();
        orderCards.Clear();
        MergeArea activeArea = MergeProgression.ActiveArea(MergeCatalog.Default, state.RenovationPoints);
        foreach (var active in state.Orders)
        {
            var definition = MergeCatalog.Default.Orders.Single(x => x.Id == active.DefinitionId);
            bool available = MergeOrders.SelectRequired(state, definition) is not null;
            if (!string.Equals(definition.AreaId, activeArea.Id, StringComparison.Ordinal) && !available) continue;

            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
            var description = new StackPanel();
            foreach (var required in definition.Requirements)
            {
                int count = state.Items.Count(x => x.DefinitionId == required.DefinitionId);
                description.Children.Add(Text(Localization.F("Merge.Requirement", required.Count, Localization.T(MergeCatalog.Default.Items[required.DefinitionId].DisplayName)), 11, "#E4D6BB"));
                description.Children.Add(Text(Localization.F("Merge.Available", count, required.Count), 9, available ? "#9CCEB7" : "#8F9EA3"));
            }
            description.Children.Add(Text(Localization.F("Merge.OrderReward", definition.Reward), 10, "#CCB67F"));
            content.Children.Add(description);
            Button fulfill = ActionButton(Localization.T(available ? "Merge.Deliver" : "Merge.Waiting"), "MergeOrder_" + definition.Id.Replace('-', '_'));
            fulfill.Opacity = available ? 1 : .6;
            fulfill.Click += (_, _) => Execute(() => board.TryComplete(active.Id), fulfill, definition.Id);
            Grid.SetColumn(fulfill, 1);
            content.Children.Add(fulfill);
            var card = Card(content, available ? "#8EAA80" : "#34494D", new Thickness(9));
            card.Margin = new Thickness(0, 0, 5, 7);
            orderCards[definition.Id] = card;
            orders.Children.Add(card);
        }
    }

    private void RefreshRestoration(MergeGameState state)
    {
        renovation.Children.Clear();
        IReadOnlyList<MergeArea> areas = MergeCatalog.Default.Areas;
        MergeArea area = MergeProgression.ActiveArea(MergeCatalog.Default, state.RenovationPoints);
        int areaIndex = Enumerable.Range(0, areas.Count).First(i => string.Equals(areas[i].Id, area.Id, StringComparison.Ordinal));
        MergeMilestone current = MergeProgression.Current(area, state.RenovationPoints);

        var heading = new Grid { Margin = new Thickness(2, 0, 2, 4) };
        heading.ColumnDefinitions.Add(new ColumnDefinition());
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var leftHeading = new StackPanel();
        var headingText = Text(Localization.T(area.DisplayName), 15, "#ECD8AF");
        headingText.FontWeight = FontWeights.SemiBold;
        leftHeading.Children.Add(headingText);
        leftHeading.Children.Add(Text(Localization.F("Merge.AreaCounter", areaIndex + 1, areas.Count), 8, "#7FA398"));
        heading.Children.Add(leftHeading);
        var stage = Text(Localization.T(current.DisplayName), 12, "#E3C884");
        Grid.SetColumn(stage, 1);
        heading.Children.Add(stage);
        renovation.Children.Add(heading);
        renovation.Children.Add(area.Id == "bar"
            ? new MergeBarArtwork(state.RenovationPoints, 1400, 184)
            : new MergeLobbyArtwork(state.RenovationPoints, 1400, 184));

        var track = new Grid { Height = 6, Margin = new Thickness(2, 7, 2, 3), Background = Ink("#2C3D41") };
        track.Children.Add(new Border
        {
            Background = Ink("#D8B46C"), HorizontalAlignment = HorizontalAlignment.Left,
            Width = 1400 * MergeProgression.AreaProgress(area, state.RenovationPoints), CornerRadius = new CornerRadius(3),
        });
        renovation.Children.Add(track);

        var milestones = new Grid { Margin = new Thickness(2, 0, 2, 0) };
        for (int i = 0; i < area.Milestones.Count; i++) milestones.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < area.Milestones.Count; i++)
        {
            MergeMilestone milestone = area.Milestones[i];
            bool unlocked = state.RenovationPoints >= milestone.Points;
            var label = Text(Localization.T(milestone.DisplayName), 9, unlocked ? "#E3C884" : "#758489");
            label.TextAlignment = i == 0 ? TextAlignment.Left : i == area.Milestones.Count - 1 ? TextAlignment.Right : TextAlignment.Center;
            Grid.SetColumn(label, i);
            milestones.Children.Add(label);
        }
        renovation.Children.Add(milestones);
    }

    private void RefreshChallenges()
    {
        challenges.Children.Clear();
        PlayerProfileState? value = profile();
        if (value is null)
        {
            challenges.Children.Add(Text(Localization.T("Merge.ProfileUnavailable"), 10));
            return;
        }

        var claimed = value.ClaimedChallengeIds.ToHashSet(StringComparer.Ordinal);
        var candidates = ChallengeCatalog.For(ChallengeCategory.MidnightMerge)
            .Where(x => !claimed.Contains(x.Id))
            .Select(x => new { Challenge = x, Progress = ChallengeCatalog.Progress(value, x) })
            .OrderBy(x => x.Progress >= x.Challenge.Goal ? 0 : 1)
            .ThenBy(x => Math.Max(0, x.Challenge.Goal - x.Progress))
            .Take(2)
            .ToArray();

        if (candidates.Length == 0)
        {
            challenges.Children.Add(Text(Localization.T("Merge.AllChallengesComplete"), 10, "#9CCEB7"));
            return;
        }

        foreach (var entry in candidates)
        {
            bool complete = entry.Progress >= entry.Challenge.Goal;
            var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            var labels = new StackPanel();
            labels.Children.Add(Text(entry.Challenge.Name, 10, complete ? "#E7D08F" : "#D9D0BC"));
            labels.Children.Add(Text(entry.Challenge.Description, 9, "#8F9EA3"));
            row.Children.Add(labels);
            var progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            progressStack.Children.Add(Text(complete ? Localization.T("Merge.ChallengeReady") : Localization.F("Merge.ChallengeProgress", Math.Min(entry.Progress, entry.Challenge.Goal), entry.Challenge.Goal), 9, complete ? "#9CCEB7" : "#AAB7B8"));
            var bar = new Grid { Height = 5, Background = Ink("#263C40"), Margin = new Thickness(0, 5, 0, 0) };
            bar.Children.Add(new Border
            {
                Background = Ink(complete ? "#D8B46C" : "#72A990"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 135 * Math.Min(1, (double)(entry.Progress / Math.Max(1, entry.Challenge.Goal))),
                CornerRadius = new CornerRadius(2),
            });
            progressStack.Children.Add(bar);
            Grid.SetColumn(progressStack, 1);
            row.Children.Add(progressStack);
            challenges.Children.Add(row);
        }
    }

    private void RefreshHighlights()
    {
        var state = board.Snapshot;
        foreach (var (position, cell) in cells)
        {
            bool own = selected is { } id && state.Items.Any(x => x.Id == id && x.Position == position);
            bool merge = selected is { } source && board.CanMerge(source, position);
            cell.BorderBrush = Ink(own ? "#F2D18A" : merge ? "#84CDAA" : "#345052");
            cell.BorderThickness = new Thickness(own || merge ? 2 : 1);
        }
    }

    private void Play(MergeCue cue)
    {
        if (sounds()) CasinoAudio.Play(Cues[cue], audioOwner, .75);
    }

    private void Pop(FrameworkElement? element)
    {
        if (!animations() || element is null) return;
        element.RenderTransformOrigin = new Point(.5, .5);
        var scale = new ScaleTransform(1, 1);
        var rise = new TranslateTransform();
        var transform = new TransformGroup();
        transform.Children.Add(scale);
        transform.Children.Add(rise);
        element.RenderTransform = transform;
        var lift = new DoubleAnimationUsingKeyFrames();
        lift.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        lift.KeyFrames.Add(new EasingDoubleKeyFrame(-7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
        lift.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
        rise.BeginAnimation(TranslateTransform.YProperty, lift);
        var pulse = new DoubleAnimationUsingKeyFrames();
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(.76, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(.35, 1, TimeSpan.FromMilliseconds(220)));
    }

    private void Shake(FrameworkElement element)
    {
        if (!animations()) return;
        var translate = new TranslateTransform();
        element.RenderTransform = translate;
        var animation = new DoubleAnimationUsingKeyFrames();
        double[] values = [0, -5, 5, -3, 3, 0];
        for (int i = 0; i < values.Length; i++)
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(values[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 45))));
        translate.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void Travel(MergeActionResult result, bool merge)
    {
        if (!animations()) return;
        FrameworkElement? item = cells[result.Position].Content as FrameworkElement;
        if (item is null) return;
        var slide = new TranslateTransform(
            (result.FromPosition % MergeGameState.Columns - result.Position % MergeGameState.Columns) * Pitch,
            (result.FromPosition / MergeGameState.Columns - result.Position / MergeGameState.Columns) * Pitch);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        if (!merge)
        {
            item.RenderTransform = slide;
            Panel.SetZIndex(cells[result.Position], 5);
            slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(slide.X, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
            slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(slide.Y, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
            return;
        }

        string sourceDefinition = result.SourceDefinitionId!;
        var ghost = new MergeChipArtwork(MergeCatalog.Default.Items[sourceDefinition], 52) { RenderTransform = slide };
        double x = Canvas.GetLeft(cells[result.Position]) + 6;
        double y = Canvas.GetTop(cells[result.Position]) + 6;
        Canvas.SetLeft(ghost, x);
        Canvas.SetTop(ghost, y);
        effects.Children.Add(ghost);
        var finish = new DoubleAnimation(slide.X, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
        finish.Completed += (_, _) => effects.Children.Remove(ghost);
        slide.BeginAnimation(TranslateTransform.XProperty, finish);
        slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(slide.Y, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
        Pop(item);
        var flash = new Border
        {
            Width = CellSize,
            Height = CellSize,
            CornerRadius = new CornerRadius(10),
            BorderBrush = Ink("#F8D98E"),
            BorderThickness = new Thickness(3),
            Background = Ink("#D4B778"),
            Opacity = .45,
        };
        Canvas.SetLeft(flash, x - 6);
        Canvas.SetTop(flash, y - 6);
        effects.Children.Add(flash);
        var fade = new DoubleAnimation(.45, 0, TimeSpan.FromMilliseconds(400));
        fade.Completed += (_, _) => effects.Children.Remove(flash);
        flash.BeginAnimation(OpacityProperty, fade);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Localization.LanguageChanged -= LanguageChanged;
        CasinoAudio.Mixer.Stop(audioOwner);
    }
}
