using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckAlbum(MainWindow window, PlayerProfileState profile, TabControl tabs)
    {
        WrapPanel shop = Find<WrapPanel>(window).Single(p => p.Name == "PackShopPanel");
        Require(shop.Children.Count == 5, "Shop must offer all five pack rarities");
        int commonBefore = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        long coinsBefore = profile.Coins;
        Find<Button>(shop).Single(b => Equals(b.Tag, CollectionRarity.Common)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Coins == coinsBefore - 100, "Pack purchase did not debit exactly 100 coins");
        Require(profile.Collection.Packs[CollectionRarity.Common] == commonBefore + 1, "Purchased pack was not stored");
        tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => Equals(t.Header, "Album"));
        window.UpdateLayout();
        Require(Find<StackPanel>(window).Single(p => p.Name == "AlbumThemes").Children.Count == 16, "Album does not show 16 themes");
        foreach (CollectionTheme theme in CollectionCatalog.Themes)
        {
            Find<Button>(Find<StackPanel>(window).Single(p => p.Name == "AlbumThemes")).Single(b => Equals(b.Tag, theme.Id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(Find<WrapPanel>(window).Single(p => p.Name == "AlbumCards").Children.Count == theme.Cards.Count, "Wrong number of theme slots");
            // Every full-art card must load and measure, including cards not drawn in the random pack.
            foreach (CollectionCard card in theme.Cards)
            {
                Border preview = CollectionArtwork.Card(card, true, animationsEnabled: false);
                preview.Measure(new Size(200, 260));
                Require(preview.DesiredSize.Width > 0, "Collection artwork failed to measure: " + card.Id);
            }
        }
        foreach (var size in new[] { (1080d, 720d), (1440d, 900d) })
        {
            window.Width = size.Item1; window.Height = size.Item2; window.UpdateLayout(); await Task.Delay(40);
            RequireWithin(window, Find<TextBlock>(window).Single(t => t.Name == "AlbumSummary"), "Album summary");
            Capture(window, "album-" + (int)size.Item1);
        }
        long opened = profile.Collection.PacksOpened;
        Find<Button>(Find<WrapPanel>(window).Single(p => p.Name == "AlbumPacks"))
            .Single(b => Equals(b.Tag, CollectionRarity.Common)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(profile.Collection.PacksOpened == opened + 1, "Open did not consume one pack");
        CollectionOpening receipt = profile.Collection.PendingOpening!;
        Require(receipt.Cards.Count == 5, "Reveal receipt does not contain five cards");
        Find<Button>(window).Single(b => b.Name == "AlbumRevealAll").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(Find<WrapPanel>(window).Single(p => p.Name == "AlbumOpeningCards").Children.Count == 5, "Reveal must show exactly five card slots");
        Require(Find<Button>(window).Single(b => b.Name == "AlbumCollect").IsEnabled, "Continue is disabled after reveal all");
        // Repeated button input does not roll or award another pack.
        Find<Button>(window).Single(b => b.Name == "AlbumRevealAll").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PacksOpened == opened + 1, "Repeated reveal consumed a pack");
        Capture(window, "album-five-card-reveal");
        // Reload a saved pending opening into a fresh presentation, then finish that exact receipt.
        var reload = ManorPersistence.Clone(profile);
        using var controller = new PlayerProgressionController(reload);
        var resumed = new ProgressionWindow(controller, false, false);
        resumed.SelectTab("Album");
        resumed.Show(); resumed.UpdateLayout();
        Require(reload.Collection.PendingOpening!.Id == receipt.Id, "Reload changed the pack receipt");
        ((Button)resumed.FindName("AlbumRevealAll")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ((Button)resumed.FindName("AlbumCollect")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(reload.Collection.PendingOpening is null, "Continue failed to acknowledge resumed receipt");
        Require(reload.Collection.PacksOpened == opened + 1, "Resume consumed another pack");
        resumed.Close();
        Find<Button>(window).Single(b => b.Name == "AlbumCollect").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PendingOpening is null, "Continue failed to clear the original receipt");
        Require(receipt.Cards.All(c => profile.Collection.OwnedCardIds.Contains(c.CardId)), "Collected cards disappeared after Continue");
        await CheckCollectionArt(window, profile);
    }
}
