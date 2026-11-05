using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernLobbyView? modernLobby;
    private DispatcherTimer? modernLobbyRefresh;
    private Border? modernLobbyLoading;

    private void InitializeModernLobby()
    {
        if (modernLobby is not null || !ModernLobbyView.IsAvailable || progressionController is null)
            return;

        modernLobby = new ModernLobbyView(
            modeRegistry, GetModernLobbySnapshot, OpenProgression,
            OpenPlayerProfile, () => VaultButton_Click(this, new RoutedEventArgs()),
            () => { ClaimDailyLoginFromDrawer(); modernLobby?.Refresh(); },
            Close, OnModernLobbyStateChanged);

        // Replace the legacy WPF artwork *before* WebView2 starts. Never display
        // the old lobby during navigation or while React is still loading.
        modernLobbyLoading = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(4, 22, 17)),
            IsHitTestVisible = false,
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "♠  BLACKJACK CASINO",
                        FontFamily = new FontFamily("Georgia"),
                        FontSize = 32,
                        Foreground = new SolidColorBrush(Color.FromRgb(240, 210, 149)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "THE MIDNIGHT CLUB",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(174, 154, 110)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };
        Grid.SetRow(modernLobbyLoading, 0);
        Grid.SetRowSpan(modernLobbyLoading, 2);
        Grid.SetColumnSpan(modernLobbyLoading, 2);
        Panel.SetZIndex(modernLobbyLoading, 900);
        CasinoRoot.Children.Add(modernLobbyLoading);

        Grid.SetRow(modernLobby, 0);
        Grid.SetRowSpan(modernLobby, 2);
        Grid.SetColumnSpan(modernLobby, 2);
        Panel.SetZIndex(modernLobby, 901);
        CasinoRoot.Children.Add(modernLobby);
        modernLobby.SetLobbyVisible(lobby?.Visibility == Visibility.Visible);
        OnModernLobbyStateChanged();

        modernLobbyRefresh = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        modernLobbyRefresh.Tick += (_, _) =>
        {
            if (modernLobby?.IsVisible == true && modernLobby.Ready)
                modernLobby.Refresh();
        };
        modernLobbyRefresh.Start();
        Closed += (_, _) =>
        {
            modernLobbyRefresh?.Stop();
            modernLobby?.Dispose();
        };
    }

    private void OnModernLobbyStateChanged()
    {
        bool fallback = modernLobby is null || modernLobby.Failed;
        if (lobby is not null)
        {
            // Visibility still tracks the active mode for existing game navigation.
            // Opacity permanently suppresses legacy drawing while React owns the lobby.
            lobby.Opacity = fallback ? 1d : 0d;
            lobby.IsHitTestVisible = fallback;
        }

        if (modernLobbyLoading is not null)
            modernLobbyLoading.Visibility = !fallback && modernLobby?.Ready != true
                && lobby?.Visibility == Visibility.Visible
                ? Visibility.Visible : Visibility.Collapsed;

        // The old slide-out daily drawer belongs only to the WPF fallback.
        if (fallback && lobby is not null && dailyLoginDrawer is null)
            InitializeDailyLoginDrawer();

        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
    }

    private object GetModernLobbySnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        LobbyProgressSnapshot progress = LobbyProgressSnapshot.From(profile);
        CasinoVaultSnapshot vault = CasinoVaultService.Snapshot(profile);
        DailyLoginStatus daily = GetDailyLoginStatus();
        int cycleStart = Math.Max(0, daily.TotalClaims - daily.CycleCompletedSteps);
        var dailyRewards = Enumerable.Range(0, DailyLoginRewardSchedule.CycleLength)
            .Select(index =>
            {
                DailyLoginRewardDefinition reward = DailyLoginRewardSchedule.GetDefinition(cycleStart + index);
                return new
                {
                    Day = index + 1,
                    Bankroll = Money(reward.Bankroll),
                    Coins = reward.CosmeticCoins,
                    Pack = reward.PackRarity?.ToString(),
                };
            }).ToArray();
        string Localize(CasinoMode mode, string source)
        {
            if (mode.UseResourceKeys) return Localization.T(source);
            string? key = Localization.KeyForEnglishSource(source);
            return key is null ? source : Localization.T(key);
        }

        return new
        {
            DailyLogin = new
            {
                daily.CanClaim,
                daily.ClaimedToday,
                daily.TotalClaims,
                daily.ActiveStep,
                daily.NextStep,
                daily.CycleCompletedSteps,
                ActiveReward = Money(daily.ActiveReward),
                NextReward = Money(daily.NextReward),
                Rewards = dailyRewards,
            },
            DisplayName = profile.DisplayName,
            Level = profile.Level,
            Coins = profile.Coins.ToString("N0", CultureInfo.InvariantCulture),
            Prestige = profile.Prestige,
            Title = CosmeticCatalog.Get(profile.EquippedTitleId).Name,
            Bankroll = Money(session.Bankroll),
            EmpireCredits = empire is null ? "—" : $"{empire.Credits:N0} cr",
            AlbumOwned = profile.Collection.OwnedCardIds.Count,
            AlbumTotal = CollectionCatalog.Cards.Count,
            SeasonTier = progress.SeasonTier,
            SeasonNextTier = progress.SeasonNextTier,
            SeasonExperience = progress.SeasonExperience,
            SeasonRequired = progress.SeasonRequired,
            SeasonFraction = progress.SeasonFraction,
            SeasonComplete = progress.SeasonComplete,
            ChallengeName = progress.ChallengeName,
            ChallengeFraction = progress.ChallengeFraction,
            ChallengesReady = progress.ReadyCount,
            VaultTier = vault.Tier.Name.Replace(" Vault", "", StringComparison.Ordinal).ToUpperInvariant(),
            VaultAmount = Money(vault.CurrentAmount),
            VaultCapacity = Money(vault.Tier.Capacity),
            VaultFraction = Math.Clamp((double)vault.Progress, 0, 1),
            AnimationsEnabled = preferences.Presentation("lobby").AnimationsEnabled,
            Language = Localization.Language == UiLanguagePreference.German ? "de" : "en",
            Modes = modeRegistry.Modes.Select(mode => new
            {
                Id = mode.Id,
                Title = Localize(mode, mode.Title),
                Category = Localize(mode, mode.Category),
                Description = Localize(mode, mode.Description),
                Enabled = modeRegistry.CanOpen(mode.Id),
                Secondary = modeRegistry.HasSecondary(mode.Id) && modeRegistry.CanOpenSecondary(mode.Id),
            }).ToArray(),
        };
    }
}
