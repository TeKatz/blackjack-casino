using System.Text.Json;
using System.Text.Json.Nodes;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ManorPersistenceTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"manor-test-"+Guid.NewGuid());
    private string FileAt(string file)=>Path.Combine(folder,file);
    private static ManorCheckpoint Snapshot(decimal bankroll=975)=>new(1,true,new AppPreferences {Bankroll=bankroll},
        new PlayerProfileState {Level=3,Coins=120,LifetimeExperience=800},new ManorSessionState{Bankroll=bankroll});

    [Fact]
    public void CommitPersistsWalletProfileAndStateAsOneRecoverableAggregate()
    {
        var store=new ManorPersistence(folder); Assert.Null(store.Commit(Snapshot()));
        Assert.False(store.Load()!.NeedsRecovery);
        var preferences=JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FileAt("player-state.json")),ManorPersistence.Json)!;
        var profile=JsonSerializer.Deserialize<PlayerProfileState>(File.ReadAllText(FileAt("profile.json")),ManorPersistence.Json)!;
        Assert.Equal(975,preferences.Bankroll); Assert.Equal(120,profile.Coins); Assert.Equal(800,profile.LifetimeExperience);
        // A projected checkpoint must not roll back later Blackjack play.
        File.WriteAllText(FileAt("player-state.json"),"{\"Bankroll\":1234}"); store.Recover();
        Assert.Equal(1234,JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FileAt("player-state.json")))!.Bankroll);
    }
    [Fact]
    public void InterruptedProjectionReplaysExactlyAndIsIdempotent()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FileAt("manor-checkpoint.json"),JsonSerializer.Serialize(Snapshot(),ManorPersistence.Json));
        File.WriteAllText(FileAt("player-state.json"),"{}"); File.WriteAllText(FileAt("profile.json"),"{}");
        var store=new ManorPersistence(folder); store.Recover();
        string profile=File.ReadAllText(FileAt("profile.json")); store.Recover();
        Assert.Equal(profile,File.ReadAllText(FileAt("profile.json")));
        Assert.False(store.Load()!.NeedsRecovery);
        Assert.Equal(975,JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FileAt("player-state.json")),ManorPersistence.Json)!.Bankroll);
    }
    [Fact]
    public void ProjectionFailureKeepsDurableCheckpointForRecovery()
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(FileAt("profile.json"));
        var store=new ManorPersistence(folder); Assert.NotNull(store.Commit(Snapshot()));
        Assert.True(store.Load()!.NeedsRecovery);
        Directory.Delete(FileAt("profile.json")); store.Recover(); Assert.False(store.Load()!.NeedsRecovery);
    }
    [Fact]
    public void CorruptOrInconsistentCheckpointCannotOverwriteExistingFiles()
    {
        var store=new ManorPersistence(folder); store.Commit(Snapshot());
        string original=File.ReadAllText(FileAt("player-state.json"));
        Assert.Throws<InvalidDataException>(()=>store.Commit(Snapshot() with {Machine=new ManorSessionState{Bankroll=-1}}));
        Assert.Equal(original,File.ReadAllText(FileAt("player-state.json")));
        File.WriteAllText(FileAt("manor-checkpoint.json"),"{bad json");
        Assert.Throws<JsonException>(()=>store.Recover());
        Assert.Equal(original,File.ReadAllText(FileAt("player-state.json")));
    }
    [Fact]
    public void OldPreferencesAndProfilesHaveSafeDefaults()
    {
        var p=JsonSerializer.Deserialize<AppPreferences>("{}",ManorPersistence.Json)!;
        var profile=JsonSerializer.Deserialize<PlayerProfileState>("{}",ManorPersistence.Json)!;
        Assert.Equal(25,p.ManorBet); Assert.True(p.ManorInteractive); Assert.Equal(SlotSpinSpeed.Normal,p.ManorSpeed);
        Assert.Equal(0,p.ManorAuto.Count); Assert.NotNull(profile.Manor); Assert.NotNull(profile.Slots);
        var checkpoint=ManorPersistence.Clone(Snapshot()); Assert.Equal(975,checkpoint.Machine.Bankroll);
    }
    [Fact]
    public void ExistingWinningCheckpointAndPendingFreeSpinsKeepTheirOriginalPaytable()
    {
        int[] stops=Enumerable.Range(0,5).Select(reel=>Enumerable.Range(0,64).First(stop=>ManorRules.SymbolAt(reel,stop,1)==ManorSymbol.Jack)).ToArray();
        ManorSpin oldSpin=ManorRules.Evaluate(stops,25,paytableVersion:1);
        Assert.True(oldSpin.Payout>ManorRules.Evaluate(stops,25).Payout);
        var old=new ManorSessionState {Bankroll=1_000,Bet=25,LastSpin=oldSpin,CycleId=Guid.NewGuid(),CycleActive=true,FreeSpinsRemaining=1,CycleReturned=oldSpin.Payout};
        var checkpoint=Snapshot(1_000) with {Machine=old};
        var node=JsonNode.Parse(JsonSerializer.Serialize(checkpoint,ManorPersistence.Json))!;
        node["Machine"]!.AsObject().Remove("PaytableVersion");
        node["Machine"]!["LastSpin"]!.AsObject().Remove("PaytableVersion");
        Directory.CreateDirectory(folder);
        File.WriteAllText(FileAt("manor-checkpoint.json"),node.ToJsonString());
        var store=new ManorPersistence(folder); store.Recover();
        ManorCheckpoint saved=store.Load()!;
        Assert.Equal(oldSpin.Payout,saved.Machine.LastSpin!.Payout);
        Assert.Equal(1,saved.Machine.PaytableVersion);
        int i=0;
        var machine=new ManorSession(saved.Machine,_=>stops[i++%5]);
        Assert.Equal(oldSpin.Payout*2,machine.SpinFree().Payout);
        Assert.Equal(1_000+oldSpin.Payout*2,saved.Machine.Bankroll);
        Assert.Null(store.Commit(saved with {Preferences=saved.Preferences with {Bankroll=saved.Machine.Bankroll}}));
        Assert.Equal(saved.Machine.Bankroll,store.Load()!.Machine.Bankroll);
        machine.Start(25);
        Assert.Equal(ManorRules.CurrentPaytableVersion,saved.Machine.PaytableVersion);
        Assert.Equal(ManorRules.Evaluate(stops,25).Payout,saved.Machine.LastSpin!.Payout);
    }

    [Theory]
    [InlineData("0")] [InlineData("2")] [InlineData("-1")]
    public void UnknownPrizeScalesCannotBeCommitted(string value)
    {
        var snapshot=Snapshot();
        snapshot.Machine.Run=ManorRun.Create(2,2).State;
        snapshot.Machine.Run.PrizeScale=decimal.Parse(value,System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<InvalidDataException>(()=>new ManorPersistence(folder).Commit(snapshot));
    }
    public void Dispose() { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
}
