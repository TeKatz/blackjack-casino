using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Actual React/WebView2 lounge route and legacy handoff verification.
/// Older per-game smokes separately exercise the native rollback implementation.
/// </summary>
internal static class ModernLoungeSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("chill-lounge");
        ModernLoungeView modern = await UntilAsync(
            () => Find<ModernLoungeView>(window).FirstOrDefault(view =>
                view.Ready && view.IsVisible && view.Browser.CoreWebView2 is not null),
            "React Lounge never became ready.");
        CoreWebView2 browser = modern.Browser.CoreWebView2!;
        await UntilAsync(async () =>
        {
            string count = await browser.ExecuteScriptAsync(
                "document.querySelectorAll('.chill-card').length");
            return count == "6";
        }, "React Lounge did not render all six launchable activities.");
        Require(await browser.ExecuteScriptAsync(
            "document.querySelector('.chill-tales-backroom')!==null") == "true",
            "Adventure library is missing from the React Lounge.");

        string filtered = await browser.ExecuteScriptAsync(
            "(()=>{const b=[...document.querySelectorAll('.chill-filter button')]"
            + ".find(x=>/Puzzles|Rätsel/.test(x.textContent||''));b?.click();"
            + "return !!b;})()");
        Require(filtered == "true", "React Lounge does not expose its puzzle filter.");
        await UntilAsync(async () => await browser.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "3",
            "Lounge filter did not show exactly three puzzles.");
        await browser.ExecuteScriptAsync(
            "document.querySelector('.chill-filter button')?.click()");
        await UntilAsync(async () => await browser.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "6",
            "Lounge filter did not restore all six activities.");

        await DispatchNavigation(browser, "document.querySelector('.chill-sudoku')?.click()");
        await UntilAsync(() => Find<Button>(window).Any(button =>
            button.IsVisible && button.Name == "SudokuNew_Medium"), 
            "React Sudoku tile did not open the existing saved C# game route.");
        Require(Find<ModernLoungeView>(window).All(view => !view.IsVisible),
            "React Lounge remained interactive over the native game.");

        Button back = Find<Button>(window).Single(button =>
            button.Name == "LoungeGamesHeaderBack" && button.IsVisible);
        back.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        ModernLoungeView returned = await UntilAsync(
            () => Find<ModernLoungeView>(window).FirstOrDefault(view =>
                view.Ready && view.IsVisible && view.Browser.CoreWebView2 is not null),
            "The native Games action did not reopen the React Lounge.");
        Require(await returned.Browser.CoreWebView2!.ExecuteScriptAsync(
            "document.querySelectorAll('.chill-card').length") == "6",
            "The returned React Lounge lost activity navigation.");
        await DispatchNavigation(returned.Browser.CoreWebView2!,
            "document.querySelector('.chill-exit')?.click()");
        await UntilAsync(() => Find<ModernLoungeView>(window).All(view => !view.IsVisible),
            "React Lounge could not return to the casino lobby.");

        File.WriteAllText(Path.Combine(output, "modern-lounge-result.txt"),
            "PASS: React six-game activity hub; puzzle filter; native Sudoku handoff; return to React; return to casino.");
    }

    private static async Task DispatchNavigation(CoreWebView2 browser, string script)
    {
        // A working click may dispose its own WebView2 before its JS result arrives.
        // Dispatch once, then prove navigation by observing the native destination.
        try { _ = await browser.ExecuteScriptAsync(script); }
        catch (Exception ex) when (ex is InvalidOperationException
            or System.Runtime.InteropServices.COMException) { }
    }

    private static async Task<T> UntilAsync<T>(Func<T?> get, string message) where T : class
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(20))
        {
            T? value = get();
            if (value is not null) return value;
            await Task.Delay(130);
        }
        throw new InvalidOperationException(message);
    }

    private static async Task UntilAsync(Func<bool> condition, string message)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(20))
        {
            if (condition()) return;
            await Task.Delay(130);
        }
        throw new InvalidOperationException(message);
    }

    private static async Task UntilAsync(Func<Task<bool>> condition, string message)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(20))
        {
            if (await condition()) return;
            await Task.Delay(130);
        }
        throw new InvalidOperationException(message);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (int index=0; index<VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root,index)))
                yield return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
