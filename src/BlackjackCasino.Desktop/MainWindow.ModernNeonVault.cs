using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernNeonVaultView? modernNeonVaultView;
    private NeonVaultSession? modernNeonVaultMachine;
    private ModernNeonVaultSession? modernNeonVaultCommands;
    private NeonVaultSpinResult? modernNeonVaultLastSpin;
    private ProgressionAward? modernNeonVaultAward;
    private decimal modernNeonVaultBet;
    private SlotAudio? modernNeonVaultAudio;
    private BackgroundMusic? modernNeonVaultMusic;
    private string? modernNeonVaultAudioSpinId;
    private readonly HashSet<int> modernNeonVaultStopped = [];
    private bool modernNeonVaultOutcomePlayed, modernNeonVaultLevelUp;
    private bool ModernNeonVaultOpen => modernNeonVaultView is not null;

    private void OpenModernNeonVault()
    {
        if (session.RoundInProgress || progressionController is null || ModernNeonVaultOpen
            || ModernReelsOpen || ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || ModernNineLivesOpen || embeddedGameShell is not null) return;
        if (!ModernNeonVaultView.IsAvailable)
        {
            StatusText.Text = "Neon Vault frontend missing. Reinstall the Development build.";
            if (!testSession)
                MessageBox.Show(this, StatusText.Text, "Neon Vault unavailable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        slotProgression ??= new SlotProgression(progressionController.Service);
        if (modernNeonVaultMachine is null)
        {
            modernNeonVaultMachine = new NeonVaultSession(session.Bankroll);
            modernNeonVaultCommands = new ModernNeonVaultSession();
            decimal saved = Math.Clamp(preferences.NeonVaultBet,
                NeonVaultRules.MinimumBet, NeonVaultRules.MaximumBet);
            modernNeonVaultBet = NeonVaultRules.Bets.OrderBy(bet => Math.Abs(bet - saved)).First();
            modernNeonVaultLastSpin = null;
            modernNeonVaultAward = null;
        }
        else
        {
            // Browser recovery preserves bonus spins; lobby changes to the shared
            // wallet must not resurrect a stale spendable balance.
            modernNeonVaultMachine.SynchronizeBankroll(session.Bankroll);
        }
        modernNeonVaultAudioSpinId = modernNeonVaultLastSpin?.SpinId.ToString("D");
        modernNeonVaultOutcomePlayed = modernNeonVaultLastSpin is not null;
        modernNeonVaultStopped.Clear();
        modernNeonVaultAudio = new SlotAudio();
        var presentation = preferences.Presentation("neon-vault");
        modernNeonVaultMusic = new BackgroundMusic(Soundtrack.Reels);
        modernNeonVaultMusic.Start(presentation.MusicEnabled, presentation.MusicVolume);
        backgroundMusic.SetEnabled(false);
        modernNeonVaultView = new ModernNeonVaultView(
            modernNeonVaultCommands!,
            () => ModernNeonVaultSnapshot.Create(modernNeonVaultMachine!, modernNeonVaultBet,
                modernNeonVaultLastSpin, modernNeonVaultAward, progressionController.Profile,
                preferences.Presentation("neon-vault").SoundEnabled,
                preferences.Presentation("neon-vault").AnimationsEnabled,
                Localization.Language == UiLanguagePreference.German ? "de" : "en"),
            ApplyModernNeonVaultAction, CloseModernNeonVault, OnModernNeonVaultFailed,
            ModernNeonVaultStopSound, ModernNeonVaultCompleteSound);
        MountModernSurface(modernNeonVaultView);
        // Attach the live WebView before collapsing the lobby: WPF must never
        // briefly expose the retired Blackjack surface between destinations.
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }

    private (bool Success, string Message) ApplyModernNeonVaultAction(ModernNeonVaultCommand command)
    {
        if (modernNeonVaultView is not { Ready: true } || modernNeonVaultMachine is null
            || progressionController is null) return (false, "Neon Vault is not ready.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                {
                    if (modernNeonVaultMachine.BonusActive)
                        return (false, "Finish free spins before changing the wager.");
                    decimal wager = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (wager > modernNeonVaultMachine.Bankroll)
                        return (false, "Not enough play money for that wager.");
                    modernNeonVaultBet = wager;
                    preferences = preferences with { NeonVaultBet = wager };
                    SavePreferences();
                    return (true, "Wager selected.");
                }
                case "sound.set":
                case "animations.set":
                {
                    AreaPresentationSettings area = preferences.Presentation("neon-vault");
                    bool enabled = command.Value == "true";
                    area = command.Action == "sound.set" ? area with { SoundEnabled = enabled }
                        : area with { AnimationsEnabled = enabled };
                    preferences = preferences.WithPresentation("neon-vault", area);
                    if (!area.SoundEnabled) modernNeonVaultAudio?.Stop();
                    SavePreferences();
                    return (true, "Presentation updated.");
                }
                case "spin":
                {
                    if (!modernNeonVaultMachine.BonusActive
                        && modernNeonVaultBet > modernNeonVaultMachine.Bankroll)
                        return (false, "Place an affordable wager before spinning.");
                    NeonVaultSpinResult spin = modernNeonVaultMachine.Spin(modernNeonVaultBet);
                    // Finalize once in C# BEFORE React receives an outcome.
                    session.ResetBankroll(spin.BankrollAfter);
                    selectedBet = Math.Min(selectedBet, session.Bankroll);
                    preferences = preferences with { NeonVaultBet = modernNeonVaultBet };
                    SavePreferences();
                    slotProgression ??= new SlotProgression(progressionController.Service);
                    ProgressionAward award = slotProgression.Process(spin);
                    progressionController.SaveAll();
                    modernNeonVaultLastSpin = spin;
                    modernNeonVaultAward = award;
                    modernNeonVaultLevelUp |= award.HasLevelUp;
                    modernNeonVaultAudioSpinId = spin.SpinId.ToString("D");
                    modernNeonVaultStopped.Clear();
                    modernNeonVaultOutcomePlayed = false;
                    modernNeonVaultAudio?.Play(SlotSound.Start,
                        preferences.Presentation("neon-vault").SoundEnabled);
                    if (!preferences.Presentation("neon-vault").AnimationsEnabled)
                        ModernNeonVaultCompleteSound(modernNeonVaultAudioSpinId);
                    RefreshGameUi(); RefreshProfileButton();
                    return (true, "Neon Vault spin settled.");
                }
                default: return (false, "Unsupported Neon Vault action.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            return (false, ex.Message);
        }
    }

    private void ModernNeonVaultStopSound(string id, int reel)
    {
        if (id != modernNeonVaultAudioSpinId || !modernNeonVaultStopped.Add(reel)) return;
        modernNeonVaultAudio?.Play(SlotSound.Stop,
            preferences.Presentation("neon-vault").SoundEnabled);
    }

    private void ModernNeonVaultCompleteSound(string id)
    {
        if (id != modernNeonVaultAudioSpinId || modernNeonVaultOutcomePlayed || modernNeonVaultLastSpin is null) return;
        modernNeonVaultOutcomePlayed = true;
        NeonVaultSpinResult result = modernNeonVaultLastSpin;
        bool enabled = preferences.Presentation("neon-vault").SoundEnabled;
        if (result.Blackout != NeonVaultBlackout.None)
            modernNeonVaultAudio?.Play(SlotSound.Blackout, enabled);
        else if (result.ExpandedReels.Count > 0 || result.SecurityOverrideReel.HasValue)
            modernNeonVaultAudio?.Play(SlotSound.Wild, enabled);
        modernNeonVaultAudio?.Play(result.Jackpot != NeonVaultJackpot.None ? SlotSound.Jackpot
            : result.FreeSpinsAwarded > 0 ? SlotSound.FreeSpins
            : result.Payout > 0 ? SlotSound.Win : SlotSound.Loss, enabled);
    }

    private void OnModernNeonVaultFailed()
    {
        if (modernNeonVaultView?.Failed == true && IsLoaded) CloseModernNeonVault();
    }

    private void CloseModernNeonVault()
    {
        if (modernNeonVaultView is null) return;
        bool failed = modernNeonVaultView.Failed;
        modernNeonVaultView.Dispose();
        UnmountModernSurface(modernNeonVaultView);
        modernNeonVaultView = null;
        modernNeonVaultMusic?.Dispose(); modernNeonVaultMusic = null;
        modernNeonVaultAudio?.Dispose(); modernNeonVaultAudio = null;
        preferences = preferences with { NeonVaultBet = modernNeonVaultBet };
        SavePreferences();
        // On a deliberate exit the old transient session ends. On a browser
        // fault the same machine/receipt ledger is reused on the next opening.
        if (!failed)
        {
            modernNeonVaultMachine = null;
            modernNeonVaultCommands = null;
            modernNeonVaultLastSpin = null;
            modernNeonVaultAward = null;
        }
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        if (failed) StatusText.Text = "Neon Vault interrupted. Reopen to resume the C# result.";
        SyncHostMusic(); NormalizeBetForTable(); RefreshGameUi();
        ApplyBetLimitState(); RefreshProfileButton();
        if (modernNeonVaultLevelUp)
        {
            modernNeonVaultLevelUp = false;
            Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
        }
        modeRegistry.NotifyStateChanged();
    }
}
