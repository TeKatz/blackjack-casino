using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static partial class Program
{
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
    private static string output="";
    [STAThread] private static int Main(string[] args)
    {
        output=Path.GetFullPath(args.FirstOrDefault()??"artifacts/presentation-regression");Directory.CreateDirectory(output);
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoControls.xaml",UriKind.Relative)});
        int exit=0;
        var qaProfile=new PlayerProfileState {DisplayName="Presentation QA",Coins=20_000,SlotTrackingVersion=1,
            LifetimeStatistics=new StatisticsSnapshot {RoundsPlayed=50,HandsPlayed=50,HandsWon=25},
            EquippedPortraitId="portrait-moon-cat",OwnedCosmeticIds=[CosmeticCatalog.DefaultTitleId,CosmeticCatalog.DefaultFrameId,CosmeticCatalog.DefaultPortraitId,"portrait-moon-cat"]};
        var window=new MainWindow(new AppPreferences {SoundEnabled=false,MusicEnabled=false,AnimationsEnabled=false,WindowWidth=1280,WindowHeight=900},qaProfile);
        window.MaxWidth=4096;window.MaxHeight=2160;
        // The hosted runner has a small virtual monitor. Raise only this test HWND's
        // tracking limit before WPF reads it, then assert actual rendered dimensions.
        window.SourceInitialized+=(_,_)=>HwndSource.FromHwnd(new WindowInteropHelper(window).Handle).AddHook((IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,ref bool handled)=>
        {
            if(msg==0x24) {Marshal.WriteInt32(lp,32,4096);Marshal.WriteInt32(lp,36,2160);}
            return IntPtr.Zero;
        });
        app.DispatcherUnhandledException+=(_,e)=> {File.WriteAllText(Path.Combine(output,"fatal.txt"),e.Exception.ToString());e.Handled=true;app.Shutdown(1);};
        window.ContentRendered+=async(_,_)=>
        {
            try {await Check(window,qaProfile);File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: startup lobby, registered future mode, shared hover/focus/open/disabled dropdown contracts, all table themes, proportional controls, embedded Reels and responsive React Neon Vault bounds, full-surface single-window casino hosting, embedded profile rename, nested profile/progression navigation, compact challenge pagination, scrolling shop and search.");}
            catch(Exception ex){exit=1;Console.Error.WriteLine(ex);File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());}
            finally {window.Close();CasinoAudio.Shutdown();app.Shutdown(exit);}
        };
        window.Show();app.Run();return exit;
    }
    private static async Task Check(MainWindow window,PlayerProfileState qaProfile)
    {
        await Task.Delay(200);await CaptureModernLobby(window,"01-lobby");
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Application did not start in the lobby");
        Require(Find<Button>(window).Any(v => v.Name == "LobbyNativeUpdatesButton"),
            "WPF lobby fallback has no update entry.");
        Require(((FrameworkElement)window.FindName("NativeUpdateOverlay")).Visibility == Visibility.Collapsed,
            "Native updates overlay was visible at startup.");
        Require(window.FindName("TableSurface") is null
            && window.FindName("BlackjackSidebar") is null
            && window.FindName("DealButton") is null
            && ((FrameworkElement)window.FindName("CasinoHeader")).IsVisible == !ModernLobbyView.IsAvailable,
            "Retired Blackjack WPF controls remained, or the lobby header was incorrect.");
        Require(window.Modes.Modes.Count(m=>m.Id=="midnight-manor")==1,
            "Midnight Manor must be registered exactly once.");
        Require(window.Modes.CanOpen("midnight-manor"),
            "The single Midnight Manor lobby action must remain enabled.");
        Require(!window.Modes.HasSecondary("midnight-manor"),
            "The retired Manor playtest/legacy selector is still registered.");
        Require(Find<Button>(window).Count(b=>b.Name=="Mode_midnight_manor")==1
            && !Find<Button>(window).Any(b=>b.Name=="ModeSecondary_midnight_manor"),
            "Midnight Manor must have precisely one selectable lobby button.");
        foreach (var size in new[] { (1080d,720d), (1440d,900d), (1920d,1080d) })
        {
            window.Width = size.Item1; window.Height = size.Item2; window.UpdateLayout();
            CasinoLobbyView lobby = Find<CasinoLobbyView>(window).Single();
            Button[] tiles = Find<Button>(lobby).Where(button => button.Name.StartsWith("Mode_", StringComparison.Ordinal)).ToArray();
            Require(tiles.Select(tile => tile.Tag as string).OrderBy(id => id).SequenceEqual(window.Modes.Modes.Select(mode => mode.Id).OrderBy(id => id)), "Lobby tiles do not match the registered modes");
            foreach (Button tile in tiles)
            {
                Rect bounds = tile.TransformToAncestor(lobby).TransformBounds(new Rect(0,0,tile.ActualWidth,tile.ActualHeight));
                Require(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= lobby.ActualWidth + 1 && bounds.Bottom <= lobby.ActualHeight + 1,
                    "Lobby tile is outside the visible surface at " + size + ": " + tile.Name);
            }
            Require(!Find<ScrollViewer>(lobby).Any(), "Lobby still requires a scrolling surface");
            await Task.Delay(30); window.UpdateLayout();
            Border hub = Find<Border>(lobby).Single(b => b.Name == "LobbyOverviewHub");
            Rect hubBounds = hub.TransformToAncestor(lobby).TransformBounds(new Rect(hub.RenderSize));
            Require(hubBounds.Bottom <= lobby.ActualHeight + 1, "Lobby hub overflows the viewport.");
            foreach (Button destination in Find<Button>(hub)) RequireWithin(window, destination, "Lobby hub destination");
            Require(tiles.Max(tile => tile.TransformToAncestor(lobby).TransformBounds(new Rect(tile.RenderSize)).Right) < hubBounds.Left, "Hub overlaps game tiles.");

            await CaptureModernLobby(window, "lobby-grid-" + (int)size.Item1);
        }
        await CheckMenuPerformance(window, qaProfile);
        await CheckDistinctFrames(window);
        bool future=false;
        window.Modes.Register(new("future-room","Future room","Registry test","A future mode without changing lobby layout.","#527D9A","◇","Future"),()=>future=true);
        window.UpdateLayout();
        Button added=Find<Button>(window).Single(b=>Equals(b.Tag,"future-room"));added.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(future,"Newly registered mode did not appear or invoke its command");
        await CheckModernBlackjackRoute(window);
        await CheckModernReelsRoute(window, qaProfile);await Task.Delay(100);
        Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,"Host casino header remained visible over embedded Midnight Reels");
        foreach(var size in new[]{(1080d,720d),(1440d,900d),(1920d,1080d),(2560d,1440d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(50);
            Require(Math.Abs(window.ActualWidth-size.Item1)<2 && Math.Abs(window.ActualHeight-size.Item2)<2,"Host OS clamped requested window size "+size+" to "+window.ActualWidth+"x"+window.ActualHeight);
            Button spin=Find<Button>(window).Single(b=>b.Name=="SpinButton");
            Rect bounds=spin.TransformToAncestor(window).TransformBounds(new Rect(0,0,spin.ActualWidth,spin.ActualHeight));
            Require(bounds.Top>=60 && bounds.Bottom<=window.ActualHeight-8 && bounds.Left>=0 && bounds.Right<=window.ActualWidth,"Reels Spin button clipped at "+size);
            Capture(window,"04-reels-"+(int)size.Item1);
        }
        Find<CheckBox>(window).Single(b => b.Name == "SoundToggle").IsChecked = true;
        Find<CheckBox>(window).Single(b => b.Name == "AnimationToggle").IsChecked = true;
        Find<Button>(window).Single(b=>b.Name=="ReelsBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(50);
        var settingsField = typeof(MainWindow).GetField("preferences", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        AppPreferences scoped = (AppPreferences)settingsField.GetValue(window)!;
        Require(scoped.Presentation("midnight-reels").SoundEnabled && scoped.Presentation("midnight-reels").AnimationsEnabled, "Reels settings were not retained locally.");
        Require(!scoped.SoundEnabled && !scoped.AnimationsEnabled && !scoped.Presentation("neon-vault").SoundEnabled, "Reels settings leaked into another destination.");

        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Leaving a slot did not return to the lobby");
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible
            == (!ModernLobbyView.IsAvailable || Find<ModernLobbyView>(window).Any(v=>v.Failed)),
            "Lobby header visibility did not match the active React/WPF presentation after Midnight Reels");

        await CheckModernNeonVaultRoute(window);

        Find<Button>(window).Single(b=>b.Name=="PlayerProfileButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(100);window.UpdateLayout();
        ModernLobbyView? profileHub = Find<ModernLobbyView>(window).FirstOrDefault(v=>v.Ready);
        if (profileHub is not null)
        {
            for (int i=0;i<60 && profileHub.RecordsPage!="profile";i++) await Task.Delay(75);
            Require(profileHub.RecordsPage=="profile","Native profile shortcut did not open React account hub.");
            bool accountEditorReady = false;
            for(int i=0;i<100 && !accountEditorReady;i++)
            {
                accountEditorReady = await profileHub.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.account-page .account-routes button:last-child'))") == "true";
                if(!accountEditorReady) await Task.Delay(50);
            }
            Require(accountEditorReady,"React account edit control is missing.");
            await profileHub.Browser.CoreWebView2!.ExecuteScriptAsync(
                "document.querySelector('.account-page .account-routes button:last-child')?.click()");
            for (int i=0;i<60 && !Find<Button>(window).Any(b=>b.Name=="ProfileNameBox" || b.Name=="RenameProfileButton");i++) await Task.Delay(75);
        }
        RequireSingleHost(window,"Profile");
        Require(window.FindName("DealButton") is null,"A covered legacy Blackjack input still exists.");
        await CheckMetaFocusCycle(window);
        TextBox name=Find<TextBox>(window).Single(t=>t.Name=="ProfileNameBox");name.Text="Midnight Timo";
        Find<Button>(window).Single(b=>b.Name=="RenameProfileButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(qaProfile.DisplayName=="Midnight Timo","Embedded profile rename failed");
        Require(System.Text.Json.JsonSerializer.Deserialize<PlayerProfileState>(System.Text.Json.JsonSerializer.Serialize(qaProfile))!.DisplayName=="Midnight Timo","Save lost profile name");
        foreach(var size in new[]{(1080d,720d),(1440d,900d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();
            RequireWithin(window,Find<Button>(window).Single(b=>b.Name=="ProfileBackButton"),"Profile Back");
            RequireWithin(window,Find<TextBox>(window).Single(b=>b.Name=="ProfileNameBox"),"Profile name");
            Capture(window,"06-profile-"+(int)size.Item1);
        }
        // WebView2 can finish initializing while the native Profile fallback is
        // being exercised. Re-read Ready now rather than relying on an earlier
        // point-in-time choice (otherwise Windows CI races the migration).
        ModernLobbyView? readyProfile = Find<ModernLobbyView>(window).FirstOrDefault(v=>v.Ready);
        for(int i=0;i<90 && readyProfile is null && ModernLobbyView.IsAvailable;i++)
        {
            await Task.Delay(75);
            readyProfile = Find<ModernLobbyView>(window).FirstOrDefault(v=>v.Ready);
        }
        // Hand History and statistics belong to React whenever the host is ready.
        // When WebView2 really is unavailable, the native fallback stays valid.
        if (readyProfile is { Ready: true } modernProfile)
        {
            Find<Button>(window).Single(b=>b.Name=="ProfileHandHistoryButton" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i=0;i<100 && modernProfile.RecordsPage!="history";i++) await Task.Delay(75);
            Require(modernProfile.RecordsPage=="history" && modernProfile.IsVisible,
                "Native profile History shortcut did not navigate to React.");
            bool historyRendered=false;
            for (int i=0;i<100 && !historyRendered;i++)
            {
                historyRendered=await modernProfile.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.history-page .history-filters'))")=="true";
                if(!historyRendered)await Task.Delay(75);
            }
            Require(historyRendered,"React Hand History did not render its search and result controls.");
            await modernProfile.Browser.CoreWebView2!.ExecuteScriptAsync(
                "document.querySelector('.history-page .shop-back')?.click()");
            for(int i=0;i<70 && modernProfile.RecordsPage is not null;i++)await Task.Delay(75);
            Require(modernProfile.RecordsPage is null,"React Hand History did not return to the lobby.");

            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.profile-widget')?.click()");
            for(int i=0;i<70 && modernProfile.RecordsPage!="profile";i++)await Task.Delay(75);
            Require(modernProfile.RecordsPage=="profile","React account hub did not reopen after Hand History.");
            // Native route state can change before the next React commit. Waiting
            // only on RecordsPage and immediately clicking the old DOM is racy.
            bool accountRoutesRendered=false;
            for(int i=0;i<120 && !accountRoutesRendered;i++)
            {
                accountRoutesRendered=await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                    "Boolean([...document.querySelectorAll('.account-page .account-routes button')]"
                    +".find(x=>/Statistics|Statistik/.test(x.textContent||'')))")=="true";
                if(!accountRoutesRendered)await Task.Delay(65);
            }
            Require(accountRoutesRendered,"React account routes did not render after Hand History.");
            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "[...document.querySelectorAll('.account-page .account-routes button')]"
                +".find(x=>/Statistics|Statistik/.test(x.textContent||''))?.click()");
            for (int i=0;i<110 && (modernProfile.RecordsPage!="statistics" || !modernProfile.IsVisible);i++)
                await Task.Delay(75);
            Require(modernProfile.RecordsPage=="statistics" && modernProfile.IsVisible,
                "Profile Statistics did not migrate to React. Route: "
                +(modernProfile.RecordsPage??"<closed>")+" / Visible: "+modernProfile.IsVisible);
            bool statsFromProfile=false;
            for(int i=0;i<100 && !statsFromProfile;i++)
            {
                statsFromProfile=await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.statistics-page [data-stat-game=\"blackjack\"]'))")=="true";
                if(!statsFromProfile)await Task.Delay(75);
            }
            Require(statsFromProfile,"Profile Statistics lacks the real Blackjack lifetime card.");
            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.statistics-page .shop-back')?.click()");
            for(int i=0;i<70 && modernProfile.RecordsPage is not null;i++)await Task.Delay(75);

            // Native Profile is retained only for export/advanced fallback features.
            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.profile-widget')?.click()");
            for(int i=0;i<70 && modernProfile.RecordsPage!="profile";i++)await Task.Delay(75);
            bool exportReady = false;
            for(int i=0;i<100 && !exportReady;i++)
            {
                exportReady=await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.account-page .account-routes button:last-child'))")=="true";
                if(!exportReady)await Task.Delay(50);
            }
            Require(exportReady,"React account export fallback is not available.");
            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.account-page .account-routes button:last-child')?.click()");
            for(int i=0;i<70 && !Find<Button>(window).Any(b=>b.Name=="ProfileProgressionButton"&&b.IsVisible);i++)
                await Task.Delay(80);
        }
        else
        {
            Find<Button>(window).Single(b=>b.Name=="ProfileHandHistoryButton" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);window.UpdateLayout();
            Require(!Find<TabControl>(window).Any(t=>t.Items.OfType<TabItem>()
                .Any(i=>Equals(i.Header,"Cosmetic Shop"))),
                "Native Hand History retained shop navigation.");
            Require(Find<TabControl>(window).Any(t=>t.Items.Count==1),
                "Native fallback Hand History did not open its own page.");
            Find<Button>(window).Single(b=>b.Name=="ProgressionBackButton" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);window.UpdateLayout();
        }
        Find<Button>(window).Single(b=>b.Name=="ProfileProgressionButton" && b.IsVisible)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(100);window.UpdateLayout();RequireSingleHost(window,"Progression from profile");
        TabControl progressionTabs=Find<TabControl>(window).Single(t=>t.Items.OfType<TabItem>().Any(i=>Equals(i.Header,"Challenges")));
        Require(!progressionTabs.Items.OfType<TabItem>().Any(t => t.Name is "StatisticsTab" or "HandHistoryTab"), "Statistics/history remain in Progression & Shop.");
        foreach(string tab in new[]{"Season Pass"})
        {
            progressionTabs.SelectedItem=progressionTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,tab));
            foreach(var size in new[]{(1080d,720d),(1440d,900d)})
            {
                window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(40);
                Capture(window,"06-progression-"+tab.Replace(' ','-')+"-"+(int)size.Item1);
            }
        }
        await CheckAlbum(window, qaProfile, progressionTabs);
        Find<Button>(window).Single(b=>b.Name=="ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);window.UpdateLayout();
        Require(Find<TextBox>(window).Any(t=>t.Name=="ProfileNameBox" && t.Text=="Midnight Timo"),"Progression Back did not restore the profile");
        Find<Button>(window).Single(b=>b.Name=="ProfileBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Closing the profile lost the previous lobby");

        ModernLobbyView? modernShop = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modernShop is not null)
        {
            var browser = modernShop.Browser.CoreWebView2!;
            await browser.ExecuteScriptAsync(
                "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Shop/.test(x.textContent||''))?.click()");
            for (int i = 0; i < 60; i++)
            {
                string shopReady = await browser.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.modern-shop')"
                    + "&&document.querySelectorAll('.shop-product').length===" + CosmeticCatalog.All.Count + ")");
                if (shopReady == "true") break;
                await Task.Delay(100);
            }
            string view = await browser.ExecuteScriptAsync(
                "(()=>{const root=document.querySelector('.modern-shop');"
                + "return Boolean(root&&root.querySelectorAll('.shop-product').length>60"
                + "&&root.querySelectorAll('.shop-pack').length===5"
                + "&&root.querySelector('[data-cosmetic-id=\"portrait-moon-cat\"]')"
                + "&&root.querySelector('[data-cosmetic-id=\"diceset-aurora\"]'));})()");
            Require(view == "true", "Modern Shop does not expose full live cosmetic/pack inventory.");
            await using (FileStream capture = File.Create(Path.Combine(output, "modern-shop-presentation.png")))
                await browser.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, capture);
            await browser.ExecuteScriptAsync("document.querySelector('.shop-back')?.click()");
            await Task.Delay(90);
            Require(!modernShop.ShopOpen, "Shop did not return to the shared React lobby.");

            // Season Pass and Shop now share one React compositor.
            await browser.ExecuteScriptAsync(
                "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Season Pass/.test(x.textContent||''))?.click()");
            for (int i=0;i<70 && modernShop.RecordsPage!="season";i++) await Task.Delay(80);
            Require(modernShop.RecordsPage=="season","Season Pass did not open in React.");
            await browser.ExecuteScriptAsync(
                "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Shop/.test(x.textContent||''))?.click()");
            for (int i = 0; i < 70 && !modernShop.ShopOpen; i++) await Task.Delay(100);
            Require(modernShop.ShopOpen && modernShop.IsVisible,
                "Modern Season -> Shop navigation did not preserve the React host.");
            await browser.ExecuteScriptAsync("document.querySelector('.shop-back')?.click()");
            await Task.Delay(80);
        }

        ModernLobbyView? modernChallenges = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modernChallenges is not null)
        {
            var web = modernChallenges.Browser.CoreWebView2!;
            var readyChallenges = ChallengeCatalog.All.Where(c =>
                ChallengeCatalog.Complete(qaProfile,c) && !qaProfile.ClaimedChallengeIds.Contains(c.Id)).ToArray();
            Require(readyChallenges.Length>0, "QA profile lacks ready challenges for claim-all acceptance.");
            long beforeClaims = qaProfile.Coins;
            await web.ExecuteScriptAsync(
                "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Challenges|Aufgaben/.test(x.textContent||''))?.click()");
            for (int i=0;i<70 && modernChallenges.RecordsPage!="challenges";i++) await Task.Delay(70);
            Require(modernChallenges.RecordsPage=="challenges","Lobby Challenges did not open React.");
            for (int i=0;i<70;i++)
            {
                string populated=await web.ExecuteScriptAsync(
                    "Boolean(document.querySelectorAll('.record-challenge').length>0)");
                if(populated=="true")break;
                await Task.Delay(70);
            }
            string challengeUi=await web.ExecuteScriptAsync(
                "Boolean(document.querySelector('.record-challenge')"
                + "&&document.querySelector('.record-claim-all')"
                + "&&document.querySelector('.record-search input'))");
            Require(challengeUi=="true","React Challenge board did not render its real controls.");
            await web.ExecuteScriptAsync(
                "(()=>{const input=document.querySelector('.record-search input');"
                + "input.value='___no_challenge___';input.dispatchEvent(new Event('input',{bubbles:true}));})()");
            // The Claim All button must operate on every native reward regardless
            // of client-side filters, including entries hidden by the current page.
            await web.ExecuteScriptAsync("document.querySelector('.record-claim-all')?.click()");
            for (int i=0;i<70 && qaProfile.Coins==beforeClaims;i++) await Task.Delay(80);
            Require(qaProfile.Coins==beforeClaims+readyChallenges.Sum(c=>c.CoinReward),
                "React Claim All did not credit every reward, including hidden challenges.");
            Require(readyChallenges.All(c=>qaProfile.ClaimedChallengeIds.Contains(c.Id)),
                "React Claim All did not persist the complete reward set.");
            await using (FileStream shot = File.Create(Path.Combine(output,"modern-challenges-presentation.png")))
                await web.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,shot);
            await web.ExecuteScriptAsync("document.querySelector('.record-page .shop-back')?.click()");
            for(int i=0;i<30 && modernChallenges.RecordsPage is not null;i++)await Task.Delay(70);
            Require(modernChallenges.RecordsPage is null,"Challenges did not return to lobby.");

            await web.ExecuteScriptAsync(
                "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Statistics|Statistik/.test(x.textContent||''))?.click()");
            for(int i=0;i<70 && modernChallenges.RecordsPage!="statistics";i++)await Task.Delay(70);
            Require(modernChallenges.RecordsPage=="statistics","React Statistics navigation failed.");
            // Native RecordsPage changes when WebView2 receives the command, before
            // React necessarily commits its asynchronously delivered statistics snapshot.
            // Wait for real data cards, not merely the route flag or an empty page shell.
            string statsUi = "false";
            for (int i = 0; i < 100 && statsUi != "true"; i++)
            {
                statsUi = await web.ExecuteScriptAsync(
                    "(()=>{const root=document.querySelector('.statistics-page');"
                    + "return Boolean(root&&root.querySelector('[data-stat-game=\"blackjack\"]')"
                    + "&&root.querySelector('[data-stat-game=\"midnight-dice\"]')"
                    + "&&root.querySelector('[data-stat-game=\"neon-vault\"]')"
                    + "&&root.querySelector('.session-metrics')"
                    + "&&root.querySelector('.table-archive'));})()");
                if (statsUi != "true") await Task.Delay(80);
            }
            Require(statsUi=="true","Statistics missing game breakdown, session or table statistics after snapshot delivery.");
            await using(FileStream shot=File.Create(Path.Combine(output,"modern-statistics-presentation.png")))
                await web.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,shot);
            await web.ExecuteScriptAsync("document.querySelector('.record-page .shop-back')?.click()");
            for(int i=0;i<30 && modernChallenges.RecordsPage is not null;i++)await Task.Delay(70);
            Require(modernChallenges.RecordsPage is null,"Statistics failed to return to lobby.");
        }
        else
        {
            Find<Button>(window).Single(b=>b.Name=="LobbyProgression_Challenges" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(60);window.UpdateLayout();
            Require(Find<TabControl>(window).Any(t=>t.SelectedItem is TabItem item && Equals(item.Header,"Challenges")),
                "WPF fallback challenge navigation failed.");
            Find<Button>(window).Single(b=>b.Name=="ProgressionBackButton" && b.IsVisible)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(40);
        }
        await CheckEmpire(window);

    }

    private static Task CheckEmpire(MainWindow window) => EmpireWebSmoke.Run(window, output);

    private static void CheckCollectionPaging(MainWindow window,string panelName,string nextName,string previousName)
    {
        string[] Ids()=>Find<Panel>(window).Single(p=>p.Name==panelName).Children.OfType<Border>()
            .SelectMany(p=>Find<Button>(p)).Select(b=>b.Tag).OfType<string>().ToArray();
        string[] first=Ids();Require(first.Length>=2 && first.Length<=12,"Expected a bounded responsive challenge grid in "+panelName);
        Button next=Find<Button>(window).Single(b=>b.Name==nextName);
        Require(next.IsEnabled,"Next is unavailable with multiple pages in "+panelName);
        next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
        Require(!first.SequenceEqual(Ids()),"Next did not change "+panelName);
        Find<Button>(window).Single(b=>b.Name==previousName).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
        Require(first.SequenceEqual(Ids()),"Previous did not restore "+panelName);
    }
    private static async Task CheckMetaFocusCycle(MainWindow window)
    {
        FrameworkElement overlay=Find<Grid>(window).Single(g=>g.Name=="CasinoMetaOverlay");
        foreach(var direction in new[]{System.Windows.Input.FocusNavigationDirection.Next,System.Windows.Input.FocusNavigationDirection.Previous})
        {
            for(int i=0;i<30;i++)
            {
                if(System.Windows.Input.Keyboard.FocusedElement is UIElement focused)
                    focused.MoveFocus(new System.Windows.Input.TraversalRequest(direction));
                await Task.Delay(1);
                Require(overlay.IsKeyboardFocusWithin,"Tab navigation escaped the active casino page");
            }
        }
    }
    private static void RequireSingleHost(MainWindow window,string page)
    {
        Require(Application.Current.Windows.Cast<Window>().Count(w=>w.IsVisible)==1,page+" created another visible native window");
        Require(Application.Current.Windows.Cast<Window>().Where(w=>w.IsVisible).Single()==window,page+" replaced the casino host");
    }
    private static void RequireWithin(MainWindow host,FrameworkElement element,string label)
    {
        Rect rect=element.TransformToAncestor(host).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
        Require(rect.Left>=0 && rect.Top>=0 && rect.Right<=host.ActualWidth && rect.Bottom<=host.ActualHeight,label+" is clipped");
    }
    private static async Task CheckCombo(ComboBox combo)
    {
        combo.ApplyTemplate();var toggle=(ToggleButton)combo.Template.FindName("DropDownToggle",combo);toggle.ApplyTemplate();
        Require(toggle.Template.Triggers.Count==0,"Dropdown hit surface regained Windows hover/disabled triggers");
        Require(VisualTreeHelper.GetChild(toggle,0) is Border {Background:SolidColorBrush {Color.A:0}},"Dropdown hit area is no longer transparent");
        CheckDark(combo);
        bool enabled=combo.IsEnabled;
        // Disabled selectors must retain dark chrome, but WPF correctly refuses to open
        // their native popups. Do not mistake that for a broken dropdown template.
        if (enabled)
        {
            combo.Focus();CheckDark(combo);
            combo.IsDropDownOpen=true;
            var popup=(Popup)combo.Template.FindName("PART_Popup",combo);
            for (int attempt=0; !popup.IsOpen && attempt<12; attempt++)
            {
                await Task.Delay(30);
                combo.UpdateLayout();
            }
            Require(popup.IsOpen,"Enabled dropdown did not open: "+combo.Name);
            combo.IsDropDownOpen=false;
        }
        combo.IsEnabled=false;CheckDark(combo);combo.IsEnabled=enabled;
    }
    private static void CheckDark(FrameworkElement element)
    {
        if(element.ActualWidth<8 || element.ActualHeight<8)return;
        var bmp=Render(element);var pixels=new byte[bmp.PixelWidth*bmp.PixelHeight*4];bmp.CopyPixels(pixels,bmp.PixelWidth*4,0);
        int index=((bmp.PixelHeight/2)*bmp.PixelWidth+5)*4;
        Require(!(pixels[index]>175 && pixels[index+1]>175 && pixels[index+2]>175),"Blue/white theme chrome covers the dropdown");
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);if(c is T match)yield return match;foreach(T nested in Find<T>(c))yield return nested;}
    }
    private static RenderTargetBitmap Render(FrameworkElement target)
    {
        target.UpdateLayout();var visual=new DrawingVisual();using(var d=visual.RenderOpen())d.DrawRectangle(new VisualBrush(target) {ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,target.ActualWidth,target.ActualHeight)},null,new Rect(0,0,target.ActualWidth,target.ActualHeight));
        var bmp=new RenderTargetBitmap((int)Math.Ceiling(target.ActualWidth),(int)Math.Ceiling(target.ActualHeight),96,96,PixelFormats.Pbgra32);bmp.Render(visual);return bmp;
    }
    /// <summary>
    /// Capture the real browser compositor rather than a WPF VisualBrush, which
    /// cannot see the WebView2 HWND and previously captured only the boot cover.
    /// Wait for every original PixiJS key-art texture to finish loading.
    /// </summary>
    private static async Task CaptureModernLobby(MainWindow window,string name)
    {
        if (!ModernLobbyView.IsAvailable)
        {
            Capture(window,name);
            return;
        }
        ModernLobbyView view=Find<ModernLobbyView>(window).Single();
        for (int attempt=0;attempt<300 && !view.Ready && !view.Failed;attempt++)
            await Task.Delay(100);
        Require(view.Ready && !view.Failed && view.Browser.CoreWebView2 is not null,
            "Cannot capture lobby artwork: React WebView2 did not become ready. "
            + $"Ready={view.Ready}; Failed={view.Failed}; Reason={view.FailureReason}; "
            + $"Navigation={view.LastNavigation}; Source={view.LastMessageSource}; Message={view.LastMessageType}");
        CoreWebView2 core=view.Browser.CoreWebView2!;
        bool texturesReady=false;
        for (int attempt=0;attempt<100;attempt++)
        {
            string status=await core.ExecuteScriptAsync(
                "(()=>{const canvas=document.querySelector('.lobby-pixi canvas');"
                + "return Boolean(canvas&&canvas.width>0&&canvas.height>0"
                + "&&document.querySelectorAll('.lobby-game .game-art-window[data-pixi-art-ready]').length===9)})()");
            if(status=="true"){texturesReady=true;break;}
            await Task.Delay(100);
        }
        Require(texturesReady,"Cannot capture lobby artwork: one or more Pixi key-art textures failed.");
        await core.ExecuteScriptAsync(
            "document.querySelector('dialog.casino-news:modal .lobby-gold-button')?.click()");
        await Task.Delay(220);
        using var file=File.Create(Path.Combine(output,name+".png"));
        await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,file);
        Require(file.Length>15_000,"WebView2 lobby capture unexpectedly empty.");
    }
    private static void Capture(FrameworkElement target,string name){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(Render(target)));using var file=File.Create(Path.Combine(output,name+".png"));png.Save(file);}
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}

