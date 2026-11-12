using BlackjackCasino.Core;
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
        Assert.Equal(ManorRunStatus.CashedOut,state.Run!.Status);
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

    [Fact]
    public void SavedClassicRunCanResumeThroughModernCommandsAndReopenWithoutDuplicatePay()
    {
        // The legacy UI and modern bridge both operate on this exact Manor journal.
        var profile=new PlayerProfileState();
        var preferences=new AppPreferences { Bankroll=500m,ManorBet=25m };
        var state=new ManorSessionState
        {
            Bankroll=500m,Bet=25m,PaytableVersion=ManorRules.CurrentPaytableVersion,
            CycleActive=true,CycleId=Guid.NewGuid(),Run=ManorRun.Create(3,2).State,
        };
        var store=new ManorPersistence(folder);
        Assert.Null(store.Commit(new ManorCheckpoint(1,true,preferences,profile,state)));
        using var session=new PlayerProgressionController(profile);
        var controller=new ManorController(preferences,500m,session,_=>{},store);
        var modern=new ManorModernGameService(controller);
        Assert.NotNull(modern.Snapshot().Run);
        Assert.Equal(0,modern.Snapshot().Run!.PlayerRoom);
        var firstCommand=new ManorPreviewProtocol.Command("run.move","legacy-room-step",0,Room:1);
        var first=modern.Execute(firstCommand);
        Assert.Equal("1",first.Revision);
        Assert.Equal(1,first.Run!.PlayerRoom);
        Assert.Equal(500m,controller.State.Bankroll);
        Assert.Equal(1,controller.State.Statistics.InteractiveRuns);

        var reopenedCheckpoint=store.Load()!;
        using var reopenedProgression=new PlayerProgressionController(reopenedCheckpoint.Profile);
        var reopened=new ManorController(reopenedCheckpoint.Preferences,reopenedCheckpoint.Machine.Bankroll,
            reopenedProgression,_=>{},store);
        var continuation=new ManorModernGameService(reopened);
        Assert.Equal(first.Revision,continuation.Execute(firstCommand).Revision);
        Assert.Equal(500m,reopened.State.Bankroll);
        Assert.Equal(1,reopened.State.Statistics.InteractiveRuns);

        var exit=continuation.Execute(new ManorPreviewProtocol.Command("run.cashout","leave-manor",1));
        Assert.Null(exit.Run);
        Assert.Equal("2",exit.Revision);
        Assert.True(reopened.State.RunPaid);
        Assert.False(reopened.State.CycleActive);
        Assert.Equal(1,reopened.State.Statistics.CashOuts);
        decimal settledBalance=reopened.State.Bankroll;
        Assert.True(settledBalance>500m);
        Assert.Equal(0,reopened.State.Statistics.PaidSpins);
        var finalCheckpoint=store.Load()!;
        using var lastProgression=new PlayerProgressionController(finalCheckpoint.Profile);
        var classicResume=new ManorController(finalCheckpoint.Preferences,
            finalCheckpoint.Machine.Bankroll,lastProgression,_=>{},store);
        Assert.Equal(settledBalance,classicResume.State.Bankroll);
        Assert.Equal("leave-manor",classicResume.State.LastModernCommandId);
        Assert.Equal(2,classicResume.State.Revision);
        Assert.Equal(1,classicResume.State.Statistics.CashOuts);
        Assert.True(classicResume.State.RunPaid);
    }

    [Fact]
    public void SavedOldTableFreeSpinsResolveInModernAndStaySettledAfterClassicReopen()
    {
        var profile=new PlayerProfileState();
        var preferences=new AppPreferences { Bankroll=500m,ManorBet=25m };
        var state=new ManorSessionState
        {
            Bankroll=500m,Bet=25m,CycleActive=true,
            PaytableVersion=1,FreeSpinsRemaining=1,FreeSpinsAwarded=8,
            CycleId=Guid.NewGuid()
        };
        var store=new ManorPersistence(folder);
        Assert.Null(store.Commit(new ManorCheckpoint(1,true,preferences,profile,state)));
        using var progression=new PlayerProgressionController(profile);
        var modernController=new ManorController(preferences,500m,progression,_=>{},store,_=>0);
        var service=new ManorModernGameService(modernController);
        Assert.Equal(1,service.Snapshot().Rules.Version);
        Assert.True(service.Snapshot().CanFreeSpin);
        var command=new ManorPreviewProtocol.Command("spin.free","old-table-free",0);
        var finished=service.Execute(command);
        Assert.Equal("1",finished.Revision);
        Assert.Equal(0,finished.FreeSpinsRemaining);
        Assert.True(modernController.State.LastSpin!.Free);
        Assert.Equal(1,modernController.State.LastSpin.PaytableVersion);
        Assert.Equal(0,modernController.State.Statistics.PaidSpins);
        Assert.Equal(1,modernController.State.Statistics.FreeSpins);
        Assert.Equal(0,modernController.State.Statistics.TotalWagered);
        Assert.False(modernController.State.CycleActive);
        decimal balance=modernController.State.Bankroll;
        Assert.Equal(500m+modernController.State.LastSpin.Payout,balance);

        var saved=store.Load()!;
        using var afterProgression=new PlayerProgressionController(saved.Profile);
        var classic=new ManorController(saved.Preferences,saved.Machine.Bankroll,
            afterProgression,_=>{},store,_=>0);
        Assert.Equal(balance,classic.State.Bankroll);
        Assert.Equal(1,classic.State.Statistics.FreeSpins);
        Assert.Equal(1,classic.State.LastSpin!.PaytableVersion);
        var replay=new ManorModernGameService(classic).Execute(command);
        Assert.Equal(finished.Revision,replay.Revision);
        Assert.Equal(balance,classic.State.Bankroll);
        Assert.Equal(1,classic.State.Statistics.FreeSpins);
        Assert.Equal(1,classic.State.Revision);
    }

    [Fact]
    public void PracticeUsesSameGhostAndItemEngineButNeverMutatesBankrollOrJournal()
    {
        var prefs=new AppPreferences { Bankroll=750m,ManorBet=25m };
        using var progression=new PlayerProgressionController(new PlayerProfileState());
        var persistence=new ManorPersistence(folder);
        var controller=new ManorController(prefs,750m,progression,_=>{},persistence,_=>0);
        var service=new ManorModernGameService(controller);
        var start=new ManorPreviewProtocol.Command("practice.start","practice-open",0);
        var first=service.Execute(start);
        Assert.Null(first.Run);
        Assert.NotNull(first.PracticeRun);
        Assert.Equal(0,first.PracticeRun!.PlayerRoom);
        Assert.Equal("0",first.Revision);
        Assert.Null(persistence.Load()); // Not even a checkpoint is written.

        var step=service.Execute(new ManorPreviewProtocol.Command(
            "practice.move","practice-first",0,Room:1));
        Assert.Equal(1,step.PracticeRun!.PlayerRoom);
        Assert.True(step.PracticeRun.HasKey); // Same C# key pickup as real runs.
        Assert.Contains("Key found",step.PracticeRun.Message);
        Assert.Equal("1",step.PracticeRun.Turn.ToString());
        Assert.Equal("0",step.Revision);
        Assert.Equal(750m,controller.State.Bankroll);

        var library=service.Execute(new ManorPreviewProtocol.Command(
            "practice.move","practice-library",0,Room:4));
        Assert.True(library.PracticeRun!.HasCandle);
        Assert.True(library.PracticeRun.Banked.EndsWith("×",StringComparison.Ordinal));
        var candle=service.Execute(new ManorPreviewProtocol.Command(
            "practice.candle","practice-candle",0));
        Assert.False(candle.PracticeRun!.HasCandle);
        Assert.StartsWith("Candle reveals:",candle.PracticeRun.GhostClue);
        Assert.Equal(library.PracticeRun.Turn,candle.PracticeRun.Turn); // No time advance.

        var leave=service.Execute(new ManorPreviewProtocol.Command("practice.exit","practice-exit",0));
        Assert.Null(leave.PracticeRun);
        Assert.Equal(750m,controller.State.Bankroll);
        Assert.Equal(0,controller.State.Revision);
        Assert.Equal(0,controller.State.Statistics.PaidSpins);
        Assert.Null(controller.State.Run);
        Assert.Null(persistence.Load());
        Assert.Throws<InvalidOperationException>(()=>service.Execute(
            new ManorPreviewProtocol.Command("practice.move","invalid-after-exit",0,Room:1)));
    }

    [Fact]
    public void PracticeCannotRunDuringAnUnresolvedMoneyCycle()
    {
        var prefs=new AppPreferences { Bankroll=500m,ManorBet=25m };
        using var progression=new PlayerProgressionController(new PlayerProfileState());
        var store=new ManorPersistence(folder);
        var controller=new ManorController(prefs,500m,progression,_=>{},store,_=>0);
        controller.State.CycleActive=true;
        var service=new ManorModernGameService(controller);
        Assert.Throws<InvalidOperationException>(()=>service.Execute(
            new ManorPreviewProtocol.Command("practice.start","cycle-practice",0)));
        Assert.Equal(500m,controller.State.Bankroll);
        Assert.Null(service.Snapshot().PracticeRun);
    }

    public void Dispose()
    {
        if(Directory.Exists(folder)) Directory.Delete(folder,true);
    }
}
