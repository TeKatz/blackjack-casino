using System.Globalization;

namespace BlackjackCasino.Core;

public sealed record DailyLoginStatus(
    bool CanClaim,
    bool ClaimedToday,
    int TotalClaims,
    int CycleCompletedSteps,
    int ActiveStep,
    decimal ActiveReward,
    int NextStep,
    decimal NextReward);

public sealed record DailyLoginClaim(
    decimal Reward,
    int TotalClaims,
    string ClaimDate,
    DailyLoginStatus Status);

public static class DailyLoginProgress
{
    private const string DateFormat = "yyyy-MM-dd";

    public static DailyLoginStatus GetStatus(string? lastClaimDate, int completedClaims, DateOnly today)
    {
        int claims = Math.Max(0, completedClaims);
        string todayText = today.ToString(DateFormat, CultureInfo.InvariantCulture);
        bool claimedToday = string.Equals(lastClaimDate, todayText, StringComparison.Ordinal);
        int cycleLength = DailyLoginRewardSchedule.CycleLength;

        if (!claimedToday)
        {
            int completedInCycle = claims % cycleLength;
            int activeStep = completedInCycle + 1;
            decimal activeReward = DailyLoginRewardSchedule.GetReward(claims);
            return new DailyLoginStatus(
                true,
                false,
                claims,
                completedInCycle,
                activeStep,
                activeReward,
                activeStep,
                activeReward);
        }

        int claimedStep = claims <= 0 ? 1 : ((claims - 1) % cycleLength) + 1;
        int completedSteps = claims <= 0 ? 0 : claims % cycleLength == 0 ? cycleLength : claims % cycleLength;
        decimal claimedReward = claims <= 0 ? DailyLoginRewardSchedule.GetReward(0) : DailyLoginRewardSchedule.GetReward(claims - 1);
        int nextStep = (claims % cycleLength) + 1;
        decimal nextReward = DailyLoginRewardSchedule.GetReward(claims);

        return new DailyLoginStatus(
            false,
            true,
            claims,
            completedSteps,
            claimedStep,
            claimedReward,
            nextStep,
            nextReward);
    }

    public static DailyLoginClaim? TryClaim(string? lastClaimDate, int completedClaims, DateOnly today)
    {
        DailyLoginStatus before = GetStatus(lastClaimDate, completedClaims, today);
        if (!before.CanClaim)
        {
            return null;
        }

        int nextClaims = checked(before.TotalClaims + 1);
        string claimDate = today.ToString(DateFormat, CultureInfo.InvariantCulture);
        DailyLoginStatus after = GetStatus(claimDate, nextClaims, today);
        return new DailyLoginClaim(before.ActiveReward, nextClaims, claimDate, after);
    }
}
