using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;
using static ModernDiceSmokeHelper;

internal static class KniffelCasinoSmoke
{
    internal static async Task Run(MainWindow window, string output, PlayerProfileState profile)
    {
        ModernDiceView view = await Open(window);
        CoreWebView2 web = view.Browser.CoreWebView2!;
        await ModernDiceSmokeHelper.Click(web, "dice-setup", false);
        await Capture(web, Path.Combine(output, "dice-modern-casino-setup.png"));
        decimal initialCash = window.DiceCasinoBankroll;
        await ModernDiceSmokeHelper.Click(web, "dice-casino-start");
        Require(window.DiceCasinoBankroll == initialCash - 100m,
            "The casino entry was not debited exactly once.");
        Require(Application.Current.Windows.Cast<Window>().Count(x => x.IsVisible) == 1,
            "The React casino table opened a second native window.");
        Require(await JavaScript(web,
            "document.querySelector('[data-testid=\"modern-dice-app\"]')?.dataset.mode==='casino'"),
            "React Midnight Dice did not switch to the paid table.");
        Guid gameId = Game(window).GameId;

        await ModernDiceSmokeHelper.Click(web, "dice-setup", false);
        Require(await JavaScript(web,
            "Boolean(document.querySelector('[data-testid=\"dice-casino-start\"]')?.disabled)"),
            "React game setup allows paying a second entry while a paid table is active.");
        await ModernDiceSmokeHelper.Click(web, "dice-setup-close", false);
        long xp = profile.LifetimeExperience;
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            await WaitFor(window, () => window.DiceTableCheckpoint is { Settled: false } current
                && !KniffelTable.NeedsOpponents(current), "Opponents failed to finish their last turn.", 90);
            await ModernDiceSmokeHelper.Click(web, "dice-roll");
            int points = Game(window).Preview(category);
            await Select(web, category.ToString());
            await ModernDiceSmokeHelper.Click(web, points > 0 ? "dice-commit" : "dice-scratch");
            int turn = Game(window).FilledCount;
            if (turn == 1 || turn == 13)
            {
                // Closing during a saved AI turn must not advance cash or rerun the seat.
                await ModernDiceSmokeHelper.Click(web, "dice-back", false);
                await WaitForExit(window);
                decimal cashWhileAway = window.DiceCasinoBankroll;
                await Task.Delay(140);
                Require(window.DiceCasinoBankroll == cashWhileAway,
                    "Leaving a pending AI turn changed the shared bankroll.");
                view = await Open(window);web = view.Browser.CoreWebView2!;
                Require(Game(window).GameId == gameId, "Paid seat identity changed on resume.");
            }
            await WaitFor(window, () => window.DiceTableCheckpoint is { } current
                && current.Opponents.All(s => s.Entries.Length == turn),
                "The three native AI score sheets did not save exactly one turn each.", 90);
            if (turn == 1)
            {
                await ModernDiceSmokeHelper.Click(web, "dice-opponents", false);
                Require(await JavaScript(web,
                    "Boolean(document.querySelector('[data-testid=\"dice-opponents-panel\"]')"
                    + "?.textContent.includes('Mara')"
                    + "&&document.querySelector('[data-testid=\"dice-opponents-panel\"]')?.textContent.includes('Felix')"
                    + "&&document.querySelector('[data-testid=\"dice-opponents-panel\"]')?.textContent.includes('June'))"),
                    "Opponent score sheet is missing named, independently saved AI seats.");
                await Capture(web, Path.Combine(output, "dice-modern-opponents.png"));
                await ModernDiceSmokeHelper.Click(web, "dice-opponents-close", false);
                await Capture(web, Path.Combine(output, "dice-modern-casino-table.png"));
            }
        }

        await WaitFor(window, () => window.DiceTableCheckpoint?.Settled == true,
            "The 13-turn paid table was not settled.", 90);
        KniffelTableState result = window.DiceTableCheckpoint!;
        Require(window.DiceCasinoBankroll == initialCash - 100m + result.Payout,
            "Casino payout was not the net original entry plus saved placement reward.");
        Require(profile.Kniffel.CasinoGamesCompleted == 1 && profile.Kniffel.TotalWagered == 100m,
            "Native casino profile statistics were not recorded exactly once.");
        Require(profile.LifetimeExperience == xp + KniffelProgression.PlayerExperience(Game(window).Total),
            "Completed paid dice game did not award the correct native XP.");

        bool receiptVisible = false;
        for (int i = 0; i < 200 && !receiptVisible; i++)
        {
            receiptVisible = await JavaScript(web,
                "Boolean(document.querySelector('[data-testid=\"dice-result-panel\"]'))");
            if (!receiptVisible) await Task.Delay(80);
        }
        Require(receiptVisible, "Casino placement did not show the modern match result receipt.");
        Require(await JavaScript(web,
            "Boolean(document.querySelector('.md-receipt-money')"
            + "&&document.querySelector('.md-results-standings .md-result-seat.player'))"),
            "Modern match receipt lacks net-prize breakdown and ranked player score.");
        await Capture(web, Path.Combine(output, "dice-modern-casino-result.png"));

        await ModernDiceSmokeHelper.Click(web, "dice-result-close", false);
        await ModernDiceSmokeHelper.Click(web, "dice-result", false);
        await ModernDiceSmokeHelper.Click(web, "dice-result-close", false);
        decimal settledCash = window.DiceCasinoBankroll;
        long settledXp = profile.LifetimeExperience;
        await ModernDiceSmokeHelper.Click(web, "dice-back", false);
        await WaitForExit(window);
        view = await Open(window);web = view.Browser.CoreWebView2!;
        Require(window.DiceCasinoBankroll == settledCash && profile.LifetimeExperience == settledXp,
            "Reopening completed casino dice paid cash or XP again.");

        for (int i = 0; i < 150; i++)
        {
            if (await JavaScript(web,
                "Boolean(document.querySelector('[data-testid=\"dice-result-setup\"]'))")) break;
            await Task.Delay(70);
        }
        await ModernDiceSmokeHelper.Click(web, "dice-result-setup", false);
        await ModernDiceSmokeHelper.Click(web, "dice-casino-start");
        Require(window.DiceCasinoBankroll == settledCash - 100m,
            "Opening another settled table did not debit exactly one new entry.");

        await ModernDiceSmokeHelper.Click(web, "dice-setup", false);
        await ModernDiceSmokeHelper.Click(web, "dice-casino-forfeit", false);
        Require(window.DiceTableCheckpoint is { Forfeited: false },
            "React casino forfeit did not require the second explicit confirmation.");
        await ModernDiceSmokeHelper.Click(web, "dice-casino-forfeit");
        Require(window.DiceTableCheckpoint is { Forfeited: true },
            "Confirmed forfeit did not durably settle the paid table.");
        Require(window.DiceCasinoBankroll == settledCash - 100m
            && profile.LifetimeExperience == settledXp && profile.Kniffel.Forfeits == 1,
            "Forfeit refunded play money, granted XP or missed native forfeits.");
        Require(await JavaScript(web,
            "Boolean(document.querySelector('[data-testid=\"dice-roll\"]')?.disabled)"),
            "A forfeited table still allows rolling.");
        for (int i = 0; i < 150; i++)
        {
            if (await JavaScript(web,
                "Boolean(document.querySelector('[data-testid=\"dice-result-lobby\"]'))")) break;
            await Task.Delay(70);
        }
        Require(await JavaScript(web,
            "Boolean(document.querySelector('.md-receipt-money')?.textContent.includes('100.00'))"),
            "A forfeited table has no receipt for the lost entry.");
        await ModernDiceSmokeHelper.Click(web, "dice-result-lobby", false);
        await WaitForExit(window);
        await OpenLobbyProgression(window, "Statistics", "LobbyProgression_Overview");
        ModernLobbyView? modern = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modern is not null)
        {
            var web = modern.Browser.CoreWebView2!;
            await Until(() => modern.RecordsPage == "statistics");
            bool diceStats = false;
            for (int i = 0; i < 100 && !diceStats; i++)
            {
                diceStats = await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.statistics-page [data-stat-game=\"midnight-dice\"]'))") == "true";
                if (!diceStats) await Task.Delay(75);
            }
            Require(diceStats, "React lifetime statistics have no Midnight Dice card.");
            await using(FileStream capture = File.Create(Path.Combine(output,"dice-statistics.png")))
                await web.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,capture);
            await web.ExecuteScriptAsync("document.querySelector('.statistics-page .shop-back')?.click()");
            await Until(() => modern.RecordsPage is null);
        }
        else
        {
            await Task.Delay(120);
            Require(Find<TextBlock>(window).Any(text => text.Text.Contains("MIDNIGHT DICE", StringComparison.Ordinal)),
                "Statistics overview has no Midnight Dice card.");
            Capture(window, Path.Combine(output, "dice-statistics.png"));
            ClickLegacy(window, "ProgressionBackButton");
        }
        await OpenLobbyProgression(window, "Challenges", "LobbyProgression_Challenges");
        if (modern is not null)
        {
            var web = modern.Browser.CoreWebView2!;
            await Until(() => modern.RecordsPage == "challenges");
            bool selected = false;
            for (int i = 0; i < 100 && !selected; i++)
            {
                selected = await web.ExecuteScriptAsync(
                    "(()=>{const button=[...document.querySelectorAll('.record-category')]"
                    + ".find(x=>x.textContent?.includes('Midnight Dice'));button?.click();return !!button;})()") == "true";
                if(!selected) await Task.Delay(75);
            }
            Require(selected, "React Challenges lacks Midnight Dice category.");
            bool firstSheet = false;
            for (int i = 0; i < 100 && !firstSheet; i++)
            {
                firstSheet = await web.ExecuteScriptAsync(
                    "Boolean([...document.querySelectorAll('.record-challenge h3')].some(x=>x.textContent==='First Sheet'))") == "true";
                if(!firstSheet) await Task.Delay(75);
            }
            Require(firstSheet, "Midnight Dice achievement is absent from the React challenge board.");
            await using(FileStream capture = File.Create(Path.Combine(output,"dice-achievements.png")))
                await web.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,capture);
            await web.ExecuteScriptAsync("document.querySelector('.record-page .shop-back')?.click()");
            await Until(() => modern.RecordsPage is null);
        }
        else
        {
            await Task.Delay(100);
            ComboBox filter = Find<ComboBox>(window).Single(combo => combo.Name == "ChallengeCategoryFilter");
            filter.SelectedItem = filter.Items.Cast<object>().Single(item => item.ToString() == "Midnight Dice");
            await Task.Delay(60);
            Require(Find<TextBlock>(window).Any(text => text.Text.Contains("First Sheet", StringComparison.Ordinal)),
                "Dice achievements did not appear in WPF fallback challenges.");
            Capture(window, Path.Combine(output, "dice-achievements.png"));
            ClickLegacy(window, "ProgressionBackButton");
        }
        File.WriteAllText(Path.Combine(output, "dice-casino-result.txt"), "PASS: React casino game, native 100-entry, three independent AI seats, 13 committed score boxes, true placement payout, exact resume without repeat fees, idempotent XP, confirmed forfeit, ranked receipts, modern statistics and achievements.");
    }
    private static async Task OpenLobbyProgression(MainWindow window, string destination, string fallbackName)
    {
        ModernLobbyView? modern = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modern is null)
        {
            ClickLegacy(window, fallbackName);
            return;
        }

        string label = destination == "Statistics" ? "Statistics|Statistik" : "Challenges|Aufgaben";
        string expression = "[...document.querySelectorAll('.lobby-nav-item')]"
            + ".find(x=>/" + label + "/.test(x.textContent||''))";
        string found = await modern.Browser.CoreWebView2!.ExecuteScriptAsync("Boolean(" + expression + ")");
        Require(found.Equals("true", StringComparison.OrdinalIgnoreCase),
            "React lobby does not expose " + destination + " navigation.");
        await modern.Browser.CoreWebView2.ExecuteScriptAsync(expression + "?.click()");
        await Until(() => modern.RecordsPage == (destination == "Statistics" ? "statistics" : "challenges"));
        Require(modern.IsVisible, "React records did not remain in the shared lobby compositor.");
    }

    private static async Task Until(Func<bool> condition)
    {
        var time = Stopwatch.StartNew();
        while (!condition())
        {
            if (time.Elapsed > TimeSpan.FromSeconds(90)) throw new InvalidOperationException("Casino dice operation did not finish in time.");
            await Task.Delay(30);
        }
    }
    private static void ClickLegacy(DependencyObject root, string name)
    {
        Button button = Find<Button>(root).Single(b => b.Name == name);
        Require(button.IsEnabled && button.IsVisible, name + " is not available.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path); encoder.Save(file);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
