using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class KniffelProgressionTests
{
    internal static KniffelState CompletedGame() => new()
    {
        Entries = new[] { 3, 6, 9, 12, 20, 18, 18, 25, 25, 30, 0, 0, 22 }
            .Select((score, i) => new KniffelEntry((KniffelCategory)i, score)).ToArray(),
    };

    [Fact]
    public void CompletedGameAwardsPlayerAndSeasonExperienceWithoutBlackjackStatistics()
    {
        var service = new ProgressionService(new PlayerProfileState());
        ProgressionAward award = new KniffelProgression(service).Process(CompletedGame());
        Assert.Equal(646, award.PlayerExperience);
        Assert.Equal(323, award.SeasonExperience);
        Assert.Equal(646, service.State.LifetimeExperience);
        Assert.NotEmpty(award.LevelsGained);
        Assert.True(award.CoinsEarned > 0);
        Assert.Equal(1, service.State.Kniffel.CompletedGames);
        Assert.Equal(223, service.State.Kniffel.BestScore);
        Assert.Equal(0, service.State.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(0, service.State.Slots.Spins);
    }

    [Fact]
    public void ReceiptSurvivesProfileReloadAndPreventsRepeatAwards()
    {
        KniffelState game = CompletedGame();
        var service = new ProgressionService(new PlayerProfileState());
        var progression = new KniffelProgression(service);
        progression.Process(game);
        Assert.Equal(ProgressionAward.None, progression.Process(game));
        PlayerProfileState reloaded = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(service.State))!;
        Assert.Equal(ProgressionAward.None, new KniffelProgression(new ProgressionService(reloaded)).Process(game));
        Assert.Equal(646, reloaded.LifetimeExperience);
        Assert.Equal(1, reloaded.Kniffel.CompletedGames);
        Assert.Single(reloaded.Kniffel.RewardedGameIds);
    }

    [Fact]
    public void NewGameCanEarnAgainButIncompleteAndRestartedGamesEarnNothing()
    {
        var service = new ProgressionService(new PlayerProfileState());
        var rewards = new KniffelProgression(service);
        Assert.Equal(ProgressionAward.None, rewards.Process(new KniffelGame().Snapshot));
        Assert.Empty(service.State.Kniffel.RewardedGameIds);
        rewards.Process(CompletedGame());
        rewards.Process(CompletedGame());
        Assert.Equal(1292, service.State.LifetimeExperience);
        Assert.Equal(2, service.State.Kniffel.CompletedGames);
    }

    [Fact]
    public void MaxLevelStillAccruesLifetimeAndSeasonExperience()
    {
        var profile = new PlayerProfileState { Level = 100 };
        var service = new ProgressionService(profile);
        ProgressionAward award = new KniffelProgression(service).Process(CompletedGame());
        Assert.Equal(646, profile.LifetimeExperience);
        Assert.Equal(0, profile.LevelExperience);
        Assert.Empty(award.LevelsGained);
        Assert.Equal(323, profile.Seasons[profile.ActiveSeasonId].Experience);
    }

    [Fact]
    public void FailedProfileSaveLeavesRewardRetryableAndNeverMutatesLiveXp()
    {
        bool fail = true;
        PlayerProfileState? disk = null;
        using var controller = new PlayerProgressionController(new PlayerProfileState(), kniffelProfileSaver: profile =>
        {
            if (fail) throw new IOException("Disk unavailable");
            disk = ManorPersistence.Clone(profile);
        });
        KniffelState game = CompletedGame();
        Assert.Throws<IOException>(() => controller.ProcessKniffel(game));
        Assert.Equal(0, controller.Profile.LifetimeExperience);
        Assert.Empty(controller.Profile.Kniffel.RewardedGameIds);
        fail = false;
        Assert.Equal(646, controller.ProcessKniffel(game).PlayerExperience);
        Assert.NotNull(disk);
        Assert.Contains(game.GameId, disk.Kniffel.RewardedGameIds);
        Assert.Equal(646, disk.LifetimeExperience);
        Assert.Equal(ProgressionAward.None, controller.ProcessKniffel(game));
    }

    [Fact]
    public void MigratedGameIdSurvivesCheckpointAndNewGamesGetDifferentIds()
    {
        KniffelGame legacy = KniffelGame.Restore(CompletedGame() with { GameId = Guid.Empty });
        Assert.NotEqual(Guid.Empty, legacy.GameId);
        KniffelGame resumed = KniffelGame.Restore(JsonSerializer.Deserialize<KniffelState>(JsonSerializer.Serialize(legacy.Snapshot))!);
        Assert.Equal(legacy.GameId, resumed.GameId);
        Assert.NotEqual(legacy.GameId, new KniffelGame().GameId);
    }

    [Fact]
    public void OtherGameProfileAdoptionPreservesReceipts()
    {
        using var controller = new PlayerProgressionController(new PlayerProfileState());
        KniffelState game = CompletedGame();
        controller.ProcessKniffel(game);
        controller.AdoptManorProfile(ManorPersistence.Clone(controller.Profile));
        Assert.Equal(ProgressionAward.None, controller.ProcessKniffel(game));
        Assert.Equal(646, controller.Profile.LifetimeExperience);
    }
}
