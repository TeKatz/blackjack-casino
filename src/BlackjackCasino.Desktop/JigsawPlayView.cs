using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BlackjackCasino.Core.Jigsaw;

namespace BlackjackCasino.Desktop;

/// <summary>One lightweight drawing surface; large puzzles never create 1,000 interactive WPF controls.</summary>
internal sealed class JigsawPlayView : UserControl, IDisposable
{
    private readonly JigsawGameState game;
    private readonly Action<JigsawGameState> save;
    private readonly Func<JigsawGameState, string> complete;
    private readonly Action back;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly JigsawBoardSurface board;
    private readonly TextBlock status = PremiumLoungeTheme.Label("", 13, PremiumLoungeTheme.Cream);
    private readonly TextBlock count = PremiumLoungeTheme.Label("", 14, PremiumLoungeTheme.Teal, true);
    private readonly TextBlock time = PremiumLoungeTheme.Label("", 12, PremiumLoungeTheme.Muted);
    private readonly TextBlock page = PremiumLoungeTheme.Label("", 12, PremiumLoungeTheme.Muted);
    private bool disposed;
    private bool completionReported;
    private int secondsSinceSave;

    public JigsawPlayView(JigsawGameState game, Action<JigsawGameState> save,
        Func<JigsawGameState, string> complete, Action back)
    {
        this.game = game;
        this.save = save;
        this.complete = complete;
        this.back = back;
        board = new JigsawBoardSurface(game, OnBoardCommitted);
        Background = LoungeUi.Ink("#081819");
        Content = Build();
        Refresh();
        timer.Tick += ClockTick;
        if (!game.Completed) timer.Start();
        Unloaded += (_, _) => Dispose();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(20, 13, 20, 14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(4, 0, 4, 13) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(PremiumLoungeTheme.Label("CHILL LOUNGE · JIGSAW PUZZLE", 11, PremiumLoungeTheme.Teal, true));
        heading.Children.Add(new TextBlock
        {
            Text = JigsawText.Motif(game.MotifId),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 28,
            Foreground = LoungeUi.Ink("#F6E8D0"),
        });
        heading.Children.Add(PremiumLoungeTheme.Label(
            game.PieceCount.ToString("N0") + JigsawText.T(" pieces · Drag from the tray and connect matching edges.", " Teile · Aus der Ablage ziehen und passende Kanten verbinden."),
            12, PremiumLoungeTheme.Muted));
        header.Children.Add(heading);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(SmallButton("− ZOOM", "JigsawZoomOut", () => board.ZoomBy(1 / 1.25)));
        actions.Children.Add(SmallButton("+ ZOOM", "JigsawZoomIn", () => board.ZoomBy(1.25)));
        actions.Children.Add(SmallButton(JigsawText.T("RESET VIEW", "ANSICHT"), "JigsawResetView", board.ResetView));
        actions.Children.Add(SmallButton(JigsawText.T("REFERENCE", "VORLAGE"), "JigsawReference", board.ToggleReference));
        actions.Children.Add(SmallButton("← PUZZLES", "JigsawBackToMenu", back));
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        root.Children.Add(header);

        var strip = new Grid { Margin = new Thickness(4, 0, 4, 10) };
        strip.ColumnDefinitions.Add(new ColumnDefinition());
        strip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var numbers = new StackPanel { Orientation = Orientation.Horizontal };
        count.Margin = new Thickness(0, 0, 22, 0);
        time.Margin = new Thickness(0, 0, 20, 0);
        numbers.Children.Add(count);
        numbers.Children.Add(time);
        numbers.Children.Add(status);
        strip.Children.Add(numbers);

        var trayActions = new StackPanel { Orientation = Orientation.Horizontal };
        trayActions.Children.Add(SmallButton("◀", "JigsawPreviousTray", () =>
        {
            board.ChangeTrayPage(-1);
            Refresh();
        }));
        page.VerticalAlignment = VerticalAlignment.Center;
        page.Margin = new Thickness(12, 0, 12, 0);
        trayActions.Children.Add(page);
        trayActions.Children.Add(SmallButton("▶", "JigsawNextTray", () =>
        {
            board.ChangeTrayPage(1);
            Refresh();
        }));
        Grid.SetColumn(trayActions, 1);
        strip.Children.Add(trayActions);
        Grid.SetRow(strip, 1);
        root.Children.Add(strip);

        var frame = new Border
        {
            Background = LoungeUi.Ink("#0D2525"),
            BorderBrush = LoungeUi.Ink("#8E744A"),
            BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Child = board,
        };
        Grid.SetRow(frame, 2);
        root.Children.Add(frame);
        return root;
    }

    private static Button SmallButton(string label, string name, Action action)
    {
        var button = LoungeUi.SmallActionButton(label);
        button.Name = name;
        button.Margin = new Thickness(0, 0, 7, 0);
        button.Click += (_, _) => action();
        return button;
    }

    private void OnBoardCommitted()
    {
        if (disposed) return;
        if (game.Completed && !completionReported)
        {
            completionReported = true;
            timer.Stop();
            status.Text = complete(game);
        }
        else if (!game.Completed) save(game);
        Refresh();
    }

    private void ClockTick(object? sender, EventArgs e)
    {
        if (disposed || game.Completed) return;
        game.ElapsedSeconds++;
        if (++secondsSinceSave >= 30)
        {
            secondsSinceSave = 0;
            save(game);
        }
        Refresh();
    }

    private void Refresh()
    {
        count.Text = game.LockedCount.ToString("N0") + " / " +
                     game.PieceCount.ToString("N0") + JigsawText.T(" placed", " gelegt");
        time.Text = TimeSpan.FromSeconds(game.ElapsedSeconds).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        page.Text = JigsawText.T("TRAY ", "ABLAGE ") + board.TrayPageNumber + " / " + board.TrayPageCount;
        if (!completionReported)
            status.Text = game.OnBoardCount.ToString("N0") + JigsawText.T(" on board · ", " auf dem Tisch · ") +
                          (game.PieceCount - game.OnBoardCount).ToString("N0") + JigsawText.T(" in tray", " in der Ablage");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        timer.Tick -= ClockTick;
        if (!game.Completed) save(game);
    }
}

internal sealed class JigsawBoardSurface : FrameworkElement
{
    private readonly JigsawGameState game;
    private readonly Action commit;
    private readonly BitmapSource image;
    private readonly int[] trayOrder;
    private readonly Dictionary<int, PieceSprite> spriteCache = [];
    private readonly Pen edges = new(LoungeUi.Ink("#F5E4CC"), 0.9);
    private readonly Pen lockedEdges = new(LoungeUi.Ink("#A6E9D5"), 0.7);
    private readonly Brush plate = LoungeUi.Ink("#102E2A");
    private readonly Brush trayFill = LoungeUi.Ink("#17332E");
    private readonly Brush highlight = LoungeUi.Ink("#D6B779");
    private int trayPage;
    private int? pendingTrayPiece;
    private int? activePiece;
    private bool draggingFromTray;
    private bool movingPiece;
    private bool panning;
    private bool showReference;
    private Point previous;
    private double zoom = 1;
    private double panX;
    private double panY;

    private sealed record PieceSprite(Geometry Shape, CroppedBitmap Image, Rect PictureRect);

    public JigsawBoardSurface(JigsawGameState game, Action commit)
    {
        this.game = game;
        this.commit = commit;
        image = JigsawImages.Get(game.MotifId);
        uint seed = BitConverter.ToUInt32(game.GameId.ToByteArray());
        trayOrder = Enumerable.Range(0, game.PieceCount).OrderBy(id => TrayKey((uint)id ^ seed)).ToArray();
        Focusable = true;
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        edges.Freeze();
        lockedEdges.Freeze();
        MouseDown += MouseDownHandler;
        MouseMove += MouseMoveHandler;
        MouseUp += MouseUpHandler;
        MouseWheel += MouseWheelHandler;
        LostMouseCapture += (_, _) => { movingPiece = false; draggingFromTray = false; panning = false; };
        KeyDown += KeyDownHandler;
        Loaded += (_, _) => Focus();
    }

    private Rect Viewport => new(22, 17, Math.Max(200, ActualWidth - 44), Math.Max(120, ActualHeight - 178));
    private Rect Tray => new(18, Math.Max(150, ActualHeight - 146), Math.Max(200, ActualWidth - 36), 132);
    private JigsawSize Size => JigsawCatalog.Size(game.PieceCount);
    private double CellW => JigsawGame.BoardWidth / Size.Columns;
    private double CellH => JigsawGame.BoardHeight / Size.Rows;
    private double Scale => Math.Min((Viewport.Width - 36) / JigsawGame.BoardWidth,
        (Viewport.Height - 36) / JigsawGame.BoardHeight) * zoom;
    private Point Origin => new(
        Viewport.X + (Viewport.Width - JigsawGame.BoardWidth * Scale) / 2 + panX,
        Viewport.Y + (Viewport.Height - JigsawGame.BoardHeight * Scale) / 2 + panY);

    private int PageSize => Math.Clamp((int)((Tray.Width - 32) / 100), 3, 16);
    private List<JigsawPieceState> Unplaced =>
        trayOrder.Where(id => !game.Pieces[id].OnBoard).Select(id => game.Pieces[id]).ToList();

    // Saved GUID is the deterministic shuffle seed. Tray order stays stable across reloads
    // while the solved coordinates remain undisclosed instead of appearing left-to-right.
    private static uint TrayKey(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            return value ^ (value >> 16);
        }
    }
    public int TrayPageCount => Math.Max(1, (Unplaced.Count + PageSize - 1) / PageSize);
    public int TrayPageNumber => Math.Min(trayPage, TrayPageCount - 1) + 1;

    public void ChangeTrayPage(int delta)
    {
        trayPage = Math.Clamp(trayPage + delta, 0, TrayPageCount - 1);
        pendingTrayPiece = null;
        InvalidateVisual();
    }

    public void ToggleReference() { showReference = !showReference; InvalidateVisual(); }
    public void ResetView() { zoom = 1; panX = 0; panY = 0; InvalidateVisual(); }

    public void ZoomBy(double multiplier)
    {
        zoom = Math.Clamp(zoom * multiplier, 0.55, 8);
        InvalidateVisual();
    }

    private Point ToWorld(Point screen)
    {
        Point offset = Origin;
        return new Point((screen.X - offset.X) / Scale, (screen.Y - offset.Y) / Scale);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(plate, null, new Rect(0, 0, ActualWidth, ActualHeight));
        Rect viewport = Viewport;
        dc.PushClip(new RectangleGeometry(viewport));
        Point origin = Origin;
        double s = Scale;
        Rect board = new(origin.X, origin.Y, JigsawGame.BoardWidth * s, JigsawGame.BoardHeight * s);
        dc.DrawRectangle(LoungeUi.Ink("#061B1B"), new Pen(LoungeUi.Ink("#A18A61"), 1.3), board);
        if (showReference)
        {
            dc.PushOpacity(.21);
            dc.DrawImage(image, board);
            dc.Pop();
        }

        foreach (JigsawPieceState piece in game.Pieces)
            if (piece.OnBoard && piece.Locked) DrawPiece(dc, piece.Id, origin.X + piece.X * s, origin.Y + piece.Y * s, s, true);
        foreach (JigsawPieceState piece in game.Pieces)
            if (piece.OnBoard && !piece.Locked && piece.Id != activePiece)
                DrawPiece(dc, piece.Id, origin.X + piece.X * s, origin.Y + piece.Y * s, s, false);
        if (activePiece is int active && game.Pieces[active].OnBoard)
        {
            JigsawPieceState selected = game.Pieces[active];
            DrawPiece(dc, active, origin.X + selected.X * s, origin.Y + selected.Y * s, s, false);
        }
        dc.Pop();

        Rect tray = Tray;
        dc.DrawRoundedRectangle(trayFill, new Pen(LoungeUi.Ink("#6E805E"), 1), tray, 9, 9);
        DrawText(dc, JigsawText.T("PIECE TRAY · Drag to board, drag back to return", "TEILEABLAGE · Aufs Spielfeld ziehen oder zurücklegen"), new Point(tray.X + 16, tray.Y + 8), 12,
            LoungeUi.Ink("#DBBB7B"));
        IReadOnlyList<JigsawPieceState> pending = CurrentPage();
        double pitch = Math.Min(102, (tray.Width - 32) / Math.Max(1, PageSize));
        for (int i = 0; i < pending.Count; i++)
        {
            JigsawPieceState piece = pending[i];
            double sx = tray.X + 17 + i * pitch;
            double sy = tray.Y + 30;
            bool selected = pendingTrayPiece == piece.Id;
            dc.DrawRoundedRectangle(LoungeUi.Ink(selected ? "#315C4A" : "#0F2625"),
                new Pen(selected ? highlight : LoungeUi.Ink("#3F6158"), selected ? 2 : .8),
                new Rect(sx, sy, pitch - 5, 88), 6, 6);
            double previewScale = Math.Min(68 / CellW, 67 / CellH);
            DrawPiece(dc, piece.Id,
                sx + (pitch - 5 - CellW * previewScale) / 2,
                sy + 7 + (66 - CellH * previewScale) / 2,
                previewScale, false);
        }
        if (pending.Count == 0)
            DrawText(dc, JigsawText.T("All pieces are on the table.", "Alle Teile liegen auf dem Tisch."), new Point(tray.X + 22, tray.Y + 66), 14,
                LoungeUi.Ink("#A9DCC7"));
    }

    private static void DrawText(DrawingContext dc, string text, Point point, double size, Brush color)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, color, 1);
        dc.DrawText(formatted, point);
    }

    private IReadOnlyList<JigsawPieceState> CurrentPage()
    {
        List<JigsawPieceState> all = Unplaced;
        trayPage = Math.Clamp(trayPage, 0, Math.Max(0, (all.Count - 1) / PageSize));
        return all.Skip(trayPage * PageSize).Take(PageSize).ToArray();
    }

    private void DrawPiece(DrawingContext dc, int id, double x, double y, double scale, bool locked)
    {
        PieceSprite sprite = Sprite(id);
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.PushClip(sprite.Shape);
        dc.DrawImage(sprite.Image, sprite.PictureRect);
        dc.Pop();
        dc.DrawGeometry(null, locked ? lockedEdges : edges, sprite.Shape);
        dc.Pop();
        dc.Pop();
    }

    private PieceSprite Sprite(int id)
    {
        if (spriteCache.TryGetValue(id, out PieceSprite? existing)) return existing;
        JigsawSize size = Size;
        int col = id % size.Columns, row = id / size.Columns;
        int x0 = Math.Max(0, (int)Math.Floor((col - .28) * image.PixelWidth / size.Columns));
        int y0 = Math.Max(0, (int)Math.Floor((row - .28) * image.PixelHeight / size.Rows));
        int x1 = Math.Min(image.PixelWidth, (int)Math.Ceiling((col + 1.28) * image.PixelWidth / size.Columns));
        int y1 = Math.Min(image.PixelHeight, (int)Math.Ceiling((row + 1.28) * image.PixelHeight / size.Rows));
        var cropped = new CroppedBitmap(image, new Int32Rect(x0, y0, x1 - x0, y1 - y0));
        cropped.Freeze();
        var dest = new Rect(x0 * JigsawGame.BoardWidth / image.PixelWidth - col * CellW,
            y0 * JigsawGame.BoardHeight / image.PixelHeight - row * CellH,
            (x1 - x0) * JigsawGame.BoardWidth / image.PixelWidth,
            (y1 - y0) * JigsawGame.BoardHeight / image.PixelHeight);
        var sprite = new PieceSprite(JigsawPieceGeometry.Create(size, id, CellW, CellH), cropped, dest);
        spriteCache[id] = sprite;
        return sprite;
    }

    private int? TrayHit(Point point)
    {
        Rect tray = Tray;
        if (point.Y < tray.Y + 30 || point.Y > tray.Y + 118) return null;
        double pitch = Math.Min(102, (tray.Width - 32) / PageSize);
        int index = (int)Math.Floor((point.X - tray.X - 17) / pitch);
        IReadOnlyList<JigsawPieceState> current = CurrentPage();
        return index >= 0 && index < current.Count ? current[index].Id : null;
    }

    private int? BoardHit(Point screen)
    {
        if (!Viewport.Contains(screen)) return null;
        Point point = ToWorld(screen);
        for (int index = game.Pieces.Count - 1; index >= 0; index--)
        {
            JigsawPieceState piece = game.Pieces[index];
            if (piece.OnBoard && !piece.Locked &&
                Sprite(piece.Id).Shape.FillContains(new Point(point.X - piece.X, point.Y - piece.Y)))
                return piece.Id;
        }
        return null;
    }

    private void MouseDownHandler(object sender, MouseButtonEventArgs e)
    {
        if (game.Completed) return;
        Point point = e.GetPosition(this);
        if (e.ChangedButton == MouseButton.Middle ||
            (e.ChangedButton == MouseButton.Left &&
             (Keyboard.IsKeyDown(Key.Space) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))))
        {
            panning = true;
            previous = point;
            CaptureMouse();
            e.Handled = true;
            return;
        }
        if (e.ChangedButton == MouseButton.Right)
        {
            int? piece = BoardHit(point);
            if (piece is int trayReturnId && JigsawGame.ReturnToTray(game, trayReturnId))
            {
                pendingTrayPiece = null;
                InvalidateVisual();
                commit();
            }
            e.Handled = true;
            return;
        }
        if (e.ChangedButton != MouseButton.Left) return;
        Focus();
        int? tray = TrayHit(point);
        if (tray is int trayId)
        {
            pendingTrayPiece = trayId;
            activePiece = null;
            draggingFromTray = true;
            previous = point;
            CaptureMouse();
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (!Viewport.Contains(point)) return;
        if (pendingTrayPiece is int pending)
        {
            BeginPlacement(pending, point);
            e.Handled = true;
            return;
        }
        int? hit = BoardHit(point);
        if (hit is int id)
        {
            activePiece = id;
            movingPiece = true;
            previous = point;
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void BeginPlacement(int id, Point screen)
    {
        Point world = ToWorld(screen);
        if (!JigsawGame.Place(game, id, world.X - CellW / 2, world.Y - CellH / 2)) return;
        activePiece = id;
        pendingTrayPiece = null;
        draggingFromTray = false;
        movingPiece = true;
        previous = screen;
        CaptureMouse();
        InvalidateVisual();
    }

    private void MouseMoveHandler(object sender, MouseEventArgs e)
    {
        Point point = e.GetPosition(this);
        if (panning)
        {
            panX += point.X - previous.X;
            panY += point.Y - previous.Y;
            previous = point;
            InvalidateVisual();
            return;
        }
        if (draggingFromTray && pendingTrayPiece is int id &&
            e.LeftButton == MouseButtonState.Pressed && Viewport.Contains(point))
        {
            BeginPlacement(id, point);
            return;
        }
        if (!movingPiece || activePiece is not int piece) return;
        double dx = (point.X - previous.X) / Scale;
        double dy = (point.Y - previous.Y) / Scale;
        if (JigsawGame.Move(game, piece, dx, dy))
        {
            previous = point;
            InvalidateVisual();
        }
    }

    private void MouseUpHandler(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Middle) return;
        if (movingPiece && activePiece is int id)
        {
            // A dropped group returns to the tray instead of vanishing off-screen.
            if (Tray.Contains(e.GetPosition(this))) JigsawGame.ReturnToTray(game, id);
            else JigsawGame.Snap(game, id);
            movingPiece = false;
            activePiece = null;
            InvalidateVisual();
            commit();
        }
        panning = false;
        draggingFromTray = false;
        ReleaseMouseCapture();
    }

    private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
    {
        Point cursor = e.GetPosition(this);
        if (!Viewport.Contains(cursor)) return;
        Point world = ToWorld(cursor);
        double oldPanX = panX, oldPanY = panY;
        zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.18 : 1 / 1.18), .55, 8);
        Point origin = Origin;
        panX = oldPanX + cursor.X - (origin.X + world.X * Scale);
        panY = oldPanY + cursor.Y - (origin.Y + world.Y * Scale);
        InvalidateVisual();
        e.Handled = true;
    }

    private void KeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            pendingTrayPiece = null;
            InvalidateVisual();
        }
        else if (e.Key == Key.PageDown) ChangeTrayPage(1);
        else if (e.Key == Key.PageUp) ChangeTrayPage(-1);
        else return;
        e.Handled = true;
    }
}

internal static class JigsawPieceGeometry
{
    public static Geometry Create(JigsawSize size, int id, double width, double height)
    {
        int row = id / size.Columns, col = id % size.Columns;
        double amplitude = Math.Min(width, height) * .22;
        var shape = new StreamGeometry();
        using (StreamGeometryContext ctx = shape.Open())
        {
            ctx.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);
            Edge(ctx, new Point(0, 0), new Point(width, 0), new Vector(0, -1),
                row == 0 ? 0 : -Sign(col, row - 1, 1), amplitude);
            Edge(ctx, new Point(width, 0), new Point(width, height), new Vector(1, 0),
                col == size.Columns - 1 ? 0 : Sign(col, row, 0), amplitude);
            Edge(ctx, new Point(width, height), new Point(0, height), new Vector(0, 1),
                row == size.Rows - 1 ? 0 : Sign(col, row, 1), amplitude);
            Edge(ctx, new Point(0, height), new Point(0, 0), new Vector(-1, 0),
                col == 0 ? 0 : -Sign(col - 1, row, 0), amplitude);
        }
        shape.Freeze();
        return shape;
    }

    private static int Sign(int col, int row, int axis)
    {
        unchecked
        {
            uint hash = (uint)(col * 73856093) ^ (uint)(row * 19349663) ^ (uint)((axis + 1) * 83492791);
            hash ^= hash >> 16;
            return (hash & 1) == 0 ? 1 : -1;
        }
    }

    private static void Edge(StreamGeometryContext ctx, Point from, Point to, Vector outward, int sign, double amplitude)
    {
        if (sign == 0) { ctx.LineTo(to, true, false); return; }
        double dx = to.X - from.X, dy = to.Y - from.Y;
        Point P(double t, double n) => new(
            from.X + dx * t + outward.X * amplitude * sign * n,
            from.Y + dy * t + outward.Y * amplitude * sign * n);
        ctx.LineTo(P(.32, 0), true, false);
        ctx.BezierTo(P(.36, 0), P(.39, -.12), P(.39, -.6), true, false);
        ctx.BezierTo(P(.39, -1.1), P(.61, -1.1), P(.61, -.6), true, false);
        ctx.BezierTo(P(.61, -.12), P(.64, 0), P(.68, 0), true, false);
        ctx.LineTo(to, true, false);
    }
}
