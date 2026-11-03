using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private LobbyDailyDrawer? dailyLoginDrawer;

    private void InitializeDailyLoginDrawer()
    {
        if (dailyLoginDrawer is not null || lobby is null)
        {
            return;
        }

        dailyLoginDrawer = new LobbyDailyDrawer(
            GetDailyLoginStatus,
            ClaimDailyLoginFromDrawer,
            () => preferences.Presentation("lobby").AnimationsEnabled)
        {
            Visibility = lobby.IsVisible ? Visibility.Visible : Visibility.Collapsed,
        };

        Grid.SetRow(dailyLoginDrawer, 1);
        Grid.SetColumnSpan(dailyLoginDrawer, 2);
        Panel.SetZIndex(dailyLoginDrawer, 1100);
        CasinoRoot.Children.Add(dailyLoginDrawer);

        lobby.IsVisibleChanged += LobbyDailyVisibilityChanged;
        dailyLoginDrawer.Refresh(allowAutoOpen: lobby.IsVisible);
    }

    private void ShutdownDailyLoginDrawer()
    {
        if (dailyLoginDrawer is null)
        {
            return;
        }

        if (lobby is not null)
        {
            lobby.IsVisibleChanged -= LobbyDailyVisibilityChanged;
        }

        CasinoRoot.Children.Remove(dailyLoginDrawer);
        dailyLoginDrawer = null;
    }

    private void LobbyDailyVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (dailyLoginDrawer is null)
        {
            return;
        }

        bool visible = (bool)e.NewValue;
        dailyLoginDrawer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (visible)
        {
            dailyLoginDrawer.Refresh(allowAutoOpen: true);
        }
    }

    private DailyLoginStatus GetDailyLoginStatus()
    {
        DailyLoginStatus status = DailyLoginProgress.GetStatus(
            preferences.LastDailyLoginClaimDate,
            preferences.DailyLoginClaims,
            DateOnly.FromDateTime(DateTime.Now));

        if (!testSession)
        {
            return status;
        }

        return status with { CanClaim = false };
    }

    private DailyLoginClaim? ClaimDailyLoginFromDrawer()
    {
        if (testSession)
        {
            return null;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DailyLoginClaim? claim = DailyLoginProgress.TryClaim(
            preferences.LastDailyLoginClaimDate,
            preferences.DailyLoginClaims,
            today);
        if (claim is null)
        {
            return null;
        }

        session.CreditBankroll(claim.Reward);
        preferences = preferences with
        {
            LastDailyLoginClaimDate = claim.ClaimDate,
            DailyLoginClaims = claim.TotalClaims,
        };
        SavePreferences();
        BankrollText.Text = Money(session.Bankroll);
        StatusText.Text = $"Daily reward · +{Money(claim.Reward)} · day {claim.Status.ActiveStep}/{DailyLoginRewardSchedule.CycleLength}";
        CasinoFeedback.PlayChip(SoundEnabled);
        AnimateDailyBankrollCredit();
        return claim;
    }

    private void AnimateDailyBankrollCredit()
    {
        if (!preferences.Presentation("lobby").AnimationsEnabled)
        {
            return;
        }

        var scale = BankrollText.RenderTransform as ScaleTransform ?? new ScaleTransform(1d, 1d);
        BankrollText.RenderTransform = scale;
        BankrollText.RenderTransformOrigin = new Point(.5, .5);

        var pulse = new DoubleAnimation(1d, 1.13d, TimeSpan.FromMilliseconds(190))
        {
            AutoReverse = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse, HandoffBehavior.SnapshotAndReplace);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse, HandoffBehavior.SnapshotAndReplace);
    }
}
