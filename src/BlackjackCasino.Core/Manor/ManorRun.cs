namespace BlackjackCasino.Core.Manor;

public enum ManorRunStatus { Hunting, Escaped, Caught, CashedOut, Exhausted }
public enum ManorArea { Entrance, Library, Ballroom, Corridor, Attic, Crypt }
public sealed record ManorRoom(int Id, string Name, int X, int Y, ManorArea Area, int Reward = 0,
    bool Checkpoint = false, bool Key = false, bool Candle = false, bool Charm = false, int Pressure = 0);

public sealed class ManorRunState
{
    public int Seed { get; set; }
    public int ExitTarget { get; set; } = 250;
    public int Player { get; set; }
    public int Ghost { get; set; } = 17;
    public int Turn { get; set; }
    public int ExtraHaunt { get; set; }
    public int Current { get; set; } = 5;
    public int Banked { get; set; } = 5;
    public bool HasKey { get; set; }
    public bool HasCandle { get; set; }
    public bool HasCharm { get; set; }
    public int RevealUntilTurn { get; set; } = -1;
    public int StunnedTurns { get; set; }
    public ulong Collected { get; set; } = 1;
    public List<int> Route { get; set; } = [0];
    public ManorRunStatus Status { get; set; }
    public string Message { get; set; } = "The entrance seals behind you. The Moonlit Gate lies to the east.";
    public int Haunt => Math.Min(8, Turn + ExtraHaunt);
    public int Payout => Status == ManorRunStatus.Hunting ? 0 : Status == ManorRunStatus.Escaped ? ExitTarget
        : Status is ManorRunStatus.Caught or ManorRunStatus.Exhausted ? Banked : Current;
}

public static class ManorMap
{
    public static IReadOnlyList<ManorRoom> Rooms { get; } = Array.AsReadOnly(new ManorRoom[]
    {
        new(0,"Entrance Hall",0,2,ManorArea.Entrance),
        new(1,"Servants' Corridor",1,2,ManorArea.Corridor,5,Key:true),
        new(2,"Candle Landing",2,2,ManorArea.Corridor,10,Checkpoint:true),
        new(3,"Grand Staircase",3,2,ManorArea.Entrance,10),
        new(4,"Library",1,1,ManorArea.Library,10,Checkpoint:true,Candle:true),
        new(5,"Reading Room",2,1,ManorArea.Library,10),
        new(6,"Study",3,1,ManorArea.Library,30,Checkpoint:true,Charm:true),
        new(7,"Ballroom",1,3,ManorArea.Ballroom,25,Pressure:1),
        new(8,"Music Room",2,3,ManorArea.Ballroom,20,Checkpoint:true),
        new(9,"Winter Garden",3,3,ManorArea.Ballroom,30),
        new(10,"Attic",4,2,ManorArea.Attic,40),
        new(11,"Forbidden Wing",4,1,ManorArea.Crypt,80,Pressure:2),
        new(12,"Crypt",4,3,ManorArea.Crypt,80,Pressure:2),
        new(13,"Moonlit Landing",5,2,ManorArea.Attic,40,Checkpoint:true),
        new(14,"Secret Passage",5,3,ManorArea.Crypt,30),
        new(15,"Courtyard",6,2,ManorArea.Entrance,50),
        new(16,"Moonlit Gate",7,2,ManorArea.Entrance),
        new(17,"Distant Vestibule",-2,2,ManorArea.Corridor),
        new(18,"Rear Foyer",-1,2,ManorArea.Corridor),
    });

    public static IReadOnlyList<(int A, int B)> Edges { get; } = Array.AsReadOnly(new[]
    {
        (17,18),(18,0),(0,1),(0,4),(0,7),(1,2),(1,4),(1,7),(4,5),(7,8),
        (2,3),(2,5),(2,8),(5,6),(8,9),(3,10),(6,11),(9,12),(10,13),(11,13),
        (12,14),(13,15),(14,15),(15,16),(14,16),
    });

    public static bool Locked(int a, int b) => (Math.Min(a,b), Math.Max(a,b)) is (6,11) or (14,16);
    public static int[] Adjacent(int room, bool key = true, bool player = false) => Edges
        .Where(e => e.A == room || e.B == room).Select(e => e.A == room ? e.B : e.A)
        .Where(n => (!player || n < 17) && (key || !Locked(room,n))).Order().ToArray();

    public static int[] Path(int from, int to, bool key = true, bool player = false)
    {
        var queue = new Queue<int[]>();
        var seen = new HashSet<int> { from };
        queue.Enqueue([from]);
        while (queue.TryDequeue(out int[]? path))
        {
            if (path[^1] == to) return path.Skip(1).ToArray();
            foreach (int next in Adjacent(path[^1],key,player))
                if (seen.Add(next)) queue.Enqueue([..path,next]);
        }
        return [];
    }
}

/// <summary>Choices advance real state; no preselected result is hidden behind the map.</summary>
public sealed class ManorRun
{
    public ManorRun(ManorRunState state) => State = state;
    public ManorRunState State { get; }
    public bool Finished => State.Status != ManorRunStatus.Hunting;
    public int Distance => ManorMap.Path(State.Ghost,State.Player).Length;
    private string GhostDirection
    {
        get
        {
            ManorRoom ghost=ManorMap.Rooms[State.Ghost],player=ManorMap.Rooms[State.Player];
            return ghost.X<player.X ? "west" : ghost.X>player.X ? "east" : ghost.Y<player.Y ? "north" : ghost.Y>player.Y ? "south" : "nearby";
        }
    }
    public string GhostClue => State.RevealUntilTurn >= State.Turn
        ? $"Candle reveals: {ManorMap.Rooms[State.Ghost].Name}."
        : Distance <= 2 ? $"Cold air creeps from the {GhostDirection}. The ghost is CLOSE (within two rooms)."
        : Distance <= 4 ? $"A door slams to the {GhostDirection}. The ghost is approaching."
        : "Distant footsteps echo through the Manor. The ghost is far away.";

    public static ManorRun Create(int doors, int seed)
    {
        if (doors is < 3 or > 5 || seed is < 0 or >= 16) throw new ArgumentOutOfRangeException(nameof(doors));
        return new(new ManorRunState { Seed=seed, Ghost=seed>=8 ? 18 : 17, ExitTarget=doors==3 ? 250 : doors==4 ? 500 : 1000 });
    }

    public void UseCandle()
    {
        RequireActive();
        if (!State.HasCandle) throw new InvalidOperationException("No candle remains.");
        State.HasCandle=false;
        State.RevealUntilTurn=State.Turn+1;
        State.Message="The candle reveals her position through the walls for one turn.";
    }

    public void Move(int room)
    {
        RequireActive();
        if (!ManorMap.Adjacent(State.Player,State.HasKey,player:true).Contains(room))
            throw new InvalidOperationException("That route is not adjacent or requires a key.");
        State.Player=room;
        State.Route.Add(room);
        State.Turn++;
        if (room==16)
        {
            State.Current=State.ExitTarget;
            State.Status=ManorRunStatus.Escaped;
            State.Message=$"You reached the Moonlit Gate. {State.ExitTarget}× secured.";
            return;
        }
        ManorRoom entered=ManorMap.Rooms[room];
        State.Message=$"You enter {entered.Name}.";
        if ((State.Collected & (1UL<<room))==0)
        {
            State.Collected |= 1UL<<room;
            State.Current=Math.Min(State.ExitTarget,State.Current+entered.Reward);
            State.ExtraHaunt+=entered.Pressure;
            State.HasKey |= entered.Key;
            State.HasCandle |= entered.Candle;
            State.HasCharm |= entered.Charm;
            if (entered.Checkpoint) State.Banked=State.Current;
            State.Message += $" +{entered.Reward}×." + (entered.Checkpoint ? " Checkpoint secured." : "")
                + (entered.Key ? " Key found." : "") + (entered.Candle ? " Candle found." : "") + (entered.Charm ? " Holy charm found." : "");
        }
        if (State.Ghost==State.Player) { Catch(); if (Finished) return; }
        if (State.StunnedTurns>0) State.StunnedTurns--;
        else
        {
            int steps=State.Turn<=2 ? State.Seed%2 : 1+((State.Turn+State.Seed)%4==0 || State.Haunt>=6 ? 1 : 0);
            for(int i=0;i<steps;i++)
            {
                int[] path=ManorMap.Path(State.Ghost,State.Player);
                if(path.Length>0) State.Ghost=path[0];
                if(State.Ghost==State.Player) { Catch(); break; }
            }
        }
        if (!Finished && State.Turn>=24)
        {
            State.Status=ManorRunStatus.Exhausted;
            State.Message="The night closes in. Your checkpoint is secured.";
        }
    }

    public void CashOut()
    {
        RequireActive();
        State.Status=ManorRunStatus.CashedOut;
        State.Message=$"You leave the Manor with {State.Current}×.";
    }

    public void FollowPolicy()
    {
        RequireActive();
        if (Distance<=2 && State.Current-State.Banked>=50) { CashOut(); return; }
        int[] path=ManorMap.Path(State.Player,16,State.HasKey,player:true);
        if(path.Length==0) { CashOut(); return; }
        Move(path[0]);
    }

    public void AutoResolve() { while(!Finished) FollowPolicy(); }

    private void Catch()
    {
        if(State.HasCharm)
        {
            State.HasCharm=false;
            State.Ghost=ManorMap.Adjacent(State.Player).OrderByDescending(n=>ManorMap.Path(n,16).Length).First();
            State.StunnedTurns=1;
            State.Message += " Your charm repels her one room and buys a turn.";
        }
        else
        {
            State.Status=ManorRunStatus.Caught;
            State.Message=$"The ghost catches you. Checkpoint pays {State.Banked}×.";
        }
    }
    private void RequireActive() { if(Finished) throw new InvalidOperationException("This Manor run has ended."); }
}
