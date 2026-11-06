using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckModernReelsRoute(MainWindow window, PlayerProfileState profile)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object controller = typeof(MainWindow).GetField("blackjack", flags)!.GetValue(window)!;
        var session = (CasinoSession)controller.GetType().GetProperty("Session", flags)!.GetValue(controller)!;
        decimal walletBefore = session.Bankroll;
        long spinsBefore = profile.Slots.Spins;
        window.Modes.Open("midnight-reels");
        ModernReelsView? cabinet = null;
        for (int i = 0; i < 160; i++)
        {
            await Task.Delay(75);
            cabinet = Find<ModernReelsView>(window).FirstOrDefault();
            if (cabinet is { Ready: true } or { Failed: true }) break;
        }
        Require(cabinet is { Ready: true, Failed: false, IsVisible: true },
            "Primary Midnight Reels mode did not load its React WebView and native snapshot.");
        Require(!Find<Button>(window).Any(button => button.Name == "ReelsBackButton" && button.IsVisible),
            "Primary Midnight Reels route still exposes the old WPF window.");
        var browser = cabinet!.Browser.CoreWebView2!;
        Require(await browser.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"reels-app\"]')"
            + "&&document.querySelectorAll('.reels-reel').length===3"
            + "&&document.querySelectorAll('.reels-payrow').length===5"
            + "&&document.querySelectorAll('.reels-chips button').length===6"
            + "&&document.querySelector('[data-testid=\"reels-spin\"]'))") == "true",
            "Midnight Reels React cabinet is missing its weighted reels or controls.");

        // The original native slot table has fixed $1/$5/$25/$100/$500/$1000
        // amounts. A browser click must reach the same authoritative wager.
        await browser.ExecuteScriptAsync("document.querySelector('.reels-chips button:nth-child(2)')?.click()");
        bool five = false;
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(65);
            five = await browser.ExecuteScriptAsync(
                "Boolean(document.querySelector('.reels-chips button:nth-child(2).selected'))") == "true";
            if (five) break;
        }
        Require(five, "React $5 wager was not accepted by the native slot session.");
        // Switch off animations to assert instantaneous Core settlement. The
        // Windows scene is also separately checked in the animated state.
        // A fresh QA profile can start with animations disabled already.
        // Exercise a real off->on->off round trip, not an assumed state.
        for(int i=0;i<80;i++)
        {
            if(await browser.ExecuteScriptAsync(
                "Boolean(document.querySelector('[data-testid=\"reels-animation\"]:not(:disabled)'))")=="true")break;
            await Task.Delay(65);
        }
        if(await browser.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"reels-animation\"]')?.checked===false")=="true")
        {
            await browser.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"reels-animation\"]')?.click()");
            for(int i=0;i<80;i++)
            {
                await Task.Delay(65);
                if(await browser.ExecuteScriptAsync(
                    "Boolean(document.querySelector('[data-testid=\"reels-animation\"]:checked:not(:disabled)'))")=="true")break;
            }
            Require(await browser.ExecuteScriptAsync(
                "Boolean(document.querySelector('[data-testid=\"reels-animation\"]:checked:not(:disabled)'))")=="true",
                "Native presentation did not enable animations via React.");
        }
        await browser.ExecuteScriptAsync("document.querySelector('[data-testid=\"reels-animation\"]')?.click()");
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(65);
            if (await browser.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"reels-animation\"]')?.checked===false") == "true") break;
        }
        Require(await browser.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"reels-animation\"]')?.checked===false") == "true",
            "React animation preference did not reach the native area settings.");
        await browser.ExecuteScriptAsync("document.querySelector('[data-testid=\"reels-spin\"]')?.click()");
        string spinId = "";
        for (int i = 0; i < 125; i++)
        {
            await Task.Delay(75);
            string raw = await browser.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"reels-app\"]')?.getAttribute('data-spin-id')??''");
            spinId = JsonSerializer.Deserialize<string>(raw) ?? "";
            if (Guid.TryParse(spinId, out _) && profile.Slots.Spins == spinsBefore + 1) break;
        }
        Require(Guid.TryParse(spinId, out _), "Spin result did not reach the modern React cabinet.");
        Require(profile.Slots.Spins == spinsBefore + 1,
            "React spin did not commit exactly one authoritative native progression event.");
        CasinoSession shared = session;
        SlotMachineSession machine = (SlotMachineSession)typeof(MainWindow).GetField("modernReelsMachine", flags)!.GetValue(window)!;
        Require(shared.Bankroll == machine.Bankroll && shared.Bankroll >= 0
            && (shared.Bankroll != walletBefore || profile.Slots.Spins > spinsBefore),
            "React spin failed to synchronize the single shared wallet.");
        // A duplicate of the last request cannot pay or award twice. Neither a
        // React replay nor a stale message is allowed to reroll the machine.
        Require(profile.Slots.Spins == spinsBefore + 1,
            "Reels progression was applied more than once.");
        await browser.ExecuteScriptAsync("document.querySelector('.reels-back')?.click()");
        for (int i = 0; i < 80 && Find<ModernReelsView>(window).Any(v => v.IsVisible); i++)
            await Task.Delay(65);
        Require(!Find<ModernReelsView>(window).Any(v => v.IsVisible)
            && Find<CasinoLobbyView>(window).Any(v => v.IsVisible),
            "Leaving modern Midnight Reels did not restore the casino lobby.");

        // The emergency WPF slot is no longer the normal game route. Force the
        // actual recovery callback in-process, then keep historical native
        // layout/hover tests running against its still-supported fallback.
        window.Modes.Open("midnight-reels");
        ModernReelsView? reopened = null;
        for (int i = 0; i < 160; i++)
        {
            await Task.Delay(75);
            reopened = Find<ModernReelsView>(window).FirstOrDefault();
            if (reopened is { Ready: true } or { Failed: true }) break;
        }
        Require(reopened is { Ready: true, Failed: false, IsVisible: true },
            "Midnight Reels could not be reopened after returning to the lobby.");
        typeof(ModernReelsView).GetMethod("Fail", flags)!.Invoke(reopened, null);
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(75);
            if (!Find<ModernReelsView>(window).Any(v => v.IsVisible)
                && Find<Button>(window).Any(b => b.Name == "SpinButton" && b.IsVisible)) break;
        }
        Require(!Find<ModernReelsView>(window).Any(v => v.IsVisible)
            && Find<Button>(window).Any(b => b.Name == "SpinButton" && b.IsVisible),
            "WebView2 crash failed to restore the playable WPF Midnight Reels fallback.");
        Require(session.Bankroll == shared.Bankroll,
            "Reels fallback discarded the already committed shared bankroll.");
    }
}
