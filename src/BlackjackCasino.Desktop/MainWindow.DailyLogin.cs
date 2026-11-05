using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
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
        if (testSession || progressionController is null)
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

        DailyLoginRewardDefinition reward = DailyLoginRewardSchedule.GetDefinition(claim.TotalClaims - 1);
        long updatedCoins = checked(progressionController.Profile.Coins + reward.CosmeticCoins);

        session.CreditBankroll(reward.Bankroll);
        progressionController.Profile.Coins = updatedCoins;
        if (reward.PackRarity is { } packRarity)
        {
            CollectionService.GrantPack(progressionController.Profile, packRarity);
        }

        preferences = preferences with
        {
            LastDailyLoginClaimDate = claim.ClaimDate,
            DailyLoginClaims = claim.TotalClaims,
        };

        progressionController.SaveAll();
        SavePreferences();
        BankrollText.Text = Money(session.Bankroll);
        RefreshProfileButton();
        RefreshCollectionAlbumHeaderButton();

        string packText = reward.PackRarity is { } rarity ? $" · {rarity} card pack" : string.Empty;
        StatusText.Text = $"Daily reward · +{Money(reward.Bankroll)} · +{reward.CosmeticCoins:N0} cosmetic coins{packText} · day {claim.Status.ActiveStep}/{DailyLoginRewardSchedule.CycleLength}";
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
