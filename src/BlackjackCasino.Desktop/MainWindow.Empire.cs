using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private EmpireService? empire;
    private EmpireView? empireView;
    private DispatcherTimer? empireTimer;
    private DateTimeOffset empireLastSave;
    private string? empireReturnNotice;
    private readonly List<(UIElement Element, bool Enabled)> empireCoveredElements = [];
    private bool EmpirePageOpen => empireView is not null;

    private void InitializeEmpire()
    {
        empire = new EmpireService(preferences.Empire);
        EmpireAccrual catchUp = empire.Accrue(DateTimeOffset.UtcNow);
        if (catchUp.Elapsed > TimeSpan.FromMinutes(1))
            empireReturnNotice = $"While you were away: +{EmpireView.Number(catchUp.Credits)} credits over {catchUp.Elapsed.TotalHours:0.0} hours."
                + (catchUp.Capped ? " Offline earnings are capped at 8 hours." : "");
        empireLastSave = DateTimeOffset.UtcNow;
        empireTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        empireTimer.Tick += (_, _) =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            empire.Accrue(now);
            empireView?.Refresh();
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
            empire.Accrue(DateTimeOffset.UtcNow);
            SavePreferences();
        };
    }

    private void OpenEmpire()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || empire is null) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        empire.Start(DateTimeOffset.UtcNow);
        empire.Accrue(DateTimeOffset.UtcNow);
        SavePreferences();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        empireView = new EmpireView(empire, UpgradeEmpire, ClaimEmpireDividend, CloseEmpire, () => AnimationsEnabled);
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
        if (empireReturnNotice is not null)
        {
            empireView.SetNotice(empireReturnNotice);
            empireReturnNotice = null;
        }
        Dispatcher.BeginInvoke(() => empireView?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
    }

    private bool UpgradeEmpire(string id)
    {
        if (empire is null || !empire.Upgrade(id, DateTimeOffset.UtcNow)) return false;
        SavePreferences();
        return true;
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
            empireView?.SetNotice($"Owner dividend: {amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"))} added to your casino cash.");
            return true;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or OverflowException)
        {
            empireView?.SetNotice("The dividend could not be saved. Your reserve is still available; please try again.");
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
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
