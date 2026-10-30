using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal sealed class ManorController
{
    private readonly ManorPersistence persistence;
    private readonly Func<int,int>? rng;
    private readonly PlayerProgressionController progression;
    private readonly Action<AppPreferences> projectWallet;
    public ManorSessionState State { get; private set; }
    public AppPreferences Preferences { get; private set; }
    public ProgressionAward LastAward { get; private set; }=ProgressionAward.None;
    public string? SaveNotice { get; private set; }
    public PlayerProfileState Profile => progression.Profile;

    public ManorController(AppPreferences preferences,decimal bankroll,PlayerProgressionController progression,Action<AppPreferences> projectWallet,ManorPersistence? persistence=null,Func<int,int>? rng=null)
    {
        this.persistence=persistence ?? new(ManorPersistence.DefaultDirectory);
        this.rng=rng;
        this.progression=progression;
        this.projectWallet=projectWallet;
        Preferences=preferences;
        this.persistence.Recover();
        State=this.persistence.Load()?.Machine ?? new ManorSessionState { Bet=preferences.ManorBet };
        State.Bankroll=bankroll;
        State.Statistics=progression.Profile.Manor;
    }

    public void Execute(Action<ManorSession> action,AppPreferences? preferences=null)
    {
        // Stage changes on copies. A failed initial journal write changes neither wallet nor profile.
        PlayerProfileState profile=ManorPersistence.Clone(progression.Profile);
        ManorSessionState next=ManorPersistence.Clone(State);
        next.Statistics=profile.Manor;
        var machine=new ManorSession(next,rng);
        action(machine);
        var grants=new ProgressionService(profile);
        ProgressionAward award=ManorProgression.Award(grants,next);
        // Paid Manor spins update TotalWagered while free spins and room decisions do not.
        // Synchronize that durable statistic before the existing journal atomically commits it.
        CasinoVaultService.SyncFromStatistics(profile);
        AppPreferences prefs=(preferences ?? Preferences) with { Bankroll=next.Bankroll };
        SaveNotice=persistence.Commit(new(1,true,prefs,profile,next));
        State=next;
        Preferences=prefs;
        LastAward=award;
        progression.AdoptManorProfile(profile);
        projectWallet(prefs);
    }
}
