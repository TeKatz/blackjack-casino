using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly Dictionary<Guid, object> rouletteReceipts = new();
    private ModernRouletteView? modernRouletteView;
    private bool ModernRouletteOpen => modernRouletteView is not null;
    private void OpenModernRoulette()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || modernGameSurface?.IsOccupied == true || progressionController is null) return;
        if (!ModernRouletteView.IsAvailable) { ShowBrowserRecovery("Roulette", false, OpenModernRoulette); return; }
        ProcessLatestCompletedRound();
        modernRouletteView = new ModernRouletteView(() => new
        {
            Bankroll = Money(session.Bankroll), BankrollValue = session.Bankroll,
            Language = Localization.Language == UiLanguagePreference.German ? "de" : "en",
            Motion = preferences.Presentation("roulette").AnimationsEnabled,
            Sound = preferences.Presentation("roulette").SoundEnabled,
        }, ModernRouletteSpin, CloseModernRoulette, () =>
        {
            CloseModernRoulette();
            ShowBrowserRecovery("Roulette", ModernRouletteView.IsAvailable, OpenModernRoulette);
        });
        MountModernSurface(modernRouletteView);
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        TableModeText.Text = "Roulette · Grand Table";
        modeRegistry.NotifyStateChanged();
    }
    private void CloseModernRoulette()
    {
        if (modernRouletteView is not { } view) return;
        modernRouletteView = null;
        view.Dispose();
        UnmountModernSurface(view);
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        SyncHostMusic();
        RefreshGameUi();
        modeRegistry.NotifyStateChanged();
    }
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
