using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Tests;

public sealed class WordSearchChallengeTests
{
    // The expanded 288-puzzle offline catalog keeps previous milestone IDs and raises reachable endgame goals.
    [Fact]
    public void CatalogUsesReachableWordSearchGoalsAndNeverRewardsSpeed()
    {
        ChallengeDefinition[] challenges = ChallengeCatalog.For(ChallengeCategory.WordSearch).ToArray();
        Assert.Equal(26, challenges.Length);
        Assert.Contains(challenges, challenge => challenge.Id == "word-search-first");
        Assert.Contains(challenges, challenge => challenge.Id == "word-search-144" && !challenge.IsEndgame);
        Assert.Contains(challenges, challenge => challenge.Id == "word-search-288" && challenge.IsEndgame);
        Assert.Contains(challenges, challenge => challenge.Id == "word-search-words-2736" && challenge.IsEndgame);
        Assert.All(challenges, challenge => Assert.True(challenge.CoinReward > 0));

        var maxima = new Dictionary<ChallengeMetric, decimal>
        {
            [ChallengeMetric.WordSearchCompleted] = WordSearchCatalog.All.Count,
            [ChallengeMetric.WordSearchWordsFound] = WordSearchCatalog.All.Sum(x => x.WordCount),
            [ChallengeMetric.WordSearchNoHintCompleted] = WordSearchCatalog.All.Count,
            [ChallengeMetric.WordSearchExpertCompleted] = WordSearchCatalog.For(BlackjackCasino.Core.WordSearch.WordSearchDifficulty.Expert).Count,
            [ChallengeMetric.WordSearchMasterCompleted] = WordSearchCatalog.For(BlackjackCasino.Core.WordSearch.WordSearchDifficulty.Master).Count,
        };

        Assert.All(challenges, challenge =>
        {
            Assert.True(maxima.TryGetValue(challenge.Metric, out decimal maximum));
            Assert.InRange(challenge.Goal, 1, maximum);
        });
    }

    [Fact]
    public void ProgressReadsExistingLifetimeWordSearchStatisticsRetroactively()
    {
        var profile = new PlayerProfileState();
        profile.WordSearch.CompletedPuzzles = 37;
        profile.WordSearch.WordsFound = 321;
        profile.WordSearch.NoHintCompletions = 18;
        profile.WordSearch.ExpertCompleted = 7;
        profile.WordSearch.MasterCompleted = 3;

        Assert.Equal(37, Progress(profile, ChallengeMetric.WordSearchCompleted));
        Assert.Equal(321, Progress(profile, ChallengeMetric.WordSearchWordsFound));
        Assert.Equal(18, Progress(profile, ChallengeMetric.WordSearchNoHintCompleted));
        Assert.Equal(7, Progress(profile, ChallengeMetric.WordSearchExpertCompleted));
        Assert.Equal(3, Progress(profile, ChallengeMetric.WordSearchMasterCompleted));
    }

    [Fact]
    public void FirstPuzzleChallengeCanBeClaimedOnceAndLifetimePlaysIncludesWordSearch()
    {
        var profile = new PlayerProfileState();
        profile.WordSearch.CompletedPuzzles = 1;
        var service = new ProgressionService(profile);

        ChallengeClaimResult claim = service.ClaimChallenge("word-search-first");

        Assert.True(claim.Success);
        Assert.Equal(50, claim.CoinsEarned);
        Assert.False(service.ClaimChallenge("word-search-first").Success);
        ChallengeDefinition lifetime = ChallengeCatalog.All.Single(x => x.Metric == ChallengeMetric.LifetimePlays);
        Assert.Equal(1m, ChallengeCatalog.Progress(profile, lifetime));
    }

    private static decimal Progress(PlayerProfileState profile, ChallengeMetric metric)
    {
        var challenge = new ChallengeDefinition("test", ChallengeCategory.WordSearch, "Test", "Test", metric, 1, 1);
        return ChallengeCatalog.Progress(profile, challenge);
    }
}
