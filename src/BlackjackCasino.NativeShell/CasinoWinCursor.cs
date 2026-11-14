using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Pure presentation: one gold/emerald 32px cursor pair for the native forms.
/// The WebView2 child HWND uses the matching SVG pair from ModernFrontend.
/// No OS-wide cursor replacement, persistent setting or input interception.
/// </summary>
internal static class CasinoWinCursor
{
    private const int Size = 32;
    private static readonly Lazy<Cursor> IdleCursor = new(() => Create(false));
    private static readonly Lazy<Cursor> DownCursor = new(() => Create(true));

    private static Cursor Normal => IdleCursor.Value;
    private static Cursor Pressed => DownCursor.Value;

    // Expose the exact cached instance to the native transition HWND.
    // Creating a fresh HCURSOR during each WM_SETCURSOR is expensive and
    // sometimes causes a transient black/standard cursor in WebView2 fades.
    internal static Cursor ForState(bool pressed) => pressed ? Pressed : Normal;

    internal static void Install(Form host)
    {
        host.Cursor = Normal;
        Attach(host, host);
        host.Deactivate += (_, _) => host.Cursor = Normal;
    }

    private static void Attach(Form host, Control node)
    {
        node.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) host.Cursor = Pressed;
        };
        node.MouseUp += (_, _) => host.Cursor = Normal;
        node.MouseCaptureChanged += (_, _) =>
        {
            if (Control.MouseButtons == MouseButtons.None) host.Cursor = Normal;
        };
        node.ControlAdded += (_, e) => { if (e.Control is { } child) Attach(host, child); };
        foreach (Control child in node.Controls) Attach(host, child);
    }

    private static Cursor Create(bool down)
    {
        try
        {
            using var picture = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
            using (Graphics painter = Graphics.FromImage(picture))
            {
                painter.SmoothingMode = SmoothingMode.AntiAlias;
                painter.CompositingMode = CompositingMode.SourceOver;
                painter.TranslateTransform(down ? .7f : 0, down ? .5f : 0);
                using var shape = new GraphicsPath();
                shape.AddLines(new[]
                {
                    new PointF(3.5f, 2.5f), new PointF(4.6f, 25.2f),
                    new PointF(10.6f, 19.7f), new PointF(15.4f, 29),
                    new PointF(20.2f, 26.5f), new PointF(15.4f, 17.1f),
                    new PointF(25.5f, 14.4f),
                });
                shape.CloseFigure();
                using var metallic = new LinearGradientBrush(
                    new Point(3, 2), new Point(24, 29),
                    down ? Color.FromArgb(255, 249, 218) : Color.FromArgb(244, 220, 159),
                    down ? Color.FromArgb(212, 154, 71) : Color.FromArgb(157, 104, 43));
                using var shadow = new Pen(Color.FromArgb(2, 10, 9), 3f) { LineJoin = LineJoin.Round };
                using var trim = new Pen(down ? Color.FromArgb(255, 238, 183) :
                    Color.FromArgb(207, 163, 86), 1.4f) { LineJoin = LineJoin.Round };
                painter.DrawPath(shadow, shape);
                painter.FillPath(metallic, shape);
                painter.DrawPath(trim, shape);
                using var enamel = new SolidBrush(down ? Color.FromArgb(25, 100, 79) :
                    Color.FromArgb(11, 59, 49));
                painter.FillPolygon(enamel, new[]
                {
                    new PointF(6.2f, 7.8f), new PointF(7, 19.4f),
                    new PointF(11.4f, 15.6f), new PointF(17.5f, 14.2f),
                });
                using var highlight = new Pen(Color.FromArgb(255, 248, 221), 1.1f);
                painter.DrawLine(highlight, 5.2f, 4.4f, 6.3f, 14.6f);
            }
            // Standard Windows .cur with a 32-bit color DIB + AND transparency mask.
            // No P/Invoke, no process-wide cursor side effects.
            using var data = new MemoryStream();
            using (var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true))
            {
                const int xorBytes = Size * Size * 4;
                const int andBytes = Size * 4; // 1bpp, aligned to 32-bit rows
                const int imageBytes = 40 + xorBytes + andBytes;
                writer.Write((ushort)0); writer.Write((ushort)2); writer.Write((ushort)1);
                writer.Write((byte)Size); writer.Write((byte)Size);
                writer.Write((byte)0); writer.Write((byte)0);
                writer.Write((ushort)4); writer.Write((ushort)2);
                writer.Write((uint)imageBytes); writer.Write((uint)22);
                writer.Write((uint)40);
                writer.Write(Size); writer.Write(Size * 2);
                writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(0u); writer.Write((uint)(xorBytes + andBytes));
                writer.Write(0); writer.Write(0); writer.Write(0u); writer.Write(0u);
                for (int y = Size - 1; y >= 0; --y)
                    for (int x = 0; x < Size; ++x)
                    {
                        Color pixel = picture.GetPixel(x, y);
                        writer.Write(pixel.B); writer.Write(pixel.G);
                        writer.Write(pixel.R); writer.Write(pixel.A);
                    }
                for (int y = Size - 1; y >= 0; --y)
                    for (int byteIndex = 0; byteIndex < 4; ++byteIndex)
                    {
                        int mask = 0;
                        for (int bit = 0; bit < 8; ++bit)
                            if (picture.GetPixel(byteIndex * 8 + bit, y).A < 16)
                                mask |= 1 << (7 - bit);
                        writer.Write((byte)mask);
                    }
            }
            data.Position = 0;
            return new Cursor(data);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or System.Runtime.InteropServices.ExternalException)
        {
            return down ? Cursors.Hand : Cursors.Arrow;
        }
    }
}
