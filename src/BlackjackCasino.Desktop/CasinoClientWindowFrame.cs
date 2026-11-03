using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Lightweight client-area frame support for the main casino window. WPF WindowChrome is deliberately
/// avoided here: the title bar stays entirely in the client area while native hit testing preserves
/// resize/maximize behavior and the correct monitor bounds.
/// </summary>
internal static class CasinoClientWindowFrame
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const int WmNcCalcSize = 0x0083;
    private const int WmNcHitTest = 0x0084;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const uint MonitorDefaultToNearest = 2;
    private const double ResizeBorder = 6d;
    private const int DwmWindowAttributeBorderColor = 34;
    private const uint DwmColorNone = 0xFFFFFFFE;

    private static readonly Dictionary<Window, FrameState> States = [];

    internal static void Attach(Window window, FrameworkElement titleBar)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(titleBar);
        if (States.ContainsKey(window) || PresentationSource.FromVisual(window) is not HwndSource source)
        {
            return;
        }

        var state = new FrameState(window, titleBar, source);
        States.Add(window, state);
        window.Closed += (_, _) =>
        {
            state.Dispose();
            States.Remove(window);
        };
    }

    internal static bool IsNativeBorderSuppressed(Window window)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source)
        {
            return false;
        }

        uint color = 0;
        int result = DwmGetWindowAttribute(
            source.Handle,
            DwmWindowAttributeBorderColor,
            out color,
            Marshal.SizeOf<uint>());
        return result != 0 || color == DwmColorNone;
    }

    private sealed class FrameState : IDisposable
    {
        private readonly Window window;
        private readonly FrameworkElement titleBar;
        private readonly HwndSource source;

        internal FrameState(Window window, FrameworkElement titleBar, HwndSource source)
        {
            this.window = window;
            this.titleBar = titleBar;
            this.source = source;
            SuppressNativeBorder(source.Handle);
            source.AddHook(WndProc);
            titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            titleBar.MouseRightButtonUp += TitleBar_MouseRightButtonUp;
        }

        private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WmNcCalcSize)
            {
                // WindowStyle=None still keeps the native thick-frame metrics when ResizeMode is resizable.
                // WM_NCCALCSIZE has both TRUE and FALSE wParam forms; USER32 can use the latter during
                // activation/frame recalculation. Letting either form through can temporarily resurrect the
                // invisible non-client resize frame as bright inactive strips. The full native window is our
                // client area in both forms; WmNcHitTest below continues to provide resizing while restored.
                handled = true;
                return IntPtr.Zero;
            }

            if (message == WmNcHitTest
                && window.WindowState == WindowState.Normal
                && window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
            {
                long packed = lParam.ToInt64();
                Point point = window.PointFromScreen(new Point(
                    unchecked((short)(packed & 0xFFFF)),
                    unchecked((short)((packed >> 16) & 0xFFFF))));

                bool left = point.X >= 0 && point.X < ResizeBorder;
                bool right = point.X <= window.ActualWidth && point.X > window.ActualWidth - ResizeBorder;
                bool top = point.Y >= 0 && point.Y < ResizeBorder;
                bool bottom = point.Y <= window.ActualHeight && point.Y > window.ActualHeight - ResizeBorder;

                int hit = top && left ? HtTopLeft
                    : top && right ? HtTopRight
                    : bottom && left ? HtBottomLeft
                    : bottom && right ? HtBottomRight
                    : left ? HtLeft
                    : right ? HtRight
                    : top ? HtTop
                    : bottom ? HtBottom
                    : 0;

                if (hit != 0)
                {
                    handled = true;
                    return new IntPtr(hit);
                }
            }
            else if (message == WmGetMinMaxInfo)
            {
                ApplyMonitorBounds(hwnd, lParam, CasinoWindowFrame.GetIsFullscreen(window));
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (e.ClickCount == 2 && window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(window);
                }
                else
                {
                    SystemCommands.MaximizeWindow(window);
                }
                e.Handled = true;
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            try
            {
                window.DragMove();
                e.Handled = true;
            }
            catch (InvalidOperationException)
            {
                // The mouse can be released between the routed event and the native move loop.
            }
        }

        private void TitleBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            SystemCommands.ShowSystemMenu(window, window.PointToScreen(e.GetPosition(window)));
            e.Handled = true;
        }

        public void Dispose()
        {
            titleBar.MouseLeftButtonDown -= TitleBar_MouseLeftButtonDown;
            titleBar.MouseRightButtonUp -= TitleBar_MouseRightButtonUp;
            source.RemoveHook(WndProc);
        }
    }

    private static void SuppressNativeBorder(IntPtr hwnd)
    {
        uint color = DwmColorNone;
        _ = DwmSetWindowAttribute(
            hwnd,
            DwmWindowAttributeBorderColor,
            ref color,
            Marshal.SizeOf<uint>());
    }

    private static void ApplyMonitorBounds(IntPtr hwnd, IntPtr lParam, bool fullscreen)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        NativeRect target = fullscreen ? monitorInfo.Monitor : monitorInfo.Work;
        MinMaxInfo minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        minMax.MaxPosition.X = target.Left - monitorInfo.Monitor.Left;
        minMax.MaxPosition.Y = target.Top - monitorInfo.Monitor.Top;
        minMax.MaxSize.X = target.Right - target.Left;
        minMax.MaxSize.Y = target.Bottom - target.Top;
        Marshal.StructureToPtr(minMax, lParam, fDeleteOld: false);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref uint attributeValue,
        int attributeSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        out uint attributeValue,
        int attributeSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }
}
