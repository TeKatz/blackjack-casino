using BlackjackCasino.Core;
namespace BlackjackCasino.Tests;

public sealed class RouletteTests
{
    [Theory]
    [InlineData("n17",17,36)]
    [InlineData("red",1,2)]
    [InlineData("red",0,0)]
    [InlineData("black",0,0)]
    [InlineData("even",0,0)]
    [InlineData("d2",17,3)]
    [InlineData("c2",17,3)]
    [InlineData("i1,2",2,18)]
    [InlineData("i1,2,3",2,12)]
    [InlineData("i1,2,4,5",5,9)]
    [InlineData("i1,2,3,4,5,6",6,6)]
    [InlineData("i0,1,2,3",0,9)]
    public void ReturnsPublishedMultiplier(string key,int number,int multiplier)
    {
        var result=Roulette.Settle(new[]{new RouletteBet(key,100m)},1000m,number);
        Assert.Equal(100m*multiplier,result.Return);
        Assert.Equal(900m+100m*multiplier,result.Bankroll);
        Assert.Equal(result.Return-100m,result.Net);
    }

    [Theory]
    [InlineData("i3,4")]
    [InlineData("i1,5")]
    [InlineData("n37")]
    [InlineData("i1,1")]
    [InlineData("i2,1")]
    public void RejectsInvalidBets(string key)
    {
        Assert.Throws<ArgumentException>(()=>Roulette.Settle(new[]{new RouletteBet(key,100m)},1000m,0));
    }

    [Fact]
    public void EnforcesWalletAndAggregateLimit()
    {
        Assert.Throws<ArgumentException>(()=>Roulette.Settle(new[]{new RouletteBet("red",100m)},99m,1));
        Assert.Throws<ArgumentException>(()=>Roulette.Settle(new[]{new RouletteBet("red",60000m),new RouletteBet("black",60000m)},200000m,1));
        Assert.Throws<ArgumentException>(()=>Roulette.Settle(new[]{new RouletteBet("red",100m),new RouletteBet("red",100m)},1000m,1));
        Assert.Equal(3600000m,Roulette.Settle(new[]{new RouletteBet("n0",100000m)},100000m,0).Bankroll);
    }

    [Fact]
    public void EverySupportedBetHasEuropeanHouseEdge()
    {
        foreach(string key in new[]{"n0","red","black","even","odd","low","high","d1","c3","i0,1","i0,1,2","i0,1,2,3","i1,2,3,4,5,6"})
        {
            decimal total=0;
            for(int n=0;n<37;n++)total+=Roulette.Settle(new[]{new RouletteBet(key,100m)},1000m,n).Return;
            Assert.Equal(3600m,total);
        }
    }
}
