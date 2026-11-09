using BlackjackCasino.Core.TowerDefense;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class TowerDefensePhaseOneTests
{
    private static TowerDefenseGame NewGame()
    {
        var game = new TowerDefenseGame();
        Assert.True(game.Execute("tower.play", kind: "forest").Success);
        return game;
    }

    [Fact]
    public void LegacyCompletedMapKeepsVictoryAndReceivesBackfilledMasteryExperience()
    {
        var old = new TdState { Screen = "menu", Stage = "forest", Phase = "won", Wave = 15 };
        var game = new TowerDefenseGame(old);
        Assert.Contains("forest", game.State.Progress.CompletedStages);
        Assert.Equal(1, game.State.Progress.Victories);
        Assert.Equal(15, game.State.Progress.WavesCleared);
        Assert.True(game.State.Progress.Level >= 3);
        Assert.True(game.Execute("tower.play", kind: "frost").Success);
        var again = new TowerDefenseGame(game.Save());
        Assert.Equal(15, again.State.Progress.WavesCleared);
        Assert.Equal(1, again.State.Progress.Victories);
    }

    [Fact]
    public void MapsAreDistinctAndRetainTheirIndividualDefences()
    {
        var progress = new TdProgress { Level = 3 };
        var game = new TowerDefenseGame(new TdState { Progress = progress });
        Assert.Equal("menu", game.State.Screen);
        Assert.True(game.Execute("tower.play", kind: "forest").Success);
        Assert.True(game.Execute("tower.build", 2, "archer").Success);
        Assert.True(game.Execute("tower.menu").Success);
        Assert.True(game.Execute("tower.play", kind: "frost").Success);
        Assert.Equal("frost", game.State.Stage);
        Assert.Equal(20, TdMastery.StageWaves(game.State.Stage));
        Assert.Empty(game.State.Towers);
        Assert.NotEqual(TdMap.Path, TdMap.FrostPath);
        Assert.NotEqual(TdMap.Pads, TdMap.FrostPads);
        Assert.True(game.Execute("tower.build", 5, "frost").Success);
        Assert.True(game.Execute("tower.menu").Success);
        Assert.True(game.Execute("tower.play", kind: "forest").Success);
        Assert.Equal("forest", game.State.Stage);
        Assert.Single(game.State.Towers);
        Assert.Equal("archer", game.State.Towers[0].Kind);
        var restored = new TowerDefenseGame(game.Save());
        Assert.Single(restored.State.Towers);
        Assert.True(restored.Execute("tower.menu").Success);
        Assert.True(restored.Execute("tower.play", kind: "frost").Success);
        Assert.Equal("frost", restored.State.Stage);
        Assert.Equal("frost", Assert.Single(restored.State.Towers).Kind);
    }

    [Fact]
    public void MasterySpendsOnePointPerLevelAndUnlocksMapsAndTowers()
    {
        var game = new TowerDefenseGame();
        Assert.False(game.Execute("tower.play", kind: "frost").Success);
        Assert.True(game.Execute("tower.play", kind: "forest").Success);
        Assert.False(game.Execute("tower.build", 0, "ballista").Success);
        Assert.True(game.Execute("tower.menu").Success);
        Assert.True(game.Execute("tower.skills").Success);
        Assert.False(game.Execute("tower.skill.buy", kind: "capital").Success);
        var levelled = game.Save();
        levelled.Progress.Level = 4;
        var resumed = new TowerDefenseGame(levelled);
        Assert.Equal(3, resumed.State.Progress.PointsAvailable);
        Assert.True(resumed.Execute("tower.skill.buy", kind: "capital").Success);
        Assert.Equal(2, resumed.State.Progress.PointsAvailable);
        Assert.True(resumed.Execute("tower.skill.buy", kind: "last-stand").Success);
        Assert.True(resumed.Execute("tower.skill.buy", kind: "scouting").Success);
        Assert.False(resumed.Execute("tower.skill.buy", kind: "focus").Success);
        Assert.True(resumed.Execute("tower.skill.reset").Success);
        Assert.Equal(3, resumed.State.Progress.PointsAvailable);
        Assert.True(resumed.Execute("tower.menu").Success);
        Assert.True(resumed.Execute("tower.play", kind: "frost").Success);
        Assert.True(resumed.Execute("tower.build", 0, "ballista").Success);
        Assert.True(resumed.Execute("tower.build", 1, "flame").Success);
    }

    [Fact]
    public void DiscountsAndRefundsCanNeverGenerateCoins()
    {
        var state = new TdState { Screen = "game",
            Progress = new TdProgress { Level = 11, Skills = new()
            {
                ["engineer"] = 5, ["refund"] = 5,
            } },
        };
        var game = new TowerDefenseGame(state);
        int originalCoins = game.State.Coins;
        Assert.True(game.Execute("tower.build", 0, "archer").Success);
        Assert.True(game.Execute("tower.upgrade", 0).Success);
        // Retaining actual build expenditure prevents profit after changing discounts.
        Assert.True(game.Execute("tower.menu").Success);
        Assert.True(game.Execute("tower.skills").Success);
        Assert.True(game.Execute("tower.skill.reset").Success);
        Assert.True(game.Execute("tower.menu").Success);
        Assert.True(game.Execute("tower.play", kind: "forest").Success);
        Assert.True(game.Execute("tower.sell", 0).Success);
        Assert.True(game.State.Coins <= originalCoins);
        Assert.True(game.Execute("tower.build", 0, "cannon").Success);
        Assert.True(game.Execute("tower.sell", 0).Success);
        Assert.True(game.State.Coins <= originalCoins);
    }

    [Fact]
    public void LastStandSavesExactlyOneFatalEscapePerRunAndSurvivesSave()
    {
        var state = new TdState
        {
            Screen = "game", Phase = "running", Wave = 1, Lives = 1,
            Spawned = 1, SpawnTotal = 1, NextEnemyId = 2,
            Progress = new TdProgress { Level = 2, Skills = new() { ["last-stand"] = 1 } },
            Enemies = [new TdEnemy(1, "grunt", TdMap.PathLength - 1, 50, 0)],
        };
        var game = new TowerDefenseGame(state);
        Assert.Equal("running", game.State.Phase);
        Assert.True(game.State.Paused);
        Assert.True(game.Execute("tower.pause").Success);
        Assert.True(game.Tick());
        Assert.Equal(1, game.State.Lives);
        Assert.True(game.State.LastStandUsed);
        var restored = new TowerDefenseGame(game.Save());
        Assert.True(restored.State.LastStandUsed);
        restored.State.Phase = "running";
        restored.State.Enemies.Add(new TdEnemy(2, "grunt", TdMap.PathLength - 1, 50, 0));
        restored.State.NextEnemyId = 3;
        restored.State.Spawned = 2;
        restored.State.SpawnTotal = 2;
        Assert.True(restored.Execute("tower.pause").Success);
        restored.Tick();
        Assert.Equal("lost", restored.State.Phase);
        Assert.Equal(1, restored.State.Progress.Defeats);
    }

    [Fact]
    public void CampaignVictoryCanContinueToEndlessWithoutReplayingEarlyWaves()
    {
        var saved = new TdState { Screen = "game", Phase = "won", Wave = 15,
            Progress = new TdProgress { Level = 4 } };
        var game = new TowerDefenseGame(saved);
        Assert.True(game.Execute("tower.endless").Success);
        Assert.Equal("planning", game.State.Phase);
        Assert.True(game.State.Endless);
        Assert.True(game.Execute("tower.start").Success);
        Assert.Equal(16, game.State.Wave);
    }

    [Fact]
    public void BastionMilestonesArePartOfTheGlobalChallengeCatalog()
    {
        var profile = new BlackjackCasino.Core.PlayerProfileState();
        profile.TowerDefense.WavesCleared = 12;
        profile.TowerDefense.Victories = 1;
        profile.TowerDefense.EnemiesDefeated = 250;
        Assert.Equal(12, BlackjackCasino.Core.ChallengeCatalog.Progress(profile,
            BlackjackCasino.Core.ChallengeCatalog.TryGet("bastion-waves-10")!));
        Assert.Equal(1, BlackjackCasino.Core.ChallengeCatalog.Progress(profile,
            BlackjackCasino.Core.ChallengeCatalog.TryGet("bastion-first-win")!));
        Assert.True(BlackjackCasino.Core.ChallengeCatalog.For(BlackjackCasino.Core.ChallengeCategory.TowerDefense).Count >= 10);
    }

    [Fact]
    public void StartBuildUpgradeAndSellAreBoundedAndCannotCreateMoney()
    {
        var game = NewGame();
        Assert.Equal(320, game.State.Coins);
        Assert.True(game.Execute("tower.build", 0, "archer").Success);
        Assert.Equal(250, game.State.Coins);
        Assert.False(game.Execute("tower.build", 0, "cannon").Success);
        Assert.False(game.Execute("tower.build", 999, "archer").Success);
        Assert.False(game.Execute("tower.build", 1, "not-a-tower").Success);
        Assert.True(game.Execute("tower.upgrade", 0).Success);
        Assert.Equal(180, game.State.Coins);
        Assert.True(game.Execute("tower.sell", 0).Success);
        Assert.Equal(264, game.State.Coins); // 60% of 70 + 70 spent
        Assert.Empty(game.State.Towers);
        Assert.False(game.Execute("tower.sell", 0).Success);
    }

    [Fact]
    public void WavesHaveManualStartPauseAndFiniteFifteenWaveCap()
    {
        var game = NewGame();
        Assert.False(game.Tick());
        Assert.True(game.Execute("tower.start").Success);
        Assert.Equal(1, game.State.Wave);
        Assert.False(game.Execute("tower.start").Success);
        Assert.True(game.Tick());
        Assert.NotEmpty(game.State.Enemies);
        long tick = game.State.Ticks;
        Assert.True(game.Execute("tower.pause").Success);
        Assert.False(game.Tick());
        Assert.Equal(tick, game.State.Ticks);
        Assert.True(game.Execute("tower.pause").Success);
        for (int i = 0; i < 800 && game.State.Phase == "running"; i++) game.Tick();
        Assert.Equal("planning", game.State.Phase);
        Assert.Equal(11, game.State.Lives); // Nine enemies passed on wave 1.
        Assert.True(game.Execute("tower.start").Success);
        Assert.Equal(2, game.State.Wave);
    }

    [Fact]
    public void SavedActiveBattleResumesPausedAndRejectsCorruptInputs()
    {
        var game = NewGame();
        Assert.True(game.Execute("tower.build", 4, "frost").Success);
        Assert.True(game.Execute("tower.start").Success);
        for (int i = 0; i < 20; i++) game.Tick();
        TdState saved = game.Save();
        var restored = new TowerDefenseGame(saved);
        Assert.True(restored.State.Paused);
        Assert.Equal(saved.Towers.Count, restored.State.Towers.Count);
        Assert.Equal(saved.Enemies.Count, restored.State.Enemies.Count);
        Assert.True(restored.Execute("tower.pause").Success);
        Assert.True(restored.Tick());
        var tampered = saved.Copy();
        tampered.Towers.Add(new TdTower(1000, "cannon", 1, 0));
        Assert.Empty(new TowerDefenseGame(tampered).State.Towers);
    }

    [Fact]
    public void LastSpawnNeverMakesClockNegativeOrInvalidatesTheSave()
    {
        var game = NewGame();
        Assert.True(game.Execute("tower.start").Success);
        for (int i = 0; i < 95; i++) game.Tick();
        Assert.Equal(game.State.SpawnTotal, game.State.Spawned);
        Assert.Equal(0, game.State.SpawnClock);
        Assert.Equal(0, game.State.WaveKills);
        Assert.Equal(0, game.State.WaveEscapes);
        Assert.Equal("running", game.State.Phase);
        var resumed = new TowerDefenseGame(game.Save());
        Assert.Equal(game.State.Wave, resumed.State.Wave);
        Assert.Equal(game.State.Spawned, resumed.State.Spawned);
        Assert.Equal(game.State.Enemies.Count, resumed.State.Enemies.Count);
        Assert.True(resumed.State.Paused);
    }

    [Fact]
    public void LegacyBattleWithNegativeSpawnClockIsRecoveredInsteadOfReset()
    {
        var game = NewGame();
        Assert.True(game.Execute("tower.build", 4, "frost").Success);
        Assert.True(game.Execute("tower.start").Success);
        for (int i = 0; i < 95; i++) game.Tick();
        TdState legacy = game.Save();
        legacy.SpawnClock = -4.5; // valid in an earlier release after the last enemy spawned
        var restored = new TowerDefenseGame(legacy);
        Assert.Equal(1, restored.State.Wave);
        Assert.Equal(0, restored.State.SpawnClock);
        Assert.Single(restored.State.Towers);
        Assert.True(restored.State.Paused);
    }

    [Fact]
    public void OnlyDefeatsIncrementKillCounterAndTheNextWaveResetsIt()
    {
        var game = NewGame();
        Assert.True(game.Execute("tower.build", 0, "archer").Success);
        Assert.True(game.Execute("tower.build", 1, "archer").Success);
        Assert.True(game.Execute("tower.start").Success);
        Assert.True(game.Tick()); // Native shot is not yet a defeat.
        Assert.Equal(0, game.State.WaveKills);
        for (int i = 0; i < 400 && game.State.Phase == "running"; i++)
        {
            game.Tick();
            Assert.Equal(game.State.Spawned - game.State.Enemies.Count,
                game.State.WaveKills + game.State.WaveEscapes);
        }
        Assert.True(game.State.WaveKills > 0);
        TdState checkpoint = game.Save();
        var restored = new TowerDefenseGame(checkpoint);
        Assert.Equal(checkpoint.WaveKills, restored.State.WaveKills);
        Assert.Equal(checkpoint.WaveEscapes, restored.State.WaveEscapes);
        if (game.State.Phase == "planning")
        {
            Assert.True(game.Execute("tower.start").Success);
            Assert.Equal(0, game.State.WaveKills);
            Assert.Equal(0, game.State.WaveEscapes);
        }
    }

    [Fact]
    public void StaleRevisionCannotTrapThePlayerInsideBastion()
    {
        var session = new ModernTowerDefenseSession();
        var newId = Guid.NewGuid().ToString("D");
        var start = new ModernTowerDefenseCommand(newId, session.Id, 0, "tower.start", null, null);
        Assert.True(session.Execute(start, _ => (true, "started")).Success);
        var oldRevision = new ModernTowerDefenseCommand(Guid.NewGuid().ToString("D"),
            session.Id, 0, "tower.pause", null, null);
        Assert.False(session.Execute(oldRevision, _ => (true, "paused")).Success);
        var exit = oldRevision with
        {
            RequestId = Guid.NewGuid().ToString("D"),
            Action = "tower.exit",
        };
        Assert.True(session.Execute(exit, _ => (true, "saved")).Success);
    }

    [Fact]
    public void NativeBridgeRejectsMalformedCommandsAndIsIdempotent()
    {
        var session = new ModernTowerDefenseSession();
        string id = Guid.NewGuid().ToString("D");
        string payload = $$"""{"type":"lounge.tower-defense.command","version":1,"requestId":"{{id}}","session":"{{session.Id}}","expectedRevision":0,"action":"tower.build","pad":1,"kind":"archer"}""";
        ModernTowerDefenseCommand? command = ModernTowerDefenseProtocol.Parse(payload);
        Assert.NotNull(command);
        Assert.Null(ModernTowerDefenseProtocol.Parse(payload.Replace("\"pad\":1", "\"pad\":999")));
        Assert.Null(ModernTowerDefenseProtocol.Parse(payload.Replace("\"kind\":\"archer\"", "\"kind\":\"unknown\"")));
        int calls = 0;
        ModernTowerDefenseReply Commit(ModernTowerDefenseCommand c) =>
            session.Execute(c, _ => { calls++; return (true, "saved"); });
        ModernTowerDefenseReply first = Commit(command!);
        Assert.True(first.Success);
        Assert.Equal(first, Commit(command!));
        Assert.Equal(1, calls);
        Assert.Equal(1, session.Revision);
        Assert.False(Commit(command! with { RequestId = Guid.NewGuid().ToString("D") }).Success);
    }

    [Fact]
    public void MapDistanceAlwaysEndsAtTheGate()
    {
        var beginning = TdMap.Position(0);
        Assert.Equal(0, beginning.X);
        TdPoint end = TdMap.Position(TdMap.PathLength);
        Assert.Equal(960, end.X);
        Assert.Equal(325, end.Y);
    }
}
