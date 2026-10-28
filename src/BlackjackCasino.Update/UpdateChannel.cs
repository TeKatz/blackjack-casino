namespace BlackjackCasino.Update;

public enum UpdateChannel
{
    Stable,
    Development,
}

public static class UpdateChannelExtensions
{
    public static string ToManifestValue(this UpdateChannel channel) => channel switch
    {
        UpdateChannel.Stable => "stable",
        UpdateChannel.Development => "development",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };
}
