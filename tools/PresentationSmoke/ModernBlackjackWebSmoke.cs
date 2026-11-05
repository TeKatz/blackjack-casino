using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckModernBlackjackPreview(MainWindow window)
    {
        Button preview = Find<Button>(window)
            .SingleOrDefault(b => b.Name == "ModernBlackjackPreviewButton")
            ?? throw new InvalidOperationException("Modern Blackjack preview button is missing.");
        preview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ModernBlackjackView? table = null;
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(75);
            table = Find<ModernBlackjackView>(window).FirstOrDefault();
            if (table is { Ready: true }) break;
            if (table is { Failed: true }) break;
        }
        Require(table is { Ready: true, Failed: false, IsVisible: true },
            "Modern Blackjack preview failed to initialize WebView2 and native snapshot.");
        var web = table!.Browser.CoreWebView2!;
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.bj-app[data-phase]') && document.querySelector('[data-testid=\"blackjack-deal\"]'))") == "true",
            "Modern Blackjack React DOM was not mounted.");

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
            "The modern Blackjack preview remained hosted after returning to the lobby.");
    }
}
