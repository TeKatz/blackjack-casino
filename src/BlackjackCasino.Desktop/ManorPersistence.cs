using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal sealed record ManorCheckpoint(int SchemaVersion,bool NeedsRecovery,AppPreferences Preferences,
    PlayerProfileState Profile,ManorSessionState Machine);

/// <summary>Write-ahead aggregate: one atomic commit for wallet, progression and pending features.</summary>
internal sealed class ManorPersistence(string directory)
{
    internal static readonly JsonSerializerOptions Json = MakeOptions();
    public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BlackjackCasino");
    private string Journal => Path.Combine(directory,"manor-checkpoint.json");
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,Json),Json)!;

    public ManorCheckpoint? Load()
    {
        if(!File.Exists(Journal)) return null;
        ManorCheckpoint value=JsonSerializer.Deserialize<ManorCheckpoint>(File.ReadAllText(Journal),Json)
            ?? throw new InvalidDataException("The Manor checkpoint is empty.");
        Validate(value);
        return value;
    }

    public void Recover()
    {
        ManorCheckpoint? value=Load();
        if(value is { NeedsRecovery:true }) Project(value);
    }

    // Once Write returns, the aggregate is durable. Projection failure is recoverable, not a failed spin.
    public string? Commit(ManorCheckpoint value)
    {
        Validate(value);
        Write(Journal,value with { NeedsRecovery=true });
        try { Project(value); return null; }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        { return "Saved safely in the Manor checkpoint; local mirrors will recover at restart: "+ex.Message; }
    }

    private void Project(ManorCheckpoint value)
    {
        Write(Path.Combine(directory,"player-state.json"),value.Preferences);
        Write(Path.Combine(directory,"profile.json"),value.Profile);
        Write(Journal,value with { NeedsRecovery=false });
    }

    private void Write<T>(string path,T value)
    {
        Directory.CreateDirectory(directory);
        string temp=path+".manor.tmp";
        using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
        {
            JsonSerializer.Serialize(file,value,Json);
            file.Flush(flushToDisk:true);
        }
        File.Move(temp,path,overwrite:true);
    }

    private static void Validate(ManorCheckpoint checkpoint)
    {
        if(checkpoint.SchemaVersion!=1 || checkpoint.Machine is null || checkpoint.Profile is null || checkpoint.Preferences is null)
            throw new InvalidDataException("Unsupported Manor checkpoint.");
        ManorSessionState s=checkpoint.Machine;
        ManorRules.ValidateBet(s.Bet);
        if(s.Bankroll<0 || checkpoint.Preferences.Bankroll!=s.Bankroll || s.FreeSpinsRemaining is <0 or >20
            || s.CycleReturned<0 || s.CycleReturned>s.Bet*ManorRules.MaximumCycleMultiplier
            || s.Statistics is null || checkpoint.Profile.Manor is null)
            throw new InvalidDataException("Invalid Manor financial state.");
        if(s.LastSpin is {} spin)
        {
            ManorSpin evaluated=ManorRules.Evaluate(spin.Stops,s.Bet,spin.Free);
            if(evaluated.Payout!=spin.Payout || !evaluated.Cells.SequenceEqual(spin.Cells))
                throw new InvalidDataException("Invalid stored Manor reel result.");
        }
        if(s.Run is {} r && (r.Player is <0 or >16 || r.Ghost is <0 or >18 || r.Seed is <0 or >15
            || r.ExitTarget is not (250 or 500 or 1000) || r.Current<0 || r.Current>r.ExitTarget
            || r.Banked<0 || r.Banked>r.Current || r.Turn is <0 or >24 || !Enum.IsDefined(r.Status)))
            throw new InvalidDataException("Invalid Manor labyrinth state.");
    }

    private static JsonSerializerOptions MakeOptions()
    {
        var options=new JsonSerializerOptions { WriteIndented=true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
