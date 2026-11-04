using System.Text.Json;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;
using Xunit;

namespace BlackjackCasino.Tests;

public sealed class ManorPreviewProtocolTests
{
    [Theory]
    [InlineData("https://midnight-manor.local/index.html", true)]
    [InlineData("https://MIDNIGHT-MANOR.local/index.html", true)]
    [InlineData("http://midnight-manor.local/index.html", false)]
    [InlineData("https://midnight-manor.local.attacker.test/index.html", false)]
    [InlineData("https://midnight-manor.local/other.html", false)]
    [InlineData("https://midnight-manor.local:444/index.html", false)]
    [InlineData("javascript:alert(1)", false)]
    public void SourceValidationAllowsOnlyTheBundledFrontend(string source, bool expected) =>
        Assert.Equal(expected, ManorPreviewProtocol.IsTrustedSource(source));

    [Fact]
    public void CommandsAreVersionedAndAllowListed()
    {
        const string url = ManorPreviewProtocol.EntryPoint;
        Assert.True(ManorPreviewProtocol.TryParseCommand(
            url, """{"protocolVersion":1,"type":"snapshot.request","requestId":"test-1"}""", out var command));
        Assert.Equal("snapshot.request", command!.Type);
        Assert.False(ManorPreviewProtocol.TryParseCommand(
            url, """{"protocolVersion":1,"type":"roulette.placeBet","requestId":"a"}""", out _));
        Assert.False(ManorPreviewProtocol.TryParseCommand(
            url, """{"protocolVersion":2,"type":"ui.close","requestId":"a"}""", out _));
        Assert.False(ManorPreviewProtocol.TryParseCommand(
            "https://malicious.local/index.html",
            """{"protocolVersion":1,"type":"ui.close","requestId":"a"}""", out _));
        Assert.False(ManorPreviewProtocol.TryParseCommand(url, "{", out _));
    }

    [Fact]
    public void SnapshotReflectsCommittedSpinWithoutChangingFinancialState()
    {
        var state = new ManorSessionState
        {
            Bankroll = 1_234.5m,
            Bet = 20m,
            LastSpin = ManorRules.Evaluate([0, 1, 2, 3, 4], 20m),
            FreeSpinsRemaining = 2,
            CycleActive = true,
        };
        string before = JsonSerializer.Serialize(state);
        var snapshot = ManorPreviewProtocol.CreateSnapshot(state, false);
        Assert.Equal("$1,234.50", snapshot.Bankroll);
        Assert.Equal("$20.00", snapshot.Bet);
        Assert.Equal(15, snapshot.Cells.Length);
        Assert.False(snapshot.AnimationsEnabled);
        Assert.Contains("2 free spins", snapshot.Feature);
        Assert.Equal(before, JsonSerializer.Serialize(state));
        using JsonDocument response = JsonDocument.Parse(ManorPreviewProtocol.SerializeSnapshot(snapshot, "r7"));
        Assert.Equal("snapshot", response.RootElement.GetProperty("type").GetString());
        Assert.Equal("r7", response.RootElement.GetProperty("requestId").GetString());
        Assert.Equal(15, response.RootElement.GetProperty("payload").GetProperty("cells").GetArrayLength());
    }

    [Fact]
    public void EmptySaveStillProvidesFifteenVisibleCellsWithoutDrawingRandomOutcomes()
    {
        var state = new ManorSessionState { Bankroll = 100m, Bet = 10m };
        var snapshot = ManorPreviewProtocol.CreateSnapshot(state, true);
        Assert.False(snapshot.HasCommittedSpin);
        Assert.Equal(15, snapshot.Cells.Length);
        Assert.Equal("$0.00", snapshot.Payout);
    }
}
