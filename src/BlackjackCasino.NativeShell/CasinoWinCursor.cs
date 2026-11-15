using System.Drawing;
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
    // Both Windows and Chromium now consume the same original SVG rasterization.
    // Do NOT replace these with independently drawn GDI shapes. The previous
    // WinForms painter generated visibly different/black cursor variants.
    private const string NormalPngBase64 = "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";
    private const string PressedPngBase64 = "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";

    internal static bool HasNativeArtwork =>
        !ReferenceEquals(Normal, Cursors.Arrow) &&
        !ReferenceEquals(Pressed, Cursors.Hand);
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
            // Bitmap data is shared byte-for-byte with the browser cursor.
            // Use Windows' reliable classic BGRA/DIB .cur format: PNG-compressed
            // .cur frames are not accepted by all supported Windows 10 builds.
            using var png = new MemoryStream(Convert.FromBase64String(
                down ? PressedPngBase64 : NormalPngBase64));
            using var picture = new Bitmap(png);
            if (picture.Width != Size || picture.Height != Size)
                throw new InvalidDataException("Casino cursor raster must be 32x32.");
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
