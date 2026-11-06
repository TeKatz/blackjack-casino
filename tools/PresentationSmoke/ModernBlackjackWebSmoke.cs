using System.IO;
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
        Require(window.FindName("TableSurface") is null
            && window.FindName("BlackjackSidebar") is null,
            "Primary Blackjack navigation still contains the retired WPF table.");
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
            var diagnosticFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var overlay = typeof(MainWindow).GetField("embeddedGameOverlay", diagnosticFlags)?.GetValue(window)
                as FrameworkElement;
            var lobby = Find<CasinoLobbyView>(window).FirstOrDefault();
            string reason = "Primary modern Blackjack route did not initialize. "
                + $"FrontendPresent={ModernBlackjackView.IsAvailable}; "
                + $"HostOverlay={overlay?.Visibility}/{overlay?.IsVisible}; "
                + $"LegacyLobby={lobby?.Visibility}/{lobby?.IsVisible}; "
                + $"LegacyTableRemoved={window.FindName("TableSurface") is null}; "
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
        Require(window.FindName("TableSurface") is null,
            "Reopening Blackjack restored the retired WPF surface.");
        // WebView failure must preserve the controller and round without bringing
        // back a second WPF implementation. A new React host must be usable.
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var controller = typeof(MainWindow).GetField("blackjack", flags)!.GetValue(window)!;
        var currentSession = controller.GetType().GetProperty("Session", flags)!.GetValue(controller);
        typeof(ModernBlackjackView).GetMethod("Fail", flags)!
            .Invoke(reopened, ["Simulated WebView2 failure during Blackjack"]);
        BrowserRecoveryView? recovery = null;
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(75);
            recovery = Find<BrowserRecoveryView>(window).FirstOrDefault(v => v.IsVisible);
            if (recovery is not null) break;
        }
        Require(recovery is not null
            && !Find<ModernBlackjackView>(window).Any(v => v.IsVisible),
            "Blackjack WebView failure did not show the independent recovery screen.");
        Require(recovery!.RetryButton.IsEnabled && recovery.LobbyButton.IsEnabled
            && !recovery.LegacyButton.IsEnabled,
            "Blackjack recovery incorrectly offered a removed WPF table.");
        Require(window.FindName("TableSurface") is null,
            "Blackjack failure recreated a retired native table.");
        Require(ReferenceEquals(controller.GetType().GetProperty("Session", flags)!.GetValue(controller), currentSession),
            "Blackjack failure replaced the authoritative Core session.");
        recovery.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ModernBlackjackView? resumed = null;
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(75);
            resumed = Find<ModernBlackjackView>(window).FirstOrDefault(v => v is { Ready: true, Failed: false });
            if (resumed is not null) break;
        }
        Require(resumed is { Ready: true, IsVisible: true },
            "Reopening after browser failure did not restore a playable React host.");
        Require(ReferenceEquals(controller.GetType().GetProperty("Session", flags)!.GetValue(controller), currentSession),
            "Reopening after a browser failure discarded the Core session.");
        await resumed!.Browser.CoreWebView2!.ExecuteScriptAsync("document.querySelector('.bj-back')?.click()");
        for (int i = 0; i < 80 && Find<ModernBlackjackView>(window).Any(v => v.IsVisible); i++)
            await Task.Delay(75);
        Require(!Find<ModernBlackjackView>(window).Any(v => v.IsVisible),
            "Blackjack was still hosted after its recovery smoke.");
    }
}
