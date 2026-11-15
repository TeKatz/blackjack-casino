using System.Text.Json;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.HostShared;
using Xunit;

namespace BlackjackCasino.Tests;

public class EmpireModernBridgeTests
{
    [Fact]
    public void OnlyNewClickerCommandsAreAccepted()
    {
        var epoch = new EmpireModernSession();
        string Make(string action, string? id = null, int count = 1) =>
            JsonSerializer.Serialize(new {version = EmpireModernProtocol.Version,
                requestId = Guid.NewGuid().ToString("D"), session = epoch.Id,
                expectedRevision = 0, action, id, count});
        Assert.NotNull(EmpireModernProtocol.Parse(Make("tap", count: 20)));
        Assert.NotNull(EmpireModernProtocol.Parse(Make("building.upgrade", "cashier")));
        Assert.NotNull(EmpireModernProtocol.Parse(Make("prestige")));
        Assert.NotNull(EmpireModernProtocol.Parse(Make("legacy.buy", "active-1")));
        Assert.Null(EmpireModernProtocol.Parse(Make("legacy.buy", "fake-node")));
        Assert.Null(EmpireModernProtocol.Parse(Make("legacy.buy", "active-1", count: 2)));
        Assert.Null(EmpireModernProtocol.Parse(Make("upgrade", "slots")));
        Assert.Null(EmpireModernProtocol.Parse(Make("research", "construction")));
        Assert.Null(EmpireModernProtocol.Parse(Make("automation")));
        Assert.Null(EmpireModernProtocol.Parse(Make("tap", count: 21)));
        Assert.Null(EmpireModernProtocol.Parse(Make("building.upgrade", "unknown")));
    }

    [Fact]
    public void AcknowledgementIsIdempotentAndRevisionFenced()
    {
        var epoch = new EmpireModernSession();
        var command = new EmpireModernCommand(Guid.NewGuid().ToString("D"), epoch.Id, 0,
            "tap", null, 4);
        int actions = 0;
        var result = epoch.Execute(command, _ => { actions++; return new(true, "OK"); });
        Assert.True(result.Success);
        Assert.Equal(1, result.Revision);
        Assert.Equal(result, epoch.Execute(command, _ => throw new Exception("replayed")));
        Assert.Equal(1, actions);
        Assert.False(epoch.Execute(command with { RequestId = Guid.NewGuid().ToString("D") },
            _ => throw new Exception("stale")).Success);
    }

    [Fact]
    public void ClickerSnapshotExposesTwelveBoundedBuildingsAndVisibleGoals()
    {
        var service = new EmpireService();
        service.Start(DateTimeOffset.UtcNow);
        using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(EmpireModernSnapshot.Create(service),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        JsonElement state = doc.RootElement;
        Assert.Equal(12, state.GetProperty("buildings").GetArrayLength());
        JsonElement legacy = state.GetProperty("legacy");
        Assert.False(legacy.GetProperty("unlocked").GetBoolean());
        Assert.Equal(15, legacy.GetProperty("upgrades").GetArrayLength());
        Assert.Equal(20, legacy.GetProperty("milestones").GetArrayLength());
        Assert.Equal(0, state.GetProperty("prestige").GetInt32());
        Assert.Equal("cashier", state.GetProperty("goal").GetProperty("id").GetString());
        Assert.True(state.GetProperty("buildings")[0].GetProperty("unlocked").GetBoolean());
        Assert.False(state.GetProperty("buildings")[1].GetProperty("unlocked").GetBoolean());
    }
}
