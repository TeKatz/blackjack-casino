using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

/// <summary>End-to-end shop purchase/equip and pack transactions through the real React DOM.</summary>
internal static class ModernShopSmoke
{
    internal static async Task Run(MainWindow window, PlayerProfileState profile, string output)
    {
        ModernLobbyView modern = Find<ModernLobbyView>(window).Single();
        Require(modern.Ready && modern.IsVisible && modern.Browser.CoreWebView2 is not null,
            "The real React lobby is not ready for shop migration acceptance.");
        CoreWebView2 web = modern.Browser.CoreWebView2!;

        string shopNav = await web.ExecuteScriptAsync(
            "(()=>{const b=[...document.querySelectorAll('.lobby-nav-item')]"
            + ".find(x=>/Shop/.test(x.textContent||''));b?.click();return !!b;})()");
        Require(shopNav == "true", "The React lobby has no Shop navigation.");
        await UntilAsync(web, "Boolean(document.querySelector('.modern-shop')"
            + "&&document.querySelector('[data-cosmetic-id=\"frame-slate\"]')"
            + "&&document.querySelector('.shop-pack-grid'))", "Shop did not load its live catalogue.");

        // The native WPF cosmetics shop must not appear over or beneath this
        // surface; the React compositor remains the one visible shop view.
        Require(modern.ShopOpen, "Native shop bridge did not acknowledge the React shop.");
        Require(Find<ProgressionWindow>(window).All(x => !x.IsVisible),
            "Legacy progression window was opened alongside the React shop.");
        string catalogue = await web.ExecuteScriptAsync(
            "(()=>{const shop=document.querySelector('.modern-shop');"
            + "return shop?.querySelectorAll('.shop-product').length>50"
            + "&&shop.querySelectorAll('.shop-pack').length===5"
            + "&&!!shop.querySelector('[data-cosmetic-id=\"cardset-solar-eclipse\"]')"
            + "&&!!shop.querySelector('[data-cosmetic-id=\"diceset-aurora\"]')"
            + "&&!!shop.querySelector('[data-cosmetic-id=\"portrait-neon-emperor\"]');})()");
        Require(catalogue == "true", "React Shop does not expose complete cards/dice/portrait catalog and five packs.");

        Require(await web.ExecuteScriptAsync(
            "(()=>{const item=document.querySelector('[data-cosmetic-id=\\\"portrait-secret-guest\\\"]');"
            + "const art=item?.querySelector('[data-testid=\\\"secret-guest-showcase\\\"]');"
            + "const picture=art?.querySelector('picture');"
            + "const media=picture?.querySelector('source[media]');"
            + "return Boolean(item&&art&&picture&&media"
            + "&&art.querySelector('img')?.getAttribute('src')==='/shop/portraits/secret-guest.webp'"
            + "&&media.getAttribute('srcset')==='/shop/portraits/secret-guest.jpg'"
            + "&&item.textContent.includes('She appears before the room remembers.')"
            + "&&item.textContent.includes('Reward only'));})()") == "true",
            "Secret Guest lacks its exclusive animated card, still fallback or challenge-only description.");

        long previousCoins = profile.Coins;
        bool ownedBefore = profile.OwnedCosmeticIds.Contains("frame-slate");
        if (!ownedBefore)
        {
            string clicked = await web.ExecuteScriptAsync(
                "(()=>{const b=document.querySelector('[data-cosmetic-id=\"frame-slate\"] .shop-card-action');"
                + "b?.click();return !!b&&!b.disabled;})()");
            Require(clicked == "true", "Slate shop purchase button is not enabled.");
            await UntilAsync(web, "Boolean(document.querySelector('.shop-confirm'))",
                "React shop omitted purchase confirmation.");
            await web.ExecuteScriptAsync("document.querySelector('.shop-confirm-actions button:last-child')?.click()");
            await UntilAsync(web, "Boolean(document.querySelector('[data-cosmetic-id=\"frame-slate\"].is-equipped'))",
                "Shop did not update equip state following purchase.");
            Require(profile.OwnedCosmeticIds.Contains("frame-slate")
                && profile.EquippedFrameId == "frame-slate", "Native purchase/equip state did not update.");
            Require(profile.Coins == previousCoins - CosmeticCatalog.Get("frame-slate").Price,
                "Cosmetic purchase charged an incorrect amount.");
        }
        // A second click on an already equipped item must never charge coins.
        long coinsAfter = profile.Coins;
        await web.ExecuteScriptAsync(
            "document.querySelector('[data-cosmetic-id=\"frame-slate\"] .shop-card-action')?.click()");
        await Task.Delay(120);
        Require(profile.Coins == coinsAfter, "Equipped item was purchased more than once.");

        int packsBefore = profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        await web.ExecuteScriptAsync(
            "document.querySelector('.pack-common .shop-pack-buy')?.click()");
        await UntilAsync(web, "Boolean(document.querySelector('.shop-confirm'))",
            "Card pack confirmation dialog did not open.");
        await web.ExecuteScriptAsync("document.querySelector('.shop-confirm-actions button:last-child')?.click()");
        await UntilAsync(web, "Boolean(document.querySelector('.pack-common .pack-owned strong')"
            + "?.textContent?.includes('" + (packsBefore + 1) + "'))",
            "Pack purchase did not refresh the React inventory.");
        Require(profile.Coins == coinsAfter - CollectionCatalog.Pack(CollectionRarity.Common).Price
            && profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common) == packsBefore + 1,
            "Pack purchase did not use the native transactional inventory.");

        await using (FileStream capture = File.Create(Path.Combine(output, "modern-shop-live-win.png")))
            await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, capture);
        await web.ExecuteScriptAsync("document.querySelector('.shop-back')?.click()");
        await UntilAsync(web, "Boolean(document.querySelector('.lobby-game-grid')"
            + "&&!document.querySelector('.modern-shop'))",
            "Shop did not navigate back to the React lobby.");
        Require(!modern.ShopOpen && modern.IsVisible, "Closing the shop did not restore the lobby view.");
    }

    private static async Task UntilAsync(CoreWebView2 web, string script, string failure)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (await web.ExecuteScriptAsync(script) == "true") return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(failure);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T item) yield return item;
            foreach (T nested in Find<T>(child)) yield return nested;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
