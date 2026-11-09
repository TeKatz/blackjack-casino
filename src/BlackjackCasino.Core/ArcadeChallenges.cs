namespace BlackjackCasino.Core;

/// <summary>Persistent per-game arcade statistics used by account-wide challenges.</summary>
public sealed class ArcadeGameStatistics
{
    public long Runs { get; set; }
    public long BestScore { get; set; }
    public long TotalScore { get; set; }
    public long LongestSeconds { get; set; }
}

public sealed class ArcadeStatistics
{
    public Dictionary<string, ArcadeGameStatistics> Games { get; set; } = new(StringComparer.Ordinal);
    public List<Guid> RecordedRuns { get; set; } = [];

    public void Normalize()
    {
        Games ??= new(StringComparer.Ordinal);
        RecordedRuns ??= [];
        foreach (string key in Games.Keys.ToArray())
            if (!ArcadeChallengeCatalog.IsGame(key))
                Games.Remove(key);
            else
            {
                Games[key] ??= new();
                ArcadeGameStatistics row = Games[key];
                row.Runs = Math.Max(0, row.Runs);
                row.BestScore = Math.Max(0, row.BestScore);
                row.TotalScore = Math.Max(0, row.TotalScore);
                row.LongestSeconds = Math.Max(0, row.LongestSeconds);
            }
        RecordedRuns.RemoveAll(id => id == Guid.Empty);
        if (RecordedRuns.Count > 2048)
            RecordedRuns.RemoveRange(0, RecordedRuns.Count - 2048);
    }
}

public sealed record ArcadeRunReceipt(Guid RunId, string GameId, int Score, int Seconds);

/// <summary>Only terminal game receipts count: no per-frame messages or partial scores.</summary>
public static class ArcadeProgression
{
    public static bool Process(ArcadeStatistics state, ArcadeRunReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.RunId == Guid.Empty || !ArcadeChallengeCatalog.IsGame(receipt.GameId)
            || receipt.Score is < 0 or > 100_000_000
            || receipt.Seconds is < 0 or > 21_600
            || state.RecordedRuns.Contains(receipt.RunId)) return false;

        ArcadeGameStatistics stats = state.Games.TryGetValue(receipt.GameId, out ArcadeGameStatistics? existing)
            ? existing : new ArcadeGameStatistics();
        stats.Runs = Math.Min(10_000_000, stats.Runs + 1);
        stats.BestScore = Math.Max(stats.BestScore, receipt.Score);
        stats.TotalScore = Math.Min(1_000_000_000_000L, stats.TotalScore + receipt.Score);
        stats.LongestSeconds = Math.Max(stats.LongestSeconds, receipt.Seconds);
        state.Games[receipt.GameId] = stats;
        state.RecordedRuns.Add(receipt.RunId);
        if (state.RecordedRuns.Count > 2048) state.RecordedRuns.RemoveAt(0);
        return true;
    }
}

/// <summary>Exactly ten independently tracked global milestones for each of ten Arcade games.</summary>
public static class ArcadeChallengeCatalog
{
    private sealed record Game(string Id, string Name, int[] Scores, int[] Runs,
        int[] Totals, int Survival);
    private static readonly Game[] Games =
    [
        new("midnight-wings", "Midnight Wings", [5, 15, 40, 100], [1, 10, 75], [100, 1000], 60),
        new("cave-escape", "Cave Escape", [100, 300, 700, 1500], [1, 10, 75], [1000, 10000], 90),
        new("hexagon-hell", "Hexagon Hell", [3, 10, 25, 60], [1, 10, 75], [60, 400], 45),
        new("perfect-stack", "Perfect Stack", [5, 10, 20, 40], [1, 10, 75], [50, 400], 60),
        new("spike-rush", "Spike Rush", [5, 15, 35, 75], [1, 10, 75], [100, 850], 60),
        new("blade-timing", "Blade Timing", [5, 12, 25, 50], [1, 10, 75], [60, 500], 45),
        new("bullet-storm", "Bullet Storm", [100, 300, 600, 1000], [1, 10, 75], [1000, 9500], 90),
        new("neon-breaker", "Neon Breaker", [100, 350, 800, 1800], [1, 10, 75], [1000, 10000], 120),
        new("death-snake", "Death Snake", [5, 12, 25, 50], [1, 10, 75], [50, 400], 90),
        new("endless-ascent", "Endless Ascent", [50, 150, 400, 1000], [1, 10, 75], [700, 6500], 90),
    ];
    private static readonly HashSet<string> GameIds = Games.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
    public static bool IsGame(string? id) => id is not null && GameIds.Contains(id);
    private static readonly (string Suffix, ChallengeMetric Metric, int Tier, int Coins)[] Milestones =
    [
        ("first", ChallengeMetric.ArcadeRuns, 0, 75),
        ("runs-10", ChallengeMetric.ArcadeRuns, 1, 200),
        ("runs-75", ChallengeMetric.ArcadeRuns, 2, 800),
        ("score-1", ChallengeMetric.ArcadeBestScore, 0, 100),
        ("score-2", ChallengeMetric.ArcadeBestScore, 1, 225),
        ("score-3", ChallengeMetric.ArcadeBestScore, 2, 550),
        ("score-master", ChallengeMetric.ArcadeBestScore, 3, 2000),
        ("total-1", ChallengeMetric.ArcadeTotalScore, 0, 250),
        ("total-2", ChallengeMetric.ArcadeTotalScore, 1, 1150),
        ("survivor", ChallengeMetric.ArcadeLongestSeconds, 0, 750),
    ];
    private static ChallengeDefinition Make(Game game, (string Suffix, ChallengeMetric Metric, int Tier, int Coins) m)
    {
        int goal = m.Metric switch
        {
            ChallengeMetric.ArcadeRuns => game.Runs[m.Tier],
            ChallengeMetric.ArcadeBestScore => game.Scores[m.Tier],
            ChallengeMetric.ArcadeTotalScore => game.Totals[m.Tier],
            ChallengeMetric.ArcadeLongestSeconds => game.Survival,
            _ => throw new InvalidOperationException("Unknown Arcade challenge metric.")
        };
        string name = m.Suffix switch
        {
            "first" => $"{game.Name}: First Attempt",
            "runs-10" => $"{game.Name}: Regular",
            "runs-75" => $"{game.Name}: Veteran",
            "score-1" => $"{game.Name}: Spark",
            "score-2" => $"{game.Name}: Momentum",
            "score-3" => $"{game.Name}: Expert",
            "score-master" => $"{game.Name}: Master",
            "total-1" => $"{game.Name}: Dedicated",
            "total-2" => $"{game.Name}: Unstoppable",
            _ => $"{game.Name}: Survivor",
        };
        string description = m.Metric switch
        {
            ChallengeMetric.ArcadeRuns => $"Complete {goal:N0} {game.Name} runs.",
            ChallengeMetric.ArcadeBestScore => $"Reach a personal run score of {goal:N0} in {game.Name}.",
            ChallengeMetric.ArcadeTotalScore => $"Earn {goal:N0} total points across finished {game.Name} runs.",
            ChallengeMetric.ArcadeLongestSeconds => $"Survive at least {goal:N0} seconds in one {game.Name} run.",
            _ => ""
        };
        return new ChallengeDefinition($"arcade-{game.Id}-{m.Suffix}", ChallengeCategory.Arcade,
            name, description, m.Metric, goal, m.Coins, m.Suffix == "score-master");
    }
    public static IReadOnlyList<ChallengeDefinition> All { get; } =
        Games.SelectMany(g => Milestones.Select(m => Make(g, m))).ToArray();
    public static decimal Progress(ArcadeStatistics stats, ChallengeDefinition challenge)
    {
        if (!challenge.Id.StartsWith("arcade-", StringComparison.Ordinal)) return 0;
        Game? game = Games.FirstOrDefault(g =>
            challenge.Id.StartsWith("arcade-" + g.Id + "-", StringComparison.Ordinal));
        if (game is null || !stats.Games.TryGetValue(game.Id, out ArcadeGameStatistics? row) || row is null)
            return 0;
        return challenge.Metric switch
        {
            ChallengeMetric.ArcadeRuns => row.Runs,
            ChallengeMetric.ArcadeBestScore => row.BestScore,
            ChallengeMetric.ArcadeTotalScore => row.TotalScore,
            ChallengeMetric.ArcadeLongestSeconds => row.LongestSeconds,
            _ => 0,
        };
    }
}
