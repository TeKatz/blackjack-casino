using System.Text.Json;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class AreaPresentationTests
{
    [Fact]
    public void LegacySettingsAreCopiedOnceAndRemainIndependent()
    {
        AppPreferences legacy = JsonSerializer.Deserialize<AppPreferences>("""{"SoundEnabled":false,"AnimationsEnabled":false,"MusicEnabled":false,"MusicVolume":0.07}""")!;
        AppPreferences migrated = legacy.InitializePresentationAreas();
        AppPreferences changed = migrated with { SoundEnabled = true, AnimationsEnabled = true, MusicEnabled = true, MusicVolume = .25 };
        foreach (string area in AreaPresentationPreferences.Areas)
        {
            Assert.False(changed.Presentation(area).SoundEnabled);
            Assert.False(changed.Presentation(area).AnimationsEnabled);
            Assert.False(changed.Presentation(area).MusicEnabled);
            Assert.Equal(.07, changed.Presentation(area).MusicVolume);
        }
        AppPreferences slot = changed.WithPresentation("neon-vault", changed.Presentation("neon-vault") with { SoundEnabled = true, MusicEnabled = true });
        Assert.True(slot.Presentation("neon-vault").SoundEnabled);
        Assert.False(slot.Presentation("midnight-reels").SoundEnabled);
        Assert.False(slot.Presentation("midnight-manor").MusicEnabled);
        Assert.False(changed.Presentation("neon-vault").SoundEnabled);
    }

    [Fact]
    public void AreaSettingsSurviveRoundTripAndSanitizeMalformedVolume()
    {
        AppPreferences state = new AppPreferences().InitializePresentationAreas()
            .WithPresentation("midnight-manor", new AreaPresentationSettings { SoundEnabled = false, MusicEnabled = false, AnimationsEnabled = false, MusicVolume = .03 });
        AppPreferences restored = JsonSerializer.Deserialize<AppPreferences>(JsonSerializer.Serialize(state))!.InitializePresentationAreas();
        Assert.Equal(state.Presentation("midnight-manor"), restored.Presentation("midnight-manor"));
        Assert.True(restored.SoundEnabled);
        Assert.True(restored.Presentation("nine-lives").SoundEnabled);
        Assert.Equal(.32, restored.WithPresentation("album", new() { MusicVolume = 100 }).Presentation("album").MusicVolume);
        Assert.Equal(.10, restored.WithPresentation("album", new() { MusicVolume = double.NaN }).Presentation("album").MusicVolume);
        Assert.NotEmpty(JsonSerializer.Deserialize<AppPreferences>("""{"AreaPresentation":null}""")!.InitializePresentationAreas().AreaPresentation);
    }
}
