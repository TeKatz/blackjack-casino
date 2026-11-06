using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernDiceView? modernDiceView;
    private ModernDiceSession? modernDiceCommands;
    private KniffelGame? modernDiceGame;
    private BackgroundMusic? modernDiceMusic;
    private string modernDiceNotice = "";
    private string modernDiceReward = "";
    private long modernDiceRollToken, modernDiceEventCounter;
    private int[] modernDiceRollingIndices = [];
    private readonly List<ModernDiceCelebration> modernDiceEvents = [];
    private readonly List<(UIElement Element, bool Enabled)> modernDiceCoveredElements = [];
    private bool modernDiceAdvancing;
    private bool ModernDiceOpen => modernDiceView is not null;

    private void OpenModernDice()
    {
        if (session.RoundInProgress || ModernDiceOpen || MetaPageOpen || embeddedGameShell is not null
            || EmpirePageOpen || ModernMergeOpen || ModernNineLivesOpen || ModernReelsOpen
            || ModernNeonVaultOpen || ModernBlackjackOpen) return;
        if (!ModernDiceView.IsAvailable)
        {
            StatusText.Text = "Midnight Dice frontend missing. Reinstall the Development build.";
            if (!testSession)
                MessageBox.Show(this, StatusText.Text, "Midnight Dice unavailable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        if (modernDiceCommands is null)
        {
            modernDiceCommands = new ModernDiceSession();
            LoadModernDiceGame();
            modernDiceEvents.Clear();
            modernDiceEventCounter = 0;
            modernDiceRollToken = 0;
            modernDiceRollingIndices = [];
        }
        else if (modernDiceGame is null) LoadModernDiceGame();

        // Profile ledgers are authoritative even if the previous UI disappeared
        // after the durable dice checkpoint but before a profile write succeeded.
        if (KniffelCasinoMode && preferences.KniffelTable is { Settled: true } completed)
            modernDiceReward = RecordKniffelTable(completed);
        else if (!KniffelCasinoMode && modernDiceGame is { IsComplete: true })
            modernDiceReward = AwardKniffelCompletion(modernDiceGame.Snapshot);

        modernDiceView = new ModernDiceView(
            modernDiceCommands,
            () => ModernDiceSnapshot.Create(modernDiceGame!,
                preferences.Kniffel, preferences.KniffelTable, preferences.KniffelCasinoSelected,
                session.Bankroll, progressionController?.Profile,
                CosmeticCatalog.Get(progressionController?.Profile.EquippedDiceSetId
                    ?? CosmeticCatalog.DefaultDiceSetId).VisualVariant,
                preferences.Presentation("kniffel"), modernDiceNotice,
                modernDiceReward, ModernDiceWaiting, modernDiceEvents,
                modernDiceRollToken, modernDiceRollingIndices),
            ApplyModernDiceAction, CloseModernDice, OnModernDiceFailed);

        foreach (UIElement element in CasinoRoot.Children.OfType<UIElement>()
            .Where(element => element != embeddedGameOverlay))
        {
            modernDiceCoveredElements.Add((element, element.IsEnabled));
            element.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = modernDiceView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        KeyboardNavigation.SetTabNavigation(modernDiceView, KeyboardNavigationMode.Cycle);
        modernDiceMusic = new BackgroundMusic(Soundtrack.DiceLounge);
        AreaPresentationSettings settings = preferences.Presentation("kniffel");
        modernDiceMusic.Start(settings.MusicEnabled, settings.MusicVolume);
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
        if (ModernDiceWaiting) AdvanceModernDiceOpponents();
    }

    private bool ModernDiceWaiting =>
        KniffelCasinoMode && preferences.KniffelTable is { Settled: false } table
        && KniffelTable.NeedsOpponents(table);

    private void LoadModernDiceGame()
    {
        KniffelState saved = KniffelCasinoMode ? preferences.KniffelTable!.Player : preferences.Kniffel;
        KniffelState normalized = KniffelGame.Normalize(saved);
        modernDiceGame = KniffelGame.Restore(normalized);
        if (!KniffelCasinoMode)
        {
            // Keep the same checkpoint migration semantics as the original WPF game.
            PersistKniffelPreferences(preferences with { Kniffel = normalized, Bankroll = session.Bankroll });
        }
        modernDiceNotice = modernDiceGame.IsComplete
            ? $"Game complete · {modernDiceGame.Total} points."
            : "Roll five dice, hold the ones you want, and record a box. Progress saves automatically.";
        modernDiceReward = "";
    }

    private (bool Success, string Message) ApplyModernDiceAction(ModernDiceCommand command)
    {
        if (modernDiceView is not { Ready: true } || modernDiceGame is null || modernDiceCommands is null)
            return (false, "Midnight Dice has not finished loading.");
        try
        {
            switch (command.Action)
            {
                case "sound.set":
                case "animations.set":
                case "music.set":
                {
                    AreaPresentationSettings settings = preferences.Presentation("kniffel");
                    bool enabled = command.Value == "true";
                    settings = command.Action switch
                    {
                        "sound.set" => settings with { SoundEnabled = enabled },
                        "animations.set" => settings with { AnimationsEnabled = enabled },
                        _ => settings with { MusicEnabled = enabled },
                    };
                    preferences = preferences.WithPresentation("kniffel", settings);
                    SavePreferences();
                    modernDiceMusic?.SetEnabled(settings.MusicEnabled);
                    return (true, "Presentation settings saved.");
                }
                case "casual.start":
                case "casual.resume":
                {
                    KniffelState casual = command.Action == "casual.start"
                        ? new KniffelGame().Snapshot : KniffelGame.Normalize(preferences.Kniffel);
                    PersistKniffelPreferences(preferences with
                    {
                        KniffelCasinoSelected = false, Kniffel = casual, Bankroll = session.Bankroll,
                    });
                    modernDiceGame = KniffelGame.Restore(casual);
                    modernDiceEvents.Clear();
                    modernDiceRollToken = 0;
                    modernDiceRollingIndices = [];
                    modernDiceReward = modernDiceGame.IsComplete
                        ? AwardKniffelCompletion(modernDiceGame.Snapshot) : "";
                    modernDiceNotice = "Casual game loaded. No entry fee.";
                    return (true, "Casual dice table ready.");
                }
                case "casino.start":
                {
                    if (ModernDiceWaiting || modernDiceAdvancing)
                        return (false, "Wait for the opponents to finish.");
                    if (preferences.KniffelTable is { Settled: true } previous)
                        _ = progressionController?.ProcessKniffelTable(previous);
                    decimal stake = decimal.Parse(command.Value!, CultureInfo.InvariantCulture);
                    KniffelCash.Start(session.Bankroll, preferences.KniffelTable, stake, PersistKniffelCash);
                    modernDiceGame = KniffelGame.Restore(preferences.KniffelTable!.Player);
                    modernDiceEvents.Clear();
                    modernDiceRollToken = 0;
                    modernDiceRollingIndices = [];
                    modernDiceNotice = "Casino table joined. Entry paid and saved.";
                    modernDiceReward = "";
                    RefreshGameUi(); RefreshProfileButton();
                    return (true, "Casino entry committed once.");
                }
                case "casino.resume":
                {
                    if (preferences.KniffelTable is not { } table)
                        return (false, "No paid table is saved.");
                    PersistKniffelPreferences(preferences with { KniffelCasinoSelected = true, Bankroll = session.Bankroll });
                    modernDiceGame = KniffelGame.Restore(table.Player);
                    modernDiceEvents.Clear();
                    modernDiceRollingIndices = [];
                    modernDiceNotice = table.Settled ? "Previous casino table restored." : "Casino table resumed without another entry fee.";
                    modernDiceReward = table.Settled ? RecordKniffelTable(table) : "";
                    if (ModernDiceWaiting) Dispatcher.BeginInvoke(new Action(AdvanceModernDiceOpponents));
                    return (true, "Paid table restored.");
                }
                case "casino.forfeit":
                {
                    if (!KniffelCasinoMode || preferences.KniffelTable is not { Settled: false } table
                        || modernDiceAdvancing || ModernDiceWaiting)
                        return (false, "This casino table cannot be forfeited now.");
                    KniffelCash.Apply(session.Bankroll, table, KniffelTable.Forfeit(table), PersistKniffelCash);
                    modernDiceReward = RecordKniffelTable(preferences.KniffelTable!);
                    modernDiceNotice = "Table forfeited. Your entry fee remains spent; no prize or completion XP.";
                    RefreshGameUi(); RefreshProfileButton();
                    return (true, "Casino forfeit persisted.");
                }
                case "roll":
                case "hold":
                case "score":
                case "scratch":
                    return ApplyModernDiceTurn(command);
                default: return (false, "Unsupported dice action.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidOperationException or OverflowException)
        {
            modernDiceNotice = ex is IOException or UnauthorizedAccessException
                ? "Could not save. No dice operation was applied; try again."
                : ex.Message;
            return (false, modernDiceNotice);
        }
    }

    private (bool Success, string Message) ApplyModernDiceTurn(ModernDiceCommand command)
    {
        if (modernDiceGame is null || modernDiceAdvancing || ModernDiceWaiting
            || KniffelCasinoMode && preferences.KniffelTable is { Settled: true })
            return (false, "Wait for the current table before playing.");

        KniffelState before = modernDiceGame.Snapshot;
        KniffelGame candidate = KniffelGame.Restore(before);
        int points = 0;
        KniffelCategory category = default;
        switch (command.Action)
        {
            case "roll": candidate.Roll(); break;
            case "hold": candidate.ToggleHold(command.Index!.Value); break;
            case "score":
            case "scratch":
                category = Enum.Parse<KniffelCategory>(command.Category!);
                points = candidate.Commit(category, command.Action == "scratch");
                break;
            default: return (false, "Unsupported dice action.");
        }

        KniffelState after = candidate.Snapshot;
        // Transaction boundary: persist the entire candidate with shared cash
        // BEFORE changing the live view, emitting animation or awarding XP.
        if (KniffelCasinoMode)
        {
            KniffelTableState table = preferences.KniffelTable!;
            KniffelTableState next = KniffelTable.WithPlayer(table, after);
            PersistKniffelPreferences(preferences with { KniffelTable = next, Bankroll = session.Bankroll });
        }
        else PersistKniffelPreferences(preferences with { Kniffel = after, Bankroll = session.Bankroll });
        modernDiceGame = candidate;

        switch (command.Action)
        {
            case "roll":
                modernDiceRollToken++;
                modernDiceRollingIndices = Enumerable.Range(0, 5).Where(i => !before.Held[i]).ToArray();
                modernDiceNotice = candidate.IsAdditionalKniffel
                    ? "Additional five of a kind! Commit any open box for the maximum and +50 bonus."
                    : candidate.RollsUsed == 3 ? "Third roll: choose a box and record its score."
                    : "Choose dice to hold, roll again, or record a score.";
                if (preferences.Presentation("kniffel").SoundEnabled)
                    CasinoAudio.Play(DiceRollCue.Value, kniffelAudioOwner, .75);
                break;
            case "hold":
                modernDiceNotice = $"Die {command.Index!.Value + 1} {(after.Held[command.Index.Value] ? "held" : "released")}.";
                break;
            default:
                CasinoFeedback.Menu(preferences.Presentation("kniffel").SoundEnabled);
                modernDiceNotice = $"{KniffelView.CategoryName(category)}: {points} points recorded."
                    + (candidate.IsComplete ? $" Sheet complete with {candidate.Total} points." : $" Turn {candidate.FilledCount + 1} next.");
                if (after.KniffelsScored > before.KniffelsScored)
                    AddModernDiceCelebration("You", before, after);
                if (candidate.IsComplete)
                {
                    modernDiceReward = KniffelCasinoMode
                        ? "Opponents are finishing the match."
                        : AwardKniffelCompletion(after);
                    if (!KniffelCasinoMode) RefreshProfileButton();
                }
                if (KniffelCasinoMode && ModernDiceWaiting)
                    _ = Dispatcher.BeginInvoke(new Action(AdvanceModernDiceOpponents));
                break;
        }
        return (true, "Dice state saved.");
    }

    private void AddModernDiceCelebration(string player, KniffelState before, KniffelState after)
    {
        if (after.KniffelsScored <= before.KniffelsScored) return;
        KniffelEntry? entry = after.Entries.FirstOrDefault(e =>
            before.Entries.All(previous => previous.Category != e.Category));
        if (entry is null) return;
        int bonus = (Math.Max(0, after.KniffelsScored - 1)
            - Math.Max(0, before.KniffelsScored - 1)) * 50;
        modernDiceEvents.Add(new(++modernDiceEventCounter, player, entry.Category.ToString(), entry.Points,
            bonus, before.RollsUsed > 0 ? before.Dice[0] : 0));
        if (modernDiceEvents.Count > 24) modernDiceEvents.RemoveAt(0);
        if (preferences.Presentation("kniffel").SoundEnabled)
            CasinoAudio.Play(DiceFiveOfAKindCue.Value, kniffelAudioOwner, .85);
    }

    private async void AdvanceModernDiceOpponents()
    {
        if (modernDiceAdvancing || !ModernDiceWaiting || modernDiceView is null) return;
        KniffelTableState snapshot = preferences.KniffelTable!;
        ModernDiceView view = modernDiceView;
        modernDiceAdvancing = true;
        view.Refresh();
        try
        {
            KniffelTableState next = await Task.Run(() => KniffelTable.Advance(snapshot));
            KniffelTableState? current = preferences.KniffelTable;
            if (!ReferenceEquals(view, modernDiceView) || current is null
                || current.Player.GameId != snapshot.Player.GameId || current.Settled
                || !KniffelCasinoMode || session.RoundInProgress || embeddedGameShell is not null) return;
            KniffelCash.Apply(session.Bankroll, current, next, PersistKniffelCash);
            for (int i = 0; i < 3; i++)
                AddModernDiceCelebration(KniffelTable.Names[i], snapshot.Opponents[i], next.Opponents[i]);
            modernDiceReward = next.Settled ? RecordKniffelTable(next) : "";
            modernDiceNotice = next.Settled
                ? next.Forfeited ? "Table forfeited." :
                    $"Casino match complete · Place #{next.Placement}. Prize {KniffelSetupView.Money(next.Payout)}."
                : string.Join(" · ", next.LastMoves) + ". Your turn.";
            modernDiceCommands?.Bump();
            modernDiceView?.Refresh();
            RefreshGameUi(); RefreshProfileButton();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidOperationException or OverflowException)
        {
            if (ReferenceEquals(view, modernDiceView))
            {
                modernDiceNotice = "Opponents could not be saved. Return to the lobby and resume the table to retry.";
                modernDiceView?.Refresh();
            }
        }
        finally
        {
            modernDiceAdvancing = false;
            if (!ReferenceEquals(view, modernDiceView) && ModernDiceWaiting)
                _ = Dispatcher.BeginInvoke(new Action(AdvanceModernDiceOpponents));
        }
    }

    private void OnModernDiceFailed()
    {
        if (modernDiceView?.Failed == true && IsLoaded) CloseModernDice();
    }

    private void CloseModernDice()
    {
        if (modernDiceView is null) return;
        bool failed = modernDiceView.Failed;
        modernDiceView.Dispose();
        modernDiceView = null;
        embeddedGameHost!.Content = null;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach ((UIElement element, bool enabled) in modernDiceCoveredElements)
            element.SetCurrentValue(IsEnabledProperty, enabled);
        modernDiceCoveredElements.Clear();
        modernDiceMusic?.Dispose(); modernDiceMusic = null;
        CasinoAudio.Mixer.Stop(kniffelAudioOwner);
        if (!failed)
        {
            modernDiceCommands = null;
            modernDiceGame = null;
            modernDiceEvents.Clear();
        }
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        if (failed)
            StatusText.Text = "Midnight Dice browser interrupted. Reopen to resume the saved C# dice and table.";
        SyncHostMusic();
        NormalizeBetForTable();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
        if (kniffelLevelUp)
        {
            kniffelLevelUp = false;
            Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
        }
        modeRegistry.NotifyStateChanged();
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
