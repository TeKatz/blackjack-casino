using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class BlackjackTableControllerTests
{
    private static BlackjackModernCommand Command(string action, string? value = null,
        GameRules? rules = null) => new(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"),
            0, action, value, rules);

    [Fact]
    public void RestoresPersistedRulesAndSettingsWithoutAnyWpfControls()
    {
        var custom = new GameRules { MinimumBet = 10m, MaximumBet = 100m, AllowInsurance = false };
        var prefs = new AppPreferences
        {
            TablePreset = TablePresetId.Custom, CustomRules = custom,
            SelectedBet = 250m, Bankroll = 500m, AiSeatCount = 5,
            PlayMode = PlayModePreference.FullTable, Theme = TableThemePreference.Burgundy,
            SoundEnabled = false, AnimationsEnabled = false,
        };
        var table = new BlackjackTableController(prefs);
        Assert.Equal(TablePresetId.Custom, table.CurrentPreset);
        Assert.Equal(custom, table.Session.Rules);
        Assert.Equal(100m, table.SelectedBet);
        Assert.Equal(5, table.AiSeatCount);
        Assert.Equal(PlayModePreference.FullTable, table.PlayMode);
        Assert.Equal(TableThemePreference.Burgundy, table.Theme);
        Assert.False(table.SoundEnabled);
        Assert.False(table.AnimationsEnabled);
    }

    [Fact]
    public void AppliesNativeValidatedConfigurationAndReusesBankroll()
    {
        var table = new BlackjackTableController(new AppPreferences { Bankroll = 300m });
        Assert.True(table.Apply(Command("mode.set", "full")).Success);
        Assert.True(table.Apply(Command("seats.set", "4")).Success);
        Assert.True(table.Apply(Command("preset.set", "European")).Success);
        Assert.Equal(PlayModePreference.FullTable, table.PlayMode);
        Assert.Equal(4, table.AiSeatCount);
        Assert.Equal(TablePresetId.European, table.CurrentPreset);
        Assert.Equal(300m, table.Session.Bankroll);
        CasinoSession original = table.Session;
        Assert.True(table.Apply(Command("theme.set", "MidnightBlue")).Success);
        Assert.True(table.Apply(Command("sound.set", "false")).Success);
        Assert.Same(original, table.Session);
        Assert.Equal(TableThemePreference.MidnightBlue, table.Theme);
        Assert.False(table.SoundEnabled);
    }

    [Fact]
    public void RejectsIllegalBetsAndProtectsActiveRoundFromReconfiguration()
    {
        var shoe = new FixedShoe(
            new Card(Rank.Ten, Suit.Spades), new Card(Rank.Seven, Suit.Clubs),
            new Card(Rank.Five, Suit.Hearts), new Card(Rank.Nine, Suit.Diamonds),
            new Card(Rank.Six, Suit.Clubs), new Card(Rank.Four, Suit.Spades));
        var table = new BlackjackTableController(
            new AppPreferences { Bankroll = 1000m, SelectedBet = 25m }, () => shoe);
        Assert.False(table.Apply(Command("bet.set", "1001")).Success);
        Assert.Equal(1000m, table.Session.Bankroll);
        Assert.True(table.Apply(Command("deal")).Success);
        Assert.True(table.Session.RoundInProgress);
        CasinoSession original = table.Session;
        Assert.False(table.Apply(Command("mode.set", "full")).Success);
        Assert.False(table.Apply(Command("rules.custom.set", null, new GameRules())).Success);
        Assert.False(table.Apply(Command("bankroll.reset")).Success);
        Assert.Same(original, table.Session);
        Assert.True(table.Apply(Command("stand")).Success);
        Assert.False(table.Session.RoundInProgress);
        Assert.True(table.Apply(Command("preset.set", "Casual")).Success);
        Assert.Same(table.CurrentRules, table.Session.Rules is null ? null : table.CurrentRules);
    }

    private sealed class FixedShoe(params Card[] values) : ICardShoe
    {
        private readonly Queue<Card> cards = new(values);
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
