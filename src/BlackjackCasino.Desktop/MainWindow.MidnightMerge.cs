using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernMergeView? modernMergeView;
    private MergeBoard? modernMergeBoard;
    private ModernMergeSession? modernMergeCommands;
    private MergeActionResult? modernMergeLastAction;
    private readonly object modernMergeAudioOwner = new();
    private static readonly IReadOnlyDictionary<MergeCue, AudioClip> ModernMergeCues =
        Enum.GetValues<MergeCue>().ToDictionary(x => x, MergeSoundSynthesis.Build);
    private string modernMergeFeedback = "";
    private long modernMergePlayerXp, modernMergeSeasonXp;
    private bool modernMergeProgressionSaveFailed, modernMergeLevelUp;

    private bool ModernMergeOpen => modernMergeView is not null;

    private void OpenMidnightMerge()
    {
        if (session.RoundInProgress || ModernMergeOpen || MetaPageOpen || embeddedGameShell is not null
            || EmpirePageOpen || KniffelPageOpen || ModernBlackjackOpen || ModernNeonVaultOpen
            || ModernNineLivesOpen || ModernReelsOpen) return;

        if (!ModernMergeView.IsAvailable)
        {
            StatusText.Text = "Midnight Merge frontend missing. Reinstall the Development build.";
            if (!testSession)
                MessageBox.Show(this, StatusText.Text, "Midnight Merge unavailable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();

        if (modernMergeBoard is null)
        {
            void PersistMerge(MergeGameState candidate)
            {
                // The current atomic player-state writer and save schema remain unchanged.
                var updated = preferences with
                {
                    MidnightMerge = candidate,
                    Empire = empire?.Snapshot ?? preferences.Empire,
                    Bankroll = session.Bankroll,
                };
                if (!testSession) PlayerStateStore.SaveOrThrow(updated);
                preferences = updated;
            }
            try
            {
                modernMergeBoard = new MergeBoard(preferences.MidnightMerge, PersistMerge);
                PersistMerge(modernMergeBoard.Snapshot);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                modernMergeBoard = null;
                StatusText.Text = Localization.T("Merge.SaveFailed");
                return;
            }
            modernMergeCommands = new ModernMergeSession();
            modernMergeLastAction = null;
            modernMergeFeedback = Localization.T("Merge.Help");
            modernMergePlayerXp = modernMergeSeasonXp = 0;
            modernMergeProgressionSaveFailed = false;
        }

        SyncModernMergeProgression(modernMergeBoard.Snapshot);
        modernMergeView = new ModernMergeView(
            modernMergeCommands!,
            () => ModernMergeSnapshot.Create(modernMergeBoard!, progressionController?.Profile,
                preferences.Presentation("midnight-merge").SoundEnabled,
                preferences.Presentation("midnight-merge").AnimationsEnabled,
                modernMergeFeedback, modernMergePlayerXp, modernMergeSeasonXp,
                modernMergeProgressionSaveFailed, modernMergeLastAction),
            ApplyModernMergeAction,
            () => modernMergeBoard!.Snapshot.Revision,
            CloseModernMerge,
            OnModernMergeFailed);

        MountModernSurface(modernMergeView);
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        keepHostMusicForEmbeddedGame = true;
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }

    private void SyncModernMergeProgression(MergeGameState state)
    {
        if (progressionController is null) return;
        try
        {
            MergeProfileSyncResult synced = progressionController.ProcessMidnightMerge(state);
            CreditProgressionBankrollReward(synced.Award.BankrollEarned, "Midnight Merge reward");
            modernMergeProgressionSaveFailed = false;
            if (synced.Award.PlayerExperience > 0 || synced.Award.SeasonExperience > 0)
            {
                modernMergePlayerXp = synced.Award.PlayerExperience;
                modernMergeSeasonXp = synced.Award.SeasonExperience;
            }
            modernMergeLevelUp |= synced.Award.HasLevelUp;
            if (synced.Changed) RefreshProfileButton();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Board persistence succeeded already. The receipt ledger in the
            // player profile will reconcile this order on the next opening.
            modernMergeProgressionSaveFailed = true;
        }
    }

    private (bool Success, string Message, long Revision) ApplyModernMergeAction(ModernMergeCommand command)
    {
        if (modernMergeView is not { Ready: true } || modernMergeBoard is null)
            return (false, "Midnight Merge is not ready.", modernMergeBoard?.Snapshot.Revision ?? 0);
        try
        {
            if (command.Action is "sound.set" or "animations.set")
            {
                AreaPresentationSettings area = preferences.Presentation("midnight-merge");
                bool enabled = command.Value == "true";
                area = command.Action == "sound.set" ? area with { SoundEnabled = enabled }
                    : area with { AnimationsEnabled = enabled };
                preferences = preferences.WithPresentation("midnight-merge", area);
                SavePreferences();
                return (true, "Presentation updated.", modernMergeBoard.Snapshot.Revision);
            }

            MergeActionResult outcome = command.Action switch
            {
                "spawn" => modernMergeBoard.TrySpawn(command.ProducerId!),
                "drop" => modernMergeBoard.TryDrop(Guid.Parse(command.ItemId!), command.Target!.Value),
                "deliver" => modernMergeBoard.TryComplete(Guid.Parse(command.OrderId!)),
                _ => new MergeActionResult(MergeOutcome.UnknownDefinition),
            };
            if (!outcome.Success)
            {
                modernMergeFeedback = Localization.T("Merge.Feedback." + outcome.Outcome);
                ModernMergeSound(MergeCue.Invalid);
                return (false, modernMergeFeedback, modernMergeBoard.Snapshot.Revision);
            }

            modernMergeLastAction = outcome;
            modernMergeFeedback = outcome.Outcome == MergeOutcome.OrderCompleted
                ? Localization.F("Merge.Reward", outcome.Reward)
                : Localization.T("Merge.Feedback." + outcome.Outcome);
            SyncModernMergeProgression(modernMergeBoard.Snapshot);
            if (outcome.Milestones is { Count: > 0 })
            {
                MergeArea area = MergeProgression.ActiveArea(MergeCatalog.Default, modernMergeBoard.Snapshot.RenovationPoints);
                modernMergeFeedback = Localization.F("Merge.Milestone",
                    Localization.T(MergeProgression.Current(area, modernMergeBoard.Snapshot.RenovationPoints).DisplayName),
                    outcome.Reward);
            }

            ModernMergeSound(outcome.Outcome switch
            {
                MergeOutcome.Spawned => MergeCue.Spawn,
                MergeOutcome.Moved => MergeCue.Drop,
                MergeOutcome.Merged when outcome.DefinitionId is { } id
                    && MergeCatalog.Default.Items[id].Tier >= 4 => MergeCue.TierUp,
                MergeOutcome.Merged => MergeCue.Merge,
                _ => outcome.Milestones is { Count: > 0 } ? MergeCue.Milestone : MergeCue.Order,
            });
            RefreshProfileButton();
            RefreshGameUi();
            return (true, modernMergeFeedback, modernMergeBoard.Snapshot.Revision);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            modernMergeFeedback = Localization.T("Merge.SaveFailed");
            ModernMergeSound(MergeCue.Invalid);
            return (false, modernMergeFeedback, modernMergeBoard.Snapshot.Revision);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            ModernMergeSound(MergeCue.Invalid);
            return (false, ex.Message, modernMergeBoard.Snapshot.Revision);
        }
    }

    private void ModernMergeSound(MergeCue cue)
    {
        if (preferences.Presentation("midnight-merge").SoundEnabled)
            CasinoAudio.Play(ModernMergeCues[cue], modernMergeAudioOwner, .75);
    }

    private void OnModernMergeFailed()
    {
        if (modernMergeView?.Failed == true && IsLoaded) CloseModernMerge();
    }

    private void CloseModernMerge()
    {
        if (modernMergeView is null) return;
        bool failed = modernMergeView.Failed;
        modernMergeView.Dispose();
        UnmountModernSurface(modernMergeView);
        modernMergeView = null;
        CasinoAudio.Mixer.Stop(modernMergeAudioOwner);
        if (!failed)
        {
            modernMergeBoard = null;
            modernMergeCommands = null;
            modernMergeLastAction = null;
        }

        keepHostMusicForEmbeddedGame = false;
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        if (failed)
            StatusText.Text = "Midnight Merge browser interrupted. Reopen to resume the saved C# board.";
        SyncHostMusic();
        NormalizeBetForTable();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
        if (modernMergeLevelUp)
        {
            modernMergeLevelUp = false;
            Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
        }
        modeRegistry.NotifyStateChanged();
    }
}
