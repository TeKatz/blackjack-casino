using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal partial class ManorWindow : Window
{
    private readonly ManorController controller;
    private readonly SlotPlayController play=new();
    private readonly ManorAudio audio=new();
    private readonly CancellationTokenSource lifetime=new();
    private readonly DispatcherTimer autoTimer=new() { Interval=TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer reelSound=new() { Interval=TimeSpan.FromMilliseconds(120) };
    private readonly DispatcherTimer atmosphere=new() { Interval=TimeSpan.FromMilliseconds(50) };
    private readonly List<ManorReelView> reels=[];
    private readonly List<UIElement> routePreviewElements=[];
    private bool initializing=true,operation,closed;
    private decimal bet;
    private double phase;
    private int lastMapPlayer=-1;
    private CancellationToken Token=>lifetime.Token;
    private ManorSessionState State=>controller.State;
    private SlotSpinSpeed Speed=>controller.Preferences.ManorSpeed;
    private bool Animations=>controller.Preferences.AnimationsEnabled;
    public AppPreferences Preferences=>controller.Preferences;

    public ManorWindow(ManorController controller)
    {
        InitializeComponent();
        this.controller=controller;
        MenuAudio.Attach(this, () => controller.Preferences.SoundEnabled);
        bet=controller.Preferences.ManorBet;
        AppPreferences p=controller.Preferences;
        SpeedBox.SelectedIndex=(int)p.ManorSpeed;
        AutoBox.SelectedIndex=Array.IndexOf(new[]{0,10,25,50,100,-1},p.ManorAuto.Count);
        InteractiveBox.IsChecked=p.ManorInteractive;
        StopBigBox.IsChecked=p.ManorAuto.StopOnBigWin;
        StopBonusBox.IsChecked=p.ManorAuto.StopOnBonus;
        FloorBox.Text=p.ManorAuto.MinimumBalance.ToString(CultureInfo.InvariantCulture);
        SoundBox.IsChecked=p.SoundEnabled; MusicBox.IsChecked=p.MusicEnabled; AnimationBox.IsChecked=p.AnimationsEnabled;
        for(int i=0;i<5;i++)
        {
            ReelsHost.ColumnDefinitions.Add(new ColumnDefinition());
            var reel=new ManorReelView { Reel=i,Margin=new Thickness(3,0,3,0),Position=State.LastSpin?.Stops[i] ?? i*5 };
            Grid.SetColumn(reel,i); ReelsHost.Children.Add(reel); reels.Add(reel);
        }
        foreach(ManorSymbolDefinition symbol in ManorRules.Symbols)
        {
            var row=new Grid { Margin=new Thickness(0,2,0,2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(29) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new Image { Source=ManorArtwork.Symbol(symbol.Symbol),Width=27,Height=27,ToolTip=symbol.Name });
            var text=new TextBlock { FontSize=11,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(5,0,0,0),
                Text=symbol.Three>0 ? $"{symbol.Name}  {symbol.Three}/{symbol.Four}/{symbol.Five}" : symbol.Name,
                Foreground=ManorArtwork.Ink(symbol.Symbol>=ManorSymbol.Wild ? "#D6BD8D" : "#C4B5C8") };
            Grid.SetColumn(text,1); row.Children.Add(text); PaytableRows.Children.Add(row);
        }
        autoTimer.Tick+=AutoTick;
        atmosphere.Tick+=AtmosphereTick;
        reelSound.Tick+=ReelSoundTick;
        MouseMove+=(_,e)=>
        {
            if(!Animations || BonusPanel.Visibility!=Visibility.Visible) return;
            Point point=e.GetPosition(this);
            RoomScene.RenderTransform=new TranslateTransform((point.X/ActualWidth-0.5)*5,(point.Y/ActualHeight-0.5)*3);
        };
        initializing=false;
        ApplyAudio(); Refresh();
    }

    private async void Window_Loaded(object sender,RoutedEventArgs e)
    {
        if(Animations) atmosphere.Start();
        if(State.CycleActive) await RunOperation(async()=>
        {
            _=play.TryBegin(false,State.Bankroll,0);
            StatusText.Text="Resuming your saved feature. No new wager has been charged.";
            await ContinueFeatures();
        });
    }

    private async Task RunOperation(Func<Task> action)
    {
        if(operation || closed) return;
        operation=true; Refresh();
        try { await action(); }
        catch(OperationCanceledException) { }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or OverflowException)
        {
            play.CancelAuto();
            play.Complete(State.Bankroll,false,false);
            StatusText.Text="Play paused: "+ex.Message+" Your last committed checkpoint is retained.";
        }
        finally { operation=false; if(!closed) { Refresh(); ScheduleAuto(); } }
    }

    private async Task StartPaid(bool automatic)
    {
        if((!automatic && play.AutoActive) || State.CycleActive || !play.TryBegin(automatic,State.Bankroll,bet)) return;
        try { controller.Execute(s=>s.Start(bet),controller.Preferences with { ManorBet=bet }); }
        catch { play.Complete(State.Bankroll,false,false); throw; }
        await PresentSpin(State.LastSpin!);
        await ContinueFeatures();
    }

    private async Task ContinueFeatures()
    {
        while(State.CycleActive && !closed)
        {
            if(State.Run is { Status:ManorRunStatus.Hunting })
            {
                if(controller.Preferences.ManorInteractive)
                {
                    controller.Execute(s=>s.MarkInteractive());
                    await DoorTransition(false);
                    ShowRun(); play.Pause(); return;
                }
                controller.Execute(s=>s.ResolveAutomatically()); // committed before summary/transition
                await DoorTransition(true);
                ShowRun();
                await Wait(Timing(1000,450,180));
                BonusPanel.Visibility=Visibility.Collapsed;
                play.Resume();
            }
            else if(State.FreeSpinsRemaining>0)
            {
                audio.Play(ManorCue.FreeStart);
                WinText.Text=$"HAUNTED NIGHT · {State.FreeSpinsRemaining} FREE SPINS";
                await Wait(Timing(600,240,0));
                while(State.FreeSpinsRemaining>0 && !closed)
                {
                    controller.Execute(s=>s.SpinFree());
                    await PresentSpin(State.LastSpin!);
                }
                audio.Play(ManorCue.FreeEnd);
                StatusText.Text=$"Haunted Night complete · cycle returned {Money(State.CycleReturned)}.";
            }
            else break;
        }
        if(!closed && !State.CycleActive)
        {
            audio.EndRoom();
            play.Resume();
            play.Complete(State.Bankroll,State.CycleReturned>=State.Bet*25,State.BonusTriggered);
            StatusText.Text=$"Cycle returned {Money(State.CycleReturned)} · +{(State.CycleReturned>State.Bet ? 25 : 20)} XP / +{(State.CycleReturned>State.Bet ? 20 : 15)} Season XP";
            Refresh();
        }
    }

    private async Task PresentSpin(ManorSpin spin)
    {
        Refresh();
        PaylineCanvas.Children.Clear();
        ReelEffects.BeginAnimation(ManorEffects.BurstProperty,null);
        ReelFrame.Background=ManorArtwork.Ink(spin.Free ? "#16313C" : "#17141D");
        MachineScene.Opacity=spin.Free ? 0.65 : 0.44;
        WinText.Text=spin.Free ? "HAUNTED NIGHT · 2× LINE WINS" : "The reels awaken…";
        ModeText.Text=spin.Free ? $"FREE SPINS LEFT: {State.FreeSpinsRemaining} · FEATURES DO NOT RETRIGGER" : "20 FIXED PAYLINES · FIVE REELS · ONE HAUNTED NIGHT";
        audio.EndRoom();
        audio.Play(ManorCue.Start);
        int doors=0;
        if(Animations) ReelEffects.Activity=1;
        for(int i=0;i<5;i++) { reels[i].BeginAnimation(ManorReelView.EmphasisProperty,null); reels[i].WinningRows.Clear(); reels[i].DimOthers=false; reels[i].InvalidateVisual(); }
        if(Animations)
        {
            reelSound.Start();
            var motions=new List<Task>();
            for(int i=0;i<5;i++)
            {
                int reel=i;
                int from=((int)Math.Round(reels[i].Position)%64+64)%64;
                int target=spin.Stops[i];
                int travel=(Speed==SlotSpinSpeed.Normal ? 64 : Speed==SlotSpinSpeed.Fast ? 32 : 0)+(target-from+64)%64;
                int duration=Speed==SlotSpinSpeed.Normal ? 1550+i*170 : Speed==SlotSpinSpeed.Fast ? 450+i*70 : 115+i*20;
                // Anticipation is based on an actually visible Door, never an altered outcome.
                bool possible=Enumerable.Range(0,4).Sum(r=>spin.Cells.Skip(r*3).Take(3).Count(s=>s==ManorSymbol.Bonus))>=1;
                if(possible && i==4 && Speed==SlotSpinSpeed.Normal) duration+=350;
                reels[i].Effect=Speed==SlotSpinSpeed.Normal ? new BlurEffect { Radius=1.7,KernelType=KernelType.Gaussian } : null;
                motions.Add(Animate(reels[i],ManorReelView.PositionProperty,from,from+travel,duration,()=>
                {
                    reels[reel].Effect=null;
                    reels[reel].Position=target;
                    audio.Play((ManorCue)((int)ManorCue.Stop1+reel));
                    reels[reel].BeginAnimation(ManorReelView.StopImpactProperty,new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(Timing(280,170,100))) { FillBehavior=FillBehavior.Stop });
                    doors+=spin.Cells.Skip(reel*3).Take(3).Count(s=>s==ManorSymbol.Bonus);
                    if(doors==1 && reel<4) { audio.Mood(0,true); audio.Play(ManorCue.Heartbeat); }
                }));
            }
            try { await Task.WhenAll(motions); } finally { reelSound.Stop(); }
        }
        else for(int i=0;i<5;i++) { reels[i].Position=spin.Stops[i]; reels[i].Effect=null; }
        Token.ThrowIfCancellationRequested();
        ReelEffects.Activity=0;
        audio.Mood(0);
        if(spin.Doors==1 && !spin.Free) audio.Play(ManorCue.Fakeout);
        if(spin.Cells.Contains(ManorSymbol.Wild)) audio.Play(ManorCue.Wild);
        if(spin.Scatters>=3 && !spin.Free) audio.Play(ManorCue.Scatter);
        if(spin.Doors>=2 && !spin.Free) audio.Play(ManorCue.Bonus);
        Highlight(spin);
        if(Animations && spin.Cells.Any(s=>s>=ManorSymbol.Wild))
            foreach(ManorReelView reel in reels)
                reel.BeginAnimation(ManorReelView.EmphasisProperty,new DoubleAnimation(0.15,1,TimeSpan.FromMilliseconds(650)) {AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,FillBehavior=FillBehavior.Stop});
        if(Animations && (spin.Payout>0 || (!spin.Free && (spin.Doors>=2 || spin.Scatters>=3))))
        {
            ReelEffects.Grand=spin.Payout>=State.Bet*25 || (!spin.Free && (spin.Doors>=2 || spin.Scatters>=3));
            ReelEffects.BeginAnimation(ManorEffects.BurstProperty,new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(Timing(ReelEffects.Grand?1600:850,500,220))) { FillBehavior=FillBehavior.Stop });
        }
        string prefix=!spin.Free && spin.Doors>=2 ? "THE MANOR OPENS" : !spin.Free && spin.Scatters>=3 ? "HAUNTED NIGHT AWARDED" : spin.Payout>=State.Bet*100 ? "SUPERNATURAL WIN" : spin.Payout>=State.Bet*25 ? "BIG WIN" : spin.Payout>0 ? "RETURNED" : "NO PAYLINE THIS TIME";
        if(spin.Payout>0)
        {
            audio.Play(spin.Payout>=State.Bet*25 ? ManorCue.BigWin : spin.Payout>=State.Bet*5 ? ManorCue.MediumWin : ManorCue.SmallWin);
            int duration=Timing(spin.Payout>=State.Bet*25 ? 700 : 260,120,0);
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(watch.ElapsedMilliseconds<duration)
            {
                WinText.Text=prefix+" · "+Money(spin.Payout*(decimal)watch.ElapsedMilliseconds/duration);
                await Wait(20);
            }
            if(spin.Payout>=State.Bet*25 && Animations)
            {
                WinText.RenderTransformOrigin=new Point(0.5,0.5);
                var scale=new ScaleTransform(1,1); WinText.RenderTransform=scale;
                var pulse=new DoubleAnimation(1,1.09,TimeSpan.FromMilliseconds(260)) { AutoReverse=true,RepeatBehavior=new RepeatBehavior(2),FillBehavior=FillBehavior.Stop };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty,pulse);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty,pulse);
            }
        }
        WinText.Text=prefix+(spin.Payout>0 ? " · "+Money(spin.Payout) : "");
        SpinNote.Text=spin.Free ? $"{State.FreeSpinsRemaining} Free Spins remain · 2× line awards" : $"{spin.Wins.Length} paying lines · {spin.Scatters} Moons · {spin.Doors} Doors";
        await Wait(Timing(220,80,0));
    }

    private void Highlight(ManorSpin spin)
    {
        var winning=new HashSet<int>();
        foreach(ManorLineWin win in spin.Wins)
        for(int col=0;col<win.Count;col++) winning.Add(col*3+ManorRules.Paylines[win.Line][col]);
        for(int i=0;i<5;i++)
        {
            reels[i].WinningRows.Clear();
            foreach(int cell in winning.Where(c=>c/3==i)) reels[i].WinningRows.Add(cell%3);
            reels[i].DimOthers=winning.Count>0; reels[i].InvalidateVisual();
        }
        PaylineCanvas.Children.Clear();
        // Show every paid line, only along the symbols that actually paid.
        // A dark outer stroke, bright gold core and moving moonlight trace keep it
        // readable on ivory reels without hiding the artwork under a solid band.
        foreach(ManorLineWin win in spin.Wins)
        {
            var points=new PointCollection(Enumerable.Range(0,win.Count).Select(col=>new Point(76+col*152,47+ManorRules.Paylines[win.Line][col]*94)));
            PaylineCanvas.Children.Add(new Polyline { Points=points,Stroke=ManorArtwork.Ink("#D0181829"),StrokeThickness=6,StrokeLineJoin=PenLineJoin.Round });
            var line=new Polyline { Points=points,Stroke=ManorArtwork.Ink("#FFF0A7"),StrokeThickness=2.8,StrokeLineJoin=PenLineJoin.Round,
                Effect=new DropShadowEffect {Color=Color.FromRgb(100,255,225),BlurRadius=8,ShadowDepth=0,Opacity=0.8} };
            PaylineCanvas.Children.Add(line);
            if(Animations)
            {
                var trace=new Polyline { Points=points,Stroke=Brushes.White,StrokeThickness=1.5,StrokeDashArray=new DoubleCollection {3,9},StrokeLineJoin=PenLineJoin.Round };
                trace.BeginAnimation(Shape.StrokeDashOffsetProperty,new DoubleAnimation(12,0,TimeSpan.FromMilliseconds(1000)) {RepeatBehavior=RepeatBehavior.Forever});
                PaylineCanvas.Children.Add(trace);
            }
        }
    }

    private async Task DoorTransition(bool automatic)
    {
        if(closed) return;
        DoorTitle.Text=State.Run!.PrizeScale<1 ? "A VISIT TO THE MANOR" : "ESCAPE THE MANOR"; DoorScene.Scene="Door";
        audio.Play(ManorCue.Trigger); DoorPanel.Visibility=Visibility.Visible;
        DoorNote.Text=automatic ? "Auto-resolved with the same map and ghost rules" : $"Exit target {State.Run!.Multiplier(State.Run.ExitTarget):0.##}× · your choices change the risk";
        DoorScene.DoorOpen=0;
        DoorPanel.Opacity=1;
        if(Animations)
        {
            // Normal speed deliberately breathes here: a major bonus should feel like
            // entering another space, not like a popup that flashes by between spins.
            DoorPanel.Opacity=0;
            await Animate(DoorPanel,OpacityProperty,0,1,Timing(460,160,60));
            await Wait(Timing(420,100,0));
            audio.Play(ManorCue.Unlock);
            await Wait(Timing(350,90,0));
            audio.Play(ManorCue.DoorOpen);
            await Animate(DoorScene,ManorBackdrop.DoorOpenProperty,0,1,Timing(1450,420,140));
            await Wait(Timing(420,120,0));
        }
        DoorPanel.BeginAnimation(OpacityProperty,null);
        DoorPanel.Opacity=1;
        DoorPanel.Visibility=Visibility.Collapsed;
    }

    private void ShowRun()
    {
        ManorRunState run=State.Run!;
        BonusPanel.Visibility=Visibility.Visible;
        RoomScene.Scene=ManorMap.Rooms[run.Player].Area.ToString(); RoomScene.Threat=run.Haunt;
        if(Animations) RoomScene.BeginAnimation(OpacityProperty,new DoubleAnimation(0.5,1,TimeSpan.FromMilliseconds(Timing(650,180,80))) { FillBehavior=FillBehavior.Stop });
        RoomText.Text=$"{ManorMap.Rooms[run.Player].Name} · turn {run.Turn}/24 · Exit {run.Multiplier(run.ExitTarget):0.##}×";
        RewardText.Text=$"{run.Multiplier(run.Current):0.##}× / {run.Multiplier(run.Banked):0.##}×";
        HauntText.Text=$"{run.Haunt} / 8";
        GhostText.Text=new ManorRun(run).GhostClue;
        RoomNote.Text=run.Message+(run.HasKey ? "  KEY" : "")+(run.HasCharm ? "  CHARM" : "")+(run.HasCandle ? "  CANDLE" : "");
        LeaveButton.Content=$"LEAVE · TAKE {run.Multiplier(run.Current):0.##}×  [L]";
        bool hunting=run.Status==ManorRunStatus.Hunting;
        LeaveButton.IsEnabled=hunting; RecommendButton.IsEnabled=hunting; CandleButton.IsEnabled=hunting && run.HasCandle;
        RoutesPanel.Children.Clear();
        int index=1;
        foreach(int room in ManorMap.Adjacent(run.Player,key:true,player:true))
        {
            ManorRoom destination=ManorMap.Rooms[room];
            bool unlocked=!ManorMap.Locked(run.Player,room) || run.HasKey;
            bool visited=(run.Collected&(1UL<<room))!=0;
            var button=new Button { Content=$"[{index++}] {destination.Name}"+(unlocked ? "" : " · KEY"),Tag=room,
                IsEnabled=hunting && unlocked,Style=(Style)FindResource("ManorButton"),FontSize=11,Padding=new Thickness(8,8,8,8),
                ToolTip=(visited ? "Already visited" : $"+{run.Multiplier(destination.Reward):0.##}×")+(destination.Checkpoint ? " · Checkpoint" : "")+(destination.Pressure>0 ? " · Extra Haunt" : "") };
            button.Click+=Route_Click;
            button.MouseEnter+=(_,_)=>ShowRoutePreview(room);
            button.MouseLeave+=(_,_)=>ClearRoutePreview();
            button.GotKeyboardFocus+=(_,_)=>ShowRoutePreview(room);
            button.LostKeyboardFocus+=(_,_)=>ClearRoutePreview();
            RoutesPanel.Children.Add(button);
        }
        DrawMap(run); audio.Room(ManorMap.Rooms[run.Player].Area,run.Haunt);
        if(!hunting)
        {
            RoomNote.Text=run.Message+$"  Returned {Money(run.PaidMultiplier*State.Bet)}. "+(State.InteractiveRunCounted ? "" : "Auto-resolved; same reference policy available interactively.");
            audio.Play(run.Status==ManorRunStatus.Escaped ? run.ExitTarget==1000 ? ManorCue.MaximumExit : ManorCue.Escape : run.Status==ManorRunStatus.Caught ? ManorCue.Catch : ManorCue.Checkpoint);
        }
        Refresh();
    }

    private static Point MapPoint(int id)
    {
        ManorRoom room=ManorMap.Rooms[id];
        return new Point(35+room.X*100,30+(room.Y-1)*50);
    }

    private void DrawMap(ManorRunState run)
    {
        MapCanvas.Children.Clear();
        routePreviewElements.Clear();
        foreach((int a,int b) in ManorMap.Edges.Where(e=>e.A<17 && e.B<17))
        {
            Point p=MapPoint(a),q=MapPoint(b);
            bool traced=run.Route.Zip(run.Route.Skip(1)).Any(e=>(e.First==a && e.Second==b)||(e.First==b && e.Second==a));
            MapCanvas.Children.Add(new Line { X1=p.X,Y1=p.Y,X2=q.X,Y2=q.Y,Stroke=ManorArtwork.Ink(traced ? "#C6AA77" : "#4D4058"),StrokeThickness=traced?3:1.5,
                StrokeDashArray=ManorMap.Locked(a,b) ? new DoubleCollection {3,2} : null });
        }
        foreach(ManorRoom room in ManorMap.Rooms.Take(17))
        {
            Point p=MapPoint(room.Id);
            bool visited=(run.Collected&(1UL<<room.Id))!=0;
            var node=new Ellipse { Width=room.Id==run.Player?19:12,Height=room.Id==run.Player?19:12,
                Fill=ManorArtwork.Ink(room.Id==run.Player ? "#E6D09B" : room.Id==16 ? "#8DB2B3" : visited ? "#95826A" : "#342C42"),
                Stroke=ManorArtwork.Ink("#AB8C68"),StrokeThickness=1,ToolTip=room.Name };
            Canvas.SetLeft(node,p.X-node.Width/2); Canvas.SetTop(node,p.Y-node.Height/2); MapCanvas.Children.Add(node);
            if(Animations && room.Id==run.Player && lastMapPlayer>=0 && lastMapPlayer!=run.Player)
            {
                Point old=MapPoint(lastMapPlayer); var movement=new TranslateTransform(); node.RenderTransform=movement;
                movement.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(old.X-p.X,0,TimeSpan.FromMilliseconds(180)));
                movement.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(old.Y-p.Y,0,TimeSpan.FromMilliseconds(180)));
            }
            if(room.Id==run.Player || room.Id==16)
            {
                var label=new TextBlock { Text=room.Id==16?"GATE":"YOU",Foreground=ManorArtwork.Ink("#E3D3AF"),FontSize=9 };
                Canvas.SetLeft(label,p.X-12); Canvas.SetTop(label,p.Y+11); MapCanvas.Children.Add(label);
            }
        }
        lastMapPlayer=run.Player;
        if(run.RevealUntilTurn>=run.Turn && run.Ghost<17)
        {
            Point p=MapPoint(run.Ghost); var ghost=new TextBlock { Text="G",Foreground=ManorArtwork.Ink("#D48CA6"),FontWeight=FontWeights.Bold,FontSize=15 };
            Canvas.SetLeft(ghost,p.X-5); Canvas.SetTop(ghost,p.Y-11); MapCanvas.Children.Add(ghost);
        }
    }

    private void ShowRoutePreview(int room)
    {
        if(State.Run is not {Status:ManorRunStatus.Hunting} run || room<0 || room>=17) return;
        ClearRoutePreview();
        Point start=MapPoint(run.Player),end=MapPoint(room);
        var glow=new Line
        {
            X1=start.X,Y1=start.Y,X2=end.X,Y2=end.Y,
            Stroke=ManorArtwork.Ink("#E8CF99"),StrokeThickness=10,Opacity=0.2,IsHitTestVisible=false,
            Effect=new DropShadowEffect {Color=Color.FromRgb(244,211,145),BlurRadius=18,ShadowDepth=0,Opacity=0.9}
        };
        var guide=new Line
        {
            X1=start.X,Y1=start.Y,X2=end.X,Y2=end.Y,
            Stroke=ManorArtwork.Ink("#FFE6AA"),StrokeThickness=3,Opacity=0.95,IsHitTestVisible=false,
            StrokeDashArray=new DoubleCollection {4,2},
            Effect=new DropShadowEffect {Color=Color.FromRgb(255,226,166),BlurRadius=10,ShadowDepth=0,Opacity=0.9}
        };
        var halo=new Ellipse
        {
            Width=28,Height=28,Fill=ManorArtwork.Ink("#3017121D"),Stroke=ManorArtwork.Ink("#FFE6AA"),StrokeThickness=2,
            IsHitTestVisible=false,Effect=new DropShadowEffect {Color=Color.FromRgb(255,218,150),BlurRadius=14,ShadowDepth=0,Opacity=0.9}
        };
        Canvas.SetLeft(halo,end.X-14); Canvas.SetTop(halo,end.Y-14);
        var name=new Border
        {
            Background=ManorArtwork.Ink("#EE17131D"),BorderBrush=ManorArtwork.Ink("#B99C6C"),BorderThickness=new Thickness(1),
            CornerRadius=new CornerRadius(4),Padding=new Thickness(6,3,6,3),IsHitTestVisible=false,
            Child=new TextBlock {Text=ManorMap.Rooms[room].Name,Foreground=ManorArtwork.Ink("#F2DFB2"),FontSize=10,FontWeight=FontWeights.SemiBold}
        };
        Canvas.SetLeft(name,Math.Clamp(end.X+12,4,650));
        Canvas.SetTop(name,Math.Clamp(end.Y-31,3,137));
        routePreviewElements.Add(glow); routePreviewElements.Add(guide); routePreviewElements.Add(halo); routePreviewElements.Add(name);
        foreach(UIElement element in routePreviewElements) MapCanvas.Children.Add(element);
        if(Animations)
        {
            guide.BeginAnimation(Shape.StrokeDashOffsetProperty,new DoubleAnimation(12,0,TimeSpan.FromMilliseconds(850)) {RepeatBehavior=RepeatBehavior.Forever});
            var pulse=new DoubleAnimation(0.55,1,TimeSpan.FromMilliseconds(430)) {AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever};
            halo.BeginAnimation(UIElement.OpacityProperty,pulse);
            glow.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0.12,0.32,TimeSpan.FromMilliseconds(430)) {AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});
        }
    }

    private void ClearRoutePreview()
    {
        foreach(UIElement element in routePreviewElements.ToArray()) MapCanvas.Children.Remove(element);
        routePreviewElements.Clear();
    }

    private async Task RunChoice(Action<ManorSession> choice)
    {
        if(State.Run is not {Status:ManorRunStatus.Hunting}) return;
        int oldBank=State.Run.Banked,oldReward=State.Run.Current;
        controller.Execute(choice);
        ShowRun();
        if(State.Run!.Banked>oldBank) audio.Play(ManorCue.Checkpoint);
        else if(State.Run.Current>oldReward) audio.Play(ManorCue.Item);
        else audio.Play(new ManorRun(State.Run).Distance<=2 ? ManorCue.GhostNear : ManorCue.GhostFar);
        if(State.Run.Status!=ManorRunStatus.Hunting)
        {
            if(State.Run.Status==ManorRunStatus.Escaped)
            {
                DoorScene.Scene="Exterior"; DoorPanel.Visibility=Visibility.Visible;
                DoorTitle.Text=State.Run.ExitTarget==1000 ? "1000× · THE MOONLIT GATE" : $"ESCAPED · {State.Run.Multiplier(State.Run.ExitTarget):0.##}×";
                DoorNote.Text=$"{Money(State.Run.PaidMultiplier*State.Bet)} secured. The Manor releases you.";
            }
            await Wait(Timing(State.Run.Status==ManorRunStatus.Escaped?1900:1100,500,200));
            DoorPanel.Visibility=Visibility.Collapsed;
            BonusPanel.Visibility=Visibility.Collapsed; play.Resume();
            await ContinueFeatures();
        }
    }

    private async void Route_Click(object sender,RoutedEventArgs e) { if(sender is Button {Tag:int room}) await RunOperation(()=>RunChoice(s=>s.Move(room))); }
    private async void Recommend_Click(object sender,RoutedEventArgs e)=>await RunOperation(()=>RunChoice(s=>s.FollowPolicy()));
    private async void Leave_Click(object sender,RoutedEventArgs e)=>await RunOperation(()=>RunChoice(s=>s.CashOut()));
    private async void Candle_Click(object sender,RoutedEventArgs e)=>await RunOperation(()=>RunChoice(s=>s.UseCandle()));
    private async void Spin_Click(object sender,RoutedEventArgs e)=>await RunOperation(()=>State.CycleActive ? Resume() : StartPaid(false));
    private async Task Resume() { if(!play.Busy) _=play.TryBegin(false,State.Bankroll,0); await ContinueFeatures(); }

    private void Auto_Click(object sender,RoutedEventArgs e)
    {
        if(operation || closed) return;
        try { SaveSettings(); play.StartAuto(controller.Preferences.ManorAuto); ScheduleAuto(); Refresh(); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        { StatusText.Text="Auto was not started: "+ex.Message; }
    }
    private void Stop_Click(object sender,RoutedEventArgs e) { autoTimer.Stop(); play.CancelAuto(); StatusText.Text="Auto stopped. Any committed feature remains yours."; Refresh(); }
    private async void AutoTick(object? sender,EventArgs e)
    {
        autoTimer.Stop();
        if(play.CanAutoStart && !operation && !State.CycleActive) await RunOperation(()=>StartPaid(true));
    }
    private void ScheduleAuto() { if(!closed && play.CanAutoStart && !State.CycleActive && !operation) { autoTimer.Interval=TimeSpan.FromMilliseconds(Animations ? Speed==SlotSpinSpeed.Normal?350:Speed==SlotSpinSpeed.Fast?180:100 : 150); autoTimer.Start(); } }

    private void Refresh()
    {
        if(initializing || closed) return;
        BalanceText.Text=Money(State.Bankroll);
        BetText.Text=Money(State.CycleActive ? State.Bet : bet);
        BetControls.IsEnabled=!operation && !State.CycleActive && !play.AutoActive;
        SpinButton.IsEnabled=!operation && !play.AutoActive && (State.CycleActive || State.Bankroll>=bet);
        SpinButton.Content=State.CycleActive ? "RESUME FEATURE" : "SPIN  [SPACE]";
        AutoButton.IsEnabled=!operation && controller.Preferences.ManorAuto.Count!=0 && !play.AutoActive;
        StopButton.IsEnabled=play.AutoActive;
        SpeedBox.IsEnabled=!operation;
        AutoText.Text=play.AutoActive ? $"AUTO {(play.Remaining<0?"∞":play.Remaining)}"+(play.Paused ? " · MANOR PAUSED" : "") : "AUTO OFF";
        PlayerProfileState p=controller.Profile;
        ProfileText.Text=$"P{p.Prestige} · LEVEL {p.Level} · {p.Coins:N0} COSMETIC COINS · {p.LifetimeExperience:N0} LIFETIME XP";
        ManorStatistics s=State.Statistics;
        StatsText.Text=$"Paid spins: {s.PaidSpins:N0}\nFree spins: {s.FreeSpins:N0}\nWagered: {Money(s.TotalWagered)}\nReturned: {Money(s.TotalReturned)}\nNet: {Money(s.NetProfit)}\nBiggest return: {Money(s.BiggestWin)}\nBig Wins: {s.BigWins}\nFree sessions: {s.FreeSpinsTriggered}\nManor Bonuses: {s.ManorBonuses}\nInteractive / Auto: {s.InteractiveRuns} / {s.AutomaticRuns}\nExits / 1000×: {s.Exits} / {s.MaximumExits}\nCaught / Cash-outs: {s.Catches} / {s.CashOuts}\nBest Manor: {s.BestManorMultiplier}×";
        if(controller.SaveNotice is not null) StatusText.Text=controller.SaveNotice;
        foreach(Button button in BetControls.Children.OfType<Button>()) button.IsEnabled=decimal.Parse((string)button.Tag,CultureInfo.InvariantCulture)<=State.Bankroll;
    }

    private void SaveSettings()
    {
        if(!decimal.TryParse(FloorBox.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out decimal floor) || floor<0)
            throw new ArgumentException("Enter a nonnegative dollar amount for the Auto bankroll floor.");
        var auto=new AutoSpinSettings { Count=new[]{0,10,25,50,100,-1}[Math.Clamp(AutoBox.SelectedIndex,0,5)],
            StopOnBigWin=StopBigBox.IsChecked==true,StopOnBonus=StopBonusBox.IsChecked==true,MinimumBalance=floor };
        AppPreferences prefs=controller.Preferences with { ManorBet=bet,ManorSpeed=(SlotSpinSpeed)Math.Clamp(SpeedBox.SelectedIndex,0,2),
            ManorAuto=auto.Sanitize(),ManorInteractive=InteractiveBox.IsChecked==true,SoundEnabled=SoundBox.IsChecked==true,
            MusicEnabled=MusicBox.IsChecked==true,AnimationsEnabled=AnimationBox.IsChecked==true };
        controller.Execute(_=>{},prefs);
        if(play.AutoActive)
        {
            play.UpdateSettings(prefs.ManorAuto);
            if(!play.AutoActive) autoTimer.Stop();
        }
        ApplyAudio(); Refresh();
    }
    private async void Settings_Changed(object sender,RoutedEventArgs e)
    {
        if(initializing || closed) return;
        try
        {
            SaveSettings();
            if(!operation && BonusPanel.Visibility==Visibility.Visible && !controller.Preferences.ManorInteractive && State.CycleActive)
            { play.Resume(); await RunOperation(ContinueFeatures); }
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        { play.CancelAuto(); StatusText.Text="Settings were not changed: "+ex.Message; }
    }
    private void ApplyAudio()
    {
        MenuAudio.Enabled=controller.Preferences.SoundEnabled;
        audio.SoundEnabled=controller.Preferences.SoundEnabled; audio.MusicEnabled=controller.Preferences.MusicEnabled; audio.MusicVolume=controller.Preferences.MusicVolume;
        if(State.Run is {Status:ManorRunStatus.Hunting} r) audio.Room(ManorMap.Rooms[r.Player].Area,r.Haunt);
        else { audio.EndRoom(); audio.Mood(0); }
        if(Animations) atmosphere.Start();
        else
        {
            atmosphere.Stop(); ReelEffects.Activity=0; ReelEffects.BeginAnimation(ManorEffects.BurstProperty,null);
            foreach(ManorReelView reel in reels) { reel.BeginAnimation(ManorReelView.EmphasisProperty,null); reel.Pulse=0; }
            foreach(Polyline trace in PaylineCanvas.Children.OfType<Polyline>()) trace.BeginAnimation(Shape.StrokeDashOffsetProperty,null);
        }
    }
    private void ReelSoundTick(object? sender,EventArgs e)=>audio.Play(ManorCue.Reel);
    private void AtmosphereTick(object? sender,EventArgs e)
    {
        phase+=0.05; MachineScene.Phase=phase; RoomScene.Phase=phase; DoorScene.Phase=phase;
        ReelEffects.Phase=phase;
        foreach(ManorReelView reel in reels) reel.Pulse=(Math.Sin(phase*4)+1)/2;
    }
    private void Bet_Click(object sender,RoutedEventArgs e)
    {
        if(operation || State.CycleActive || play.AutoActive || sender is not Button {Tag:string value}) return;
        decimal before=bet; bet=decimal.Parse(value,CultureInfo.InvariantCulture);
        try { SaveSettings(); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        { bet=before; StatusText.Text=ex.Message; Refresh(); }
    }
    private void Tab_Click(object sender,RoutedEventArgs e)
    {
        string tab=(string)((Button)sender).Tag;
        PaytablePanel.Visibility=tab=="Paytable"?Visibility.Visible:Visibility.Collapsed;
        SettingsPanel.Visibility=tab=="Settings"?Visibility.Visible:Visibility.Collapsed;
        StatsPanel.Visibility=tab=="Stats"?Visibility.Visible:Visibility.Collapsed;
    }
    private async void Window_KeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape) { Close(); e.Handled=true; return; }
        if(e.IsRepeat) { e.Handled=true; return; }
        if(Keyboard.FocusedElement is TextBox or ComboBox) return;
        if(e.Key==Key.X) { Stop_Click(sender,e); e.Handled=true; return; }
        if(BonusPanel.Visibility==Visibility.Visible && State.Run is {Status:ManorRunStatus.Hunting})
        {
            Action<ManorSession>? choice=e.Key switch { Key.R=>s=>s.FollowPolicy(),Key.L=>s=>s.CashOut(),Key.C when State.Run.HasCandle=>s=>s.UseCandle(),_=>null };
            int number=e.Key>=Key.D1 && e.Key<=Key.D5 ? e.Key-Key.D1 : e.Key>=Key.NumPad1 && e.Key<=Key.NumPad5 ? e.Key-Key.NumPad1 : -1;
            if(number>=0 && RoutesPanel.Children.OfType<Button>().ElementAtOrDefault(number) is {IsEnabled:true,Tag:int room}) choice=s=>s.Move(room);
            if(choice is not null) { e.Handled=true; await RunOperation(()=>RunChoice(choice)); }
        }
        else if(e.Key==Key.Space && Keyboard.FocusedElement is not Button and not CheckBox)
        { e.Handled=true; await RunOperation(()=>State.CycleActive?Resume():StartPaid(false)); }
    }
    private void Back_Click(object sender,RoutedEventArgs e)=>Close();
    private void Window_Closing(object? sender,CancelEventArgs e) { play.CancelAuto(); autoTimer.Stop(); }
    private void Window_Closed(object? sender,EventArgs e)
    {
        closed=true; lifetime.Cancel(); autoTimer.Stop(); atmosphere.Stop(); reelSound.Stop();
        autoTimer.Tick-=AutoTick; atmosphere.Tick-=AtmosphereTick; reelSound.Tick-=ReelSoundTick;
        foreach(ManorReelView reel in reels)
        {
            reel.BeginAnimation(ManorReelView.PositionProperty,null);
            reel.BeginAnimation(ManorReelView.EmphasisProperty,null);
            reel.BeginAnimation(ManorReelView.StopImpactProperty,null);
        }
        foreach(Polyline trace in PaylineCanvas.Children.OfType<Polyline>()) trace.BeginAnimation(Shape.StrokeDashOffsetProperty,null);
        ReelEffects.BeginAnimation(ManorEffects.BurstProperty,null);
        DoorScene.BeginAnimation(ManorBackdrop.DoorOpenProperty,null); audio.Dispose(); lifetime.Dispose();
    }
    private int Timing(int normal,int fast,int turbo)=>!Animations?0:Speed==SlotSpinSpeed.Normal?normal:Speed==SlotSpinSpeed.Fast?fast:turbo;
    private Task Wait(int milliseconds)=>milliseconds<=0 ? Task.CompletedTask : Task.Delay(milliseconds,Token);
    private async Task Animate(FrameworkElement target,DependencyProperty property,double from,double to,int milliseconds,Action? completed=null)
    {
        if(milliseconds<=0) { target.SetValue(property,to); completed?.Invoke(); return; }
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration=Token.Register(()=>done.TrySetCanceled());
        var animation=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(milliseconds)) { EasingFunction=new CubicEase {EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.HoldEnd };
        animation.Completed+=(_,_)=> { if(closed) { done.TrySetCanceled(); return; } target.BeginAnimation(property,null); target.SetValue(property,to); completed?.Invoke(); done.TrySetResult(); };
        target.BeginAnimation(property,animation); await done.Task;
    }
    private static string Money(decimal value)=>value.ToString("C2",CultureInfo.GetCultureInfo("en-US"));
}
