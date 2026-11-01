using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Tests;

public sealed class MidnightMergeUpgradeProgressTests
{
    [Fact]
    public void LobbyReportsProgressToTheNextVisualUpgrade()
    {
        MergeArea lobby = MergeCatalog.Default.Areas.Single(x => x.Id == "lobby");

        Assert.Equal("dusty", MergeProgression.Current(lobby, 10).Id);
        Assert.Equal("cleanup", MergeProgression.Next(lobby, 10)!.Id);
        Assert.Equal(10, MergeProgression.PointsUntilNext(lobby, 10));
        Assert.Equal(.5, MergeProgression.UpgradeProgress(lobby, 10), 6);

        Assert.Equal("cleanup", MergeProgression.Current(lobby, 20).Id);
        Assert.Equal("lights", MergeProgression.Next(lobby, 20)!.Id);
        Assert.Equal(30, MergeProgression.PointsUntilNext(lobby, 20));
        Assert.Equal(0, MergeProgression.UpgradeProgress(lobby, 20));
    }

    [Fact]
    public void BarUsesTheSameProgressFrameworkWithoutLevelSpecificMath()
    {
        MergeCatalog catalog = MergeCatalog.Default;
        MergeArea bar = MergeProgression.ActiveArea(catalog, 1434);

        Assert.Equal("bar", bar.Id);
        Assert.Equal("stocked", MergeProgression.Current(bar, 1434).Id);
        Assert.Equal("open", MergeProgression.Next(bar, 1434)!.Id);
        Assert.Equal(1066, MergeProgression.PointsUntilNext(bar, 1434));
        Assert.Equal(234d / 1300d, MergeProgression.UpgradeProgress(bar, 1434), 6);
    }

    [Fact]
    public void CompletedAreaReportsNoFurtherUpgrade()
    {
        MergeArea bar = MergeCatalog.Default.Areas.Single(x => x.Id == "bar");

        Assert.Null(MergeProgression.Next(bar, 2500));
        Assert.Equal(0, MergeProgression.PointsUntilNext(bar, 2500));
        Assert.Equal(1, MergeProgression.UpgradeProgress(bar, 2500));
    }
}
