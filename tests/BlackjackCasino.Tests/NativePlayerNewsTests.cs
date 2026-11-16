using System.Text.Json;
using BlackjackCasino.NativeShell;

namespace BlackjackCasino.Tests;

public sealed class NativePlayerNewsTests
{
    private static string Feed(string title, string titleDe) =>
        JsonSerializer.Serialize(new
        {
            version = "0.8.3-dev.123",
            sourceCommit = "abc123",
            changes = new[]
            {
                new { title = "fix(native): refactor internal route bridge",
                    description = "src/foo.tsx, test/update-manifest.json" },
            },
            playerNews = new[]
            {
                new
                {
                    title, titleDe, description = "Play more games in the lobby.",
                    descriptionDe = "Entdecke weitere Spiele in der Lobby.",
                    category = "feature", accent = "#d2b273",
                },
            },
        });

    [Fact]
    public void PublishedEditorialNotesAreLocalizedAndNeverCopyDeveloperCommits()
    {
        NativePlayerNewsFeed feed = NativePlayerNews.FromJson(
            Feed("New games in the casino", "Neue Spiele im Casino"));
        Assert.True(feed.ReleaseSpecific);
        Assert.Equal("0.8.3-dev.123", feed.InstalledVersion);
        NativePlayerNewsEntry note = Assert.Single(feed.Entries);
        Assert.Equal("New games in the casino", note.Title);
        Assert.Equal("Neue Spiele im Casino", note.TitleDe);
        Assert.Equal("#d2b273", note.Accent);
        Assert.DoesNotContain("src/foo.tsx", note.Description);
        Assert.DoesNotContain("refactor", note.Title);
    }

    [Fact]
    public void LegacyTechnicalOnlyReleasesFallbackToGenuineGameHighlights()
    {
        NativePlayerNewsFeed feed = NativePlayerNews.FromJson(
            "{\"version\":\"old\",\"changes\":[{\"title\":\"fix(ci): technical\"}]}");
        Assert.False(feed.ReleaseSpecific);
        Assert.NotEmpty(feed.Entries);
        Assert.All(feed.Entries, note =>
        {
            Assert.False(string.IsNullOrWhiteSpace(note.TitleDe));
            Assert.False(string.IsNullOrWhiteSpace(note.DescriptionDe));
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{invalid")]
    [InlineData("{}")]
    [InlineData("{\"playerNews\":[]}")]
    public void BrokenOrMissingInstalledFileCannotBlockNews(string? json)
    {
        NativePlayerNewsFeed feed = NativePlayerNews.FromJson(json);
        Assert.False(feed.ReleaseSpecific);
        Assert.NotEmpty(feed.Entries);
    }

    [Fact]
    public void InvalidCuratedEntriesAreRejectedWithoutExposingRawChangelog()
    {
        string valid = Feed("Casino news", "Casino-Neuigkeiten");
        NativePlayerNewsFeed invalid = NativePlayerNews.FromJson(
            valid.Replace("#d2b273", "javascript:alert(1)", StringComparison.Ordinal));
        Assert.False(invalid.ReleaseSpecific);
        Assert.NotEmpty(invalid.Entries);
    }
}
