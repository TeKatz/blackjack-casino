using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernReelsView? modernReelsView;
    private SlotMachineSession? modernReelsMachine;
    private SlotSpinResult? modernReelsLastSpin;
    private ProgressionAward? modernReelsAward;
    private decimal modernReelsBet;
    private BackgroundMusic? modernReelsMusic;
    private SlotAudio? modernReelsAudio;
    private string? modernReelsAudioSpinId;
    private readonly HashSet<int> modernReelsStopped = [];
    private bool modernReelsOutcomePlayed;
    private bool modernReelsLevelUp;
    private bool ModernReelsOpen => modernReelsView is not null;

    private void OpenMidnightReels()
    {
        if (session.RoundInProgress || progressionController is null || ModernReelsOpen
            || ModernBlackjackOpen || EmpirePageOpen || KniffelPageOpen
            || MetaPageOpen || embeddedGameShell is not null) return;
        if (!ModernReelsView.IsAvailable)
        {
            EmbeddedSlots_Click(this, new RoutedEventArgs());
            return;
        }
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        slotProgression ??= new SlotProgression(progressionController.Service);
        modernReelsMachine = new SlotMachineSession(session.Bankroll);
        modernReelsBet = Math.Clamp(preferences.SlotBet, SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet);
        if (modernReelsMachine.Bankroll >= SlotMachineRules.MinimumBet)
            modernReelsBet = Math.Min(modernReelsBet, decimal.Floor(modernReelsMachine.Bankroll * 100) / 100);
        modernReelsLastSpin = null;
        modernReelsAward = null;
        modernReelsLevelUp = false;
        modernReelsAudioSpinId = null;
        modernReelsOutcomePlayed = false;
        modernReelsStopped.Clear();
        modernReelsAudio = new SlotAudio();
        AreaPresentationSettings presentation = preferences.Presentation("midnight-reels");
        modernReelsMusic = new BackgroundMusic(Soundtrack.Reels);
        modernReelsMusic.Start(presentation.MusicEnabled, presentation.MusicVolume);
        hostMusic.Mute();

        // Detach the lobby browser before hosting the new WebView, avoiding the
        // first-layout/hover deadlock that once affected Casino Empire.
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        modernReelsView = new ModernReelsView(
            () => ModernReelsSnapshot.Create(modernReelsMachine!, modernReelsBet,
                modernReelsLastSpin, modernReelsAward, progressionController.Profile,
                preferences.Presentation("midnight-reels").SoundEnabled,
                preferences.Presentation("midnight-reels").AnimationsEnabled,
                Localization.Language == UiLanguagePreference.German ? "de" : "en"),
            ApplyModernReelsAction, CloseModernReels, OnModernReelsFailed,
            ModernReelsStopSound, ModernReelsCompleteSound);
        MountModernSurface(modernReelsView);
        // Hiding the lobby raises a host-music synchronization event before
        // the new view is attached. Reapply the mute now that Reels owns audio.
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }

    private (bool Success, string Message) ApplyModernReelsAction(ModernReelsCommand command)
    {
        if (modernReelsView is not { Ready: true } || modernReelsMachine is null
            || progressionController is null) return (false, "Reels are not ready.");
        try
        {
            switch (command.Action)
            {
                case "bet.set":
                    decimal wager = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    if (wager > modernReelsMachine.Bankroll)
                        return (false, "Not enough play money for that wager.");
                    modernReelsBet = wager;
                    preferences = preferences with { SlotBet = wager };
                    SavePreferences();
                    return (true, "Wager selected.");
                case "sound.set":
                case "animations.set":
                    AreaPresentationSettings current = preferences.Presentation("midnight-reels");
                    bool enabled = command.Value == "true";
                    current = command.Action == "sound.set"
                        ? current with { SoundEnabled = enabled }
                        : current with { AnimationsEnabled = enabled };
                    preferences = preferences.WithPresentation("midnight-reels", current);
                    if (!current.SoundEnabled) modernReelsAudio?.Stop();
                    SavePreferences();
                    return (true, "Presentation updated.");
                case "spin":
                    if (modernReelsBet < SlotMachineRules.MinimumBet
                        || modernReelsBet > SlotMachineRules.MaximumBet
                        || modernReelsBet > modernReelsMachine.Bankroll)
                        return (false, "Place an affordable wager before spinning.");

                    SlotSpinResult result = modernReelsMachine.Spin(modernReelsBet);
                    // The old synchronous transaction order is preserved. The
                    // browser receives only a committed, already-persisted result.
                    session.ResetBankroll(result.BankrollAfter);
                    preferences = preferences with { SlotBet = result.Bet };
                    selectedBet = Math.Min(selectedBet, session.Bankroll);
                    SavePreferences();
                    slotProgression ??= new SlotProgression(progressionController.Service);
                    ProgressionAward award = slotProgression.Process(result);
                    progressionController.SaveAll();
                    modernReelsLastSpin = result;
                    modernReelsAward = award;
                    modernReelsLevelUp |= award.HasLevelUp;
                    modernReelsAudioSpinId = result.SpinId.ToString("D");
                    modernReelsStopped.Clear();
                    modernReelsOutcomePlayed = false;
                    bool sounds = preferences.Presentation("midnight-reels").SoundEnabled;
                    modernReelsAudio?.Play(SlotSound.Start, sounds);
                    if (!preferences.Presentation("midnight-reels").AnimationsEnabled)
                        ModernReelsCompleteSound(modernReelsAudioSpinId);
                    RefreshGameUi();
                    RefreshProfileButton();
                    return (true, "Spin settled.");
                default:
                    return (false, "Unsupported reels command.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or OverflowException)
        {
            return (false, ex.Message);
        }
    }

    private void ModernReelsStopSound(string spinId, int reel)
    {
        if (spinId != modernReelsAudioSpinId || !modernReelsStopped.Add(reel)) return;
        modernReelsAudio?.Play(SlotSound.Stop,
            preferences.Presentation("midnight-reels").SoundEnabled);
    }

    private void ModernReelsCompleteSound(string spinId)
    {
        if (spinId != modernReelsAudioSpinId || modernReelsOutcomePlayed
            || modernReelsLastSpin is null) return;
        modernReelsOutcomePlayed = true;
        SlotSpinResult result = modernReelsLastSpin;
        SlotSound outcome = result.Multiplier == 100 ? SlotSound.Jackpot
            : result.Net > 0 ? SlotSound.Win
            : result.Net == 0 ? SlotSound.Push : SlotSound.Loss;
        modernReelsAudio?.Play(outcome, preferences.Presentation("midnight-reels").SoundEnabled);
    }

    private void OnModernReelsFailed()
    {
        if (modernReelsView?.Failed == true && IsLoaded)
            CloseModernReels();
    }

    private void CloseModernReels()
    {
        if (modernReelsView is null) return;
        bool failed = modernReelsView.Failed;
        modernReelsView.Dispose();
        UnmountModernSurface(modernReelsView);
        modernReelsView = null;
        modernReelsMusic?.Dispose();
        modernReelsMusic = null;
        modernReelsAudio?.Dispose();
        modernReelsAudio = null;
        if (modernReelsMachine is not null)
            preferences = preferences with { SlotBet = modernReelsBet };
        modernReelsMachine = null;
        modernReelsLastSpin = null;
        modernReelsAward = null;
        modernReelsAudioSpinId = null;
        SavePreferences();
        if (failed)
        {
            // A committed spin has already been synchronized to the shared
            // bankroll. The native fallback opens at that exact balance.
            EmbeddedSlots_Click(this, new RoutedEventArgs());
        }
        else if (lobby is not null)
        {
            lobby.Visibility = Visibility.Visible;
        }
        SyncHostMusic();
        NormalizeBetForTable();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
        if (modernReelsLevelUp)
        {
            modernReelsLevelUp = false;
            Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
        }
        modeRegistry.NotifyStateChanged();
    }
}
