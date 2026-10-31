using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BlackjackCasino.Desktop;

/// <summary>Small embedded cursor set so the casino keeps its own visual language without shipping loose cursor files.</summary>
internal static class CasinoCursors
{
    private const string ArrowCursorBase64 = "AAACAAEAICAAAAMAAgA9AQAAFgAAAIlQTkcNChoKAAAADUlIRFIAAAAgAAAAIAgGAAAAc3p69AAAAQRJREFUeJzt1jESgyAQBdBPJjNWNl7AM3kGz+UZnOQs6VJZpqGhCZUpMpshCgi4YOO2yuzzAzsCZxlVNfVcuuflaMQKQIhSECtgGvsf5BAAALyftyKIq+8hJdF2wwwAWirBDXAmYIPkSCMIQIhp7NkRwQATwnlLogGEAHi2JAlACI4tSQaYkD1bshtAiNQ0WAAmJBbhHUS2arth8x1ChAyupGsYWiFpRCdgFsdodibwetwBfCNfxl5sDoTsdzYANddSCS2VcGGyDCKzuWthzGGMBtia+1JgB4Sebo7DuAJsNedO4W8ObDXXUgn62iWkauo5ZS5EL/DFXQSwROT4UY0CnLW3Plcme3ah3xlBAAAAAElFTkSuQmCC";
    private const string HandCursorBase64 = "AAACAAEAICAAAA4ABAA4AQAAFgAAAIlQTkcNChoKAAAADUlIRFIAAAAgAAAAIAgGAAAAc3p69AAAAP9JREFUeJztlbsRgzAMhk0aaGhYIBXzcJkhc2WGHPNQZQEaGqhIpZxOka0HDk38V7pDj8+SbEIocqru2r3u2v1onou3OGefAgAF12kM6zQehnB1AApT+zSAnCoA2QC819IEoClgBVEDaJO+nneTfxTA004ojiGkPCwABHnner09vkDUANjZ2k4s2g01wDYvlZSg6QfW5gTdiB2CHcE2LxUEAgQFavrhUzx2Ss0ooksIEBgEA3E2J2kUyWsI44hB0KScnyTxHUhBcLJCqB4igEjJ242sPyNpH34O4JEZINVa/I3uTixOnC2W9kWE4pw/3SdTBzTLiH2ovya+qOj/9Ab9Qom0usVhkgAAAABJRU5ErkJggg==";

    private static readonly MemoryStream ArrowStream = new(Convert.FromBase64String(ArrowCursorBase64), writable: false);
    private static readonly MemoryStream HandStream = new(Convert.FromBase64String(HandCursorBase64), writable: false);

    internal static Cursor Arrow { get; } = new(ArrowStream);
    internal static Cursor Hand { get; } = new(HandStream);

    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;

        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => ((Window)sender).Cursor = Arrow), handledEventsToo: true);

        EventManager.RegisterClassHandler(typeof(ButtonBase), Mouse.MouseEnterEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is ButtonBase { IsEnabled: true } button) button.Cursor = Hand;
            }), handledEventsToo: true);
        EventManager.RegisterClassHandler(typeof(ButtonBase), Mouse.MouseLeaveEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is ButtonBase button) button.Cursor = Arrow;
            }), handledEventsToo: true);

        EventManager.RegisterClassHandler(typeof(ComboBox), Mouse.MouseEnterEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is ComboBox { IsEnabled: true } combo) combo.Cursor = Hand;
            }), handledEventsToo: true);
        EventManager.RegisterClassHandler(typeof(ComboBox), Mouse.MouseLeaveEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is ComboBox combo) combo.Cursor = Arrow;
            }), handledEventsToo: true);

        // Text editing should keep the conventional I-beam even though the window itself inherits the casino arrow.
        EventManager.RegisterClassHandler(typeof(TextBoxBase), Mouse.MouseEnterEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is TextBoxBase textBox) textBox.Cursor = Cursors.IBeam;
            }), handledEventsToo: true);
        EventManager.RegisterClassHandler(typeof(TextBoxBase), Mouse.MouseLeaveEvent,
            new MouseEventHandler((sender, _) =>
            {
                if (sender is TextBoxBase textBox) textBox.Cursor = Arrow;
            }), handledEventsToo: true);
    }
}
