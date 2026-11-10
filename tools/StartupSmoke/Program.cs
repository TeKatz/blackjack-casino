using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/startup-smoke");
        Directory.CreateDirectory(output);

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoControls.xaml", UriKind.Relative),
        });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoTooltips.xaml", UriKind.Relative),
        });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoWindow.xaml", UriKind.Relative),
        });
        CasinoWindowFrame.Install();
        Localization.InstallAutomaticTranslation();
        CasinoCursors.Install();

        int exit = 0;
        app.DispatcherUnhandledException += (_, e) =>
        {
            File.WriteAllText(Path.Combine(output, "dispatcher-failure.txt"), e.Exception.ToString());
            exit = 1;
            e.Handled = true;
            app.Shutdown(exit);
        };

        var profile = new PlayerProfileState
        {
            DisplayName = "Startup Smoke",
            Coins = 10_000,
            SlotTrackingVersion = 1,
            Vault = new CasinoVaultState
            {
                SchemaVersion = CasinoVaultService.CurrentSchemaVersion,
                TierId = "gold",
                CurrentAmount = 50_000m,
                LifetimeContributions = 50_000m,
            },
        };
        // Seed one completed, unclaimed challenge so the persistent header notification is exercised.
        profile.MidnightMerge.Merges = 1;

        var preferences = new AppPreferences
        {
            Bankroll = 50_000m,
            SoundEnabled = false,
            MusicEnabled = false,
            AnimationsEnabled = false,
            WindowWidth = 1280,
            WindowHeight = 900,
        };

        var window = new MainWindow(preferences, profile);
        window.ContentRendered += async (_, _) =>
        {
            try
            {
                await Task.Delay(180);
                window.UpdateLayout();
                Require(window.IsVisible, "Main window never became visible.");
                Require(window.FindName("TableSurface") is null &&
                    window.FindName("BlackjackSidebar") is null &&
                    window.FindName("DealButton") is null,
                    "Retired native Blackjack controls must not be part of the window.");
                Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,
                    "The old Blackjack header is visible above the startup lobby.");
                ModernGameSurfaceSmoke.Run();
                BrowserRecoverySmoke.Run();
                CheckWelcomeOverlay(window, profile, output);
                await VerifyPackagedModernLobby(window, output);
                RequireNoLegacyLobbyFrame(window, "startup");
                Require(window.Icon is not null && window.Template.FindName("CasinoTitleBar", window) is FrameworkElement { IsVisible: true }, "The desktop icon or custom title bar is missing.");
                await WindowFrameSmoke.Run(window);
                CasinoWindowGeometrySmoke.Run(window);
                object originalContent = window.Content;
                CasinoWindowFrame.SetIsFullscreen(window, true); window.UpdateLayout();
                Require(window.Template.FindName("CasinoTitleBar", window) is FrameworkElement { IsVisible: false }, "Full screen retained the custom title bar.");
                CasinoWindowFrame.SetIsFullscreen(window, false); window.UpdateLayout();
                Require(ReferenceEquals(originalContent, window.Content), "Window frame replaced the existing game content.");
                Require(window.Cursor == CasinoCursors.Arrow, "Main window did not install the custom casino cursor.");
                Require(!ReferenceEquals(CasinoCursors.Hand, System.Windows.Input.Cursors.Hand),
                    "Interactive casino cursor regressed to the stock Windows hand instead of the custom pointing glove.");
                Require(!ReferenceEquals(CasinoCursors.Hand, CasinoCursors.Arrow),
                    "Interactive casino cursor must remain distinct from the default casino arrow.");
                Require(Application.Current.TryFindResource(typeof(ToolTip)) is Style,
                    "Global casino tooltip style is not available to application controls.");
                // The React lobby is the authoritative visible navigation.
                // Its challenge badge and Vault route are checked below. The native
                // Blackjack header is intentionally collapsed until table mode.
                ModernLobbyView reactLobby = Find<ModernLobbyView>(window).Single();
                string clubNavigation = await reactLobby.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "(()=>{const links=[...document.querySelectorAll('.lobby-nav-item')].map(x=>x.textContent);"
                    + "const ready=document.querySelector('.nav-ready')?.textContent||'0';"
                    + "return links.some(x=>x.includes('Challenges'))&&links.some(x=>x.includes('Vault'))"
                    + "&&links.some(x=>x.includes('Season Pass'))&&Number(ready)>0;})()");
                Require(clubNavigation.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Compact React lobby hides claimable rewards or Vault navigation.");
                Require(profile.MidnightMerge.Merges > 0,
                    "Startup test profile must retain its seeded challenge progress.");
                SaveScreenshot(window, Path.Combine(output, "global-header-polish.png"));

                Require(window.Modes.Modes.Any(mode => mode.Id == "nine-lives"), "Nine Lives was not registered in the lobby.");

                // The approved Manor replacement has a single primary lobby action.
                // Verify that we enter the modern WebView2 presentation, rather than
                // relying on a retired secondary playtest or WPF legacy selector.
                // This UI smoke does not wager; xUnit covers transactional settlement.
                Require(ModernManorPreviewWindow.IsAvailable, "Modern Manor assets were not copied to the smoke runner.");
                Require(window.Modes.Modes.Count(mode => mode.Id == "midnight-manor") == 1,
                    "The lobby must register exactly one Midnight Manor game.");
                Require(window.Modes.CanOpen("midnight-manor") && !window.Modes.HasSecondary("midnight-manor"),
                    "Midnight Manor must open solely through the primary lobby action.");
                Require(Find<Button>(window).Count(button => button.Name == "Mode_midnight_manor") == 1
                    && !Find<Button>(window).Any(button => button.Name == "ModeSecondary_midnight_manor"),
                    "Midnight Manor has duplicate player-selectable buttons.");
                window.Modes.Open("midnight-manor");
                Require(window.Title == "Midnight Manor · Blackjack Casino",
                    "The primary Manor action did not activate the modern game host.");
                await Task.Delay(200);
                // The modern lobby has its own WebView2; target the embedded Manor browser explicitly.
                WebView2 modernBrowser = Find<WebView2>(window).Single(browser => browser.Name == "ModernManorBrowser");
                var modernClock = Stopwatch.StartNew();
                string modernTitle = string.Empty;
                while (modernClock.Elapsed < TimeSpan.FromSeconds(15))
                {
                    if (modernBrowser.CoreWebView2 is not null)
                    {
                        try
                        {
                            modernTitle = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                                "document.querySelector('h1')?.textContent || ''");
                        }
                        catch (InvalidOperationException) { }
                    }
                    if (modernTitle.Contains("MIDNIGHT MANOR", StringComparison.OrdinalIgnoreCase))
                        break;
                    await Task.Delay(150);
                }
                Require(modernTitle.Contains("MIDNIGHT MANOR", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor WebView2 did not load its React interface.");
                // React can render its heading before the journal-backed game snapshot
                // arrives. Also respect the saved German UI language instead of checking
                // only the English CTA on the first animation frame.
                string modernGameplay = "false";
                var gameplayClock = Stopwatch.StartNew();
                while (gameplayClock.Elapsed < TimeSpan.FromSeconds(15))
                {
                    modernGameplay = await modernBrowser.CoreWebView2!.ExecuteScriptAsync(
                        "Array.from(document.querySelectorAll('.gold-button')).some(button => " +
                        "/SPIN THE REELS|PLAY FREE SPIN|WALZEN DREHEN|FREISPIN SPIELEN/i.test(button.textContent || ''))");
                    if (modernGameplay.Equals("true", StringComparison.OrdinalIgnoreCase)) break;
                    await Task.Delay(150);
                }
                Require(modernGameplay.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor did not expose gameplay controls after loading.");
                string polishedControls = await modernBrowser.CoreWebView2!.ExecuteScriptAsync(
                    "(()=>{const bets=[...document.querySelectorAll('.preset-bets .bet-chip')];" +
                    "const auto=[...document.querySelectorAll('.auto-buttons .auto-chip')];" +
                    "return bets.length===6 && auto.length===4" +
                    "&& bets.some(b=>b.textContent?.includes('5,000'))" +
                    "&& document.querySelector('#manor-bet')?.readOnly===true" +
                    "&& !document.body.innerText.includes('C#');})()");
                Require(polishedControls.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "The Manor must expose six fixed stakes, auto-spins, and no technical C# text.");
                // The DOM title alone is not enough: require the real Pixi canvas to
                // initialize in the packaged WebView2 build and capture it for review.
                var canvasClock = Stopwatch.StartNew();
                string canvasReady = string.Empty;
                while (canvasClock.Elapsed < TimeSpan.FromSeconds(12))
                {
                    canvasReady = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "Boolean(document.querySelector('.manor-canvas canvas'))");
                    if (canvasReady.Equals("true", StringComparison.OrdinalIgnoreCase)) break;
                    await Task.Delay(150);
                }
                Require(canvasReady.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor PixiJS canvas did not initialize in the Windows WebView2 host.");
                // Readiness includes artwork loading; no black flash or late
                // illustration pop-in should be visible when the splash is lifted.
                var splashShell=Application.Current.Windows.OfType<ModernManorPreviewWindow>().Single();
                var splashField=typeof(ModernManorPreviewWindow).GetField("splash",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var splashPanel=(Border)splashField.GetValue(splashShell)!;
                var bootClock=Stopwatch.StartNew();
                while (bootClock.Elapsed < TimeSpan.FromSeconds(12)
                    && splashPanel.Visibility!=Visibility.Collapsed)
                    await Task.Delay(120);
                Require(splashPanel.Visibility==Visibility.Collapsed && modernBrowser.IsVisible,
                    "The branded Manor boot cover did not transition to the ready WebView2 game.");
                // One-page desktop casino: the whole playing surface stays in the viewport.
                string noPageScroll = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const root=document.querySelector('.manor-app');" +
                    "const surface=document.querySelector('.slot-viewport');" +
                    "return Boolean(root&&surface&&surface.getBoundingClientRect().height>160" +
                    "&&document.documentElement.scrollHeight<=innerHeight+2" +
                    "&&document.body.scrollHeight<=innerHeight+2);})()");
                Require(noPageScroll.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor requires page scrolling to reach gameplay on the Windows host.");
                string largerControls = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const spin=document.querySelector('.action-console .gold-button');" +
                    "const stake=document.querySelector('.action-console .bet-input');" +
                    "if(!spin||!stake)return false;" +
                    "const a=spin.getBoundingClientRect(),b=stake.getBoundingClientRect();" +
                    "const minimum=innerHeight<=720?45:54;" +
                    "return a.width>=190&&a.height>=minimum&&b.height>=minimum-7" +
                    "&&a.bottom<=innerHeight&&b.bottom<=innerHeight;})()");
                Require(largerControls.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor stake or primary spin control is small or clipped in Windows WebView2.");
                string canvasGeometry = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const host=document.querySelector('.manor-canvas');" +
                    "if(!host)return false;const rect=host.getBoundingClientRect();" +
                    "return rect.width>100&&Math.abs(rect.height/rect.width-500/960)<0.06;})()");
                Require(canvasGeometry.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor reel stage is stretched vertically or letterboxed.");
                // Debug metrics are opt-in and never require a wager or touch a save.
                string diagnosticsControl = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const input=document.querySelector('#manor-perf-diagnostics');" +
                    "return Boolean(input && input.type==='checkbox' && !input.checked);})()");
                Require(diagnosticsControl.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor renderer diagnostics must start disabled and remain optional.");
                string responsiveReel = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const canvas=document.querySelector('.manor-canvas canvas');" +
                    "if(!canvas)return false;const r=canvas.getBoundingClientRect();" +
                    "return r.width>100&&Math.abs(r.height/r.width-500/960)<.06;})()");
                Require(responsiveReel.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor canvas has the wrong aspect ratio after actual Windows layout.");
                // Exercise the *actual packaged WebView2* bilingual controls and
                // backend-authored paytable, without placing a wager or changing saves.
                var manorLocaleClock = Stopwatch.StartNew();
                string hostConnected = "false";
                while(manorLocaleClock.Elapsed < TimeSpan.FromSeconds(8))
                {
                    hostConnected = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "(()=>{const balance=document.querySelector('.status-card .money-stat strong');" +
                        "const spin=document.querySelector('.action-console .gold-button');" +
                        "return Boolean(balance?.textContent?.trim().charCodeAt(0)===36" +
                        "&& spin && !spin.disabled);})()");
                    if(hostConnected.Equals("true",StringComparison.OrdinalIgnoreCase)) break;
                    await Task.Delay(100);
                }
                Require(hostConnected.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor did not receive its authoritative C# snapshot.");
                string initialLanguage = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('#manor-language')?.value || ''");
                Require(initialLanguage.Contains("en",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor did not reflect the casino's English preference.");
                string openedRules = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const btn=[...document.querySelectorAll('button')]" +
                    ".find(b=>b.textContent?.includes('Rules & payout table'));" +
                    "if(!btn)return false;btn.click();return true;})()");
                Require(openedRules.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor rules dialog button is missing.");
                await Task.Delay(130);
                string tablePresent = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const dialog=document.querySelector('[role=dialog]');" +
                    "return Boolean(dialog&&dialog.querySelectorAll('.manor-paytable tbody tr').length===14" +
                    "&&dialog.querySelectorAll('.manor-line-card').length===20" +
                    "&&dialog.textContent.includes('version 2'));})()");
                Require(tablePresent.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor rules dialog does not show all C# symbols and twenty lines.");
                await using(FileStream ruleCapture=File.Create(
                    Path.Combine(output,"modern-manor-rules-en.png")))
                    await modernBrowser.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png,ruleCapture);
                await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('.manor-help-heading button')?.click()");
                await Task.Delay(80);
                string switchedGerman = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const picker=document.querySelector('#manor-language');" +
                    "if(!picker)return false;picker.value='de';" +
                    "picker.dispatchEvent(new Event('change',{bubbles:true}));return true;})()");
                Require(switchedGerman.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor German language switch is unavailable.");
                await Task.Delay(140);
                string germanHud = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.money-stat span')?.textContent==='Guthaben')");
                Require(germanHud.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor German-language HUD did not update.");
                string germanHelp = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const btn=[...document.querySelectorAll('button')]" +
                    ".find(b=>b.textContent?.includes('Regeln & Gewinnübersicht'));" +
                    "if(!btn)return false;btn.click();return true;})()");
                Require(germanHelp.Equals("true",StringComparison.OrdinalIgnoreCase),
                    "Modern Manor rules dialog was not translated to German.");
                await Task.Delay(100);
                await using(FileStream germanCapture=File.Create(
                    Path.Combine(output,"modern-manor-rules-de.png")))
                    await modernBrowser.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png,germanCapture);
                await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('.manor-help-heading button')?.click()");
                await Task.Delay(70);
                await Task.Delay(250);
                await using (FileStream modernCapture = File.Create(
                    Path.Combine(output, "modern-manor-phase3.png")))
                {
                    await modernBrowser.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png, modernCapture);
                }
                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Modern Manor opened another visible native window.");
                // The modern game is embedded in MainWindow: no separate native
                // Playtest window exists after the official Manor replacement.
                Window modernShell = window;
                double savedWidth=modernShell.Width, savedHeight=modernShell.Height;
                try
                {
                    // Real WPF/WebView2 narrow-window reflow (not a CSS-only unit test).
                    modernShell.Width=960;
                    modernShell.Height=680;
                    await Task.Delay(260);
                    modernShell.UpdateLayout();
                    string narrowLayout=await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "(()=>{const host=document.querySelector('.manor-canvas');" +
                        "const buttons=[...document.querySelectorAll('.topbar-actions button')];" +
                        "if(!host||buttons.length<2)return false;" +
                        "const r=host.getBoundingClientRect();" +
                        "return r.width>100 && Math.abs(r.width/r.height-960/500)<.07 &&" +
                        "buttons.every(b=>b.getBoundingClientRect().width>32);})()");
                    Require(narrowLayout.Equals("true",StringComparison.OrdinalIgnoreCase),
                        "Modern Manor controls or reel ratio failed at the compact host size.");
                    string compactNoScroll = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "document.documentElement.scrollHeight<=innerHeight+2 &&" +
                        "document.body.scrollHeight<=innerHeight+2");
                    Require(compactNoScroll.Equals("true",StringComparison.OrdinalIgnoreCase),
                        "Compact Modern Manor still requires page scrolling to reach reels.");
                    await using(FileStream compactCapture=File.Create(
                        Path.Combine(output,"modern-manor-compact-host.png")))
                        await modernBrowser.CoreWebView2.CapturePreviewAsync(
                            CoreWebView2CapturePreviewImageFormat.Png,compactCapture);
                    modernShell.Width=savedWidth;
                    modernShell.Height=savedHeight;
                    await Task.Delay(230);
                    modernShell.UpdateLayout();

                    // Observe the packaged WebView2 idle event loop, not hypothetical 60 FPS.
                    // No wagers are issued and the global animation-off setting is respected.
                    await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "(()=>{const sample={count:0,gaps:[],start:performance.now()," +
                        "heapStart:performance.memory?.usedJSHeapSize??null};" +
                        "window.__manorPhase3Sample=sample;let previous=0;" +
                        "const tick=now=>{if(previous>0&&now-previous<250){" +
                        "sample.count++;sample.gaps.push(now-previous)}previous=now;" +
                        "if(now-sample.start<3200)requestAnimationFrame(tick);" +
                        "else sample.heapEnd=performance.memory?.usedJSHeapSize??null};" +
                        "requestAnimationFrame(tick);return true;})()");
                    await Task.Delay(3900);
                    string sampleJson=await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                        "(()=>{const s=window.__manorPhase3Sample;" +
                        "if(!s)return {frames:0};const ordered=[...s.gaps].sort((a,b)=>a-b);" +
                        "const mean=ordered.reduce((a,b)=>a+b,0)/(ordered.length||1);" +
                        "return {source:'Windows WebView2 requestAnimationFrame (idle, not Pixi/GPU FPS)'," +
                        "frames:s.count,meanMs:mean,p95Ms:ordered[Math.ceil(ordered.length*.95)-1]??null," +
                        "heapBytesStart:s.heapStart,heapBytesEnd:s.heapEnd??null," +
                        "devicePixelRatio:window.devicePixelRatio," +
                        "hostWidth:window.innerWidth,hostHeight:window.innerHeight};})()");
                    using(System.Text.Json.JsonDocument sampleDoc=
                        System.Text.Json.JsonDocument.Parse(sampleJson))
                    {
                        Require(sampleDoc.RootElement.GetProperty("frames").GetInt32()>=12,
                            "Modern Manor idle WebView2 was not receiving animation frames.");
                        File.WriteAllText(Path.Combine(output,"modern-manor-idle-frame-sample.json"),
                            sampleDoc.RootElement.GetRawText());
                    }
                }
                finally
                {
                    modernShell.Width=savedWidth;
                    modernShell.Height=savedHeight;
                }
                // Leave through the actual frontend Back control. Closing modernShell
                // here would close MainWindow itself, not the embedded Manor game.
                await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('.topbar-actions button:last-of-type')?.click()");
                var returnClock = Stopwatch.StartNew();
                while (returnClock.Elapsed < TimeSpan.FromSeconds(5)
                    && !Find<CasinoLobbyView>(window).Any(v => v.IsVisible))
                    await Task.Delay(80);
                Require(window.IsVisible && Find<CasinoLobbyView>(window).Any(v => v.IsVisible),
                    "Modern Manor Back control did not return to the visible casino lobby.");
                RequireNoLegacyLobbyFrame(window, "Manor return");

                await ModernNineLivesWebSmoke.Run(window, profile, output);
                RequireNoLegacyLobbyFrame(window, "Modern Nine Lives return");

                await ModernNeonVaultWebSmoke.Run(window, profile, output);
                RequireNoLegacyLobbyFrame(window, "Modern Neon Vault return");

                await UiResponsivenessSmoke.Run(window, output);
                await LocalizationSmoke.Run(window, output);
                await MidnightMergeSmoke.Run(window, output);
                await SudokuSmoke.Run(window, output);
                await SolitaireSmoke.Run(window, output);
                await MemorySmoke.Run(window, output);
                await JigsawSmoke.Run(window, output);
                await WordSearchSmoke.Run(window, output);
                await BackroomLoungeSmoke.Run(window, output);
                await ModernLoungeSmoke.Run(window, output);
                await KniffelSmoke.Run(window, output, profile);
                await KniffelCasinoSmoke.Run(window, output, profile);
                await KniffelFeedbackSmoke.Run(window, output);
                await EmpireStrategySmoke.Run(window, output);
                await VaultSmoke.Run(window, output);
                await ModernShopSmoke.Run(window, profile, output);
                await ModernRecordsSmoke.Run(window, profile, output);
                await ModernAlbumSmoke.Run(window, profile, output);

                File.WriteAllText(Path.Combine(output, "result.txt"),
                    "PASS: window bounds; EN/DE live localization; Nine Lives presentation; Chill Lounge Sudoku, Solitaire and Memory; Midnight Merge foundation; Midnight Dice; Casino Empire; and in-app Casino Vault acceptance all passed.");
            }
            catch (Exception ex)
            {
                exit = 1;
                File.WriteAllText(Path.Combine(output, "failure.txt"), ex.ToString());
                Console.Error.WriteLine(ex);
            }
            finally
            {
                window.Close();
                CasinoAudio.Shutdown();
                app.Shutdown(exit);
            }
        };

        window.Show();
        app.Run();
        return exit;
    }

    /// <summary>
    /// RenderTargetBitmap cannot capture a WebView2 HWND, so WPF-only screenshots
    /// can silently show the legacy fallback even when the React lobby is broken.
    /// Require the actual packaged browser to load, receive live C# profile data,
    /// expose the full casino navigation (ten native modes and Arcade) and produce a real WebView2 capture.
    /// </summary>
    private static async Task VerifyPackagedModernLobby(MainWindow window, string output)
    {
        Require(ModernLobbyView.IsAvailable,
            "The packaged modern lobby HTML is missing from ModernFrontend.");
        ModernLobbyView modern = Find<ModernLobbyView>(window).Single();
        Stopwatch watch = Stopwatch.StartNew();
        while (!modern.Ready && !modern.Failed && watch.Elapsed < TimeSpan.FromSeconds(18))
            await Task.Delay(150);
        if (!modern.Ready)
        {
            string browserState = modern.Browser.CoreWebView2 is null ? "CoreWebView2 not initialized"
                : await modern.Browser.CoreWebView2.ExecuteScriptAsync(
                    "JSON.stringify({state:document.readyState, url:location.href,"
                    + "title:document.title, html:document.body?.innerHTML?.slice(0,600),"
                    + "react:!!document.querySelector('.casino-lobby-shell'),"
                    + "scripts:[...document.querySelectorAll('script')].map(s=>s.src),"
                    + "resourceCount:performance.getEntriesByType('resource').length,"
                    + "webview:!!window.chrome?.webview})");
            File.WriteAllText(Path.Combine(output, "modern-lobby-browser-diagnostic.txt"), browserState);
            Console.Error.WriteLine("Lobby browser diagnostic: " + browserState);
        }
        Require(modern.Ready && modern.IsVisible && modern.Browser.CoreWebView2 is not null,
            "Modern lobby failed to replace the WPF fallback. Ready=" + modern.Ready
            + ", Failed=" + modern.Failed + ", Reason=" + (modern.FailureReason ?? "no ready signal")
            + ", Source=" + modern.Browser.Source + ", Navigation=" + modern.LastNavigation
            + ", LastMessageSource=" + modern.LastMessageSource + ", LastMessage=" + modern.LastMessageType);

        ModernWebViewBootstrapSmoke.RequireHardened(
            modern.Browser.CoreWebView2!, "React lobby");

        // Do not accept the static browser preview or a blank canvas. The host
        // must have delivered the real seeded C# profile and all registered modes.
        string result = await modern.Browser.CoreWebView2!.ExecuteScriptAsync(
            "(()=>{const shell=document.querySelector('.casino-lobby-shell');"
            + "const games=[...document.querySelectorAll('.lobby-game')];"
            + "const profile=document.querySelector('.profile-widget');"
            + "const nav=document.querySelectorAll('.lobby-nav-item');"
            + "const daily=document.querySelector('.lobby-daily');"
            + "const rewards=document.querySelectorAll('.daily-reward');"
            + "const art=games.map(g=>g.querySelector('.game-art-window'));"
            + "const gpu=document.querySelector('.lobby-pixi');"
            + "const hero=getComputedStyle(document.querySelector('.lobby-hero-picture')).backgroundImage;"
            + "const expected=['blackjack','roulette','kniffel','chill-lounge','casino-empire','midnight-reels','neon-vault','nine-lives','midnight-manor','midnight-merge','arcade-games'];"
            + "return Boolean(shell&&games.length===expected.length&&expected.every(id=>games.some(g=>g.classList.contains('game-'+id)))&&nav.length>=11"
            + "&&daily&&rewards.length===7&&!document.querySelector('.lobby-overview')"
            + "&&gpu"
            + "&&art.every(x=>x&&x.dataset.modeArt&&(x.dataset.modeArt==='arcade-games'"
            + "?Boolean(x.querySelector('.arcade-tile-scene')):x.dataset.modeArt==='roulette'"
            + "?Boolean(x.querySelector('.roulette-tile-art')):!x.firstElementChild))"
            + "&&Boolean(document.querySelector('.game-arcade-games .lobby-gold-button'))"
            + "&&Boolean(document.querySelector('.game-roulette .lobby-gold-button:not(:disabled)'))"
            + "&&hero.includes('/lobby/art/hero.png')"
            + "&&profile?.textContent?.includes('Startup Smoke')"
            + "&&document.querySelector('.lobby-hero h1')?.textContent?.length>5);})()");
        Require(result.Equals("true", StringComparison.OrdinalIgnoreCase),
            "Packaged React lobby did not render the ten native modes and Arcade Games tile. "
            + "DOM validation returned " + result);

        // The browser-side GPU art system loads local SVG texture pairs in the
        // background. A visible Canvas alone is not proof that artwork loaded.
        bool graphicsReady = false;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            string value = await modern.Browser.CoreWebView2.ExecuteScriptAsync(
                "(()=>document.querySelectorAll('.lobby-game .game-art-window[data-pixi-art-ready]').length===9"
                + "&&Boolean(document.querySelector('.lobby-pixi canvas')))()");
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                graphicsReady = true;
                break;
            }
            await Task.Delay(100);
        }
        Require(graphicsReady,
            "Not all nine PixiJS lobby mode scenes loaded their original packaged artwork.");

        Require(await modern.Browser.CoreWebView2.ExecuteScriptAsync("Boolean(document.querySelector('dialog.casino-news:modal'))") == "true",
            "The React startup news dialog is missing.");
        await modern.Browser.CoreWebView2.ExecuteScriptAsync("document.querySelector('.casino-news .lobby-gold-button')?.click()");
        await Task.Delay(100);
        RequireNoLegacyLobbyFrame(window, "rendered React lobby");
        await CheckModernRoulette(window, modern);
        await CheckModernArcadeMenu(modern);
        await CheckModernLobbyUpdates(window, modern);
        Require(!Find<FrameworkElement>(window).Any(drawer => drawer.Name == "LobbyDailyDrawer" && drawer.IsVisible),
            "The obsolete daily-login drawer is still layered over the React lobby.");

        await Task.Delay(400);
        using var capture = File.Create(Path.Combine(output, "modern-lobby-webview2-live.png"));
        await modern.Browser.CoreWebView2.CapturePreviewAsync(
            CoreWebView2CapturePreviewImageFormat.Png, capture);
        Require(capture.Length > 15_000,
            "Modern lobby WebView2 preview is unexpectedly small or empty.");
    }

    private static async Task CheckModernRoulette(MainWindow window, ModernLobbyView modern)
    {
        CoreWebView2 web = modern.Browser.CoreWebView2!;
        await web.ExecuteScriptAsync("document.querySelector('.game-roulette .lobby-gold-button')?.click()");
        ModernRouletteView? roulette = null;
        for (int attempt=0;attempt<150;attempt++) { roulette=Find<ModernRouletteView>(window).FirstOrDefault(); if(roulette?.Ready==true)break; await Task.Delay(100); }
        Require(roulette?.Ready==true && !modern.IsVisible, "Roulette did not mount a separate ready WebView.");
        web=roulette!.Browser.CoreWebView2!;
        async Task WaitFor(string script, string message)
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                if (await web.ExecuteScriptAsync(script) == "true") return;
                await Task.Delay(100);
            }
            Require(false, message);
        }

        await WaitFor(
            "Boolean(document.querySelector('.roulette-room')"
            + "&&document.querySelector('.roulette-wheel canvas')"
            + "&&document.querySelectorAll('.roulette-numbers .roulette-cell').length===36"
            + "&&document.querySelector('.roulette-zero')"
            + "&&document.querySelectorAll('.roulette-chip').length===8"
            + "&&document.querySelector('.roulette-spin:disabled')"
            + "&&!document.querySelector('.lobby-hero')&&!document.querySelector('.lobby-nav'))",
            "Roulette did not open its React/PixiJS table with all 37 pockets and high-limit chips.");

        // Exercise actual React interactions without changing the seeded wallet
        // used by the remaining startup tests.
        await web.ExecuteScriptAsync("document.querySelector('.roulette-numbers .roulette-cell')?.click()");
        await WaitFor(
            "Boolean(document.querySelector('.roulette-placed')&&document.querySelector('.roulette-spin:not(:disabled)'))",
            "Roulette did not accept a valid chip on the number layout.");
        await web.ExecuteScriptAsync("document.querySelector('.roulette-controls button:nth-child(2)')?.click()");
        await WaitFor(
            "Boolean(!document.querySelector('.roulette-placed')&&document.querySelector('.roulette-spin:disabled'))",
            "Roulette did not clear the wager and disable spinning.");
        await web.ExecuteScriptAsync("document.querySelector('.roulette-back')?.click()");
        web=modern.Browser.CoreWebView2!;
        await WaitFor(
            "Boolean(document.querySelector('.lobby-hero')&&document.querySelector('.game-roulette')&&!document.querySelector('.roulette-room')&&document.querySelectorAll('.game-art-window[data-pixi-art-ready]').length===9)",
            "Back from Roulette did not restore the casino lobby.");
    }

    private static async Task CheckModernArcadeMenu(ModernLobbyView modern)
    {
        CoreWebView2 web = modern.Browser.CoreWebView2!;
        await web.ExecuteScriptAsync("document.querySelector('.game-arcade-games .lobby-gold-button')?.click()");
        bool menuOpened = false;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            string ready = await web.ExecuteScriptAsync(
                "Boolean(document.querySelector('[data-testid=arcade-menu]')"
                + "&&document.querySelectorAll('[data-arcade-id]').length===20"
                + "&&!document.querySelector('.lobby-hero'))");
            if (ready == "true") { menuOpened = true; break; }
            await Task.Delay(100);
        }
        Require(menuOpened, "Arcade Games did not open its dedicated twenty-game menu.");
        // The streamlined hub must open a full personal high-score table.
        await web.ExecuteScriptAsync(
            "document.querySelector('[data-testid=arcade-highscores-open]')?.click()");
        string highScores = await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=arcade-highscores-dialog]')"
            + "&&document.querySelectorAll('.arcade-highscores-entry').length===20)");
        Require(highScores == "true", "Arcade personal high-score table is missing games.");
        await web.ExecuteScriptAsync(
            "document.querySelector('.arcade-highscores-dialog .arcade-detail-close')?.click()");

        // Launch and exit all twenty real Arcade games in packaged WebView2.
        // Static cards and nonfunctional "Play" buttons cannot pass this check.
        foreach (string id in new[] {
            "midnight-wings", "cave-escape", "perfect-stack", "blade-timing",
            "death-snake", "neon-breaker", "bullet-storm",
            "hexagon-hell", "spike-rush", "endless-ascent",
            "midnight-beats", "phantom-hook", "neon-drift", "gravity-chamber", "echo-sequence",
            "prism-lock", "meteor-miner", "circuit-rush", "shadow-heist", "pulse-perfect" })
        {
            await web.ExecuteScriptAsync(
                "document.querySelector('[data-arcade-id=" + id + "]')?.click()");
            bool detailsOpened = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string ready = await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.arcade-detail[role=dialog]')"
                    + "&&document.querySelector('[data-testid=arcade-play-button]'))");
                if (ready == "true") { detailsOpened = true; break; }
                await Task.Delay(100);
            }
            Require(detailsOpened, "Playable arcade game did not open details: " + id);
            await web.ExecuteScriptAsync(
                "document.querySelector('[data-testid=arcade-play-button]')?.click()");
            bool gameOpened = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string ready = await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('[data-testid=arcade-game-screen][data-game-id=" + id + "]')"
                    + "&&document.querySelector('[data-testid=arcade-canvas]')"
                    + "&&document.querySelector('.arcade-play-overlay[data-phase=ready]'))");
                if (ready == "true") { gameOpened = true; break; }
                await Task.Delay(100);
            }
            Require(gameOpened, "Arcade game failed to mount its real canvas: " + id);
            // Gameplay must fit the actual client area, not introduce its own scrollbar.
            string fits = await web.ExecuteScriptAsync(
                "(()=>{const p=document.querySelector('.arcade-play');"
                + "const s=document.querySelector('.arcade-play-stage');"
                + "const c=document.querySelector('[data-testid=arcade-canvas]');"
                + "const h=document.querySelector('.arcade-play-header');"
                + "const runs=document.querySelector('[data-testid=arcade-runs]');"
                + "const shell=document.querySelector('.casino-lobby-shell');"
                + "const bounds=p?.getBoundingClientRect(),stage=s?.getBoundingClientRect();"
                + "return Boolean(p&&s&&c&&h&&runs&&shell&&p.clientHeight>180"
                + "&&p.scrollHeight<=p.clientHeight+3&&p.scrollWidth<=p.clientWidth+3"
                + "&&document.documentElement.scrollHeight<=innerHeight+3"
                + "&&document.body.scrollHeight<=innerHeight+3"
                + "&&shell.getBoundingClientRect().bottom<=innerHeight+3"
                + "&&stage.bottom<=bounds.bottom+2"
                + "&&runs.getBoundingClientRect().bottom<=h.getBoundingClientRect().bottom+1"
                + "&&stage.width<innerWidth-100"
                + "&&stage.height<innerHeight*.78"
                + "&&c.getBoundingClientRect().height>80)})()");
            Require(fits == "true", "Arcade game overflowed its viewport: " + id);
            await web.ExecuteScriptAsync(
                "document.querySelector('.arcade-primary-action')?.click()");
            bool started = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string ready = await web.ExecuteScriptAsync(
                    "(()=>{const c=document.querySelector('[data-testid=arcade-canvas]');"
                    + "if(!c||document.querySelector('.arcade-play-overlay[data-phase=ready]'))return false;"
                    + "const p=c.getContext('2d')?.getImageData(Math.floor(c.width/2),Math.floor(c.height/2),1,1).data;"
                    + "return Boolean(p&&p[3]>0)})()");
                if (ready == "true") { started = true; break; }
                await Task.Delay(100);
            }
            Require(started, "Arcade game did not start or render a frame: " + id);
            // Pause must stop simulation and show a real pause overlay.
            await web.ExecuteScriptAsync(
                "document.querySelector('.arcade-play-actions button:last-child')?.click()");
            bool paused = await web.ExecuteScriptAsync(
                "Boolean(document.querySelector('.arcade-play-overlay[data-phase=paused]'))") == "true";
            Require(paused, "Arcade pause control did not pause the run: " + id);
            await web.ExecuteScriptAsync(
                "document.querySelector('.arcade-play-heading .arcade-back')?.click()");
            bool returned = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string ready = await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('[data-testid=arcade-menu]')"
                    + "&&document.querySelectorAll('[data-arcade-id]').length===20)");
                if (ready == "true") { returned = true; break; }
                await Task.Delay(100);
            }
            Require(returned, "Arcade game did not return to selection: " + id);
        }
        await web.ExecuteScriptAsync("document.querySelector('.arcade-back')?.click()");
        bool backToLobby = false;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            string ready = await web.ExecuteScriptAsync(
                "Boolean(document.querySelector('.lobby-hero')"
                + "&&document.querySelector('.game-arcade-games')"
                + "&&document.querySelector('.lobby-daily'))");
            if (ready == "true") { backToLobby = true; break; }
            await Task.Delay(100);
        }
        Require(backToLobby, "Back from Arcade Games did not restore the casino lobby.");
    }

    private static async Task CheckModernLobbyUpdates(MainWindow window, ModernLobbyView modern)
    {
        var core = modern.Browser.CoreWebView2!;
        // Update access must remain at the bottom-left of the lobby, not hidden
        // behind a Blackjack table that will eventually be removed.
        Require(await core.ExecuteScriptAsync(
            "(()=>{const b=document.querySelector('[data-testid=\"lobby-updates-entry\"]');"
            + "const r=b?.getBoundingClientRect();"
            + "return Boolean(r&&r.width>70&&r.left<185&&innerHeight-r.bottom<90)})()") == "true",
            "React lobby updater entry is missing or not anchored at the lower left.");

        Require(window.FindName("NativeUpdateOverlay") is FrameworkElement { Visibility: Visibility.Collapsed },
            "Native fallback updater was unexpectedly exposed over the React lobby.");

        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"lobby-updates-entry\"]').click()");
        bool panelOpen = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(60);
            panelOpen = await core.ExecuteScriptAsync(
                "Boolean(document.querySelector('[data-testid=\"lobby-updates-panel\"]')"
                + "&&document.querySelector('[data-testid=\"lobby-update-channel\"]')"
                + "&&document.querySelector('[data-testid=\"lobby-update-check\"]')"
                + "&&document.querySelector('[data-testid=\"lobby-update-install\"]'))") == "true";
            if (panelOpen) break;
        }
        Require(panelOpen, "Lobby update modal did not receive a valid native status snapshot.");

        // A release-channel change must invoke the original C# preference
        // control; selecting a channel in React must not be cosmetic only.
        ComboBox nativeChannel = (ComboBox)window.FindName("UpdateChannelComboBox");
        await core.ExecuteScriptAsync(
            "(()=>{const s=document.querySelector('[data-testid=\"lobby-update-channel\"]');"
            + "s.value='development';s.dispatchEvent(new Event('change',{bubbles:true}));})()");
        for (int i = 0; i < 50 && nativeChannel.SelectedIndex != 1; i++)
            await Task.Delay(50);
        Require(nativeChannel.SelectedIndex == 1,
            "React lobby Development channel did not reach native update preferences.");

        await core.ExecuteScriptAsync(
            "(()=>{const s=document.querySelector('[data-testid=\"lobby-update-channel\"]');"
            + "s.value='stable';s.dispatchEvent(new Event('change',{bubbles:true}));})()");
        for (int i = 0; i < 50 && nativeChannel.SelectedIndex != 0; i++)
            await Task.Delay(50);
        Require(nativeChannel.SelectedIndex == 0,
            "React lobby Stable channel did not reach native update preferences.");

        await core.ExecuteScriptAsync("document.querySelector('.lobby-updates-close').click()");
        for (int i = 0; i < 40; i++)
        {
            if (await core.ExecuteScriptAsync(
                "Boolean(document.querySelector('[data-testid=\"lobby-updates-panel\"]'))") == "false")
                return;
            await Task.Delay(50);
        }
        throw new InvalidOperationException("Lobby update modal did not close.");
    }

    private static void RequireNoLegacyLobbyFrame(MainWindow window, string moment)
    {
        if (!ModernLobbyView.IsAvailable) return;
        CasinoLobbyView legacy = Find<CasinoLobbyView>(window).Single();
        Require(legacy.Opacity <= .001 && !legacy.IsHitTestVisible,
            "The old WPF lobby can flash or steal input during " + moment
            + " (opacity=" + legacy.Opacity + ").");
    }

    // Exercise the actual WPF welcome layout with intentionally long generated titles,
    // rather than trusting a short static announcement as the only test case.
    private static void CheckWelcomeOverlay(MainWindow owner, PlayerProfileState profile, string output)
    {
        Require(owner.IsLoaded && owner.IsVisible &&
            Find<CasinoLobbyView>(owner).Any(lobby => lobby.IsVisible),
            "The casino lobby must be fully prepared before showing the startup welcome.");

        Type welcomeType = typeof(MainWindow).Assembly.GetType(
            "BlackjackCasino.Desktop.StartupWelcomeWindow", throwOnError: true)!;
        var constructor = welcomeType.GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null, new[] { typeof(PlayerProfileState) }, modifiers: null)
            ?? throw new InvalidOperationException("Welcome constructor is missing.");
        var welcome = (Window)constructor.Invoke([profile]);
        welcome.Owner = owner;
        try
        {
            welcome.Show();
            welcome.UpdateLayout();
            ScrollViewer notesScroll = Find<ScrollViewer>(welcome).Single(scroll =>
                scroll.Content is StackPanel notes &&
                Find<TextBlock>(notes).Any(label => label.Text == "Tonight's changes"));
            StackPanel content = (StackPanel)notesScroll.Content;
            TextBlock longHeading = Find<TextBlock>(content).First(label =>
                label.FontWeight == FontWeights.Bold && Math.Abs(label.FontSize - 12) < .01);
            longHeading.Text = "Translate Dead Man's Hand investigation decisions, evidence board " +
                "and consequences for all chapters while preserving loaded adventure saves and " +
                "making every release note readable without horizontal clipping.";
            welcome.UpdateLayout();
            notesScroll.UpdateLayout();

            Require(owner.Opacity > .99 && Find<CasinoLobbyView>(owner).Any(lobby => lobby.IsVisible),
                "The welcome dialog must have a visible casino lobby as its owner/backdrop.");
            Require(notesScroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled &&
                notesScroll.VerticalScrollBarVisibility == ScrollBarVisibility.Auto,
                "Release notes must scroll vertically, not clip or scroll sideways.");
            Require(notesScroll.ViewportWidth > 200,
                "Release notes have no usable viewport width.");
            Require(longHeading.TextWrapping == TextWrapping.Wrap && longHeading.ActualHeight > 30,
                "Long changelog headings did not wrap onto multiple lines.");
            Require(longHeading.ActualWidth <= notesScroll.ViewportWidth - 24,
                "Changelog heading exceeds its available text column.");
            SaveScreenshot(welcome, Path.Combine(output, "startup-welcome-long-news.png"));
        }
        finally
        {
            welcome.Close();
        }
    }

    private static void SaveScreenshot(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i)))
            {
                yield return child;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
