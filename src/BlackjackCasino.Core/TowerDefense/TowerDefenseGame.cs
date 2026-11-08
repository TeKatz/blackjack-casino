namespace BlackjackCasino.Core.TowerDefense;

/// <summary>Offline, wager-free tower defence rules. Rendering never determines hits or rewards.</summary>
public sealed record TdPoint(double X, double Y);
public sealed record TdTower(int Pad, string Kind, int Level, double Cooldown);
public sealed record TdEnemy(int Id, string Kind, double Distance, double Hp, double Slow);
public sealed record TdEffect(double FromX, double FromY, double ToX, double ToY, string Kind, int Ticks);
public sealed record TdState
{
    public int Version { get; set; } = 1;
    public int Wave { get; set; }
    public int Coins { get; set; } = 320;
    public int Lives { get; set; } = 20;
    public string Phase { get; set; } = "planning";
    public bool Paused { get; set; }
    public int Speed { get; set; } = 1;
    public int NextEnemyId { get; set; } = 1;
    public int Spawned { get; set; }
    public int SpawnTotal { get; set; }
    public double SpawnClock { get; set; }
    public long Ticks { get; set; }
    public List<TdTower> Towers { get; set; } = [];
    public List<TdEnemy> Enemies { get; set; } = [];
    public List<TdEffect> Effects { get; set; } = [];
    public TdState Copy() => new()
    {
        Version = Version, Wave = Wave, Coins = Coins, Lives = Lives, Phase = Phase,
        Paused = Paused, Speed = Speed, NextEnemyId = NextEnemyId, Spawned = Spawned,
        SpawnTotal = SpawnTotal, SpawnClock = SpawnClock, Ticks = Ticks,
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
    public static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
    public static double PathLength { get; } = Enumerable.Range(1, Path.Count - 1)
        .Sum(i => TdMap.Distance(Path[i].X - Path[i - 1].X, Path[i].Y - Path[i - 1].Y));
    public static TdPoint Position(double distance)
    {
        double d = Math.Max(0, distance);
        for (int i = 1; i < Path.Count; i++)
        {
            TdPoint a = Path[i - 1], b = Path[i];
            double length = TdMap.Distance(b.X - a.X, b.Y - a.Y);
            if (d <= length) return new(a.X + (b.X - a.X) * d / length,
                a.Y + (b.Y - a.Y) * d / length);
            d -= length;
        }
        return Path[^1];
    }
}

public sealed class TowerDefenseGame
{
    public const int MaxWaves = 15;
    public static readonly string[] TowerKinds = ["archer", "frost", "cannon"];
    public static readonly string[] EnemyKinds = ["grunt", "runner", "brute", "shield"];
    public TdState State { get; private set; }

    public TowerDefenseGame(TdState? saved = null)
    {
        // Corrupt or future-version saves must not load unbounded arrays or malformed geometry.
        State = Valid(saved) ? saved!.Copy() : new TdState();
        if (State.Phase == "running") State.Paused = true;
        State.Effects.Clear();
    }

    private static bool Valid(TdState? s) =>
        s is { Version: 1 } && s.Wave is >= 0 and <= MaxWaves
        && s.Coins is >= 0 and <= 1_000_000 && s.Lives is >= 0 and <= 20
        && s.Phase is "planning" or "running" or "won" or "lost"
        && s.Speed is 1 or 2 && s.NextEnemyId is > 0 and < 100_000
        && s.Spawned is >= 0 and <= 100 && s.SpawnTotal is >= 0 and <= 100
        && s.Ticks is >= 0 and < 10_000_000 && double.IsFinite(s.SpawnClock)
        && s.SpawnClock is >= 0 and < 10
        && s.Towers is { Count: <= 10 } && s.Enemies is { Count: <= 100 }
        && s.Effects is { Count: <= 100 }
        && s.Towers.All(t => t.Pad >= 0 && t.Pad < TdMap.Pads.Count
            && TowerKinds.Contains(t.Kind) && t.Level is >= 1 and <= 3
            && double.IsFinite(t.Cooldown) && t.Cooldown >= 0 && t.Cooldown <= 10)
        && s.Towers.Select(t => t.Pad).Distinct().Count() == s.Towers.Count
        && s.Enemies.All(e => e.Id > 0 && e.Id < s.NextEnemyId
            && EnemyKinds.Contains(e.Kind) && double.IsFinite(e.Distance)
            && e.Distance >= 0 && e.Distance <= TdMap.PathLength
            && double.IsFinite(e.Hp) && e.Hp > 0 && e.Hp < 1_000_000
            && double.IsFinite(e.Slow) && e.Slow >= 0 && e.Slow <= 10)
        && s.Enemies.Select(e => e.Id).Distinct().Count() == s.Enemies.Count;

    public TdState Save() { TdState copy = State.Copy(); copy.Effects.Clear(); return copy; }

    public static int BuildCost(string kind) => kind switch
    {
        "archer" => 70, "frost" => 105, "cannon" => 135, _ => int.MaxValue,
    };
    public static int UpgradeCost(TdTower tower) => tower.Level >= 3 ? int.MaxValue
        : (BuildCost(tower.Kind) * (tower.Level + 1)) / 2;

    public (bool Success, string Message) Execute(string action, int? pad = null, string? kind = null)
    {
        TdState s = State;
        if (action == "tower.restart") { State = new TdState(); return (true, "New defence ready."); }
        if (action == "tower.exit") { s.Paused = true; return (true, "Defence saved."); }
        if (action == "tower.pause")
        {
            if (s.Phase != "running") return (false, "No wave is active.");
            s.Paused = !s.Paused;
            return (true, s.Paused ? "Paused." : "Resumed.");
        }
        if (action == "tower.speed")
        {
            s.Speed = s.Speed == 1 ? 2 : 1;
            return (true, "Speed changed.");
        }
        if (action == "tower.start")
        {
            if (s.Phase != "planning" || s.Wave >= MaxWaves) return (false, "Wave cannot start.");
            s.Wave++; s.Spawned = 0; s.SpawnTotal = 7 + s.Wave * 2;
            s.SpawnClock = 0; s.Phase = "running"; s.Paused = false;
            return (true, "Wave started.");
        }
        if (pad is null || pad < 0 || pad >= TdMap.Pads.Count
            || s.Phase is "won" or "lost") return (false, "Invalid build position.");
        TdTower? current = s.Towers.FirstOrDefault(t => t.Pad == pad);
        if (action == "tower.build")
        {
            int cost = BuildCost(kind ?? "");
            if (current is not null || cost == int.MaxValue || s.Coins < cost)
                return (false, "Occupied pad, unknown tower or insufficient coins.");
            s.Coins -= cost; s.Towers.Add(new(pad.Value, kind!, 1, 0));
            return (true, "Tower built.");
        }
        if (current is null) return (false, "No tower at this position.");
        if (action == "tower.upgrade")
        {
            int cost = UpgradeCost(current);
            if (s.Coins < cost || cost == int.MaxValue) return (false, "Upgrade unavailable.");
            s.Coins -= cost; s.Towers[s.Towers.IndexOf(current)] = current with { Level = current.Level + 1 };
            return (true, "Tower upgraded.");
        }
        if (action == "tower.sell")
        {
            int spent = BuildCost(current.Kind);
            for (int level = 1; level < current.Level; level++)
                spent += BuildCost(current.Kind) * (level + 1) / 2;
            s.Coins += spent * 60 / 100;
            s.Towers.Remove(current);
            return (true, "Tower sold.");
        }
        return (false, "Unknown action.");
    }

    private static double Radius(TdTower tower) => tower.Kind switch
    {
        "cannon" => 148 + tower.Level * 10,
        "frost" => 152 + tower.Level * 9,
        _ => 162 + tower.Level * 12,
    };
    private static double Damage(TdTower tower) => tower.Kind switch
    {
        "cannon" => 30 + tower.Level * 16,
        "frost" => 9 + tower.Level * 7,
        _ => 12 + tower.Level * 10,
    };
    private static double Interval(TdTower tower) => tower.Kind switch
    {
        "cannon" => 1.55, "frost" => 0.9, _ => 0.7,
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
        s.Enemies.Add(new(s.NextEnemyId++, kind, 0, MaxHealth(s.Wave, kind), 0));
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
            s.SpawnClock -= delta;
            if (s.Spawned < s.SpawnTotal && s.SpawnClock <= 0)
            {
                Spawn();
                s.SpawnClock = Math.Max(0, s.SpawnClock) + 0.73;
            }
            for (int i = s.Enemies.Count - 1; i >= 0; i--)
            {
                TdEnemy enemy = s.Enemies[i];
                double movement = MoveSpeed(enemy.Kind) * delta * (enemy.Slow > 0 ? 0.5 : 1);
                enemy = enemy with { Distance = enemy.Distance + movement,
                    Slow = Math.Max(0, enemy.Slow - delta) };
                if (enemy.Distance >= TdMap.PathLength)
                {
                    s.Lives = Math.Max(0, s.Lives - (enemy.Kind == "brute" ? 2 : 1));
                    s.Enemies.RemoveAt(i);
                }
                else s.Enemies[i] = enemy;
            }
            if (s.Lives == 0) { s.Phase = "lost"; s.Paused = true; break; }
            for (int i = 0; i < s.Towers.Count; i++)
            {
                TdTower tower = s.Towers[i];
                double cooldown = Math.Max(0, tower.Cooldown - delta);
                if (cooldown > 0) { s.Towers[i] = tower with { Cooldown = cooldown }; continue; }
                TdPoint source = TdMap.Pads[tower.Pad];
                TdEnemy? target = s.Enemies.Where(e =>
                {
                    TdPoint point = TdMap.Position(e.Distance);
                    return TdMap.Distance(source.X - point.X, source.Y - point.Y) <= Radius(tower);
                }).OrderByDescending(e => e.Distance).FirstOrDefault();
                if (target is null) { s.Towers[i] = tower with { Cooldown = 0 }; continue; }
                s.Towers[i] = tower with { Cooldown = Interval(tower) / (1 + (tower.Level - 1) * .12) };
                TdPoint hit = TdMap.Position(target.Distance);
                s.Effects.Add(new(source.X, source.Y, hit.X, hit.Y, tower.Kind, 3));
                double damage = Damage(tower);
                if (tower.Kind == "cannon")
                {
                    foreach (TdEnemy enemy in s.Enemies.ToArray())
                    {
                        TdPoint p = TdMap.Position(enemy.Distance);
                        if (TdMap.Distance(p.X - hit.X, p.Y - hit.Y) <= 60)
                            Hit(enemy.Id, damage, false);
                    }
                }
                else Hit(target.Id, damage, tower.Kind == "frost");
            }
            if (s.Spawned >= s.SpawnTotal && s.Enemies.Count == 0)
            {
                s.Coins += 35 + s.Wave * 9;
                s.Phase = s.Wave >= MaxWaves ? "won" : "planning";
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
            s.Coins += Bounty(e.Kind); s.Enemies.RemoveAt(index);
        }
        else s.Enemies[index] = e with { Hp = hp, Slow = frost ? 1.6 : e.Slow };
    }
}
