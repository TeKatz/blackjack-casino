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
    private static readonly Lazy<AudioClip> DiceFiveOfAKindCue = new(() => CasinoSoundSynthesis.Build(TableCue.DiceFiveOfAKind));
    private readonly List<(UIElement Element, bool Enabled)> kniffelCoveredElements = [];
    private bool KniffelPageOpen => kniffelView is not null;
    private bool KniffelCasinoMode => preferences.KniffelCasinoSelected && preferences.KniffelTable is not null;
    internal decimal DiceCasinoBankroll => session.Bankroll;
    internal KniffelTableState? DiceTableCheckpoint => preferences.KniffelTable;
    private bool kniffelAdvancing;
    private KniffelSetupView? kniffelSetupView;

    private void OpenKniffel()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        CreateKniffelView();
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
        RefreshKniffelTable();
        if (KniffelCasinoMode) AdvanceKniffelTable();
    }

    private void CreateKniffelView()
    {
        bool casino = KniffelCasinoMode;
        KniffelState checkpoint = casino ? preferences.KniffelTable!.Player : preferences.Kniffel;
        kniffelView = new KniffelView(checkpoint, state =>
        {
            if (casino)
            {
                KniffelTableState current = preferences.KniffelTable!;
                if (current.Settled) return;
                var next = preferences with { KniffelTable = KniffelTable.WithPlayer(current, state), Bankroll = session.Bankroll };
                PersistKniffelPreferences(next);
            }
            else
            {
                preferences = preferences with { Kniffel = state };
                if (!testSession) SavePreferences();
            }
        }, CloseKniffel, () => AnimationsEnabled, () =>
        {
            if (SoundEnabled) CasinoAudio.Play(DiceRollCue.Value, kniffelAudioOwner, .75);
        }, AwardKniffelCompletion, ShowKniffelSetup, _ => { if (casino) AdvanceKniffelTable(); }, persistInitialState: !casino,
        fiveOfAKindSound: () => { if (SoundEnabled) CasinoAudio.Play(DiceFiveOfAKindCue.Value, kniffelAudioOwner, .85); });
    }

    private void PersistKniffelPreferences(AppPreferences next)
    {
        if (!testSession) PlayerStateStore.SaveOrThrow(next);
        preferences = next;
    }
    private void PersistKniffelCash(KniffelCashChange change)
    {
        PersistKniffelPreferences(preferences with { Bankroll = change.Bankroll, KniffelTable = change.Table, KniffelCasinoSelected = true });
        session.ResetBankroll(change.Bankroll);
    }
    private void ReplaceKniffelView()
    {
        kniffelView?.Dispose();
        embeddedGameHost!.Content = null;
        kniffelSetupView = null;
        CreateKniffelView();
        embeddedGameHost.Content = kniffelView;
        embeddedGameHost.UpdateLayout();
        KeyboardNavigation.SetTabNavigation(kniffelView, KeyboardNavigationMode.Cycle);
        RefreshKniffelTable();
        if (KniffelCasinoMode) AdvanceKniffelTable();
    }
    private void ShowKniffelSetup()
    {
        if (kniffelView is null) return;
        kniffelSetupView = new KniffelSetupView(session.Bankroll, preferences.Kniffel, preferences.KniffelTable,
            fresh => ChangeKniffelGame(() =>
            {
                PersistKniffelPreferences(preferences with { KniffelCasinoSelected = false,
                    Kniffel = fresh ? new KniffelGame().Snapshot : preferences.Kniffel, Bankroll = session.Bankroll });
                ReplaceKniffelView();
            }),
            stake => ChangeKniffelGame(() =>
            {
                if (preferences.KniffelTable is KniffelTableState previous) progressionController?.ProcessKniffelTable(previous);
                KniffelCash.Start(session.Bankroll, preferences.KniffelTable, stake, PersistKniffelCash);
                ReplaceKniffelView();
            }),
            () => ChangeKniffelGame(() =>
            {
                PersistKniffelPreferences(preferences with { KniffelCasinoSelected = true, Bankroll = session.Bankroll });
                ReplaceKniffelView();
            }),
            () => ChangeKniffelGame(() =>
            {
                KniffelTableState current = preferences.KniffelTable!;
                KniffelCash.Apply(session.Bankroll, current, KniffelTable.Forfeit(current), PersistKniffelCash);
                RecordKniffelTable(preferences.KniffelTable!);
                ReplaceKniffelView();
            }),
            () => kniffelView?.HideGameSetup());
        kniffelView.ShowGameSetup(kniffelSetupView);
    }
    private void ChangeKniffelGame(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or OverflowException)
        {
            kniffelSetupView?.SetNotice("The change was not completed: " + ex.Message);
        }
    }
    private string RecordKniffelTable(KniffelTableState table)
    {
        try
        {
            ProgressionAward award = progressionController?.ProcessKniffelTable(table) ?? ProgressionAward.None;
            kniffelLevelUp |= award.HasLevelUp;
            return award.PlayerExperience > 0 ? $"+{award.PlayerExperience} XP · +{award.SeasonExperience} Season XP earned!"
                : table.Forfeited ? "No completion XP for forfeited tables." : "Completion rewards already credited.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Cash is saved. Reopen this table to retry saving its statistics and XP.";
        }
    }
    private void RefreshKniffelTable(string? reward = null)
    {
        if (!KniffelCasinoMode || kniffelView is null) return;
        KniffelTableState table = preferences.KniffelTable!;
        if (kniffelView.Game.GameId != table.Player.GameId) return;
        if (table.Settled) kniffelView.SetCompletionNotice(reward ?? RecordKniffelTable(table));
        kniffelView.UpdateTable(table, session.Bankroll, KniffelTable.NeedsOpponents(table));
        if (table.Settled)
        {
            string result = table.Forfeited ? "Table forfeited. Your entry fee was lost; no prize was paid."
                : $"Table complete · Place #{table.Placement} · Paid {KniffelSetupView.Money(table.Payout)} · Net {KniffelSetupView.Money(table.Payout - table.Stake)} · Bankroll {KniffelSetupView.Money(session.Bankroll)}.";
            kniffelView.SetNotice(result + " " + kniffelView.CompletionNotice);
        }
    }
    private async void AdvanceKniffelTable()
    {
        if (kniffelAdvancing || preferences.KniffelTable is not KniffelTableState snapshot) return;
        if (!KniffelTable.NeedsOpponents(snapshot))
        {
            RecordKniffelTable(snapshot);
            RefreshKniffelTable();
            return;
        }
        kniffelAdvancing = true;
        RefreshKniffelTable();
        try
        {
            KniffelTableState next = await Task.Run(() => KniffelTable.Advance(snapshot));
            KniffelTableState? current = preferences.KniffelTable;
            if (current is null || current.Player.GameId != snapshot.Player.GameId || current.Settled) return;
            // A paused table must not replace another game's live bankroll/round.
            // Its deterministic AI turn is recovered when the paid table is resumed.
            if (!KniffelCasinoMode || kniffelView is null || session.RoundInProgress || embeddedGameShell is not null) return;
            KniffelCash.Apply(session.Bankroll, current, next, change =>
            {
                // A result can settle while a separately saved casual game is on screen.
                bool selected = preferences.KniffelCasinoSelected;
                PersistKniffelPreferences(preferences with { Bankroll = change.Bankroll, KniffelTable = change.Table, KniffelCasinoSelected = selected });
                session.ResetBankroll(change.Bankroll);
            });
            kniffelView.CelebrateOpponents(snapshot, next);
            string reward = RecordKniffelTable(next);
            RefreshKniffelTable(reward);
            if (!next.Settled && KniffelCasinoMode)
            {
                string moves = string.Join(" · ", next.LastMoves);
                foreach (KniffelCategory category in KniffelScoring.Categories)
                    moves = moves.Replace(category.ToString(), KniffelView.CategoryName(category), StringComparison.Ordinal);
                kniffelView?.SetNotice(moves + ". Your turn.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException)
        {
            if (KniffelCasinoMode) kniffelView?.SetNotice("The opponents' turn could not be saved. Your score and entry are safe; return to the lobby and resume to retry.");
        }
        finally
        {
            kniffelAdvancing = false;
            if (preferences.KniffelTable is KniffelTableState current && current.Player.GameId != snapshot.Player.GameId && KniffelTable.NeedsOpponents(current))
                AdvanceKniffelTable();
        }
    }

    private string AwardKniffelCompletion(KniffelState state)
    {
        if (KniffelCasinoMode)
        {
            KniffelTableState table = preferences.KniffelTable!;
            return table.Settled ? RecordKniffelTable(table) : "The casino result will settle after all opponents finish.";
        }
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
        if (!KniffelCasinoMode) preferences = preferences with { Kniffel = kniffelView.Game.Snapshot };
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
        NormalizeBetForTable();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
        if (kniffelLevelUp)
        {
            kniffelLevelUp = false;
            AnimateLevelUpCue();
        }
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
