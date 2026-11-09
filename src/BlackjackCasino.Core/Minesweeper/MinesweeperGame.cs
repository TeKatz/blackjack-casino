using System.Security.Cryptography;

namespace BlackjackCasino.Core.Minesweeper;

/// <summary>Midnight Sweep: an offline, no-wager minefield. Only Core knows hidden hazards.</summary>
public sealed record SweepCell
{
    public bool Mine { get; set; }
    public bool Revealed { get; set; }
    public bool Flagged { get; set; }
    public int Adjacent { get; set; }
    public bool Scanned { get; set; }
}

public sealed record SweepRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Difficulty { get; set; } = "easy";
    public int Width { get; set; } = 8;
    public int Height { get; set; } = 8;
    public int Mines { get; set; } = 10;
    public string Phase { get; set; } = "ready";
    public int Seconds { get; set; }
    public int ScansUsed { get; set; }
    public int SafeRevealed { get; set; }
    public int ExplodedIndex { get; set; } = -1;
    public List<SweepCell> Cells { get; set; } = Enumerable.Range(0, 64).Select(_ => new SweepCell()).ToList();
    public bool Finished => Phase is "won" or "lost";
    public SweepRun Copy() => this with { Cells = Cells.Select(c => c with { }).ToList() };
    public static SweepRun New(string difficulty)
    {
        (int width, int height, int mines) = difficulty switch
        {
            "standard" => (12, 12, 24),
            "expert" => (16, 16, 40),
            _ => (8, 8, 10),
        };
        return new SweepRun
        {
            Difficulty = difficulty is "easy" or "standard" or "expert" ? difficulty : "easy",
            Width = width, Height = height, Mines = mines,
            Cells = Enumerable.Range(0, width * height).Select(_ => new SweepCell()).ToList(),
        };
    }
}

public sealed record SweepProgress
{
    public string Screen { get; set; } = "menu";
    public SweepRun Run { get; set; } = SweepRun.New("easy");
    public SweepProgress Copy() => this with { Run = Run.Copy() };
}

/// <summary>Only terminal run ids are counted; retries, reopens and duplicate commands cannot farm milestones.</summary>
public sealed record SweepStatistics
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int EasyWins { get; set; }
    public int StandardWins { get; set; }
    public int ExpertWins { get; set; }
    public int NoScanWins { get; set; }
    public int BestWinStreak { get; set; }
    public int CurrentWinStreak { get; set; }
    public long SafeCellsRevealed { get; set; }
    public long ScansUsed { get; set; }
    public int BestWinSeconds { get; set; }
    public HashSet<Guid> CompletedRunIds { get; set; } = [];
    public SweepStatistics Copy() => this with { CompletedRunIds = new(CompletedRunIds) };
    public void Normalize()
    {
        CompletedRunIds ??= [];
        CompletedRunIds.Remove(Guid.Empty);
        Wins = Math.Max(0, Wins);
        Losses = Math.Max(0, Losses);
        EasyWins = Math.Max(0, EasyWins);
        StandardWins = Math.Max(0, StandardWins);
        ExpertWins = Math.Max(0, ExpertWins);
        NoScanWins = Math.Max(0, NoScanWins);
        BestWinStreak = Math.Max(0, BestWinStreak);
        CurrentWinStreak = Math.Max(0, CurrentWinStreak);
        SafeCellsRevealed = Math.Max(0, SafeCellsRevealed);
        ScansUsed = Math.Max(0, ScansUsed);
        BestWinSeconds = Math.Max(0, BestWinSeconds);
    }
    public bool Record(SweepRun run)
    {
        if (run.Id == Guid.Empty || !run.Finished || !CompletedRunIds.Add(run.Id)) return false;
        SafeCellsRevealed += run.SafeRevealed;
        ScansUsed += run.ScansUsed;
        if (run.Phase == "lost")
        {
            Losses++;
            CurrentWinStreak = 0;
            return true;
        }
        Wins++;
        CurrentWinStreak++;
        BestWinStreak = Math.Max(BestWinStreak, CurrentWinStreak);
        if (run.ScansUsed == 0) NoScanWins++;
        switch (run.Difficulty)
        {
            case "easy": EasyWins++; break;
            case "standard": StandardWins++; break;
            case "expert": ExpertWins++; break;
        }
        if (BestWinSeconds == 0 || run.Seconds < BestWinSeconds)
            BestWinSeconds = Math.Max(1, run.Seconds);
        return true;
    }
}

public sealed class MinesweeperGame
{
    public SweepProgress State { get; private set; }
    public MinesweeperGame(SweepProgress? saved = null)
    {
        var candidate = saved?.Copy();
        State = candidate is not null && Valid(candidate) ? candidate : new SweepProgress();
    }
    private static bool Valid(SweepProgress p)
    {
        if (p.Screen is not ("menu" or "game") || p.Run is null) return false;
        SweepRun r = p.Run;
        var expected = SweepRun.New(r.Difficulty);
        if (r.Id == Guid.Empty || r.Width != expected.Width || r.Height != expected.Height ||
            r.Mines != expected.Mines || r.Difficulty != expected.Difficulty ||
            r.Cells is null || r.Cells.Count != r.Width * r.Height ||
            r.Phase is not ("ready" or "active" or "won" or "lost") ||
            r.Seconds < 0 || r.Seconds > 100_000_000 || r.ScansUsed is < 0 or > 1 ||
            r.ExplodedIndex < -1 || r.ExplodedIndex >= r.Cells.Count ||
            r.Cells.Any(c => c is null || c.Adjacent is < 0 or > 8) ||
            r.Cells.Count(c => c.Mine) != (r.Phase == "ready" ? 0 : r.Mines) ||
            r.SafeRevealed != r.Cells.Count(c => !c.Mine && c.Revealed) ||
            (r.Phase == "won" && r.SafeRevealed != r.Cells.Count - r.Mines)) return false;
        return true;
    }
    public SweepProgress Save() => State.Copy();

    public object Snapshot()
    {
        SweepRun r = State.Run;
        return new
        {
            State.Screen, RunId = r.Id, r.Difficulty, r.Width, r.Height, r.Mines, r.Phase,
            r.Seconds, r.ScansUsed, r.SafeRevealed, r.ExplodedIndex,
            Flagged = r.Cells.Count(c => c.Flagged),
            Cells = r.Cells.Select(c => new
            {
                c.Revealed, c.Flagged, c.Scanned,
                Adjacent = c.Revealed ? c.Adjacent : -1,
                ScanCount = c.Scanned && !c.Revealed ? c.Adjacent + (c.Mine ? 1 : 0) : -1,
                Mine = r.Finished && c.Mine,
            }).ToArray(),
        };
    }

    private IEnumerable<int> Neighbours(int index)
    {
        int x = index % State.Run.Width, y = index / State.Run.Width;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < State.Run.Width && ny >= 0 && ny < State.Run.Height)
                yield return ny * State.Run.Width + nx;
        }
    }
    private void Seed(int safe)
    {
        var cells = State.Run.Cells;
        var excluded = Neighbours(safe).Append(safe).ToHashSet();
        var candidates = Enumerable.Range(0, cells.Count).Where(i => !excluded.Contains(i)).ToArray();
        // Cryptographic shuffle is used for the game's random hazard placement, never UI timing.
        for (int i = 0; i < State.Run.Mines; i++)
        {
            int selected = RandomNumberGenerator.GetInt32(i, candidates.Length);
            (candidates[i], candidates[selected]) = (candidates[selected], candidates[i]);
            cells[candidates[i]].Mine = true;
        }
        for (int i = 0; i < cells.Count; i++)
            cells[i].Adjacent = Neighbours(i).Count(n => cells[n].Mine);
        State.Run.Phase = "active";
    }
    private void Reveal(int index)
    {
        SweepRun r = State.Run;
        var queue = new Queue<int>();
        queue.Enqueue(index);
        while (queue.TryDequeue(out int current))
        {
            SweepCell cell = r.Cells[current];
            if (cell.Revealed || cell.Flagged) continue;
            cell.Revealed = true;
            cell.Scanned = false;
            if (cell.Mine)
            {
                r.Phase = "lost";
                r.ExplodedIndex = current;
                return;
            }
            r.SafeRevealed++;
            if (cell.Adjacent == 0)
                foreach (int neighbor in Neighbours(current)) queue.Enqueue(neighbor);
        }
        if (r.SafeRevealed == r.Cells.Count - r.Mines) r.Phase = "won";
    }
    public (bool Success, string Message) Execute(string action, int? index = null, string? difficulty = null)
    {
        if (action == "mine.exit") { State.Screen = "menu"; return (true, "Lounge"); }
        if (action == "mine.menu") { State.Screen = "menu"; return (true, "Map selection"); }
        if (action == "mine.resume") { State.Screen = "game"; return (true, "Resumed"); }
        if (action == "mine.new")
        {
            if (difficulty is not ("easy" or "standard" or "expert") || index is not null)
                return (false, "Invalid difficulty.");
            State.Run = SweepRun.New(difficulty);
            State.Screen = "game";
            return (true, "New field");
        }
        SweepRun run = State.Run;
        if (State.Screen != "game" || run.Finished || index is null || index < 0 || index >= run.Cells.Count)
            return (false, "Field unavailable.");
        int i = index.Value;
        SweepCell cell = run.Cells[i];
        if (action == "mine.flag")
        {
            if (cell.Revealed) return (false, "Already uncovered.");
            cell.Flagged = !cell.Flagged;
            return (true, cell.Flagged ? "Marked" : "Unmarked");
        }
        if (action == "mine.scan")
        {
            if (run.Phase != "active" || run.ScansUsed >= 1 || cell.Revealed || cell.Flagged || cell.Scanned)
                return (false, "Echo scan unavailable.");
            cell.Scanned = true;
            run.ScansUsed++;
            return (true, "Echo located");
        }
        if (action == "mine.reveal")
        {
            if (cell.Flagged) return (false, "Remove the marker first.");
            if (cell.Revealed) return (false, "Already uncovered.");
            if (run.Phase == "ready") Seed(i);
            Reveal(i);
            return (true, run.Phase);
        }
        if (action == "mine.chord")
        {
            if (!cell.Revealed || cell.Adjacent == 0 ||
                Neighbours(i).Count(n => run.Cells[n].Flagged) != cell.Adjacent)
                return (false, "Markers do not match the number.");
            foreach (int n in Neighbours(i).ToArray())
            {
                if (run.Phase != "active") break;
                Reveal(n);
            }
            return (true, run.Phase);
        }
        return (false, "Unknown action.");
    }
    public bool Tick()
    {
        if (State.Screen != "game" || State.Run.Phase != "active") return false;
        State.Run.Seconds = Math.Min(100_000_000, State.Run.Seconds + 1);
        return true;
    }
}
