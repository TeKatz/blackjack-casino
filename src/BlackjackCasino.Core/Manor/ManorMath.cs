namespace BlackjackCasino.Core.Manor;

public sealed record ManorMathReport(decimal BaseReturn,decimal FreeSpinProbability,decimal BonusProbability,
    decimal ExpectedFreeSpins,decimal BonusReturn,decimal ReferenceReturn,decimal[] RunAverages);

public static class ManorMath
{
    public static ManorMathReport Calculate()
    {
        long weighted=0;
        var line=new ManorSymbol[5];
        void Visit(int reel,long weight)
        {
            if(reel==5) { weighted+=weight*ManorRules.EvaluateLine(line).Credits; return; }
            foreach(ManorSymbolDefinition symbol in ManorRules.Symbols)
            {
                line[reel]=symbol.Symbol;
                Visit(reel+1,weight*symbol.Frequency);
            }
        }
        Visit(0,1);
        decimal denominator=Pow(64,5);
        decimal baseReturn=weighted/denominator;
        decimal freeProbability=0,bonusProbability=0,freeCount=0,bonusReturn=0;
        var runAverages=new decimal[4];
        for(int n=2;n<=5;n++)
        {
            decimal f=n>=3 ? Choose(n)*Pow(6,n)*Pow(58,5-n)/denominator : 0;
            decimal b=Choose(n)*Pow(3,n)*Pow(61,5-n)/denominator;
            freeProbability+=f;
            bonusProbability+=b;
            freeCount+=f*ManorRules.FreeSpinCount(n);
            decimal sum=0;
            for(int seed=0;seed<16;seed++)
            {
                ManorRun run=ManorRun.Create(n,seed);
                run.AutoResolve();
                sum+=run.State.PaidMultiplier;
            }
            runAverages[n-2]=sum/16m;
            bonusReturn+=b*runAverages[n-2];
        }
        return new(baseReturn,freeProbability,bonusProbability,freeCount,bonusReturn,
            baseReturn*(1+2*freeCount)+bonusReturn,runAverages);
    }
    private static decimal Pow(int n,int p) { decimal value=1; for(int i=0;i<p;i++) value*=n; return value; }
    private static int Choose(int n) => n is 2 or 3 ? 10 : n==4 ? 5 : 1;
}
