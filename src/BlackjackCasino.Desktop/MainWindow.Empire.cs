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
    private EmpireView? empireView;
    private EmpireView? empireDetachedView;
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
        // Idle production belongs to the whole casino, not just the Empire page. Keep the
        // accrual clock responsive while other WPF surfaces are rendering or animating.
        empireTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        empireTimer.Tick += (_, _) =>
        {
            if (empire is null) return;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            SyncEmpireAchievementBoosts();
            empire.ObserveActivity(progressionController is null ? new() : EmpireActivitySnapshot.FromProfile(progressionController.Profile), now);
            empireView?.Refresh();
            empireDetachedView?.Refresh();
            RefreshEmpireHeader();
            if (empire.Started && now - empireLastSave >= TimeSpan.FromSeconds(30))
            {
                SavePreferences();
                empireLastSave = now;
            }
        };
        empireTimer.Start();
        Closed += (_, _) =>
        {
            empireTimer.Stop();
            if (empireDetachedWindow is { IsVisible: true }) empireDetachedWindow.Close();
            empire.Accrue(DateTimeOffset.UtcNow);
            SavePreferences();
        };
    }

    private void SyncEmpireAchievementBoosts()
        => empire?.SetClaimedAchievements(progressionController?.Profile.ClaimedChallengeIds);

    private void OpenEmpire()
    {
        if (EmpireDetachedOpen)
        {
            empireDetachedWindow!.Activate();
            return;
        }
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen || empire is null) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        StartEmpireSession();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        empireView = new EmpireView(
            empire,
            UpgradeEmpire,
            ClaimEmpireDividend,
            PrestigeEmpire,
            CloseEmpire,
            () => AnimationsEnabled,
            "←  Casino lobby",
            MutateEmpire);
        EmpireStatisticsPage.Attach(empireView, empire);
        foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>().Where(child => child != embeddedGameOverlay))
        {
            empireCoveredElements.Add((covered, covered.IsEnabled));
            covered.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = empireView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
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
            MinWidth = 940,
            MinHeight = 650,
            Background = new SolidColorBrush(Color.FromRgb(8, 20, 19)),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
        };
        EmpireView? view = null;
        view = new EmpireView(
            empire,
            UpgradeEmpire,
            ClaimEmpireDividend,
            PrestigeEmpire,
            () => window.Close(),
            () => AnimationsEnabled,
            "Close Empire window",
            MutateEmpire);
        EmpireStatisticsPage.Attach(view, empire);
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

    private void StartEmpireSession()
    {
        if (empire is null) return;
        SyncEmpireAchievementBoosts();
        empire.Start(DateTimeOffset.UtcNow);
        empire.Accrue(DateTimeOffset.UtcNow);
        SavePreferences();
        RefreshEmpireHeader();
    }

    private void ShowStartupEmpireReport(EmpireView? target)
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
        empireView.Dispose();
        embeddedGameHost!.Content = null;
        empireView = null;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach (var (element, enabled) in empireCoveredElements) element.SetCurrentValue(IsEnabledProperty, enabled);
        empireCoveredElements.Clear();
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
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

    private void EmpireBalanceSettingChanged(object sender, RoutedEventArgs e)
    {
        // XAML may raise Checked before constructor preferences have been loaded.
        if (empire is null || suppressPreferenceEvents) return;
        RefreshEmpireHeader();
        SavePreferences();
    }

    private void RefreshEmpireHeader()
    {
        if (empire is null) return;
        bool visible = empire.Started && EmpireBalanceCheckBox.IsChecked == true;
        EmpireBalancePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        EmpireHeaderBalance.SetAmount(empire.Credits, AnimationsEnabled && visible && EmpireBalancePanel.IsVisible);
        EmpireHeaderRate.Text = "+" + EmpireView.Number(empire.CreditsPerSecond) + " cr / sec";
    }
}
