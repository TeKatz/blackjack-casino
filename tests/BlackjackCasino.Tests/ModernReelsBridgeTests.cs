using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class ModernReelsBridgeTests
{
    private static string Command(ModernReelsSession epoch,string action="spin",string? value=null)
        => JsonSerializer.Serialize(new
        {
            version=1,session=epoch.Id,expectedRevision=epoch.Revision,
            requestId=Guid.NewGuid().ToString("D"),action,value,
        });

    [Theory]
    [InlineData("https://midnight-manor.local/reels.html",true)]
    [InlineData("https://midnight-manor.local/reels.html?debug=1",false)]
    [InlineData("https://midnight-manor.local/lobby.html",false)]
    [InlineData("https://attacker.local/reels.html",false)]
    [InlineData("http://midnight-manor.local/reels.html",false)]
    public void AcceptsOnlyPackagedOrigin(string origin,bool allowed)
        => Assert.Equal(allowed,ModernReelsProtocol.Trusted(origin));

    [Fact]
    public void RejectsUnsafeCommandsAndAmounts()
    {
        var epoch=new ModernReelsSession();
        Assert.NotNull(ModernReelsProtocol.Parse(Command(epoch)));
        foreach(string wager in new[]{"1","5","25","100","500","1000"})
            Assert.NotNull(ModernReelsProtocol.Parse(Command(epoch,"bet.set",wager)));
        foreach(string text in new[]{
            Command(epoch,"spin","1000"),Command(epoch,"bet.set","3"),
            Command(epoch,"bet.set","NaN"),Command(epoch,"bet.set","5000"),
            Command(epoch,"payout","1000"),Command(epoch,"sound.set","yes"),
            "[]","bad json",new string('x',2049),
        })Assert.Null(ModernReelsProtocol.Parse(text));
    }

    [Fact]
    public void DuplicateReceiptNeverSettlesASecondSpin()
    {
        var epoch=new ModernReelsSession();
        ModernReelsCommand command=ModernReelsProtocol.Parse(Command(epoch))!;
        var machine=new SlotMachineSession(1000m,_=>0);
        int commits=0;
        (bool,string) Commit(ModernReelsCommand _)
        {
            machine.Spin(25m);commits++;return (true,"Settled");
        }
        ModernReelsReply first=epoch.Execute(command,Commit);
        Assert.True(first.Success);
        Assert.Equal(first,epoch.Execute(command,Commit));
        Assert.Equal(1,commits);
        Assert.Equal(1100m,machine.Bankroll);
        Assert.False(epoch.Execute(command with {RequestId=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.False(new ModernReelsSession().Execute(command,Commit).Success);
    }

    [Fact]
    public void ReadOnlySnapshotPreservesFractionalCentsFromLegacySaves()
    {
        var machine = new SlotMachineSession(975.125m);
        var profile = new PlayerProfileState();
        profile.Slots.TotalWagered = 25.0005m;
        JsonElement state = JsonSerializer.SerializeToElement(
            ModernReelsSnapshot.Create(machine,25m,null,null,profile,true,true,"en"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Equal("975.125",state.GetProperty("bankroll").GetString());
        Assert.Equal("-25.0005",state.GetProperty("stats").GetProperty("netProfit").GetString());
        Assert.Equal(JsonValueKind.Null,state.GetProperty("lastSpin").ValueKind);
        // Rendering must never round the real wallet or rewrite historical statistics.
        Assert.Equal(975.125m,machine.Bankroll);
        Assert.Equal(25.0005m,profile.Slots.TotalWagered);
    }

    [Fact]
    public void NativeResultAndWeightedStripMatchExactly()
    {
        var machine=new SlotMachineSession(1000m,_=>0);
        SlotSpinResult spin=machine.Spin(5m);
        JsonElement state=JsonSerializer.SerializeToElement(
            ModernReelsSnapshot.Create(machine,5m,spin,null,new PlayerProfileState(),true,false,"de"),
            new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        Assert.Equal("de",state.GetProperty("language").GetString());
        Assert.Equal("1020",state.GetProperty("bankroll").GetString());
        Assert.Equal(32,state.GetProperty("reelStrip").GetArrayLength());
        Assert.Equal(5,state.GetProperty("paytable").GetArrayLength());
        Assert.Equal(5,state.GetProperty("lastSpin").GetProperty("multiplier").GetInt32());
        Assert.All(state.GetProperty("lastSpin").GetProperty("symbols").EnumerateArray().ToArray(),
            symbol=>Assert.Equal("Cherry",symbol.GetString()));
    }
}
