using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernNineLivesView? modernNineLivesView;
    private NineLivesSession? modernNineLivesMachine;
    private ModernNineLivesSession? modernNineLivesCommands;
    private NineLivesSpinResult? modernNineLivesLastSpin;
    private ProgressionAward? modernNineLivesAward;
    private decimal modernNineLivesBet;
    private SlotAudio? modernNineLivesAudio;
    private BackgroundMusic? modernNineLivesMusic;
    private string? modernNineLivesAudioSpinId;
    private readonly HashSet<int> modernNineLivesStopped = [];
    private bool modernNineLivesOutcomePlayed, modernNineLivesLevelUp;
    private bool ModernNineLivesOpen => modernNineLivesView is not null;

    private void OpenModernNineLives()
    {
        if (session.RoundInProgress || progressionController is null || ModernNineLivesOpen
            || ModernReelsOpen || ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null) return;
        if (!ModernNineLivesView.IsAvailable)
        {
            ShowBrowserRecovery("Nine Lives", assetsAvailable: false, OpenModernNineLives);
            return;
        }
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        slotProgression ??= new SlotProgression(progressionController.Service);
        if (modernNineLivesMachine is null)
        {
            modernNineLivesMachine = new NineLivesSession(session.Bankroll);
            modernNineLivesCommands = new ModernNineLivesSession();
            decimal saved = Math.Clamp(preferences.NineLivesBet,
                NineLivesRules.MinimumBet, NineLivesRules.MaximumBet);
            modernNineLivesBet = NineLivesRules.Bets.OrderBy(bet => Math.Abs(bet - saved)).First();
            modernNineLivesLastSpin = null;
            modernNineLivesAward = null;
        }
        else
        {
            // Browser recovery preserves bonus spins; lobby changes to the shared
            // wallet must not resurrect a stale spendable balance.
            modernNineLivesMachine.SynchronizeBankroll(session.Bankroll);
        }
        modernNineLivesAudioSpinId = modernNineLivesLastSpin?.SpinId.ToString("D");
        modernNineLivesOutcomePlayed = modernNineLivesLastSpin is not null;
        modernNineLivesStopped.Clear();
        modernNineLivesAudio = new SlotAudio();
        var presentation = preferences.Presentation("nine-lives");
        modernNineLivesMusic = new BackgroundMusic(Soundtrack.Manor);
        modernNineLivesMusic.Start(presentation.MusicEnabled, presentation.MusicVolume);
        hostMusic.Mute();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        modernNineLivesView = new ModernNineLivesView(
            modernNineLivesCommands!,
            () => ModernNineLivesSnapshot.Create(modernNineLivesMachine!, modernNineLivesBet,
                modernNineLivesLastSpin, modernNineLivesAward, progressionController.Profile,
                preferences.Presentation("nine-lives").SoundEnabled,
                preferences.Presentation("nine-lives").AnimationsEnabled,
                Localization.Language == UiLanguagePreference.German ? "de" : "en"),
            ApplyModernNineLivesAction, CloseModernNineLives, OnModernNineLivesFailed,
            ModernNineLivesStopSound, ModernNineLivesCompleteSound);
        MountModernSurface(modernNineLivesView);
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }

    private (bool Success, string Message) ApplyModernNineLivesAction(ModernNineLivesCommand command)
    {
        if (modernNineLivesView is not { Ready: true } || modernNineLivesMachine is null
            || progressionController is null) return (false, "Nine Lives is not ready.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                {
                    if (modernNineLivesMachine.BonusActive)
                        return (false, "Finish free spins before changing the wager.");
                    decimal wager = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (wager > modernNineLivesMachine.Bankroll)
                        return (false, "Not enough play money for that wager.");
                    modernNineLivesBet = wager;
                    preferences = preferences with { NineLivesBet = wager };
                    SavePreferences();
                    return (true, "Wager selected.");
                }
                case "sound.set":
                case "animations.set":
                {
                    AreaPresentationSettings area = preferences.Presentation("nine-lives");
                    bool enabled = command.Value == "true";
                    area = command.Action == "sound.set" ? area with { SoundEnabled = enabled }
                        : area with { AnimationsEnabled = enabled };
                    preferences = preferences.WithPresentation("nine-lives", area);
                    if (!area.SoundEnabled) modernNineLivesAudio?.Stop();
                    SavePreferences();
                    return (true, "Presentation updated.");
                }
                case "spin":
                {
                    if (!modernNineLivesMachine.BonusActive
                        && modernNineLivesBet > modernNineLivesMachine.Bankroll)
                        return (false, "Place an affordable wager before spinning.");
                    NineLivesSpinResult spin = modernNineLivesMachine.Spin(modernNineLivesBet);
                    // Finalize once in C# BEFORE React receives an outcome.
                    session.ResetBankroll(spin.BankrollAfter);
                    selectedBet = Math.Min(selectedBet, session.Bankroll);
                    preferences = preferences with { NineLivesBet = modernNineLivesBet };
                    SavePreferences();
                    slotProgression ??= new SlotProgression(progressionController.Service);
                    ProgressionAward award = slotProgression.Process(spin);
                    progressionController.SaveAll();
                    modernNineLivesLastSpin = spin;
                    modernNineLivesAward = award;
                    modernNineLivesLevelUp |= award.HasLevelUp;
                    modernNineLivesAudioSpinId = spin.SpinId.ToString("D");
                    modernNineLivesStopped.Clear();
                    modernNineLivesOutcomePlayed = false;
                    modernNineLivesAudio?.Play(SlotSound.Start,
                        preferences.Presentation("nine-lives").SoundEnabled);
                    if (!preferences.Presentation("nine-lives").AnimationsEnabled)
                        ModernNineLivesCompleteSound(modernNineLivesAudioSpinId);
                    RefreshGameUi(); RefreshProfileButton();
                    return (true, "Nine Lives spin settled.");
                }
                default: return (false, "Unsupported Nine Lives action.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            return (false, ex.Message);
        }
    }

    private void ModernNineLivesStopSound(string id, int reel)
    {
        if (id != modernNineLivesAudioSpinId || !modernNineLivesStopped.Add(reel)) return;
        modernNineLivesAudio?.Play(SlotSound.Stop,
            preferences.Presentation("nine-lives").SoundEnabled);
    }

    private void ModernNineLivesCompleteSound(string id)
    {
        if (id != modernNineLivesAudioSpinId || modernNineLivesOutcomePlayed || modernNineLivesLastSpin is null) return;
        modernNineLivesOutcomePlayed = true;
        NineLivesSpinResult result = modernNineLivesLastSpin;
        bool enabled = preferences.Presentation("nine-lives").SoundEnabled;
        if (result.PounceWildCell.HasValue) modernNineLivesAudio?.Play(SlotSound.Wild, enabled);
        modernNineLivesAudio?.Play(result.FullGridFrenzy ? SlotSound.Jackpot
            : result.BonusSpinsAwarded > 0 ? SlotSound.FreeSpins
            : result.Payout > 0 ? SlotSound.Win : SlotSound.Loss, enabled);
    }

    private void OnModernNineLivesFailed()
    {
        if (modernNineLivesView?.Failed != true || !IsLoaded) return;
        CloseModernNineLives();
        ShowBrowserRecovery("Nine Lives", ModernNineLivesView.IsAvailable, OpenModernNineLives);
    }

    private void CloseModernNineLives()
    {
        if (modernNineLivesView is null) return;
        bool failed = modernNineLivesView.Failed;
        modernNineLivesView.Dispose();
        UnmountModernSurface(modernNineLivesView);
        modernNineLivesView = null;
        modernNineLivesMusic?.Dispose(); modernNineLivesMusic = null;
        modernNineLivesAudio?.Dispose(); modernNineLivesAudio = null;
        preferences = preferences with { NineLivesBet = modernNineLivesBet };
        SavePreferences();
        // On a deliberate exit the old transient session ends. On a browser
        // fault the same machine/receipt ledger is reused on the next opening.
        if (!failed)
        {
            modernNineLivesMachine = null;
            modernNineLivesCommands = null;
            modernNineLivesLastSpin = null;
            modernNineLivesAward = null;
        }
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        if (failed) StatusText.Text = "Nine Lives interrupted. Reopen to resume the C# result.";
        SyncHostMusic(); NormalizeBetForTable(); RefreshGameUi();
        ApplyBetLimitState(); RefreshProfileButton();
        if (modernNineLivesLevelUp)
        {
            modernNineLivesLevelUp = false;
            Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
        }
        modeRegistry.NotifyStateChanged();
    }
}
