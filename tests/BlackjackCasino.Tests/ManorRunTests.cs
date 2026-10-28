using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Tests;

public sealed class ManorRunTests
{
    [Fact]
    public void GraphIsConnectedAndEveryPlayerRoomCanReachTheRealGate()
    {
        Assert.Equal(19,ManorMap.Rooms.Count);
        for(int room=0;room<19;room++) if(room!=16) Assert.NotEmpty(ManorMap.Path(room,16));
        for(int room=0;room<16;room++) Assert.NotEmpty(ManorMap.Path(room,16,key:true,player:true));
        Assert.All(ManorMap.Edges,e=> { Assert.InRange(e.A,0,18); Assert.InRange(e.B,0,18); Assert.NotEqual(e.A,e.B); });
    }

    [Fact]
    public void CaughtPaysCheckpointAndVoluntaryCashoutChangesActualReward()
    {
        ManorRun caught=ManorRun.Create(3,9); caught.AutoResolve();
        Assert.Equal(ManorRunStatus.Caught,caught.State.Status); Assert.Equal(20,caught.State.Payout);
        ManorRun cash=ManorRun.Create(3,2); cash.CashOut(); Assert.Equal(5,cash.State.Payout);
        ManorRun deeper=ManorRun.Create(3,2); deeper.AutoResolve(); Assert.Equal(250,deeper.State.Payout);
        Assert.Throws<InvalidOperationException>(()=>cash.Move(1));
        Assert.Throws<InvalidOperationException>(()=>cash.CashOut());
    }

    [Fact]
    public void RevisitedRoomsCannotFarmRewardsAndLockedRoutesNeedAKey()
    {
        ManorRun run=ManorRun.Create(3,0); run.Move(4); int reward=run.State.Current;
        run.Move(0); run.Move(4); Assert.Equal(reward,run.State.Current);
        var locked=new ManorRun(new ManorRunState {Player=6,Ghost=17});
        Assert.Throws<InvalidOperationException>(()=>locked.Move(11));
        locked.State.HasKey=true; locked.Move(11); Assert.Equal(11,locked.State.Player); Assert.True(locked.State.Haunt>=3);
        Assert.Throws<InvalidOperationException>(()=>ManorRun.Create(3,0).Move(16));
    }

    [Fact]
    public void CandleRevealsWithoutAdvancingTimeAndCharmRepelsAlongAnEdge()
    {
        ManorRun run=ManorRun.Create(3,0); run.State.HasCandle=true; run.UseCandle();
        Assert.Equal(0,run.State.Turn); Assert.Contains("reveals",run.GhostClue);
        Assert.Throws<InvalidOperationException>(()=>run.UseCandle());
        var charm=new ManorRun(new ManorRunState {Player=0,Ghost=1,HasCharm=true});
        charm.Move(1); Assert.False(charm.Finished); Assert.False(charm.State.HasCharm);
        Assert.Contains(charm.State.Ghost,ManorMap.Adjacent(charm.State.Player));
        Assert.NotEqual(charm.State.Player,charm.State.Ghost);
    }

    [Fact]
    public void GhostMovesAlongReadableShortestEdgesAndRunsAreBounded()
    {
        ManorRun run=ManorRun.Create(3,0);
        int oldGhost=run.State.Ghost;
        run.Move(1); Assert.Equal(oldGhost,run.State.Ghost);
        run.Move(2); Assert.Equal(oldGhost,run.State.Ghost);
        run.Move(3); Assert.Equal(18,run.State.Ghost);
        var random=new Random(16);
        for(int n=0;n<200;n++)
        {
            ManorRun r=ManorRun.Create(3,n%16);
            while(!r.Finished)
            {
                int[] choices=ManorMap.Adjacent(r.State.Player,r.State.HasKey,player:true);
                r.Move(choices[random.Next(choices.Length)]);
                Assert.InRange(r.State.Current,0,r.State.ExitTarget);
                Assert.InRange(r.State.Banked,0,r.State.Current);
                Assert.InRange(r.State.Ghost,0,18);
            }
            Assert.InRange(r.State.Turn,1,24);
        }
    }
}
