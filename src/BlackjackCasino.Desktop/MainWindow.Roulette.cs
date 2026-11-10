using System;
using System.Collections.Generic;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly Dictionary<Guid, object> rouletteReceipts = new();
    private object ModernRouletteSpin(Guid roundId, JsonElement raw)
    {
        if (rouletteReceipts.TryGetValue(roundId, out var previous)) return previous;
        object Reject(string message) => new { RoundId = roundId, Success = false, Message = message };
        if(session.RoundInProgress || progressionController is null) return Reject("Finish the current table game first.");
        if(raw.ValueKind != JsonValueKind.Array || raw.GetArrayLength() is < 1 or > 100) return Reject("Invalid bets.");
        var bets = new List<RouletteBet>();
        foreach(var item in raw.EnumerateArray())
        {
            if(item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("key",out var key) || key.ValueKind!=JsonValueKind.String
                || !item.TryGetProperty("amount",out var amount) || amount.ValueKind!=JsonValueKind.Number || !amount.TryGetDecimal(out var stake))
                return Reject("Invalid bet.");
            bets.Add(new RouletteBet(key.GetString()!,stake));
        }
        ProcessLatestCompletedRound();
        RouletteResult result;
        try { result = Roulette.Settle(bets,session.Bankroll,Roulette.Draw()); }
        catch(ArgumentException e) { return Reject(e.Message); }
        catch(OverflowException) { return Reject("Bankroll limit exceeded."); }
        session.ResetBankroll(result.Bankroll);
        selectedBet = Math.Min(selectedBet,session.Bankroll);
        var receipt = new { RoundId=roundId, Success=true, result.Number, result.Stake, result.Return, result.Net, result.Bankroll };
        rouletteReceipts.Add(roundId,receipt);
        SavePreferences();
        progressionController.SaveAll();
        RefreshGameUi();
        return receipt;
    }
}
