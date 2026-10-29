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

internal static class Program
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
            try {await Check(window,qaProfile);File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: startup lobby, registered future mode, shared hover/focus/open/disabled dropdown contracts, all table themes, proportional controls, embedded Reels and Neon Vault bounds, full-surface single-window casino hosting, embedded profile rename, nested profile/progression navigation, compact challenges/shop search and pagination. Mouse hover capability recorded separately.");}
            catch(Exception ex){exit=1;File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());}
            finally {window.Close();CasinoAudio.Shutdown();app.Shutdown(exit);}
        };
        window.Show();app.Run();return exit;
    }
    private static async Task Check(MainWindow window,PlayerProfileState qaProfile)
    {
        await Task.Delay(200);Capture(window,"01-lobby");
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Application did not start in the lobby");
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
        window.Modes.Open("midnight-reels");await Task.Delay(100);
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
        Find<Button>(window).Single(b=>b.Name=="ReelsBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(50);
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Leaving a slot did not return to the lobby");
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,"Host casino header did not return after leaving Midnight Reels");

        window.Modes.Open("neon-vault");await Task.Delay(120);window.UpdateLayout();
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
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,"Host casino header did not return after leaving Neon Vault");

        Find<Button>(window).Single(b=>b.Name=="PlayerProfileButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(100);window.UpdateLayout();
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
        Find<Button>(window).Single(b=>b.Name=="ProfileProgressionButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(100);window.UpdateLayout();RequireSingleHost(window,"Progression from profile");
        TabControl progressionTabs=Find<TabControl>(window).Single(t=>t.Items.OfType<TabItem>().Any(i=>Equals(i.Header,"Challenges")));
        foreach(string tab in new[]{"Overview","Challenges","Cosmetic Shop"})
        {
            progressionTabs.SelectedItem=progressionTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,tab));
            foreach(var size in new[]{(1080d,720d),(1440d,900d)})
            {
                window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(40);
                Capture(window,"06-progression-"+tab.Replace(' ','-')+"-"+(int)size.Item1);
            }
        }
        progressionTabs.SelectedItem=progressionTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"Challenges"));window.UpdateLayout();
        var challengeSearch=Find<TextBox>(window).Single(t=>t.Name=="ChallengeSearchBox");
        Require(Find<StackPanel>(window).Single(p=>p.Name=="ChallengesPanel").Children.OfType<Border>().Count()<=4,"Challenge pagination renders an unbounded list");
        CheckCollectionPaging(window,"ChallengesPanel","ChallengeNextButton","ChallengePreviousButton");
        ChallengeDefinition[] ready=ChallengeCatalog.All.Where(c=>ChallengeCatalog.Complete(qaProfile,c) && !qaProfile.ClaimedChallengeIds.Contains(c.Id)).ToArray();
        Require(ready.Length>0,"QA profile did not prepare claimable challenges");
        challengeSearch.Text="Neon Vault";window.UpdateLayout();
        long coinsBefore=qaProfile.Coins;
        Find<Button>(window).Single(b=>b.Name=="ClaimAllButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(qaProfile.Coins==coinsBefore+ready.Sum(c=>c.CoinReward),"UI Claim All omitted rewards hidden by the active search");
        Require(ready.All(c=>qaProfile.ClaimedChallengeIds.Contains(c.Id)),"UI Claim All did not retain all claimed IDs");
        Require(!Find<Button>(window).Single(b=>b.Name=="ClaimAllButton").IsEnabled,"Claim All stayed enabled after all ready rewards were claimed");
        challengeSearch.Text="__no_matching_challenge__";window.UpdateLayout();
        Require(!Find<Button>(window).Any(b=>b.Tag is string id && ChallengeCatalog.TryGet(id) is not null),"Challenge search did not filter rows");
        challengeSearch.Clear();
        progressionTabs.SelectedItem=progressionTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"Cosmetic Shop"));window.UpdateLayout();
        var shopSearch=Find<TextBox>(window).Single(t=>t.Name=="ShopSearchBox");
        CheckCollectionPaging(window,"ShopPanel","ShopNextButton","ShopPreviousButton");
        shopSearch.Text="Moon Cat";window.UpdateLayout();
        Require(Find<Button>(window).Any(b=>Equals(b.Tag,"portrait-moon-cat")),"Shop search failed to find the requested portrait");
        shopSearch.Clear();
        Find<Button>(window).Single(b=>b.Name=="ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);window.UpdateLayout();
        Require(Find<TextBox>(window).Any(t=>t.Name=="ProfileNameBox" && t.Text=="Midnight Timo"),"Progression Back did not restore the profile");
        Find<Button>(window).Single(b=>b.Name=="ProfileBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);window.UpdateLayout();
        Require(Find<CasinoLobbyView>(window).Any(v=>v.IsVisible),"Closing the profile lost the previous lobby");
        Find<Button>(window).Single(b=>Equals(b.Content,"Challenges") && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(60);window.UpdateLayout();RequireSingleHost(window,"Lobby Challenges");
        Require(Find<TabControl>(window).Any(t=>t.SelectedItem is TabItem item && Equals(item.Header,"Challenges")),"Lobby shortcut selected the wrong tab");
        Find<Button>(window).Single(b=>b.Name=="ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(40);
        window.Width=1440;window.Height=900;window.Modes.Open("blackjack");mode.SelectedIndex=1;theme.SelectedIndex=0;
        ((Button)window.FindName("DealButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(500);window.UpdateLayout();
        var viewport=(ScrollViewer)window.FindName("PlayerHandViewport");var hands=(FrameworkElement)window.FindName("PlayerHandsPanel");
        Rect handBounds=hands.TransformToAncestor(viewport).TransformBounds(new Rect(0,0,hands.ActualWidth,hands.ActualHeight));
        Require(handBounds.Top>=-1 && handBounds.Bottom<=viewport.ActualHeight+1 && handBounds.Height>=90,"Player cards clipped or reduced below readable size in AI table");
        Capture(window,"07-blackjack-dealt");
        await CheckWagerGeometry(window);
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
            ((ComboBox)window.FindName("GameModeComboBox")).SelectedIndex=fullTable?1:0;
            AiPlayerProfile[] ai=fullTable?[new("A",10m),new("B",10m),new("C",10m),new("D",10m),new("E",10m)]:[];
            var fixture=new CasinoSession(new GameRules {MaxSplitHands=4,AllowInsurance=false},10_000m,()=>new WagerShoe(),ai);
            fixture.StartRound(138.25m);
            for(int i=1;i<handCount;i++) fixture.Act(PlayerAction.Split);
            sessionField.SetValue(window,fixture);refresh.Invoke(window,null);
            foreach(var size in new[]{(1080d,720d),(1440d,900d),(2560d,1440d)})
            {
                window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(30);
                var viewport=(ScrollViewer)window.FindName("PlayerHandViewport");
                var table=(FrameworkElement)window.FindName("TableArt");
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
                foreach(TextBlock heading in headings)
                {
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
                Capture(window,"08-wager-"+(fullTable?"full":"solo")+"-"+handCount+"-"+(int)size.Item1);
            }
        }
    }
    private static void CheckCollectionPaging(MainWindow window,string panelName,string nextName,string previousName)
    {
        string[] Ids()=>Find<StackPanel>(window).Single(p=>p.Name==panelName).Children.OfType<Border>()
            .SelectMany(p=>Find<Button>(p)).Select(b=>b.Tag).OfType<string>().ToArray();
        string[] first=Ids();Require(first.Length==4,"Expected four compact rows in "+panelName);
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
