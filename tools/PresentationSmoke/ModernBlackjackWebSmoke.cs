using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckModernBlackjackRoute(MainWindow window)
    {
        window.Modes.Open("blackjack");
        window.UpdateLayout();
        Require(((FrameworkElement)window.FindName("TableSurface")).Visibility == Visibility.Collapsed
            && ((FrameworkElement)window.FindName("BlackjackSidebar")).Visibility == Visibility.Collapsed,
            "Primary Blackjack navigation exposed the old WPF table.");
        Require(!Find<Button>(window).Any(button => button.Name == "ModernBlackjackPreviewButton"),
            "The obsolete Blackjack preview entry still exists.");
        ModernBlackjackView? table = null;
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(75);
            table = Find<ModernBlackjackView>(window).FirstOrDefault();
            if (table is { Ready: true }) break;
            if (table is { Failed: true }) break;
        }
        if (table is not { Ready: true, Failed: false, IsVisible: true })
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var overlay = typeof(MainWindow).GetField("embeddedGameOverlay", flags)?.GetValue(window)
                as FrameworkElement;
            var lobby = Find<CasinoLobbyView>(window).FirstOrDefault();
            string reason = "Primary modern Blackjack route did not initialize. "
                + $"FrontendPresent={ModernBlackjackView.IsAvailable}; "
                + $"HostOverlay={overlay?.Visibility}/{overlay?.IsVisible}; "
                + $"LegacyLobby={lobby?.Visibility}/{lobby?.IsVisible}; "
                + $"NativeSurface={((FrameworkElement)window.FindName("TableSurface")).Visibility}; "
                + $"ModernViews={Find<ModernBlackjackView>(window).Count()}; "
                + $"BrowserSource={table?.Browser.Source}; "
                + $"Ready={table?.Ready}; Failed={table?.Failed}; Visible={table?.IsVisible}.";
            File.WriteAllText(Path.Combine(output, "modern-blackjack-route-diagnostic.txt"), reason);
            throw new InvalidOperationException(reason);
        }
        var web = table!.Browser.CoreWebView2!;
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.bj-app[data-phase]') && document.querySelector('[data-testid=\"blackjack-deal\"]'))") == "true",
            "Modern Blackjack React DOM was not mounted.");

        // A custom rule edit must make a complete native rules snapshot, not
        // reconfigure the frontend alone. This also ensures the rule modal can
        // be used without the old WPF dialog.
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"blackjack-edit-rules\"]')?.click()");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"blackjack-custom-rules\"]'))") == "true",
            "The React custom-rule editor was not available.");
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"blackjack-rules-save\"]')?.click()");
        bool custom = false;
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(60);
            custom = await web.ExecuteScriptAsync(
                "document.querySelector('.bj-app')?.getAttribute('data-preset') === 'Custom'") == "true";
            if (custom) break;
        }
        Require(custom, "Custom rules were not committed to the native C# session.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"blackjack-deal\"]:not(:disabled)'))") == "true",
            "Native table rules did not authorize the initial playable wager.");
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"blackjack-deal\"]').click()");
        string phase = "none";
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(75);
            string raw = await web.ExecuteScriptAsync("document.querySelector('.bj-app')?.getAttribute('data-phase') ?? 'none'");
            phase = JsonSerializer.Deserialize<string>(raw) ?? "none";
            if (phase == "Settled") break;
            if (phase == "InsuranceDecision")
            {
                await web.ExecuteScriptAsync(
                    "Array.from(document.querySelectorAll('.bj-actions button:not(:disabled)')).find(b=>b.textContent?.includes('NO INSURANCE'))?.click()");
            }
            if (phase == "PlayerTurn")
            {
                await web.ExecuteScriptAsync(
                    "Array.from(document.querySelectorAll('.bj-actions button:not(:disabled)')).find(b=>b.textContent?.includes('STAND'))?.click()");
            }
        }
        Require(phase == "Settled", "Modern Blackjack did not finish a native hand.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.bj-dealer .bj-card') && document.querySelector('.bj-hand'))") == "true",
            "Dealer/player cards were not presented in the modern table.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.bj-app[data-card-variant] .bj-card[style*=card-face-a]'))") == "true",
            "Equipped native card palette was not applied to the modern table.");
        await web.ExecuteScriptAsync("document.querySelector('.bj-back')?.click()");
        bool returned = false;
        for (int i = 0; i < 70; i++)
        {
            await Task.Delay(75);
            if (Find<CasinoLobbyView>(window).Any(v => v.IsVisible))
            {
                returned = true;
                break;
            }
        }
        Require(returned, "Modern Blackjack did not return to the lobby after settlement.");
        Require(!Find<ModernBlackjackView>(window).Any(v => v.IsVisible),
            "The modern Blackjack view remained hosted after returning to the lobby.");
        // A second entry must also be fully functional without the WPF preview
        // button and without any native hover or mouse-move event.
        window.Modes.Open("blackjack");
        ModernBlackjackView? reopened = null;
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(75);
            reopened = Find<ModernBlackjackView>(window).FirstOrDefault();
            if (reopened is { Ready: true, Failed: false }) break;
        }
        Require(reopened is { Ready: true, Failed: false, IsVisible: true },
            "Reopening the normal Blackjack route did not produce a playable React table.");
        Require(((FrameworkElement)window.FindName("TableSurface")).Visibility == Visibility.Collapsed,
            "Reopening Blackjack exposed the retired WPF surface.");
        // Simulate WebView2 failing *after* the modern table has opened.
        // The route must recover automatically to the original playable table
        // without discarding the authoritative CasinoSession.
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var session = typeof(MainWindow).GetField("session", flags)!.GetValue(window);
        typeof(ModernBlackjackView).GetMethod("Fail", flags)!
            .Invoke(reopened, ["Simulated WebView2 failure during Blackjack"]);
        bool nativeRecovered = false;
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(75);
            nativeRecovered = ((FrameworkElement)window.FindName("TableSurface")).IsVisible
                && ((FrameworkElement)window.FindName("BlackjackSidebar")).IsVisible
                && !Find<ModernBlackjackView>(window).Any(v => v.IsVisible);
            if (nativeRecovered) break;
        }
        Require(nativeRecovered, "Modern Blackjack browser failure did not recover to the native table.");
        Require(ReferenceEquals(session, typeof(MainWindow).GetField("session", flags)!.GetValue(window)),
            "WebView2 fallback replaced the authoritative Blackjack session.");
    }
}
