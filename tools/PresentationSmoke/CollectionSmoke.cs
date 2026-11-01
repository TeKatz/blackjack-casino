using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckAlbum(MainWindow window, PlayerProfileState profile, TabControl tabs)
    {
        // Pack purchasing remains part of the cosmetic shop, but browsing/opening the album is now
        // a first-class casino page rather than a cramped Progression tab.
        WrapPanel shop = Find<WrapPanel>(window).Single(p => p.Name == "PackShopPanel");
        Require(shop.Children.Count == 5, "Shop must offer all five pack rarities");
        int commonBefore = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        long coinsBefore = profile.Coins;
        Find<Button>(shop).Single(b => Equals(b.Tag, CollectionRarity.Common)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Coins == coinsBefore - 100, "Pack purchase did not debit exactly 100 coins");
        Require(profile.Collection.Packs[CollectionRarity.Common] == commonBefore + 1, "Purchased pack was not stored");
        Require(!tabs.Items.OfType<TabItem>().Any(t => Equals(t.Header, "Album")), "Legacy Album tab is still present in Progression");

        // Leave nested Progression/Profile and exercise the lobby entry point.
        Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Find<Button>(window).Single(b => b.Name == "ProfileBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v => v.IsVisible), "Album test did not return to the lobby");
        Button lobbyAlbum = Find<Button>(window).Single(b => b.Name == "LobbyProgression_Album" && b.IsVisible);
        Require(Find<Button>(window).Any(b => b.Name == "HeaderCollectionAlbumButton" && b.IsVisible), "Album header shortcut is missing");
        lobbyAlbum.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(70); window.UpdateLayout();
        RequireSingleHost(window, "Collector album from lobby");
        Require(Find<Grid>(window).Any(g => g.Name == "CollectionAlbumRoot" && g.IsVisible), "Lobby Album shortcut did not open the dedicated page");

        StackPanel themes = Find<StackPanel>(window).Single(p => p.Name == "AlbumThemes");
        Require(themes.Children.Count == 16, "Album does not show 16 themes");
        foreach (CollectionTheme theme in CollectionCatalog.Themes)
        {
            Find<Button>(themes).Single(b => Equals(b.Tag, theme.Id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Grid cardArea = Find<Grid>(window).Single(p => p.Name == "AlbumCards");
            Button[] slots = Find<Button>(cardArea).Where(b => b.Tag is string tag && tag.StartsWith("inspect:", StringComparison.Ordinal)).ToArray();
            Require(slots.Length == theme.Cards.Count, "Wrong number of theme slots for " + theme.Id);
            foreach (CollectionCard card in theme.Cards)
            {
                Border preview = CollectionArtwork.Card(card, true, animationsEnabled: false);
                preview.Measure(new Size(200, 260));
                Require(preview.DesiredSize.Width > 0, "Collection artwork failed to measure: " + card.Id);
            }
        }

        // The requested desktop baseline must keep a complete regular collection on one screen.
        window.Width = 1920; window.Height = 1080; window.UpdateLayout(); await Task.Delay(50);
        Find<Button>(themes).Single(b => Equals(b.Tag, CollectionCatalog.Themes.First(t => !t.UltraRare).Id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Grid regularCards = Find<Grid>(window).Single(p => p.Name == "AlbumCards");
        Require(!Find<ScrollViewer>(regularCards).Any(), "Regular album cards still depend on an internal scroll surface");
        foreach (Button card in Find<Button>(regularCards).Where(b => b.Tag is string tag && tag.StartsWith("inspect:", StringComparison.Ordinal)))
            RequireWithin(window, card, "Album card at 1920x1080");
        RequireWithin(window, Find<TextBlock>(window).Single(t => t.Name == "AlbumSummary"), "Album summary at 1920x1080");
        TextBlock duplicatePoints = Find<TextBlock>(window).Single(t => t.Name == "AlbumDuplicatePoints");
        Require(duplicatePoints.Text.Contains(profile.Collection.DuplicatePoints.ToString(), StringComparison.Ordinal)
            && duplicatePoints.Text.Contains(CollectionCatalog.DuplicatePackCost.ToString(), StringComparison.Ordinal),
            "Duplicate recycling does not consistently expose the numeric point total");
        Capture(window, "album-dedicated-1920");

        // Return once and reopen through the header shortcut so both requested access paths stay covered.
        Find<Button>(window).Single(b => b.Name == "AlbumBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v => v.IsVisible), "Album Back did not restore the lobby");
        Find<Button>(window).Single(b => b.Name == "HeaderCollectionAlbumButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(60); window.UpdateLayout();
        Require(Find<Grid>(window).Any(g => g.Name == "CollectionAlbumRoot" && g.IsVisible), "Header Album shortcut did not open the dedicated page");

        // Clicking a pack now stages the sealed five-card overview. Inventory and RNG must remain
        // untouched until the explicit Open 1 Pack / Open All Packs confirmation.
        long opened = profile.Collection.PacksOpened;
        int commonPacksBeforeStage = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        Find<Button>(window).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(profile.Collection.PacksOpened == opened, "Selecting a pack consumed it before confirmation");
        Require(profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common) == commonPacksBeforeStage,
            "Selecting a pack changed inventory before confirmation");
        Require(profile.Collection.PendingOpening is null, "Selecting a pack rolled cards before confirmation");
        Require(Find<Border>(window).Any(b => b.Name == "AlbumOpeningStage" && b.IsVisible), "Pack staging overview is missing");

        Button primaryOpen = Find<Button>(window).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
        Button secondaryOpen = Find<Button>(window).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
        Require(Equals(primaryOpen.Content, "Open 1 Pack"), "Pack staging primary action is not Open 1 Pack");
        Require(secondaryOpen.Content?.ToString()?.StartsWith("Open All Packs", StringComparison.Ordinal) == true,
            "Pack staging secondary action is not Open All Packs");
        Require(primaryOpen.IsEnabled && secondaryOpen.IsEnabled, "Pack staging actions are not enabled");
        Capture(window, "album-pack-staging");

        primaryOpen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(profile.Collection.PacksOpened == opened + 1, "Open 1 Pack did not consume exactly one pack");
        Require(profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common) <= commonPacksBeforeStage,
            "Open 1 Pack did not update inventory");
        CollectionOpening receipt = profile.Collection.PendingOpening!;
        Require(receipt.Cards.Count == 5, "Reveal receipt does not contain five cards");
        Require(Equals(primaryOpen.Content, "Reveal all"), "Opened pack did not switch the primary action to Reveal all");

        primaryOpen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(secondaryOpen.IsEnabled, "Continue is disabled after reveal all");
        primaryOpen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PacksOpened == opened + 1, "Repeated reveal consumed a pack");
        Capture(window, "album-five-card-reveal");

        // Rebuild the dedicated presentation from a serialized-style clone and finish the same receipt.
        var reload = ManorPersistence.Clone(profile);
        using var controller = new PlayerProgressionController(reload);
        var resumed = new CollectionAlbumPage(controller, false, false, () => { });
        resumed.Show(); resumed.UpdateLayout(); await Task.Delay(40);
        Require(reload.Collection.PendingOpening!.Id == receipt.Id, "Reload changed the pack receipt");
        Find<Button>(resumed).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Find<Button>(resumed).Single(b => b.Name == "AlbumCollect" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(reload.Collection.PendingOpening is null, "Continue failed to acknowledge resumed receipt");
        Require(reload.Collection.PacksOpened == opened + 1, "Resume consumed another pack");
        resumed.Close();

        secondaryOpen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PendingOpening is null, "Continue failed to clear the original receipt");
        Require(receipt.Cards.All(c => profile.Collection.OwnedCardIds.Contains(c.CardId)), "Collected cards disappeared after Continue");

        // Open All is a bounded queue: it snapshots the selected rarity count, persists one receipt at
        // a time, and must finish exactly that many packs without recursively consuming bonus rewards.
        var batchProfile = ManorPersistence.Clone(profile);
        batchProfile.Collection.PendingOpening = null;
        batchProfile.Collection.Packs[CollectionRarity.Uncommon] = 3;
        long batchOpenedBefore = batchProfile.Collection.PacksOpened;
        using (var batchController = new PlayerProgressionController(batchProfile))
        {
            var batchPage = new CollectionAlbumPage(batchController, false, false, () => { });
            batchPage.Show(); batchPage.UpdateLayout(); await Task.Delay(35);
            Find<Button>(batchPage).Single(b => Equals(b.Tag, CollectionRarity.Uncommon) && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            batchPage.UpdateLayout();
            Button batchPrimary = Find<Button>(batchPage).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
            Button batchSecondary = Find<Button>(batchPage).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
            Require(Equals(batchSecondary.Content, "Open All Packs (3)"), "Open All did not snapshot all three Uncommon packs");
            batchSecondary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(batchProfile.Collection.PacksOpened == batchOpenedBefore + 1, "Open All did not start with exactly one persisted receipt");

            for (int packIndex = 0; packIndex < 3; packIndex++)
            {
                Require(batchProfile.Collection.PendingOpening is not null, $"Open All lost pending receipt {packIndex + 1}");
                batchPrimary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                batchPage.UpdateLayout();
                Require(batchSecondary.IsEnabled, $"Open All pack {packIndex + 1} cannot be continued after reveal");
                batchSecondary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                batchPage.UpdateLayout();
                if (packIndex < 2)
                    Require(batchProfile.Collection.PacksOpened == batchOpenedBefore + packIndex + 2,
                        $"Open All failed to advance to pack {packIndex + 2}");
            }

            Require(batchProfile.Collection.PendingOpening is null, "Open All left a pending receipt after its queue finished");
            Require(batchProfile.Collection.PacksOpened == batchOpenedBefore + 3, "Open All did not open exactly three packs");
            Require(batchProfile.Collection.Packs.GetValueOrDefault(CollectionRarity.Uncommon) == 0,
                "Open All did not consume exactly the snapshotted Uncommon inventory");
            batchPage.Close();
        }

        await CheckCollectionArt(window, profile);

        // Restore the caller's nested Profile -> Progression state; Program.cs intentionally keeps
        // exercising the old Back contract after this helper returns.
        Find<Button>(window).Single(b => b.Name == "AlbumBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Find<Button>(window).Single(b => b.Name == "PlayerProfileButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(50); window.UpdateLayout();
        Find<Button>(window).Single(b => b.Name == "ProfileProgressionButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(60); window.UpdateLayout();
        Require(Find<TabControl>(window).Any(t => t.Items.OfType<TabItem>().Any(i => Equals(i.Header, "Challenges"))), "Album smoke did not restore Progression");
        Require(!Find<TabControl>(window).Where(t => t.IsVisible).SelectMany(t => t.Items.OfType<TabItem>()).Any(i => Equals(i.Header, "Album")), "Restored Progression exposed the legacy Album tab");
    }
}
