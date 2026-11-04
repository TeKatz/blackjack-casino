using System.Text.Json;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;
using Xunit;

namespace BlackjackCasino.Tests;

public sealed class ManorPreviewProtocolTests
{
    [Theory]
    [InlineData("https://midnight-manor.local/index.html",true)]
    [InlineData("https://MIDNIGHT-MANOR.local/index.html",true)]
    [InlineData("http://midnight-manor.local/index.html",false)]
    [InlineData("https://midnight-manor.local.attacker.test/index.html",false)]
    [InlineData("https://midnight-manor.local/other.html",false)]
    [InlineData("https://midnight-manor.local:444/index.html",false)]
    [InlineData("javascript:alert(1)",false)]
    public void SourceValidationAllowsOnlyBundledFrontend(string source,bool expected) =>
        Assert.Equal(expected,ManorPreviewProtocol.IsTrustedSource(source));

    [Fact]
    public void MutatingCommandsMustBeV2RevisionedAndAllowListed()
    {
        const string url=ManorPreviewProtocol.EntryPoint;
        Assert.True(ManorPreviewProtocol.TryParseCommand(
            url,"""{"protocolVersion":2,"type":"spin.paid","requestId":"one","expectedRevision":"0","bet":"25"}""",out var command));
        Assert.Equal("spin.paid",command!.Type);
        Assert.Equal(0,command.ExpectedRevision);
        Assert.Equal(25m,command.Bet);

        Assert.True(ManorPreviewProtocol.TryParseCommand(
            url,"""{"protocolVersion":2,"type":"run.move","requestId":"room_1","expectedRevision":"17","room":4}""",out var move));
        Assert.Equal(4,move!.Room);
        Assert.Equal(17,move.ExpectedRevision);

        string[] invalid=
        [
            """{"protocolVersion":1,"type":"spin.paid","requestId":"x","expectedRevision":"0","bet":"25"}""",
            """{"protocolVersion":2,"type":"spin.paid","requestId":"x","bet":"25"}""",
            """{"protocolVersion":2,"type":"spin.paid","requestId":"x","expectedRevision":"-1","bet":"25"}""",
            """{"protocolVersion":2,"type":"spin.paid","requestId":"x","expectedRevision":"0","bet":"1.5"}""",
            """{"protocolVersion":2,"type":"spin.paid","requestId":"x","expectedRevision":"0","bet":"1001"}""",
            """{"protocolVersion":2,"type":"run.move","requestId":"x","expectedRevision":"0","room":17}""",
            """{"protocolVersion":2,"type":"run.move","requestId":"x","expectedRevision":"0","room":"4"}""",
            """{"protocolVersion":2,"type":"roulette.placeBet","requestId":"x","expectedRevision":"0"}""",
            """{"protocolVersion":2,"type":"ui.close","requestId":"bad id"}""",
            "{",
        ];
        foreach(string body in invalid)
            Assert.False(ManorPreviewProtocol.TryParseCommand(url,body,out _));
        Assert.False(ManorPreviewProtocol.TryParseCommand(
            "https://malicious.local/index.html",
            """{"protocolVersion":2,"type":"ui.close","requestId":"x"}""",out _));
    }

    [Fact]
    public void SnapshotReflectsCommittedSpinWithoutMutatingFinancialState()
    {
        var state=new ManorSessionState
        {
            Bankroll=1_234.5m,Bet=20m,Revision=7,
            LastSpin=ManorRules.Evaluate([0,1,2,3,4],20m),
            FreeSpinsRemaining=2,CycleActive=true
        };
        string before=JsonSerializer.Serialize(state);
        var snapshot=ManorPreviewProtocol.CreateSnapshot(state,false,selectedBet:20m);
        Assert.Equal("$1,234.50",snapshot.Bankroll);
        Assert.Equal("$20.00",snapshot.SelectedBet);
        Assert.Equal("7",snapshot.Revision);
        Assert.Equal(15,snapshot.Cells.Length);
        Assert.True(snapshot.CanFreeSpin);
        Assert.False(snapshot.CanSpin);
        Assert.False(snapshot.AnimationsEnabled);
        Assert.Contains("2 free spins",snapshot.Feature);
        Assert.Equal(before,JsonSerializer.Serialize(state));

        using JsonDocument response=JsonDocument.Parse(ManorPreviewProtocol.SerializeSnapshot(snapshot,"r7"));
        Assert.Equal(2,response.RootElement.GetProperty("protocolVersion").GetInt32());
        Assert.Equal("snapshot",response.RootElement.GetProperty("type").GetString());
        Assert.Equal("r7",response.RootElement.GetProperty("requestId").GetString());
        Assert.Equal("7",response.RootElement.GetProperty("payload").GetProperty("revision").GetString());
    }

    [Fact]
    public void RunSnapshotExposesLegalRoomsAndCluesButNotHiddenGhostCoordinates()
    {
        var state=new ManorSessionState
        {
            Bankroll=500m,Bet=25m,CycleActive=true,
            Run=ManorRun.Create(3,2).State,
        };
        var snapshot=ManorPreviewProtocol.CreateSnapshot(state,true);
        Assert.NotNull(snapshot.Run);
        Assert.Equal(0,snapshot.Run!.PlayerRoom);
        Assert.Contains(snapshot.Run!.Rooms,r=>r.Id==1 && r.Available);
        Assert.DoesNotContain(snapshot.Run.Rooms,r=>r.Id==16 && r.Available);
        Assert.False(snapshot.CanFreeSpin);
        Assert.False(snapshot.CanSpin);
        using JsonDocument json=JsonDocument.Parse(ManorPreviewProtocol.SerializeSnapshot(snapshot,"m1"));
        Assert.False(json.RootElement.GetProperty("payload").GetProperty("run").TryGetProperty("ghost",out _));
    }

    [Fact]
    public void EmptySaveHasFifteenDeterministicCellsWithoutDrawingRandomOutcomes()
    {
        var state=new ManorSessionState { Bankroll=100m,Bet=10m };
        var a=ManorPreviewProtocol.CreateSnapshot(state,true);
        var b=ManorPreviewProtocol.CreateSnapshot(state,true);
        Assert.False(a.HasCommittedSpin);
        Assert.True(a.CanSpin);
        Assert.Equal(a.Cells,b.Cells);
        Assert.Equal(15,a.Cells.Length);
        Assert.Equal("$0.00",a.Payout);
    }
}
