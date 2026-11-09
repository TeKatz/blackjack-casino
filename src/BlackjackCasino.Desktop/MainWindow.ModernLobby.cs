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
    private static string FormatModernEmpireCredits(decimal value)
    {
        (decimal divisor, string suffix) = value switch
        {
            >= 1_000_000_000_000m => (1_000_000_000_000m, "T"),
            >= 1_000_000_000m => (1_000_000_000m, "B"),
            >= 1_000_000m => (1_000_000m, "M"),
            >= 1_000m => (1_000m, "K"),
            _ => (1m, ""),
        };
        return (value / divisor).ToString(divisor == 1m ? "N0" : "0.#", CultureInfo.InvariantCulture) + suffix;
    }

    private ModernLobbyView? modernLobby;
    private DispatcherTimer? modernLobbyRefresh;
    private Border? modernLobbyLoading;

    private void InitializeModernLobby()
    {
        if (modernLobby is not null || !ModernLobbyView.IsAvailable || progressionController is null)
            return;

        modernLobby = new ModernLobbyView(
            modeRegistry, GetModernLobbySnapshot, GetModernShopSnapshot,
            ModernShopCosmeticAction, ModernShopPackAction, OnModernShopClosed,
            GetModernChallengesSnapshot, GetModernStatisticsSnapshot,
            GetModernAccountSnapshot, GetModernHistorySnapshot, GetModernSeasonsSnapshot,
            GetModernVaultSnapshot, GetModernAlbumSnapshot, GetModernAlbumPendingSnapshot,
            ModernAlbumOpen, ModernAlbumComplete,
            ModernAlbumClaim, ModernProfileRename, ModernProfileEquip,
            ActivateModernSeason, ModernVaultOpen,
            ModernChallengeClaim, ModernChallengesClaimAll, OnModernProgressionClosed, OpenProgression,
            ModernArcadeRun,
            OpenNativePlayerProfile, () => OpenModernProgression("vault"),
            () => { ClaimDailyLoginFromDrawer(); modernLobby?.Refresh(); },
            SetModernLobbyLanguage, Close, OnModernLobbyStateChanged,
            GetModernUpdateSnapshot, SetModernUpdateChannel,
            () => CheckForUpdatesAsync(silent: false), InstallUpdateAsync,
            ChangeModernLobbyPresentation);

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

    private void ChangeModernLobbyPresentation(string command, string? value)
    {
        switch (command)
        {
            case "lobby.settings.music" when value is "true" or "false":
            {
                bool enabled = value == "true";
                preferences = preferences with
                {
                    MusicEnabled = enabled,
                    AreaPresentation = preferences.AreaPresentation.ToDictionary(
                        item => item.Key, item => item.Value with { MusicEnabled = enabled },
                        StringComparer.Ordinal),
                };
                SyncHostMusic();
                break;
            }
            case "lobby.settings.volume" when int.TryParse(value, out int units) && units is >= 0 and <= 32:
            {
                double volume = units / 100d;
                preferences = preferences with
                {
                    MusicVolume = volume,
                    AreaPresentation = preferences.AreaPresentation.ToDictionary(
                        item => item.Key, item => item.Value with { MusicVolume = volume },
                        StringComparer.Ordinal),
                };
                SyncHostMusic();
                break;
            }
            case "lobby.settings.empire" when value is "true" or "false":
                preferences = preferences with { ShowEmpireBalance = value == "true" };
                RefreshEmpireHeader();
                break;
            case "lobby.settings.fullscreen":
                ToggleFullscreen();
                break;
            default:
                return;
        }
        SavePreferences();
        modernLobby?.Refresh();
    }

    private object GetModernUpdateSnapshot() => new
    {
        InstalledVersion = AppVersion.Current.ToString(),
        Channel = SelectedUpdateChannel == BlackjackCasino.Update.UpdateChannel.Development ? "development" : "stable",
        Status = UpdateStatusText.Text,
        Busy = updateOperationBusy,
        CanCheck = CheckUpdateButton.IsEnabled,
        CanInstall = InstallUpdateButton.IsEnabled && pendingManifest is not null,
        UpdateAvailable = pendingManifest is not null,
    };

    private void SetModernUpdateChannel(string channel)
    {
        if (updateOperationBusy || channel is not ("stable" or "development")) return;
        UpdateChannelComboBox.SelectedIndex = channel == "development" ? 1 : 0;
        modernLobby?.RefreshUpdates();
    }

    private void SetModernLobbyLanguage(string id)
    {
        UiLanguagePreference requested = id == "de" ? UiLanguagePreference.German : UiLanguagePreference.English;
        if (preferences.Language == requested) return;
        preferences = preferences with { Language = requested };
        Localization.SetLanguage(requested);
        SavePreferences();
        modernLobby?.Refresh();
    }

    private void OnModernLobbyStateChanged()
    {
        // A visible legacy frame during initialization would be a real flash:
        // the visibility event can run before InitializeModernLobby creates
        // the browser. Only use WPF when assets are absent or WebView2 fails.
        bool fallback = !ModernLobbyView.IsAvailable || modernLobby?.Failed == true;
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
            TitleVariant = CosmeticCatalog.Get(profile.EquippedTitleId).VisualVariant,
            PortraitVariant = CosmeticCatalog.Get(profile.EquippedPortraitId).VisualVariant,
            FrameVariant = CosmeticCatalog.Get(profile.EquippedFrameId).VisualVariant,
            Bankroll = Money(session.Bankroll),
            EmpireCredits = empire is null ? "—" : FormatModernEmpireCredits(empire.Credits) + " cr",
            News = ReleaseNotesCatalog.Current,
            InstalledVersion = AppVersion.Current.ToString(),
            AlbumOwned = profile.Collection.OwnedCardIds.Count,
            AlbumTotal = CollectionCatalog.Cards.Count,
            AlbumPacksReady = CollectionService.UnopenedPackCount(profile),
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
            AlbumSoundEnabled = preferences.Presentation("album").SoundEnabled,
            MusicEnabled = preferences.MusicEnabled,
            MusicVolume = preferences.MusicVolume,
            ShowEmpireBalance = preferences.ShowEmpireBalance,
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
