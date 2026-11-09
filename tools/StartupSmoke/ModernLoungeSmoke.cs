using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Exercises the actual React Sudoku UI in WebView2. Existing per-game fallback smoke
/// still verifies the native WPF rollback until the Lounge host cleanup phase.
/// </summary>
internal static class ModernLoungeSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("chill-lounge");
        ModernLoungeView view = await UntilAsync(
            () => Find<ModernLoungeView>(window).FirstOrDefault(v =>
                v.Ready && v.IsVisible && v.Browser.CoreWebView2 is not null),
            "React Lounge never became ready.");
        CoreWebView2 web = view.Browser.CoreWebView2!;
        ModernWebViewBootstrapSmoke.RequireHardened(web, "React Lounge");
        Require(LoungeHostedGameRegistry.Snapshot.Count == 0,
            "Opening React Lounge must not register or instantiate WPF rollback modules.");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "8",
            "React Lounge must show all eight activities.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.chill-tales-backroom'))") == "true",
            "The adventure entry was lost during the Sudoku migration.");

        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.chill-tower-defense .lounge-bastion-preview'))") == "true",
            "The custom Tower Defense entry is missing.");
        await web.ExecuteScriptAsync("document.querySelector('.chill-tower-defense')?.click()");
        // Bastion now opens its own campaign menu instead of entering a battle
        // immediately. Exercise the real native bridge through map selection,
        // the seven-skill mastery screen and the battlefield before exiting.
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.td-stage-card[data-stage=forest]'))") == "true",
            "Midnight Bastion campaign map selection did not open.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.td-stage-card[data-stage=frost]'))") == "true",
            "Frostpass was missing from the Bastion map selection.");
        await Capture(web, Path.Combine(output, "modern-tower-defense-menu.png"));
        await ClickAndWait(web, ".td-mastery-summary button",
            "Boolean(document.querySelector('[data-testid=midnight-bastion-skills]'))",
            "Midnight Bastion mastery tree did not open.");
        Require(await web.ExecuteScriptAsync(
            "document.querySelectorAll('.td-skill-card').length") == "7",
            "Midnight Bastion did not show all seven mastery skills.");
        await ClickAndWait(web, ".td-skills-footer .td-skill-back",
            "Boolean(document.querySelector('.td-stage-card[data-stage=forest]'))",
            "Midnight Bastion mastery could not return to map selection.");
        await ClickAndWait(web, ".td-stage-card[data-stage=forest] .td-primary",
            "Boolean(document.querySelector('.td-map canvas'))",
            "Tower Defense PixiJS battlefield did not render after entering Forest Border.");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelector('.td-map')?.dataset.renderer === 'illustrated'") == "true",
            "The new Tower Defense illustrated SVG sprites did not finish loading.");
        await Capture(web, Path.Combine(output, "modern-tower-defense.png"));
        await ClickAndWait(web, ".chill-exit",
            "Boolean(document.querySelector('.td-stage-card[data-stage=forest]'))",
            "Tower Defense battlefield did not return to map selection.");
        await ClickAndWait(web, ".chill-exit",
            "document.querySelectorAll('.chill-card').length === 8",
            "Tower Defense map selection did not return to Chill Lounge.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.chill-minesweeper .lounge-sweep-preview'))") == "true",
            "The original Midnight Sweep featured tile is missing.");
        await web.ExecuteScriptAsync("document.querySelector('.chill-minesweeper')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sweep-difficulty').length") == "3",
            "Midnight Sweep three-difficulty selection failed.");
        await web.ExecuteScriptAsync("document.querySelector('.sweep-difficulty-easy')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sweep-tile-target').length") == "64",
            "The new React/PixiJS minefield failed to open.");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.sweep-pixi canvas'))") == "true",
            "Midnight Sweep PixiJS rendering did not mount.");
        await web.ExecuteScriptAsync("document.querySelectorAll('.sweep-tile-target')[27]?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sweep-tile-target.opened').length > 0") == "true",
            "Midnight Sweep native safe-first reveal never completed.");
        await Capture(web, Path.Combine(output, "modern-midnight-sweep.png"));
        await ClickAndWait(web, ".chill-exit",
            "document.querySelectorAll('.sweep-difficulty').length === 3",
            "Midnight Sweep did not return to its difficulty menu.");
        await ClickAndWait(web, ".chill-exit",
            "document.querySelectorAll('.chill-card').length === 8",
            "Midnight Sweep did not return to the Chill Lounge.");

        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.chill-filter button')]"
            + ".find(x=>/Puzzles|Rätsel/.test(x.textContent||''))?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "3",
            "The Lounge puzzle filter did not work.");
        await web.ExecuteScriptAsync("document.querySelector('.chill-filter button')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.chill-sudoku')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-mode-tile').length") == "6",
            "The six-mode Sudoku selection menu did not appear.");
        Require(await web.ExecuteScriptAsync(
            "new Set([...document.querySelectorAll('.sudoku-mode-tile img')].map(x=>x.getAttribute('src'))).size") == "6",
            "Sudoku variants must each have an independently named preview image.");
        Require(await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.sudoku-mode-tile img')].every(x=>x.complete&&x.naturalWidth>0)") == "true",
            "At least one Sudoku preview artwork failed to load.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\\\"sudoku-career\\\"]'))") == "true",
            "The Sudoku career overview was missing.");
        await Capture(web, Path.Combine(output, "modern-sudoku-modes.png"));
        await web.ExecuteScriptAsync("document.querySelector('.sudoku-career-overview')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-career-milestone').length") == "12",
            "Sudoku career did not show twelve milestone title rewards.");
        Require(await web.ExecuteScriptAsync(
            "(() => { const title = document.querySelector('.sudoku-mode-hero h1')?.textContent || '';"
            + " const reward = document.querySelector('.sudoku-career-milestone .sudoku-milestone-name')?.textContent;"
            + " return title.includes('Sechs') ? reward === 'Zahlenkenner' : reward === 'Number Adept'; })()") == "true",
            "Sudoku milestone title names do not follow the selected UI language.");
        await web.ExecuteScriptAsync("document.querySelector('.sudoku-career-modal-head > button')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.sudoku-mode-tile[data-testid*=killer]')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-variant-option').length") == "3",
            "Killer Sudoku does not expose its three difficulty choices.");
        Require(await web.ExecuteScriptAsync(
            "(() => { const hero = document.querySelector('.sudoku-mode-hero')?.getBoundingClientRect();"
            + " const panel = document.querySelector('.sudoku-variant-select')?.getBoundingClientRect();"
            + " return !!hero && !!panel && panel.width >= hero.width * 0.8; })()") == "true",
            "Sudoku variant selection is cramped instead of using the desktop menu width.");
        Require(await web.ExecuteScriptAsync(
            "window.innerWidth < 1450 || window.innerHeight < 850 ||"
            + " document.documentElement.scrollHeight <= window.innerHeight + 5") == "true",
            "The 1920x1080 Sudoku variant selection unexpectedly scrolls.");
        await web.ExecuteScriptAsync("document.querySelector('.sudoku-variant-option button.primary')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.sudoku-variant[data-mode=killer]'))") == "true",
            "Killer Sudoku did not start a native-generated board.");
        Require(await web.ExecuteScriptAsync(
            "document.querySelectorAll('.variant-cage-label').length > 20") == "true",
            "Killer Sudoku lost its visible sum cages.");
        Require(await web.ExecuteScriptAsync(
            "(() => { const board = document.querySelector('.variant-grid')?.getBoundingClientRect();"
            + " const panel = document.querySelector('.variant-board-panel')?.getBoundingClientRect();"
            + " return window.innerWidth < 1250 || !board || !panel ||"
            + " board.width >= panel.width * 0.56; })()") == "true",
            "Sudoku play grid is too small relative to the available desktop panel.");
        Require(await web.ExecuteScriptAsync(
            "window.innerWidth < 1450 || window.innerHeight < 850 ||"
            + " document.documentElement.scrollHeight <= window.innerHeight + 5") == "true",
            "The 1920x1080 Sudoku board unexpectedly scrolls.");
        await Capture(web, Path.Combine(output, "modern-sudoku-killer.png"));
        await web.ExecuteScriptAsync("document.querySelector('.variant-hint')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelector('.variant-timers span:last-child b')?.textContent==='1'") == "true",
            "Killer hint did not persist in the native board.");
        await web.ExecuteScriptAsync("document.querySelector('.variant-return')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.sudoku-variant-option button:not(.primary):not(:disabled)'))") == "true",
            "Unfinished Killer Sudoku could not be resumed.");
        await web.ExecuteScriptAsync("document.querySelector('.sudoku-mode-back')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-mode-tile').length") == "6",
            "Sudoku variant selection did not return to six modes.");

        // Each route must open a real board with mode-specific dimensions.
        foreach ((string mode, int expectedCells) in new[]
        {
            ("x", 81), ("jigsaw", 81), ("samurai", 369), ("mini", 16),
        })
        {
            await web.ExecuteScriptAsync(
                "document.querySelector('.sudoku-mode-tile[data-testid*=\"" + mode + "\"]')?.click()");
            await UntilAsync(async () => await web.ExecuteScriptAsync(
                "document.querySelectorAll('.sudoku-variant-option').length") ==
                    (mode == "mini" ? "6" : "3"),
                mode + " variant difficulty options are missing.");
            await web.ExecuteScriptAsync("document.querySelector('.sudoku-variant-option button.primary')?.click()");
            await UntilAsync(async () => await web.ExecuteScriptAsync(
                "document.querySelectorAll('.variant-cell').length") == expectedCells.ToString(),
                mode + " does not render its correct native board dimensions.");
            if (mode == "jigsaw")
            {
                Require(await web.ExecuteScriptAsync(
                    "(() => { const cell = document.querySelector('.variant-jigsaw .variant-cell');"
                    + " return !!cell && getComputedStyle(cell).borderTopColor === 'rgb(223, 168, 255)'"
                    + " && parseFloat(getComputedStyle(cell).borderTopWidth) >= 4; })()") == "true",
                    "Jigsaw region borders must be visibly violet and at least four pixels wide.");
            }
            await Capture(web, Path.Combine(output, "modern-sudoku-" + mode + ".png"));
            await web.ExecuteScriptAsync("document.querySelector('.variant-return')?.click()");
            await UntilAsync(async () => await web.ExecuteScriptAsync(
                "Boolean(document.querySelector('.sudoku-variant-options'))") == "true",
                mode + " could not return to its own difficulty menu.");
            await web.ExecuteScriptAsync("document.querySelector('.sudoku-mode-back')?.click()");
            await UntilAsync(async () => await web.ExecuteScriptAsync(
                "document.querySelectorAll('.sudoku-mode-tile').length") == "6",
                mode + " could not return to the Sudoku lounge.");
        }
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\\\"sudoku-mode-classic\\\"]')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-difficulty').length") == "6",
            "The modern Sudoku library did not appear.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"sudoku-daily\"]'))") == "true",
            "The offline Daily Sudoku route is missing.");
        await Capture(web, Path.Combine(output, "modern-sudoku-library.png"));

        // Native fallback Sudoku smoke runs earlier in the same test profile; a
        // fresh Relaxed puzzle avoids changing that test's expected saved states.
        await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"sudoku-new-Relaxed\"]')?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"modern-sudoku-game\"]'))") == "true",
            "React Sudoku did not open a C#-authored board.");
        Require(await web.ExecuteScriptAsync(
            "document.querySelectorAll('.sudoku-cell').length") == "81",
            "The 9x9 React Sudoku board does not contain 81 cells.");
        await Capture(web, Path.Combine(output, "modern-sudoku-board.png"));

        string? id = JsonSerializer.Deserialize<string>(await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"modern-sudoku-game\"]').dataset.puzzleId"));
        Require(id is not null, "The React Sudoku view omitted the puzzle ID.");
        SudokuPuzzle puzzle = SudokuLibrary.Get(id!);
        Require(SudokuSolver.TrySolve(puzzle.Givens, out string solved),
            "The Sudoku smoke puzzle cannot be solved.");
        int editable = Enumerable.Range(0, 81).First(i => puzzle.Givens[i] == '0');
        int answer = solved[editable] - '0';
        int wrong = answer == 9 ? 1 : answer + 1;

        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"sudoku-cell-"
            + editable + "\"]')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"sudoku-pencil\"]')?.click()");
        await ClickAndWait(web, "[data-testid=\"sudoku-digit-" + wrong + "\"]",
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable +
            "\"] .sudoku-notes')?.textContent?.includes('" + wrong + "')===true",
            "The pencil mark was not rendered.");
        await ClickAndWait(web, "[data-testid=\"sudoku-undo\"]",
            "!document.querySelector('[data-testid=\"sudoku-cell-" + editable +
            "\"] .sudoku-notes')?.textContent?.includes('" + wrong + "')",
            "Undo did not restore the previous Sudoku frame.");
        await ClickAndWait(web, "[data-testid=\"sudoku-redo\"]",
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable +
            "\"] .sudoku-notes')?.textContent?.includes('" + wrong + "')===true",
            "Redo did not restore the pencil mark.");

        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"sudoku-pencil\"]')?.click()");
        await ClickAndWait(web, "[data-testid=\"sudoku-digit-" + wrong + "\"]",
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable
            + "\"]')?.classList.contains('incorrect')===true",
            "Native C# solver did not flag the wrong value.");
        await ClickAndWait(web, "[data-testid=\"sudoku-erase\"]",
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable
            + "\"]')?.textContent?.trim()==='' && !document.querySelector('[data-testid=\"sudoku-back\"]')?.disabled",
            "Erase did not clear the selected cell and release the Sudoku command lock.");
        await ClickAndWait(web, "[data-testid=\"sudoku-hint\"]",
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable
            + "\"]')?.textContent?.trim()==='" + answer + "'",
            "A native C# hint did not correct the selected cell.");
        Require(await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"sudoku-hints\"]')?.textContent==='1'") == "true",
            "The C# hint counter did not update.");

        await ClickAndWait(web, "[data-testid=\"sudoku-back\"]",
            "Boolean(document.querySelector('[data-testid=\"sudoku-resume-Relaxed\"]:not(:disabled)'))",
            "React Sudoku did not retain its unfinished saved puzzle.");
        await ClickAndWait(web, "[data-testid=\"sudoku-resume-Relaxed\"]",
            "Boolean(document.querySelector('[data-testid=\"modern-sudoku-game\"]'))",
            "Sudoku resume did not restore the saved board.");
        Require(await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"sudoku-cell-" + editable +
            "\"]')?.textContent?.trim()==='" + answer + "'") == "true",
            "The saved hint result was lost when resuming.");

        await ClickAndWait(web, "[data-testid=\"sudoku-back\"]",
            "Boolean(document.querySelector('[data-testid=\"sudoku-library\"]'))",
            "Sudoku could not return to the puzzle library.");
        await ClickAndWait(web, ".chill-exit",
            "Boolean(document.querySelector('.chill-card'))",
            "The Sudoku library could not return to React Lounge.");
        await ModernSolitaireSmoke.Run(web, output);
        await ModernWordSearchSmoke.Run(window, web, output);
        await ModernMemorySmoke.Run(window, web, output);
        await ModernJigsawSmoke.Run(window, web, output);
        await ModernAdventureSmoke.Run(window, web, output);
        await DispatchNavigation(web, "document.querySelector('.chill-exit')?.click()");
        await UntilAsync(() => Find<ModernLoungeView>(window).All(v => !v.IsVisible),
            "React Lounge could not return to the casino.");

        File.WriteAllText(Path.Combine(output, "modern-lounge-result.txt"),
            "PASS: React eight-game Lounge, Midnight Bastion campaign menu, seven mastery skills, Forest Border Pixi battlefield and two-step exit; six Sudoku difficulty levels and Daily; 81 cells; notes; undo/redo; wrong-value C# feedback; native hint; persisted resume; navigation.");
    }

    private static async Task Capture(CoreWebView2 web, string path)
    {
        using FileStream stream = File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
    }

    private static async Task ClickAndWait(CoreWebView2 web, string selector, string predicate, string error)
    {
        string clicked = await web.ExecuteScriptAsync(
            "(()=>{const e=document.querySelector(" + JsonSerializer.Serialize(selector)
            + ");if(!e||e.disabled)return false;e.click();return true})()");
        Require(clicked == "true", "Sudoku action unavailable: " + selector);
        await UntilAsync(async () => await web.ExecuteScriptAsync(predicate) == "true", error);
    }

    private static async Task DispatchNavigation(CoreWebView2 web, string script)
    {
        try { _ = await web.ExecuteScriptAsync(script); }
        catch (Exception ex) when (ex is InvalidOperationException
            or System.Runtime.InteropServices.COMException) { }
    }

    private static async Task<T> UntilAsync<T>(Func<T?> get, string message) where T : class
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            T? value = get();
            if (value is not null) return value;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(message);
    }

    private static async Task UntilAsync(Func<bool> condition, string message)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (condition()) return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(message);
    }

    private static async Task UntilAsync(Func<Task<bool>> condition, string message)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (await condition()) return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(message);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        if (root is not Visual and not System.Windows.Media.Media3D.Visual3D) yield break;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
