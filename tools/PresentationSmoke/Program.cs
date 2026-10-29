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
        var window=new MainWindow(new AppPreferences {SoundEnabled=false,MusicEnabled=false,AnimationsEnabled=false,WindowWidth=1280,WindowHeight=900},new PlayerProfileState {DisplayName="Presentation QA",EquippedPortraitId="portrait-moon-cat",OwnedCosmeticIds=[CosmeticCatalog.DefaultTitleId,CosmeticCatalog.DefaultFrameId,CosmeticCatalog.DefaultPortraitId,"portrait-moon-cat"]});
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
            try {await Check(window);File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: startup lobby, registered future mode, shared hover/focus/open/disabled dropdown contracts, all table themes, proportional controls, embedded Reels bounds, standalone profile rename and serialization. Mouse hover capability recorded separately.");}
            catch(Exception ex){exit=1;File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());}
            finally {window.Close();CasinoAudio.Shutdown();app.Shutdown(exit);}
        };
        window.Show();app.Run();return exit;
    }
    private static async Task Check(MainWindow window)
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
        using var profile=new PlayerProgressionController(new PlayerProfileState {DisplayName="Before"});
        var identity=new PlayerProfileWindow(profile);identity.Show();identity.UpdateLayout();
        TextBox name=Find<TextBox>(identity).Single(t=>t.Name=="ProfileNameBox");name.Text="Midnight Timo";
        Find<Button>(identity).Single(b=>b.Name=="RenameProfileButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(profile.Profile.DisplayName=="Midnight Timo","Profile rename failed");
        Require(System.Text.Json.JsonSerializer.Deserialize<PlayerProfileState>(System.Text.Json.JsonSerializer.Serialize(profile.Profile))!.DisplayName=="Midnight Timo","Save lost profile name");
        Capture(identity,"05-profile");identity.Close();
        window.Width=1440;window.Height=900;window.Modes.Open("blackjack");mode.SelectedIndex=1;theme.SelectedIndex=0;
        ((Button)window.FindName("DealButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(500);window.UpdateLayout();
        var viewport=(ScrollViewer)window.FindName("PlayerHandViewport");var hands=(FrameworkElement)window.FindName("PlayerHandsPanel");
        Rect handBounds=hands.TransformToAncestor(viewport).TransformBounds(new Rect(0,0,hands.ActualWidth,hands.ActualHeight));
        Require(handBounds.Top>=-1 && handBounds.Bottom<=viewport.ActualHeight+1 && handBounds.Height>=90,"Player cards clipped or reduced below readable size in AI table");
        Capture(window,"06-blackjack-dealt");
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
