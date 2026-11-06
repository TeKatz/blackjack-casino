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
        Require(LoungeHostedGameRegistry.Snapshot.Count == 0,
            "Opening React Lounge must not register or instantiate WPF rollback modules.");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "6",
            "React Lounge must show all six activities.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.chill-tales-backroom'))") == "true",
            "The adventure entry was lost during the Sudoku migration.");

        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.chill-filter button')]"
            + ".find(x=>/Puzzles|Rätsel/.test(x.textContent||''))?.click()");
        await UntilAsync(async () => await web.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "3",
            "The Lounge puzzle filter did not work.");
        await web.ExecuteScriptAsync("document.querySelector('.chill-filter button')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.chill-sudoku')?.click()");
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
            "PASS: React six-game Lounge; six Sudoku difficulty levels and Daily; 81 cells; notes; undo/redo; wrong-value C# feedback; native hint; persisted resume; navigation.");
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
