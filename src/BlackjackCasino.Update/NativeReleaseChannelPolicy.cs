namespace BlackjackCasino.Update;

/// <summary>
/// A native Development build must never install a still-WPF Stable package,
/// nor can a Stable build accept a Development downgrade implicitly.
/// Switching channels is a separate, explicitly-tested installer operation.
/// </summary>
public static class NativeReleaseChannelPolicy
{
    public static UpdateChannel InstalledChannel(SemanticVersion installed) =>
        installed.PreRelease is null ? UpdateChannel.Stable : UpdateChannel.Development;

    public static bool MayInstall(SemanticVersion installed, UpdateChannel selected) =>
        selected == InstalledChannel(installed);
}
