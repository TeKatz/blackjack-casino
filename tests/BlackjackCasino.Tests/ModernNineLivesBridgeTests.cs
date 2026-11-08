using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernNineLivesBridgeTests
{
    private static string Command(ModernNineLivesSession epoch, string action = "spin", string? value = null)
        => JsonSerializer.Serialize(new
        {
            version = 1, session = epoch.Id, expectedRevision = epoch.Revision,
            requestId = Guid.NewGuid().ToString("D"), action, value,
        });

    [Theory]
    [InlineData("https://midnight-manor.local/nine-lives.html", true)]
    [InlineData("https://midnight-manor.local/nine-lives.html?debug=1", false)]
    [InlineData("https://midnight-manor.local/blackjack.html", false)]
    [InlineData("https://evil.local/nine-lives.html", false)]
    [InlineData("http://midnight-manor.local/nine-lives.html", false)]
    [InlineData("https://midnight-manor.local/nine-lives.html#anchor", false)]
    public void ChecksExactOfflineOrigin(string url, bool expected)
        => Assert.Equal(expected, ModernNineLivesProtocol.Trusted(url));

    [Fact]
    public void DeniesInjectedPayoutsOrUnlistedWagers()
    {
        var epoch = new ModernNineLivesSession();
        Assert.NotNull(ModernNineLivesProtocol.Parse(Command(epoch)));
        foreach (decimal bet in NineLivesRules.Bets)
            Assert.NotNull(ModernNineLivesProtocol.Parse(Command(epoch, "bet.set",
                bet.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        foreach (string malformed in new[]
        {
            Command(epoch, "spin", "5000"), Command(epoch, "bet.set", "7"),
            Command(epoch, "bet.set", "-1"), Command(epoch, "bet.set", "NaN"),
            Command(epoch, "bet.set", "1000000"), Command(epoch, "payout", "500000"),
            Command(epoch, "sound.set", "yes"), "[]", "broken", new string('x', 2049),
        })
            Assert.Null(ModernNineLivesProtocol.Parse(malformed));
    }

    [Fact]
    public void DuplicateReceiptNeverRerollsOrDuplicatesTheSharedWallet()
    {
        var epoch = new ModernNineLivesSession();
        var machine = new NineLivesSession(1000m, _ => 0);
        ModernNineLivesCommand cmd = ModernNineLivesProtocol.Parse(Command(epoch))!;
        int commits = 0;
        (bool, string) Commit(ModernNineLivesCommand _)
        {
            machine.Spin(5m);
            commits++;
            return (true, "Done");
        }
        ModernNineLivesReply first = epoch.Execute(cmd, Commit);
        Assert.True(first.Success);
        Assert.Equal(first, epoch.Execute(cmd, Commit));
        Assert.Equal(1, commits);
        Assert.Equal(25_995m, machine.Bankroll);
        Assert.False(epoch.Execute(cmd with { RequestId = Guid.NewGuid().ToString("D") }, Commit).Success);
        Assert.False(new ModernNineLivesSession().Execute(cmd, Commit).Success);
    }

    [Fact]
    public void ProjectionPreservesEveryAuthoritativeBonusAndLineField()
    {
        var random = new Queue<int>(new[] { 970, 970, 970, 0, 280, 510, 690, 810, 890 });
        var machine = new NineLivesSession(10000m, max => random.TryDequeue(out int next) ? next : 0);
        NineLivesSpinResult spin = machine.Spin(25m);
        JsonElement state = JsonSerializer.SerializeToElement(
            ModernNineLivesSnapshot.Create(machine, 25m, spin, null,
                new PlayerProfileState(), false, false, "de"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Equal("de", state.GetProperty("language").GetString());
        Assert.Equal(13, state.GetProperty("bets").GetArrayLength());
        Assert.Equal(5, state.GetProperty("paylines").GetArrayLength());
        Assert.Equal(8, state.GetProperty("paytable").GetArrayLength());
        JsonElement result = state.GetProperty("lastSpin");
        Assert.Equal(9, result.GetProperty("grid").GetArrayLength());
        Assert.Equal(9, result.GetProperty("bonusSpinsRemaining").GetInt32());
        Assert.Equal(9, result.GetProperty("bonusSpinsAwarded").GetInt32());
        Assert.Equal("9975", state.GetProperty("bankroll").GetString());
        // The numeric result and playable credits are computed by C#, not JS.
        Assert.Equal(spin.Payout.ToString(System.Globalization.CultureInfo.InvariantCulture),
            result.GetProperty("payout").GetString());
    }

    [Fact]
    public void BrowserRecoveryKeepsAwardedBonusSpinsWhileReconcilingSharedWallet()
    {
        var random = new Queue<int>(new[] { 970, 970, 970, 0, 280, 510, 690, 810, 890 });
        var machine = new NineLivesSession(10000m, max => random.TryDequeue(out int next) ? next : 0);
        _ = machine.Spin(25m);
        Assert.Equal(9, machine.BonusSpinsRemaining);
        machine.SynchronizeBankroll(750m);
        NineLivesSpinResult free = machine.Spin(1m);
        Assert.True(free.IsBonusSpin);
        Assert.Equal(25m, free.Bet);
        Assert.Equal(0m, free.Stake);
        Assert.Equal(8, free.BonusSpinsRemaining);
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.SynchronizeBankroll(-1m));
    }
}
