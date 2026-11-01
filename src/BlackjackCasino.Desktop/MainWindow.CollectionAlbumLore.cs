using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private static readonly bool CollectionLoreHandlerRegistered = RegisterCollectionLoreHandler();

    private static bool RegisterCollectionLoreHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Button.ClickEvent,
            new RoutedEventHandler(CollectionAlbumCardClicked),
            handledEventsToo: true);
        return true;
    }

    private static void CollectionAlbumCardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || !window.MetaPageOpen ||
            !window.metaPages.TryPeek(out MetaPage? page) || page.Shell is not CollectionAlbumPage ||
            window.metaHost?.Content is not FrameworkElement content)
            return;

        Button? clicked = e.Source as Button ?? e.OriginalSource as Button;
        if (clicked?.Tag is not string tag || !tag.StartsWith("inspect:", StringComparison.Ordinal)) return;
        CollectionCard? card = CollectionCatalog.Find(tag["inspect:".Length..]);
        if (card is null) return;

        // The album's own Button.Click handler opens the inspector and writes its placeholder.
        // Apply authored copy after that routed click has fully unwound so ordering cannot leave
        // the placeholder on screen.
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => ApplyCollectionLore(window, content, card));
    }

    private static void ApplyCollectionLore(MainWindow window, FrameworkElement content, CollectionCard card)
    {
        if (!ReferenceEquals(window.metaHost?.Content, content)) return;

        Border? inspector = VisualDescendants<Border>(content)
            .FirstOrDefault(border => border.Name == "AlbumArtOverlay" && border.IsVisible);
        if (inspector is null) return;

        TextBlock? archiveHeader = VisualDescendants<TextBlock>(inspector)
            .FirstOrDefault(text => text.Text == "ARCHIVE ENTRY");
        if (archiveHeader?.Parent is not StackPanel loreStack) return;

        TextBlock? body = loreStack.Children.OfType<TextBlock>().LastOrDefault();
        if (body is null || ReferenceEquals(body, archiveHeader)) return;

        body.Name = "AlbumLoreBody";
        body.Text = CollectionLore.BodyFor(card);
        AutomationProperties.SetName(body, card.Name + " archive lore");
    }
}
