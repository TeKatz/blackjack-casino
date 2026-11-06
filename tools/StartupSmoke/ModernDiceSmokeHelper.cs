using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static class ModernDiceSmokeHelper
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static KniffelGame Game(MainWindow host) =>
        (KniffelGame)typeof(MainWindow).GetField("modernDiceGame", Flags)!.GetValue(host)!;
    internal static ModernDiceSession Session(MainWindow host) =>
        (ModernDiceSession)typeof(MainWindow).GetField("modernDiceCommands", Flags)!.GetValue(host)!;

    internal static async Task<ModernDiceView> Open(MainWindow host)
    {
        NavigationSmoke.OpenCovered(host, "kniffel");
        return await WaitForView(host);
    }
    internal static async Task<ModernDiceView> WaitForView(MainWindow host)
    {
        ModernDiceView? view = null;
        for (int i = 0; i < 190; i++)
        {
            await Task.Delay(75);
            view = Find<ModernDiceView>(host).FirstOrDefault();
            if (view is { Ready: true } or { Failed: true }) break;
        }
        if (view is not { Ready: true, Failed: false, IsVisible: true })
        {
            string details = $"view={view is not null}; ready={view?.Ready}; failed={view?.Failed}; visible={view?.IsVisible}; "
                + $"frontendFile={ModernDiceView.IsAvailable}; webCore={view?.Browser.CoreWebView2 is not null}";
            if (view?.Browser.CoreWebView2 is { } diagnosticCore)
            {
                try
                {
                    string clientState = await diagnosticCore.ExecuteScriptAsync(
                        "(()=>JSON.stringify({url:location.href,load:document.readyState,"
                        + "root:!!document.getElementById('root'),game:!!document.querySelector('[data-testid=\\\"modern-dice-app\\\"]'),"
                        + "html:document.body?.innerText?.slice(0,450),scripts:document.scripts.length,"
                        + "snapshotDiagnostics:window.__diceBridgeDiagnostics??'no bridge snapshot received'}))()");
                    details += "; browser=" + clientState;
                }
                catch (Exception ex) when (ex is InvalidOperationException
                    or System.Runtime.InteropServices.COMException) { details += "; scriptError=" + ex.Message; }
            }
            throw new InvalidOperationException("The primary Midnight Dice route did not load: " + details);
        }
        Require(await JavaScript(view!.Browser.CoreWebView2!,
            "Boolean(document.querySelector('[data-testid=\"modern-dice-app\"]')"
            + "&&document.querySelectorAll('[data-testid^=\"dice-die-\"]').length===5"
            + "&&document.querySelectorAll('[data-testid^=\"dice-category-\"]').length===13)"),
            "The React dice table has no five dice or thirteen score categories.");
        return view!;
    }
    internal static async Task Click(CoreWebView2 web, string testId, bool requiresRevision = true)
    {
        long revision = await Revision(web);
        string selector = "[data-testid=" + JsonSerializer.Serialize(testId) + "]";
        string script = "(()=>{const element=document.querySelector("
            + JsonSerializer.Serialize(selector)
            + ");if(!element||element.disabled)return false;element.click();return true})()";
        // A revision can render from the authoritative snapshot before React has
        // processed the matching command receipt and cleared its pending lock.
        // Wait for the actual enabled control rather than racing the WebView2 event loop.
        bool clicked = false;
        for (int i = 0; i < 150 && !clicked; i++)
        {
            clicked = await JavaScript(web, script);
            if (!clicked) await Task.Delay(40);
        }
        if (!clicked)
        {
            string diagnostics = await web.ExecuteScriptAsync(
                "(()=>{const el=document.querySelector(" + JsonSerializer.Serialize(selector)
                + ");const app=document.querySelector('[data-testid=\\\"modern-dice-app\\\"]');"
                + "return {exists:!!el,disabled:el?.disabled,revision:app?.dataset.revision,"
                + "mode:app?.dataset.mode,status:document.querySelector('[data-testid=\\\"dice-notice\\\"]')?.textContent,"
                + "saving:document.querySelector('.md-status small')?.textContent}})()");
            throw new InvalidOperationException("Unavailable React dice action: " + testId
                + "; browser state: " + diagnostics);
        }
        if (requiresRevision)
        {
            for (int i = 0; i < 160; i++)
            {
                await Task.Delay(65);
                if (await Revision(web) > revision) return;
            }
            throw new InvalidOperationException("Dice action did not commit a new revision: " + testId);
        }
    }
    internal static async Task Select(CoreWebView2 web, string category)
    {
        await Click(web, "dice-category-" + category, false);
        Require(await JavaScript(web, "Boolean(document.querySelector('.md-category.selected'))"),
            "Selected score row was not visibly highlighted.");
    }
    internal static async Task SetAnimations(CoreWebView2 web, bool enabled)
    {
        await Click(web, "dice-settings", false);
        string script = "(()=>{const input=document.querySelectorAll('[data-testid=\"dice-settings-panel\"] input')[1];"
            + "if(!input||input.checked===" + (enabled ? "true" : "false") + ")return false;input.click();return true})()";
        long previous = await Revision(web);
        Require(await JavaScript(web, script), "Animation toggle did not render.");
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(65);
            if (await Revision(web) > previous) break;
        }
        Require(await Revision(web) > previous, "Animation preference did not persist to C#.");
        await Click(web, "dice-settings", false);
    }
    internal static async Task<long> Revision(CoreWebView2 web)
    {
        string raw = await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"modern-dice-app\"]')?.dataset.revision??'-1'");
        string? value = JsonSerializer.Deserialize<string>(raw);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) ? parsed : -1;
    }
    internal static async Task<bool> JavaScript(CoreWebView2 web, string code) =>
        await web.ExecuteScriptAsync(code) == "true";
    internal static async Task WaitFor(MainWindow host, Func<bool> condition, string message, int seconds = 45)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed.TotalSeconds >= seconds) throw new InvalidOperationException(message);
            await Task.Delay(65);
        }
    }
    internal static async Task WaitForExit(MainWindow host)
    {
        for (int i = 0; i < 100 && Find<ModernDiceView>(host).Any(v => v.IsVisible); i++)
            await Task.Delay(75);
        Require(!Find<ModernDiceView>(host).Any(v => v.IsVisible), "Midnight Dice did not return to the lobby.");
    }
    internal static async Task Capture(CoreWebView2 web, string output)
    {
        using FileStream stream = File.Create(output);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
    }
    internal static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        int count = root is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetChildrenCount(root) : 0;
        for (int i = 0; i < count; i++)
            foreach (T nested in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return nested;
    }
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
