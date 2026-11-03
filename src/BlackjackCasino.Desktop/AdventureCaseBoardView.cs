using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal sealed class AdventureCaseBoardView : Grid
{
    private const int CurrentLayoutVersion = 3;
    private const double LegacyWidth = 1440;
    private const double LegacyHeight = 900;
    private const double CardWidth = 280;
    private const double CardHeight = 150;
    private const double FrameInset = 34;
    private const double EdgePadding = 72;

    private readonly AdventureEngine engine;
    private readonly CaseBoardDefinition definition;
    private readonly Action<AdventureRunState> persist;
    private readonly double boardWidth;
    private readonly double boardHeight;
    private readonly Canvas canvas;
    private readonly Grid boardSurface;
    private readonly Border viewport = new();
    private readonly StackPanel details = new();
    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform translate = new();
    private readonly HashSet<CaseNodeCategory> visibleCategories = Enum.GetValues<CaseNodeCategory>().ToHashSet();
    private readonly Dictionary<string, Border> nodeCards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Line> connectionLines = new(StringComparer.OrdinalIgnoreCase);

    private string? selectedNodeId;
    private Border? draggedCard;
    private string? draggedNodeId;
    private Point? nodeDragOrigin;
    private double nodeDragStartX;
    private double nodeDragStartY;
    private bool nodeMoved;
    private Point? panOrigin;
    private double panStartX;
    private double panStartY;
    private Popup? nodePopup;
    private string storySignature = string.Empty;

    internal AdventureCaseBoardView(AdventureEngine engine, Action<AdventureRunState> persist)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        definition = engine.Definition.CaseBoard
            ?? throw new ArgumentException($"Adventure '{engine.Definition.Id}' does not define a Case Board.", nameof(engine));

        (boardWidth, boardHeight) = CalculateExtent(definition);
        canvas = new Canvas
        {
            Width = boardWidth,
            Height = boardHeight,
            Background = Brushes.Transparent,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };
        boardSurface = BuildBoardSurface();

        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        bool migrated = MigrateLayoutIfNeeded();
        BuildShell();
        RestoreView();
        RefreshBoard();
        storySignature = BuildStorySignature();

        Loaded += (_, _) =>
        {
            if (migrated) Dispatcher.BeginInvoke(FitKnownNodes, DispatcherPriority.Loaded);
            else Dispatcher.BeginInvoke(ConstrainCamera, DispatcherPriority.Loaded);
        };
        Unloaded += (_, _) => ClosePopup();
    }

    internal void RefreshFromRunState(bool force = false)
    {
        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        string next = BuildStorySignature();
        if (!force && string.Equals(next, storySignature, StringComparison.Ordinal)) return;
        storySignature = next;
        ClosePopup();
        RefreshBoard();
    }

    private static (double Width, double Height) CalculateExtent(CaseBoardDefinition definition)
    {
        int count = Math.Max(1, definition.Nodes.Count);
        int columns = Math.Max(5, (int)Math.Ceiling(Math.Sqrt(count * 1.2)));
        int rows = (int)Math.Ceiling(count / (double)columns);
        double explicitMaxX = definition.Nodes.Where(n => n.X is not null).Select(n => n.X!.Value).DefaultIfEmpty(LegacyWidth - CardWidth).Max();
        double explicitMaxY = definition.Nodes.Where(n => n.Y is not null).Select(n => n.Y!.Value).DefaultIfEmpty(LegacyHeight - CardHeight).Max();
        double width = Math.Max(1900, Math.Max(explicitMaxX + CardWidth + 600, 500 + columns * 340));
        double height = Math.Max(1250, Math.Max(explicitMaxY + CardHeight + 450, 420 + rows * 250));
        return (width, height);
    }

    private Grid BuildBoardSurface()
    {
        var surface = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = boardWidth,
            Height = boardHeight,
            Background = AdventureUi.Ink("#17110E"),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };
        surface.Children.Add(new Border
        {
            Margin = new Thickness(FrameInset),
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#3E2E24"),
                (Color)ColorConverter.ConvertFromString("#2B211B"), 90),
            BorderBrush = AdventureUi.Ink("#9A7B5D"),
            BorderThickness = new Thickness(5),
            CornerRadius = new CornerRadius(3),
            IsHitTestVisible = false,
        });
        surface.Children.Add(new Border
        {
            Margin = new Thickness(FrameInset + 12),
            Background = Brushes.Transparent,
            BorderBrush = AdventureUi.Ink("#4D382C"),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
        });
        surface.Children.Add(canvas);
        return surface;
    }

    private void BuildShell()
    {
        Background = AdventureUi.Ink("#070A0B");
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border toolbar = AdventureUi.Panel("#151515", "#0D0E0F", "#51463A", new Thickness(16, 10, 16, 10), radius: 0, shadow: false);
        Grid.SetColumnSpan(toolbar, 2);
        Children.Add(toolbar);

        var toolbarBody = new StackPanel();
        toolbarBody.Children.Add(new TextBlock
        {
            Text = $"{definition.Title.ToUpperInvariant()}   ·   INVESTIGATION BOARD",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 16.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#E7D7B6"),
            Margin = new Thickness(0, 0, 370, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        var controls = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 9, 370, 0),
        };
        foreach (CaseNodeCategory category in Enum.GetValues<CaseNodeCategory>())
        {
            var check = new CheckBox
            {
                Content = CategoryLabel(category),
                IsChecked = true,
                Tag = category,
                Margin = new Thickness(0, 0, 14, 4),
                Foreground = AdventureUi.Ink("#C7B69A"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
            };
            check.Checked += (_, _) => { visibleCategories.Add((CaseNodeCategory)check.Tag); RefreshBoard(); };
            check.Unchecked += (_, _) => { visibleCategories.Remove((CaseNodeCategory)check.Tag); RefreshBoard(); };
            controls.Children.Add(check);
        }
        controls.Children.Add(new Border { Width = 1, Height = 24, Background = AdventureUi.Ink("#51463A"), Margin = new Thickness(2, 0, 8, 4) });
        controls.Children.Add(ActionButton("−", "Zoom out", () => ChangeZoom(-.12)));
        controls.Children.Add(ActionButton("+", "Zoom in", () => ChangeZoom(.12)));
        controls.Children.Add(ActionButton("FIT", "Fit known nodes", FitKnownNodes));
        controls.Children.Add(ActionButton("FRAME", "Show complete board boundary", FitBoardFrame));
        controls.Children.Add(ActionButton("RESET", "Reset zoom and pan", ResetView));
        toolbarBody.Children.Add(controls);
        toolbar.Child = toolbarBody;

        viewport.Background = AdventureUi.Ink("#0E0B09");
        viewport.BorderBrush = AdventureUi.Ink("#251B16");
        viewport.BorderThickness = new Thickness(7);
        viewport.ClipToBounds = true;
        viewport.Cursor = CasinoCursors.Hand;
        viewport.SnapsToDevicePixels = true;
        viewport.UseLayoutRounding = true;
        viewport.SizeChanged += (_, _) => ConstrainCamera();
        viewport.MouseWheel += (_, e) => { ChangeZoom(e.Delta > 0 ? .1 : -.1, e.GetPosition(viewport)); e.Handled = true; };
        viewport.MouseLeftButtonDown += OnPanStart;
        viewport.MouseMove += OnPanMove;
        viewport.MouseLeftButtonUp += OnPanEnd;

        var transforms = new TransformGroup();
        transforms.Children.Add(scale);
        transforms.Children.Add(translate);
        boardSurface.RenderTransform = transforms;
        // A world-sized Grid directly inside Border acquires a layout clip BEFORE its
        // RenderTransform. Canvas arranges it at its full desired extent at origin (0,0);
        // only the outer viewport clips the transformed world.
        var worldHost = new Canvas { Name = "CaseBoardWorldHost", Background = Brushes.Transparent, ClipToBounds = false };
        worldHost.Children.Add(boardSurface);
        Canvas.SetLeft(boardSurface, 0);
        Canvas.SetTop(boardSurface, 0);
        viewport.Name = "CaseBoardViewport";
        boardSurface.Name = "CaseBoardSurface";
        canvas.Name = "CaseBoardNodes";
        viewport.Child = worldHost;
        Grid.SetRow(viewport, 1);
        Children.Add(viewport);

        Border detailPanel = AdventureUi.Panel("#111315", "#090A0C", "#40382F", new Thickness(22), radius: 0, shadow: false);
        detailPanel.Child = new ScrollViewer
        {
            Content = details, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Grid.SetColumn(detailPanel, 1);
        Grid.SetRow(detailPanel, 1);
        Children.Add(detailPanel);
        ShowEmptyDetails();
    }

    private Button ActionButton(string text, string tooltip, Action action)
    {
        Button button = AdventureUi.Button(text, fontSize: 10, scaleOnHover: false);
        button.Padding = new Thickness(10, 6, 10, 6);
        button.Margin = new Thickness(4, 0, 0, 4);
        button.ToolTip = tooltip;
        button.Click += (_, _) => action();
        return button;
    }

    private void RefreshBoard()
    {
        ClosePopup();
        canvas.Children.Clear();
        nodeCards.Clear();
        connectionLines.Clear();
        CaseBoardState state = engine.State.CaseBoard;
        CaseNodeDefinition[] nodes = definition.Nodes
            .Where(node => state.Nodes.TryGetValue(node.Id, out CaseNodeState? nodeState) && nodeState.IsKnown)
            .Where(node => visibleCategories.Contains(node.Category))
            .ToArray();
        HashSet<string> visibleIds = nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (CaseConnectionDefinition connection in definition.Connections)
        {
            if (!state.Connections.TryGetValue(connection.Id, out CaseConnectionState? connectionState) || !connectionState.IsKnown) continue;
            if (!visibleIds.Contains(connection.FromNodeId) || !visibleIds.Contains(connection.ToNodeId)) continue;
            Point a = NodeCenter(connection.FromNodeId);
            Point b = NodeCenter(connection.ToNodeId);
            var line = new Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = ConnectionBrush(connectionState.Status),
                StrokeThickness = connectionState.Status == CaseConnectionStatus.Confirmed ? 3.2 : 2.25,
                Opacity = connectionState.Status == CaseConnectionStatus.Contradicted ? .48 : .82,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
            };
            if (connectionState.Status == CaseConnectionStatus.Uncertain) line.StrokeDashArray = new DoubleCollection { 7, 5 };
            if (connectionState.Status == CaseConnectionStatus.Contradicted) line.StrokeDashArray = new DoubleCollection { 2, 5 };
            Panel.SetZIndex(line, 2);
            connectionLines[connection.Id] = line;
            canvas.Children.Add(line);
        }

        foreach (CaseNodeDefinition node in nodes)
        {
            Border card = BuildNodeCard(node, state.Nodes[node.Id]);
            nodeCards[node.Id] = card;
            Panel.SetZIndex(card, 10);
            canvas.Children.Add(card);
        }

        if (nodes.Length == 0)
        {
            var empty = new TextBlock
            {
                Text = "No evidence pinned yet.\nThe board grows as the investigation uncovers people, places, evidence, leads and theories.",
                FontFamily = new FontFamily("Georgia"),
                FontSize = 20,
                LineHeight = 30,
                Foreground = AdventureUi.Ink("#B39A7C"),
                TextAlignment = TextAlignment.Center,
                Width = 720,
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(empty, (boardWidth - empty.Width) / 2);
            Canvas.SetTop(empty, boardHeight / 2 - 70);
            canvas.Children.Add(empty);
        }

        if (selectedNodeId is not null && visibleIds.Contains(selectedNodeId))
        {
            CaseNodeDefinition node = FindNode(selectedNodeId);
            ShowDetails(node, state.Nodes[node.Id]);
            ApplySelection();
        }
        else if (selectedNodeId is not null)
        {
            selectedNodeId = null;
            ShowEmptyDetails();
        }
    }

    private Border BuildNodeCard(CaseNodeDefinition node, CaseNodeState state)
    {
        Border card = AdventureUi.Panel(NodeSurface(node.Category), "#D8C8A8", NodeBorder(node.Category), new Thickness(16, 13, 16, 12), radius: 3, shadow: false);
        card.Width = CardWidth;
        card.Height = CardHeight;
        card.Tag = node.Id;
        card.Cursor = CasinoCursors.Hand;
        card.RenderTransformOrigin = new Point(.5, .5);
        card.RenderTransform = new RotateTransform(DeterministicTilt(node.Id));
        card.SnapsToDevicePixels = true;
        card.UseLayoutRounding = true;
        TextOptions.SetTextFormattingMode(card, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(card, TextRenderingMode.ClearType);

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = CategoryLabel(node.Category).ToUpperInvariant(),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#75463A"),
        });
        var heading = new Grid { Margin = new Thickness(0, 4, 0, 3) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        FrameworkElement icon = BuildCategoryIcon(node.Category);
        icon.Margin = new Thickness(0, 2, 9, 0);
        heading.Children.Add(icon);
        var title = new TextBlock
        {
            Text = node.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#211D18"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(title, 1);
        heading.Children.Add(title);
        content.Children.Add(heading);
        content.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Subtitle) ? Shorten(node.Description, 96) : node.Subtitle,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11.5,
            LineHeight = 16,
            Foreground = AdventureUi.Ink("#51483D"),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 34,
        });
        content.Children.Add(new TextBlock
        {
            Text = StateLabel(node, state),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.5,
            FontStyle = FontStyles.Italic,
            Foreground = AdventureUi.Ink("#765C4A"),
            Margin = new Thickness(0, 6, 0, 0),
        });
        card.Child = content;

        card.MouseLeftButtonDown += (_, e) => BeginNodeDrag(card, node, state, e);
        card.MouseMove += (_, e) => ContinueNodeDrag(card, node, e);
        card.MouseLeftButtonUp += (_, e) => EndNodeDrag(card, node, state, e);
        card.MouseRightButtonUp += (_, e) => OpenPopup(card, node, state, e);

        CaseBoardNodePosition position = ResolvePosition(node);
        Canvas.SetLeft(card, position.X);
        Canvas.SetTop(card, position.Y);
        return card;
    }

    private void BeginNodeDrag(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        ClosePopup();
        selectedNodeId = node.Id;
        ShowDetails(node, state);
        ApplySelection();
        draggedCard = card;
        draggedNodeId = node.Id;
        nodeMoved = false;
        nodeDragOrigin = e.GetPosition(canvas);
        CaseBoardNodePosition position = ResolvePosition(node);
        nodeDragStartX = position.X;
        nodeDragStartY = position.Y;
        Panel.SetZIndex(card, 1000);
        card.CaptureMouse();
        e.Handled = true;
    }

    private void ContinueNodeDrag(Border card, CaseNodeDefinition node, MouseEventArgs e)
    {
        if (!ReferenceEquals(draggedCard, card) || nodeDragOrigin is not Point origin || e.LeftButton != MouseButtonState.Pressed) return;
        Point current = e.GetPosition(canvas);
        double min = FrameInset + EdgePadding;
        double x = Math.Clamp(nodeDragStartX + current.X - origin.X, min, boardWidth - CardWidth - min);
        double y = Math.Clamp(nodeDragStartY + current.Y - origin.Y, min, boardHeight - CardHeight - min);
        nodeMoved |= Math.Abs(x - nodeDragStartX) > 1 || Math.Abs(y - nodeDragStartY) > 1;
        CaseBoardNodePosition position = ResolvePosition(node);
        position.X = x;
        position.Y = y;
        Canvas.SetLeft(card, x);
        Canvas.SetTop(card, y);
        UpdateConnectedLines(node.Id);
        e.Handled = true;
    }

    private void EndNodeDrag(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(draggedCard, card)) return;
        card.ReleaseMouseCapture();
        draggedCard = null;
        draggedNodeId = null;
        nodeDragOrigin = null;
        selectedNodeId = node.Id;
        ShowDetails(node, state);
        ApplySelection();
        if (nodeMoved) persist(engine.State);
        nodeMoved = false;
        e.Handled = true;
    }

    private void OpenPopup(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs e)
    {
        selectedNodeId = node.Id;
        ShowDetails(node, state);
        ApplySelection();
        ClosePopup();

        var stack = new StackPanel { Width = 224 };
        stack.Children.Add(new TextBlock
        {
            Text = node.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#DCC28C"),
            Margin = new Thickness(10, 7, 10, 6),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        stack.Children.Add(PopupButton("Center on this node", () => CenterOnNode(node)));
        stack.Children.Add(PopupButton("Bring to front", () => Panel.SetZIndex(card, 1200)));
        stack.Children.Add(PopupButton("Reset node position", () => ResetNodePosition(node)));
        stack.Children.Add(PopupButton("Copy case note", () => CopyNote(node)));

        nodePopup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            PlacementTarget = card,
            AllowsTransparency = true,
            StaysOpen = false,
            PopupAnimation = PopupAnimation.Fade,
            Child = new Border
            {
                Background = AdventureUi.Ink("#111516"),
                BorderBrush = AdventureUi.Ink("#8A684A"),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6),
                Child = stack,
            },
        };
        Popup popup = nodePopup;
        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(nodePopup, popup)) nodePopup = null;
        };
        popup.IsOpen = true;
        e.Handled = true;
    }

    private Button PopupButton(string text, Action action)
    {
        var button = new Button
        {
            Content = text,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Foreground = AdventureUi.Ink("#DED3C1"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 8, 10, 8),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11.5,
            Cursor = CasinoCursors.Hand,
        };
        button.MouseEnter += (_, _) => button.Background = AdventureUi.Ink("#27312D");
        button.MouseLeave += (_, _) => button.Background = Brushes.Transparent;
        button.Click += (_, _) => { ClosePopup(); action(); };
        return button;
    }

    private void ClosePopup()
    {
        Popup? popup = nodePopup;
        if (popup is null) return;
        nodePopup = null;
        popup.IsOpen = false;
    }

    private static void CopyNote(CaseNodeDefinition node)
    {
        try { Clipboard.SetText($"{node.Title}\n{node.Description}"); }
        catch { }
    }

    private void ApplySelection()
    {
        foreach ((string id, Border card) in nodeCards)
        {
            bool selected = selectedNodeId is not null && string.Equals(id, selectedNodeId, StringComparison.OrdinalIgnoreCase);
            CaseNodeDefinition node = FindNode(id);
            card.BorderBrush = AdventureUi.Ink(selected ? "#F0C86B" : NodeBorder(node.Category));
            card.BorderThickness = selected ? new Thickness(3) : new Thickness(1.2);
            Panel.SetZIndex(card, selected ? 30 : 10);
        }
    }

    private void UpdateConnectedLines(string nodeId)
    {
        foreach (CaseConnectionDefinition connection in definition.Connections)
        {
            if (!string.Equals(connection.FromNodeId, nodeId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(connection.ToNodeId, nodeId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!connectionLines.TryGetValue(connection.Id, out Line? line)) continue;
            Point a = NodeCenter(connection.FromNodeId);
            Point b = NodeCenter(connection.ToNodeId);
            line.X1 = a.X; line.Y1 = a.Y; line.X2 = b.X; line.Y2 = b.Y;
        }
    }

    private void ResetNodePosition(CaseNodeDefinition node)
    {
        CaseBoardNodePosition position = ResolvePosition(node);
        Point reset = DefaultPosition(node);
        position.X = reset.X;
        position.Y = reset.Y;
        if (nodeCards.TryGetValue(node.Id, out Border? card))
        {
            Canvas.SetLeft(card, position.X);
            Canvas.SetTop(card, position.Y);
        }
        UpdateConnectedLines(node.Id);
        persist(engine.State);
    }

    private void CenterOnNode(CaseNodeDefinition node)
    {
        Point center = NodeCenter(node.Id);
        translate.X = Math.Max(1, viewport.ActualWidth) / 2 - center.X * scale.ScaleX;
        translate.Y = Math.Max(1, viewport.ActualHeight) / 2 - center.Y * scale.ScaleY;
        SaveView();
    }

    private void ShowDetails(CaseNodeDefinition node, CaseNodeState state)
    {
        details.Children.Clear();
        var category = new StackPanel { Orientation = Orientation.Horizontal };
        FrameworkElement icon = BuildCategoryIcon(node.Category, true);
        icon.Margin = new Thickness(0, 0, 7, 0);
        category.Children.Add(icon);
        category.Children.Add(AdventureUi.Eyebrow(CategoryLabel(node.Category).ToUpperInvariant(), 10));
        details.Children.Add(category);
        details.Children.Add(new TextBlock
        {
            Text = node.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#E8D9BE"),
            Margin = new Thickness(0, 7, 0, 7),
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(node.Subtitle))
            AddPlainText(node.Subtitle!, 11, "#A98B6C", 0, 0, 0, 10);
        AddPlainText(node.Description, 13, "#CBBEAA", 0, 0, 0, 0, 20);
        AddDetail("STATUS", StateLabel(node, state));
        AddDetail("RELIABILITY", state.Reliability.ToString());
        if (!string.IsNullOrWhiteSpace(node.DiscoveryContext)) AddDetail("DISCOVERY", node.DiscoveryContext!);
        if ((node.KnownFacts ?? []).Count > 0) AddDetail("KNOWN FACTS", string.Join("\n• ", (node.KnownFacts ?? []).Prepend(""))[2..]);
        if ((node.StoryQuotes ?? []).Count > 0) AddDetail("NOTES / QUOTES", string.Join("\n\n", node.StoryQuotes ?? []));

        CaseConnectionDefinition[] connections = definition.Connections
            .Where(c => string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(c.ToNodeId, node.Id, StringComparison.OrdinalIgnoreCase))
            .Where(c => engine.State.CaseBoard.Connections.TryGetValue(c.Id, out CaseConnectionState? cs) && cs.IsKnown)
            .ToArray();
        if (connections.Length > 0)
        {
            string text = string.Join("\n", connections.Select(c =>
            {
                string otherId = string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) ? c.ToNodeId : c.FromNodeId;
                return $"{FindNode(otherId).Title} · {c.Type} · {engine.State.CaseBoard.Connections[c.Id].Status}";
            }));
            AddDetail("KNOWN CONNECTIONS", text);
        }
    }

    private void ShowEmptyDetails()
    {
        details.Children.Clear();
        details.Children.Add(AdventureUi.Eyebrow("CASE NOTES", 10));
        AddPlainText("Select a pinned item", 23, "#DCCBAF", 0, 7, 0, 8);
        AddPlainText("Drag a selected card to rearrange the investigation. Connections follow it live. Right-click a card for board actions. FIT focuses known evidence; FRAME shows the complete workspace boundary.", 12.5, "#9F9485", 0, 0, 0, 0, 20);
        AddDetail("BOARD LIMIT", $"{boardWidth:0} × {boardHeight:0} workspace units");
    }

    private void AddPlainText(string text, double size, string color, double l, double t, double r, double b, double lineHeight = double.NaN)
    {
        var block = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Georgia"),
            FontSize = size,
            Foreground = AdventureUi.Ink(color),
            Margin = new Thickness(l, t, r, b),
            TextWrapping = TextWrapping.Wrap,
        };
        if (!double.IsNaN(lineHeight)) block.LineHeight = lineHeight;
        details.Children.Add(block);
    }

    private void AddDetail(string heading, string content)
    {
        AddPlainText(heading, 9.5, "#A9805F", 0, 18, 0, 4);
        if (details.Children[^1] is TextBlock label) label.FontWeight = FontWeights.Bold;
        AddPlainText(content, 11.5, "#C5B9A8", 0, 0, 0, 0, 18);
    }

    private bool MigrateLayoutIfNeeded()
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        if (layout.LayoutVersion >= CurrentLayoutVersion) return false;
        // Preserve hand-arranged cards. V3 repairs the viewport and camera, rather than
        // stretching saved coordinates or discarding investigation progress.
        foreach (CaseNodeDefinition node in definition.Nodes)
        {
            CaseBoardNodePosition position = ResolvePosition(node);
            double min = FrameInset + EdgePadding;
            position.X = Math.Clamp(position.X, min, boardWidth - CardWidth - min);
            position.Y = Math.Clamp(position.Y, min, boardHeight - CardHeight - min);
        }
        layout.LayoutVersion = CurrentLayoutVersion;
        layout.Zoom = 1;
        layout.PanX = layout.PanY = 0;
        persist(engine.State);
        return true;
    }

    private CaseBoardNodePosition ResolvePosition(CaseNodeDefinition node)
    {
        if (engine.State.CaseBoard.Layout.NodePositions.TryGetValue(node.Id, out CaseBoardNodePosition? existing)) return existing;
        Point p = DefaultPosition(node);
        var created = new CaseBoardNodePosition { X = p.X, Y = p.Y };
        engine.State.CaseBoard.Layout.NodePositions[node.Id] = created;
        return created;
    }

    private Point DefaultPosition(CaseNodeDefinition node)
    {
        if (node.X is double definedX && node.Y is double definedY)
        {
            double min = FrameInset + EdgePadding;
            return new Point(
                Math.Clamp(definedX, min, boardWidth - CardWidth - min),
                Math.Clamp(definedY, min, boardHeight - CardHeight - min));
        }

        int index = Math.Max(0, definition.Nodes.IndexOf(node));
        int columns = Math.Max(5, (int)Math.Floor((boardWidth - 360) / 420));
        double x = FrameInset + EdgePadding + (index % columns) * 360 + ((index / columns) % 2) * 38;
        double y = FrameInset + EdgePadding + (index / columns) * 245 + (index % 2) * 20;
        double edge = FrameInset + EdgePadding;
        return new Point(Math.Clamp(x, edge, boardWidth - CardWidth - edge), Math.Clamp(y, edge, boardHeight - CardHeight - edge));
    }

    private CaseNodeDefinition FindNode(string id) => definition.Nodes.First(n => string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase));

    private Point NodeCenter(string id)
    {
        CaseBoardNodePosition p = ResolvePosition(FindNode(id));
        return new Point(p.X + CardWidth / 2, p.Y + CardHeight / 2);
    }

    private void ChangeZoom(double delta, Point? pointer = null)
    {
        ClosePopup();
        Point focus = pointer ?? new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2);
        double oldZoom = scale.ScaleX;
        double next = Math.Clamp(oldZoom + delta, .18, 3.6);
        if (Math.Abs(next - oldZoom) < .001) return;
        double worldX = (focus.X - translate.X) / oldZoom;
        double worldY = (focus.Y - translate.Y) / oldZoom;
        scale.ScaleX = scale.ScaleY = next;
        translate.X = focus.X - worldX * next;
        translate.Y = focus.Y - worldY * next;
        SaveView();
    }

    private void ResetView()
    {
        ClosePopup();
        scale.ScaleX = scale.ScaleY = 1;
        translate.X = translate.Y = 0;
        SaveView();
    }

    private void FitKnownNodes()
    {
        CaseNodeDefinition[] known = definition.Nodes
            .Where(n => engine.State.CaseBoard.Nodes.TryGetValue(n.Id, out CaseNodeState? s) && s.IsKnown)
            .Where(n => visibleCategories.Contains(n.Category))
            .ToArray();
        if (known.Length == 0) { FitBoardFrame(); return; }
        double minX = known.Min(n => ResolvePosition(n).X) - 120;
        double minY = known.Min(n => ResolvePosition(n).Y) - 120;
        double maxX = known.Max(n => ResolvePosition(n).X + CardWidth) + 120;
        double maxY = known.Max(n => ResolvePosition(n).Y + CardHeight) + 120;
        FitRectangle(minX, minY, maxX - minX, maxY - minY, 1.35);
    }

    private void FitBoardFrame() => FitRectangle(0, 0, boardWidth, boardHeight, 1);

    private void FitRectangle(double x, double y, double width, double height, double maxZoom)
    {
        ClosePopup();
        double aw = Math.Max(1, viewport.ActualWidth - 30);
        double ah = Math.Max(1, viewport.ActualHeight - 30);
        double zoom = Math.Clamp(Math.Min(aw / Math.Max(1, width), ah / Math.Max(1, height)), .18, maxZoom);
        scale.ScaleX = scale.ScaleY = zoom;
        translate.X = (aw - width * zoom) / 2 - x * zoom + 15;
        translate.Y = (ah - height * zoom) / 2 - y * zoom + 15;
        SaveView();
    }

    private void RestoreView()
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        scale.ScaleX = scale.ScaleY = Math.Clamp(layout.Zoom, .18, 3.6);
        translate.X = layout.PanX;
        translate.Y = layout.PanY;
    }

    private void SaveView()
    {
        ConstrainCamera();
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        layout.Zoom = scale.ScaleX;
        layout.PanX = translate.X;
        layout.PanY = translate.Y;
        persist(engine.State);
    }

    private void ConstrainCamera()
    {
        double width = viewport.ActualWidth - viewport.BorderThickness.Left - viewport.BorderThickness.Right;
        double height = viewport.ActualHeight - viewport.BorderThickness.Top - viewport.BorderThickness.Bottom;
        if (width <= 0 || height <= 0) return;
        double scaledWidth = boardWidth * scale.ScaleX;
        double scaledHeight = boardHeight * scale.ScaleY;
        translate.X = scaledWidth <= width ? (width - scaledWidth) / 2 : Math.Clamp(translate.X, width - scaledWidth, 0);
        translate.Y = scaledHeight <= height ? (height - scaledHeight) / 2 : Math.Clamp(translate.Y, height - scaledHeight, 0);
    }

    private void OnPanStart(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindParent<Border>(source, b => b.Tag is string) is not null) return;
        ClosePopup();
        panOrigin = e.GetPosition(this);
        panStartX = translate.X;
        panStartY = translate.Y;
        viewport.CaptureMouse();
        e.Handled = true;
    }

    private void OnPanMove(object sender, MouseEventArgs e)
    {
        if (draggedNodeId is not null || panOrigin is not Point origin || e.LeftButton != MouseButtonState.Pressed) return;
        Point current = e.GetPosition(this);
        translate.X = panStartX + current.X - origin.X;
        translate.Y = panStartY + current.Y - origin.Y;
        ConstrainCamera();
    }

    private void OnPanEnd(object sender, MouseButtonEventArgs e)
    {
        if (panOrigin is null) return;
        panOrigin = null;
        viewport.ReleaseMouseCapture();
        SaveView();
        e.Handled = true;
    }

    private string BuildStorySignature()
    {
        var sb = new StringBuilder();
        foreach (CaseNodeDefinition node in definition.Nodes.OrderBy(n => n.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (!engine.State.CaseBoard.Nodes.TryGetValue(node.Id, out CaseNodeState? s)) continue;
            sb.Append(node.Id).Append('|').Append(s.IsKnown).Append('|').Append((int)s.Reliability).Append('|')
                .Append((int)s.LeadState).Append('|').Append((int)s.TheoryState).Append('|').Append(s.LastUpdatedSceneId).Append(';');
        }
        sb.Append('#');
        foreach (CaseConnectionDefinition c in definition.Connections.OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (!engine.State.CaseBoard.Connections.TryGetValue(c.Id, out CaseConnectionState? s)) continue;
            sb.Append(c.Id).Append('|').Append(s.IsKnown).Append('|').Append((int)s.Status).Append('|').Append(s.LastUpdatedSceneId).Append(';');
        }
        return sb.ToString();
    }

    private static T? FindParent<T>(DependencyObject? start, Func<T, bool> predicate) where T : DependencyObject
    {
        DependencyObject? current = start;
        while (current is not null)
        {
            if (current is T match && predicate(match)) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static FrameworkElement BuildCategoryIcon(CaseNodeCategory category, bool light = false)
    {
        Brush brush = AdventureUi.Ink(light ? "#C6A46C" : "#643D31");
        var host = new Viewbox { Width = 17, Height = 17 };
        var c = new Canvas { Width = 20, Height = 20 };
        if (category == CaseNodeCategory.Person)
        {
            c.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = brush, Margin = new Thickness(6.5, 1.5, 0, 0) });
            c.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M3,19 C3.3,13.8 6.2,11.2 10,11.2 C13.8,11.2 16.7,13.8 17,19 Z"), Fill = brush });
        }
        else if (category == CaseNodeCategory.Location)
        {
            c.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M10,1 C5.5,1 2.5,4.2 2.5,8.2 C2.5,13.1 10,19 10,19 C10,19 17.5,13.1 17.5,8.2 C17.5,4.2 14.5,1 10,1 Z M10,5.2 A3,3 0 1 0 10,11.2 A3,3 0 1 0 10,5.2 Z"), Fill = brush });
        }
        else if (category == CaseNodeCategory.Evidence)
        {
            c.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M4,1 L13.5,1 L18,5.5 L18,19 L4,19 Z M13.5,1 L13.5,5.5 L18,5.5 M7,9 L15,9 M7,12 L15,12 M7,15 L13,15"), Stroke = brush, StrokeThickness = 1.8, Fill = Brushes.Transparent });
        }
        else if (category == CaseNodeCategory.Lead)
        {
            c.Children.Add(new Ellipse { Width = 11, Height = 11, Stroke = brush, StrokeThickness = 2, Margin = new Thickness(2, 2, 0, 0) });
            c.Children.Add(new Line { X1 = 11.5, Y1 = 11.5, X2 = 18, Y2 = 18, Stroke = brush, StrokeThickness = 2.3 });
        }
        else
        {
            c.Children.Add(new Ellipse { Width = 11, Height = 11, Stroke = brush, StrokeThickness = 1.8, Margin = new Thickness(4.5, 1, 0, 0) });
            c.Children.Add(new Line { X1 = 8, Y1 = 13, X2 = 12, Y2 = 13, Stroke = brush, StrokeThickness = 1.8 });
            c.Children.Add(new Line { X1 = 8, Y1 = 16, X2 = 12, Y2 = 16, Stroke = brush, StrokeThickness = 1.8 });
        }
        host.Child = c;
        return host;
    }

    private static Brush ConnectionBrush(CaseConnectionStatus status) => status switch
    {
        CaseConnectionStatus.Confirmed => AdventureUi.Ink("#D0A24C"),
        CaseConnectionStatus.Uncertain => AdventureUi.Ink("#A58E78"),
        CaseConnectionStatus.Contradicted => AdventureUi.Ink("#4F4640"),
        _ => AdventureUi.Ink("#9E2F2D"),
    };

    private static string NodeSurface(CaseNodeCategory category) => category switch
    {
        CaseNodeCategory.Evidence => "#E4D4B3",
        CaseNodeCategory.Lead => "#E9D8A9",
        CaseNodeCategory.Theory => "#D9C5A0",
        CaseNodeCategory.Location => "#D8CAB0",
        _ => "#E0D0B2",
    };

    private static string NodeBorder(CaseNodeCategory category) => category switch
    {
        CaseNodeCategory.Theory => "#806848",
        CaseNodeCategory.Lead => "#936B3D",
        CaseNodeCategory.Evidence => "#765946",
        CaseNodeCategory.Location => "#655A4B",
        _ => "#735247",
    };

    private static string CategoryLabel(CaseNodeCategory category) => category switch
    {
        CaseNodeCategory.Person => "People",
        CaseNodeCategory.Location => "Locations",
        CaseNodeCategory.Evidence => "Evidence",
        CaseNodeCategory.Lead => "Leads",
        CaseNodeCategory.Theory => "Theories",
        _ => category.ToString(),
    };

    private static string StateLabel(CaseNodeDefinition node, CaseNodeState state) => node.Category switch
    {
        CaseNodeCategory.Lead => state.LeadState.ToString(),
        CaseNodeCategory.Theory => state.TheoryState.ToString(),
        _ => state.Reliability == CaseReliability.Unknown ? "Known" : state.Reliability.ToString(),
    };

    private static double DeterministicTilt(string id)
    {
        int hash = 17;
        foreach (char ch in id) hash = unchecked(hash * 31 + char.ToUpperInvariant(ch));
        return (Math.Abs(hash % 9) - 4) * .32;
    }

    private static string Shorten(string value, int length) => value.Length <= length ? value : value[..Math.Max(0, length - 1)].TrimEnd() + "…";
}

internal sealed class AdventureCaseBoardWindow : Window
{
    private readonly AdventureEngine engine;
    private readonly Action<AdventureRunState> persist;
    private readonly AdventureCaseBoardView boardView;
    private readonly DispatcherTimer syncTimer;
    private readonly Button maximizeButton;

    internal AdventureCaseBoardWindow(AdventureEngine engine, Action<AdventureRunState> persist)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        if (engine.Definition.CaseBoard is null)
            throw new ArgumentException($"Adventure '{engine.Definition.Id}' does not define a Case Board.", nameof(engine));

        Title = $"{engine.Definition.Title} — {engine.Definition.CaseBoard.Title}";
        MinWidth = 900;
        MinHeight = 620;
        Width = engine.State.CaseBoard.Layout.DetachedWidth ?? 1380;
        Height = engine.State.CaseBoard.Layout.DetachedHeight ?? 860;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = AdventureUi.Ink("#080B0D");
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
        });

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Border titleBar = new()
        {
            Background = AdventureUi.Ink("#0B1515"),
            BorderBrush = AdventureUi.Ink("#3B514A"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(15, 0, 8, 0),
        };
        titleBar.MouseLeftButtonDown += OnTitleBarMouseDown;
        root.Children.Add(titleBar);

        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleGrid.Children.Add(new TextBlock
        {
            Text = $"THE MIDNIGHT CLUB   ·   {engine.Definition.Title}   ·   {engine.Definition.CaseBoard.Title}",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = AdventureUi.Ink("#E5D2AD"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        var windowButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        Button minimize = WindowButton("—", "Minimize");
        minimize.Click += (_, _) => WindowState = WindowState.Minimized;
        windowButtons.Children.Add(minimize);
        maximizeButton = WindowButton("□", "Maximize / Restore");
        maximizeButton.Click += (_, _) => ToggleMaximize();
        windowButtons.Children.Add(maximizeButton);
        Button close = WindowButton("×", "Close");
        close.Click += (_, _) => Close();
        windowButtons.Children.Add(close);
        Grid.SetColumn(windowButtons, 1);
        titleGrid.Children.Add(windowButtons);
        titleBar.Child = titleGrid;

        boardView = new AdventureCaseBoardView(engine, persist);
        Grid.SetRow(boardView, 1);
        root.Children.Add(boardView);
        Content = root;

        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        if (layout.DetachedLeft is double left && layout.DetachedTop is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        syncTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(550) };
        syncTimer.Tick += (_, _) =>
        {
            if (IsVisible && WindowState != WindowState.Minimized) boardView.RefreshFromRunState();
        };
        syncTimer.Start();

        StateChanged += (_, _) => maximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        Closing += OnClosing;
        Closed += (_, _) => syncTimer.Stop();
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (e.ClickCount == 2) { ToggleMaximize(); return; }
        if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
        DragMove();
    }

    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private static Button WindowButton(string text, string tooltip)
    {
        var button = new Button
        {
            Content = text,
            Width = 42,
            Height = 29,
            Margin = new Thickness(3, 0, 0, 0),
            Background = AdventureUi.Ink("#101C1B"),
            BorderBrush = AdventureUi.Ink("#39524C"),
            BorderThickness = new Thickness(1),
            Foreground = AdventureUi.Ink("#D7C9B2"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            Cursor = CasinoCursors.Hand,
            ToolTip = tooltip,
            Padding = new Thickness(0),
        };
        button.MouseEnter += (_, _) => button.Background = AdventureUi.Ink("#1C2C29");
        button.MouseLeave += (_, _) => button.Background = AdventureUi.Ink("#101C1B");
        return button;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        if (WindowState == WindowState.Normal)
        {
            layout.DetachedLeft = Left;
            layout.DetachedTop = Top;
            layout.DetachedWidth = Width;
            layout.DetachedHeight = Height;
        }
        persist(engine.State);
    }
}

internal sealed class AdventureCaseBoardWindowController
{
    private AdventureCaseBoardWindow? window;

    internal void Open(AdventureEngine engine, Action<AdventureRunState> persist)
    {
        if (window is { IsVisible: true })
        {
            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
            window.Activate();
            return;
        }
        window = new AdventureCaseBoardWindow(engine, persist);
        window.Closed += (_, _) => window = null;
        window.Show();
        window.Activate();
    }

    internal void Close()
    {
        window?.Close();
        window = null;
    }
}
