using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Shared interim WPF presentation adapter for React/WebView2 game pages.
/// It owns foreground view attachment and exact restoration of underlying UI
/// input state; game rules, saves, navigation and browser messages remain in
/// their respective application services.
/// A future native shell should replace only this adapter, not each game route.
/// </summary>
internal sealed class ModernGameSurface
{
    private readonly Panel root;
    private readonly UIElement overlay;
    private readonly ContentControl content;
    private UIElement? active;
    private (UIElement Element, bool Enabled)[] covered = [];

    internal ModernGameSurface(Panel root, UIElement overlay, ContentControl content)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        this.overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        this.content = content ?? throw new ArgumentNullException(nameof(content));
    }

    internal bool IsOccupied => active is not null;

    internal void Mount(UIElement view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (IsOccupied || content.Content is not null || overlay.Visibility == Visibility.Visible)
            throw new InvalidOperationException("Another embedded game already owns the desktop surface.");

        var previous = root.Children.OfType<UIElement>()
            .Where(element => !ReferenceEquals(element, overlay))
            .Select(element => (Element: element, Enabled: element.IsEnabled))
            .ToArray();

        try
        {
            foreach ((UIElement element, _) in previous)
                element.SetCurrentValue(UIElement.IsEnabledProperty, false);
            content.Content = view;
            overlay.Visibility = Visibility.Visible;
            KeyboardNavigation.SetTabNavigation(view, KeyboardNavigationMode.Cycle);
            covered = previous;
            active = view;
        }
        catch
        {
            // Failure to attach a view must not strand the lobby or its input.
            if (ReferenceEquals(content.Content, view))
                content.Content = null;
            overlay.Visibility = Visibility.Collapsed;
            foreach ((UIElement element, bool enabled) in previous)
                element.SetCurrentValue(UIElement.IsEnabledProperty, enabled);
            throw;
        }
    }

    internal void Unmount(UIElement view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!ReferenceEquals(active, view))
            throw new InvalidOperationException("A different embedded game owns the desktop surface.");

        try
        {
            content.Content = null;
            overlay.Visibility = Visibility.Collapsed;
        }
        finally
        {
            // Capture actual pre-mount values, including intentionally disabled
            // native controls, instead of blindly enabling the entire shell.
            foreach ((UIElement element, bool enabled) in covered)
                element.SetCurrentValue(UIElement.IsEnabledProperty, enabled);
            covered = [];
            active = null;
        }
    }
}
