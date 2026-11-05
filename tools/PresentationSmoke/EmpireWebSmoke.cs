using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static class EmpireWebSmoke
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo Field(string name) => typeof(MainWindow).GetField(name, Flags)!;
    internal static EmpireService Fixture()
    {
        EmpireState state = EmpireState.Normalize(new() { Started = true, LastAccruedAt = DateTimeOffset.UtcNow,
            Prestige = 25, Credits = EmpireRules.MaximumCredits,
            Levels = EmpireCatalog.Departments.ToDictionary(r => r.Id, _ => 100),
            Research = new() { ["construction"] = 2, ["compound"] = 1, ["automation"] = 1 } });
        var estate = state.Properties[state.ActivePropertyId] with { Projects = new(["grand-entrance", "executive-offices", "resort-wing", "skyline-property"]),
            Levels = EmpireCatalog.Departments.ToDictionary(r => r.Id, _ => 100) };
        return new(state with { Properties = new(state.Properties) { [state.ActivePropertyId] = estate } });
    }
    internal static async Task Run(MainWindow window, string output, bool benchmark = false)
    {
        var old = (EmpireService)Field("empire").GetValue(window)!;
        var oldPreferences = (AppPreferences)Field("preferences").GetValue(window)!;
        UiLanguagePreference oldLanguage = BlackjackCasino.Desktop.Localization.Language;
        var originalSession = (CasinoSession)Field("session").GetValue(window)!;
        decimal originalBankroll = originalSession.Bankroll;
        var fixture = Fixture();
        Require(fixture.OpenRooms == 15, "Late-career fixture must retain all Resort levels.");
        Field("empire").SetValue(window, fixture);
        int nativeWindows = Application.Current.Windows.Count;
        try
        {
            NavigationSmoke.OpenCovered(window, "casino-empire");
            window.UpdateLayout();
            await Wait(() => Find<ModernEmpireView>(window).Any(), "Empire host did not mount after navigation.");
            var view = Find<ModernEmpireView>(window).Single();
            await Wait(() => view.Ready, "Modern Empire did not complete its native handshake.");
            Require(!Find<EmpireView>(window).Any(), "A legacy Empire view is rendering behind React.");
            Require(Application.Current.Windows.Count == nativeWindows, "Embedded Empire created another native window.");
            Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible, "Native header covers Empire.");
            Require(!((Button)window.FindName("DealButton")).IsEnabled, "Covered Blackjack controls remain active.");
            await Click(view, "floor-3");
            await Check(view, "document.querySelectorAll('.empire-gallery .empire-room').length===5", "Resort gallery lost a department.");
            await Click(view, "room-hotel");
            var viewports = new List<object>();
            foreach (var size in new[] { (1080d, 720d), (1366d, 768d), (1920d, 1080d) })
            {
                window.Width = size.Item1; window.Height = size.Item2; window.UpdateLayout(); await Task.Delay(150);
                // Hosted runners can clamp native windows to their virtual monitor.
                // Exercise exact CSS viewports separately and record both dimensions.
                await view.Browser.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride",
                    JsonSerializer.Serialize(new { width = (int)size.Item1, height = (int)size.Item2, deviceScaleFactor = 1, mobile = false }));
                await Task.Delay(100);
                await Check(view, $"innerWidth=={(int)size.Item1} && innerHeight=={(int)size.Item2}", "Emulated viewport dimensions were not applied.");
                viewports.Add(new { RequestedWidth = size.Item1, RequestedHeight = size.Item2,
                    NativeClientWidth = view.ActualWidth, NativeClientHeight = view.ActualHeight });
                await Check(view, "document.documentElement.scrollWidth<=innerWidth+1 && document.querySelectorAll('canvas').length===1 && document.querySelector('[data-testid=upgrade]').getBoundingClientRect().bottom<=innerHeight+1 && [...document.querySelectorAll('.empire-room')].every(e=>e.getBoundingClientRect().right<=innerWidth+1)", "Empire clips at " + size);
                string capture = await view.Browser.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{\"format\":\"png\",\"captureBeyondViewport\":true,\"fromSurface\":true}");
                using var image = JsonDocument.Parse(capture);
                File.WriteAllBytes(Path.Combine(output, "empire-react-" + (int)size.Item1 + ".png"), Convert.FromBase64String(image.RootElement.GetProperty("data").GetString()!));
            }
            await view.Browser.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.clearDeviceMetricsOverride", "{}");
            File.WriteAllText(Path.Combine(output, "empire-viewports.json"), JsonSerializer.Serialize(viewports, new JsonSerializerOptions { WriteIndented = true }));
            int before = fixture.Level("hotel");
            await Script(view, "document.querySelector('[data-testid=upgrade]').dispatchEvent(new FocusEvent('focusin',{bubbles:true})); true");
            await Task.Delay(250);
            Require(fixture.Level("hotel") == before, "A preview committed an upgrade.");
            await Click(view, "upgrade");
            await Wait(() => fixture.Level("hotel") == before + 1, "Upgrade did not reach C#.");
            await Click(view, "nav-research");
            await Check(view, "document.querySelector('[data-testid=research-automation]').disabled", "Research ignored its maximum rank.");
            await Click(view, "research-construction");
            await Wait(() => fixture.ResearchRank("construction") == 3, "Research did not commit.");
            await Click(view, "nav-projects"); await Click(view, "project-casino-vault");
            await Wait(() => fixture.Projects.Contains("casino-vault"), "Project purchase did not commit.");
            await Click(view, "nav-team");
            await Script(view, "const s=document.querySelector('select[aria-label=\"Elena Ward\"]');s.value='hotel';s.dispatchEvent(new Event('change',{bubbles:true}));true");
            await Click(view, "assign-elena");
            await Wait(() => fixture.ManagerAssignments.GetValueOrDefault("elena") == "hotel", "Specialist assignment did not commit.");
            await Click(view, "nav-automation");
            await Script(view, "const e=document.querySelector('input[name=target]');Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(e,'110');e.dispatchEvent(new Event('input',{bubbles:true}));true");
            fixture.Accrue(DateTimeOffset.UtcNow); view.RefreshLive(); await Task.Delay(150);
            await Check(view, "document.querySelector('input[name=target]').value==='110'", "Production refresh overwrote an unsaved automation form.");
            await Script(view, "document.querySelector('.empire-form input[type=checkbox]').click();true");
            await Click(view, "automation-save");
            await Wait(() => fixture.Automation.Enabled && fixture.Automation.TargetLevel == 110, "Automation policy did not commit.");
            await Click(view, "nav-events"); await Click(view, "event-blackjack");
            await Wait(() => fixture.ActiveEvent?.Id == "blackjack", "Event did not start.");
            await Click(view, "nav-contracts"); await Click(view, "contract-expansion-drive");
            await Wait(() => fixture.ActiveContract?.Id == "expansion-drive", "Contract did not start.");
            await Click(view, "nav-estate"); await Click(view, "floor-1"); await Click(view, "room-tables");
            await Capture(view, Path.Combine(output, "empire-react-main-floor.png"));
            await Script(view, "[...document.querySelectorAll('.empire-bulk button')].find(e=>e.textContent==='×50').click();true");
            await Click(view, "upgrade"); await Wait(() => fixture.CanClaimContract, "Construction did not advance the contract.");
            await Click(view, "nav-contracts"); await Click(view, "contract-claim");
            await Wait(() => fixture.ActiveContract is null && fixture.ContractBuff is not null, "Contract reward did not commit.");
            await Click(view, "nav-properties"); await Click(view, "property-neon-bay");
            await Wait(() => fixture.OwnedProperties.Count == 2, "Property purchase did not commit.");
            await Click(view, "property-neon-bay");
            await Wait(() => fixture.ActivePropertyId == "neon-bay", "Property selection did not commit.");
            await Click(view, "property-midnight-club");
            await Wait(() => fixture.ActivePropertyId == "midnight-club", "Original property was lost.");
            foreach (string route in new[] { "synergies", "statistics", "estate" }) await Click(view, "nav-" + route);
            fixture.Accrue(DateTimeOffset.UtcNow.AddHours(1)); view.Refresh(); await Task.Delay(150);
            var casino = (CasinoSession)Field("session").GetValue(window)!;
            decimal cash = casino.Bankroll, dividend = fixture.ClaimableDividend;
            Require(dividend > 0, "Dividend fixture has no reserve.");
            await Click(view, "claim-dividend");
            await Wait(() => casino.Bankroll == cash + dividend, "Dividend did not settle into the casino wallet.");
            await Check(view, "document.querySelector('[data-testid=claim-dividend]').disabled", "Empty reserve can be collected again.");
            BlackjackCasino.Desktop.Localization.SetLanguage(UiLanguagePreference.German); view.Refresh();
            await Check(view, "document.documentElement.lang==='de' && document.querySelector('[data-testid=nav-estate]').textContent.includes('Dein Anwesen')", "German Empire controls did not localize.");
            await Capture(view, Path.Combine(output, "empire-react-german.png"));
            BlackjackCasino.Desktop.Localization.SetLanguage(oldLanguage); view.Refresh();
            await Click(view, "overview");
            Require(fixture.CanPrestige, "Prestige fixture did not meet its requirements.");
            await Click(view, "prestige");
            await Script(view, "document.querySelector('.empire-confirm .empire-gold').click();true");
            await Wait(() => fixture.Prestige == 26, "Prestige did not reset the selected property.");
            Require(fixture.OwnedProperties.Count == 2 && fixture.ResearchRank("construction") == 3, "Prestige lost permanent portfolio progress.");
            var area = (AppPreferences)Field("preferences").GetValue(window)!;
            Field("preferences").SetValue(window, area.WithPresentation("casino-empire", area.Presentation("casino-empire") with { AnimationsEnabled = false }));
            view.Refresh(); await Task.Delay(150);
            await Check(view, "document.querySelector('.empire-pixi').dataset.ticker==='stopped'", "Animation-off did not stop Pixi.");
            await Click(view, "back");
            await Wait(() => !Find<ModernEmpireView>(window).Any(), "Back did not restore the lobby.");
            window.Modes.OpenSecondary("casino-empire");
            Window detached = Application.Current.Windows.Cast<Window>().Single(w => w != window && Find<ModernEmpireView>(w).Any());
            var detachedView = Find<ModernEmpireView>(detached).Single();
            await Wait(() => detachedView.Ready, "Detached modern Empire did not load.");
            detached.WindowState = WindowState.Minimized; await Task.Delay(150);
            await Check(detachedView, "document.querySelector('.empire-pixi').dataset.ticker==='stopped'", "Minimized Empire keeps animating.");
            detached.WindowState = WindowState.Normal; await Task.Delay(150);
            await Capture(detachedView, Path.Combine(output, "empire-react-detached.png"));
            detached.Close(); await Task.Delay(100);
            Require(Application.Current.Windows.Count == nativeWindows, "Empire leaked a detached window.");
            if (benchmark) await EmpirePerformanceSmoke.Run(window, output);
        }
        finally
        {
            typeof(MainWindow).GetMethod("CloseEmpire", Flags)!.Invoke(window, null);
            if (Field("empireDetachedWindow").GetValue(window) is Window detached) detached.Close();
            Field("empire").SetValue(window, old); Field("preferences").SetValue(window, oldPreferences);
            originalSession.ResetBankroll(originalBankroll); BlackjackCasino.Desktop.Localization.SetLanguage(oldLanguage);
        }
    }
    internal static async Task<string> Script(ModernEmpireView view, string script)
        => await view.Browser.CoreWebView2.ExecuteScriptAsync("(()=>{" + script + "})()");
    internal static async Task Check(ModernEmpireView view, string expression, string message)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(8))
        {
            if (await Script(view, "return !!(" + expression + ");") == "true") return;
            await Task.Delay(60);
        }
        throw new InvalidOperationException(message);
    }
    internal static async Task Click(ModernEmpireView view, string id)
    {
        await Check(view, "document.querySelector('main[aria-busy=true]')===null", "An Empire command is stuck.");
        string result = await Script(view, "const e=document.querySelector('[data-testid=" + id + "]');if(!e||e.disabled)return false;e.click();return true;");
        // Closing the view may dispose WebView2 before ExecuteScript's result is
        // delivered. The caller verifies the restored native lobby afterwards.
        if (id == "back") { await Task.Delay(100); return; }
        Require(result == "true", "Empire control is unavailable: " + id);
        await Task.Delay(100);
        await Check(view, "document.querySelector('main[aria-busy=true]')===null", "An Empire command was not acknowledged: " + id);
    }
    internal static async Task Wait(Func<bool> test, string message)
    {
        var clock = Stopwatch.StartNew();
        while (!test() && clock.Elapsed < TimeSpan.FromSeconds(20)) await Task.Delay(60);
        Require(test(), message);
    }
    internal static async Task Capture(ModernEmpireView view, string path)
    {
        await using var file = File.Create(path);
        await view.Browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
