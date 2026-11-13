using System.Text.Json;
using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.Tests;

public sealed class HostCutoverLaunchPolicyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "host-launch-test-" + Guid.NewGuid().ToString("N"));
    private string Install => Path.Combine(root, "install");
    private string AppData => Path.Combine(root, "local-data");

    public HostCutoverLaunchPolicyTests()
    {
        Directory.CreateDirectory(Install);
        Directory.CreateDirectory(AppData);
        File.WriteAllText(Path.Combine(Install, HostCutoverLaunchPolicy.LegacyExecutable), "fallback");
    }

    [Fact]
    public void LegacyIsAlwaysDefaultAndDoesNotRequireNativeOrCandidate()
    {
        HostCutoverLaunchPlan plan = HostCutoverLaunchPolicy.Resolve(Install, AppData);
        Assert.False(plan.UsesNativeHost);
        Assert.EndsWith(HostCutoverLaunchPolicy.LegacyExecutable, plan.Executable);
        Assert.Empty(plan.Arguments);
        Assert.Empty(HostCutoverLaunchPolicy.AvailableCandidates(Install, AppData));
    }

    [Fact]
    public void StagedCandidateCanOnlyLaunchWithExplicitOptIn()
    {
        WriteCandidate("rehearsal_2");
        HostCutoverLaunchPlan plan = HostCutoverLaunchPolicy.Resolve(Install, AppData, "rehearsal_2");
        Assert.True(plan.UsesNativeHost);
        Assert.Equal(new[] { "--candidate", "rehearsal_2" }, plan.Arguments);
        Assert.Equal(new[] { "rehearsal_2" },
            HostCutoverLaunchPolicy.AvailableCandidates(Install, AppData));
        Assert.False(HostCutoverLaunchPolicy.Resolve(Install, AppData).UsesNativeHost);
    }

    [Theory]
    [InlineData("../production")]
    [InlineData("other/candidate")]
    [InlineData("")]
    [InlineData("-escape")]
    public void CandidateTraversalAndMalformedNamesAreRejected(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            HostCutoverLaunchPolicy.GetCandidateDirectory(AppData, name));
    }

    [Fact]
    public void MissingFallbackBinaryBlocksEvenAValidNativeCandidate()
    {
        WriteCandidate("test1");
        File.Delete(Path.Combine(Install, HostCutoverLaunchPolicy.LegacyExecutable));
        Assert.Throws<FileNotFoundException>(() =>
            HostCutoverLaunchPolicy.Resolve(Install, AppData, "test1"));
    }

    [Fact]
    public void MissingOrMismatchedManifestNeverStartsTheNativeHost()
    {
        WriteCandidate("test1");
        string path = HostCutoverLaunchPolicy.GetCandidateDirectory(AppData, "test1");
        string marker = Path.Combine(path, "native-migration-manifest.json");
        File.WriteAllText(marker, JsonSerializer.Serialize(new
        {
            Version = 2, Candidate = "another", Files = new[] { new { Name = "profile.json" } },
        }));
        Assert.Throws<InvalidDataException>(() =>
            HostCutoverLaunchPolicy.Resolve(Install, AppData, "test1"));
        Assert.Empty(HostCutoverLaunchPolicy.AvailableCandidates(Install, AppData));
    }

    [Fact]
    public void UnstagedProfilesCannotBeUsedToStartNative()
    {
        string path = HostCutoverLaunchPolicy.GetCandidateDirectory(AppData, "unknown");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "player-state.json"), "{}");
        Assert.Throws<FileNotFoundException>(() =>
            HostCutoverLaunchPolicy.Resolve(Install, AppData, "unknown"));
    }

    private void WriteCandidate(string name)
    {
        File.WriteAllText(Path.Combine(Install, HostCutoverLaunchPolicy.NativeExecutable), "native");
        string path = HostCutoverLaunchPolicy.GetCandidateDirectory(AppData, name);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "player-state.json"), "{}");
        File.WriteAllText(Path.Combine(path, "profile.json"), "{}");
        File.WriteAllText(Path.Combine(path, "native-migration-manifest.json"),
            JsonSerializer.Serialize(new
            {
                Version = 2, Candidate = name,
                Files = new[] { new { Name = "profile.json" }, new { Name = "player-state.json" } },
            }));
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
