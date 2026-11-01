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
    // Diagonal pointing glove: broad palm, lateral thumb, three separated folded fingers, and a gold cuff.
    // The wider silhouette stays readable at 32 px and avoids the ambiguous narrow shape of the first draft.
    private const string HandCursorBase64 = "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";

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
