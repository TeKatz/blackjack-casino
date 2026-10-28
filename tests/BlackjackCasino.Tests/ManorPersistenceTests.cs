using System.Text.Json;
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
    public void Dispose() { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
}
