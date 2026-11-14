using BlackjackCasino.HostShared;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class CasinoHostPreferencesTests
{
    [Fact]
    public void CapturePreservesUnrelatedSettingsAndCapsWagerToNativeBankroll()
    {
        var original = new AppPreferences
        {
            Bankroll = 500m,
            SelectedBet = 250m,
            MusicVolume = 0.17,
            Language = UiLanguagePreference.German,
            ShowEmpireBalance = false,
        };
        var blackjack = new BlackjackTableController(original) { SelectedBet = 900m };

        AppPreferences saved = CasinoHostPreferences.Capture(
            original, blackjack, null, UpdateChannel.Development, 1440, 900, true);

        Assert.Equal(500m, saved.Bankroll);
        Assert.Equal(500m, saved.SelectedBet);
        Assert.Equal(0.17, saved.MusicVolume);
        Assert.Equal(UiLanguagePreference.German, saved.Language);
        Assert.False(saved.ShowEmpireBalance);
        Assert.Same(original.Empire, saved.Empire);
        Assert.Equal(UpdateChannel.Development, saved.UpdateChannel);
        Assert.Equal(1440, saved.WindowWidth);
        Assert.Equal(900, saved.WindowHeight);
        Assert.True(saved.WindowMaximized);

        // Snapshotting must not mutate the original configuration or game state.
        Assert.Equal(250m, original.SelectedBet);
        Assert.Equal(900m, blackjack.SelectedBet);
    }
}
