using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class ModernDiceBridgeTests
{
    private static string Command(ModernDiceSession session, long revision, string action,
        int? index = null, string? category = null, string? value = null)
        => JsonSerializer.Serialize(new
        {
            type = "midnight-dice.command", version = 1,
            requestId = Guid.NewGuid().ToString("D"), session = session.Id,
            expectedRevision = revision, action, index, category, value,
        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

    [Theory]
    [InlineData("https://midnight-manor.local/midnight-dice.html", true)]
    [InlineData("https://midnight-manor.local/midnight-dice.html?value=7", false)]
    [InlineData("https://midnight-manor.local/midnight-dice.html#fragment", false)]
    [InlineData("http://midnight-manor.local/midnight-dice.html", false)]
    [InlineData("https://evil.local/midnight-dice.html", false)]
    [InlineData("https://midnight-manor.local/midnight-merge.html", false)]
    public void OnlyBundledDiceOriginIsTrusted(string uri, bool expected)
        => Assert.Equal(expected, ModernDiceProtocol.Trusted(uri));

    [Fact]
    public void OnlyWhitelistedNativeDiceIntentsCanReachCore()
    {
        var session = new ModernDiceSession();
        foreach (string valid in new[]
        {
            Command(session, 0, "roll"),
            Command(session, 0, "hold", index: 4),
            Command(session, 0, "score", category: "FullHouse"),
            Command(session, 0, "scratch", category: "Kniffel"),
            Command(session, 0, "casual.start"),
            Command(session, 0, "casual.resume"),
            Command(session, 0, "casino.start", value: "100"),
            Command(session, 0, "casino.resume"),
            Command(session, 0, "casino.forfeit"),
            Command(session, 0, "sound.set", value: "false"),
            Command(session, 0, "animations.set", value: "true"),
            Command(session, 0, "music.set", value: "true"),
        })
            Assert.NotNull(ModernDiceProtocol.Parse(valid));

        foreach (string invalid in new[]
        {
            Command(session, 0, "roll", index: 0),
            Command(session, 0, "hold", index: -1),
            Command(session, 0, "hold", index: 5),
            Command(session, 0, "score", category: "NotACategory"),
            Command(session, 0, "score", category: "0"),
            Command(session, 0, "scratch", category: "Chance", value: "1000000"),
            Command(session, 0, "casino.start", value: "900"),
            Command(session, 0, "casino.start", value: "100.00"),
            Command(session, 0, "casino.forfeit", value: "true"),
            Command(session, 0, "payout", value: "99999999"),
            Command(session, 0, "sound.set", value: "maybe"),
            "{}", "[]", "not json", new string('!', 2049),
        })
            Assert.Null(ModernDiceProtocol.Parse(invalid));
    }

    [Fact]
    public void RollReceiptCannotBeReplayedAfterWebViewRecreation()
    {
        var commands = new ModernDiceSession();
        int calls = 0;
        KniffelGame game = new(() => 4);
        ModernDiceCommand roll = ModernDiceProtocol.Parse(Command(commands, commands.Revision, "roll"))!;
        (bool Success, string Message) Apply(ModernDiceCommand _)
        {
            calls++;
            game.Roll();
            return (true, "Saved");
        }
        ModernDiceReply first = commands.Execute(roll, Apply);
        Assert.True(first.Success);
        Assert.Equal(1, game.RollsUsed);
        Assert.Equal(first, commands.Execute(roll, Apply));
        Assert.Equal(1, calls);
        Assert.Equal(1, game.RollsUsed);
        Assert.False(commands.Execute(roll with
        {
            RequestId = Guid.NewGuid().ToString("D"),
        }, Apply).Success);
        Assert.Equal(1, calls);

        ModernDiceCommand hold = ModernDiceProtocol.Parse(Command(commands, 1, "hold", index: 0))!;
        Assert.True(commands.Execute(hold, _ =>
        {
            game.ToggleHold(0);
            return (true, "Held");
        }).Success);
        Assert.True(game.Held[0]);
        Assert.Equal(2, commands.Revision);
        Assert.Equal(first, commands.Execute(roll, Apply));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FailedCheckpointWriteLeavesRevisionRetryableWithNewId()
    {
        var session = new ModernDiceSession();
        ModernDiceCommand command = ModernDiceProtocol.Parse(Command(session, 0, "roll"))!;
        ModernDiceReply failed = session.Execute(command, _ => (false, "Local save failed"));
        Assert.False(failed.Success);
        Assert.Equal(0, failed.Revision);
        Assert.Equal(failed, session.Execute(command, _ => (true, "Should not retry old ID")));
        ModernDiceCommand retry = command with { RequestId = Guid.NewGuid().ToString("D") };
        Assert.True(session.Execute(retry, _ => (true, "Saved")).Success);
        Assert.Equal(1, session.Revision);
    }

    [Fact]
    public void RealCasinoEntryAndPayoutRemainAtomicAcrossClientRecreation()
    {
        decimal bankroll = 20_000m;
        KniffelTableState? current = null;
        ModernDiceSession session = new();
        ModernDiceCommand command = ModernDiceProtocol.Parse(Command(session, 0, "casino.start", value: "100"))!;
        int charged = 0;
        ModernDiceReply entry = session.Execute(command, _ =>
        {
            KniffelCash.Start(bankroll, current, 100m, change =>
            {
                bankroll = change.Bankroll;
                current = change.Table;
                charged++;
            });
            return (true, "Entry persisted");
        });
        Assert.True(entry.Success);
        Assert.Equal(19_900m, bankroll);
        Assert.Equal(entry, session.Execute(command, _ => throw new InvalidOperationException()));
        Assert.Equal(1, charged);
        Assert.NotNull(current);
        Assert.Equal(100m, current.Stake);
    }
}
