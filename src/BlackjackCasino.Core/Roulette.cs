using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public sealed record RouletteBet(string Key, decimal Amount);
public sealed record RouletteResult(int Number, decimal Stake, decimal Return, decimal Net, decimal Bankroll);

/// <summary>European single-zero roulette. All monetary decisions are made by the host.</summary>
public static class Roulette
{
    public const decimal Minimum = 100m;
    public const decimal Maximum = 100_000m;
    public static readonly int[] Wheel = { 0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26 };
    private static readonly HashSet<int> Red = new() {1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36};
    public static int Draw() => RandomNumberGenerator.GetInt32(37);
    public static bool IsRed(int n) => Red.Contains(n);
    private static int[] Numbers(string key)
    {
        if (key == "red") return Red.ToArray();
        if (key == "black") return Enumerable.Range(1,36).Where(n=>!Red.Contains(n)).ToArray();
        if (key == "even") return Enumerable.Range(1,36).Where(n=>n%2==0).ToArray();
        if (key == "odd") return Enumerable.Range(1,36).Where(n=>n%2==1).ToArray();
        if (key == "low") return Enumerable.Range(1,18).ToArray();
        if (key == "high") return Enumerable.Range(19,18).ToArray();
        if (key.Length == 2 && key[1] is >= '1' and <= '3')
        {
            int i=key[1]-'1';
            if (key[0]=='d') return Enumerable.Range(1+i*12,12).ToArray();
            if (key[0]=='c') return Enumerable.Range(0,12).Select(n=>1+i+n*3).ToArray();
        }
        if (key.Length>1 && key[0]=='n' && int.TryParse(key[1..], out int number) && number is >=0 and <=36 && key=="n"+number)
            return new[]{number};
        // Inside bets use a canonical comma-separated set; only adjacent layout bets qualify.
        if (key.Length>2 && key[0]=='i')
        {
            var parts=key[1..].Split(',');
            if(parts.Length is 2 or 3 or 4 or 6)
            {
                var ns=new List<int>();
                foreach(var part in parts)
                {
                    if(!int.TryParse(part,out int n)||n<0||n>36||part!=n.ToString()||ns.Contains(n)) return Array.Empty<int>();
                    ns.Add(n);
                }
                ns.Sort();
                if(key!="i"+string.Join(",",ns))return Array.Empty<int>();
                int a=ns[0];
                bool ok=ns.Count switch {
                    2 => a==0 ? ns[1]<=3 : (ns[1]==a+3 || (a%3!=0&&ns[1]==a+1)),
                    3 => a==0 ? (ns.SequenceEqual(new[]{0,1,2})||ns.SequenceEqual(new[]{0,2,3})) : a%3==1&&ns.SequenceEqual(new[]{a,a+1,a+2}),
                    4 => a==0 ? ns.SequenceEqual(new[]{0,1,2,3}) : a%3!=0&&ns.SequenceEqual(new[]{a,a+1,a+3,a+4}),
                    6 => a>0&&a%3==1&&ns.SequenceEqual(Enumerable.Range(a,6)),
                    _=>false
                };
                if(ok)return ns.ToArray();
            }
        }
        return Array.Empty<int>();
    }
    public static RouletteResult Settle(IReadOnlyList<RouletteBet> bets, decimal bankroll, int number)
    {
        if(number<0||number>36||bets.Count<1||bets.Count>100)throw new ArgumentException("Invalid roulette round.");
        decimal stake=0, returned=0;
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var bet in bets)
        {
            if(bet.Key is null||bet.Key.Length>32||!seen.Add(bet.Key)||bet.Amount<Minimum||bet.Amount>Maximum||decimal.Truncate(bet.Amount)!=bet.Amount)
                throw new ArgumentException("Invalid roulette bet.");
            int[] ns=Numbers(bet.Key);
            if(ns.Length==0)throw new ArgumentException("Unknown roulette bet.");
            stake+=bet.Amount;
            if(ns.Contains(number))returned+=bet.Amount*(36/ns.Length);
        }
        if(stake>Maximum||stake>bankroll)throw new ArgumentException("Insufficient bankroll or table limit exceeded.");
        return new(number,stake,returned,returned-stake,checked(bankroll-stake+returned));
    }
}
