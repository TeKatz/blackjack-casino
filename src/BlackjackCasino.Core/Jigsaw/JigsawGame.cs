namespace BlackjackCasino.Core.Jigsaw;

public sealed record JigsawMotif(string Id, string Name, string Category, string Asset);

public sealed record JigsawSize(int Count, int Columns, int Rows);

public static class JigsawCatalog
{
    // Reuse bundled, offline collection illustrations. Never download pictures during a game.
    public static readonly IReadOnlyList<JigsawMotif> Motifs =
    [
        new("enchanted-forest", "Enchanted Forest", "Nature", "enchanted-forest.png"),
        new("sakura-garden", "Sakura Garden", "Nature", "sakura-garden.png"),
        new("ocean-depths", "Ocean Depths", "Nature", "ocean-depths.png"),
        new("frost-kingdom", "Frost Kingdom", "Fantasy", "frost-kingdom.png"),
        new("celestial-voyage", "Celestial Voyage", "Fantasy", "celestial-voyage.png"),
        new("pirate-cove", "Pirate Cove", "Fantasy", "pirate-cove.png"),
        new("midnight-manor", "Midnight Manor", "The Midnight Club", "midnight-manor.png"),
        new("neon-city", "Neon City", "The Midnight Club", "neon-city.png"),
        new("eternal-casino", "Eternal Casino", "The Midnight Club", "eternal-casino.png"),
        new("clockwork", "Clockwork", "Art & Design", "clockwork.png"),
        new("desert-gold", "Desert Gold", "Art & Design", "desert-gold.png"),
        new("primordial-dragons", "Primordial Dragons", "Fantasy", "primordial-dragons.png"),
    ];

    public static readonly IReadOnlyList<JigsawSize> Sizes =
    [
        new(24, 6, 4), new(48, 8, 6), new(96, 12, 8), new(150, 15, 10),
        new(300, 20, 15), new(500, 25, 20), new(750, 30, 25), new(1000, 40, 25),
    ];

    public static JigsawMotif Motif(string? id) => Motifs.FirstOrDefault(x => x.Id == id) ?? Motifs[0];
    public static JigsawSize Size(int count) => Sizes.FirstOrDefault(x => x.Count == count) ?? Sizes[2];
}

public sealed class JigsawPieceState
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool OnBoard { get; set; }
    public bool Locked { get; set; }
}

public sealed class JigsawGameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public string MotifId { get; set; } = JigsawCatalog.Motifs[0].Id;
    public int PieceCount { get; set; } = 96;
    public List<JigsawPieceState> Pieces { get; set; } = [];
    public long ElapsedSeconds { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Completed => Pieces.Count == PieceCount && Pieces.All(x => x.Locked);
    public int LockedCount => Pieces.Count(x => x.Locked);
    public int OnBoardCount => Pieces.Count(x => x.OnBoard);

    public void Normalize()
    {
        if (GameId == Guid.Empty) GameId = Guid.NewGuid();
        MotifId = JigsawCatalog.Motif(MotifId).Id;
        JigsawSize size = JigsawCatalog.Size(PieceCount);
        PieceCount = size.Count;
        double tileW = JigsawGame.BoardWidth / size.Columns;
        double tileH = JigsawGame.BoardHeight / size.Rows;
        Dictionary<int, JigsawPieceState> valid = new();
        foreach (JigsawPieceState? piece in Pieces ?? [])
        {
            if (piece is null || piece.Id < 0 || piece.Id >= PieceCount ||
                !double.IsFinite(piece.X) || !double.IsFinite(piece.Y) ||
                Math.Abs(piece.X) > 5000 || Math.Abs(piece.Y) > 5000) continue;
            valid.TryAdd(piece.Id, piece);
        }
        Pieces = Enumerable.Range(0, PieceCount).Select(id =>
        {
            if (!valid.TryGetValue(id, out JigsawPieceState? original))
                return new JigsawPieceState { Id = id };
            int col = id % size.Columns;
            int row = id / size.Columns;
            bool locked = original.OnBoard && original.Locked &&
                Math.Abs(original.X - col * tileW) < .01 &&
                Math.Abs(original.Y - row * tileH) < .01;
            return new JigsawPieceState
            {
                Id = id, X = locked ? col * tileW : original.X,
                Y = locked ? row * tileH : original.Y,
                OnBoard = original.OnBoard, Locked = locked,
            };
        }).ToList();
        ElapsedSeconds = Math.Clamp(ElapsedSeconds, 0, 20_000_000);
        if (!Completed) CompletedAt = null;
        else CompletedAt ??= DateTimeOffset.UtcNow;
    }
}

public sealed record JigsawCompletionRecord(
    Guid GameId, string MotifId, int PieceCount, long ElapsedSeconds, DateTimeOffset CompletedAt);

public sealed class JigsawProgressState
{
    public const int Schema = 1;
    public int SchemaVersion { get; set; } = Schema;
    public string SelectedMotifId { get; set; } = JigsawCatalog.Motifs[0].Id;
    public int SelectedPieceCount { get; set; } = 96;
    public JigsawGameState? ActiveGame { get; set; }
    public List<JigsawCompletionRecord> CompletedGames { get; set; } = [];

    public void Normalize()
    {
        if (SchemaVersion > Schema)
            throw new NotSupportedException("Jigsaw save was created by a newer version.");
        SchemaVersion = Schema;
        SelectedMotifId = JigsawCatalog.Motif(SelectedMotifId).Id;
        SelectedPieceCount = JigsawCatalog.Size(SelectedPieceCount).Count;
        CompletedGames = (CompletedGames ?? [])
            .Where(c => c is not null && c.GameId != Guid.Empty &&
                        JigsawCatalog.Motifs.Any(m => m.Id == c.MotifId) &&
                        JigsawCatalog.Sizes.Any(s => s.Count == c.PieceCount))
            .GroupBy(c => c.GameId)
            .Select(g => g.First())
            .OrderBy(c => c.CompletedAt)
            .TakeLast(2000).ToList();
        ActiveGame?.Normalize();
        if (ActiveGame?.Completed == true)
        {
            JigsawGameState game = ActiveGame;
            if (!CompletedGames.Any(c => c.GameId == game.GameId))
                CompletedGames.Add(new(game.GameId, game.MotifId, game.PieceCount, game.ElapsedSeconds,
                    game.CompletedAt ?? DateTimeOffset.UtcNow));
            ActiveGame = null;
        }
    }
}

public sealed class JigsawStatistics
{
    public HashSet<Guid> RecordedGameIds { get; set; } = [];
    public long CompletedGames { get; set; }
    public long PiecesCompleted { get; set; }
    public int LargestPuzzle { get; set; }

    public void Normalize()
    {
        RecordedGameIds ??= [];
        RecordedGameIds.Remove(Guid.Empty);
        CompletedGames = Math.Max(0, CompletedGames);
        PiecesCompleted = Math.Max(0, PiecesCompleted);
        LargestPuzzle = Math.Max(0, LargestPuzzle);
    }

    public bool Record(JigsawCompletionRecord completion)
    {
        Normalize();
        if (!RecordedGameIds.Add(completion.GameId)) return false;
        CompletedGames = checked(CompletedGames + 1);
        PiecesCompleted = checked(PiecesCompleted + completion.PieceCount);
        LargestPuzzle = Math.Max(LargestPuzzle, completion.PieceCount);
        return true;
    }
}

public sealed class JigsawProgression(ProgressionService progression)
{
    public static long PlayerExperience(int pieces) => pieces switch
    {
        24 => 100, 48 => 160, 96 => 260, 150 => 400,
        300 => 700, 500 => 1100, 750 => 1600, 1000 => 2200,
        _ => throw new ArgumentOutOfRangeException(nameof(pieces)),
    };

    public ProgressionAward Process(JigsawCompletionRecord completion) =>
        LoungeProgression.GrantOnce(progression,
            () => progression.State.Jigsaw.Record(completion),
            PlayerExperience(completion.PieceCount),
            PlayerExperience(completion.PieceCount) / 2);
}

public static class JigsawGame
{
    // Stable puzzle-coordinate space, independent of window size and zoom.
    public const double BoardWidth = 960;
    public const double BoardHeight = 600;

    public static JigsawGameState Start(string motifId, int count)
    {
        var game = new JigsawGameState
        {
            GameId = Guid.NewGuid(),
            MotifId = JigsawCatalog.Motif(motifId).Id,
            PieceCount = JigsawCatalog.Size(count).Count,
        };
        game.Normalize();
        return game;
    }

    public static (double X, double Y) Target(JigsawGameState game, int id)
    {
        JigsawSize size = JigsawCatalog.Size(game.PieceCount);
        return ((id % size.Columns) * BoardWidth / size.Columns,
            (id / size.Columns) * BoardHeight / size.Rows);
    }

    public static bool Place(JigsawGameState game, int id, double x, double y)
    {
        if (game.Completed || id < 0 || id >= game.PieceCount || !double.IsFinite(x) || !double.IsFinite(y))
            return false;
        JigsawPieceState piece = game.Pieces[id];
        if (piece.OnBoard) return false;
        piece.OnBoard = true;
        piece.X = Math.Clamp(x, -400, BoardWidth + 400);
        piece.Y = Math.Clamp(y, -400, BoardHeight + 400);
        return true;
    }

    public static IReadOnlyList<int> Connected(JigsawGameState game, int id)
    {
        if (id < 0 || id >= game.PieceCount || !game.Pieces[id].OnBoard) return [];
        JigsawSize size = JigsawCatalog.Size(game.PieceCount);
        double w = BoardWidth / size.Columns, h = BoardHeight / size.Rows;
        HashSet<int> seen = [id];
        var queue = new Queue<int>();
        queue.Enqueue(id);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int row = current / size.Columns, col = current % size.Columns;
            foreach (int other in new[]
            {
                col > 0 ? current - 1 : -1, col + 1 < size.Columns ? current + 1 : -1,
                row > 0 ? current - size.Columns : -1,
                row + 1 < size.Rows ? current + size.Columns : -1,
            })
            {
                if (other < 0 || seen.Contains(other) || !game.Pieces[other].OnBoard) continue;
                JigsawPieceState a = game.Pieces[current], b = game.Pieces[other];
                if (Math.Abs((b.X - a.X) - (other % size.Columns - col) * w) > .02 ||
                    Math.Abs((b.Y - a.Y) - (other / size.Columns - row) * h) > .02) continue;
                seen.Add(other);
                queue.Enqueue(other);
            }
        }
        return seen.ToArray();
    }

    public static bool Move(JigsawGameState game, int id, double dx, double dy)
    {
        if (game.Completed || !double.IsFinite(dx) || !double.IsFinite(dy)) return false;
        IReadOnlyList<int> connected = Connected(game, id);
        if (connected.Count == 0 || connected.Any(i => game.Pieces[i].Locked)) return false;
        if (connected.Any(i => Math.Abs(game.Pieces[i].X + dx) > 4000 ||
                               Math.Abs(game.Pieces[i].Y + dy) > 4000)) return false;
        foreach (int i in connected) { game.Pieces[i].X += dx; game.Pieces[i].Y += dy; }
        return true;
    }

    // A complete connection aligns all of a group's pieces by one identical translation.
    // Groups can merge away from the board; touching a locked group anchors the entire group.
    public static bool Snap(JigsawGameState game, int id)
    {
        IReadOnlyList<int> group = Connected(game, id);
        if (group.Count == 0) return game.Completed;
        // An unlocked piece exactly touching an anchored group has already joined it.
        // Anchor the whole connected component instead of refusing the drop.
        if (group.Any(i => game.Pieces[i].Locked))
        {
            foreach (int i in group)
            {
                (game.Pieces[i].X, game.Pieces[i].Y) = Target(game, i);
                game.Pieces[i].Locked = true;
            }
            if (game.Completed) game.CompletedAt ??= DateTimeOffset.UtcNow;
            return game.Completed;
        }
        JigsawSize size = JigsawCatalog.Size(game.PieceCount);
        double w = BoardWidth / size.Columns, h = BoardHeight / size.Rows;
        double radius = Math.Min(w, h) * .36;

        bool TryAnchor(IReadOnlyList<int> ids)
        {
            double best = radius * radius;
            double shiftX = 0, shiftY = 0;
            bool found = false;
            foreach (int i in ids)
            {
                (double x, double y) = Target(game, i);
                double dx = x - game.Pieces[i].X, dy = y - game.Pieces[i].Y;
                double distance = dx * dx + dy * dy;
                if (distance > best) continue;
                best = distance;
                shiftX = dx; shiftY = dy; found = true;
            }
            if (!found) return false;
            foreach (int i in ids)
            {
                JigsawPieceState piece = game.Pieces[i];
                (piece.X, piece.Y) = Target(game, i);
                piece.Locked = true;
            }
            return true;
        }

        if (!TryAnchor(group))
        {
            HashSet<int> selected = group.ToHashSet();
            double best = radius * radius, shiftX = 0, shiftY = 0;
            bool joined = false, anchor = false;
            foreach (int i in group)
            {
                int row = i / size.Columns, col = i % size.Columns;
                foreach (int other in new[]
                {
                    col > 0 ? i - 1 : -1, col + 1 < size.Columns ? i + 1 : -1,
                    row > 0 ? i - size.Columns : -1,
                    row + 1 < size.Rows ? i + size.Columns : -1,
                })
                {
                    if (other < 0 || selected.Contains(other) || !game.Pieces[other].OnBoard) continue;
                    JigsawPieceState nearby = game.Pieces[other];
                    double dx = nearby.X + (col - other % size.Columns) * w - game.Pieces[i].X;
                    double dy = nearby.Y + (row - other / size.Columns) * h - game.Pieces[i].Y;
                    double distance = dx * dx + dy * dy;
                    if (distance > best) continue;
                    best = distance;
                    shiftX = dx; shiftY = dy;
                    anchor = nearby.Locked;
                    joined = true;
                }
            }
            if (joined)
            {
                foreach (int i in group)
                {
                    game.Pieces[i].X += shiftX;
                    game.Pieces[i].Y += shiftY;
                }
                if (anchor)
                {
                    foreach (int i in group)
                    {
                        (game.Pieces[i].X, game.Pieces[i].Y) = Target(game, i);
                        game.Pieces[i].Locked = true;
                    }
                }
                else TryAnchor(Connected(game, id));
            }
        }
        if (game.Completed) game.CompletedAt ??= DateTimeOffset.UtcNow;
        return game.Completed;
    }
}
