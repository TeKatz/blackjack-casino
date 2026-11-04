using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernLobbyView? modernLobby;
    private DispatcherTimer? modernLobbyRefresh;

    private void InitializeModernLobby()
    {
        if (modernLobby is not null || !ModernLobbyView.IsAvailable || progressionController is null)
            return;

        modernLobby = new ModernLobbyView(
            modeRegistry, GetModernLobbySnapshot, OpenProgression,
            OpenPlayerProfile, () => VaultButton_Click(this, new RoutedEventArgs()),
            Close,
            () => UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true));

        Grid.SetRow(modernLobby, 0);
        Grid.SetRowSpan(modernLobby, 2);
        Grid.SetColumnSpan(modernLobby, 2);
        Panel.SetZIndex(modernLobby, 901);
        CasinoRoot.Children.Add(modernLobby);
        modernLobby.SetLobbyVisible(lobby?.Visibility == Visibility.Visible);

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

    private object GetModernLobbySnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        LobbyProgressSnapshot progress = LobbyProgressSnapshot.From(profile);
        CasinoVaultSnapshot vault = CasinoVaultService.Snapshot(profile);
        string Localize(CasinoMode mode, string source)
        {
            if (mode.UseResourceKeys) return Localization.T(source);
            string? key = Localization.KeyForEnglishSource(source);
            return key is null ? source : Localization.T(key);
        }

        return new
        {
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
