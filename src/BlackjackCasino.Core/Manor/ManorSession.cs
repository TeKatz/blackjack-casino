namespace BlackjackCasino.Core.Manor;

public sealed class ManorStatistics
{
    public long PaidSpins { get; set; }
    public long FreeSpins { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalReturned { get; set; }
    public decimal NetProfit => TotalReturned-TotalWagered;
    public decimal BiggestWin { get; set; }
    public long BigWins { get; set; }
    public long FreeSpinsTriggered { get; set; }
    public long ManorBonuses { get; set; }
    public long InteractiveRuns { get; set; }
    public long AutomaticRuns { get; set; }
    public long Exits { get; set; }
    public long MaximumExits { get; set; }
    public long Catches { get; set; }
    public long CashOuts { get; set; }
    public decimal BestManorMultiplier { get; set; }
    public Guid LastAwardedCycle { get; set; }
}

public sealed class ManorSessionState
{
    // Old JSON has no version; pending Free Spins keep its original payout table.
    public int PaytableVersion { get; set; }=1;
    public decimal Bankroll { get; set; }
    public decimal Bet { get; set; }=25;
    public Guid CycleId { get; set; }
    public decimal CycleReturned { get; set; }
    public bool CycleActive { get; set; }
    public bool BonusTriggered { get; set; }
    public int FreeSpinsRemaining { get; set; }
    public int FreeSpinsAwarded { get; set; }
    public ManorSpin? LastSpin { get; set; }
    public ManorRunState? Run { get; set; }
    public bool InteractiveRunCounted { get; set; }
    public bool RunPaid { get; set; }
    public ManorStatistics Statistics { get; set; }=new();
}

/// <summary>Every method is a synchronous financial/state transition, independent of presentation timers.</summary>
public sealed class ManorSession(ManorSessionState state,Func<int,int>? rng=null)
{
    public ManorSessionState State { get; }=state;
    public bool HasRun => State.Run is { Status:ManorRunStatus.Hunting };
    public bool HasFreeSpin => State.CycleActive && !HasRun && State.FreeSpinsRemaining>0;

    public ManorSpin Start(decimal bet)
    {
        if(State.CycleActive) throw new InvalidOperationException("Resolve the current feature before wagering again.");
        ManorRules.ValidateBet(bet);
        if(bet>State.Bankroll) throw new InvalidOperationException("Insufficient play-money bankroll.");
        // Prove enough decimal headroom for every permitted future feature before debiting.
        if(State.Bankroll>decimal.MaxValue-bet*ManorRules.MaximumCycleMultiplier) throw new OverflowException("Bankroll exceeds the supported range.");
        ManorSpin result=Draw(bet,false,ManorRules.CurrentPaytableVersion);
        ManorRunState? run=result.Doors>=2 ? ManorRun.Create(result.Doors,ManorRules.Draw(rng,16)).State : null;
        State.Bet=bet;
        State.PaytableVersion=ManorRules.CurrentPaytableVersion;
        State.CycleId=Guid.NewGuid();
        State.CycleReturned=0;
        State.CycleActive=true;
        State.BonusTriggered=run is not null;
        State.FreeSpinsRemaining=ManorRules.FreeSpinCount(result.Scatters);
        State.FreeSpinsAwarded=State.FreeSpinsRemaining;
        State.Run=run;
        State.RunPaid=false;
        State.InteractiveRunCounted=false;
        State.Bankroll-=bet;
        State.Statistics.PaidSpins++;
        State.Statistics.TotalWagered+=bet;
        State.Statistics.FreeSpinsTriggered+=State.FreeSpinsRemaining>0 ? 1 : 0;
        State.Statistics.ManorBonuses+=run is not null ? 1 : 0;
        Apply(result);
        return result;
    }

    public ManorSpin SpinFree()
    {
        if(!HasFreeSpin) throw new InvalidOperationException("No free spin is currently available.");
        ManorSpin spin=Draw(State.Bet,true,State.PaytableVersion);
        State.FreeSpinsRemaining--;
        State.Statistics.FreeSpins++;
        Apply(spin);
        return spin;
    }

    public void Move(int room) { RequireRun().Move(room); FinishRunIfNeeded(); }
    public void UseCandle() => RequireRun().UseCandle();
    public void CashOut() { RequireRun().CashOut(); FinishRunIfNeeded(); }
    public void FollowPolicy() { RequireRun().FollowPolicy(); FinishRunIfNeeded(); }
    public void MarkInteractive()
    {
        _=RequireRun();
        if(!State.InteractiveRunCounted) { State.Statistics.InteractiveRuns++; State.InteractiveRunCounted=true; }
    }
    public void ResolveAutomatically()
    {
        ManorRun run=RequireRun();
        if(!State.InteractiveRunCounted) State.Statistics.AutomaticRuns++;
        run.AutoResolve();
        FinishRunIfNeeded();
    }

    private ManorRun RequireRun() => HasRun ? new(State.Run!) : throw new InvalidOperationException("No active Manor run.");
    private ManorSpin Draw(decimal bet,bool free,int version) => ManorRules.Evaluate(Enumerable.Range(0,5).Select(_=>ManorRules.Draw(rng,64)).ToArray(),bet,free,version);
    private void Apply(ManorSpin spin)
    {
        State.LastSpin=spin;
        Credit(spin.Payout);
        FinishCycleIfNeeded();
    }
    private void Credit(decimal amount)
    {
        if(amount<0 || State.CycleReturned+amount>State.Bet*ManorRules.MaximumCycleMultiplier)
            throw new InvalidOperationException("Cycle payout exceeds the documented safety bound.");
        State.Bankroll=checked(State.Bankroll+amount);
        State.CycleReturned+=amount;
        State.Statistics.TotalReturned+=amount;
    }
    private void FinishRunIfNeeded()
    {
        if(State.Run is not { Status:not ManorRunStatus.Hunting } run || State.RunPaid) return;
        Credit(State.Bet*run.PaidMultiplier);
        State.RunPaid=true;
        ManorStatistics stats=State.Statistics;
        stats.BestManorMultiplier=Math.Max(stats.BestManorMultiplier,run.PaidMultiplier);
        stats.Exits+=run.Status==ManorRunStatus.Escaped ? 1 : 0;
        stats.MaximumExits+=run.Status==ManorRunStatus.Escaped && run.ExitTarget==1000 ? 1 : 0;
        stats.Catches+=run.Status==ManorRunStatus.Caught ? 1 : 0;
        stats.CashOuts+=run.Status==ManorRunStatus.CashedOut ? 1 : 0;
        FinishCycleIfNeeded();
    }
    private void FinishCycleIfNeeded()
    {
        if(HasRun || State.FreeSpinsRemaining>0) return;
        State.CycleActive=false;
        State.Statistics.BiggestWin=Math.Max(State.Statistics.BiggestWin,State.CycleReturned);
        State.Statistics.BigWins+=State.CycleReturned>=State.Bet*ManorRules.BigWinMultiplier ? 1 : 0;
    }
}

public static class ManorProgression
{
    public static ProgressionAward Award(ProgressionService progression,ManorSessionState state)
    {
        if(state.CycleActive || state.CycleId==Guid.Empty || state.Statistics.LastAwardedCycle==state.CycleId) return ProgressionAward.None;
        long player=20+(state.CycleReturned>state.Bet ? 5 : 0);
        long season=15+(state.CycleReturned>state.Bet ? 5 : 0);
        ProgressionAward p=progression.GrantPlayerExperience(player);
        ProgressionAward s=progression.GrantSeasonExperience(progression.State.ActiveSeasonId,season);
        state.Statistics.LastAwardedCycle=state.CycleId;
        progression.State.LastPlayedAt=DateTimeOffset.UtcNow;
        return new(player,season,p.LevelsGained,s.SeasonTiersGained,p.CoinsEarned+s.CoinsEarned,
            p.CosmeticsUnlocked.Concat(s.CosmeticsUnlocked).ToArray());
    }
}
