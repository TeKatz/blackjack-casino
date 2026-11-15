using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// A real owned, non-activating native window above WebView2's child HWND.
/// Unlike a WinForms sibling Panel or CSS on the departing document it remains
/// painted while WebView2 controllers are disposed and re-created. It never
/// receives game intents or touches saved data.
/// </summary>
internal sealed class NativeRouteTransitionOverlay : Form
{
    private readonly Form host;
    private readonly Control viewport;
    private readonly System.Windows.Forms.Timer frames = new() { Interval = 16 };
    private readonly Stopwatch clock = new();
    private TaskCompletionSource<bool>? covering;
    private string destination = "CASINO LOBBY";
    private double fromOpacity, toOpacity;
    private int durationMs, requestVersion;
    private bool fading, reducedMotion;
    private long frameIndex;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct ScreenPoint
    {
        public readonly int X, Y;
        public ScreenPoint(Point p) { X=p.X; Y=p.Y; }
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr WindowFromPoint(ScreenPoint point);
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    // Unlike an alpha/opacity-only regression, this verifies the actual HWND
    // receiving pointer hit tests on a real Windows desktop.
    internal bool OwnsPointerAt(Point point) =>
        Visible && !IsDisposed && IsHandleCreated &&
        WindowFromPoint(new ScreenPoint(point)) == Handle;

    private void KeepNativeCursorWhileCovering()
    {
        if (!Visible || IsDisposed || !IsHandleCreated || !host.IsHandleCreated
            || !Bounds.Contains(System.Windows.Forms.Cursor.Position)
            || GetForegroundWindow() != host.Handle)
            return;
        // The mouse can still be captured by the outgoing WebView2 child until
        // Windows has processed its input-release messages. Correct the native
        // cursor during *all* cover frames, not only on WM_SETCURSOR.
        var expected = CasinoWinCursor.ForState(
            (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left);
        if (System.Windows.Forms.Cursor.Current?.Handle != expected.Handle)
            System.Windows.Forms.Cursor.Current = expected;
    }

    // A zero-alpha layered window is omitted from Win32 hit-testing. During a
    // WebView2 HWND replacement that lets WM_SETCURSOR bounce between the old
    // browser, the native overlay and the new browser every fade frame.
    // Keep the covering window hit-testable until it is fully hidden.
    internal const double MinimumInteractiveOpacity = 0.02;
    internal static double VisibleOpacity(double value) =>
        Math.Clamp(value, MinimumInteractiveOpacity, 1);

    protected override void WndProc(ref Message m)
    {
        const int WmNcHitTest = 0x0084;
        const int WmSetCursor = 0x0020;
        const int HtClient = 1;
        if (Visible && !IsDisposed && m.Msg == WmNcHitTest)
        {
            m.Result = (IntPtr)HtClient;
            return;
        }
        if (Visible && !IsDisposed && m.Msg == WmSetCursor)
        {
            // This HWND owns the pointer for the complete transition. A
            // non-activating overlay does not get WebView2's CSS cursor updates.
            System.Windows.Forms.Cursor.Current = CasinoWinCursor.ForState(
                (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left);
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }

    internal static string LabelFor(string? route) => route switch
    {
        null => "CASINO LOBBY",
        "blackjack" => "BLACKJACK",
        "roulette" => "ROULETTE",
        "reels" => "MIDNIGHT REELS",
        "neon-vault" => "NEON VAULT",
        "nine-lives" => "NINE LIVES",
        "midnight-merge" => "MIDNIGHT MERGE",
        "midnight-dice" => "MIDNIGHT DICE",
        "midnight-manor" => "MIDNIGHT MANOR",
        "casino-empire" => "CASINO EMPIRE",
        "chill-lounge" => "CHILL LOUNGE",
        _ => "THE MIDNIGHT CLUB",
    };

    internal NativeRouteTransitionOverlay(Form host, Control viewport)
    {
        this.host = host;
        this.viewport = viewport;
        CasinoWinCursor.Install(this);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(7, 27, 24);
        ForeColor = Color.FromArgb(239, 214, 168);
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        frames.Tick += (_, _) => TickTransition();
    }

    // WS_EX_NOACTIVATE keeps focus in the casino window; WS_EX_TOOLWINDOW
    // prevents a second Alt+Tab/taskbar entry. The window itself blocks clicks
    // while its destination is loading, preventing accidental duplicate bets.
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000 | 0x00000080;
            return parameters;
        }
    }

    private void AlignToViewport()
    {
        if (!host.IsHandleCreated || !viewport.IsHandleCreated
            || host.WindowState == FormWindowState.Minimized) return;
        Rectangle bounds = viewport.RectangleToScreen(viewport.ClientRectangle);
        if (bounds.Width > 0 && bounds.Height > 0 && Bounds != bounds)
            Bounds = bounds;
    }

    /// <summary>Fade over the old, still-valid WebView before any HWND removal.</summary>
    internal Task CoverAsync(string? route, bool useReducedMotion, bool instant = false)
    {
        if (IsDisposed || !host.IsHandleCreated || host.IsDisposed)
            return Task.CompletedTask;

        ++requestVersion; // Invalidate a pending old-page reveal.
        covering?.TrySetResult(true);
        covering = null;
        reducedMotion = useReducedMotion;
        destination = LabelFor(route);
        AlignToViewport();
        if (!Visible)
        {
            // Even the first visible frame must participate in hit-testing.
            Opacity = MinimumInteractiveOpacity;
            Show(host);
            AlignToViewport();
            KeepNativeCursorWhileCovering();
        }
        if (useReducedMotion || instant)
        {
            fading = false;
            Opacity = 1;
            frames.Interval = 16;
            frames.Start();
            Invalidate();
            return Task.CompletedTask;
        }

        covering = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        StartFade(1, 185);
        return covering.Task;
    }

    private void StartFade(double target, int milliseconds)
    {
        fromOpacity = Opacity;
        toOpacity = target;
        durationMs = milliseconds;
        fading = true;
        clock.Restart();
        frames.Interval = 16;
        frames.Start();
    }

    /// <summary>
    /// The real UI-ready acknowledgement occurs just as each document's own
    /// 240 ms splash fades out. Wait until that splash finishes before revealing
    /// the game, so no second title or blank intermediate frame peeks through.
    /// </summary>
    internal void RevealAfterReady()
    {
        if (!Visible || IsDisposed) return;
        int version = ++requestVersion;
        covering?.TrySetResult(true);
        covering = null;
        _ = RevealWhenPaintedAsync(version);
    }

    private async Task RevealWhenPaintedAsync(int version)
    {
        await Task.Delay(reducedMotion ? 40 : 270);
        if (IsDisposed || !Visible || version != requestVersion) return;
        if (reducedMotion)
        {
            Dismiss();
            return;
        }
        StartFade(0, 245);
    }

    internal void Dismiss()
    {
        ++requestVersion;
        covering?.TrySetResult(true);
        covering = null;
        fading = false;
        frames.Stop();
        if (IsDisposed) return;
        // Hide before zeroing alpha so WebView2 regains cursor ownership once.
        Hide();
        Opacity = 0;
    }

    private void TickTransition()
    {
        if (IsDisposed || !Visible) { frames.Stop(); return; }
        AlignToViewport();
        KeepNativeCursorWhileCovering();
        ++frameIndex;
        if (fading)
        {
            double progress = Math.Clamp(clock.Elapsed.TotalMilliseconds / durationMs, 0, 1);
            double eased = progress * progress * (3 - 2 * progress);
            Opacity = VisibleOpacity(fromOpacity + (toOpacity - fromOpacity) * eased);
            if (progress >= 1)
            {
                fading = false;
                if (toOpacity >= 1)
                {
                    covering?.TrySetResult(true);
                    covering = null;
                }
                else
                {
                    Dismiss();
                    return;
                }
                frames.Interval = 16; // Keep cursor ownership stable while loading.
            }
        }
        // Small branded activity dots are cheap while the browser loads.
        if (frameIndex % (fading ? 2 : 3) == 0) Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        Rectangle rect = ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0) return;
        g.Clear(Color.FromArgb(7, 27, 24));

        using (var gradient = new LinearGradientBrush(rect,
            Color.FromArgb(20, 56, 44), Color.FromArgb(7, 27, 24), 90f))
            g.FillRectangle(gradient, rect);
        using (var glow = new SolidBrush(Color.FromArgb(28, 94, 73, 48)))
            g.FillEllipse(glow, rect.Width * .17f, -rect.Height * .45f,
                rect.Width * .66f, rect.Height * 1.1f);

        float middleX = rect.Width / 2f;
        float middleY = rect.Height / 2f;
        float scale = Math.Clamp(Math.Min(rect.Width / 1300f, rect.Height / 820f), .65f, 1.30f);
        float titleSize = Math.Clamp(destination.Length > 16 ? 34f : 41f, 25f, 44f) * scale;
        using Font markFont = new("Georgia", 38f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        using Font brandFont = new("Segoe UI", 12f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        using Font headingFont = new("Georgia", titleSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using SolidBrush gold = new(Color.FromArgb(235, 204, 153));
        using SolidBrush cream = new(Color.FromArgb(249, 231, 191));
        using Pen rule = new(Color.FromArgb(120, 151, 107), Math.Max(1f, scale));
        using StringFormat centered = new() { Alignment = StringAlignment.Center };

        g.DrawString("♠", markFont, gold,
            new RectangleF(0, middleY - 115f * scale, rect.Width, 58f * scale), centered);
        g.DrawString("THE MIDNIGHT CLUB", brandFont, gold,
            new RectangleF(0, middleY - 48f * scale, rect.Width, 25f * scale), centered);
        g.DrawString(destination, headingFont, cream,
            new RectangleF(12f, middleY - 9f * scale, rect.Width - 24f, 60f * scale), centered);

        float width = 175f * scale, lineY = middleY + 82f * scale;
        g.DrawLine(rule, middleX - width / 2f, lineY, middleX + width / 2f, lineY);
        for (int i = 0; i < 5; i++)
        {
            float radius = (i == (int)(frameIndex / 8 % 5) ? 4.2f : 2.25f) * scale;
            float x = middleX + (i - 2) * 28f * scale;
            using SolidBrush dot = new(i == (int)(frameIndex / 8 % 5)
                ? Color.FromArgb(255, 227, 165) : Color.FromArgb(107, 154, 134));
            g.FillEllipse(dot, x - radius, lineY + 16f * scale - radius,
                2 * radius, 2 * radius);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ++requestVersion;
            covering?.TrySetResult(true);
            covering = null;
            frames.Stop();
            frames.Dispose();
        }
        base.Dispose(disposing);
    }
}
