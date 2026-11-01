using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    static MainWindow()
    {
        // MainWindow owns the global Escape-to-close behavior for hosted meta pages. The album
        // inspector is one level deeper, so intercept its Escape first; otherwise the same key
        // closes the entire album before the page can restore the selected card.
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(CollectionAlbumEscapeClassHandler),
            handledEventsToo: true);
    }

    private static void CollectionAlbumEscapeClassHandler(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || sender is not MainWindow window ||
            !window.MetaPageOpen || !window.metaPages.TryPeek(out MetaPage? page) ||
            page.Shell is not CollectionAlbumPage || window.metaHost?.Content is not FrameworkElement content)
            return;

        Border? inspector = VisualDescendants<Border>(content)
            .FirstOrDefault(border => border.Name == "AlbumArtOverlay" && border.IsVisible);
        if (inspector is null) return;

        string? cardId = VisualDescendants<Image>(inspector)
            .FirstOrDefault(image => image.Name == "CollectionCardArt")?.Tag as string;
        Button? cardButton = string.IsNullOrEmpty(cardId) ? null : VisualDescendants<Button>(content)
            .FirstOrDefault(button => Equals(button.Tag, "inspect:" + cardId));
        Button? closeButton = VisualDescendants<Button>(inspector)
            .FirstOrDefault(button => button.Name == "AlbumArtClose");
        if (closeButton is null) return;

        // Use the page's own close path so its state/focus bookkeeping remains authoritative.
        closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        e.Handled = true;

        if (cardButton is null) return;
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (!cardButton.IsVisible || !cardButton.IsEnabled) return;
            cardButton.BringIntoView();
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(cardButton), cardButton);
            cardButton.Focus();
            Keyboard.Focus(cardButton);
        });
    }
}
