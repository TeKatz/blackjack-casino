using System.Text.Json;

namespace BlackjackCasino.Desktop;

internal static class PlayerStateStore
{
    private const decimal DefaultBankroll = 1_000m;

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static readonly string StatePath = Path.Combine(DirectoryPath, "player-state.json");

    public static decimal LoadBankroll()
    {
        try
        {
            if (!File.Exists(StatePath))
            {
                return DefaultBankroll;
            }

            string json = File.ReadAllText(StatePath);
            PlayerState? state = JsonSerializer.Deserialize<PlayerState>(json);
            return state is { Bankroll: >= 0m } ? state.Bankroll : DefaultBankroll;
        }
        catch
        {
            return DefaultBankroll;
        }
    }

    public static void SaveBankroll(decimal bankroll)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            string json = JsonSerializer.Serialize(new PlayerState(bankroll), new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            File.WriteAllText(StatePath, json);
        }
        catch
        {
            // A failed local save must never interrupt a blackjack hand.
        }
    }

    private sealed record PlayerState(decimal Bankroll);
}
