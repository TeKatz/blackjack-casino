using BlackjackCasino.Core.TowerDefense;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class TowerDefensePhaseOneTests
{
    [Fact]
    public void StartBuildUpgradeAndSellAreBoundedAndCannotCreateMoney()
    {
        var game = new TowerDefenseGame();
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
        var game = new TowerDefenseGame();
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
        var game = new TowerDefenseGame();
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
