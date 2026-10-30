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
            empire.Accrue(now);
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
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || empire is null) return;
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
            "←  Casino lobby");
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
            ShowStartupEmpireReport(this);
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
            "Close Empire window");
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
        Dispatcher.BeginInvoke(() => ShowStartupEmpireReport(window));
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

    private void ShowStartupEmpireReport(Window owner)
    {
        if (empireStartupReportShown || empireStartupCatchUp is null) return;
        empireStartupReportShown = true;
        var report = new EmpireOfflineReportWindow(empireStartupCatchUp) { Owner = owner };
        report.ShowDialog();
    }

    private bool UpgradeEmpire(string id, int levels)
    {
        if (empire is null || !empire.UpgradeMany(id, levels, DateTimeOffset.UtcNow)) return false;
        SavePreferences();
        empireView?.Refresh();
        empireDetachedView?.Refresh();
        RefreshEmpireHeader();
        return true;
    }

    private bool UpgradeEmpire(string id) => UpgradeEmpire(id, 1);

    private EmpirePrestigeResult PrestigeEmpire()
    {
        if (empire is null) return new(false, 0, 0, "Empire is unavailable.");
        EmpirePrestigeResult result = empire.PrestigeEmpire(DateTimeOffset.UtcNow);
        if (result.Success) SavePreferences();
        empireView?.Refresh();
        empireDetachedView?.Refresh();
        RefreshEmpireHeader();
        return result;
    }

    private bool ClaimEmpireDividend()
    {
        if (empire is null) return false;
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
