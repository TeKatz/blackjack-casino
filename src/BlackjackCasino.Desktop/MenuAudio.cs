using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BlackjackCasino.Desktop;

internal static class MenuAudio
{
    internal static bool Enabled { get; set; } = true;
    private static readonly ConditionalWeakTable<RoutedEventArgs,object> Heard = new();
    private static long lastTick;
    internal static void Attach(Window window, Func<bool>? enabled = null)
    {
        if (window.Content is not FrameworkElement root) return;
        void Tick(RoutedEventArgs e)
        {
            if (!root.IsLoaded || !(enabled?.Invoke() ?? Enabled) || Heard.TryGetValue(e,out _)) return;
            Heard.Add(e,new object()); // embedded root and host must not play the same event twice
            long now = Stopwatch.GetTimestamp();
            if (Stopwatch.GetElapsedTime(lastTick,now).TotalMilliseconds < 65) return;
            lastTick=now; CasinoFeedback.Menu(true);
        }
        root.AddHandler(ButtonBase.ClickEvent,new RoutedEventHandler((_,e) =>
        {
            if (e.OriginalSource is not ButtonBase button || !button.IsEnabled) return;
            if (button.Name is "SpinButton" or "DealButton" or "HitButton" or "StandButton" or "DoubleButton" or "SplitButton" or "SurrenderButton") return;
            if (button.Tag is string tag && decimal.TryParse(tag,out decimal ignored)) return;
            Tick(e);
        }),true);
        root.AddHandler(Selector.SelectionChangedEvent,new SelectionChangedEventHandler((_,e) =>
        {
            if (e.OriginalSource is Control control && control is ComboBox or TabControl
                && (control.IsKeyboardFocusWithin || control.IsMouseOver)) Tick(e);
        }),true);
    }
}
