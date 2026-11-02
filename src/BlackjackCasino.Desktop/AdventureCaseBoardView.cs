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

/// <summary>
/// Interactive investigation board for Adventures that opt into CaseBoardDefinition.
/// Story truth remains owned by AdventureEngine; this surface only owns presentation and layout.
/// </summary>
internal sealed class AdventureCaseBoardView : Grid
{
    private const double LegacyBoardWidth = 1440;
    private const double LegacyBoardHeight = 900;
    private const double CardWidth = 280;
    private const double CardHeight = 150;
    private const double CardEdgePadding = 72;
    private const double FrameInset = 34;

    private readonly AdventureEngine engine;
    private readonly CaseBoardDefinition definition;
    private readonly Action<AdventureRunState> persist;
    private readonly double boardWidth;
    private readonly double boardHeight;
    private readonly Grid boardSurface;
    private readonly Canvas boardCanvas;
    private readonly Border viewport = new();
    private readonly StackPanel details = new();
    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform translate = new();
    private readonly HashSet<CaseNodeCategory> visibleCategories = Enum.GetValues<CaseNodeCategory>().ToHashSet();
    private readonly Dictionary<string, Border> nodeCards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Line> connectionLines = new(StringComparer.OrdinalIgnoreCase);

    private Point? panOrigin;
    private double panStartX;
    private double panStartY;
    private Border? draggedCard;
    private string? draggedNodeId;
    private Point? nodeDragOrigin;
    private double nodeDragStartX;
    private double nodeDragStartY;
    private bool nodeMoved;
    private string? selectedNodeId;
    private string lastStoryStateSignature = string.Empty;
    private Popup? nodePopup;

    internal AdventureCaseBoardView(AdventureEngine engine, Action<AdventureRunState> persist)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        definition = engine.Definition.CaseBoard
            ?? throw new ArgumentException($"Adventure '{engine.Definition.Id}' does not define a Case Board.", nameof(engine));

        (boardWidth, boardHeight) = CalculateBoardExtent(definition.Nodes.Count);
        boardCanvas = new Canvas
        {
            Width = boardWidth,
            Height = boardHeight,
            Background = Brushes.Transparent,
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };
        boardSurface = BuildBoardSurface();

        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        bool migrated = MigrateLegacyLayoutIfNeeded();
        BuildShell();
        RestoreLayout();
        RefreshBoard();
        lastStoryStateSignature = BuildStoryStateSignature();

        Loaded += (_, _) =>
        {
            if (migrated)
                Dispatcher.BeginInvoke(FitKnownNodes, DispatcherPriority.Loaded);
        };
        Unloaded += (_, _) => CloseNodePopup();
    }

    internal void RefreshFromRunState(bool force = false)
    {
        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        string signature = BuildStoryStateSignature();
        if (!force && string.Equals(signature, lastStoryStateSignature, StringComparison.Ordinal)) return;
        lastStoryStateSignature = signature;
        CloseNodePopup();
        RefreshBoard();
    }

    private static (double Width, double Height) CalculateBoardExtent(int nodeCount)
    {
        int count = Math.Max(1, nodeCount);
        int columns = Math.Max(6, (int)Math.Ceiling(Math.Sqrt(count * 1.75)));
        int rows = (int)Math.Ceiling(count / (double)columns);
        double width = Math.Max(3200, 620 + columns * 430);
        double height = Math.Max(2000, 620 + rows * 310);
        return (width, height);
    }

    private Grid BuildBoardSurface()
    {
        var surface = new Grid
        {
            Width = boardWidth,
            Height = boardHeight,
            Background = AdventureUi.Ink("#35271F"),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };

        surface.Children.Add(new Border
        {
            Margin = new Thickness(FrameInset),
            BorderBrush = AdventureUi.Ink("#8D7157"),
            BorderThickness = new Thickness(5),
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#3E2E24"),
                (Color)ColorConverter.ConvertFromString("#2B211B"),
                90),
            CornerRadius = new CornerRadius(3),
            IsHitTestVisible = false,
        });

        surface.Children.Add(new Border
        {
            Margin = new Thickness(FrameInset + 12),
            BorderBrush = AdventureUi.Ink("#4D382C"),
            BorderThickness = new Thickness(1),
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
        });

        surface.Children.Add(boardCanvas);
        return surface;
    }

    private void BuildShell()
    {
        Background = AdventureUi.Ink("#070A0B");
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 560 });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border toolbar = AdventureUi.Panel("#151515", "#0D0E0F", "#51463A", new Thickness(16, 10, 16, 10), radius: 0, shadow: false);
        Grid.SetColumnSpan(toolbar, 2);
        Children.Add(toolbar);

        var toolbarBody = new StackPanel();
        var titleRow = new Grid { Margin = new Thickness(0, 0, 370, 0) };
        titleRow.Children.Add(new TextBlock
        {
            Text = $"{definition.Title.ToUpperInvariant()}   ·   INVESTIGATION BOARD",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 16.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#E7D7B6"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        toolbarBody.Children.Add(titleRow);

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
                Margin = new Thickness(0, 0, 14, 4),
                Foreground = AdventureUi.Ink("#C7B69A"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                Tag = category,
                VerticalAlignment = VerticalAlignment.Center,
            };
            check.Checked += (_, _) =>
            {
                visibleCategories.Add((CaseNodeCategory)check.Tag);
                CloseNodePopup();
                RefreshBoard();
            };
            check.Unchecked += (_, _) =>
            {
                visibleCategories.Remove((CaseNodeCategory)check.Tag);
                CloseNodePopup();
                RefreshBoard();
            };
            controls.Children.Add(check);
        }

        controls.Children.Add(ToolbarDivider());
        Button minus = ToolbarButton("−", "Zoom out");
        minus.Click += (_, _) => ChangeZoom(-0.12);
        controls.Children.Add(minus);
        Button plus = ToolbarButton("+", "Zoom in");
        plus.Click += (_, _) => ChangeZoom(0.12);
        controls.Children.Add(plus);
        Button fit = ToolbarButton("FIT", "Fit known nodes");
        fit.Click += (_, _) => FitKnownNodes();
        controls.Children.Add(fit);
        Button frame = ToolbarButton("FRAME", "Show complete board boundary");
        frame.Click += (_, _) => FitBoardFrame();
        controls.Children.Add(frame);
        Button reset = ToolbarButton("RESET", "Reset zoom and pan");
        reset.Click += (_, _) => ResetView();
        controls.Children.Add(reset);
        toolbarBody.Children.Add(controls);
        toolbar.Child = toolbarBody;

        viewport.Background = AdventureUi.Ink("#15100D");
        viewport.BorderBrush = AdventureUi.Ink("#251B16");
        viewport.BorderThickness = new Thickness(7);
        viewport.ClipToBounds = true;
        viewport.Cursor = CasinoCursors.Hand;
        viewport.SnapsToDevicePixels = true;
        viewport.UseLayoutRounding = true;
        viewport.MouseWheel += OnMouseWheel;
        viewport.MouseLeftButtonDown += OnPanStart;
        viewport.MouseLeftButtonUp += OnPanEnd;
        viewport.MouseMove += OnPanMove;
        viewport.PreviewMouseDown += (_, _) => CloseNodePopup();

        var transform = new TransformGroup();
        transform.Children.Add(scale);
        transform.Children.Add(translate);
        boardSurface.RenderTransform = transform;
        boardSurface.RenderTransformOrigin = new Point(0, 0);
        viewport.Child = boardSurface;
        Grid.SetRow(viewport, 1);
        Children.Add(viewport);

        Border detailPanel = AdventureUi.Panel("#111315", "#090A0C", "#40382F", new Thickness(22), radius: 0, shadow: false);
        detailPanel.Child = details;
        Grid.SetColumn(detailPanel, 1);
        Grid.SetRow(detailPanel, 1);
        Children.Add(detailPanel);
        ShowEmptyDetails();
    }

    private void RefreshBoard()
    {
        boardCanvas.Children.Clear();
        nodeCards.Clear();
        connectionLines.Clear();
        CaseBoardState state = engine.State.CaseBoard;
        CaseNodeDefinition[] visibleNodes = definition.Nodes
            .Where(node => state.Nodes.TryGetValue(node.Id, out CaseNodeState? nodeState) && nodeState.IsKnown)
            .Where(node => visibleCategories.Contains(node.Category))
            .ToArray();
        HashSet<string> visibleIds = visibleNodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (CaseConnectionDefinition connection in definition.Connections)
        {
            if (!state.Connections.TryGetValue(connection.Id, out CaseConnectionState? connectionState) || !connectionState.IsKnown)
                continue;
            if (!visibleIds.Contains(connection.FromNodeId) || !visibleIds.Contains(connection.ToNodeId))
                continue;

            Point a = NodeCenter(connection.FromNodeId);
            Point b = NodeCenter(connection.ToNodeId);
            var line = new Line
            {
                X1 = a.X,
                Y1 = a.Y,
                X2 = b.X,
                Y2 = b.Y,
                Stroke = ConnectionBrush(connectionState.Status),
                StrokeThickness = connectionState.Status == CaseConnectionStatus.Confirmed ? 3.2 : 2.25,
                Opacity = connectionState.Status == CaseConnectionStatus.Contradicted ? .48 : .82,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
            };
            if (connectionState.Status == CaseConnectionStatus.Uncertain)
                line.StrokeDashArray = new DoubleCollection { 7, 5 };
            if (connectionState.Status == CaseConnectionStatus.Contradicted)
                line.StrokeDashArray = new DoubleCollection { 2, 5 };
            Panel.SetZIndex(line, 2);
            connectionLines[connection.Id] = line;
            boardCanvas.Children.Add(line);
        }

        foreach (CaseNodeDefinition node in visibleNodes)
        {
            Border card = BuildNodeCard(node, state.Nodes[node.Id]);
            nodeCards[node.Id] = card;
            Panel.SetZIndex(card, 10);
            boardCanvas.Children.Add(card);
        }

        if (visibleNodes.Length == 0)
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
            boardCanvas.Children.Add(empty);
        }

        if (selectedNodeId is not null && visibleIds.Contains(selectedNodeId))
        {
            CaseNodeDefinition selected = FindNode(selectedNodeId);
            ShowDetails(selected, state.Nodes[selected.Id]);
            ApplySelectionVisual(selectedNodeId);
        }
        else if (selectedNodeId is not null)
        {
            selectedNodeId = null;
            ShowEmptyDetails();
        }
    }

    private Border BuildNodeCard(CaseNodeDefinition node, CaseNodeState state)
    {
        Border card = AdventureUi.Panel(
            NodeSurface(node.Category),
            "#D8C8A8",
            NodeBorder(node.Category),
            new Thickness(16, 13, 16, 12),
            radius: 3,
            shadow: false);
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
        RenderOptions.SetBitmapScalingMode(card, BitmapScalingMode.HighQuality);

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
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
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1);
        heading.Children.Add(title);
        stack.Children.Add(heading);

        stack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Subtitle) ? Shorten(node.Description, 96) : node.Subtitle,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11.5,
            LineHeight = 16,
            Foreground = AdventureUi.Ink("#51483D"),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 34,
        });
        stack.Children.Add(new TextBlock
        {
            Text = StateLabel(node, state),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.5,
            FontStyle = FontStyles.Italic,
            Foreground = AdventureUi.Ink("#765C4A"),
            Margin = new Thickness(0, 6, 0, 0),
        });
        card.Child = stack;

        card.MouseLeftButtonDown += (_, args) => BeginNodeDrag(card, node, state, args);
        card.MouseMove += (_, args) => ContinueNodeDrag(card, node, args);
        card.MouseLeftButtonUp += (_, args) => EndNodeDrag(card, node, state, args);
        card.MouseRightButtonUp += (_, args) => OpenNodePopup(card, node, state, args);

        CaseBoardNodePosition position = ResolvePosition(node);
        Canvas.SetLeft(card, position.X);
        Canvas.SetTop(card, position.Y);
        return card;
    }

    private void BeginNodeDrag(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left) return;
        CloseNodePopup();
        SelectNode(node, state);
        draggedCard = card;
        draggedNodeId = node.Id;
        nodeMoved = false;
        nodeDragOrigin = args.GetPosition(boardCanvas);
        CaseBoardNodePosition position = ResolvePosition(node);
        nodeDragStartX = position.X;
        nodeDragStartY = position.Y;
        Panel.SetZIndex(card, 1000);
        card.CaptureMouse();
        args.Handled = true;
    }

    private void ContinueNodeDrag(Border card, CaseNodeDefinition node, MouseEventArgs args)
    {
        if (!ReferenceEquals(draggedCard, card) || nodeDragOrigin is not Point origin || args.LeftButton != MouseButtonState.Pressed)
            return;
        Point current = args.GetPosition(boardCanvas);
        double nextX = Math.Clamp(nodeDragStartX + current.X - origin.X, FrameInset + CardEdgePadding, boardWidth - CardWidth - FrameInset - CardEdgePadding);
        double nextY = Math.Clamp(nodeDragStartY + current.Y - origin.Y, FrameInset + CardEdgePadding, boardHeight - CardHeight - FrameInset - CardEdgePadding);
        nodeMoved |= Math.Abs(nextX - nodeDragStartX) > 1 || Math.Abs(nextY - nodeDragStartY) > 1;
        CaseBoardNodePosition position = ResolvePosition(node);
        position.X = nextX;
        position.Y = nextY;
        Canvas.SetLeft(card, nextX);
        Canvas.SetTop(card, nextY);
        UpdateConnectedLines(node.Id);
        args.Handled = true;
    }

    private void EndNodeDrag(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs args)
    {
        if (!ReferenceEquals(draggedCard, card)) return;
        card.ReleaseMouseCapture();
        Panel.SetZIndex(card, 10);
        draggedCard = null;
        draggedNodeId = null;
        nodeDragOrigin = null;
        if (nodeMoved)
        {
            persist(engine.State);
            nodeMoved = false;
        }
        else
        {
            SelectNode(node, state);
        }
        args.Handled = true;
    }

    private void OpenNodePopup(Border card, CaseNodeDefinition node, CaseNodeState state, MouseButtonEventArgs args)
    {
        SelectNode(node, state);
        CloseNodePopup();

        var body = new StackPanel { Width = 224 };
        body.Children.Add(PopupHeading(node.Title));
        body.Children.Add(PopupButton("Center on this node", () => CenterOnNode(node)));
        body.Children.Add(PopupButton("Bring to front", () => Panel.SetZIndex(card, 1200)));
        body.Children.Add(PopupButton("Reset node position", () => ResetNodePosition(node)));
        body.Children.Add(PopupButton("Copy case note", () => CopyNodeNote(node)));

        var shell = new Border
        {
            Background = AdventureUi.Ink("#111516"),
            BorderBrush = AdventureUi.Ink("#8A684A"),
            BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6),
            Child = body,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
        };

        nodePopup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            PlacementTarget = card,
            AllowsTransparency = true,
            StaysOpen = false,
            PopupAnimation = PopupAnimation.Fade,
            Child = shell,
        };
        nodePopup.Closed += (_, _) => nodePopup = null;
        nodePopup.IsOpen = true;
        args.Handled = true;
    }

    private static TextBlock PopupHeading(string title) => new()
    {
        Text = title,
        FontFamily = new FontFamily("Georgia"),
        FontSize = 11.5,
        FontWeight = FontWeights.Bold,
        Foreground = AdventureUi.Ink("#DCC28C"),
        Margin = new Thickness(10, 7, 10, 6),
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

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
        button.Click += (_, _) =>
        {
            CloseNodePopup();
            action();
        };
        return button;
    }

    private void CloseNodePopup()
    {
        if (nodePopup is null) return;
        Popup popup = nodePopup;
        nodePopup = null;
        popup.IsOpen = false;
    }

    private static void CopyNodeNote(CaseNodeDefinition node)
    {
        try
        {
            Clipboard.SetText($"{node.Title}\n{node.Description}");
        }
        catch
        {
            // Clipboard ownership is best-effort and must never interrupt the board.
        }
    }

    private void SelectNode(CaseNodeDefinition node, CaseNodeState state)
    {
        selectedNodeId = node.Id;
        ShowDetails(node, state);
        ApplySelectionVisual(node.Id);
    }

    private void ApplySelectionVisual(string? nodeId)
    {
        foreach ((string id, Border card) in nodeCards)
        {
            bool selected = nodeId is not null && string.Equals(id, nodeId, StringComparison.OrdinalIgnoreCase);
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
                !string.Equals(connection.ToNodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!connectionLines.TryGetValue(connection.Id, out Line? line)) continue;
            Point a = NodeCenter(connection.FromNodeId);
            Point b = NodeCenter(connection.ToNodeId);
            line.X1 = a.X;
            line.Y1 = a.Y;
            line.X2 = b.X;
            line.Y2 = b.Y;
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
        double viewportWidth = Math.Max(1, viewport.ActualWidth);
        double viewportHeight = Math.Max(1, viewport.ActualHeight);
        translate.X = viewportWidth / 2 - center.X * scale.ScaleX;
        translate.Y = viewportHeight / 2 - center.Y * scale.ScaleY;
        SaveViewState();
    }

    private void ShowDetails(CaseNodeDefinition node, CaseNodeState state)
    {
        details.Children.Clear();
        var category = new StackPanel { Orientation = Orientation.Horizontal };
        FrameworkElement icon = BuildCategoryIcon(node.Category, light: true);
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
        {
            details.Children.Add(new TextBlock
            {
                Text = node.Subtitle,
                FontFamily = new FontFamily("Georgia"),
                FontSize = 11,
                Foreground = AdventureUi.Ink("#A98B6C"),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        details.Children.Add(new TextBlock
        {
            Text = node.Description,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            LineHeight = 20,
            Foreground = AdventureUi.Ink("#CBBEAA"),
            TextWrapping = TextWrapping.Wrap,
        });

        AddDetailSection("STATUS", StateLabel(node, state));
        AddDetailSection("RELIABILITY", state.Reliability.ToString());
        if (!string.IsNullOrWhiteSpace(node.DiscoveryContext))
            AddDetailSection("DISCOVERY", node.DiscoveryContext!);
        if ((node.KnownFacts ?? []).Count > 0)
            AddDetailSection("KNOWN FACTS", string.Join("\n• ", (node.KnownFacts ?? []).Prepend(""))[2..]);
        if ((node.StoryQuotes ?? []).Count > 0)
            AddDetailSection("NOTES / QUOTES", string.Join("\n\n", node.StoryQuotes ?? []));

        CaseConnectionDefinition[] connections = definition.Connections
            .Where(connection => string.Equals(connection.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(connection.ToNodeId, node.Id, StringComparison.OrdinalIgnoreCase))
            .Where(connection => engine.State.CaseBoard.Connections.TryGetValue(connection.Id, out CaseConnectionState? connectionState) && connectionState.IsKnown)
            .ToArray();
        if (connections.Length > 0)
        {
            string text = string.Join("\n", connections.Select(connection =>
            {
                string otherId = string.Equals(connection.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) ? connection.ToNodeId : connection.FromNodeId;
                string other = FindNode(otherId).Title;
                CaseConnectionState connectionState = engine.State.CaseBoard.Connections[connection.Id];
                return $"{other} · {connection.Type} · {connectionState.Status}";
            }));
            AddDetailSection("KNOWN CONNECTIONS", text);
        }
    }

    private void ShowEmptyDetails()
    {
        details.Children.Clear();
        details.Children.Add(AdventureUi.Eyebrow("CASE NOTES", 10));
        details.Children.Add(new TextBlock
        {
            Text = "Select a pinned item",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 23,
            Foreground = AdventureUi.Ink("#DCCBAF"),
            Margin = new Thickness(0, 7, 0, 8),
        });
        details.Children.Add(new TextBlock
        {
            Text = "Drag a selected card to rearrange the investigation. Connections follow it live. Right-click a card for board-only actions. FIT focuses the current evidence; FRAME shows the full working-area boundary.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12.5,
            LineHeight = 20,
            Foreground = AdventureUi.Ink("#9F9485"),
            TextWrapping = TextWrapping.Wrap,
        });
        AddDetailSection("BOARD LIMIT", $"{boardWidth:0} × {boardHeight:0} workspace units");
    }

    private void AddDetailSection(string heading, string content)
    {
        details.Children.Add(new TextBlock
        {
            Text = heading,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#A9805F"),
            Margin = new Thickness(0, 18, 0, 4),
        });
        details.Children.Add(new TextBlock
        {
            Text = content,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11.5,
            LineHeight = 18,
            Foreground = AdventureUi.Ink("#C5B9A8"),
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private bool MigrateLegacyLayoutIfNeeded()
    {
        Dictionary<string, CaseBoardNodePosition> positions = engine.State.CaseBoard.Layout.NodePositions;
        if (positions.Count == 0) return false;
        double maxX = positions.Values.Max(position => position.X);
        double maxY = positions.Values.Max(position => position.Y);
        if (maxX > LegacyBoardWidth + 80 || maxY > LegacyBoardHeight + 80) return false;

        double scaleX = (boardWidth - 2 * (FrameInset + CardEdgePadding) - CardWidth) / LegacyBoardWidth;
        double scaleY = (boardHeight - 2 * (FrameInset + CardEdgePadding) - CardHeight) / LegacyBoardHeight;
        foreach (CaseBoardNodePosition position in positions.Values)
        {
            position.X = FrameInset + CardEdgePadding + position.X * scaleX;
            position.Y = FrameInset + CardEdgePadding + position.Y * scaleY;
        }
        engine.State.CaseBoard.Layout.Zoom = .72;
        engine.State.CaseBoard.Layout.PanX = 0;
        engine.State.CaseBoard.Layout.PanY = 0;
        persist(engine.State);
        return true;
    }

    private CaseBoardNodePosition ResolvePosition(CaseNodeDefinition node)
    {
        if (engine.State.CaseBoard.Layout.NodePositions.TryGetValue(node.Id, out CaseBoardNodePosition? existing))
            return existing;
        Point fallback = DefaultPosition(node);
        var created = new CaseBoardNodePosition { X = fallback.X, Y = fallback.Y };
        engine.State.CaseBoard.Layout.NodePositions[node.Id] = created;
        return created;
    }

    private Point DefaultPosition(CaseNodeDefinition node)
    {
        int index = Math.Max(0, definition.Nodes.IndexOf(node));
        int columns = Math.Max(5, (int)Math.Floor((boardWidth - 360) / 420));
        int column = index % columns;
        int row = index / columns;
        double x = FrameInset + CardEdgePadding + 100 + column * 410 + (row % 2) * 46;
        double y = FrameInset + CardEdgePadding + 90 + row * 290 + (column % 2) * 24;
        return new Point(
            Math.Clamp(x, FrameInset + CardEdgePadding, boardWidth - CardWidth - FrameInset - CardEdgePadding),
            Math.Clamp(y, FrameInset + CardEdgePadding, boardHeight - CardHeight - FrameInset - CardEdgePadding));
    }

    private CaseNodeDefinition FindNode(string id) =>
        definition.Nodes.First(node => string.Equals(node.Id, id, StringComparison.OrdinalIgnoreCase));

    private Point NodeCenter(string nodeId)
    {
        CaseBoardNodePosition position = ResolvePosition(FindNode(nodeId));
        return new Point(position.X + CardWidth / 2, position.Y + CardHeight / 2);
    }

    private void ChangeZoom(double delta)
    {
        CloseNodePopup();
        Point focus = Mouse.GetPosition(viewport);
        double oldZoom = scale.ScaleX;
        double next = Math.Clamp(oldZoom + delta, .18, 3.6);
        if (Math.Abs(next - oldZoom) < .001) return;

        double worldX = (focus.X - translate.X) / oldZoom;
        double worldY = (focus.Y - translate.Y) / oldZoom;
        scale.ScaleX = next;
        scale.ScaleY = next;
        translate.X = focus.X - worldX * next;
        translate.Y = focus.Y - worldY * next;
        SaveViewState();
    }

    private void ResetView()
    {
        CloseNodePopup();
        scale.ScaleX = 1;
        scale.ScaleY = 1;
        translate.X = 0;
        translate.Y = 0;
        SaveViewState();
    }

    private void FitKnownNodes()
    {
        CaseNodeDefinition[] known = definition.Nodes
            .Where(node => engine.State.CaseBoard.Nodes.TryGetValue(node.Id, out CaseNodeState? state) && state.IsKnown)
            .Where(node => visibleCategories.Contains(node.Category))
            .ToArray();
        if (known.Length == 0)
        {
            FitBoardFrame();
            return;
        }

        double minX = known.Min(node => ResolvePosition(node).X) - 120;
        double minY = known.Min(node => ResolvePosition(node).Y) - 120;
        double maxX = known.Max(node => ResolvePosition(node).X + CardWidth) + 120;
        double maxY = known.Max(node => ResolvePosition(node).Y + CardHeight) + 120;
        FitRectangle(minX, minY, maxX - minX, maxY - minY, maxZoom: 1.35);
    }

    private void FitBoardFrame() =>
        FitRectangle(0, 0, boardWidth, boardHeight, maxZoom: 1);

    private void FitRectangle(double x, double y, double width, double height, double maxZoom)
    {
        CloseNodePopup();
        double availableWidth = Math.Max(1, viewport.ActualWidth - 30);
        double availableHeight = Math.Max(1, viewport.ActualHeight - 30);
        double zoom = Math.Clamp(Math.Min(availableWidth / Math.Max(1, width), availableHeight / Math.Max(1, height)), .18, maxZoom);
        scale.ScaleX = zoom;
        scale.ScaleY = zoom;
        translate.X = (availableWidth - width * zoom) / 2 - x * zoom + 15;
        translate.Y = (availableHeight - height * zoom) / 2 - y * zoom + 15;
        SaveViewState();
    }

    private void RestoreLayout()
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        scale.ScaleX = Math.Clamp(layout.Zoom, .18, 3.6);
        scale.ScaleY = scale.ScaleX;
        translate.X = layout.PanX;
        translate.Y = layout.PanY;
    }

    private void SaveViewState()
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        layout.Zoom = scale.ScaleX;
        layout.PanX = translate.X;
        layout.PanY = translate.Y;
        persist(engine.State);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs args)
    {
        ChangeZoom(args.Delta > 0 ? .1 : -.1);
        args.Handled = true;
    }

    private void OnPanStart(object sender, MouseButtonEventArgs args)
    {
        if (args.OriginalSource is DependencyObject source && FindParent<Border>(source, element => element.Tag is string) is not null)
            return;
        CloseNodePopup();
        panOrigin = args.GetPosition(this);
        panStartX = translate.X;
        panStartY = translate.Y;
        viewport.CaptureMouse();
        args.Handled = true;
    }

    private void OnPanMove(object sender, MouseEventArgs args)
    {
        if (draggedNodeId is not null || panOrigin is not Point origin || args.LeftButton != MouseButtonState.Pressed) return;
        Point current = args.GetPosition(this);
        translate.X = panStartX + current.X - origin.X;
        translate.Y = panStartY + current.Y - origin.Y;
    }

    private void OnPanEnd(object sender, MouseButtonEventArgs args)
    {
        if (panOrigin is null) return;
        panOrigin = null;
        viewport.ReleaseMouseCapture();
        SaveViewState();
        args.Handled = true;
    }

    private string BuildStoryStateSignature()
    {
        var builder = new StringBuilder();
        foreach (CaseNodeDefinition node in definition.Nodes.OrderBy(node => node.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (!engine.State.CaseBoard.Nodes.TryGetValue(node.Id, out CaseNodeState? state)) continue;
            builder.Append(node.Id).Append('|').Append(state.IsKnown).Append('|')
                .Append((int)state.Reliability).Append('|').Append((int)state.LeadState).Append('|')
                .Append((int)state.TheoryState).Append('|').Append(state.LastUpdatedSceneId).Append(';');
        }
        builder.Append('#');
        foreach (CaseConnectionDefinition connection in definition.Connections.OrderBy(connection => connection.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (!engine.State.CaseBoard.Connections.TryGetValue(connection.Id, out CaseConnectionState? state)) continue;
            builder.Append(connection.Id).Append('|').Append(state.IsKnown).Append('|')
                .Append((int)state.Status).Append('|').Append(state.LastUpdatedSceneId).Append(';');
        }
        return builder.ToString();
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

    private static FrameworkElement ToolbarDivider() => new Border
    {
        Width = 1,
        Height = 24,
        Background = AdventureUi.Ink("#51463A"),
        Margin = new Thickness(2, 0, 8, 4),
    };

    private static Button ToolbarButton(string text, string tooltip)
    {
        Button button = AdventureUi.Button(text, fontSize: 10, scaleOnHover: false);
        button.Padding = new Thickness(10, 6, 10, 6);
        button.Margin = new Thickness(4, 0, 0, 4);
        button.ToolTip = tooltip;
        return button;
    }

    private static FrameworkElement BuildCategoryIcon(CaseNodeCategory category, bool light = false)
    {
        Brush brush = AdventureUi.Ink(light ? "#C6A46C" : "#643D31");
        var host = new Viewbox { Width = 17, Height = 17, Stretch = Stretch.Uniform };
        var canvas = new Canvas { Width = 20, Height = 20 };
        switch (category)
        {
            case CaseNodeCategory.Person:
                canvas.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = brush, Margin = new Thickness(6.5, 1.5, 0, 0) });
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M3,19 C3.3,13.8 6.2,11.2 10,11.2 C13.8,11.2 16.7,13.8 17,19 Z"),
                    Fill = brush,
                });
                break;
            case CaseNodeCategory.Location:
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M10,1 C5.5,1 2.5,4.2 2.5,8.2 C2.5,13.1 10,19 10,19 C10,19 17.5,13.1 17.5,8.2 C17.5,4.2 14.5,1 10,1 Z M10,5.2 A3,3 0 1 0 10,11.2 A3,3 0 1 0 10,5.2 Z"),
                    Fill = brush,
                });
                break;
            case CaseNodeCategory.Evidence:
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M4,1 L13.5,1 L18,5.5 L18,19 L4,19 Z M13.5,1 L13.5,5.5 L18,5.5 M7,9 L15,9 M7,12 L15,12 M7,15 L13,15"),
                    Stroke = brush,
                    StrokeThickness = 1.8,
                    StrokeLineJoin = PenLineJoin.Round,
                    Fill = Brushes.Transparent,
                });
                break;
            case CaseNodeCategory.Lead:
                canvas.Children.Add(new Ellipse { Width = 11, Height = 11, Stroke = brush, StrokeThickness = 2, Margin = new Thickness(2, 2, 0, 0) });
                canvas.Children.Add(new Line
                {
                    X1 = 11.5,
                    Y1 = 11.5,
                    X2 = 18,
                    Y2 = 18,
                    Stroke = brush,
                    StrokeThickness = 2.3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                });
                break;
            case CaseNodeCategory.Theory:
                canvas.Children.Add(new Ellipse { Width = 11, Height = 11, Stroke = brush, StrokeThickness = 1.8, Margin = new Thickness(4.5, 1, 0, 0) });
                canvas.Children.Add(new Line { X1 = 8, Y1 = 13, X2 = 12, Y2 = 13, Stroke = brush, StrokeThickness = 1.8 });
                canvas.Children.Add(new Line { X1 = 8, Y1 = 16, X2 = 12, Y2 = 16, Stroke = brush, StrokeThickness = 1.8 });
                canvas.Children.Add(new Line { X1 = 10, Y1 = 0, X2 = 10, Y2 = 2, Stroke = brush, StrokeThickness = 1.5 });
                break;
        }
        host.Child = canvas;
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
        foreach (char character in id) hash = unchecked(hash * 31 + char.ToUpperInvariant(character));
        return (Math.Abs(hash % 9) - 4) * .32;
    }

    private static string Shorten(string value, int length) =>
        value.Length <= length ? value : value[..Math.Max(0, length - 1)].TrimEnd() + "…";
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
        Foreground = AdventureUi.Ink("#E7D7B6");
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
        var identity = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        identity.Children.Add(new TextBlock
        {
            Text = "THE MIDNIGHT CLUB",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#62A897"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
        });
        identity.Children.Add(new TextBlock
        {
            Text = $"{engine.Definition.Title}  ·  {engine.Definition.CaseBoard.Title}",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = AdventureUi.Ink("#E5D2AD"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleGrid.Children.Add(identity);

        var windowButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
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

        StateChanged += (_, _) => maximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        Closing += OnClosing;
        Closed += (_, _) => syncTimer.Stop();

        syncTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(550),
        };
        syncTimer.Tick += (_, _) =>
        {
            if (IsVisible && WindowState != WindowState.Minimized)
                boardView.RefreshFromRunState();
        };
        syncTimer.Start();
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left) return;
        if (args.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
        DragMove();
    }

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

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

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs args)
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
