namespace BlackjackCasino.Core.Jigsaw;

public enum JigsawAchievementTier { Bronze, Silver, Gold, Platinum }

public sealed record JigsawAchievementDefinition(
    string Id, string Name, string GermanName, string Description, string GermanDescription,
    JigsawAchievementTier Tier, string Metric, int Goal, string? Key = null)
{
    public long CoinReward => Tier switch
    {
        JigsawAchievementTier.Bronze => 100,
        JigsawAchievementTier.Silver => 250,
        JigsawAchievementTier.Gold => 600,
        JigsawAchievementTier.Platinum => 1500,
        _ => 0,
    };

    public long ExperienceReward => Tier switch
    {
        JigsawAchievementTier.Bronze => 70,
        JigsawAchievementTier.Silver => 160,
        JigsawAchievementTier.Gold => 400,
        JigsawAchievementTier.Platinum => 1000,
        _ => 0,
    };

    public long SeasonExperienceReward => ExperienceReward / 2;

    public long Current(JigsawStatistics stats) => Metric switch
    {
        "size" when int.TryParse(Key, out int size) =>
            stats.CompletedBySize.TryGetValue(size, out int count) ? count : 0,
        "total" => stats.CompletedGames,
        "pieces" => stats.PiecesCompleted,
        "scenes" => stats.CompletedByMotif.Count(x => x.Value > 0),
        "motif" => Key is not null && stats.CompletedByMotif.TryGetValue(Key, out int count) ? count : 0,
        _ => 0,
    };

    public bool Earned(JigsawStatistics stats) => Current(stats) >= Goal;
}

public static class JigsawAchievements
{
    // Exactly thirty independent, durable, one-time achievements.
    public static readonly IReadOnlyList<JigsawAchievementDefinition> All =
    [
        new("size-24", "First Steps", "Erste Schritte", "Finish a 24-piece puzzle.", "Schließe ein Puzzle mit 24 Teilen ab.", JigsawAchievementTier.Bronze, "size", 1, "24"),
        new("size-48", "Getting Comfortable", "Gut eingespielt", "Finish a 48-piece puzzle.", "Schließe ein Puzzle mit 48 Teilen ab.", JigsawAchievementTier.Bronze, "size", 1, "48"),
        new("size-96", "Finding the Pattern", "Muster erkannt", "Finish a 96-piece puzzle.", "Schließe ein Puzzle mit 96 Teilen ab.", JigsawAchievementTier.Silver, "size", 1, "96"),
        new("size-150", "Patient Hands", "Geduldige Hände", "Finish a 150-piece puzzle.", "Schließe ein Puzzle mit 150 Teilen ab.", JigsawAchievementTier.Silver, "size", 1, "150"),
        new("size-300", "Serious Puzzler", "Puzzle-Profi", "Finish a 300-piece puzzle.", "Schließe ein Puzzle mit 300 Teilen ab.", JigsawAchievementTier.Gold, "size", 1, "300"),
        new("size-500", "Master of Details", "Meister der Details", "Finish a 500-piece puzzle.", "Schließe ein Puzzle mit 500 Teilen ab.", JigsawAchievementTier.Gold, "size", 1, "500"),
        new("size-750", "Endurance Artist", "Ausdauerkünstler", "Finish a 750-piece puzzle.", "Schließe ein Puzzle mit 750 Teilen ab.", JigsawAchievementTier.Platinum, "size", 1, "750"),
        new("size-1000", "Thousand-Piece Legend", "Tausend-Teile-Legende", "Finish a 1,000-piece puzzle.", "Schließe ein Puzzle mit 1.000 Teilen ab.", JigsawAchievementTier.Platinum, "size", 1, "1000"),
        new("total-1", "First Picture", "Erstes Bild", "Complete your first puzzle.", "Schließe dein erstes Puzzle ab.", JigsawAchievementTier.Bronze, "total", 1, null),
        new("total-3", "A Cozy Habit", "Eine schöne Gewohnheit", "Complete 3 puzzles.", "Schließe 3 Puzzle ab.", JigsawAchievementTier.Bronze, "total", 3, null),
        new("total-5", "Five Finished Frames", "Fünf fertige Bilder", "Complete 5 puzzles.", "Schließe 5 Puzzle ab.", JigsawAchievementTier.Silver, "total", 5, null),
        new("total-10", "Puzzle Weekend", "Puzzle-Wochenende", "Complete 10 puzzles.", "Schließe 10 Puzzle ab.", JigsawAchievementTier.Silver, "total", 10, null),
        new("total-20", "Picture Collector", "Bildersammler", "Complete 20 puzzles.", "Schließe 20 Puzzle ab.", JigsawAchievementTier.Gold, "total", 20, null),
        new("total-35", "Seasoned Solver", "Erfahrener Tüftler", "Complete 35 puzzles.", "Schließe 35 Puzzle ab.", JigsawAchievementTier.Gold, "total", 35, null),
        new("total-50", "Gallery Curator", "Galerie-Kurator", "Complete 50 puzzles.", "Schließe 50 Puzzle ab.", JigsawAchievementTier.Platinum, "total", 50, null),
        new("total-100", "Hundred Masterpieces", "Hundert Meisterwerke", "Complete 100 puzzles.", "Schließe 100 Puzzle ab.", JigsawAchievementTier.Platinum, "total", 100, null),
        new("pieces-100", "Piece by Piece", "Stück für Stück", "Place 100 pieces in completed puzzles.", "Lege insgesamt 100 Teile in abgeschlossenen Puzzles.", JigsawAchievementTier.Bronze, "pieces", 100, null),
        new("pieces-500", "A Little More Patience", "Noch etwas Geduld", "Place 500 pieces in completed puzzles.", "Lege insgesamt 500 Teile.", JigsawAchievementTier.Silver, "pieces", 500, null),
        new("pieces-1500", "Putting It Together", "Alles fügt sich", "Place 1,500 pieces in completed puzzles.", "Lege insgesamt 1.500 Teile.", JigsawAchievementTier.Silver, "pieces", 1500, null),
        new("pieces-3000", "Eye for Detail", "Blick fürs Detail", "Place 3,000 pieces in completed puzzles.", "Lege insgesamt 3.000 Teile.", JigsawAchievementTier.Gold, "pieces", 3000, null),
        new("pieces-7500", "Steady Hands", "Ruhige Hände", "Place 7,500 pieces in completed puzzles.", "Lege insgesamt 7.500 Teile.", JigsawAchievementTier.Gold, "pieces", 7500, null),
        new("pieces-15000", "The Great Mosaic", "Das große Mosaik", "Place 15,000 pieces in completed puzzles.", "Lege insgesamt 15.000 Teile.", JigsawAchievementTier.Platinum, "pieces", 15000, null),
        new("scenes-2", "Two Worlds", "Zwei Welten", "Complete puzzles from 2 different scenes.", "Schließe Puzzle mit 2 verschiedenen Motiven ab.", JigsawAchievementTier.Bronze, "scenes", 2, null),
        new("scenes-4", "Explorer", "Entdecker", "Complete puzzles from 4 different scenes.", "Schließe Puzzle mit 4 verschiedenen Motiven ab.", JigsawAchievementTier.Silver, "scenes", 4, null),
        new("scenes-8", "World Traveler", "Weltenbummler", "Complete puzzles from 8 different scenes.", "Schließe Puzzle mit 8 verschiedenen Motiven ab.", JigsawAchievementTier.Gold, "scenes", 8, null),
        new("scenes-12", "The Whole Collection", "Die ganze Sammlung", "Complete puzzles from all 12 scenes.", "Schließe Puzzle mit allen 12 Motiven ab.", JigsawAchievementTier.Platinum, "scenes", 12, null),
        new("motif-enchanted-forest", "Forest Wanderer", "Waldwanderer", "Complete Enchanted Forest.", "Schließe Verzauberter Wald ab.", JigsawAchievementTier.Bronze, "motif", 1, "enchanted-forest"),
        new("motif-ocean-depths", "Below the Surface", "Unter der Oberfläche", "Complete Ocean Depths.", "Schließe Meerestiefen ab.", JigsawAchievementTier.Silver, "motif", 1, "ocean-depths"),
        new("motif-eternal-casino", "House Artist", "Künstler des Hauses", "Complete Eternal Casino.", "Schließe Ewiges Casino ab.", JigsawAchievementTier.Gold, "motif", 1, "eternal-casino"),
        new("motif-primordial-dragons", "Dragon Tamer", "Drachenzähmer", "Complete Primordial Dragons.", "Schließe Urdrachen ab.", JigsawAchievementTier.Gold, "motif", 1, "primordial-dragons"),
    ];

    public static IReadOnlyList<JigsawAchievementDefinition> NewlyEarned(JigsawStatistics stats) =>
        All.Where(a => a.Earned(stats) && !stats.UnlockedAchievementIds.Contains(a.Id)).ToArray();
}
