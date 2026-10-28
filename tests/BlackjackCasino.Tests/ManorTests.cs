using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ManorTests
{
    [Fact]
    public void AuthoredStripsHaveCorrectFrequenciesAndFeatureSpacing()
    {
        Assert.Equal(20,ManorRules.Paylines.Count);
        Assert.Equal(20,ManorRules.Paylines.Select(l=>string.Join("",l)).Distinct().Count());
        foreach(var line in ManorRules.Paylines) Assert.All(line,row=>Assert.InRange(row,0,2));
        for(int reel=0;reel<5;reel++)
        {
            foreach(var definition in ManorRules.Symbols)
                Assert.Equal(definition.Frequency,Enumerable.Range(0,64).Count(stop=>ManorRules.SymbolAt(reel,stop,0)==definition.Symbol));
            for(int stop=0;stop<64;stop++)
            {
                var visible=Enumerable.Range(0,3).Select(row=>ManorRules.SymbolAt(reel,stop,row)).ToArray();
                Assert.InRange(visible.Count(x=>x==ManorSymbol.Scatter),0,1);
                Assert.InRange(visible.Count(x=>x==ManorSymbol.Bonus),0,1);
                Assert.False(visible.Contains(ManorSymbol.Scatter)&&visible.Contains(ManorSymbol.Bonus));
            }
        }
    }

    [Theory]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void EveryRegularSymbolPaysThePublishedLongestLeftRun(int count)
    {
        foreach(var definition in ManorRules.Symbols.Take(11))
        {
            var line=Enumerable.Repeat(definition.Symbol,count).Concat(Enumerable.Repeat(ManorSymbol.Bonus,5-count)).ToArray();
            Assert.Equal(count==3?definition.Three:count==4?definition.Four:definition.Five,ManorRules.EvaluateLine(line).Credits);
            line[0]=ManorSymbol.Wild;
            Assert.Equal(count==3?definition.Three:count==4?definition.Four:definition.Five,ManorRules.EvaluateLine(line).Credits);
        }
        Assert.Equal(2700,ManorRules.EvaluateLine(Enumerable.Repeat(ManorSymbol.Wild,5).ToArray()).Credits);
    }

    [Fact]
    public void WildDoesNotSubstituteFeaturesAndMatchesAreLeftToRight()
    {
        Assert.Equal(0,ManorRules.EvaluateLine([ManorSymbol.Bonus,ManorSymbol.Wild,ManorSymbol.Bonus,ManorSymbol.Bonus,ManorSymbol.Bonus]).Credits);
        Assert.Equal(0,ManorRules.EvaluateLine([ManorSymbol.Scatter,ManorSymbol.Jack,ManorSymbol.Jack,ManorSymbol.Jack,ManorSymbol.Jack]).Credits);
        Assert.Throws<ArgumentException>(()=>ManorRules.EvaluateLine([(ManorSymbol)99,ManorSymbol.Wild,ManorSymbol.Jack,ManorSymbol.Jack,ManorSymbol.Jack]));
    }

    [Fact]
    public void EveryStopAndStakeBoundaryIsValidatedBeforeDebit()
    {
        foreach(decimal bet in new[]{0m,0.9m,1.01m,1001m})
        {
            var state=new ManorSessionState {Bankroll=1_000};
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ManorSession(state,_=>throw new Exception("No draw")).Start(bet));
            Assert.Equal(1_000,state.Bankroll);
        }
        foreach(int invalid in new[]{-1,64})
        {
            var state=new ManorSessionState {Bankroll=1_000};
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ManorSession(state,_=>invalid).Start(25));
            Assert.Equal(1_000,state.Bankroll); Assert.Equal(0,state.Statistics.PaidSpins);
        }
        var tooPoor=new ManorSessionState {Bankroll=2};
        Assert.Throws<InvalidOperationException>(()=>new ManorSession(tooPoor,_=>throw new Exception()).Start(25));
        var overflow=new ManorSessionState {Bankroll=decimal.MaxValue};
        Assert.Throws<OverflowException>(()=>new ManorSession(overflow).Start(1));
    }

    [Theory]
    [InlineData(3,8)] [InlineData(4,12)] [InlineData(5,20)]
    public void FreeSpinsUseOriginalStakeDoubleLinesAndDoNotRetrigger(int scatterCount,int spins)
    {
        int[] stops=FeatureStops(ManorSymbol.Scatter,scatterCount);
        int index=0;
        var state=new ManorSessionState {Bankroll=1_000};
        var machine=new ManorSession(state,_=>stops[index++%5]);
        ManorSpin paid=machine.Start(25);
        Assert.Equal(spins,state.FreeSpinsRemaining);
        Assert.Throws<InvalidOperationException>(()=>machine.Start(25));
        decimal expected=1_000-25+paid.Payout;
        for(int n=0;n<spins;n++)
        {
            ManorSpin free=machine.SpinFree();
            Assert.True(free.Free);
            Assert.Equal(paid.Payout*2,free.Payout);
            expected+=free.Payout;
        }
        Assert.Equal(expected,state.Bankroll);
        Assert.Equal(25,state.Statistics.TotalWagered);
        Assert.Equal(spins,state.Statistics.FreeSpins);
        Assert.Equal(1,state.Statistics.FreeSpinsTriggered);
        Assert.Equal(0,state.Statistics.ManorBonuses);
        Assert.False(state.CycleActive);
        Assert.Throws<InvalidOperationException>(()=>machine.SpinFree());
    }

    [Theory]
    [InlineData(3,250)] [InlineData(4,500)] [InlineData(5,1000)]
    public void ManorTriggerTiersSetActualExitTargetsAndSettleOnce(int doors,int target)
    {
        int[] stops=FeatureStops(ManorSymbol.Bonus,doors);
        int i=0;
        var state=new ManorSessionState {Bankroll=1_000};
        var machine=new ManorSession(state,max=>max==16?2:stops[i++%5]);
        ManorSpin paid=machine.Start(25);
        Assert.True(machine.HasRun); Assert.Equal(target,state.Run!.ExitTarget);
        Assert.Throws<InvalidOperationException>(()=>machine.Start(25));
        machine.ResolveAutomatically();
        Assert.Equal(ManorRunStatus.Escaped,state.Run.Status);
        Assert.Equal(1_000-25+paid.Payout+target*25,state.Bankroll);
        Assert.Equal(1,state.Statistics.Exits);
        Assert.Throws<InvalidOperationException>(()=>machine.ResolveAutomatically());
        Assert.Throws<InvalidOperationException>(()=>machine.CashOut());
    }

    [Fact]
    public void InvalidBonusSeedDoesNotChargeOrCreateAFeature()
    {
        int[] stops=FeatureStops(ManorSymbol.Bonus,3); int i=0;
        var state=new ManorSessionState {Bankroll=100};
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ManorSession(state,max=>max==16?16:stops[i++]).Start(25));
        Assert.Equal(100,state.Bankroll); Assert.False(state.CycleActive); Assert.Null(state.Run);
    }

    [Fact]
    public void SavingDuringFreeSpinsOrManorPreservesResultAndPreventsDuplicateXp()
    {
        int[] stops=FeatureStops(ManorSymbol.Scatter,3); int i=0;
        var state=new ManorSessionState {Bankroll=1_000};
        var machine=new ManorSession(state,_=>stops[i++%5]); machine.Start(5); machine.SpinFree();
        var restored=ManorPersistence.Clone(state);
        var resumed=new ManorSession(restored,_=>0);
        while(resumed.HasFreeSpin) resumed.SpinFree();
        var service=new ProgressionService(new PlayerProfileState());
        Assert.True(ManorProgression.Award(service,restored).PlayerExperience>0);
        long xp=service.State.LifetimeExperience;
        Assert.Equal(ProgressionAward.None,ManorProgression.Award(service,ManorPersistence.Clone(restored)));
        Assert.Equal(xp,service.State.LifetimeExperience);
        Assert.Equal(0,service.State.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(8,restored.Statistics.FreeSpins);
        for(int seed=0;seed<16;seed++)
        {
            ManorRun run=ManorRun.Create(3,seed); run.FollowPolicy();
            var resumedRun=new ManorRun(ManorPersistence.Clone(run.State));
            run.AutoResolve(); resumedRun.AutoResolve();
            Assert.Equal(JsonSerializer.Serialize(run.State),JsonSerializer.Serialize(resumedRun.State));
        }
    }

    [Fact]
    public void FeatureXpIsOncePerPaidCycleAndIndependentOfStake()
    {
        foreach(decimal bet in new[]{1m,25m,1000m})
        {
            int[] stops=FeatureStops(ManorSymbol.Bonus,3); int i=0;
            var state=new ManorSessionState {Bankroll=10_000};
            var machine=new ManorSession(state,max=>max==16?2:stops[i++%5]); machine.Start(bet);
            var service=new ProgressionService(new PlayerProfileState());
            Assert.Equal(ProgressionAward.None,ManorProgression.Award(service,state));
            machine.ResolveAutomatically();
            ProgressionAward award=ManorProgression.Award(service,state);
            Assert.Equal(25,award.PlayerExperience); Assert.Equal(20,award.SeasonExperience);
            Assert.Equal(ProgressionAward.None,ManorProgression.Award(service,state));
        }
    }

    [Fact]
    public void ReferenceMathAndPolicyAreReproducible()
    {
        ManorMathReport report=ManorMath.Calculate();
        Assert.Equal(0.5714641995728016m,decimal.Round(report.BaseReturn,16));
        Assert.InRange(report.ReferenceReturn,0.95022769541m,0.95022769543m);
        Assert.Equal(new[]{10.875m,108.75m,140m,202.5m},report.RunAverages);
        Assert.InRange(report.FreeSpinProbability,0.00712m,0.00713m);
        Assert.InRange(report.BonusProbability,0.019984m,0.019985m);
        for(int n=2;n<=5;n++) for(int seed=0;seed<16;seed++)
        {
            ManorRun manual=ManorRun.Create(n,seed),automatic=ManorRun.Create(n,seed);
            while(!manual.Finished) manual.FollowPolicy();
            automatic.AutoResolve();
            Assert.Equal(JsonSerializer.Serialize(manual.State),JsonSerializer.Serialize(automatic.State));
        }
    }

    [Fact]
    public void SeededSimulationRespectsExposureConservationAndFeatureFrequencies()
    {
        var random=new Random(716033);
        var state=new ManorSessionState {Bankroll=1_000_000};
        var machine=new ManorSession(state,random.Next);
        for(int i=0;i<100_000;i++)
        {
            machine.Start(1);
            while(state.CycleActive)
            {
                if(machine.HasRun) machine.ResolveAutomatically();
                else machine.SpinFree();
            }
            Assert.InRange(state.CycleReturned,0,ManorRules.MaximumCycleMultiplier);
        }
        Assert.Equal(1_000_000-state.Statistics.TotalWagered+state.Statistics.TotalReturned,state.Bankroll);
        Assert.InRange(state.Statistics.ManorBonuses,1800,2200);
        Assert.InRange(state.Statistics.FreeSpinsTriggered,610,820);
        Assert.InRange(state.Statistics.TotalReturned/state.Statistics.TotalWagered,0.75m,1.17m);
    }

    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(9)] [InlineData(15)]
    public void TwoDoorsStartARealScaledRunAndResumeWithoutChangingAwards(int seed)
    {
        int[] stops=FeatureStops(ManorSymbol.Bonus,2); int i=0;
        var state=new ManorSessionState {Bankroll=1_000};
        var session=new ManorSession(state,max=>max==16?seed:stops[i++%5]);
        ManorSpin paid=session.Start(25);
        Assert.True(session.HasRun);
        Assert.Equal(0.1m,state.Run!.PrizeScale);
        session.MarkInteractive(); session.FollowPolicy();
        var saved=ManorPersistence.Clone(state);
        var resumed=new ManorSession(saved);
        session.ResolveAutomatically(); resumed.ResolveAutomatically();
        ManorRun full=ManorRun.Create(3,seed); full.AutoResolve();
        Assert.Equal(full.State.Payout/10m,state.Run.PaidMultiplier);
        Assert.Equal(1_000-25+paid.Payout+25*state.Run.PaidMultiplier,state.Bankroll);
        Assert.Equal(state.Bankroll,saved.Bankroll);
        Assert.Equal(JsonSerializer.Serialize(state),JsonSerializer.Serialize(saved));
        Assert.Equal(1,state.Statistics.ManorBonuses);
        Assert.Throws<InvalidOperationException>(()=>resumed.ResolveAutomatically());
    }

    [Fact]
    public void LegacySavedRunsRetainFullPrizesWhenScaleIsMissing()
    {
        var old=JsonSerializer.Deserialize<ManorRunState>("{\"Seed\":2,\"ExitTarget\":250}")!;
        Assert.Equal(1m,old.PrizeScale);
        var run=new ManorRun(old); run.AutoResolve();
        Assert.Equal(250m,run.State.PaidMultiplier);
    }

    [Fact]
    public void TwoDoorsAndThreeMoonsAwardBothFeaturesWithoutAdditionalWagers()
    {
        int[] stops=Enumerable.Range(0,5).Select(reel=>Enumerable.Range(0,64).First(stop=>
            Enumerable.Range(0,3).Any(row=>ManorRules.SymbolAt(reel,stop,row)==(reel<2 ? ManorSymbol.Bonus : ManorSymbol.Scatter)))).ToArray();
        int i=0;
        var state=new ManorSessionState {Bankroll=1_000};
        var machine=new ManorSession(state,max=>max==16 ? 2 : stops[i++%5]);
        machine.Start(1);
        Assert.True(machine.HasRun); Assert.Equal(8,state.FreeSpinsRemaining);
        machine.ResolveAutomatically();
        Assert.True(state.CycleActive);
        while(machine.HasFreeSpin) machine.SpinFree();
        Assert.False(state.CycleActive);
        Assert.Equal(1,state.Statistics.TotalWagered);
        Assert.Equal(1,state.Statistics.ManorBonuses);
        Assert.Equal(8,state.Statistics.FreeSpins);
        Assert.Equal(999+state.CycleReturned,state.Bankroll);
    }

    internal static int[] FeatureStops(ManorSymbol symbol,int count) => Enumerable.Range(0,5).Select(reel=>
        Enumerable.Range(0,64).First(stop=>
        {
            var cells=Enumerable.Range(0,3).Select(row=>ManorRules.SymbolAt(reel,stop,row)).ToArray();
            return reel<count ? cells.Contains(symbol) : !cells.Contains(ManorSymbol.Bonus)&&!cells.Contains(ManorSymbol.Scatter);
        })).ToArray();
}
