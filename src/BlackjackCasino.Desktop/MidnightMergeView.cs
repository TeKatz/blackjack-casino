using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

internal sealed class MidnightMergeView : UserControl, IDisposable
{
    private const double Pitch = 86;
    private readonly MergeBoard board;
    private readonly Func<bool> sounds;
    private readonly Func<bool> animations;
    private readonly Action back;
    private readonly Action<UiLanguagePreference> setLanguage;
    private readonly object audioOwner = new();
    private static readonly IReadOnlyDictionary<MergeCue, AudioClip> Cues = Enum.GetValues<MergeCue>().ToDictionary(x => x, MergeSoundSynthesis.Build);
    private readonly Dictionary<int, Button> cells = [];
    private readonly Dictionary<int, Guid> displayed = [];
    private readonly Dictionary<string, Border> orderCards = [];
    private Canvas table = null!;
    private Canvas effects = null!;
    private StackPanel orders = null!;
    private StackPanel renovation = null!;
    private TextBlock feedback = null!;
    private TextBlock selection = null!;
    private TextBlock pointsLabel = null!;
    private TextBlock spaceLabel = null!;
    private Button producer = null!;
    private Guid? selected;
    private Point dragStart;
    private Guid? dragItem;
    private bool suppressClick;
    private bool disposed;
    private string statusKey = "Merge.Help";
    private object?[] statusArgs = [];
    internal MergeGameState Snapshot => board.Snapshot;

    internal MidnightMergeView(MergeBoard board, Func<bool> sounds, Func<bool> animations, Action back, Action<UiLanguagePreference>? setLanguage = null)
    {
        this.board = board;
        this.sounds = sounds;
        this.animations = animations;
        this.back = back;
        this.setLanguage = setLanguage ?? (language => Localization.SetLanguage(language));
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

    // A local template keeps Windows theme chrome from whitening the tabletop or producer.
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
        style.Triggers.Add(hover); style.Triggers.Add(focus);
        button.Style = style;
        AutomationProperties.SetName(button, text);
        return button;
    }

    private void Build()
    {
        cells.Clear(); displayed.Clear(); orderCards.Clear();
        var root = new Grid { Width = 1160, Height = 860, Background = new LinearGradientBrush(Color.FromRgb(17, 31, 37), Color.FromRgb(8, 17, 23), 70) };
        var body = new Grid { Margin = new Thickness(28) };
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(660) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
        root.Children.Add(body);
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(Text(Localization.T("Merge.Eyebrow"), 10, "#C5AB74"));
        title.Children.Add(new TextBlock { Text = Localization.T("Merge.Title"), FontFamily = new FontFamily("Georgia"), FontSize = 34, Foreground = Ink("#F2E2C5"), Margin = new Thickness(0, 4, 0, 3) });
        pointsLabel = Text("", 13, "#CCBC98"); pointsLabel.Name = "MergePoints";
        title.Children.Add(pointsLabel);
        header.Children.Add(title);
        var navigation = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        Button language = ActionButton(Localization.Language == UiLanguagePreference.English ? "Deutsch" : "English", "MergeLanguage");
        language.Margin = new Thickness(0, 0, 12, 0);
        language.Click += (_, _) => this.setLanguage(Localization.Language == UiLanguagePreference.English ? UiLanguagePreference.German : UiLanguagePreference.English);
        navigation.Children.Add(language);
        Button leave = ActionButton(Localization.T("Merge.Back"), "MergeBack");
        leave.Click += (_, _) => back();
        navigation.Children.Add(leave);
        Grid.SetColumn(navigation, 1); header.Children.Add(navigation); body.Children.Add(header);

        var main = new Grid();
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(724) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(356) });
        Grid.SetRow(main, 1); body.Children.Add(main);
        var left = new StackPanel();
        var supply = new Grid { Height = 64, Margin = new Thickness(0, 0, 0, 10) };
        supply.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        supply.ColumnDefinitions.Add(new ColumnDefinition());
        producer = ActionButton(Localization.T("Merge.Produce"), "MergeProducer");
        producer.ToolTip = Localization.T("Merge.ProducerHint");
        producer.Click += (_, _) => Execute(() => board.TrySpawn(), producer);
        supply.Children.Add(producer);
        var supplyText = new StackPanel { Margin = new Thickness(18, 3, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        supplyText.Children.Add(Text(Localization.T("Merge.NoEnergy"), 12, "#C6B78F"));
        spaceLabel = Text("", 11); supplyText.Children.Add(spaceLabel);
        Grid.SetColumn(supplyText, 1); supply.Children.Add(supplyText); left.Children.Add(supply);
        table = new Canvas { Width = 630, Height = 544, Background = new LinearGradientBrush(Color.FromRgb(26, 52, 53), Color.FromRgb(15, 35, 39), 90), ClipToBounds = false };
        for (int position = 0; position < MergeGameState.Capacity; position++)
        {
            int index = position;
            var cell = ActionButton("", $"MergeCell_{index}");
            cell.Tag = index; cell.Width = 78; cell.Height = 78; cell.Padding = new Thickness(0);
            cell.Background = Ink("#172E32"); cell.BorderBrush = Ink("#345052"); cell.AllowDrop = true;
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
                Play(MergeCue.PickUp); dragItem = null; selected = id; Refresh(); suppressClick = true;
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
                if (e.Data.GetData("MidnightMergeItem") is Guid id) { selected = null; Execute(() => board.TryDrop(id, index), cell); }
                e.Handled = true;
            };
            cells.Add(index, cell); table.Children.Add(cell);
        }
        effects = new Canvas { Width = 630, Height = 544, IsHitTestVisible = false };
        Panel.SetZIndex(effects, 100); table.Children.Add(effects);
        var tableFrame = Card(table, "#8E7851", new Thickness(18));
        tableFrame.HorizontalAlignment = HorizontalAlignment.Center;
        left.Children.Add(tableFrame); main.Children.Add(left);

        var side = new Grid();
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(258) });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        side.RowDefinitions.Add(new RowDefinition());
        renovation = new StackPanel(); side.Children.Add(Card(renovation));
        var orderTitle = Text(Localization.T("Merge.Orders"), 11, "#D5B879");
        orderTitle.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(orderTitle, 1); side.Children.Add(orderTitle);
        orders = new StackPanel();
        var orderScroll = new ScrollViewer { Content = orders, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(orderScroll, 2); side.Children.Add(orderScroll);
        Grid.SetColumn(side, 2); main.Children.Add(side);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        feedback = Text("", 12, "#E4D0A5"); feedback.Name = "MergeFeedback"; footer.Children.Add(feedback);
        selection = Text("", 11, "#89BBAA"); Grid.SetColumn(selection, 1); footer.Children.Add(selection);
        Grid.SetRow(footer, 2); body.Children.Add(footer);
        Content = new Viewbox { Child = root, Stretch = Stretch.Uniform };
        Refresh();
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
        else if (item is not null) { selected = item.Id; Play(MergeCue.PickUp); Refresh(); }
        else { SetStatus("Merge.SelectFirst"); Refresh(); }
    }

    private void Execute(Func<MergeActionResult> action, FrameworkElement origin, string? orderDefinition = null)
    {
        try
        {
            MergeActionResult result = action();
            if (!result.Success)
            {
                SetStatus("Merge.Feedback." + result.Outcome);
                Play(MergeCue.Invalid); Refresh(); Shake(origin); return;
            }
            if (result.Outcome == MergeOutcome.OrderCompleted)
                SetStatus("Merge.Reward", result.Reward);
            else SetStatus("Merge.Feedback." + result.Outcome);
            Refresh();
            switch (result.Outcome)
            {
                case MergeOutcome.Spawned:
                    Play(MergeCue.Spawn); Pop(cells[result.Position].Content as FrameworkElement); Pop(producer); break;
                case MergeOutcome.Moved:
                    Play(MergeCue.Drop); Travel(result, false); break;
                case MergeOutcome.Merged:
                    bool high = MergeCatalog.Default.Items[result.DefinitionId!].Tier >= 4;
                    Play(high ? MergeCue.TierUp : MergeCue.Merge); Travel(result, true); break;
                case MergeOutcome.OrderCompleted:
                    Play(MergeCue.Order);
                    if (orderDefinition is not null && orderCards.TryGetValue(orderDefinition, out var card)) Pop(card);
                    Pop(pointsLabel);
                    if (result.Milestones is { Count: > 0 })
                    {
                        Play(MergeCue.Milestone); Pop(renovation);
                        SetStatus("Merge.Milestone", MergeProgression.Current(MergeCatalog.Default.Areas[0], board.Snapshot.RenovationPoints).DisplayName, result.Reward);
                        feedback.Text = FormatStatus();
                    }
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { SetStatus("Merge.SaveFailed"); Refresh(); Play(MergeCue.Invalid); Shake(origin); }
    }

    private string FormatStatus() => statusKey == "Merge.Milestone"
        ? Localization.F(statusKey, Localization.T((string)statusArgs[0]!), statusArgs[1])
        : Localization.F(statusKey, statusArgs);
    private void SetStatus(string key, params object?[] args) { statusKey = key; statusArgs = args; }
    private void Refresh()
    {
        MergeGameState state = board.Snapshot;
        if (selected is { } id && state.Items.All(x => x.Id != id)) selected = null;
        pointsLabel.Text = Localization.F("Merge.Points", state.RenovationPoints);
        spaceLabel.Text = Localization.F("Merge.Space", MergeGameState.Capacity - state.Items.Count, MergeGameState.Capacity);
        feedback.Text = FormatStatus();
        selection.Text = selected is { } selectedId && state.Items.FirstOrDefault(x => x.Id == selectedId) is { } selectedItem
            ? Localization.F("Merge.Selected", Localization.T(MergeCatalog.Default.Items[selectedItem.DefinitionId].DisplayName)) : Localization.T("Merge.SelectionHelp");
        foreach (var (position, cell) in cells)
        {
            var item = state.Items.FirstOrDefault(x => x.Position == position);
            if (item is null)
            {
                if (displayed.Remove(position)) cell.Content = null;
                AutomationProperties.SetName(cell, Localization.F("Merge.EmptyCell", position / 7 + 1, position % 7 + 1));
            }
            else
            {
                var definition = MergeCatalog.Default.Items[item.DefinitionId];
                if (!displayed.TryGetValue(position, out Guid previous) || previous != item.Id)
                { cell.Content = new MergeChipArtwork(definition); displayed[position] = item.Id; }
                AutomationProperties.SetName(cell, Localization.F("Merge.ItemCell", Localization.T(definition.DisplayName), position / 7 + 1, position % 7 + 1));
            }
            cell.ToolTip = AutomationProperties.GetName(cell);
        }
        RefreshHighlights();
        orders.Children.Clear(); orderCards.Clear();
        foreach (var active in state.Orders)
        {
            var definition = MergeCatalog.Default.Orders.Single(x => x.Id == active.DefinitionId);
            bool available = MergeOrders.SelectRequired(state, definition) is not null;
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
            var description = new StackPanel();
            foreach (var required in definition.Requirements)
            {
                int count = state.Items.Count(x => x.DefinitionId == required.DefinitionId);
                description.Children.Add(Text(Localization.F("Merge.Requirement", required.Count, Localization.T(MergeCatalog.Default.Items[required.DefinitionId].DisplayName)), 12, "#E4D6BB"));
                description.Children.Add(Text(Localization.F("Merge.Available", count, required.Count), 10, available ? "#9CCEB7" : "#8F9EA3"));
            }
            description.Children.Add(Text(Localization.F("Merge.OrderReward", definition.Reward), 11, "#CCB67F"));
            content.Children.Add(description);
            Button fulfill = ActionButton(Localization.T(available ? "Merge.Deliver" : "Merge.Waiting"), "MergeOrder_" + definition.Id.Replace('-', '_'));
            fulfill.Opacity = available ? 1 : .6;
            // Missing-item clicks still give explicit feedback; the Core remains the authority.
            fulfill.Click += (_, _) => Execute(() => board.TryComplete(active.Id), fulfill, definition.Id);
            Grid.SetColumn(fulfill, 1); content.Children.Add(fulfill);
            var card = Card(content, available ? "#8EAA80" : "#34494D", new Thickness(10));
            card.Margin = new Thickness(0, 0, 5, 8); orderCards[definition.Id] = card; orders.Children.Add(card);
        }
        renovation.Children.Clear();
        MergeArea area = MergeCatalog.Default.Areas[0];
        MergeMilestone current = MergeProgression.Current(area, state.RenovationPoints);
        renovation.Children.Add(Text(Localization.T(area.DisplayName), 17, "#ECD8AF"));
        renovation.Children.Add(new MergeLobbyArtwork(state.RenovationPoints) { Margin = new Thickness(0, 10, 0, 8) });
        renovation.Children.Add(Text(Localization.T(current.DisplayName), 14, "#E3C884"));
        var track = new Grid { Height = 5, Margin = new Thickness(0, 8, 0, 7), Background = Ink("#2C3D41") };
        track.Children.Add(new Border { Background = Ink("#D8B46C"), HorizontalAlignment = HorizontalAlignment.Left,
            Width = 320 * Math.Min(1, state.RenovationPoints / 100d), CornerRadius = new CornerRadius(2) });
        renovation.Children.Add(track);
        renovation.Children.Add(Text(Localization.F("Merge.Progress", Math.Min(100, state.RenovationPoints), 100), 11));
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

    private void Play(MergeCue cue) { if (sounds()) CasinoAudio.Play(Cues[cue], audioOwner, .75); }
    private void Pop(FrameworkElement? element)
    {
        if (!animations() || element is null) return;
        element.RenderTransformOrigin = new Point(.5, .5);
        var scale = new ScaleTransform(1, 1);
        var rise = new TranslateTransform();
        var transform = new TransformGroup(); transform.Children.Add(scale); transform.Children.Add(rise);
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
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse); scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(.35, 1, TimeSpan.FromMilliseconds(220)));
    }
    private void Shake(FrameworkElement element)
    {
        if (!animations()) return;
        var translate = new TranslateTransform(); element.RenderTransform = translate;
        var animation = new DoubleAnimationUsingKeyFrames();
        double[] values = [0, -5, 5, -3, 3, 0];
        for (int i = 0; i < values.Length; i++) animation.KeyFrames.Add(new LinearDoubleKeyFrame(values[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 45))));
        translate.BeginAnimation(TranslateTransform.XProperty, animation);
    }
    private void Travel(MergeActionResult result, bool merge)
    {
        if (!animations()) return;
        FrameworkElement? item = cells[result.Position].Content as FrameworkElement;
        if (item is null) return;
        var slide = new TranslateTransform(
            (result.FromPosition % 7 - result.Position % 7) * Pitch,
            (result.FromPosition / 7 - result.Position / 7) * Pitch);
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
        var ghost = new MergeChipArtwork(MergeCatalog.Default.Items[sourceDefinition]) { RenderTransform = slide };
        double x = Canvas.GetLeft(cells[result.Position]) + 7, y = Canvas.GetTop(cells[result.Position]) + 7;
        Canvas.SetLeft(ghost, x); Canvas.SetTop(ghost, y); effects.Children.Add(ghost);
        var finish = new DoubleAnimation(slide.X, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
        finish.Completed += (_, _) => effects.Children.Remove(ghost);
        slide.BeginAnimation(TranslateTransform.XProperty, finish);
        slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(slide.Y, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
        Pop(item);
        var flash = new Border { Width = 78, Height = 78, CornerRadius = new CornerRadius(10), BorderBrush = Ink("#F8D98E"), BorderThickness = new Thickness(3), Background = Ink("#D4B778"), Opacity = .45 };
        Canvas.SetLeft(flash, x - 7); Canvas.SetTop(flash, y - 7); effects.Children.Add(flash);
        var fade = new DoubleAnimation(.45, 0, TimeSpan.FromMilliseconds(400));
        fade.Completed += (_, _) => effects.Children.Remove(flash);
        flash.BeginAnimation(OpacityProperty, fade);
    }

    public void Dispose()
    {
        if (disposed) return; disposed = true;
        Localization.LanguageChanged -= LanguageChanged;
        CasinoAudio.Mixer.Stop(audioOwner);
    }
}
