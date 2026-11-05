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
            try {await Check(window,qaProfile);File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: startup lobby, registered future mode, shared hover/focus/open/disabled dropdown contracts, all table themes, proportional controls, embedded Reels and Neon Vault bounds, full-surface single-window casino hosting, embedded profile rename, nested profile/progression navigation, compact challenge pagination, scrolling shop and search.");}
            catch(Exception ex){exit=1;Console.Error.WriteLine(ex);File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());}
            finally {window.Close();CasinoAudio.Shutdown();app.Shutdown(exit);}
        };
        window.Show();app.Run();return exit;
    }
    private static async Task Check(MainWindow window,PlayerProfileState qaProfile)
    {
        await Task.Delay(200);Capture(window,"01-lobby");
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Application did not start in the lobby");
        Require(((FrameworkElement)window.FindName("TableSurface")).Visibility == Visibility.Collapsed
            && ((FrameworkElement)window.FindName("BlackjackSidebar")).Visibility == Visibility.Collapsed
            && !((Button)window.FindName("DealButton")).IsEnabled
            && ((FrameworkElement)window.FindName("CasinoHeader")).IsVisible == !ModernLobbyView.IsAvailable,
            "The startup casino header did not match the modern/fallback lobby state.");
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

            Capture(window, "lobby-grid-" + (int)size.Item1);
        }
        await CheckMenuPerformance(window, qaProfile);
        await CheckDistinctFrames(window);
        bool future=false;
        window.Modes.Register(new("future-room","Future room","Registry test","A future mode without changing lobby layout.","#527D9A","◇","Future"),()=>future=true);
        window.UpdateLayout();
        Button added=Find<Button>(window).Single(b=>Equals(b.Tag,"future-room"));added.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(future,"Newly registered mode did not appear or invoke its command");
        window.Modes.Open("blackjack");window.UpdateLayout();
        ComboBox mode=(ComboBox)window.FindName("GameModeComboBox");
        mode.SelectedIndex=1;window.UpdateLayout();
        foreach(ComboBox combo in Find<ComboBox>(window).Where(c=>c.IsVisible).ToArray()) await CheckCombo(combo);
        mode.SelectedIndex=0;
        ComboBox theme=(ComboBox)window.FindName("ThemeComboBox");
        for(int i=0;i<3;i++){theme.SelectedIndex=i;window.UpdateLayout();Capture(window,"02-table-theme-"+i);}
        window.Width=1080;window.Height=720;window.Left=0;window.Top=0;window.Activate();window.UpdateLayout();
        mode.BringIntoView();window.UpdateLayout();await Task.Delay(50);
        Point point=mode.PointToScreen(new Point(mode.ActualWidth/2,mode.ActualHeight/2));
        bool cursor=SetCursorPos((int)point.X,(int)point.Y);await Task.Delay(100);System.Windows.Input.Mouse.Synchronize();
        File.WriteAllText(Path.Combine(output,"hover-input.txt"),$"Native cursor request: {cursor}; ComboBox IsMouseOver: {mode.IsMouseOver}. All state templates and composed dark pixels are required regardless of headless input support.");
        await CheckCombo(mode);Capture(window,"03-hover-minimum");
        NavigationSmoke.OpenCovered(window, "midnight-reels");await Task.Delay(100);
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

        NavigationSmoke.OpenCovered(window, "neon-vault");await Task.Delay(120);window.UpdateLayout();
        Require(Find<CheckBox>(window).Single(b => b.Name == "SoundToggle").IsChecked == false, "Neon Vault inherited Reels sound toggle.");
        Require(Application.Current.Windows.Cast<Window>().Count(w=>w.IsVisible)==1,"Neon Vault opened a second visible native window instead of using the casino host");
        Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,"Host casino header remained visible over embedded Neon Vault");
        Require(Find<ProgressBar>(window).Single(b=>b.Name=="EnergyBar").Maximum==NeonVaultRules.VaultEnergyTarget,"Neon Vault energy meter is not wired to the feature target");
        foreach(var size in new[]{(1080d,720d),(1440d,900d),(1920d,1080d),(2560d,1440d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(50);
            Button spin=Find<Button>(window).Single(b=>b.Name=="SpinButton");
            Rect bounds=spin.TransformToAncestor(window).TransformBounds(new Rect(0,0,spin.ActualWidth,spin.ActualHeight));
            Require(bounds.Top>=60 && bounds.Bottom<=window.ActualHeight-8 && bounds.Left>=0 && bounds.Right<=window.ActualWidth,"Neon Vault Spin button clipped at "+size);
            Require(Find<Button>(window).Any(b=>b.Name=="MaxBetButton" && b.IsVisible),"Neon Vault high-roller MAX bet control is missing");
            Capture(window,"05-neon-vault-"+(int)size.Item1);
        }
        Find<Button>(window).Single(b=>b.Name=="BackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(50);
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Leaving Neon Vault did not return to the lobby");
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible
            == (!ModernLobbyView.IsAvailable || Find<ModernLobbyView>(window).Any(v=>v.Failed)),
            "Lobby header visibility did not match the active React/WPF presentation after Neon Vault");

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
        Require(!((Button)window.FindName("DealButton")).IsEnabled,"Covered table can still receive keyboard activation");
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
        // Hand History and lifetime statistics now belong to the React shell.
        // In the legacy no-WebView2 run both destinations remain native fallbacks.
        if (profileHub is { Ready: true } modernProfile)
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
            await modernProfile.Browser.CoreWebView2.ExecuteScriptAsync(
                "[...document.querySelectorAll('.account-routes button')].find(x=>/Statistics|Statistik/.test(x.textContent||''))?.click()");
            for (int i=0;i<70 && modernProfile.RecordsPage!="statistics";i++) await Task.Delay(80);
            Require(modernProfile.RecordsPage=="statistics" && modernProfile.IsVisible,
                "Profile Statistics did not migrate to React.");
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
        window.Width=1440;window.Height=900;window.Modes.Open("blackjack");mode.SelectedIndex=1;theme.SelectedIndex=0;
        Require(((FrameworkElement)window.FindName("TableSurface")).IsVisible
            && ((FrameworkElement)window.FindName("BlackjackSidebar")).IsVisible
            && ((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,
            "The Blackjack route did not reveal its native game surface.");
        ((Button)window.FindName("DealButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(500);window.UpdateLayout();
        var viewport=(ScrollViewer)window.FindName("PlayerHandViewport");var hands=(FrameworkElement)window.FindName("PlayerHandsPanel");
        Rect handBounds=hands.TransformToAncestor(viewport).TransformBounds(new Rect(0,0,hands.ActualWidth,hands.ActualHeight));
        Require(handBounds.Top>=-1 && handBounds.Bottom<=viewport.ActualHeight+1 && handBounds.Height>=90,"Player cards clipped or reduced below readable size in AI table");
        Capture(window,"07-blackjack-dealt");
        await CheckWagerGeometry(window);
        await CheckResultDeal(window);
    }

    private static async Task CheckEmpire(MainWindow window)
    {
        int nativeWindows=Application.Current.Windows.Count;
        NavigationSmoke.OpenCovered(window, "casino-empire");await Task.Delay(100);window.UpdateLayout();
        RequireSingleHost(window,"Casino Empire");
        Require(Application.Current.Windows.Count==nativeWindows,"Casino Empire constructed a native Window");
        Require(Find<EmpireView>(window).Count()==1,"Casino Empire did not mount its hosted page");
        Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,"Host header covers Casino Empire");
        Require(!((Button)window.FindName("DealButton")).IsEnabled,"Covered Blackjack input is still enabled");
        var service=(BlackjackCasino.Core.Empire.EmpireService)typeof(MainWindow)
            .GetField("empire",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!;
        var buy=Find<Button>(window).Single(b=>b.Name=="EmpireUpgradeButton");
        Find<Button>(window).Single(b=>b.Name=="EmpireRoom_slots").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(buy.IsEnabled,"The starter room cannot be upgraded");
        buy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(service.Level("slots")==2,"Room upgrade did not reach the idle economy");
        foreach(var size in new[]{(1080d,720d),(1440d,900d),(1920d,1080d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(50);
            RequireWithin(window,Find<Button>(window).Single(b=>b.Name=="EmpireBackButton"),"Empire Back");
            RequireWithin(window,buy,"Empire Upgrade");
            RequireWithin(window,Find<Button>(window).Single(b=>b.Name=="EmpireClaimButton"),"Empire claim");
            var roomCards=Find<EmpireRoomCard>(window).Where(card=>card.IsVisible).ToArray();
            Require(roomCards.Length==5,"The estate dashboard lost a room");
            Require(roomCards.All(card=>card.ActualWidth<360 && card.ActualHeight<=310),"Room gallery exceeds its responsive artwork size");
            Require(roomCards.All(card => Math.Abs(card.TransformToAncestor(window).Transform(new Point()).Y
                - roomCards[0].TransformToAncestor(window).Transform(new Point()).Y) < 1), "A room wrapped below the gallery's first row at a standard width");
            Require(roomCards[0].TransformToAncestor(window).Transform(new Point()).X<roomCards[1].TransformToAncestor(window).Transform(new Point()).X,"Room cards failed to form a responsive row");
            Capture(window,"09-empire-"+(int)size.Item1);
        }
        Find<Button>(window).Single(b=>b.Name=="EmpireFilter_open").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
        Require(Find<EmpireRoomCard>(window).Count(card=>card.IsVisible)==1,"Operating filter is incorrect");
        Require(Find<EmpireRoomCard>(window).Where(card=>card.IsVisible).All(card=>card.ActualWidth<360),"Filtering stretched one room across the estate");
        Find<Button>(window).Single(b=>b.Name=="EmpireFilter_locked").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
        Require(Find<EmpireRoomCard>(window).Count(card=>card.IsVisible)==4,"Locked filter is incorrect");
        Find<Button>(window).Single(b=>b.Name=="EmpireFilter_all").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
        Require(EmpireRoomTextures.Get(0).IsFrozen && EmpireRoomTextures.Get(0).PixelWidth>=256 && ReferenceEquals(EmpireRoomTextures.Get(0),EmpireRoomTextures.Get(0)),"Room artwork is missing or not cached");
        var animationSetting=(CheckBox)window.FindName("AnimationsCheckBox");
        animationSetting.IsChecked=true;await Task.Delay(150);
        Require(Find<EmpireRoomCard>(window).All(card=>card.Artwork.Phase==0),"Blackjack's animation toggle leaked into Casino Empire.");
        var preferencesField = typeof(MainWindow).GetField("preferences", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        AppPreferences areaPrefs = (AppPreferences)preferencesField.GetValue(window)!;
        preferencesField.SetValue(window, areaPrefs.WithPresentation("casino-empire", areaPrefs.Presentation("casino-empire") with { AnimationsEnabled = true }));
        // A reflection-only change bypasses the real preference UI. Deliver its
        // presentation update to the view, just like the regular economy refresh does.
        EmpireView currentEmpire = Find<EmpireView>(window).Single();
        currentEmpire.RefreshLive();
        EmpireRoomCard slotsCard = Find<EmpireRoomCard>(window).Single(card => card.Department.Id == "slots");
        Require(currentEmpire.IsVisible && currentEmpire.IsLoaded && slotsCard.Artwork.Open,
            "The open slot-room artwork is not mounted in the visible Empire.");
        var animationDeadline = System.Diagnostics.Stopwatch.StartNew();
        while (Find<EmpireRoomCard>(window).Single(card => card.Department.Id == "slots").Artwork.Phase == 0
            && animationDeadline.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(50);
        Require(slotsCard.Artwork.Phase > 0,
            $"Empire's local animation did not activate (view loaded={currentEmpire.IsLoaded}, visible={currentEmpire.IsVisible}, room open={slotsCard.Artwork.Open}, phase={slotsCard.Artwork.Phase}).");
        animationSetting.IsChecked=false;
        await Task.Delay(150);
        Require(Find<EmpireRoomCard>(window).Single(card=>card.Department.Id=="slots").Artwork.Phase>0,"Blackjack toggle stopped Empire's local effects.");
        preferencesField.SetValue(window, areaPrefs);
        foreach (EmpireView view in Find<EmpireView>(window)) view.StopVisualAnimations();
        Find<Button>(window).Single(b=>b.Name=="EmpireBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Modes.Open("blackjack");await Task.Delay(50);window.UpdateLayout();
        var indicator=(Border)window.FindName("EmpireBalancePanel");
        Require(indicator.IsVisible,"The Blackjack header does not show the live idle balance");
        decimal before=service.Credits;
        await Task.Delay(1100);window.UpdateLayout();
        Require(service.Credits>before,"Idle income stopped on the Blackjack table");
        var ticker=(EmpireBalanceCounter)window.FindName("EmpireHeaderBalance");
        Require(Math.Abs(ticker.DisplayedAmount-(double)service.Credits)<.01,"The header ticker is not showing the current idle balance");
        foreach(var size in new[]{(1080d,720d),(1440d,900d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();
            RequireWithin(window,indicator,"Empire header balance");
            Rect cash=((FrameworkElement)window.FindName("BankrollText")).TransformToAncestor(window).TransformBounds(new Rect(((FrameworkElement)window.FindName("BankrollText")).RenderSize));
            Rect idle=indicator.TransformToAncestor(window).TransformBounds(new Rect(indicator.RenderSize));
            Rect profile=Find<Button>(window).Single(b=>b.Name=="PlayerProfileButton").TransformToAncestor(window).TransformBounds(new Rect(Find<Button>(window).Single(b=>b.Name=="PlayerProfileButton").RenderSize));
            Require(!cash.IntersectsWith(idle) && !profile.IntersectsWith(idle),"Header cash/profile overlaps the idle balance");
            Capture(window,"09-empire-header-"+(int)size.Item1);
        }
        var balanceSetting=(CheckBox)window.FindName("EmpireBalanceCheckBox");
        balanceSetting.IsChecked=false;Require(indicator.Visibility==Visibility.Collapsed,"The header balance setting does not hide the indicator");
        balanceSetting.IsChecked=true;Require(indicator.IsVisible,"The header balance setting does not restore the indicator");
        NavigationSmoke.OpenCovered(window, "casino-empire");await Task.Delay(50);window.UpdateLayout();
        buy=Find<Button>(window).Single(b=>b.Name=="EmpireUpgradeButton");
        Find<Button>(window).Single(b=>b.Name=="EmpireRoom_vip").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(!buy.IsEnabled,"An unaffordable room can be opened");
        Find<Button>(window).Single(b=>b.Name=="EmpireRoom_slots").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var session=(CasinoSession)typeof(MainWindow).GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!;
        decimal bankroll=session.Bankroll;
        service.Accrue(DateTimeOffset.UtcNow.AddHours(1));
        Find<EmpireView>(window).Single().Refresh();
        decimal claim=service.ClaimableDividend;
        Require(claim>0,"Owner dividend did not accrue");
        Find<Button>(window).Single(b=>b.Name=="EmpireClaimButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(session.Bankroll==bankroll+claim,"Dividend did not reach the shared wallet");
        Find<Button>(window).Single(b=>b.Name=="EmpireClaimButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(session.Bankroll==bankroll+claim,"Dividend was paid twice");
        foreach(var department in BlackjackCasino.Core.Empire.EmpireCatalog.Departments)
            for(int i=0;i<10;i++) service.Upgrade(department.Id,DateTimeOffset.UtcNow);
        Find<EmpireView>(window).Single().Refresh();window.UpdateLayout();
        Capture(window,"09-empire-developed-estate");
        decimal credits=service.Credits;
        Find<Button>(window).Single(b=>b.Name=="EmpireBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Empire Back did not restore the lobby");
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible
            == (!ModernLobbyView.IsAvailable || Find<ModernLobbyView>(window).Any(v=>v.Failed)),
            "Empire Back did not restore the appropriate React/WPF lobby header");
        NavigationSmoke.OpenCovered(window, "casino-empire");await Task.Delay(40);window.UpdateLayout();
        Require(service.Level("slots")>=2 && service.Credits==credits,"Reopening Casino Empire reset progress");
        var source=PresentationSource.FromVisual(window)!;
        window.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,source,0,System.Windows.Input.Key.Escape)
            {RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent});
        await Task.Delay(40);
        Require(!Find<EmpireView>(window).Any(),"Escape did not close the hosted idle page");
        Require(Application.Current.Windows.Count==nativeWindows,"Casino Empire leaked a native Window");
    }
    private sealed class WagerShoe : ICardShoe
    {
        public int Remaining {get;private set;}=312;
        public Card Draw(){Remaining--;return new Card(Rank.Eight,Suit.Clubs);}
    }
    private static async Task CheckWagerGeometry(MainWindow window)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var sessionField=typeof(MainWindow).GetField("session",flags)!;
        var refresh=typeof(MainWindow).GetMethod("RefreshGameUi",flags)!;
        foreach(bool fullTable in new[]{false,true})
        foreach(int handCount in new[]{1,2,4})
        {
            typeof(MainWindow).GetMethod("SwitchTable",flags)!.Invoke(window,[fullTable?PlayModePreference.FullTable:PlayModePreference.Solo,5]);
            ((ComboBox)window.FindName("GameModeComboBox")).SelectedIndex=fullTable?1:0;
            ((ComboBox)window.FindName("AiSeatCountComboBox")).SelectedIndex=4;
            AiPlayerProfile[] ai=fullTable?[new("A",10m),new("B",10m),new("C",10m),new("D",10m),new("E",10m)]:[];
            var fixture=new CasinoSession(new GameRules {MaxSplitHands=4,AllowInsurance=false},10_000m,()=>new WagerShoe(),ai);
            fixture.StartRound(138.25m);
            for(int i=1;i<handCount;i++) fixture.Act(PlayerAction.Split);
            sessionField.SetValue(window,fixture);refresh.Invoke(window,null);
            foreach(var size in new[]{(1080d,720d),(1440d,900d),(2560d,1440d)})
            {
                window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(30);
                Capture(window,"08-wager-"+(fullTable?"full":"solo")+"-"+handCount+"-"+(int)size.Item1);
                var viewport=(ScrollViewer)window.FindName("PlayerHandViewport");
                var table=(FrameworkElement)window.FindName("TableArt");
                Require(((ScrollViewer)window.FindName("AiSeatsScrollViewer")).IsVisible==fullTable,"Fixture did not enter requested table mode");
                var wagers=Find<TableWagerView>(window).ToArray();
                Require(wagers.Length==handCount,"Wager count differs from player hands");
                foreach(var wager in wagers)
                {
                    Require(System.Windows.Automation.AutomationProperties.GetName(wager).Contains(TableWagerPresentation.Amount(138.25m)),"Chip label does not match actual wager");
                    Rect bounds=wager.TransformToAncestor(viewport).TransformBounds(new Rect(0,0,wager.ActualWidth,wager.ActualHeight));
                    Require(bounds.Top>=-1 && bounds.Bottom<=viewport.ActualHeight+1,"Wager clipped in "+size+" / "+handCount+" hands");
                }
                TextBlock[] headings=Find<TextBlock>(window).Where(t=>t.Text=="YOUR HAND" || (t.Text.StartsWith("HAND ") && t.Text.Length<=6)).ToArray();
                Require(headings.Length==handCount,"Player hand headings are missing");
                foreach(double offset in new[]{0d,viewport.ScrollableWidth}.Distinct())
                {
                    viewport.ScrollToHorizontalOffset(offset);window.UpdateLayout();
                    int visibleHeadings=0;
                    foreach(TextBlock heading in headings)
                    {
                        Rect inViewport=heading.TransformToAncestor(viewport).TransformBounds(new Rect(0,0,heading.ActualWidth,heading.ActualHeight));
                        // Resplits intentionally scroll horizontally to keep cards legible.
                        // Offscreen content has no relationship to the native felt arc.
                        if(inViewport.Left<0 || inViewport.Right>viewport.ActualWidth) continue;
                        visibleHeadings++;
                        Rect bounds=heading.TransformToVisual(table).TransformBounds(new Rect(0,0,heading.ActualWidth,heading.ActualHeight));
                        for(int sample=0;sample<=200;sample++)
                        {
                            double t=sample/200d,u=1-t;
                            double x=table.ActualWidth*(u*u*u*.14+3*u*u*t*.32+3*u*t*t*.68+t*t*t*.86);
                            double y=table.ActualHeight*(u*u*u*.66+3*u*u*t*.39+3*u*t*t*.39+t*t*t*.66);
                            if(x>=bounds.Left && x<=bounds.Right)
                                Require(bounds.Top>=y+4,"Player heading overlaps the felt arc in "+size+" / "+handCount+" hands / full="+fullTable+"; heading="+bounds.Top+", arc="+y);
                        }
                    }
                    Require(visibleHeadings>0,"No player heading is visible in the hand viewport");
                }
                Capture(window,"08-wager-"+(fullTable?"full":"solo")+"-"+handCount+"-"+(int)size.Item1);
            }
        }
        var animations=(CheckBox)window.FindName("AnimationsCheckBox");
        animations.IsChecked=true;
        var last=(CasinoSession)sessionField.GetValue(window)!;
        last.Act(PlayerAction.DoubleDown);refresh.Invoke(window,null);window.UpdateLayout();
        Require(Find<TableChipDrawing>(window).Any(chips=>chips.HasAnimatedProperties),"Changing a wager did not animate its chips");
        animations.IsChecked=false;
        Require(Find<TableChipDrawing>(window).All(chips=>!chips.HasAnimatedProperties),"Disabling animations did not stop active chip clocks immediately");
    }
    private static async Task CheckResultDeal(MainWindow window)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var fixture = new CasinoSession(new GameRules { AllowInsurance = false }, 10000m, () => new WagerShoe());
        fixture.StartRound(100m);
        fixture.Act(PlayerAction.Stand);
        Require(!fixture.RoundInProgress, "Result Deal fixture did not settle");
        typeof(MainWindow).GetField("session", flags)!.SetValue(window, fixture);
        typeof(MainWindow).GetField("selectedBet", flags)!.SetValue(window, 100m);
        typeof(MainWindow).GetMethod("RefreshGameUi", flags)!.Invoke(window, null);
        await Task.Delay(60); window.UpdateLayout();
        var original = (Button)window.FindName("DealButton");
        var resultDeal = (Button)window.FindName("ResultDealButton");
        Require(resultDeal.IsVisible && resultDeal.IsEnabled == original.IsEnabled && resultDeal.IsEnabled, "Result Deal is missing or out of sync with betting controls");
        var round = fixture.CurrentRound;
        Capture(window, "blackjack-result-deal");
        resultDeal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.UpdateLayout();
        Require(!ReferenceEquals(round, fixture.CurrentRound), "Result Deal did not use the normal deal action");
        Require(!resultDeal.IsVisible || !fixture.RoundInProgress, "Result Deal remained visible during play");
    }
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
        CheckDark(combo);combo.Focus();CheckDark(combo);
        combo.IsDropDownOpen=true;await Task.Delay(25);
        var popup=(Popup)combo.Template.FindName("PART_Popup",combo);
        Require(popup.IsOpen,"Dropdown did not open");
        combo.IsDropDownOpen=false;bool enabled=combo.IsEnabled;combo.IsEnabled=false;CheckDark(combo);combo.IsEnabled=enabled;
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
    private static void Capture(FrameworkElement target,string name){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(Render(target)));using var file=File.Create(Path.Combine(output,name+".png"));png.Save(file);}
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
