using System.Windows;
using System.Windows.Input;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private KniffelView? kniffelView;
    private BackgroundMusic? kniffelMusic;
    private readonly object kniffelAudioOwner = new();
    private bool kniffelLevelUp;
    private static readonly Lazy<AudioClip> DiceRollCue = new(() => CasinoSoundSynthesis.Build(TableCue.DiceRoll));
    private readonly List<(UIElement Element, bool Enabled)> kniffelCoveredElements = [];
    private bool KniffelPageOpen => kniffelView is not null;

    private void OpenKniffel()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        kniffelView = new KniffelView(preferences.Kniffel, state =>
        {
            preferences = preferences with { Kniffel = state };
            if (!testSession) SavePreferences();
        }, CloseKniffel, () => AnimationsEnabled, () =>
        {
            if (SoundEnabled) CasinoAudio.Play(DiceRollCue.Value, kniffelAudioOwner, .75);
        }, AwardKniffelCompletion);
        backgroundMusic.SetEnabled(false);
        kniffelMusic = new BackgroundMusic(Soundtrack.DiceLounge);
        kniffelMusic.Start(preferences.MusicEnabled, preferences.MusicVolume);
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>().Where(child => child != embeddedGameOverlay))
        {
            kniffelCoveredElements.Add((covered, covered.IsEnabled));
            covered.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = kniffelView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        KeyboardNavigation.SetTabNavigation(kniffelView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(kniffelView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetDirectionalNavigation(kniffelView, KeyboardNavigationMode.Contained);
    }

    private string AwardKniffelCompletion(KniffelState state)
    {
        if (progressionController is null) return "";
        try
        {
            ProgressionAward award = progressionController.ProcessKniffel(state);
            kniffelLevelUp |= award.HasLevelUp;
            return award.PlayerExperience > 0
                ? $"+{award.PlayerExperience} XP · +{award.SeasonExperience} Season XP earned!"
                : $"Completion reward already credited: {KniffelProgression.PlayerExperience(KniffelGame.Restore(state).Total)} XP.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "XP could not be saved. Reopen this completed game to retry your reward.";
        }
    }

    private void CloseKniffel()
    {
        if (kniffelView is null) return;
        preferences = preferences with { Kniffel = kniffelView.Game.Snapshot };
        kniffelView.Dispose();
        CasinoAudio.Mixer.Stop(kniffelAudioOwner);
        kniffelMusic?.Dispose();
        kniffelMusic = null;
        backgroundMusic.SetEnabled(preferences.MusicEnabled);
        backgroundMusic.SetVolume(preferences.MusicVolume);
        embeddedGameHost!.Content = null;
        kniffelView = null;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach (var (element, enabled) in kniffelCoveredElements) element.SetCurrentValue(IsEnabledProperty, enabled);
        kniffelCoveredElements.Clear();
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        if (!testSession) SavePreferences();
        RefreshProfileButton();
        if (kniffelLevelUp)
        {
            kniffelLevelUp = false;
            AnimateLevelUpCue();
        }
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
