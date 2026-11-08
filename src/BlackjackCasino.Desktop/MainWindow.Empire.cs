using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private EmpireService? empire;
    private ModernEmpireView? empireView;
    private ModernEmpireView? cachedEmpireView;
    private ContentControl? empireCacheHost;
    private ModernEmpireView? empireDetachedView;
    private Window? empireDetachedWindow;
    private DispatcherTimer? empireTimer;
    private DateTimeOffset empireLastSave;
    private EmpireAccrual? empireStartupCatchUp;
    private bool empireStartupReportShown;
    private readonly List<(UIElement Element, bool Enabled)> empireCoveredElements = [];
    private bool EmpirePageOpen => empireView is not null;
    private bool EmpireDetachedOpen => empireDetachedWindow is { IsVisible: true };

    private void InitializeEmpire()
    {
        empire = new EmpireService(preferences.Empire);
        SyncEmpireAchievementBoosts();
        if (progressionController is not null) empire.SetActivitySnapshot(EmpireActivitySnapshot.FromProfile(progressionController.Profile));
        EmpireAccrual catchUp = empire.Accrue(DateTimeOffset.UtcNow);
        if (catchUp.Elapsed >= TimeSpan.FromMinutes(2) && (catchUp.Credits > 0 || catchUp.Dividend > 0))
            empireStartupCatchUp = catchUp;

        empireLastSave = DateTimeOffset.UtcNow;
        RefreshEmpireHeader();
        // Empire accrual is deterministic over elapsed time. Running it twice per second on
        // DispatcherPriority.Normal only steals time from input/layout without improving the
        // simulation. One background tick keeps the live counters responsive while allowing
        // WPF input and render work to win dispatcher scheduling conflicts.
        empireTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        empireTimer.Tick += (_, _) =>
        {
            if (empire is null || !empire.Started) return;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            SyncEmpireAchievementBoosts();
            empire.ObserveActivity(progressionController is null ? new() : EmpireActivitySnapshot.FromProfile(progressionController.Profile), now);
            if (empireView?.IsVisible == true) empireView.RefreshLive();
            if (empireDetachedView?.IsVisible == true) empireDetachedView.RefreshLive();
            RefreshEmpireHeader();
            if (now - empireLastSave >= TimeSpan.FromSeconds(30))
            {
                SavePreferences();
                empireLastSave = now;
            }
        };
        // A profile that has never started Casino Empire has no economy to accrue and no live
        // header to update. Do not wake the WPF dispatcher once a second for a dormant feature.
        if (empire.Started) empireTimer.Start();
        Closed += (_, _) =>
        {
            empireTimer.Stop();
            cachedEmpireView?.Dispose();
            cachedEmpireView = null;
            if (empireDetachedWindow is { IsVisible: true }) empireDetachedWindow.Close();
            empire.Accrue(DateTimeOffset.UtcNow);
            SavePreferences();
        };
    }

    private void SyncEmpireAchievementBoosts()
        => empire?.SetClaimedAchievements(progressionController?.Profile.ClaimedChallengeIds);

    private void OpenEmpire()
    {
        long visitStarted = Stopwatch.GetTimestamp();
        if (EmpireDetachedOpen)
        {
            empireDetachedWindow!.Activate();
            return;
        }
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen || empire is null) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        StartEmpireSession();
        // Keep one embedded WebView attached to a dedicated overlay child. Other games
        // replace embeddedGameHost.Content, never the cached Empire visual tree.
        empireCacheHost ??= new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
        };
        if (!embeddedGameOverlay!.Children.Contains(empireCacheHost))
            embeddedGameOverlay.Children.Add(empireCacheHost);
        if (cachedEmpireView is null || cachedEmpireView.Failed)
        {
            cachedEmpireView?.Dispose();
            cachedEmpireView = CreateModernEmpireView(CloseEmpire, () =>
            {
                CloseEmpire();
                Dispatcher.BeginInvoke(OpenEmpire);
            });
            empireCacheHost.Content = cachedEmpireView;
        }
        empireView = cachedEmpireView;
        empireView.BeginVisit(visitStarted);
        foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>().Where(child => child != embeddedGameOverlay))
        {
            empireCoveredElements.Add((covered, covered.IsEnabled));
            covered.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Visibility = Visibility.Collapsed;
        empireCacheHost.Visibility = Visibility.Visible;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        KeyboardNavigation.SetTabNavigation(empireView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(empireView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetDirectionalNavigation(empireView, KeyboardNavigationMode.Contained);
        modeRegistry.NotifyStateChanged();
        Dispatcher.BeginInvoke(() =>
        {
            empireView?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            ShowStartupEmpireReport(empireView);
        });
    }

    private void OpenEmpireDetached()
    {
        if (empire is null || EmpirePageOpen) return;
        if (EmpireDetachedOpen)
        {
            empireDetachedWindow!.WindowState = WindowState.Normal;
            empireDetachedWindow.Activate();
            return;
        }

        ProcessLatestCompletedRound();
        StartEmpireSession();
        var window = new Window
        {
            Title = "Casino Empire · Detached",
            Width = 1280,
            Height = 820,
            MinWidth = 800,
            MinHeight = 600,
            Background = new SolidColorBrush(Color.FromRgb(8, 20, 19)),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
        };
        ModernEmpireView view = CreateModernEmpireView(() => window.Close());
        window.Content = view;
        window.Closed += (_, _) =>
        {
            view.Dispose();
            empireDetachedView = null;
            empireDetachedWindow = null;
            empire!.Accrue(DateTimeOffset.UtcNow);
            SavePreferences();
            RefreshEmpireHeader();
            modeRegistry.NotifyStateChanged();
        };
        empireDetachedWindow = window;
        empireDetachedView = view;
        window.Show();
        modeRegistry.NotifyStateChanged();
        Dispatcher.BeginInvoke(() => ShowStartupEmpireReport(view));
    }

    private ModernEmpireView CreateModernEmpireView(Action close, Action? retry = null)
        => new(() => new
        {
            Language = Localization.Language == UiLanguagePreference.German ? "de" : "en",
            AnimationsEnabled = preferences.Presentation("casino-empire").AnimationsEnabled,
            SoundEnabled = preferences.SoundEnabled,
            Detached = EmpireDetachedOpen,
            CanClaimDividend = !session.RoundInProgress && embeddedGameShell is null && !KniffelPageOpen,
            Economy = EmpireModernSnapshot.Create(empire!),
        }, command => command.Action == "dividend"
            ? CollectModernEmpireDividend()
            : MutateEmpire(candidate => EmpireModernProtocol.Apply(candidate, command, DateTimeOffset.UtcNow)),
            command => EmpireModernSnapshot.Preview(empire!.Preview(command.InspectRoomId ?? command.RoomId ?? EmpireCatalog.Find(command.Id ?? "")?.Id ?? "slots",
                (candidate, at) => EmpireModernProtocol.Apply(candidate, command, at))), close, retry);

    private EmpireActionResult CollectModernEmpireDividend()
    {
        decimal before = session.Bankroll;
        bool success = ClaimEmpireDividend();
        if (success)
        {
            string amount = (session.Bankroll - before).ToString("C2", CultureInfo.GetCultureInfo("en-US"));
            return new(true, $"Owner dividend: {amount} added to your casino cash.");
        }
        return new(false, "The owner reserve is empty, a game is active, or the dividend could not be saved. Your unclaimed reserve remains available.");
    }

    private void StartEmpireSession()
    {
        if (empire is null) return;
        SyncEmpireAchievementBoosts();
        empire.Start(DateTimeOffset.UtcNow);
        if (empireTimer is not null && !empireTimer.IsEnabled) empireTimer.Start();
        empire.Accrue(DateTimeOffset.UtcNow);
        SavePreferences();
        RefreshEmpireHeader();
    }

    private void ShowStartupEmpireReport(ModernEmpireView? target)
    {
        if (target is null || empireStartupReportShown || empireStartupCatchUp is null) return;
        empireStartupReportShown = true;
        target.ShowOfflineReport(empireStartupCatchUp);
    }

    private EmpireActionResult MutateEmpire(Func<EmpireService, EmpireActionResult> change)
    {
        if (empire is null) return new(false, "Empire is unavailable.");
        EmpireActionResult result;
        try
        {
            result = empire.Transact(candidate =>
            {
                if (progressionController is not null) candidate.ObserveActivity(EmpireActivitySnapshot.FromProfile(progressionController.Profile), DateTimeOffset.UtcNow);
                return change(candidate);
            }, committed =>
            {
                AppPreferences next = preferences with { Empire = committed, Bankroll = session.Bankroll };
                if (!testSession) PlayerStateStore.SaveOrThrow(next);
                preferences = next;
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException)
        {
            result = new(false, "The change could not be saved. Your credits and management choices remain available; please retry.");
        }
        empireView?.SetNotice(result.Message);
        empireDetachedView?.SetNotice(result.Message);
        empireView?.Refresh();
        empireDetachedView?.Refresh();
        RefreshEmpireHeader();
        return result;
    }

    private bool UpgradeEmpire(string id, int levels) => MutateEmpire(candidate => candidate.UpgradeMany(id, levels, DateTimeOffset.UtcNow)
        ? new(true, "Department upgrade saved.") : new(false, "Upgrade requirements or construction budget are not met.")).Success;

    private bool UpgradeEmpire(string id) => UpgradeEmpire(id, 1);

    private EmpirePrestigeResult PrestigeEmpire()
    {
        if (empire is null) return new(false, 0, 0, "Empire is unavailable.");
        EmpirePrestigeResult? prestigeResult = null;
        EmpireActionResult result = MutateEmpire(candidate =>
        {
            prestigeResult = candidate.PrestigeEmpire(DateTimeOffset.UtcNow);
            return new(prestigeResult.Success, prestigeResult.Message);
        });
        return result.Success ? prestigeResult! : new(false, empire.Prestige, empire.PrestigeBonusPercent, result.Message);
    }

    private bool ClaimEmpireDividend()
    {
        if (empire is null) return false;
        // A detached estate may stay open beside a live game. Replacing that game's
        // wallet would invalidate its own settlement/checkpoint, so defer only the claim.
        if (session.RoundInProgress || embeddedGameShell is not null || KniffelPageOpen)
        {
            const string message = "Return to the lobby or finish your Blackjack hand before collecting cash. Your owner reserve remains available.";
            empireView?.SetNotice(message); empireDetachedView?.SetNotice(message);
            return false;
        }
        try
        {
            decimal amount = empire.ClaimDividend(DateTimeOffset.UtcNow, (state, cash) =>
            {
                var committed = preferences with { Empire = state, Bankroll = checked(session.Bankroll + cash) };
                if (!testSession) PlayerStateStore.SaveOrThrow(committed);
                preferences = committed;
                session.ResetBankroll(committed.Bankroll);
            });
            if (amount <= 0) return false;
            string text = $"Owner dividend: {amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"))} added to your casino cash.";
            empireView?.SetNotice(text);
            empireDetachedView?.SetNotice(text);
            empireView?.Refresh();
            empireDetachedView?.Refresh();
            RefreshEmpireHeader();
            return true;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or OverflowException)
        {
            const string text = "The dividend could not be saved. Your reserve is still available; please try again.";
            empireView?.SetNotice(text);
            empireDetachedView?.SetNotice(text);
            return false;
        }
    }

    private void CloseEmpire()
    {
        if (empireView is null) return;
        // A failed browser must be rebuilt on the next visit. A healthy browser
        // remains attached but completely hidden, so no WebView2 cold restart occurs.
        bool failed = empireView.Failed;
        empireView.PauseVisit();
        empireView = null;
        if (empireCacheHost is not null) empireCacheHost.Visibility = Visibility.Collapsed;
        if (failed)
        {
            cachedEmpireView?.Dispose();
            if (empireCacheHost is not null) empireCacheHost.Content = null;
            cachedEmpireView = null;
        }
        embeddedGameHost!.Visibility = Visibility.Visible;
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach (var (element, enabled) in empireCoveredElements) element.SetCurrentValue(IsEnabledProperty, enabled);
        empireCoveredElements.Clear();
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        SyncHostMusic();
        empire!.Accrue(DateTimeOffset.UtcNow);
        SavePreferences();
        NormalizeBetForTable();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
        RefreshEmpireHeader();
        modeRegistry.NotifyStateChanged();
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private bool empireHeaderWasLive;

    private void RefreshEmpireHeader()
    {
        if (empire is null) return;
        bool visible = empire.Started && preferences.ShowEmpireBalance;
        EmpireBalancePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        bool live = visible && EmpireBalancePanel.IsVisible && EmpireBalancePanel.IsEnabled && WindowState != WindowState.Minimized;
        if (!live)
        {
            EmpireHeaderBalance.StopInterpolation();
            empireHeaderWasLive = false;
            return;
        }
        EmpireHeaderBalance.SetAmount(empire.Credits, CurrentPresentation.AnimationsEnabled && empireHeaderWasLive);
        empireHeaderWasLive = true;
        EmpireHeaderRate.Text = "+" + EmpireView.Number(empire.CreditsPerSecond) + " cr / sec";
    }
}

