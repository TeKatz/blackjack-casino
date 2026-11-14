using BlackjackCasino.Core.Hosting;
using Xunit;

namespace BlackjackCasino.Tests;

public sealed class HostManagedProfilePolicyTests
{
    [Theory]
    [InlineData(true, true, true, ManagedProfileSource.Candidate)]
    [InlineData(true, false, true, ManagedProfileSource.Candidate)]
    [InlineData(true, false, false, ManagedProfileSource.Candidate)]
    [InlineData(false, true, true, ManagedProfileSource.ExistingNative)]
    [InlineData(false, true, false, ManagedProfileSource.ExistingNative)]
    [InlineData(false, false, true, ManagedProfileSource.ImportLegacy)]
    [InlineData(false, false, false, ManagedProfileSource.FreshNative)]
    public void ExistingNativeDataAlwaysWinsBeforeLegacyImport(
        bool candidate, bool native, bool legacy, ManagedProfileSource expected)
    {
        Assert.Equal(expected, HostManagedProfilePolicy.Select(candidate, native, legacy));
    }
}
