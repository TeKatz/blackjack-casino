using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Generic investigation-board surface for Adventures that opt into a CaseBoardDefinition.
/// The visual treatment is intentionally campaign-neutral at the API boundary while using a noir-style
/// corkboard presentation as the first skin. It reads and writes the same CaseBoardState that lives on
/// AdventureRunState, so embedded and detached presentations stay synchronized by construction.
/// </summary>
internal sealed class AdventureCaseBoardView : Grid
{
    private const double BoardWidth = 1440;
    private const double BoardHeight = 900;
    private const double CardWidth = 210;
    private const double CardHeight = 116;

    private readonly AdventureEngine engine;
    private readonly CaseBoardDefinition definition;
    private readonly Action<AdventureRunState> persist;
    private readonly Canvas boardCanvas = new() { Width = BoardWidth, Height = BoardHeight };
    private readonly Border viewport = new();
    private readonly StackPanel details = new();
    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform translate = new();
    private readonly HashSet<CaseNodeCategory> visibleCategories = Enum.GetValues<CaseNodeCategory>().ToHashSet();
    private Point? dragOrigin;
    private double dragStartX;
    private double dragStartY;
    private string? selectedNodeId;

    internal AdventureCaseBoardView(
        AdventureEngine engine,
        Action<AdventureRunState> persist)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        definition = engine.Definition.CaseBoard
            ?? throw new ArgumentException($"Adventure '{engine.Definition.Id}' does not define a Case Board.", nameof(engine));

        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        BuildShell();
        RestoreLayout();
        RefreshBoard();
    }

    internal void RefreshFromRunState()
    {
        engine.State.CaseBoard = AdventureCaseBoard.Initialize(definition, engine.State.CaseBoard);
        RestoreLayout();
        RefreshBoard();
    }

    private void BuildShell()
    {
        Background = AdventureUi.Ink("#080B0D");
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 560 });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border toolbar = AdventureUi.Panel("#151515", "#0D0E0F", "#51463A", new Thickness(16, 12, 16, 12), radius: 0, shadow: false);
        Grid.SetColumnSpan(toolbar, 2);
        Children.Add(toolbar);

        var toolbarGrid = new Grid();
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock
        {
            Text = definition.Title.ToUpperInvariant(),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#E7D7B6"),
        });
        title.Children.Add(new TextBlock
        {
            Text = "  ·  INVESTIGATION BOARD",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            Foreground = AdventureUi.Ink("#9B8062"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        toolbarGrid.Children.Add(title);

        var filters = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (CaseNodeCategory category in Enum.GetValues<CaseNodeCategory>())
        {
            var check = new CheckBox
            {
                Content = CategoryLabel(category),
                IsChecked = true,
                Margin = new Thickness(0, 0, 13, 0),
                Foreground = AdventureUi.Ink("#C7B69A"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10.5,
                Tag = category,
                VerticalAlignment = VerticalAlignment.Center,
            };
            check.Checked += (_, _) =>
            {
                visibleCategories.Add((CaseNodeCategory)check.Tag);
                RefreshBoard();
            };
            check.Unchecked += (_, _) =>
            {
                visibleCategories.Remove((CaseNodeCategory)check.Tag);
                RefreshBoard();
            };
            filters.Children.Add(check);
        }
        Grid.SetColumn(filters, 1);
        toolbarGrid.Children.Add(filters);

        var viewActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
        Button minus = ToolbarButton("−");
        minus.ToolTip = "Zoom out";
        minus.Click += (_, _) => ChangeZoom(-0.12);
        viewActions.Children.Add(minus);
        Button plus = ToolbarButton("+");
        plus.ToolTip = "Zoom in";
        plus.Click += (_, _) => ChangeZoom(0.12);
        viewActions.Children.Add(plus);
        Button fit = ToolbarButton("FIT");
        fit.ToolTip = "Fit the whole Case Board";
        fit.Click += (_, _) => FitBoard();
        viewActions.Children.Add(fit);
        Button reset = ToolbarButton("RESET");
        reset.ToolTip = "Reset zoom and pan";
        reset.Click += (_, _) => ResetView();
        viewActions.Children.Add(reset);
        Grid.SetColumn(viewActions, 2);
        toolbarGrid.Children.Add(viewActions);
        toolbar.Child = toolbarGrid;

        viewport.Background = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#3B2E24"),
            (Color)ColorConverter.ConvertFromString("#261E19"),
            90);
        viewport.BorderBrush = AdventureUi.Ink("#15100D");
        viewport.BorderThickness = new Thickness(8);
        viewport.ClipToBounds = true;
        viewport.Cursor = CasinoCursors.Hand;
        viewport.MouseWheel += OnMouseWheel;
        viewport.MouseLeftButtonDown += OnPanStart;
        viewport.MouseLeftButtonUp += OnPanEnd;
        viewport.MouseMove += OnPanMove;

        var transform = new TransformGroup();
        transform.Children.Add(scale);
        transform.Children.Add(translate);
        boardCanvas.RenderTransform = transform;
        boardCanvas.RenderTransformOrigin = new Point(0, 0);
        viewport.Child = boardCanvas;
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
        CaseBoardState state = engine.State.CaseBoard;
        var visibleNodes = definition.Nodes
            .Where(node => state.Nodes.TryGetValue(node.Id, out CaseNodeState? nodeState) && nodeState.IsKnown)
            .Where(node => visibleCategories.Contains(node.Category))
            .ToArray();
        var visibleIds = visibleNodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (CaseConnectionDefinition connection in definition.Connections ?? [])
        {
            if (!state.Connections.TryGetValue(connection.Id, out CaseConnectionState? connectionState) || !connectionState.IsKnown)
                continue;
            if (!visibleIds.Contains(connection.FromNodeId) || !visibleIds.Contains(connection.ToNodeId))
                continue;

            CaseNodeDefinition from = definition.Nodes.First(node => string.Equals(node.Id, connection.FromNodeId, StringComparison.OrdinalIgnoreCase));
            CaseNodeDefinition to = definition.Nodes.First(node => string.Equals(node.Id, connection.ToNodeId, StringComparison.OrdinalIgnoreCase));
            Point a = NodeCenter(from);
            Point b = NodeCenter(to);
            var line = new Line
            {
                X1 = a.X,
                Y1 = a.Y,
                X2 = b.X,
                Y2 = b.Y,
                Stroke = ConnectionBrush(connectionState.Status),
                StrokeThickness = connectionState.Status == CaseConnectionStatus.Confirmed ? 3.3 : 2.4,
                Opacity = connectionState.Status == CaseConnectionStatus.Contradicted ? .52 : .86,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = .35, Color = Colors.Black },
            };
            if (connectionState.Status == CaseConnectionStatus.Uncertain)
                line.StrokeDashArray = new DoubleCollection { 6, 4 };
            if (connectionState.Status == CaseConnectionStatus.Contradicted)
                line.StrokeDashArray = new DoubleCollection { 2, 4 };
            boardCanvas.Children.Add(line);
        }

        foreach (CaseNodeDefinition node in visibleNodes)
            boardCanvas.Children.Add(BuildNodeCard(node, state.Nodes[node.Id]));

        if (visibleNodes.Length == 0)
        {
            var empty = new TextBlock
            {
                Text = "No evidence pinned yet.\nThe board will grow as the investigation uncovers people, places, evidence, leads and theories.",
                FontFamily = new FontFamily("Georgia"),
                FontSize = 18,
                LineHeight = 28,
                Foreground = AdventureUi.Ink("#B39A7C"),
                TextAlignment = TextAlignment.Center,
                Width = 620,
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(empty, (BoardWidth - empty.Width) / 2);
            Canvas.SetTop(empty, 380);
            boardCanvas.Children.Add(empty);
        }

        if (selectedNodeId is not null && visibleIds.Contains(selectedNodeId))
        {
            CaseNodeDefinition selected = definition.Nodes.First(node => string.Equals(node.Id, selectedNodeId, StringComparison.OrdinalIgnoreCase));
            ShowDetails(selected, state.Nodes[selected.Id]);
        }
        else if (selectedNodeId is not null)
        {
            selectedNodeId = null;
            ShowEmptyDetails();
        }
    }

    private FrameworkElement BuildNodeCard(CaseNodeDefinition node, CaseNodeState state)
    {
        Border card = AdventureUi.Panel(
            NodeSurface(node.Category),
            "#D2C3A5",
            NodeBorder(node.Category),
            new Thickness(13, 11, 13, 11),
            radius: 2,
            shadow: true);
        card.Width = CardWidth;
        card.Height = CardHeight;
        card.Tag = node.Id;
        card.Cursor = CasinoCursors.Hand;
        card.RenderTransformOrigin = new Point(.5, .5);
        card.RenderTransform = new RotateTransform(DeterministicTilt(node.Id));

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = CategoryLabel(node.Category).ToUpperInvariant(),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#6A332F"),
        });
        stack.Children.Add(new TextBlock
        {
            Text = node.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = AdventureUi.Ink("#231F1A"),
            Margin = new Thickness(0, 4, 0, 2),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        stack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Subtitle) ? Shorten(node.Description, 84) : node.Subtitle,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            LineHeight = 14,
            Foreground = AdventureUi.Ink("#51483D"),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 31,
        });
        stack.Children.Add(new TextBlock
        {
            Text = StateLabel(node, state),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 8.5,
            FontStyle = FontStyles.Italic,
            Foreground = AdventureUi.Ink("#765C4A"),
            Margin = new Thickness(0, 5, 0, 0),
        });
        card.Child = stack;

        card.MouseLeftButtonUp += (_, args) =>
        {
            if (dragOrigin is not null) return;
            selectedNodeId = node.Id;
            ShowDetails(node, state);
            args.Handled = true;
        };

        CaseBoardNodePosition position = ResolvePosition(node);
        Canvas.SetLeft(card, position.X);
        Canvas.SetTop(card, position.Y);
        return card;
    }

    private void ShowDetails(CaseNodeDefinition node, CaseNodeState state)
    {
        details.Children.Clear();
        details.Children.Add(AdventureUi.Eyebrow(CategoryLabel(node.Category).ToUpperInvariant(), 10));
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

        var connections = (definition.Connections ?? [])
            .Where(connection => string.Equals(connection.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(connection.ToNodeId, node.Id, StringComparison.OrdinalIgnoreCase))
            .Where(connection => engine.State.CaseBoard.Connections.TryGetValue(connection.Id, out CaseConnectionState? connectionState) && connectionState.IsKnown)
            .ToArray();
        if (connections.Length > 0)
        {
            string text = string.Join("\n", connections.Select(connection =>
            {
                string otherId = string.Equals(connection.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) ? connection.ToNodeId : connection.FromNodeId;
                string other = definition.Nodes.First(candidate => string.Equals(candidate.Id, otherId, StringComparison.OrdinalIgnoreCase)).Title;
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
            Text = "People, locations, evidence, unresolved leads and working theories can all carry their own reliability and story context. Connections on the board are semantic state, not decoration.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 12.5,
            LineHeight = 20,
            Foreground = AdventureUi.Ink("#9F9485"),
            TextWrapping = TextWrapping.Wrap,
        });
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

    private CaseBoardNodePosition ResolvePosition(CaseNodeDefinition node)
    {
        if (engine.State.CaseBoard.Layout.NodePositions.TryGetValue(node.Id, out CaseBoardNodePosition? existing))
            return existing;

        int index = definition.Nodes.IndexOf(node);
        int column = index % 5;
        int row = index / 5;
        var created = new CaseBoardNodePosition
        {
            X = 70 + column * 265 + (row % 2) * 28,
            Y = 70 + row * 180 + (column % 2) * 17,
        };
        engine.State.CaseBoard.Layout.NodePositions[node.Id] = created;
        return created;
    }

    private Point NodeCenter(CaseNodeDefinition node)
    {
        CaseBoardNodePosition position = ResolvePosition(node);
        return new Point(position.X + CardWidth / 2, position.Y + CardHeight / 2);
    }

    private void ChangeZoom(double delta)
    {
        double next = Math.Clamp(scale.ScaleX + delta, .2, 4);
        scale.ScaleX = next;
        scale.ScaleY = next;
        SaveViewState();
    }

    private void ResetView()
    {
        scale.ScaleX = 1;
        scale.ScaleY = 1;
        translate.X = 0;
        translate.Y = 0;
        SaveViewState();
    }

    private void FitBoard()
    {
        double width = Math.Max(1, viewport.ActualWidth - 24);
        double height = Math.Max(1, viewport.ActualHeight - 24);
        double zoom = Math.Clamp(Math.Min(width / BoardWidth, height / BoardHeight), .2, 1.2);
        scale.ScaleX = zoom;
        scale.ScaleY = zoom;
        translate.X = Math.Max(0, (width - BoardWidth * zoom) / 2);
        translate.Y = Math.Max(0, (height - BoardHeight * zoom) / 2);
        SaveViewState();
    }

    private void RestoreLayout()
    {
        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        scale.ScaleX = layout.Zoom;
        scale.ScaleY = layout.Zoom;
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
        dragOrigin = args.GetPosition(this);
        dragStartX = translate.X;
        dragStartY = translate.Y;
        viewport.CaptureMouse();
        args.Handled = true;
    }

    private void OnPanMove(object sender, MouseEventArgs args)
    {
        if (dragOrigin is not Point origin || args.LeftButton != MouseButtonState.Pressed) return;
        Point current = args.GetPosition(this);
        translate.X = dragStartX + current.X - origin.X;
        translate.Y = dragStartY + current.Y - origin.Y;
    }

    private void OnPanEnd(object sender, MouseButtonEventArgs args)
    {
        if (dragOrigin is null) return;
        dragOrigin = null;
        viewport.ReleaseMouseCapture();
        SaveViewState();
        args.Handled = true;
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

    private static Button ToolbarButton(string text)
    {
        Button button = AdventureUi.Button(text, fontSize: 10, scaleOnHover: false);
        button.Padding = new Thickness(10, 6, 10, 6);
        button.Margin = new Thickness(4, 0, 0, 0);
        return button;
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
        return (Math.Abs(hash % 9) - 4) * .42;
    }

    private static string Shorten(string value, int length) =>
        value.Length <= length ? value : value[..Math.Max(0, length - 1)].TrimEnd() + "…";
}

/// <summary>
/// Owns the single detached board window for one active Adventure run. Closing the window only closes the
/// presentation; the AdventureRunState remains untouched and can immediately be reopened on any monitor.
/// </summary>
internal sealed class AdventureCaseBoardWindow : Window
{
    private readonly AdventureEngine engine;
    private readonly Action<AdventureRunState> persist;

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
        Content = new AdventureCaseBoardView(engine, persist);

        BoardLayoutState layout = engine.State.CaseBoard.Layout;
        if (layout.DetachedLeft is double left && layout.DetachedTop is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        Closing += OnClosing;
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
