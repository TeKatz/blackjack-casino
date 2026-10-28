using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
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
        var app=new App(); app.InitializeComponent(); app.StartupUri=null!; app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
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
            finally { if(window.IsVisible) window.Close(); app.Shutdown(failures==0?0:1); }
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
    private static int[] Feature(ManorSymbol symbol,int count)=>Enumerable.Range(0,5).Select(reel=>Enumerable.Range(0,64).First(stop=>
    {
        var visible=Enumerable.Range(0,3).Select(row=>ManorRules.SymbolAt(reel,stop,row)).ToArray();
        return reel<count ? visible.Contains(symbol) : !visible.Contains(ManorSymbol.Scatter)&&!visible.Contains(ManorSymbol.Bonus);
    })).ToArray();
    private static void Draw(params int[] values) { draws.Clear(); foreach(int value in values) draws.Enqueue(value); }
    private static async Task Check(ManorWindow w,ManorController c,ManorPersistence persistence)
    {
        await Task.Delay(100); Capture(w,"01-cabinet");
        w.Width=w.MinWidth; w.Height=w.MinHeight; await Task.Delay(50); Capture(w,"02-minimum");
        w.Width=1280; w.Height=890; Capture(w,"03-double-resolution",192);
        // Exercise actual routed button events and real WPF animations, not a second fake UI.
        Named<CheckBox>(w,"AnimationBox").IsChecked=true;
        Draw(0,0,0,0,0); Click(w,"SpinButton");
        await Until(()=>Named<Button>(w,"SpinButton").IsEnabled);
        Require(c.State.Statistics.PaidSpins==1,"Animated paid spin not recorded");
        foreach(int speed in new[]{1,2})
        {
            Named<ComboBox>(w,"SpeedBox").SelectedIndex=speed;
            long before=c.State.Statistics.PaidSpins; Draw(0,0,0,0,0); Click(w,"SpinButton");
            await Until(()=>c.State.Statistics.PaidSpins==before+1 && Named<Button>(w,"SpinButton").IsEnabled);
        }
        Named<CheckBox>(w,"AnimationBox").IsChecked=false;
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
