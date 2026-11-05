using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    /// <summary>Exercise the actual React controls when active, not a hidden WPF fallback.</summary>
    private static async Task OpenAlbumFromLobby(MainWindow window, bool fromHeader)
    {
        ModernLobbyView? modern = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modern is not null)
        {
            string selector = fromHeader
                ? "document.querySelector('.lobby-top-status .header-compact:nth-child(2)')"
                : "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Collections|Sammlungen/.test(x.textContent||''))";
            string result = await modern.Browser.CoreWebView2!.ExecuteScriptAsync(
                "Boolean(" + selector + ")");
            Require(result == "true", "React lobby is missing the Collector's Album shortcut: " + selector);
            await modern.Browser.CoreWebView2.ExecuteScriptAsync(selector + "?.click()");
        }
        else
        {
            string name = fromHeader ? "HeaderCollectionAlbumButton" : "LobbyProgression_Album";
            Find<Button>(window).Single(b => b.Name == name && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        // Clicking the WebView2 DOM crosses an asynchronous native IPC boundary.
        // Wait for the dedicated page instead of assuming WPF has already handled the click.
        for (int i = 0; i < 110; i++)
        {
            window.UpdateLayout();
            if (Find<Grid>(window).Any(g => g.Name == "CollectionAlbumRoot" && g.IsVisible))
            {
                Require(!Find<ModernLobbyView>(window).Any(v => v.IsVisible && v.Ready),
                    "The React lobby still covers the native Collector's Album page");
                return;
            }
            await Task.Delay(40);
        }
        throw new InvalidOperationException("Collector's Album did not open through the active lobby UI.");
    }

    private static async Task CheckAlbum(MainWindow window, PlayerProfileState profile, TabControl tabs)
    {
        async Task WaitUntil(Func<bool> condition, string message, int attempts = 140)
        {
            for (int i = 0; i < attempts; i++)
            {
                window.UpdateLayout();
                if (condition()) return;
                await Task.Delay(40);
            }
            Require(condition(), message);
        }

        // The migrated React shop is independently exercised through real WebView2
        // purchase controls in ModernShopSmoke. For this dedicated Album test,
        // prepare the inventory through the same authoritative collection service
        // without resurrecting the retired WPF Shop panel.
        int commonBefore = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        long coinsBefore = profile.Coins;
        CollectionActionResult pack = CollectionService.PurchasePack(profile, CollectionRarity.Common);
        Require(pack.Success, "Unable to prepare a card pack for album acceptance.");
        Require(profile.Coins == coinsBefore - 100, "Pack purchase did not debit exactly 100 coins");
        Require(profile.Collection.Packs[CollectionRarity.Common] == commonBefore + 1, "Purchased pack was not stored");
        Require(!tabs.Items.OfType<TabItem>().Any(t => Equals(t.Header, "Album")), "Legacy Album tab is still present in Progression");

        // Leave nested Progression/Profile and exercise the lobby entry point.
        Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Find<Button>(window).Single(b => b.Name == "ProfileBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v => v.IsVisible), "Album test did not return to the lobby");
        await OpenAlbumFromLobby(window, fromHeader: false);
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

        // Return all the way to the React lobby before testing its header shortcut.
        // Old WPF navigation could access the underlying Blackjack header while a
        // native Profile/Progression page was still foregrounded; that is now hidden.
        Find<Button>(window).Single(b => b.Name == "AlbumBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        if (Find<Button>(window).Any(b => b.Name == "ProgressionBackButton" && b.IsVisible))
            Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        if (Find<Button>(window).Any(b => b.Name == "ProfileBackButton" && b.IsVisible))
            Find<Button>(window).Single(b => b.Name == "ProfileBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitUntil(() => Find<CasinoLobbyView>(window).Any(v => v.IsVisible)
            && (!ModernLobbyView.IsAvailable
                || Find<ModernLobbyView>(window).Any(v => v.Ready && v.IsVisible)),
            "Album Back did not restore the active casino lobby");
        Require(Find<CasinoLobbyView>(window).Any(v => v.IsVisible), "Album Back did not restore the lobby");
        await OpenAlbumFromLobby(window, fromHeader: true);
        await Task.Delay(60); window.UpdateLayout();
        Require(Find<Grid>(window).Any(g => g.Name == "CollectionAlbumRoot" && g.IsVisible), "Header Album shortcut did not open the dedicated page");

        Grid browse = Find<Grid>(window).Single(g => g.Name == "AlbumBrowseSurface");
        Border openingOverlay = Find<Border>(window).Single(b => b.Name == "AlbumOpeningOverlay");
        Border openingStage = Find<Border>(window).Single(b => b.Name == "AlbumOpeningStage");
        Button openingBack = Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack");

        // Selecting a pack only stages the overview. The overlay must always have a real escape route.
        long opened = profile.Collection.PacksOpened;
        int commonPacksBeforeStage = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        Find<Button>(window).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(profile.Collection.PacksOpened == opened, "Selecting a pack consumed it before confirmation");
        Require(profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common) == commonPacksBeforeStage,
            "Selecting a pack changed inventory before confirmation");
        Require(profile.Collection.PendingOpening is null, "Selecting a pack rolled cards before confirmation");
        Require(openingOverlay.IsVisible && openingStage.IsVisible, "Pack staging overview is missing");
        Require(browse.Visibility == Visibility.Collapsed, "Album browser kept rendering behind the pack overlay");
        Require(!Find<Button>(openingStage).Any(), "Sealed cards are still individually clickable");

        Button primaryOpen = Find<Button>(window).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
        Button secondaryOpen = Find<Button>(window).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
        Require(Equals(primaryOpen.Content, "Open Pack"), "Pack staging primary action is not Open Pack");
        Require(secondaryOpen.Content?.ToString()?.StartsWith("Open All Packs", StringComparison.Ordinal) == true,
            "Pack staging secondary action is not Open All Packs");
        Require(openingBack.IsVisible && openingBack.IsEnabled, "Pack staging has no Back to album action");
        Capture(window, "album-pack-staging");

        // Back from staging must be lossless and immediately restore album navigation.
        openingBack.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(!openingOverlay.IsVisible && browse.Visibility == Visibility.Visible, "Back to album did not leave pack staging");
        Require(profile.Collection.PacksOpened == opened && profile.Collection.PendingOpening is null,
            "Back from staging mutated pack state");

        // Open Pack starts the automatic five-card reveal. No Reveal all / Continue state remains.
        Find<Button>(window).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        primaryOpen = Find<Button>(window).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
        primaryOpen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PacksOpened == opened + 1, "Open Pack did not consume exactly one pack");
        await WaitUntil(() => profile.Collection.PendingOpening is null, "Automatic pack reveal did not complete its saved receipt");
        Require(Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible).IsEnabled,
            "Automatic reveal did not finish on a usable Back to album state");
        Require(Find<Border>(openingStage).Count(b => b.Name == "AlbumRevealResultBadge") == CollectionCatalog.CardsPerPack,
            "Automatic reveal did not mark all five result cards");
        Capture(window, "album-five-card-reveal");
        Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(browse.Visibility == Visibility.Visible, "Album did not return after completed pack reveal");

        // Reproduce the reported softlock: start another reveal, leave immediately while its receipt is
        // pending, then exit and reopen the album through the header. Reopening must show the album,
        // not force the reveal overlay, and the saved pack must be resumable from its pack slot.
        long beforePausedReveal = profile.Collection.PacksOpened;
        Find<Button>(window).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Find<Button>(window).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Collection.PacksOpened == beforePausedReveal + 1, "Paused reveal did not persist its opening first");
        Require(profile.Collection.PendingOpening is not null, "Paused reveal has no persisted receipt");
        Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(profile.Collection.PendingOpening is not null, "Back to album discarded the pending receipt");
        Require(browse.Visibility == Visibility.Visible && !openingOverlay.IsVisible, "Back to album left the reveal overlay active");

        Find<Button>(window).Single(b => b.Name == "AlbumBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        await OpenAlbumFromLobby(window, fromHeader: true);
        await Task.Delay(60); window.UpdateLayout();
        openingOverlay = Find<Border>(window).Single(b => b.Name == "AlbumOpeningOverlay");
        browse = Find<Grid>(window).Single(g => g.Name == "AlbumBrowseSurface");
        Require(!openingOverlay.IsVisible && browse.IsVisible,
            "Reopening the album forced the pending pack overlay and recreated the softlock");

        Find<Button>(window).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitUntil(() => profile.Collection.PendingOpening is null, "Resume did not automatically finish the saved pack reveal");
        Require(Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible).IsEnabled,
            "Resumed reveal has no Back to album action");
        Find<Button>(window).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();

        // Open All is bounded to the selected rarity count and automatically reveals each persisted
        // receipt in order. It must finish without manual card clicks or a Continue button.
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
            Button batchSecondary = Find<Button>(batchPage).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
            Require(Equals(batchSecondary.Content, "Open All Packs (3)"), "Open All did not snapshot all three Uncommon packs");
            batchSecondary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; i < 140 && (batchProfile.Collection.PendingOpening is not null || batchProfile.Collection.PacksOpened < batchOpenedBefore + 3); i++)
            {
                batchPage.UpdateLayout();
                await Task.Delay(20);
            }
            Require(batchProfile.Collection.PendingOpening is null, "Open All left a pending receipt after its queue finished");
            Require(batchProfile.Collection.PacksOpened == batchOpenedBefore + 3, "Open All did not open exactly three packs");
            Require(batchProfile.Collection.Packs.GetValueOrDefault(CollectionRarity.Uncommon) == 0,
                "Open All did not consume exactly the snapshotted Uncommon inventory");
            Require(Find<Button>(batchPage).Single(b => b.Name == "AlbumOpeningBack" && b.IsVisible).IsEnabled,
                "Open All did not finish on Back to album");
            batchPage.Close();
        }

        // Regression for the reported stale count: four Common packs must become three after Open Pack,
        // and the result screen itself must immediately offer the next Open Pack / Open All. The badge
        // expectations are taken from the persisted receipt because same-pack repeats can already be duplicates.
        var countProfile = ManorPersistence.Clone(profile);
        countProfile.Collection.PendingOpening = null;
        countProfile.Collection.OwnedCardIds.Clear();
        countProfile.Collection.DuplicatePoints = 0;
        countProfile.Collection.Packs[CollectionRarity.Common] = 4;
        long countOpenedBefore = countProfile.Collection.PacksOpened;
        using (var countController = new PlayerProgressionController(countProfile))
        {
            var countPage = new CollectionAlbumPage(countController, false, false, () => { });
            countPage.Show(); countPage.UpdateLayout(); await Task.Delay(35);
            Find<Button>(countPage).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Button countPrimary = Find<Button>(countPage).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
            Button countAll = Find<Button>(countPage).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
            Require(Equals(countAll.Content, "Open All Packs (4)"), "Four-pack staging count is wrong before opening");
            countPrimary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            CollectionOpening countOpening = countProfile.Collection.PendingOpening
                ?? throw new InvalidOperationException("Open Pack did not persist a receipt for badge verification");
            int expectedNew = countOpening.Cards.Count(c => c.IsNew);
            int expectedDuplicates = countOpening.Cards.Count - expectedNew;

            for (int i = 0; i < 140 && countProfile.Collection.PendingOpening is not null; i++)
            {
                countPage.UpdateLayout();
                await Task.Delay(20);
            }
            Require(countProfile.Collection.PacksOpened == countOpenedBefore + 1, "Count regression did not open exactly one pack");
            Require(countProfile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common) == 3,
                "Opening one of four Common packs did not leave exactly three");
            countPrimary = Find<Button>(countPage).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible);
            countAll = Find<Button>(countPage).Single(b => b.Name == "AlbumCollect" && b.IsVisible);
            Require(Equals(countPrimary.Content, "Open Pack"), "Open Pack did not return after a completed reveal");
            Require(Equals(countAll.Content, "Open All Packs (3)"), "Open All did not refresh from four packs to three");

            Border countStage = Find<Border>(countPage).Single(b => b.Name == "AlbumOpeningStage");
            Border[] resultBadges = Find<Border>(countStage).Where(b => b.Name == "AlbumRevealResultBadge").ToArray();
            Require(resultBadges.Length == CollectionCatalog.CardsPerPack,
                "Completed pack did not show five result badges");
            Border[] newBadges = resultBadges.Where(b => Equals(b.Tag, "NEW")).ToArray();
            Border[] duplicateResultBadges = resultBadges.Where(b => Equals(b.Tag, "DUPLICATE")).ToArray();
            Require(newBadges.Length == expectedNew && duplicateResultBadges.Length == expectedDuplicates,
                "Reveal result badges do not match the saved opening receipt");
            Require(newBadges.Length > 0 && newBadges.All(b => b.BorderBrush is SolidColorBrush brush && brush.Color.G > brush.Color.R && brush.Color.G > brush.Color.B),
                "NEW result badges are not using the green treatment");
            Require(duplicateResultBadges.All(b => b.BorderBrush is SolidColorBrush brush && brush.Color.B > brush.Color.G && brush.Color.R > 150),
                "Same-pack duplicate badges are not using the lavender treatment");
            Require(duplicateResultBadges.SelectMany(Find<TextBlock>).All(t => t.Text.StartsWith("DUPLICATE +", StringComparison.Ordinal)),
                "Same-pack duplicate result badges do not expose their +X value");
            Capture(countPage, "album-pack-result-actions");
            countPage.Close();
        }

        // A fully owned catalog guarantees duplicates, so the lavender DUPLICATE +X treatment is
        // verified independently from RNG composition.
        var duplicateProfile = ManorPersistence.Clone(profile);
        duplicateProfile.Collection.PendingOpening = null;
        duplicateProfile.Collection.OwnedCardIds.Clear();
        foreach (CollectionCard card in CollectionCatalog.Cards) duplicateProfile.Collection.OwnedCardIds.Add(card.Id);
        duplicateProfile.Collection.DuplicatePoints = 0;
        duplicateProfile.Collection.Packs[CollectionRarity.Common] = 1;
        using (var duplicateController = new PlayerProgressionController(duplicateProfile))
        {
            var duplicatePage = new CollectionAlbumPage(duplicateController, false, false, () => { });
            duplicatePage.Show(); duplicatePage.UpdateLayout(); await Task.Delay(35);
            Find<Button>(duplicatePage).Single(b => Equals(b.Tag, CollectionRarity.Common) && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Find<Button>(duplicatePage).Single(b => b.Name == "AlbumRevealAll" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; i < 140 && duplicateProfile.Collection.PendingOpening is not null; i++)
            {
                duplicatePage.UpdateLayout();
                await Task.Delay(20);
            }
            Border duplicateStage = Find<Border>(duplicatePage).Single(b => b.Name == "AlbumOpeningStage");
            Border[] duplicateBadges = Find<Border>(duplicateStage).Where(b => b.Name == "AlbumRevealResultBadge").ToArray();
            Require(duplicateBadges.Length == CollectionCatalog.CardsPerPack && duplicateBadges.All(b => Equals(b.Tag, "DUPLICATE")),
                "Fully owned pack did not show five duplicate result badges");
            Require(duplicateBadges.All(b => b.BorderBrush is SolidColorBrush brush && brush.Color.B > brush.Color.G && brush.Color.R > 150),
                "Duplicate result badges are not using the lavender treatment");
            Require(duplicateBadges.SelectMany(Find<TextBlock>).All(t => t.Text.StartsWith("DUPLICATE +", StringComparison.Ordinal)),
                "Duplicate result badges do not expose their +X value");
            duplicatePage.Close();
        }

        await CheckCollectionArt(window, profile);

        // Restore the caller's nested Profile -> Progression state; Program.cs intentionally keeps
        // exercising the old Back contract after this helper returns.
        Find<Button>(window).Single(b => b.Name == "AlbumBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40); window.UpdateLayout();
        ModernLobbyView? activeLobby = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (activeLobby is not null)
        {
            string available = await activeLobby.Browser.CoreWebView2!.ExecuteScriptAsync(
                "Boolean(document.querySelector('.profile-widget'))");
            Require(available == "true", "Modern lobby has no player-profile shortcut");
            await activeLobby.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.profile-widget').click()");
            await WaitUntil(() => activeLobby.RecordsPage=="profile",
                "React account hub did not open from the profile widget");
            bool editorReady = false;
            for (int i = 0; i < 100 && !editorReady; i++)
            {
                editorReady = await activeLobby.Browser.CoreWebView2.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.account-page .account-routes button:last-child'))") == "true";
                if (!editorReady) await Task.Delay(50);
            }
            Require(editorReady, "The React account editor action was not rendered.");
            await activeLobby.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.account-page .account-routes button:last-child')?.click()");
        }
        else
        {
            Find<Button>(window).Single(b => b.Name == "PlayerProfileButton" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        await WaitUntil(() => Find<Button>(window).Any(b => b.Name == "ProfileProgressionButton" && b.IsVisible),
            "Modern lobby did not open the native profile page");
        Find<Button>(window).Single(b => b.Name == "ProfileProgressionButton" && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(60); window.UpdateLayout();
        Require(Find<TabControl>(window).Any(t => t.Items.OfType<TabItem>().Any(i => Equals(i.Header, "Challenges"))), "Album smoke did not restore Progression");
        Require(!Find<TabControl>(window).Where(t => t.IsVisible).SelectMany(t => t.Items.OfType<TabItem>()).Any(i => Equals(i.Header, "Album")), "Restored Progression exposed the legacy Album tab");
    }
}
