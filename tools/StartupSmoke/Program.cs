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
                CheckWelcomeOverlay(window, profile, output);
                await VerifyPackagedModernLobby(window, output);
                RequireNoLegacyLobbyFrame(window, "startup");
                Require(window.Icon is not null && window.Template.FindName("CasinoTitleBar", window) is FrameworkElement { IsVisible: true }, "The desktop icon or custom title bar is missing.");
                await WindowFrameSmoke.Run(window);
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
                // The React lobby owns the visible header. Keep validating the native
                // WPF fallback's reward state without requiring its hidden header to be visible.
                Button challenges = Find<Button>(window).Single(button => button.Name == "HeaderChallengesButton");
                Require(Find<TextBlock>(challenges).Any(text => text.Text.Contains("READY", StringComparison.Ordinal)),
                    "Completed challenge did not surface a READY indicator in the global header.");
                Require(challenges.Effect is DropShadowEffect,
                    "Completed challenge did not activate the global header glow cue.");
                Button vaultHeader = Find<Button>(window).Single(button => button.Name == "CasinoVaultButton");
                ModernLobbyView reactLobby = Find<ModernLobbyView>(window).Single();
                string clubNavigation = await reactLobby.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "(()=>{const links=[...document.querySelectorAll('.lobby-nav-item')].map(x=>x.textContent);"
                    + "const ready=document.querySelector('.nav-ready')?.textContent||'0';"
                    + "return links.some(x=>x.includes('Challenges'))&&links.some(x=>x.includes('Vault'))"
                    + "&&links.some(x=>x.includes('Season Pass'))&&Number(ready)>0;})()");
                Require(clubNavigation.Equals("true", StringComparison.OrdinalIgnoreCase),
                    "Compact React lobby hides claimable rewards or Vault navigation.");
                Require(Find<VaultHeaderArtwork>(vaultHeader).Any(),
                    "Casino Vault header card is missing its vault artwork.");
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
                string modernGameplay = await modernBrowser.CoreWebView2!.ExecuteScriptAsync(
                    "document.querySelector('.gold-button')?.textContent || ''");
                Require(modernGameplay.Contains("SPIN THE REELS", StringComparison.OrdinalIgnoreCase)
                    || modernGameplay.Contains("PLAY FREE SPIN", StringComparison.OrdinalIgnoreCase),
                    "Modern Manor did not expose gameplay controls after loading.");
                string polishedControls = await modernBrowser.CoreWebView2.ExecuteScriptAsync(
                    "(()=>{const bets=[...document.querySelectorAll('.preset-bets .bet-chip')];" +
                    "const auto=[...document.querySelectorAll('.auto-buttons .auto-chip')];" +
                    "return bets.length===6 && auto.length===4" +
                    "&& bets.some(b=>b.textContent?.includes('5,000'))" +
                    "&& document.querySelector('#manor-bet')?.max==='5000'" +
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

                NavigationSmoke.OpenCovered(window, "nine-lives");
                await Task.Delay(260);
                window.UpdateLayout();

                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Nine Lives opened another visible native window.");
                Require(Find<Image>(window).Any(image => image.IsVisible && image.Source is not null),
                    "Nine Lives did not render any artwork after opening.");

                SaveScreenshot(window, Path.Combine(output, "nine-lives-main.png"));

                Image[] reelImages = Find<Image>(window)
                    .Where(image => image.IsVisible
                        && image.Parent is Border frame
                        && Math.Abs(frame.Width - 210d) < 0.1
                        && Math.Abs(frame.Height - 210d) < 0.1)
                    .ToArray();
                Require(reelImages.Length == 9,
                    $"Nine Lives did not expose exactly nine reel artwork surfaces; found {reelImages.Length}.");
                Grid nineLivesStage = Find<Grid>(window).Single(grid => grid.Name == "NineLivesStage");
                Require(nineLivesStage.Width / nineLivesStage.Height > 1.8, "Nine Lives reverted to its letterboxed 3:2 canvas.");
                FrameworkElement cabinet = Find<FrameworkElement>(window).Single(element => element.Name == "MachineCabinet");
                FrameworkElement footer = Find<FrameworkElement>(window).Single(element => element.Name == "NineLivesFooter");
                Point footerInCabinet = footer.TranslatePoint(new Point(0, 0), cabinet);
                Require(footerInCabinet.Y + footer.ActualHeight < cabinet.ActualHeight - 3,
                    "Nine Lives control console collides with the cabinet's lower metallic rim.");
                Require(reelImages.All(image => image.Stretch == Stretch.Uniform), "Nine Lives symbol art is stretched or cropped.");
                Require(reelImages.All(image => image.Source is BitmapSource),
                    "Nine Lives regressed to replacement vector symbols instead of the restored concept-art set.");
                Require(reelImages.All(image => image.Source is BitmapSource bitmap
                    && bitmap.PixelWidth >= 1024 && bitmap.PixelHeight >= 1024),
                    "Nine Lives high-resolution symbols failed to load or were downscaled.");
                Require(reelImages.Select(image => image.Source).Distinct().Count() == 8,
                    "Nine Lives preview should contain eight distinct premium symbol images.");

                Require(Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives Spin button is missing from the hosted surface.");
                Button back = Find<Button>(window).FirstOrDefault(button => button.Name == "CasinoButton")
                    ?? throw new InvalidOperationException("Nine Lives does not expose a Casino/back button.");
                Button? duplicateMenu = Find<Button>(window).FirstOrDefault(button => button.Name == "MenuButton");
                Require(back.IsVisible,
                    "Nine Lives Casino/back button exists but is not visible.");
                Require(duplicateMenu is null || !duplicateMenu.IsVisible,
                    "Nine Lives shows its deprecated footer Casino/back navigation control.");
                TextBlock winText = Find<TextBlock>(window).FirstOrDefault(text => text.Name == "WinText")
                    ?? throw new InvalidOperationException("Nine Lives last-win display is missing.");
                Require(winText.Text == "—", "Nine Lives must show an em dash rather than NO/0 for an empty last win.");

                Button settings = Find<Button>(window).FirstOrDefault(button => button.Name == "SettingsButton")
                    ?? throw new InvalidOperationException("Nine Lives settings button is missing.");
                settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(60);
                window.UpdateLayout();
                Border settingsFlyout = Find<Border>(window).FirstOrDefault(border => border.Name == "SettingsFlyout")
                    ?? throw new InvalidOperationException("Nine Lives custom settings flyout is missing.");
                Require(settingsFlyout.Visibility == Visibility.Visible,
                    "Nine Lives settings did not open the custom in-app flyout.");
                Require(settings.ContextMenu is null,
                    "Nine Lives settings regressed to a native Windows ContextMenu.");
                SaveScreenshot(window, Path.Combine(output, "nine-lives-settings.png"));
                settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Button paytable = Find<Button>(window).FirstOrDefault(button => button.Name == "InfoButton")
                    ?? throw new InvalidOperationException("Nine Lives paytable button is missing.");
                paytable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(60);
                window.UpdateLayout();
                Border paytableFlyout = Find<Border>(window).FirstOrDefault(border => border.Name == "PaytableFlyout")
                    ?? throw new InvalidOperationException("Nine Lives custom paytable flyout is missing.");
                Require(paytableFlyout.Visibility == Visibility.Visible,
                    "Nine Lives paytable did not open inside the game surface.");
                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Nine Lives paytable opened native Windows chrome.");
                SaveScreenshot(window, Path.Combine(output, "nine-lives-paytable.png"));
                Button closePaytable = Find<Button>(paytableFlyout).FirstOrDefault(button => Equals(button.Content, "BACK TO GAME"))
                    ?? throw new InvalidOperationException("Nine Lives paytable has no visible return control.");
                Require(closePaytable.ActualWidth >= 180,
                    "Nine Lives paytable return button is too narrow and can clip its label.");
                closePaytable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Require(!string.IsNullOrWhiteSpace(back.Content?.ToString()),
                    "Nine Lives Casino/back button is an invisible hotspot without a label.");

                back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(150);
                window.UpdateLayout();

                Require(!Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives remained hosted after using the Casino/back button.");
                Require(Find<Button>(window).Any(button => button.Name == "Mode_nine_lives" && button.IsVisible),
                    "Casino lobby was not restored after leaving Nine Lives.");
                RequireNoLegacyLobbyFrame(window, "Nine Lives return");

                // Neon Vault: scalable cyber artwork, no legacy 128px thumbnails, in-app navigation.
                NavigationSmoke.OpenCovered(window, "neon-vault");
                await Task.Delay(280);
                window.UpdateLayout();
                Grid cyberStage = Find<Grid>(window).Single(grid => grid.Name == "NeonVaultStage");
                Require(cyberStage.Width / cyberStage.Height > 1.8, "Neon Vault reverted to a narrow letterboxed layout.");
                Image[] cyberSymbols = Find<Image>(cyberStage)
                    .Where(image => image.IsVisible && image.Source is BitmapSource)
                    .ToArray();
                Require(cyberSymbols.Length >= 20, $"Neon Vault rendered only {cyberSymbols.Length} cached reel symbols.");
                Require(cyberSymbols.All(image => image.Source is BitmapSource art &&
                    art.IsFrozen && art.PixelWidth >= 384 && art.PixelHeight >= 384),
                    "Neon Vault symbols must be high-resolution, frozen, shared raster textures.");
                Require(cyberSymbols.All(image => image.Stretch == Stretch.Uniform),
                    "Neon Vault is stretching or cropping symbol artwork.");
                Require(cyberSymbols.Select(image => image.Source).Distinct().Count() >= 6,
                    "Neon Vault has reverted to repeated placeholder artwork instead of the eight-symbol gallery.");
                Border cyberReelFrame = Find<Border>(window).Single(border => border.Name == "ReelFrame");
                Require(cyberReelFrame.ActualWidth > 720 && cyberReelFrame.ActualHeight > 475,
                    "Neon Vault reel stage failed to use the 1080p design area.");
                Require(Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Neon Vault spin control is missing.");
                SaveScreenshot(window, Path.Combine(output, "neon-vault-overhaul.png"));
                Button neonBack = Find<Button>(window).Single(button => button.Name == "BackButton" && button.IsVisible);
                neonBack.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(160);
                window.UpdateLayout();
                Require(Find<Button>(window).Any(button => button.Name == "Mode_neon_vault" && button.IsVisible),
                    "Casino lobby did not return after closing Neon Vault.");
                RequireNoLegacyLobbyFrame(window, "Neon Vault return");



                await UiResponsivenessSmoke.Run(window, output);
                await LocalizationSmoke.Run(window, output);
                await MidnightMergeSmoke.Run(window, output);
                await SudokuSmoke.Run(window, output);
                await SolitaireSmoke.Run(window, output);
                await MemorySmoke.Run(window, output);
                await JigsawSmoke.Run(window, output);
                await WordSearchSmoke.Run(window, output);
                await BackroomLoungeSmoke.Run(window, output);
                await KniffelSmoke.Run(window, output, profile);
                await KniffelCasinoSmoke.Run(window, output, profile);
                await KniffelFeedbackSmoke.Run(window, output);
                await EmpireStrategySmoke.Run(window, output);
                await VaultSmoke.Run(window, output);

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
    /// expose the full nine-game navigation and produce a real WebView2 capture.
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

        // Do not accept the static browser preview or a blank canvas. The host
        // must have delivered the real seeded C# profile and all registered modes.
        string result = await modern.Browser.CoreWebView2!.ExecuteScriptAsync(
            "(()=>{const shell=document.querySelector('.casino-lobby-shell');"
            + "const games=[...document.querySelectorAll('.lobby-game')];"
            + "const profile=document.querySelector('.profile-widget');"
            + "const nav=document.querySelectorAll('.lobby-nav-item');"
            + "const daily=document.querySelector('.lobby-daily');"
            + "const rewards=document.querySelectorAll('.daily-reward');"
            + "const art=getComputedStyle(document.querySelector('.art-blackjack')).backgroundImage;"
            + "const hero=getComputedStyle(document.querySelector('.lobby-hero-picture')).backgroundImage;"
            + "return Boolean(shell&&games.length===9&&nav.length>=11"
            + "&&daily&&rewards.length===7&&!document.querySelector('.lobby-overview')"
            + "&&art.includes('/lobby/art/blackjack.png')&&hero.includes('/lobby/art/hero.png')"
            + "&&profile?.textContent?.includes('Startup Smoke')"
            + "&&document.querySelector('.lobby-hero h1')?.textContent?.length>5);})()");
        Require(result.Equals("true", StringComparison.OrdinalIgnoreCase),
            "Packaged React lobby did not render nine real games and live profile state. "
            + "DOM validation returned " + result);

        RequireNoLegacyLobbyFrame(window, "rendered React lobby");
        Require(!Find<LobbyDailyDrawer>(window).Any(drawer => drawer.IsVisible),
            "The obsolete daily-login drawer is still layered over the React lobby.");

        await Task.Delay(400);
        using var capture = File.Create(Path.Combine(output, "modern-lobby-webview2-live.png"));
        await modern.Browser.CoreWebView2.CapturePreviewAsync(
            CoreWebView2CapturePreviewImageFormat.Png, capture);
        Require(capture.Length > 15_000,
            "Modern lobby WebView2 preview is unexpectedly small or empty.");
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
