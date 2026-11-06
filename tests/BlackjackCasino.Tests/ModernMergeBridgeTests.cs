using System.Text.Json;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernMergeBridgeTests
{
    private static string Command(ModernMergeSession session, long revision,
        string action, string? producerId = null, Guid? itemId = null,
        int? target = null, Guid? orderId = null, string? value = null)
        => JsonSerializer.Serialize(new
        {
            type = "midnight-merge.command", version = 1,
            session = session.Id, expectedRevision = revision,
            requestId = Guid.NewGuid().ToString("D"),
            action, producerId, itemId = itemId?.ToString("D"), target,
            orderId = orderId?.ToString("D"), value,
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    [Theory]
    [InlineData("https://midnight-manor.local/midnight-merge.html", true)]
    [InlineData("https://midnight-manor.local/midnight-merge.html?debug=1", false)]
    [InlineData("https://midnight-manor.local/midnight-merge.html#anchor", false)]
    [InlineData("http://midnight-manor.local/midnight-merge.html", false)]
    [InlineData("https://evil.local/midnight-merge.html", false)]
    [InlineData("https://midnight-manor.local/neon-vault.html", false)]
    public void OnlyPackagedMergePageIsTrusted(string address, bool expected)
        => Assert.Equal(expected, ModernMergeProtocol.Trusted(address));

    [Fact]
    public void WhitelistedActionsCannotInjectRenovationOrChangeUnlistedItems()
    {
        var session = new ModernMergeSession();
        Assert.NotNull(ModernMergeProtocol.Parse(Command(session, 0, "spawn", producerId: "chip-case")));
        Assert.NotNull(ModernMergeProtocol.Parse(Command(session, 0, "drop", itemId: Guid.NewGuid(), target: 41)));
        Assert.NotNull(ModernMergeProtocol.Parse(Command(session, 0, "deliver", orderId: Guid.NewGuid())));
        foreach (string invalid in new[]
        {
            Command(session, 0, "spawn", producerId: "fake-source"),
            Command(session, 0, "drop", itemId: Guid.NewGuid(), target: 42),
            Command(session, 0, "drop", itemId: Guid.NewGuid(), target: -1),
            Command(session, 0, "drop", itemId: Guid.Empty, target: 3).Replace(Guid.Empty.ToString("D"), "invalid-guid"),
            Command(session, 0, "reward", value: "1000000"),
            Command(session, 0, "spawn", producerId: "chip-case", value: "1000"),
            Command(session, 0, "sound.set", value: "yes"),
            new string('x', 3073), "[]", "invalid-json",
        })
            Assert.Null(ModernMergeProtocol.Parse(invalid));
    }

    [Fact]
    public void BrowserReconnectCannotDoubleSpawnOrMergeExistingIds()
    {
        MergeBoard board = new();
        var commands = new ModernMergeSession();
        ModernMergeCommand spawn = ModernMergeProtocol.Parse(Command(commands, 0, "spawn", producerId: "chip-case"))!;
        int executions = 0;
        (bool Success, string Message, long Revision) Commit(ModernMergeCommand _)
        {
            executions++;
            MergeActionResult result = board.TrySpawn("chip-case");
            return (result.Success, result.Outcome.ToString(), board.Snapshot.Revision);
        }

        ModernMergeReply first = commands.Execute(spawn, board.Snapshot.Revision, Commit);
        Assert.True(first.Success);
        Assert.Equal(1, board.Snapshot.Items.Count);
        Assert.Equal(1, board.Snapshot.Revision);
        Assert.Equal(first, commands.Execute(spawn, board.Snapshot.Revision, Commit));
        Assert.Equal(1, executions);

        ModernMergeCommand stale = spawn with
        {
            RequestId = Guid.NewGuid().ToString("D"),
        };
        Assert.False(commands.Execute(stale, board.Snapshot.Revision, Commit).Success);
        Assert.False(new ModernMergeSession().Execute(spawn, board.Snapshot.Revision, Commit).Success);
        Assert.Equal(1, board.Snapshot.Items.Count);
    }

    [Fact]
    public void NativeBoardAndDurableOrderReceiptsSurvivePresentationRecreation()
    {
        MergeGameState saved = new();
        MergeBoard board = new(saved, candidate => saved = candidate);
        _ = board.TrySpawn();
        _ = board.TrySpawn();
        Guid chip = board.Snapshot.Items.Single(item => item.Position == 0).Id;
        MergeActionResult merged = board.TryDrop(chip, 1);
        Assert.Equal(MergeOutcome.Merged, merged.Outcome);
        MergeOrderInstance order = board.Snapshot.Orders.Single(item => item.DefinitionId == "first-stack");
        MergeActionResult delivered = board.TryComplete(order.Id);
        Assert.Equal(MergeOutcome.OrderCompleted, delivered.Outcome);
        Assert.Equal(10, board.Snapshot.RenovationPoints);

        MergeBoard rehosted = new(saved);
        Assert.Equal(10, rehosted.Snapshot.RenovationPoints);
        Assert.Equal(1, rehosted.Snapshot.Receipts.Count);
        Assert.Equal(board.Snapshot.Revision, rehosted.Snapshot.Revision);
        Assert.Equal(MergeOutcome.AlreadyCompleted, rehosted.TryComplete(order.Id).Outcome);
        Assert.Equal(10, rehosted.Snapshot.RenovationPoints);
    }
}
