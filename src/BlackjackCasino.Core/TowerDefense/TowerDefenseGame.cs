namespace BlackjackCasino.Core.TowerDefense;

/// <summary>Offline, wager-free tower defence rules. Rendering never determines hits or rewards.</summary>
public sealed record TdPoint(double X, double Y);
public sealed record TdTower(int Pad, string Kind, int Level, double Cooldown);
public sealed record TdEnemy(int Id, string Kind, double Distance, double Hp, double Slow);
public sealed record TdEffect(double FromX, double FromY, double ToX, double ToY, string Kind, int Ticks);

/// <summary>Account-wide Bastion mastery, independent of the casino player level.</summary>
public sealed record TdProgress
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public Dictionary<string, int> Skills { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CompletedStages { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> BestWaves { get; set; } = new(StringComparer.Ordinal);
    public long EnemiesDefeated { get; set; }
    public long EnemiesEscaped { get; set; }
    public long WavesCleared { get; set; }
    public long Victories { get; set; }
    public long Defeats { get; set; }
    public long RunsStarted { get; set; }
    public int PointsAvailable => Math.Max(0, Level - 1 - Skills.Values.Sum());
    public int Rank(string skill) => Skills.GetValueOrDefault(skill);
    public TdProgress Copy() => new()
    {
        Level = Level, Experience = Experience,
        Skills = new(Skills, StringComparer.Ordinal),
        CompletedStages = new(CompletedStages, StringComparer.Ordinal),
        BestWaves = new(BestWaves, StringComparer.Ordinal),
        EnemiesDefeated = EnemiesDefeated, EnemiesEscaped = EnemiesEscaped,
        WavesCleared = WavesCleared, Victories = Victories, Defeats = Defeats,
        RunsStarted = RunsStarted,
    };
}
public static class TdMastery
{
    public static readonly IReadOnlyDictionary<string, int> SkillCaps = new Dictionary<string, int>
    {
        ["capital"] = 5, ["refund"] = 5, ["fortify"] = 5,
        ["engineer"] = 5, ["scouting"] = 1, ["focus"] = 5,
        ["last-stand"] = 1,
    };
    public static int NextLevelCost(int level) => 110 + level * 70;
    public static int RequiredLevel(string kind) => kind switch
    {
        "ballista" => 2, "flame" => 4, _ => 1,
    };
    public static int StageWaves(string stage) => stage == "frost" ? 20 : 15;
    public static int StageLevel(string stage) => stage == "frost" ? 3 : 1;
}

/// <summary>Battle-only checkpoint; one preserved run per map.</summary>
public sealed record TdStageRun
{
    public int Wave { get; set; }
    public int Coins { get; set; } = 320;
    public int Lives { get; set; } = 20;
    public string Phase { get; set; } = "planning";
    public bool Paused { get; set; }
    public bool Endless { get; set; }
    public bool LastStandUsed { get; set; }
    public int Speed { get; set; } = 1;
    public int NextEnemyId { get; set; } = 1;
    public int Spawned { get; set; }
    public int SpawnTotal { get; set; }
    public int WaveKills { get; set; }
    public int WaveEscapes { get; set; }
    public double SpawnClock { get; set; }
    public long Ticks { get; set; }
    public List<TdTower> Towers { get; set; } = [];
    public List<TdEnemy> Enemies { get; set; } = [];
    public TdStageRun Copy() => this with { Towers = Towers.ToList(), Enemies = Enemies.ToList() };
    public static TdStageRun Capture(TdState s) => new()
    {
        Wave = s.Wave, Coins = s.Coins, Lives = s.Lives, Phase = s.Phase,
        Paused = s.Paused, Endless = s.Endless, LastStandUsed = s.LastStandUsed, Speed = s.Speed,
        NextEnemyId = s.NextEnemyId, Spawned = s.Spawned, SpawnTotal = s.SpawnTotal,
        WaveKills = s.WaveKills, WaveEscapes = s.WaveEscapes, SpawnClock = s.SpawnClock,
        Ticks = s.Ticks, Towers = s.Towers.ToList(), Enemies = s.Enemies.ToList(),
    };
    public void Restore(TdState s)
    {
        s.Wave = Wave; s.Coins = Coins; s.Lives = Lives; s.Phase = Phase;
        s.Paused = Paused || Phase == "running"; s.Endless = Endless; s.LastStandUsed = LastStandUsed; s.Speed = Speed;
        s.NextEnemyId = NextEnemyId; s.Spawned = Spawned; s.SpawnTotal = SpawnTotal;
        s.WaveKills = WaveKills; s.WaveEscapes = WaveEscapes;
        s.SpawnClock = Math.Max(0, SpawnClock); s.Ticks = Ticks;
        s.Towers = Towers.ToList(); s.Enemies = Enemies.ToList(); s.Effects.Clear();
    }
}
public sealed record TdState
{
    public int Version { get; set; } = 1;
    public string Screen { get; set; } = "menu";
    public string Stage { get; set; } = "forest";
    public bool Endless { get; set; }
    public bool LastStandUsed { get; set; }
    public TdProgress Progress { get; set; } = new();
    public Dictionary<string, TdStageRun> StageRuns { get; set; } = new(StringComparer.Ordinal);
    public int Wave { get; set; }
    public int Coins { get; set; } = 320;
    public int Lives { get; set; } = 20;
    public string Phase { get; set; } = "planning";
    public bool Paused { get; set; }
    public int Speed { get; set; } = 1;
    public int NextEnemyId { get; set; } = 1;
    public int Spawned { get; set; }
    public int SpawnTotal { get; set; }
    public int WaveKills { get; set; }
    public int WaveEscapes { get; set; }
    public double SpawnClock { get; set; }
    public long Ticks { get; set; }
    public List<TdTower> Towers { get; set; } = [];
    public List<TdEnemy> Enemies { get; set; } = [];
    public List<TdEffect> Effects { get; set; } = [];
    public TdState Copy() => new()
    {
        Version = Version, Screen = Screen, Stage = Stage, Endless = Endless, LastStandUsed = LastStandUsed,
        Progress = Progress.Copy(), StageRuns = StageRuns.ToDictionary(kv => kv.Key, kv => kv.Value.Copy(), StringComparer.Ordinal),
        Wave = Wave, Coins = Coins, Lives = Lives, Phase = Phase,
        Paused = Paused, Speed = Speed, NextEnemyId = NextEnemyId, Spawned = Spawned,
        SpawnTotal = SpawnTotal, WaveKills = WaveKills, WaveEscapes = WaveEscapes,
        SpawnClock = SpawnClock, Ticks = Ticks,
        Towers = Towers.ToList(), Enemies = Enemies.ToList(), Effects = Effects.ToList(),
    };
}

public static class TdMap
{
    public static IReadOnlyList<TdPoint> Path { get; } =
    [
        new(0, 300), new(145, 300), new(145, 130), new(340, 130),
        new(340, 450), new(590, 450), new(590, 210), new(810, 210),
        new(810, 325), new(960, 325),
    ];
    public static IReadOnlyList<TdPoint> Pads { get; } =
    [
        new(80, 190), new(242, 242), new(245, 365), new(255, 60),
        new(440, 245), new(465, 535), new(690, 345), new(700, 130),
        new(880, 125), new(886, 425),
    ];
    public static IReadOnlyList<TdPoint> FrostPath { get; } =
    [
        new(0, 280), new(180, 280), new(180, 470), new(370, 470),
        new(370, 125), new(535, 125), new(535, 365), new(740, 365),
        new(740, 225), new(960, 225),
    ];
    public static IReadOnlyList<TdPoint> FrostPads { get; } =
    [
        new(78, 172), new(85, 395), new(270, 370), new(280, 190),
        new(445, 245), new(455, 530), new(632, 245), new(633, 460),
        new(850, 125), new(840, 365),
    ];
    public static IReadOnlyList<TdPoint> PathFor(string stage) => stage == "frost" ? FrostPath : Path;
    public static IReadOnlyList<TdPoint> PadsFor(string stage) => stage == "frost" ? FrostPads : Pads;
    public static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
    public static double PathLength { get; } = Length(Path);
    public static double Length(IReadOnlyList<TdPoint> path) => Enumerable.Range(1, path.Count - 1)
        .Sum(i => Distance(path[i].X - path[i - 1].X, path[i].Y - path[i - 1].Y));
    public static TdPoint Position(double distance) => Position(Path, distance);
    public static TdPoint Position(IReadOnlyList<TdPoint> path, double distance)
    {
        double d = Math.Max(0, distance);
        for (int i = 1; i < path.Count; i++)
        {
            TdPoint a = path[i - 1], b = path[i];
            double length = TdMap.Distance(b.X - a.X, b.Y - a.Y);
            if (d <= length) return new(a.X + (b.X - a.X) * d / length,
                a.Y + (b.Y - a.Y) * d / length);
            d -= length;
        }
        return path[^1];
    }
}

public sealed class TowerDefenseGame
{
    public const int MaxWaves = 15;
    public const int MaxEndlessWave = 250;
    public static readonly string[] TowerKinds = ["archer", "frost", "cannon", "ballista", "flame"];
    public static readonly string[] Stages = ["forest", "frost"];
    private IReadOnlyList<TdPoint> Path => TdMap.PathFor(State.Stage);
    private IReadOnlyList<TdPoint> Pads => TdMap.PadsFor(State.Stage);
    public static readonly string[] EnemyKinds = ["grunt", "runner", "brute", "shield"];
    public TdState State { get; private set; }

    public TowerDefenseGame(TdState? saved = null)
    {
        // Corrupt or future-version saves must not load unbounded arrays or malformed geometry.
        State = Valid(saved) ? saved!.Copy() : new TdState();
        // Previous releases let the spawn clock run below zero after the last spawn.
        // Preserve those valid battles and repair their clock on load.
        State.SpawnClock = Math.Max(0, State.SpawnClock);
        if (State.Phase == "running") State.Paused = true;
        State.Progress ??= new TdProgress();
        State.StageRuns ??= new(StringComparer.Ordinal);
        State.Effects.Clear();
    }

    private static bool Valid(TdState? s) =>
        s is { Version: 1 } && s.Wave is >= 0 and <= MaxEndlessWave
        && s.Stage is "forest" or "frost" && s.Screen is "menu" or "skills" or "game"
        && s.Progress is { Level: >= 1 and <= 100, Experience: >= 0 and <= 100_000_000 }
        && s.Progress.Skills is { Count: <= 8 }
        && s.Progress.Skills.All(kv => TdMastery.SkillCaps.TryGetValue(kv.Key, out int cap) && kv.Value >= 0 && kv.Value <= cap)
        && s.Progress.Skills.Values.Sum() <= s.Progress.Level - 1
        && s.Progress.CompletedStages is { Count: <= 2 }
        && s.Progress.CompletedStages.All(Stages.Contains)
        && s.Progress.BestWaves is { Count: <= 2 } && s.Progress.BestWaves.All(kv => Stages.Contains(kv.Key) && kv.Value is >= 0 and <= MaxEndlessWave)
        && s.StageRuns is { Count: <= 2 } && s.StageRuns.All(kv => Stages.Contains(kv.Key) && ValidRun(kv.Value, kv.Key))
        && s.Coins is >= 0 and <= 1_000_000 && s.Lives is >= 0 and <= 30
        && s.Phase is "planning" or "running" or "won" or "lost"
        && s.Speed is 1 or 2 && s.NextEnemyId is > 0 and < 100_000
        && s.Spawned is >= 0 and <= 100 && s.SpawnTotal is >= 0 and <= 100
        && s.WaveKills is >= 0 and <= 100 && s.WaveEscapes is >= 0 and <= 100
        && s.WaveKills + s.WaveEscapes <= s.Spawned
        && s.Ticks is >= 0 and < 100_000_000 && double.IsFinite(s.SpawnClock)
        && s.SpawnClock is > -1_000_000 and < 10
        && s.Towers is { Count: <= 10 } && s.Enemies is { Count: <= 100 }
        && s.Effects is { Count: <= 100 }
        && s.Towers.All(t => t.Pad >= 0 && t.Pad < TdMap.Pads.Count
            && TowerKinds.Contains(t.Kind) && t.Level is >= 1 and <= 3
            && double.IsFinite(t.Cooldown) && t.Cooldown >= 0 && t.Cooldown <= 10)
        && s.Towers.Select(t => t.Pad).Distinct().Count() == s.Towers.Count
        && s.Enemies.All(e => e.Id > 0 && e.Id < s.NextEnemyId
            && EnemyKinds.Contains(e.Kind) && double.IsFinite(e.Distance)
            && e.Distance >= 0 && e.Distance <= TdMap.Length(TdMap.PathFor(s.Stage))
            && double.IsFinite(e.Hp) && e.Hp > 0 && e.Hp < 1_000_000
            && double.IsFinite(e.Slow) && e.Slow >= 0 && e.Slow <= 10)
        && s.Enemies.Select(e => e.Id).Distinct().Count() == s.Enemies.Count;

    private static bool ValidRun(TdStageRun? r, string stage) =>
        r is { Wave: >= 0 and <= MaxEndlessWave, Coins: >= 0 and <= 1_000_000,
            Lives: >= 0 and <= 30, SpawnTotal: >= 0 and <= 100, Spawned: >= 0 and <= 100,
            WaveKills: >= 0 and <= 100, WaveEscapes: >= 0 and <= 100,
            Ticks: >= 0 and < 100_000_000, NextEnemyId: > 0 and < 100_000 }
        && r.Phase is "planning" or "running" or "won" or "lost"
        && r.Speed is 1 or 2 && r.Spawned <= r.SpawnTotal
        && r.WaveKills + r.WaveEscapes <= r.Spawned
        && double.IsFinite(r.SpawnClock) && r.SpawnClock is > -1_000_000 and < 10
        && r.Towers is { Count: <= 10 } && r.Enemies is { Count: <= 100 }
        && r.Towers.All(t => t.Pad >= 0 && t.Pad < TdMap.PadsFor(stage).Count
            && TowerKinds.Contains(t.Kind) && t.Level is >= 1 and <= 3
            && double.IsFinite(t.Cooldown) && t.Cooldown is >= 0 and <= 10)
        && r.Towers.Select(t => t.Pad).Distinct().Count() == r.Towers.Count
        && r.Enemies.All(e => e.Id > 0 && e.Id < r.NextEnemyId
            && EnemyKinds.Contains(e.Kind) && double.IsFinite(e.Distance)
            && e.Distance >= 0 && e.Distance <= TdMap.Length(TdMap.PathFor(stage))
            && double.IsFinite(e.Hp) && e.Hp is > 0 and < 1_000_000
            && double.IsFinite(e.Slow) && e.Slow is >= 0 and <= 10)
        && r.Enemies.Select(e => e.Id).Distinct().Count() == r.Enemies.Count;

    public TdState Save() { TdState copy = State.Copy(); copy.Effects.Clear(); return copy; }

    public static int BuildCost(string kind) => kind switch
    {
        "archer" => 70, "frost" => 105, "cannon" => 135,
        "ballista" => 170, "flame" => 150, _ => int.MaxValue,
    };
    private int Price(int amount) => amount == int.MaxValue ? int.MaxValue
        : Math.Max(1, amount * (100 - State.Progress.Rank("engineer") * 2) / 100);
    private void GrantExperience(int amount)
    {
        var p = State.Progress;
        p.Experience = Math.Min(100_000_000, p.Experience + amount);
        while (p.Level < 100 && p.Experience >= TdMastery.NextLevelCost(p.Level))
        {
            p.Experience -= TdMastery.NextLevelCost(p.Level);
            p.Level++;
        }
    }
    private void ResetBattle(string stage)
    {
        TdState s = State;
        s.Stage = stage; s.Endless = false; s.LastStandUsed = false; s.Wave = 0; s.Coins = 320 + 16 * s.Progress.Rank("capital");
        s.Lives = 20 + 2 * s.Progress.Rank("fortify"); s.Phase = "planning";
        s.Paused = false; s.Speed = 1; s.NextEnemyId = 1; s.Spawned = 0;
        s.SpawnTotal = 0; s.WaveKills = 0; s.WaveEscapes = 0; s.SpawnClock = 0;
        s.Ticks = 0; s.Towers.Clear(); s.Enemies.Clear(); s.Effects.Clear();
    }
    public static int UpgradeCost(TdTower tower) => tower.Level >= 3 ? int.MaxValue
        : (BuildCost(tower.Kind) * (tower.Level + 1)) / 2;

    public (bool Success, string Message) Execute(string action, int? pad = null, string? kind = null)
    {
        TdState s = State;
        if (action == "tower.menu")
        {
            s.StageRuns[s.Stage] = TdStageRun.Capture(s);
            s.Screen = "menu"; s.Paused = true;
            return (true, "Map selection opened.");
        }
        if (action == "tower.skills")
        {
            s.StageRuns[s.Stage] = TdStageRun.Capture(s);
            s.Screen = "skills"; s.Paused = true;
            return (true, "Mastery opened.");
        }
        if (action == "tower.skill.reset")
        {
            if (s.Screen != "skills") return (false, "Open mastery to respec.");
            s.Progress.Skills.Clear();
            return (true, "Skill points returned.");
        }
        if (action == "tower.skill.buy")
        {
            if (s.Screen != "skills" || kind is null
                || !TdMastery.SkillCaps.TryGetValue(kind, out int cap)
                || s.Progress.Rank(kind) >= cap || s.Progress.PointsAvailable < 1)
                return (false, "Skill unavailable.");
            s.Progress.Skills[kind] = s.Progress.Rank(kind) + 1;
            return (true, "Skill upgraded.");
        }
        if (action == "tower.play")
        {
            if (kind is null || !Stages.Contains(kind) || s.Progress.Level < TdMastery.StageLevel(kind))
                return (false, "Map still locked.");
            s.StageRuns[s.Stage] = TdStageRun.Capture(s);
            if (s.StageRuns.TryGetValue(kind, out TdStageRun? run))
                run.Restore(s);
            else ResetBattle(kind);
            s.Stage = kind; s.Screen = "game";
            s.StageRuns.Remove(kind);
            return (true, "Defence opened.");
        }
        if (action == "tower.restart")
        {
            ResetBattle(s.Stage);
            return (true, "New defence ready.");
        }
        if (action == "tower.endless")
        {
            if (s.Screen != "game" || s.Phase != "won") return (false, "Complete the map first.");
            s.Endless = true; s.Phase = "planning"; s.Paused = true;
            return (true, "Endless defence enabled.");
        }
        if (action == "tower.exit") { s.Paused = true; return (true, "Defence saved."); }
        if (action == "tower.pause")
        {
            if (s.Screen != "game" || s.Phase != "running") return (false, "No wave is active.");
            s.Paused = !s.Paused;
            return (true, s.Paused ? "Paused." : "Resumed.");
        }
        if (action == "tower.speed")
        {
            if (s.Screen != "game") return (false, "Open a map first.");
            s.Speed = s.Speed == 1 ? 2 : 1;
            return (true, "Speed changed.");
        }
        if (action == "tower.start")
        {
            if (s.Screen != "game" || s.Phase != "planning" || s.Wave >= MaxEndlessWave ||
                (!s.Endless && s.Wave >= TdMastery.StageWaves(s.Stage))) return (false, "Wave cannot start.");
            if (s.Wave == 0) s.Progress.RunsStarted++;
            s.Wave++; s.Spawned = 0; s.SpawnTotal = Math.Min(96, 7 + s.Wave * 2);
            s.WaveKills = 0; s.WaveEscapes = 0;
            s.SpawnClock = 0; s.Phase = "running"; s.Paused = false;
            return (true, "Wave started.");
        }
        if (s.Screen != "game" || pad is null || pad < 0 || pad >= Pads.Count
            || s.Phase is "won" or "lost") return (false, "Invalid build position.");
        TdTower? current = s.Towers.FirstOrDefault(t => t.Pad == pad);
        if (action == "tower.build")
        {
            int cost = Price(BuildCost(kind ?? ""));
            if (current is not null || cost == int.MaxValue || s.Coins < cost
                || s.Progress.Level < TdMastery.RequiredLevel(kind ?? ""))
                return (false, "Occupied pad, unknown tower or insufficient coins.");
            s.Coins -= cost; s.Towers.Add(new(pad.Value, kind!, 1, 0));
            return (true, "Tower built.");
        }
        if (current is null) return (false, "No tower at this position.");
        if (action == "tower.upgrade")
        {
            int cost = Price(UpgradeCost(current));
            if (s.Coins < cost || cost == int.MaxValue) return (false, "Upgrade unavailable.");
            s.Coins -= cost; s.Towers[s.Towers.IndexOf(current)] = current with { Level = current.Level + 1 };
            return (true, "Tower upgraded.");
        }
        if (action == "tower.sell")
        {
            int spent = Price(BuildCost(current.Kind));
            for (int level = 1; level < current.Level; level++)
                spent += Price(BuildCost(current.Kind) * (level + 1) / 2);
            s.Coins += spent * (60 + 7 * s.Progress.Rank("refund")) / 100;
            s.Towers.Remove(current);
            return (true, "Tower sold.");
        }
        return (false, "Unknown action.");
    }

    private static double Radius(TdTower tower) => tower.Kind switch
    {
        "cannon" => 148 + tower.Level * 10,
        "frost" => 152 + tower.Level * 9,
        "ballista" => 205 + tower.Level * 11,
        "flame" => 128 + tower.Level * 9,
        _ => 162 + tower.Level * 12,
    };
    private static double Damage(TdTower tower) => tower.Kind switch
    {
        "cannon" => 30 + tower.Level * 16,
        "frost" => 9 + tower.Level * 7,
        "ballista" => 49 + tower.Level * 24,
        "flame" => 16 + tower.Level * 11,
        _ => 12 + tower.Level * 10,
    };
    private static double Interval(TdTower tower) => tower.Kind switch
    {
        "cannon" => 1.55, "frost" => 0.9, "ballista" => 1.95, "flame" => 0.45, _ => 0.7,
    };
    private static double MoveSpeed(string kind) => kind switch
    {
        "runner" => 96, "brute" => 32, "shield" => 43, _ => 56,
    };
    private static double MaxHealth(int wave, string kind) => kind switch
    {
        "runner" => 18 + wave * 6, "brute" => 72 + wave * 20,
        "shield" => 40 + wave * 12, _ => 27 + wave * 10,
    };
    private static int Bounty(string kind) => kind switch
    {
        "brute" => 21, "shield" => 15, "runner" => 10, _ => 12,
    };
    private void Spawn()
    {
        TdState s = State;
        int n = s.Spawned++;
        string kind = s.Wave >= 5 && n % 7 == 0 ? "shield"
            : s.Wave >= 3 && n % 6 == 0 ? "brute"
            : n % 4 == 0 ? "runner" : "grunt";
        s.Enemies.Add(new(s.NextEnemyId++, kind, 0, MaxHealth(s.Wave, kind) * (s.Stage == "frost" ? 1.14 : 1), 0));
    }

    public bool Tick()
    {
        TdState s = State;
        if (s.Phase != "running" || s.Paused) return false;
        const double delta = 0.1;
        // Fixed-step simulation; UI timing never changes damage or movement.
        for (int substep = 0; substep < s.Speed; substep++)
        {
            s.Ticks++;
            s.Effects = s.Effects.Where(f => f.Ticks > 1)
                .Select(f => f with { Ticks = f.Ticks - 1 }).ToList();
            // Once all enemies have spawned, the clock must stay valid for snapshots
            // and saved games. Negative values previously froze the entire Lounge.
            if (s.Spawned < s.SpawnTotal)
            {
                s.SpawnClock = Math.Max(0, s.SpawnClock - delta);
                if (s.SpawnClock <= 0)
                {
                    Spawn();
                    s.SpawnClock = s.Spawned < s.SpawnTotal ? 0.73 : 0;
                }
            }
            for (int i = s.Enemies.Count - 1; i >= 0; i--)
            {
                TdEnemy enemy = s.Enemies[i];
                double movement = MoveSpeed(enemy.Kind) * delta * (enemy.Slow > 0 ? 0.5 : 1);
                enemy = enemy with { Distance = enemy.Distance + movement,
                    Slow = Math.Max(0, enemy.Slow - delta) };
                if (enemy.Distance >= TdMap.Length(Path))
                {
                    s.Lives = Math.Max(0, s.Lives - (enemy.Kind == "brute" ? 2 : 1));
                    s.WaveEscapes++;
                    s.Enemies.RemoveAt(i);
                }
                else s.Enemies[i] = enemy;
            }
            if (s.Lives == 0)
            {
                if (s.Progress.Rank("last-stand") > 0 && !s.LastStandUsed)
                {
                    s.LastStandUsed = true;
                    s.Lives = 1;
                }
                else
                {
                    s.Phase = "lost"; s.Paused = true; s.Progress.Defeats++;
                    s.Progress.EnemiesDefeated += s.WaveKills;
                    s.Progress.EnemiesEscaped += s.WaveEscapes;
                    break;
                }
            }
            for (int i = 0; i < s.Towers.Count; i++)
            {
                TdTower tower = s.Towers[i];
                double cooldown = Math.Max(0, tower.Cooldown - delta);
                if (cooldown > 0) { s.Towers[i] = tower with { Cooldown = cooldown }; continue; }
                TdPoint source = Pads[tower.Pad];
                TdEnemy? target = s.Enemies.Where(e =>
                {
                    TdPoint point = TdMap.Position(Path, e.Distance);
                    return TdMap.Distance(source.X - point.X, source.Y - point.Y) <= Radius(tower);
                }).OrderByDescending(e => e.Distance).FirstOrDefault();
                if (target is null) { s.Towers[i] = tower with { Cooldown = 0 }; continue; }
                s.Towers[i] = tower with { Cooldown = Interval(tower) / (1 + (tower.Level - 1) * .12) };
                TdPoint hit = TdMap.Position(Path, target.Distance);
                s.Effects.Add(new(source.X, source.Y, hit.X, hit.Y, tower.Kind, 3));
                double damage = Damage(tower) * (1 + .02 * s.Progress.Rank("focus"));
                if (tower.Kind == "cannon" || tower.Kind == "flame")
                {
                    foreach (TdEnemy enemy in s.Enemies.ToArray())
                    {
                        TdPoint p = TdMap.Position(Path, enemy.Distance);
                        if (TdMap.Distance(p.X - hit.X, p.Y - hit.Y) <= (tower.Kind == "flame" ? 37 : 60))
                            Hit(enemy.Id, damage, false);
                    }
                }
                else Hit(target.Id, damage, tower.Kind == "frost");
            }
            if (s.Spawned >= s.SpawnTotal && s.Enemies.Count == 0)
            {
                s.Coins += 35 + s.Wave * 9;
                s.Progress.WavesCleared++;
                s.Progress.EnemiesDefeated += s.WaveKills;
                s.Progress.EnemiesEscaped += s.WaveEscapes;
                s.Progress.BestWaves[s.Stage] = Math.Max(s.Wave, s.Progress.BestWaves.GetValueOrDefault(s.Stage));
                GrantExperience(30 + s.Wave * 13);
                bool victory = !s.Endless && s.Wave >= TdMastery.StageWaves(s.Stage);
                if (victory)
                {
                    s.Progress.Victories++;
                    if (s.Progress.CompletedStages.Add(s.Stage)) GrantExperience(200);
                }
                s.Phase = victory ? "won" : "planning";
                s.Paused = true;
                break;
            }
        }
        return true;
    }

    private void Hit(int id, double amount, bool frost)
    {
        TdState s = State;
        int index = s.Enemies.FindIndex(e => e.Id == id);
        if (index < 0) return;
        TdEnemy e = s.Enemies[index];
        double armor = e.Kind == "shield" ? 5 : e.Kind == "brute" ? 2 : 0;
        double hp = e.Hp - Math.Max(1, amount - armor);
        if (hp <= 0)
        {
            s.Coins += Bounty(e.Kind); s.WaveKills++; s.Enemies.RemoveAt(index);
        }
        else s.Enemies[index] = e with { Hp = hp, Slow = frost ? 1.6 : e.Slow };
    }
}
