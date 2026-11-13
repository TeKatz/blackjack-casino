using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    // The test-only in-memory map is retained for synthetic WPF sessions.
    // Installed WPF and NativeShell compile the exact same durable WAL source.
    private readonly Dictionary<Guid, object> rouletteReceipts = new();
    private NativeRouletteJournal? rouletteJournal;
    private bool rouletteRecoveryRequired;
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
        object Reject(string message) => new { RoundId = roundId, Success = false, Message = message };
        if (rouletteRecoveryRequired) return Reject("Roulette requires restarting the casino.");
        if (roundId == Guid.Empty) return Reject("Missing roulette transaction ID.");
        if (rouletteReceipts.TryGetValue(roundId, out var cached)) return cached;

        NativeRouletteJournal? journal = null;
        if (!testSession)
        {
            try
            {
                journal = rouletteJournal ??= new NativeRouletteJournal(
                    ManorPersistence.DefaultDirectory);
                if (journal.TryReceipt(roundId, out NativeRouletteReceipt? replay))
                    return replay!;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException
                or JsonException or InvalidDataException)
            {
                return BlockRouletteAfterSaveFailure(error);
            }
        }

        if(session.RoundInProgress || progressionController is null)
            return Reject("Finish the current table game first.");
        if(raw.ValueKind != JsonValueKind.Array || raw.GetArrayLength() is < 1 or > 100)
            return Reject("Invalid bets.");
        var bets = new List<RouletteBet>();
        foreach(var item in raw.EnumerateArray())
        {
            if(item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String
                || !item.TryGetProperty("amount", out var amount) || amount.ValueKind != JsonValueKind.Number
                || !amount.TryGetDecimal(out var stake))
                return Reject("Invalid bet.");
            bets.Add(new RouletteBet(key.GetString()!, stake));
        }
        ProcessLatestCompletedRound();
        RouletteResult result;
        try { result = Roulette.Settle(bets, session.Bankroll, Roulette.Draw()); }
        catch(ArgumentException e) { return Reject(e.Message); }
        catch(OverflowException) { return Reject("Bankroll limit exceeded."); }

        var candidate = preferences with { Bankroll = result.Bankroll };
        try
        {
            // First durably record the transaction ID and the new wallet.
            // The WPF save mirror is durable and flushes older deferred writes.
            // Returning a receipt is safe only AFTER journal + mirror succeed.
            NativeRouletteReceipt? durableReceipt = journal?.Commit(
                roundId, result, candidate,
                mirrorWriter: state => PlayerStateStore.SaveOrThrow(state));

            session.ResetBankroll(result.Bankroll);
            selectedBet = Math.Min(selectedBet, session.Bankroll);
            preferences = candidate;
            object receipt = durableReceipt ?? new {
                RoundId = roundId, Success = true, result.Number,
                result.Stake, result.Return, result.Net, result.Bankroll
            };
            rouletteReceipts.Add(roundId, receipt);
            SavePreferences();
            progressionController.SaveAll();
            RefreshGameUi();
            return receipt;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or JsonException or InvalidDataException or InvalidOperationException)
        {
            // The journal may already be durable even if mirroring failed.
            // No further bets may run until restart and idempotent recovery.
            return BlockRouletteAfterSaveFailure(error);
        }
    }

    private object BlockRouletteAfterSaveFailure(Exception error)
    {
        rouletteRecoveryRequired = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MessageBox.Show(
                "A roulette transaction may require journal recovery. "
                + "The casino will close to prevent duplicate settlements.\n\n"
                + error.Message,
                "Roulette save protection", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current?.Shutdown(1);
        }), DispatcherPriority.Send);
        return new { RoundId = Guid.Empty, Success = false,
            Message = "Roulette save uncertain. Restart the casino before another wager." };
    }
}
