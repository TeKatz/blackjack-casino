using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class SlotPlayTests
{
    [Theory]
    [InlineData(10)] [InlineData(25)] [InlineData(50)] [InlineData(100)]
    public void FiniteAutoBudgetCannotOverlapAndStopsExactlyAtBudget(int count)
    {
        var play=new SlotPlayController(); play.StartAuto(new(){Count=count});
        for(int i=0;i<count;i++)
        {
            Assert.True(play.TryBegin(true,1_000,25));
            Assert.False(play.TryBegin(true,1_000,25));
            Assert.Equal(count-i-1,play.Remaining);
            play.Complete(1_000,false,false);
        }
        Assert.False(play.CanAutoStart); Assert.False(play.TryBegin(true,1_000,25));
    }
    [Fact]
    public void StopIsImmediateDuringFeatureAndNeverStartsAgainByItself()
    {
        var play=new SlotPlayController(); Assert.False(play.AutoActive);
        play.StartAuto(new(){Count=-1}); Assert.True(play.TryBegin(true,100,5));
        play.Pause(); Assert.False(play.CanAutoStart); Assert.Equal(-1,play.Remaining);
        play.CancelAuto(); Assert.False(play.AutoActive);
        play.Resume(); play.Complete(100,false,true); Assert.False(play.CanAutoStart);
        Assert.True(play.TryBegin(false,100,5));
    }
    [Fact]
    public void AutoStopsOnConfiguredWinBonusOrFloorWithoutAnotherWager()
    {
        foreach(var settings in new[]{new AutoSpinSettings {Count=10,StopOnBigWin=true},new AutoSpinSettings{Count=10,StopOnBonus=true}})
        {
            var play=new SlotPlayController(); play.StartAuto(settings); Assert.True(play.TryBegin(true,100,5));
            play.Complete(100,settings.StopOnBigWin,settings.StopOnBonus); Assert.False(play.AutoActive);
        }
        var floor=new SlotPlayController(); floor.StartAuto(new(){Count=-1,MinimumBalance=90});
        Assert.False(floor.TryBegin(true,100,25)); Assert.False(floor.Busy); Assert.False(floor.AutoActive);
    }
    [Fact]
    public void LiveStopConditionsAndSelectingOffApplyWithoutRestartingTheBudget()
    {
        var play=new SlotPlayController(); play.StartAuto(new(){Count=25});
        Assert.True(play.TryBegin(true,100,1));
        play.UpdateSettings(new(){Count=25,StopOnBonus=true});
        Assert.Equal(24,play.Remaining);
        play.Complete(100,false,true); Assert.False(play.AutoActive);
        play.StartAuto(new(){Count=-1}); play.UpdateSettings(new(){Count=0}); Assert.False(play.CanAutoStart);
    }

    [Fact]
    public void InvalidSavedConfigurationIsSanitizedButNeverActivatesAuto()
    {
        var settings=new AutoSpinSettings {Count=-7,MinimumBalance=-5};
        Assert.Equal(0,settings.Sanitize().Count); Assert.Equal(0,settings.Sanitize().MinimumBalance);
        var play=new SlotPlayController(); play.StartAuto(settings); Assert.False(play.AutoActive);
    }
}
