using System.Security.Cryptography;

namespace BlackjackCasino.Core.Manor;

public enum ManorSymbol { Jack, Queen, King, Ace, Candelabrum, Watch, Raven, Key, Portrait, Rose, Lady, Wild, Scatter, Bonus }
public sealed record ManorSymbolDefinition(ManorSymbol Symbol, string Name, int Frequency, int Three, int Four, int Five);
public sealed record ManorLineWin(int Line, ManorSymbol Symbol, int Count, decimal Amount);
public sealed record ManorSpin(int[] Stops, ManorSymbol[] Cells, ManorLineWin[] Wins, decimal Payout, int Scatters, int Doors, bool Free);

public static class ManorRules
{
    public const int StopCount=64;
    public const int MaximumCycleMultiplier=148_600;
    public const int BigWinMultiplier=25;
    public static IReadOnlyList<ManorSymbolDefinition> Symbols { get; } = Array.AsReadOnly(new ManorSymbolDefinition[]
    {
        new(ManorSymbol.Jack,"Victorian J",10,18,48,120), new(ManorSymbol.Queen,"Victorian Q",9,24,60,150),
        new(ManorSymbol.King,"Victorian K",8,30,72,180), new(ManorSymbol.Ace,"Victorian A",7,36,90,240),
        new(ManorSymbol.Candelabrum,"Candelabrum",6,48,120,360), new(ManorSymbol.Watch,"Pocket watch",5,60,150,480),
        new(ManorSymbol.Raven,"Raven",4,90,240,720), new(ManorSymbol.Key,"Antique key",3,108,300,900),
        new(ManorSymbol.Portrait,"Haunted portrait",3,120,360,1200), new(ManorSymbol.Rose,"Black rose",3,150,450,1500),
        new(ManorSymbol.Lady,"Lady of the Manor",2,240,900,3600), new(ManorSymbol.Wild,"Haunted mirror / Wild",1,0,0,0),
        new(ManorSymbol.Scatter,"Manor moon / Scatter",2,0,0,0), new(ManorSymbol.Bonus,"Manor door / Bonus",1,0,0,0),
    });
    private static readonly int[][] Lines="11111 00000 22222 01210 21012 00100 22122 11011 11211 01110 21112 10001 12221 01010 21212 10101 12121 02020 20202 02120"
        .Split(' ').Select(s=>s.Select(c=>c-'0').ToArray()).ToArray();
    public static IReadOnlyList<IReadOnlyList<int>> Paylines { get; } = Array.AsReadOnly(Lines.Select(x=>(IReadOnlyList<int>)Array.AsReadOnly(x)).ToArray());
    private static readonly ManorSymbol[] Strip=BuildStrip();

    private static ManorSymbol[] BuildStrip()
    {
        var strip=new ManorSymbol[64];
        var special=new Dictionary<int,ManorSymbol> { [9]=ManorSymbol.Scatter,[31]=ManorSymbol.Bonus,[49]=ManorSymbol.Scatter,[60]=ManorSymbol.Wild };
        var regular=Enumerable.Range(0,10).SelectMany(i=>Symbols.Take(11).Where(d=>i<d.Frequency).Select(d=>d.Symbol)).GetEnumerator();
        for(int i=0;i<64;i++)
        {
            if(special.TryGetValue(i,out ManorSymbol symbol)) strip[i]=symbol;
            else { if(!regular.MoveNext()) throw new InvalidOperationException("Invalid authored strip."); strip[i]=regular.Current; }
        }
        return strip;
    }

    public static ManorSymbol SymbolAt(int reel,int stop,int row)
    {
        if(reel is <0 or >=5 || stop is <0 or >=64 || row is <0 or >=3) throw new ArgumentOutOfRangeException(nameof(stop));
        return Strip[(stop+row+reel*11)%64];
    }

    public static (ManorSymbol Symbol,int Count,int Credits) EvaluateLine(IReadOnlyList<ManorSymbol> symbols)
    {
        if(symbols.Count!=5 || symbols.Any(s=>!Enum.IsDefined(s))) throw new ArgumentException("A line contains five known symbols.");
        (ManorSymbol Symbol,int Count,int Credits) best=default;
        foreach(ManorSymbolDefinition definition in Symbols.Take(11))
        {
            int count=0;
            while(count<5 && (symbols[count]==definition.Symbol || symbols[count]==ManorSymbol.Wild)) count++;
            int credits=count==5 ? definition.Five : count==4 ? definition.Four : count==3 ? definition.Three : 0;
            if(credits>best.Credits) best=(definition.Symbol,count,credits);
        }
        return best;
    }

    public static ManorSpin Evaluate(int[] stops,decimal bet,bool free=false)
    {
        ValidateBet(bet);
        if(stops.Length!=5) throw new ArgumentException("Five stops required.",nameof(stops));
        var cells=new ManorSymbol[15];
        for(int reel=0;reel<5;reel++)
        for(int row=0;row<3;row++) cells[reel*3+row]=SymbolAt(reel,stops[reel],row);
        var wins=new List<ManorLineWin>();
        for(int line=0;line<20;line++)
        {
            var result=EvaluateLine(Enumerable.Range(0,5).Select(r=>cells[r*3+Lines[line][r]]).ToArray());
            if(result.Credits>0) wins.Add(new(line,result.Symbol,result.Count,bet/20*result.Credits*(free ? 2 : 1)));
        }
        return new(stops.ToArray(),cells,wins.ToArray(),wins.Sum(w=>w.Amount),
            cells.Count(s=>s==ManorSymbol.Scatter),cells.Count(s=>s==ManorSymbol.Bonus),free);
    }

    public static int FreeSpinCount(int scatters) => scatters>=5 ? 20 : scatters==4 ? 12 : scatters==3 ? 8 : 0;
    public static void ValidateBet(decimal bet)
    {
        if(bet<SlotMachineRules.MinimumBet || bet>SlotMachineRules.MaximumBet || decimal.Truncate(bet)!=bet)
            throw new ArgumentOutOfRangeException(nameof(bet),"Manor stakes are whole play-money dollars from $1 to $1,000.");
    }
    public static int Draw(Func<int,int>? rng,int max)
    {
        int value=(rng ?? RandomNumberGenerator.GetInt32)(max);
        if(value<0 || value>=max) throw new ArgumentOutOfRangeException(nameof(rng),"Random source returned an invalid stop.");
        return value;
    }
}
