using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Button? vaultButton;
    private TextBlock? vaultHeaderTitle;
    private TextBlock? vaultHeaderAmount;
    private ProgressBar? vaultHeaderProgress;
    private DispatcherTimer? vaultRefreshTimer;
    private decimal lastVaultHeaderAmount = -1m;

    private void InitializeVaultFeature()
    {
        if (progressionController is null || vaultButton is not null)
        {
            return;
        }

        try
        {
            ReconcilePendingVaultPayout();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException or InvalidOperationException)
        {
            StatusText.Text = "Casino Vault payout recovery is pending: " + ex.Message;
        }

        BuildVaultHeaderButton();
        RefreshVaultButton();
        vaultRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        vaultRefreshTimer.Tick += (_, _) => RefreshVaultButton();
        vaultRefreshTimer.Start();
    }

    private void ShutdownVaultFeature()
    {
        vaultRefreshTimer?.Stop();
        vaultRefreshTimer = null;
    }

    private void BuildVaultHeaderButton()
    {
        if (profileButton is null || profileButton.Parent is not Grid headerGrid)
        {
            return;
        }

        headerGrid.Children.Remove(profileButton);
        profileButton.Margin = new Thickness(0, 0, 8, 0);

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 20, 0),
        };
        Grid.SetColumn(headerActions, 2);
        Panel.SetZIndex(headerActions, 1);
        headerActions.Children.Add(profileButton);

        vaultHeaderTitle = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 191, 102)),
        };
        vaultHeaderAmount = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(239, 231, 210)),
        };
        vaultHeaderProgress = new ProgressBar
        {
            Height = 4,
            Minimum = 0,
            Maximum = 1,
            Margin = new Thickness(0, 4, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(28, 44, 47)),
            Foreground = new SolidColorBrush(Color.FromRgb(217, 172, 68)),
        };
        var content = new StackPanel { Width = 150 };
        content.Children.Add(vaultHeaderTitle);
        content.Children.Add(vaultHeaderAmount);
        content.Children.Add(vaultHeaderProgress);

        vaultButton = new Button
        {
            Name = "CasinoVaultButton",
            Height = 52,
            MinWidth = 170,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0),
            Style = (Style)FindResource("MutedButton"),
            Content = content,
            VerticalAlignment = VerticalAlignment.Center,
        };
        vaultButton.Click += VaultButton_Click;
        headerActions.Children.Add(vaultButton);
        headerGrid.Children.Add(headerActions);
    }

    private void RefreshVaultButton()
    {
        if (vaultButton is null || vaultHeaderTitle is null || vaultHeaderAmount is null || vaultHeaderProgress is null || progressionController is null)
        {
            return;
        }

        CasinoVaultSnapshot snapshot = CasinoVaultService.Snapshot(progressionController.Profile);
        Color accent = (Color)ColorConverter.ConvertFromString(snapshot.Tier.Accent);
        vaultHeaderProgress.Value = (double)snapshot.Progress;
        vaultHeaderProgress.Foreground = new SolidColorBrush(accent);
        vaultHeaderTitle.Foreground = new SolidColorBrush(accent);

        if (snapshot.PayoutPending)
        {
            vaultHeaderTitle.Text = "VAULT · RECOVERY";
            vaultHeaderAmount.Text = "Payout pending";
        }
        else if (snapshot.IsFull)
        {
            vaultHeaderTitle.Text = "VAULT FULL";
            vaultHeaderAmount.Text = $"BREAK OPEN · +{snapshot.Tier.FullBonusRate:P0}";
            vaultButton.BorderBrush = new SolidColorBrush(accent);
            vaultButton.Effect = new DropShadowEffect { Color = accent, BlurRadius = 11, ShadowDepth = 0, Opacity = 0.32 };
        }
        else
        {
            vaultHeaderTitle.Text = "VAULT · " + snapshot.Tier.Name.Replace(" Vault", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
            vaultHeaderAmount.Text = $"{Money(snapshot.CurrentAmount)} / {Money(snapshot.Tier.Capacity)}";
            vaultButton.ClearValue(EffectProperty);
        }

        vaultButton.ToolTip = $"Casino Vault · {snapshot.Progress:P1} full · {snapshot.ContributionRate:P0} of eligible wagers · {snapshot.Tier.FullBonusRate:P0} bonus at 100%";

        if (lastVaultHeaderAmount >= 0m && snapshot.CurrentAmount > lastVaultHeaderAmount && AnimationsEnabled && IsVisible)
        {
            vaultButton.BeginAnimation(OpacityProperty, new DoubleAnimation(0.70, 1.0, TimeSpan.FromMilliseconds(330))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        }
        lastVaultHeaderAmount = snapshot.CurrentAmount;
    }

    private void VaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (progressionController is null)
        {
            return;
        }
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before opening the Casino Vault.";
            return;
        }
        if (embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen || MetaPageOpen)
        {
            StatusText.Text = "Return to the casino lobby or Blackjack table before opening the Casino Vault.";
            return;
        }

        ProcessLatestCompletedRound();
        var window = new CasinoVaultWindow(
            () => CasinoVaultService.Snapshot(progressionController.Profile),
            OpenCasinoVault,
            () => AnimationsEnabled,
            () => SoundEnabled)
        {
            Owner = this,
        };
        window.ShowDialog();
        RefreshVaultButton();
        RefreshProfileButton();
        RefreshGameUi();
    }

    private CasinoVaultOpenResult OpenCasinoVault()
    {
        if (progressionController is null)
        {
            throw new InvalidOperationException("Player progression is not available.");
        }

        PlayerProfileState profile = progressionController.Profile;
        CasinoVaultState before = CopyVault(profile.Vault);
        bool preparedDurably = false;
        try
        {
            CasinoVaultOpenResult result = CasinoVaultService.PrepareOpen(profile);
            if (!testSession)
            {
                PlayerProfileStore.SaveProfileOrThrow(profile);
                preparedDurably = true;
            }

            ReconcilePendingVaultPayout();
            return result;
        }
        catch
        {
            // If the pending receipt never reached disk there is nothing to recover, so restore
            // the live state as well. Once durable, recovery owns the transition and must remain.
            if (!preparedDurably)
            {
                profile.Vault = before;
            }
            throw;
        }
    }

    private void ReconcilePendingVaultPayout()
    {
        if (progressionController is null)
        {
            return;
        }

        PlayerProfileState profile = progressionController.Profile;
        CasinoVaultState vault = profile.Vault;
        if (vault.PendingPayoutId == Guid.Empty || vault.PendingPayout <= 0m)
        {
            return;
        }

        Guid payoutId = vault.PendingPayoutId;
        decimal pendingPayout = vault.PendingPayout;
        decimal pendingBasePayout = vault.PendingBasePayout;
        decimal pendingBonus = vault.PendingBonus;
        if (preferences.LastAppliedVaultPayoutId != payoutId)
        {
            decimal nextBankroll = checked(session.Bankroll + pendingPayout);
            AppPreferences applied = preferences with
            {
                Bankroll = nextBankroll,
                LastAppliedVaultPayoutId = payoutId,
                Empire = empire?.Snapshot ?? preferences.Empire,
            };

            // Persist the bankroll and receipt together before projecting it into the live table.
            // If this write fails, the durable profile still contains the pending payout.
            if (!testSession)
            {
                PlayerStateStore.SaveOrThrow(applied);
            }
            preferences = applied;
            session.ResetBankroll(nextBankroll);
        }

        if (CasinoVaultService.CompletePendingPayout(profile, payoutId) && !testSession)
        {
            try
            {
                PlayerProfileStore.SaveProfileOrThrow(profile);
            }
            catch
            {
                // The bankroll receipt already prevents a duplicate payout. Re-create the pending
                // marker in memory so a later save/restart can finish the profile side safely.
                profile.Vault.PendingPayoutId = payoutId;
                profile.Vault.PendingPayout = pendingPayout;
                profile.Vault.PendingBasePayout = pendingBasePayout;
                profile.Vault.PendingBonus = pendingBonus;
                throw;
            }
        }

        selectedBet = Math.Min(selectedBet, session.Bankroll);
        RefreshVaultButton();
        RefreshGameUi();
    }

    private static CasinoVaultState CopyVault(CasinoVaultState state) => new()
    {
        SchemaVersion = state.SchemaVersion,
        TierId = state.TierId,
        CurrentAmount = state.CurrentAmount,
        ObservedEligibleWager = state.ObservedEligibleWager,
        LifetimeContributions = state.LifetimeContributions,
        LifetimePayouts = state.LifetimePayouts,
        LifetimeBonuses = state.LifetimeBonuses,
        VaultsOpened = state.VaultsOpened,
        BiggestPayout = state.BiggestPayout,
        LastOpenedAt = state.LastOpenedAt,
        PendingPayoutId = state.PendingPayoutId,
        PendingPayout = state.PendingPayout,
        PendingBasePayout = state.PendingBasePayout,
        PendingBonus = state.PendingBonus,
    };
}
