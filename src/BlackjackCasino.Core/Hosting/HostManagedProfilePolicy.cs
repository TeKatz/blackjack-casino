namespace BlackjackCasino.Core.Hosting;

/// <summary>
/// NativeShell player data is authoritative once it exists. Never re-import
/// potentially stale WPF data over an existing native save, including users
/// who began playing in the default fresh-install storage location.
/// </summary>
public enum ManagedProfileSource
{
    Candidate,
    ExistingNative,
    ImportLegacy,
    FreshNative,
}

public static class HostManagedProfilePolicy
{
    public static ManagedProfileSource Select(
        bool candidateExists, bool defaultNativeSaveExists, bool legacySaveExists)
    {
        if (candidateExists) return ManagedProfileSource.Candidate;
        if (defaultNativeSaveExists) return ManagedProfileSource.ExistingNative;
        if (legacySaveExists) return ManagedProfileSource.ImportLegacy;
        return ManagedProfileSource.FreshNative;
    }
}
