using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Isolated preview-only player profile and wallet aggregate journal.
/// No two independent mirrors may be made visible before the durable pending
/// checkpoint is written. Replay is idempotent after an interrupted projection.
/// </summary>
internal sealed record NativePlayerAggregateCheckpoint(
    int Version, bool Pending, AppPreferences Preferences, PlayerProfileState Profile);

internal sealed class NativePlayerAggregateJournal(string directory)
{
    private const int Version = 1;
    private static readonly JsonSerializerOptions Json = CreateJson();
    private string Journal => Path.Combine(directory, "native-player-aggregate.json");

    internal void Recover()
    {
        if (!File.Exists(Journal)) return;
        NativePlayerAggregateCheckpoint checkpoint = Read();
        if (checkpoint.Pending) Project(checkpoint);
    }

    internal void Commit(AppPreferences preferences, PlayerProfileState profile)
    {
        var checkpoint = new NativePlayerAggregateCheckpoint(Version, true,
            ManorPersistence.Clone(preferences), ManorPersistence.Clone(profile));
        Validate(checkpoint);
        Write(Journal, checkpoint, flush: true);
        Project(checkpoint);
    }

    private void Project(NativePlayerAggregateCheckpoint checkpoint)
    {
        Write(Path.Combine(directory, "player-state.json"), checkpoint.Preferences, flush: true);
        Write(Path.Combine(directory, "profile.json"), checkpoint.Profile, flush: true);
        Write(Journal, checkpoint with { Pending = false }, flush: false);
    }

    private NativePlayerAggregateCheckpoint Read()
    {
        using var stream = File.OpenRead(Journal);
        NativePlayerAggregateCheckpoint? checkpoint =
            JsonSerializer.Deserialize<NativePlayerAggregateCheckpoint>(stream, Json);
        Validate(checkpoint);
        return checkpoint!;
    }

    private static void Validate(NativePlayerAggregateCheckpoint? value)
    {
        if (value is null || value.Version != Version || value.Preferences is null
            || value.Profile is null || value.Preferences.Bankroll < 0)
            throw new InvalidDataException("Invalid native player aggregate checkpoint.");
    }

    private static JsonSerializerOptions CreateJson()
    {
        var result = new JsonSerializerOptions { WriteIndented = true };
        result.Converters.Add(new JsonStringEnumConverter());
        return result;
    }

    private static void Write<T>(string path, T value, bool flush)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".native-aggregate.tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, value, Json);
            file.Flush(flushToDisk: flush);
        }
        File.Move(temp, path, overwrite: true);
    }
}
