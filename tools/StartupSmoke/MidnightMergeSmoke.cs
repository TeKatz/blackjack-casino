using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static class MidnightMergeSmoke
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task Run(MainWindow window, string output)
    {
        NavigationSmoke.OpenCovered(window, "midnight-merge");
        ModernMergeView view = await WaitForView(window);
        Require(view is { Ready: true, Failed: false, IsVisible: true },
            "The packaged Midnight Merge React workshop did not open.");
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
            "Midnight Merge opened a second native window.");

        CoreWebView2 web = view.Browser.CoreWebView2!;
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"midnight-merge-app\"]')"
            + "&&document.querySelectorAll('.merge-cell').length===42"
            + "&&document.querySelectorAll('.merge-producer').length===2"
            + "&&document.querySelectorAll('.merge-order').length>=6"
            + "&&document.querySelector('[data-testid=\"merge-next-upgrade\"]')"
            + "&&document.querySelector('.merge-room-art'))") == "true",
            "React Merge lacks its 7x6 workshop, supply sources, orders or panorama.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"merge-next-upgrade\"]')"
            + "?.textContent.includes('First cleanup'))") == "true",
            "React Merge lost the initial restoration milestone.");
        MergeGameState initial = State(window);
        Require(initial.Items.Count == 0, "Fresh Merge board should be empty.");
        await Capture(web, Path.Combine(output, "merge-modern-initial.png"));

        await Action(web, window, "[data-testid=\"merge-producer-chip-case\"]", 1);
        await Action(web, window, "[data-testid=\"merge-producer-chip-case\"]", 2);
        Require(State(window).Items.Count == 2 && State(window).Items.All(x => x.DefinitionId == "chip-1"),
            "React supply source did not create two native first-tier chips.");
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"merge-cell-0\"]')?.click()");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"merge-selected\"]'))") == "true",
            "React Merge click-to-select fallback is missing.");
        await Action(web, window, "[data-testid=\"merge-cell-1\"]", 3);
        Require(State(window).Items.Single().DefinitionId == "chip-2"
            && State(window).TotalMerges == 1, "React Merge did not merge the native chip identities.");
        await Capture(web, Path.Combine(output, "merge-modern-tier2.png"));

        await Action(web, window, "[data-testid=\"merge-deliver-first-stack\"]", 4);
        Require(State(window).RenovationPoints == 10 && State(window).Items.Count == 0,
            "First order did not consume the exact chip and award ten renovation points.");
        await Action(web, window, "[data-testid=\"merge-producer-chip-case\"]", 5);
        await Action(web, window, "[data-testid=\"merge-producer-chip-case\"]", 6);
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"merge-cell-0\"]')?.click()");
        await Action(web, window, "[data-testid=\"merge-cell-1\"]", 7);
        await Action(web, window, "[data-testid=\"merge-deliver-first-stack\"]", 8);
        Require(State(window).RenovationPoints == 20
            && State(window).UnlockedMilestones.Contains("lobby/cleanup"),
            "React order completion did not unlock the persisted cleanup milestone.");
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"merge-next-upgrade\"]')"
            + "?.textContent.includes('Lights restored'))") == "true",
            "React restoration did not advance to the next milestone.");

        await Action(web, window, "[data-testid=\"merge-producer-chip-case\"]", 9);
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"merge-cell-0\"]')?.click()");
        await Action(web, window, "[data-testid=\"merge-cell-41\"]", 10);
        Require(State(window).Items.Single().Position == 41, "React click fallback did not move a native item.");

        MergeGameState before = State(window);
        string serialized = JsonSerializer.Serialize(before);
        await web.ExecuteScriptAsync("document.querySelector('[data-testid=\"merge-back\"]')?.click()");
        await WaitForExit(window);
        Require(Find<CasinoLobbyView>(window).Any(x => x.IsVisible),
            "Returning from Midnight Merge did not restore the casino lobby.");

        NavigationSmoke.OpenCovered(window, "midnight-merge");
        view = await WaitForView(window);
        web = view.Browser.CoreWebView2!;
        Require(JsonSerializer.Serialize(State(window)) == serialized,
            "Closing and reopening React Merge changed IDs, active orders or the saved board.");
        string path = Path.Combine(output, "merge-save-roundtrip");
        PlayerStateStore.SaveOrThrow(new AppPreferences { MidnightMerge = before }, path);
        AppPreferences loaded = PlayerStateStore.Load(path);
        Require(JsonSerializer.Serialize(new MergeBoard(loaded.MidnightMerge).Snapshot) == serialized,
            "Atomic disk save and reload changed persisted Midnight Merge data.");
        await Capture(web, Path.Combine(output, "merge-modern-resumed.png"));

        Localization.SetLanguage(UiLanguagePreference.German);
        for (int i = 0; i < 70; i++)
        {
            await Task.Delay(65);
            if (await web.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"midnight-merge-app\"]')"
                + "?.dataset.language==='de'") == "true") break;
        }
        Require(await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"merge-area\"]')"
            + "?.textContent==='Lobby-Restaurierung' && document.querySelector('.merge-section-heading')"
            + "&&document.body.textContent.includes('LIEFERAUFTRÄGE')"
            + "&&document.querySelector('[data-testid=\"merge-next-upgrade\"]')"
            + "?.textContent.includes('Beleuchtung restauriert'))") == "true",
            "React Merge did not rerender the native German resource translations.");
        await Capture(web, Path.Combine(output, "merge-modern-german.png"));

        window.Width = 1080; window.Height = 720; window.UpdateLayout();
        await Task.Delay(180);
        Require(await web.ExecuteScriptAsync(
            "(()=>{const board=document.querySelector('[data-testid=\"merge-board\"]');"
            + "const cells=document.querySelectorAll('.merge-cell');"
            + "return Boolean(board&&cells.length===42&&board.getBoundingClientRect().width>=290"
            + "&&cells[41].getBoundingClientRect().width>=24)})()") == "true",
            "React Merge 7x6 table became unusable at 1080×720.");
        await Capture(web, Path.Combine(output, "merge-modern-german-1080x720.png"));

        Localization.SetLanguage(UiLanguagePreference.English);
        await Task.Delay(120);
        Require(JsonSerializer.Serialize(State(window)) == serialized,
            "Changing language or DPI mutated authoritative Merge state.");

        // A WebView2 crash should retain the exact already-persisted C# board.
        object retained = typeof(MainWindow).GetField("modernMergeBoard", Flags)!.GetValue(window)!;
        typeof(ModernMergeView).GetMethod("Fail", Flags)!.Invoke(view, null);
        await WaitForExit(window);
        NavigationSmoke.OpenCovered(window, "midnight-merge");
        view = await WaitForView(window);
        Require(ReferenceEquals(retained, typeof(MainWindow).GetField("modernMergeBoard", Flags)!.GetValue(window))
            && JsonSerializer.Serialize(State(window)) == serialized,
            "Browser recovery replayed orders or discarded the persisted Merge board.");
        await view.Browser.CoreWebView2!.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"merge-back\"]')?.click()");
        await WaitForExit(window);
        window.Width = 1280; window.Height = 900;
        File.WriteAllText(Path.Combine(output, "merge-result.txt"),
            "PASS: React 7x6 board, two native producers, click select/move/merge, order receipts, XP milestone, atomic disk save, exact reopen, German and English, 1080x720, WebView2 failure recovery.");
    }

    private static MergeGameState State(MainWindow window)
    {
        var board = (MergeBoard)typeof(MainWindow).GetField("modernMergeBoard", Flags)!.GetValue(window)!;
        return board.Snapshot;
    }

    private static async Task Action(CoreWebView2 web, MainWindow window, string selector, long expectedRevision)
    {
        string script = "document.querySelector(" + JsonSerializer.Serialize(selector)
            + ")?.click()";
        await web.ExecuteScriptAsync(script);
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(75);
            if (State(window).Revision == expectedRevision &&
                await web.ExecuteScriptAsync(
                    "document.querySelector('[data-testid=\"midnight-merge-app\"]')"
                    + "?.dataset.revision===" + JsonSerializer.Serialize(expectedRevision.ToString())) == "true") return;
        }
        throw new InvalidOperationException($"Midnight Merge React intent did not commit revision {expectedRevision}: {selector}");
    }

    private static async Task<ModernMergeView> WaitForView(MainWindow window)
    {
        ModernMergeView? view = null;
        for (int i = 0; i < 160; i++)
        {
            await Task.Delay(75);
            view = Find<ModernMergeView>(window).FirstOrDefault();
            if (view is { Ready: true } or { Failed: true }) break;
        }
        Require(view is { Ready: true, Failed: false, IsVisible: true },
            "React Midnight Merge did not become ready.");
        return view!;
    }

    private static async Task WaitForExit(MainWindow window)
    {
        for (int i = 0; i < 90 && Find<ModernMergeView>(window).Any(x => x.IsVisible); i++)
            await Task.Delay(75);
        Require(!Find<ModernMergeView>(window).Any(x => x.IsVisible),
            "Midnight Merge WebView2 remained hosted after return.");
    }
    private static async Task Capture(CoreWebView2 web, string path)
    {
        using FileStream file = File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Require(bool ok, string reason)
    {
        if (!ok) throw new InvalidOperationException(reason);
    }
}
