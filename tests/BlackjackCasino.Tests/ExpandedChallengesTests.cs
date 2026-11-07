using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Tests;

public sealed class ExpandedChallengesTests
{
    [Fact]
    public void CatalogAddsSolitaireLifetimeAndPrestigeMilestonesWithoutChangingExistingIds()
    {
        var ids = ChallengeCatalog.All.Select(x => x.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.Matches("^[a-z0-9-]+$", id));
        Assert.True(ChallengeCatalog.For(ChallengeCategory.Solitaire).Count >= 20);
        Assert.True(ChallengeCatalog.For(ChallengeCategory.Lifetime).Count >= 15);
        Assert.True(ChallengeCatalog.For(ChallengeCategory.Prestige).Count >= 8);
        Assert.All(ChallengeCatalog.All, c =>
        {
            Assert.True(c.Goal > 0);
            Assert.True(c.CoinReward >= 0);
        });

        // Previously published IDs cannot be renamed or silently deleted.
        Assert.NotNull(ChallengeCatalog.TryGet("sudoku-master-1"));
        Assert.NotNull(ChallengeCatalog.TryGet("prestige-3"));
        Assert.NotNull(ChallengeCatalog.TryGet("lifetime-hours-250"));
        Assert.NotNull(ChallengeCatalog.TryGet("bj-rounds-10"));
        Assert.NotNull(ChallengeCatalog.TryGet("jigsaw-total-1"));
        Assert.True(ChallengeCatalog.TryGet("solitaire-1000")!.IsEndgame);
        Assert.True(ChallengeCatalog.TryGet("prestige-25")!.IsEndgame);
    }

    [Fact]
    public void SolitaireWinsUnlockChallengesThroughAuthoritativeCompletionReceiptsOnly()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        var solitaire = new SolitaireProgression(service);
        Guid game = Guid.NewGuid();
        var completion = new SolitaireCompletionRecord(
            game, SolitaireVariant.Spider, 4, 43, 1800, 230);

        Assert.False(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-first")!));
        Assert.False(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-spider-four")!));
        Assert.Equal(750, solitaire.ProcessWin(completion).PlayerExperience);
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-first")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-spider-four")!));
        Assert.Equal(1, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("solitaire-spider-four")!));
        Assert.False(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-spider-four-10")!));
        Assert.False(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-variants-3")!));
        Assert.Equal(ProgressionAward.None, solitaire.ProcessWin(completion));
        Assert.Equal(1, profile.Solitaire.Wins);

        ChallengeClaimResult claim = service.ClaimChallenge("solitaire-spider-four");
        Assert.True(claim.Success);
        Assert.Equal(300, claim.CoinsEarned);
        Assert.False(service.ClaimChallenge("solitaire-spider-four").Success);

        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(
            JsonSerializer.Serialize(profile))!;
        var restoredService = new ProgressionService(restored);
        Assert.False(restoredService.ClaimChallenge("solitaire-spider-four").Success);
        Assert.True(ChallengeCatalog.Complete(restored, ChallengeCatalog.TryGet("solitaire-first")!));
        Assert.Equal(ProgressionAward.None, new SolitaireProgression(restoredService).ProcessWin(completion));
    }

    [Fact]
    public void SolitaireVariantMasteryRequiresAllSixVariantsNotEverySpiderDifficulty()
    {
        var profile = new PlayerProfileState();
        var stats = profile.Solitaire;
        stats.KlondikeWins = 1;
        stats.FreeCellWins = 1;
        stats.SpiderFourSuitWins = 1;
        stats.SpiderWins = 1;
        stats.PyramidWins = 1;
        stats.GolfWins = 1;
        Assert.Equal(5, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("solitaire-variants-6")!));
        stats.TriPeaksWins = 1;
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-variants-6")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("solitaire-spider-four")!));
    }

    [Fact]
    public void LifetimeLoungeAndCombinedPlaysCountEveryGameWithoutMultiplyingSameReceipt()
    {
        var profile = new PlayerProfileState();
        profile.Sudoku.CompletedPuzzles = 11;
        profile.Solitaire.Wins = 5;
        profile.Solitaire.GamesStarted = 12;
        profile.WordSearch.CompletedPuzzles = 3;
        profile.Memory.CompletedGames = 4;
        profile.Jigsaw.CompletedGames = 2;
        profile.MidnightMerge.OrdersCompleted = 7;
        profile.Adventure.RunsCompleted = 8;
        profile.LifetimeStatistics.RoundsPlayed = 9;

        ChallengeDefinition lounge = ChallengeCatalog.TryGet("lifetime-lounge-25")!;
        Assert.True(ChallengeCatalog.Complete(profile, lounge));
        Assert.Equal(25, ChallengeCatalog.Progress(profile, lounge));
        Assert.Equal(49, ChallengeCatalog.Progress(
            profile, ChallengeCatalog.TryGet("lifetime-plays-1000")!));
        Assert.Equal(8, ChallengeCatalog.Progress(
            profile, ChallengeCatalog.TryGet("lifetime-adventure-25")!));
    }

    [Fact]
    public void LifetimeAndPrestigeMilestonesAreRetroactiveAndSurviveReloadAndRebirth()
    {
        var profile = new PlayerProfileState
        {
            Prestige = 2,
            LifetimeExperience = 110_000,
            TotalPlaytimeSeconds = 50L * 3600,
        };
        var service = new ProgressionService(profile);
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("prestige-1")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("prestige-2")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("lifetime-xp-100000")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("lifetime-hours-50")!));

        ChallengeClaimAllResult rewards = service.ClaimAllChallenges();
        Assert.True(rewards.Success);
        Assert.Contains(rewards.Challenges, challenge => challenge.Id == "prestige-2");
        Assert.Contains(rewards.Challenges, challenge => challenge.Id == "lifetime-xp-100000");

        var restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        var again = new ProgressionService(restored);
        Assert.False(again.ClaimChallenge("prestige-1").Success);
        Assert.False(again.ClaimChallenge("prestige-2").Success);
        Assert.True(ChallengeCatalog.Complete(restored, ChallengeCatalog.TryGet("lifetime-hours-50")!));
    }
}
