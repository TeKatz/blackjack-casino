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
        ReelFrame.Background=ManorArtwork.Ink(spin.Free ? "#16313C" : "#17141D");
        MachineScene.Opacity=spin.Free ? 0.65 : 0.44;
        WinText.Text=spin.Free ? "HAUNTED NIGHT · 2× LINE WINS" : "The reels awaken…";
        ModeText.Text=spin.Free ? $"FREE SPINS LEFT: {State.FreeSpinsRemaining} · FEATURES DO NOT RETRIGGER" : "20 FIXED PAYLINES · FIVE REELS · ONE HAUNTED NIGHT";
        audio.EndRoom();
        audio.Play(ManorCue.Start);
        int doors=0;
        for(int i=0;i<5;i++) { reels[i].WinningRows.Clear(); reels[i].DimOthers=false; reels[i].InvalidateVisual(); }
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
                // Anticipation is based on two actually visible Doors, never an altered outcome.
                bool possible=Enumerable.Range(0,4).Sum(r=>spin.Cells.Skip(r*3).Take(3).Count(s=>s==ManorSymbol.Bonus))>=2;
                if(possible && i==4 && Speed==SlotSpinSpeed.Normal) duration+=350;
                reels[i].Effect=Speed==SlotSpinSpeed.Normal ? new BlurEffect { Radius=1.7,KernelType=KernelType.Gaussian } : null;
                motions.Add(Animate(reels[i],ManorReelView.PositionProperty,from,from+travel,duration,()=>
                {
                    reels[reel].Effect=null;
                    reels[reel].Position=target;
                    audio.Play((ManorCue)((int)ManorCue.Stop1+reel));
                    doors+=spin.Cells.Skip(reel*3).Take(3).Count(s=>s==ManorSymbol.Bonus);
                    if(doors==2 && reel<4) { audio.Mood(0,true); audio.Play(ManorCue.Heartbeat); }
                }));
            }
            try { await Task.WhenAll(motions); } finally { reelSound.Stop(); }
        }
        else for(int i=0;i<5;i++) { reels[i].Position=spin.Stops[i]; reels[i].Effect=null; }
        Token.ThrowIfCancellationRequested();
        audio.Mood(0);
        if(spin.Doors==2 && !spin.Free) audio.Play(ManorCue.Fakeout);
        if(spin.Cells.Contains(ManorSymbol.Wild)) audio.Play(ManorCue.Wild);
        if(spin.Scatters>=3 && !spin.Free) audio.Play(ManorCue.Scatter);
        if(spin.Doors>=3 && !spin.Free) audio.Play(ManorCue.Bonus);
        Highlight(spin);
        if(Animations && spin.Cells.Any(s=>s>=ManorSymbol.Wild))
            foreach(ManorReelView reel in reels)
                reel.BeginAnimation(ManorReelView.EmphasisProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(240)) {AutoReverse=true,FillBehavior=FillBehavior.Stop});
        string prefix=spin.Payout>=State.Bet*100 ? "SUPERNATURAL WIN" : spin.Payout>=State.Bet*25 ? "BIG WIN" : spin.Payout>0 ? "RETURNED" : "NO PAYLINE THIS TIME";
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
                var pulse=new DoubleAnimation(1,0.68,TimeSpan.FromMilliseconds(220)) { AutoReverse=true,RepeatBehavior=new RepeatBehavior(2),FillBehavior=FillBehavior.Stop };
                ReelFrame.BeginAnimation(OpacityProperty,pulse);
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
        foreach(ManorLineWin win in spin.Wins.Take(3))
        {
            var line=new Polyline { Stroke=ManorArtwork.Ink("#A6D7B985"),StrokeThickness=1.2,IsHitTestVisible=false };
            for(int col=0;col<5;col++) line.Points.Add(new Point(76+col*152,47+ManorRules.Paylines[win.Line][col]*94));
            PaylineCanvas.Children.Add(line);
        }
    }

    private async Task DoorTransition(bool automatic)
    {
        if(closed) return;
        DoorTitle.Text="ESCAPE THE MANOR"; DoorScene.Scene="Door";
        audio.Play(ManorCue.Trigger); DoorPanel.Visibility=Visibility.Visible;
        DoorNote.Text=automatic ? "Auto-resolved with the same map and ghost rules" : $"Exit target {State.Run!.ExitTarget}× · your choices change the risk";
        DoorScene.DoorOpen=0;
        if(Animations)
        {
            audio.Play(ManorCue.Unlock);
            await Wait(Timing(180,70,0));
            audio.Play(ManorCue.DoorOpen);
            await Animate(DoorScene,ManorBackdrop.DoorOpenProperty,0,1,Timing(750,300,120));
        }
        DoorPanel.Visibility=Visibility.Collapsed;
    }

    private void ShowRun()
    {
        ManorRunState run=State.Run!;
        BonusPanel.Visibility=Visibility.Visible;
        RoomScene.Scene=ManorMap.Rooms[run.Player].Area.ToString(); RoomScene.Threat=run.Haunt;
        RoomText.Text=$"{ManorMap.Rooms[run.Player].Name} · turn {run.Turn}/24 · Exit {run.ExitTarget}×";
        RewardText.Text=$"{run.Current}× / {run.Banked}×";
        HauntText.Text=$"{run.Haunt} / 8";
        GhostText.Text=new ManorRun(run).GhostClue;
        RoomNote.Text=run.Message+(run.HasKey ? "  KEY" : "")+(run.HasCharm ? "  CHARM" : "")+(run.HasCandle ? "  CANDLE" : "");
        LeaveButton.Content=$"LEAVE · TAKE {run.Current}×  [L]";
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
                ToolTip=(visited ? "Already visited" : $"+{destination.Reward}×")+(destination.Checkpoint ? " · Checkpoint" : "")+(destination.Pressure>0 ? " · Extra Haunt" : "") };
            button.Click+=Route_Click; RoutesPanel.Children.Add(button);
        }
        DrawMap(run); audio.Room(ManorMap.Rooms[run.Player].Area,run.Haunt);
        if(!hunting)
        {
            RoomNote.Text=run.Message+$"  Returned {Money(run.Payout*State.Bet)}. "+(State.InteractiveRunCounted ? "" : "Auto-resolved; same reference policy available interactively.");
            audio.Play(run.Status==ManorRunStatus.Escaped ? run.ExitTarget==1000 ? ManorCue.MaximumExit : ManorCue.Escape : run.Status==ManorRunStatus.Caught ? ManorCue.Catch : ManorCue.Checkpoint);
        }
        Refresh();
    }

    private void DrawMap(ManorRunState run)
    {
        MapCanvas.Children.Clear();
        static Point P(int id) { ManorRoom r=ManorMap.Rooms[id]; return new Point(35+r.X*100,30+(r.Y-1)*50); }
        foreach((int a,int b) in ManorMap.Edges.Where(e=>e.A<17 && e.B<17))
        {
            Point p=P(a),q=P(b);
            bool traced=run.Route.Zip(run.Route.Skip(1)).Any(e=>(e.First==a && e.Second==b)||(e.First==b && e.Second==a));
            MapCanvas.Children.Add(new Line { X1=p.X,Y1=p.Y,X2=q.X,Y2=q.Y,Stroke=ManorArtwork.Ink(traced ? "#C6AA77" : "#4D4058"),StrokeThickness=traced?3:1.5,
                StrokeDashArray=ManorMap.Locked(a,b) ? new DoubleCollection {3,2} : null });
        }
        foreach(ManorRoom room in ManorMap.Rooms.Take(17))
        {
            Point p=P(room.Id);
            bool visited=(run.Collected&(1UL<<room.Id))!=0;
            var node=new Ellipse { Width=room.Id==run.Player?19:12,Height=room.Id==run.Player?19:12,
                Fill=ManorArtwork.Ink(room.Id==run.Player ? "#E6D09B" : room.Id==16 ? "#8DB2B3" : visited ? "#95826A" : "#342C42"),
                Stroke=ManorArtwork.Ink("#AB8C68"),StrokeThickness=1,ToolTip=room.Name };
            Canvas.SetLeft(node,p.X-node.Width/2); Canvas.SetTop(node,p.Y-node.Height/2); MapCanvas.Children.Add(node);
            if(Animations && room.Id==run.Player && lastMapPlayer>=0 && lastMapPlayer!=run.Player)
            {
                Point old=P(lastMapPlayer); var movement=new TranslateTransform(); node.RenderTransform=movement;
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
            Point p=P(run.Ghost); var ghost=new TextBlock { Text="G",Foreground=ManorArtwork.Ink("#D48CA6"),FontWeight=FontWeights.Bold,FontSize=15 };
            Canvas.SetLeft(ghost,p.X-5); Canvas.SetTop(ghost,p.Y-11); MapCanvas.Children.Add(ghost);
        }
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
                DoorTitle.Text=State.Run.ExitTarget==1000 ? "1000× · THE MOONLIT GATE" : $"ESCAPED · {State.Run.ExitTarget}×";
                DoorNote.Text=$"{Money(State.Run.Payout*State.Bet)} secured. The Manor releases you.";
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
        audio.SoundEnabled=controller.Preferences.SoundEnabled; audio.MusicEnabled=controller.Preferences.MusicEnabled; audio.MusicVolume=controller.Preferences.MusicVolume;
        if(State.Run is {Status:ManorRunStatus.Hunting} r) audio.Room(ManorMap.Rooms[r.Player].Area,r.Haunt);
        else { audio.EndRoom(); audio.Mood(0); }
        if(Animations) atmosphere.Start(); else atmosphere.Stop();
    }
    private void ReelSoundTick(object? sender,EventArgs e)=>audio.Play(ManorCue.Reel);
    private void AtmosphereTick(object? sender,EventArgs e)
    {
        phase+=0.05; MachineScene.Phase=phase; RoomScene.Phase=phase; DoorScene.Phase=phase;
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
        foreach(ManorReelView reel in reels) reel.BeginAnimation(ManorReelView.PositionProperty,null);
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
