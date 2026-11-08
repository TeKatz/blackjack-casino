using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernNeonVaultBridgeTests
{
    private static string Command(ModernNeonVaultSession epoch, string action = "spin", string? value = null)
        => JsonSerializer.Serialize(new
        {
            version = 1, session = epoch.Id, expectedRevision = epoch.Revision,
            requestId = Guid.NewGuid().ToString("D"), action, value,
        });

    [Theory]
    [InlineData("https://midnight-manor.local/neon-vault.html", true)]
    [InlineData("https://midnight-manor.local/neon-vault.html?debug=1", false)]
    [InlineData("https://midnight-manor.local/neon-vault.html#look", false)]
    [InlineData("https://midnight-manor.local/nine-lives.html", false)]
    [InlineData("https://evil.local/neon-vault.html", false)]
    [InlineData("http://midnight-manor.local/neon-vault.html", false)]
    public void OnlyTheExactPackagedOriginIsAuthorized(string url, bool authorized)
        => Assert.Equal(authorized, ModernNeonVaultProtocol.Trusted(url));

    [Fact]
    public void RejectsInjectedMoneyAndInvalidBetCommands()
    {
        var epoch = new ModernNeonVaultSession();
        Assert.NotNull(ModernNeonVaultProtocol.Parse(Command(epoch)));
        foreach (decimal value in NeonVaultRules.Bets)
        {
            Assert.NotNull(ModernNeonVaultProtocol.Parse(Command(epoch, "bet.set",
                value.ToString(CultureInfo.InvariantCulture))));
        }
        foreach (string payload in new[]
        {
            Command(epoch, "spin", "123"), Command(epoch, "bet.set", "0"),
            Command(epoch, "bet.set", "7"), Command(epoch, "bet.set", "250000.01"),
            Command(epoch, "bet.set", "NaN"), Command(epoch, "bet.set", "-1"),
            Command(epoch, "balance.set", "1000000"), Command(epoch, "payout", "9999999"),
            Command(epoch, "sound.set", "yes"), "broken", "[]", new string('x', 2049),
        })
            Assert.Null(ModernNeonVaultProtocol.Parse(payload));
    }

    [Fact]
    public void DuplicateReceiptNeverRerollsOrAwardsTheWalletTwice()
    {
        var epoch = new ModernNeonVaultSession();
        var machine = new NeonVaultSession(10000m, _ => 0);
        ModernNeonVaultCommand command = ModernNeonVaultProtocol.Parse(Command(epoch))!;
        int commits = 0;
        (bool, string) Commit(ModernNeonVaultCommand _)
        {
            machine.Spin(100m);
            commits++;
            return (true, "Settled");
        }
        ModernNeonVaultReply first = epoch.Execute(command, Commit);
        Assert.True(first.Success);
        Assert.Equal(first, epoch.Execute(command, Commit));
        Assert.Equal(1, commits);
        Assert.False(epoch.Execute(command with { RequestId = Guid.NewGuid().ToString("D") }, Commit).Success);
        Assert.False(new ModernNeonVaultSession().Execute(command, Commit).Success);
    }

    [Fact]
    public void NativeProjectionPreservesAllFiveReelsAndJackpotTiers()
    {
        int[] grid =
        [
            0,28,50,68,82, 0,28,50,68,82,
            0,28,50,68,82, 0,28,50,68,82,
        ];
        var rolls = new Queue<int>(grid.Concat([5000]));
        var machine = new NeonVaultSession(10000m, max =>
            rolls.TryDequeue(out int value) ? value : max - 1);
        NeonVaultSpinResult spin = machine.Spin(100m);
        JsonElement state = JsonSerializer.SerializeToElement(
            ModernNeonVaultSnapshot.Create(machine, 100m, spin, null,
                new PlayerProfileState(), true, false, "de"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Equal("de", state.GetProperty("language").GetString());
        Assert.Equal(16, state.GetProperty("bets").GetArrayLength());
        Assert.Equal(8, state.GetProperty("paytable").GetArrayLength());
        Assert.Equal(4, state.GetProperty("jackpots").GetArrayLength());
        Assert.Equal(12, state.GetProperty("energyTarget").GetInt32());
        JsonElement result = state.GetProperty("lastSpin");
        Assert.Equal(20, result.GetProperty("baseGrid").GetArrayLength());
        Assert.Equal(20, result.GetProperty("finalGrid").GetArrayLength());
        Assert.Equal("9900", state.GetProperty("bankroll").GetString());
        Assert.Equal(spin.Payout.ToString(CultureInfo.InvariantCulture),
            result.GetProperty("payout").GetString());
        Assert.Equal(spin.WaysWon, result.GetProperty("waysWon").GetInt32());
    }

    [Fact]
    public void RehostingBrowserCannotDiscardFreeSpinsOrVaultEnergy()
    {
        int[] trigger =
        [
            96,96,96,68,82, 0,28,50,68,82,
            0,28,50,68,82, 0,28,50,68,82,
        ];
        var rolls = new Queue<int>(trigger.Concat([5000]));
        var machine = new NeonVaultSession(10000m,
            max => rolls.TryDequeue(out int value) ? value : max - 1);
        NeonVaultSpinResult start = machine.Spin(100m);
        Assert.Equal(8, start.FreeSpinsAwarded);
        Assert.True(machine.BonusActive);
        machine.SynchronizeBankroll(875m);
        Assert.Equal(8, machine.FreeSpinsRemaining);
        NeonVaultSpinResult bonus = machine.Spin(1m);
        Assert.True(bonus.IsFreeSpin);
        Assert.Equal(100m, bonus.Bet);
        Assert.Equal(0m, bonus.Stake);
        Assert.Equal(7, bonus.FreeSpinsRemaining);
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.SynchronizeBankroll(-1m));
    }
}
