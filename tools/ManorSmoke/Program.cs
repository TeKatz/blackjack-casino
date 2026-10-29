using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Windows.Input;
using System.Xml.Linq;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;

internal static class Program
{
    private static string output="";
    private static readonly Queue<int> draws=new();
    private static int failures;
    [STAThread]
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentUICulture=CultureInfo.CurrentCulture;
        output=Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/manor-windows-smoke"); Directory.CreateDirectory(output);
        var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
        // Load the exact shared resource source without starting the normal casino/updater window.
        app.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoControls.xaml",UriKind.Relative)});
        app.DispatcherUnhandledException+=(_,e)=> { failures++; File.WriteAllText(Path.Combine(output,"fatal.txt"),e.Exception.ToString()); e.Handled=true; app.Shutdown(1); };
        var preferences=new AppPreferences {Bankroll=100_000,ManorBet=1,SoundEnabled=false,MusicEnabled=false,AnimationsEnabled=false};
        var progression=new PlayerProgressionController(new PlayerProfileState());
        var persistence=new ManorPersistence(Path.Combine(output,"save"));
        var controller=new ManorController(preferences,preferences.Bankroll,progression,_=>{},persistence,
            max=>draws.Count>0 ? draws.Dequeue() : 0);
        var window=new ManorWindow(controller);
        window.Loaded+=async(_,_)=>
        {
            try { await Check(window,controller,persistence); File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: WPF window, vector rendering, minimum size, 2x export, mouse/keyboard route actions, feature close/resume, free spins, speed/auto cancellation, wallet/XP checkpoint. Audio quality and actual monitor DPI remain manual checks."); }
            catch(Exception ex) { failures++; File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString()); }
            finally { if(window.IsVisible) window.Close(); CasinoAudio.Shutdown(); app.Shutdown(failures==0?0:1); }
        };
        window.Show(); app.Run(); return failures==0?0:1;
    }
    private static T Named<T>(ManorWindow w,string name) where T:class => w.FindName(name) as T ?? throw new Exception("Missing control "+name);
    private static void Click(ManorWindow w,string name)=>Named<Button>(w,name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> done,int maxMs=10_000)
    {
        var watch=Stopwatch.StartNew(); while(!done() && watch.ElapsedMilliseconds<maxMs) await Task.Delay(30);
        Require(done(),"UI transition timed out");
    }
    private static void Capture(ManorWindow w,string name,double dpi=96)
    {
        w.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)(w.ActualWidth*dpi/96),(int)(w.ActualHeight*dpi/96),dpi,dpi,PixelFormats.Pbgra32);
        bitmap.Render(w); var png=new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file=File.Create(Path.Combine(output,name+".png")); png.Save(file);
    }
    private static void CheckDropdown(ManorWindow w,string name)
    {
        ComboBox combo=Named<ComboBox>(w,name); combo.ApplyTemplate();
        var toggle=(ToggleButton)combo.Template.FindName("DropDownToggle",combo);
        toggle.ApplyTemplate();
        Require(toggle.Template.Triggers.Count==0,"Dropdown still has theme hover/disabled triggers");
        var hitArea=VisualTreeHelper.GetChild(toggle,0) as Border;
        Require(hitArea?.Background is SolidColorBrush {Color.A:0},"Dropdown overlay must stay transparent in every state");
        // Pixel check the actual composed control, including its disabled state in-spin.
        var drawing=new DrawingVisual();
        using(var d=drawing.RenderOpen()) d.DrawRectangle(new VisualBrush(combo),null,new Rect(0,0,combo.ActualWidth,combo.ActualHeight));
        var bitmap=new RenderTargetBitmap((int)combo.ActualWidth,(int)combo.ActualHeight,96,96,PixelFormats.Pbgra32);
        bitmap.Render(drawing); byte[] pixels=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];
        bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);
        int light=Enumerable.Range(0,pixels.Length/4).Count(i=>pixels[i*4]>180 && pixels[i*4+1]>180 && pixels[i*4+2]>180 && pixels[i*4+3]>128);
        Require(light<(pixels.Length/4)*0.2,"Dropdown turned into a light rectangle");
    }
    private static int[] Feature(ManorSymbol symbol,int count)=>Enumerable.Range(0,5).Select(reel=>Enumerable.Range(0,64).First(stop=>
    {
        var visible=Enumerable.Range(0,3).Select(row=>ManorRules.SymbolAt(reel,stop,row)).ToArray();
        return reel<count ? visible.Contains(symbol) : !visible.Contains(ManorSymbol.Scatter)&&!visible.Contains(ManorSymbol.Bonus);
    })).ToArray();
    private static void Draw(params int[] values) { draws.Clear(); foreach(int value in values) draws.Enqueue(value); }
    private static async Task Check(ManorWindow w,ManorController c,ManorPersistence persistence)
    {
        await PortraitSmoke.Check(output);
        await AudioSmoke.Check(output);
        await Task.Delay(100); Capture(w,"01-cabinet");
        CheckDropdown(w,"SpeedBox"); CheckDropdown(w,"AutoBox");
        ComboBox speedBox=Named<ComboBox>(w,"SpeedBox"); speedBox.IsDropDownOpen=true;
        await Task.Delay(100);
        var popup=(Popup)speedBox.Template.FindName("PART_Popup",speedBox);
        Require(popup.IsOpen && popup.Child is Border {Background:SolidColorBrush},"Dark dropdown popup did not open");
        speedBox.IsDropDownOpen=false;
        w.Width=w.MinWidth; w.Height=w.MinHeight; await Task.Delay(50); Capture(w,"02-minimum");
        w.Width=1280; w.Height=890; Capture(w,"03-double-resolution",192);
        // Exercise actual routed button events and real WPF animations, not a second fake UI.
        Named<CheckBox>(w,"AnimationBox").IsChecked=true;
        Draw(0,0,0,0,0); Click(w,"SpinButton");
        await Task.Delay(200);
        Require(!speedBox.IsEnabled,"Speed dropdown must be locked during a committed spin");
        CheckDropdown(w,"SpeedBox"); CheckDropdown(w,"AutoBox");
        Require(Named<ManorEffects>(w,"ReelEffects").Activity>0,"Spinning reel effects missing");
        Capture(w,"06-in-spin-dark-dropdown");
        await Until(()=>Named<Button>(w,"SpinButton").IsEnabled);
        Require(c.State.Statistics.PaidSpins==1,"Animated paid spin not recorded");
        // Force a real regular win through authored stops and capture its live glow/trace.
        int[] winning=Enumerable.Range(0,5).Select(reel=>Enumerable.Range(0,64).First(stop=>ManorRules.SymbolAt(reel,stop,1)==ManorSymbol.Jack)).ToArray();
        Draw(winning); Click(w,"SpinButton");
        await Until(()=>Named<Canvas>(w,"PaylineCanvas").Children.Count>0);
        Require(Named<ManorEffects>(w,"ReelEffects").Burst>0,"Winning particle animation missing");
        Capture(w,"07-bright-win-and-particles");
        await Task.Delay(180); Capture(w,"08-win-motion-later");
        await Until(()=>Named<Button>(w,"SpinButton").IsEnabled);
        Named<CheckBox>(w,"AnimationBox").IsChecked=false;
        Require(!Named<ManorEffects>(w,"ReelEffects").HasAnimatedProperties,"Animation preference left particle clocks running");
        Named<CheckBox>(w,"AnimationBox").IsChecked=true;
        foreach(int speed in new[]{1,2})
        {
            Named<ComboBox>(w,"SpeedBox").SelectedIndex=speed;
            long before=c.State.Statistics.PaidSpins; Draw(0,0,0,0,0); Click(w,"SpinButton");
            await Until(()=>c.State.Statistics.PaidSpins==before+1 && Named<Button>(w,"SpinButton").IsEnabled);
        }
        Named<CheckBox>(w,"AnimationBox").IsChecked=false;
        int[] visit=Feature(ManorSymbol.Bonus,2); Draw([..visit,2]); Click(w,"SpinButton");
        await Until(()=>Named<Grid>(w,"BonusPanel").Visibility==Visibility.Visible);
        Require(c.State.Run?.PrizeScale==0.1m,"Two Doors did not enter the smaller real Manor run");
        Require(Named<TextBlock>(w,"RoomText").Text.Contains("25×"),"Two-Door prize tier not displayed");
        Capture(w,"09-two-door-manor-visit");
        Click(w,"LeaveButton"); await Until(()=>!c.State.CycleActive && Named<Button>(w,"SpinButton").IsEnabled);
        int[] bonus=Feature(ManorSymbol.Bonus,3); Draw([..bonus,2]); Click(w,"SpinButton");
        await Until(()=>Named<Grid>(w,"BonusPanel").Visibility==Visibility.Visible);
        Require(c.State.Run is {Status:ManorRunStatus.Hunting},"Interactive Manor did not pause");
        Capture(w,"04-manor-map");
        foreach(ManorArea area in Enum.GetValues<ManorArea>())
        {
            Named<ManorBackdrop>(w,"RoomScene").Scene=area.ToString(); Capture(w,"room-"+area);
        }
        long xp=c.Profile.LifetimeExperience;
        w.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),Environment.TickCount,Key.R) {RoutedEvent=Keyboard.PreviewKeyDownEvent});
        await Task.Delay(20);
        Require(c.State.Run!.Turn==1,"Recommended route did not advance real state");
        w.Close();
        ManorCheckpoint saved=persistence.Load()!; Require(saved.Machine.Run is {Status:ManorRunStatus.Hunting},"Closing lost pending bonus");
        var restoredProfile=ManorPersistence.Clone(c.Profile);
        var nextProgression=new PlayerProgressionController(restoredProfile);
        var next=new ManorController(c.Preferences,c.State.Bankroll,nextProgression,_=>{},persistence,max=>draws.Count>0?draws.Dequeue():0);
        var resumed=new ManorWindow(next); resumed.Show(); await Task.Delay(60);
        await Until(()=>Named<Grid>(resumed,"BonusPanel").Visibility==Visibility.Visible);
        Click(resumed,"LeaveButton"); await Until(()=>!next.State.CycleActive);
        Require(next.Profile.LifetimeExperience>xp,"Bonus did not grant completion XP");
        Capture(resumed,"05-resumed-and-settled");
        int[] scatter=Feature(ManorSymbol.Scatter,3); Draw([..scatter]);
        long freeBefore=next.State.Statistics.FreeSpins; Click(resumed,"SpinButton");
        await Until(()=>!next.State.CycleActive && next.State.Statistics.FreeSpins==freeBefore+8);
        Require(next.State.Statistics.TotalWagered==next.State.Statistics.PaidSpins,"Free spins charged wagers");
        Named<CheckBox>(resumed,"InteractiveBox").IsChecked=false;
        Draw([..bonus,2]); Click(resumed,"SpinButton"); await Until(()=>!next.State.CycleActive && Named<Button>(resumed,"SpinButton").IsEnabled);
        Require(next.State.Statistics.AutomaticRuns==1,"Noninteractive bonus did not use the auto path");
        Named<ComboBox>(resumed,"AutoBox").SelectedIndex=1;
        long paidBefore=next.State.Statistics.PaidSpins; Click(resumed,"AutoButton");
        await Until(()=>next.State.Statistics.PaidSpins>=paidBefore+2); Click(resumed,"StopButton");
        long stopped=next.State.Statistics.PaidSpins; await Task.Delay(500);
        Require(next.State.Statistics.PaidSpins==stopped,"Auto cancellation started another spin");
        // Auto + interactive pauses for real choices; cancelling there prevents a restart.
        Named<CheckBox>(resumed,"InteractiveBox").IsChecked=true;
        Draw([..bonus,2]); paidBefore=next.State.Statistics.PaidSpins; Click(resumed,"AutoButton");
        await Until(()=>Named<Grid>(resumed,"BonusPanel").Visibility==Visibility.Visible);
        await Task.Delay(350); Require(next.State.Statistics.PaidSpins==paidBefore+1,"Auto overlapped an interactive bonus");
        Click(resumed,"StopButton"); Click(resumed,"LeaveButton"); await Until(()=>!next.State.CycleActive);
        await Task.Delay(300); Require(next.State.Statistics.PaidSpins==paidBefore+1,"Auto resumed after explicit stop");
        // Auto + skipped interaction resolves a real run and continues without a prompt.
        Named<CheckBox>(resumed,"InteractiveBox").IsChecked=false;
        Draw([..bonus,2]); paidBefore=next.State.Statistics.PaidSpins; Click(resumed,"AutoButton");
        await Until(()=>next.State.Statistics.PaidSpins>=paidBefore+2); Click(resumed,"StopButton");
        // Close during committed reel animation and verify the durable state, not the visual count-up.
        Named<CheckBox>(resumed,"AnimationBox").IsChecked=true;
        Draw(0,0,0,0,0); Click(resumed,"SpinButton"); await Task.Delay(80);
        decimal committed=next.State.Bankroll; long experience=next.Profile.LifetimeExperience;
        resumed.Close();
        var recovered=persistence.Load()!;
        Require(recovered.Preferences.Bankroll==committed && recovered.Profile.LifetimeExperience==experience,"Closing changed payout/XP");
    }
}
