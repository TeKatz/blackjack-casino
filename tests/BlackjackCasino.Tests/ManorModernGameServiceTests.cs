using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;
using Xunit;

namespace BlackjackCasino.Tests;

public sealed class ManorModernGameServiceTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"modern-manor-test-"+Guid.NewGuid());

    [Fact]
    public void PaidSpinCommitsOnceAndRetriedRequestIsNotChargedAgain()
    {
        var preferences=new AppPreferences { Bankroll=1_000m,ManorBet=25m };
        using var progression=new PlayerProgressionController(new PlayerProfileState());
        var persistence=new ManorPersistence(folder);
        decimal projectedBankroll=0;
        var controller=new ManorController(preferences,1_000m,progression,
            settings=>projectedBankroll=settings.Bankroll,persistence,_=>0);
        var service=new ManorModernGameService(controller);
        var command=new ManorPreviewProtocol.Command("spin.paid","unique-spin-1",0,25m);

        var first=service.Execute(command);
        decimal balance=controller.State.Bankroll;
        Assert.Equal(balance,projectedBankroll);
        Assert.Equal(1,controller.State.Statistics.PaidSpins);
        Assert.Equal(25m,controller.State.Statistics.TotalWagered);
        Assert.Equal(1,controller.State.Revision);
        Assert.Equal("unique-spin-1",controller.State.LastModernCommandId);

        var repeated=service.Execute(command);
        Assert.Equal(first.Revision,repeated.Revision);
        Assert.Equal(balance,controller.State.Bankroll);
        Assert.Equal(1,controller.State.Statistics.PaidSpins);
        Assert.Throws<InvalidOperationException>(()=>service.Execute(
            new ManorPreviewProtocol.Command("spin.paid","different-id",0,25m)));
        Assert.Equal(1,controller.State.Statistics.PaidSpins);

        ManorCheckpoint saved=persistence.Load()!;
        Assert.False(saved.NeedsRecovery);
        Assert.Equal("unique-spin-1",saved.Machine.LastModernCommandId);
        Assert.Equal(1,saved.Machine.Revision);
        using var resumedProgression=new PlayerProgressionController(saved.Profile);
        var resumed=new ManorController(saved.Preferences,saved.Machine.Bankroll,
            resumedProgression,_=>{},persistence,_=>0);
        var afterRestart=new ManorModernGameService(resumed);
        Assert.Equal(first.Revision,afterRestart.Execute(command).Revision);
        Assert.Equal(1,resumed.State.Statistics.PaidSpins);
        Assert.Equal(balance,resumed.State.Bankroll);
    }

    [Fact]
    public void FrontendCannotAuthorOutcomesOrUseOldRevisions()
    {
        var state=new ManorSessionState { Bankroll=500m,Bet=25m };
        var engine=new ManorSession(state,_=>0);
        Assert.Throws<InvalidOperationException>(()=>ManorModernGameService.Apply(engine,
            new ManorPreviewProtocol.Command("wallet.set","x",0)));
        ManorModernGameService.Apply(engine,new ManorPreviewProtocol.Command("spin.paid","x",0,25m));
        Assert.Equal(1,state.Statistics.PaidSpins);
        Assert.Equal(25m,state.Statistics.TotalWagered);
    }

    [Fact]
    public void RunActionsUseTheSameEngineAndDoNotChargeAdditionalStakes()
    {
        var state=new ManorSessionState
        {
            Bankroll=500m,Bet=25m,CycleActive=true,
            CycleId=Guid.NewGuid(),Run=ManorRun.Create(3,2).State
        };
        var engine=new ManorSession(state);
        ManorModernGameService.Apply(engine,new ManorPreviewProtocol.Command("run.move","first-room",0,Room:1));
        Assert.Equal(1,state.Run!.Player);
        Assert.Equal(1,state.Statistics.InteractiveRuns);
        Assert.Equal(500m,state.Bankroll); // The room decision is not a new wager.

        ManorModernGameService.Apply(engine,new ManorPreviewProtocol.Command("run.cashout","leave-room",0));
        Assert.True(state.Run.Status is ManorRunStatus.CashedOut);
        Assert.False(state.CycleActive);
        Assert.True(state.RunPaid);
        decimal balance=state.Bankroll;
        Assert.True(balance>500m);
        Assert.Throws<InvalidOperationException>(()=>ManorModernGameService.Apply(
            engine,new ManorPreviewProtocol.Command("run.cashout","again",0)));
        Assert.Equal(balance,state.Bankroll);
    }

    [Fact]
    public void FreeSpinConsumesExistingEntitlementWithoutNewStake()
    {
        var state=new ManorSessionState
        {
            Bankroll=250m,Bet=25m,PaytableVersion=1,CycleActive=true,
            CycleId=Guid.NewGuid(),FreeSpinsRemaining=1
        };
        var engine=new ManorSession(state,_=>0);
        ManorModernGameService.Apply(engine,new ManorPreviewProtocol.Command("spin.free","free-1",0));
        Assert.Equal(0,state.FreeSpinsRemaining);
        Assert.Equal(0,state.Statistics.PaidSpins);
        Assert.Equal(1,state.Statistics.FreeSpins);
        Assert.Equal(0,state.Statistics.TotalWagered);
        Assert.Equal(1,state.LastSpin!.PaytableVersion);
        Assert.False(state.CycleActive);
    }

    public void Dispose()
    {
        if(Directory.Exists(folder)) Directory.Delete(folder,true);
    }
}
