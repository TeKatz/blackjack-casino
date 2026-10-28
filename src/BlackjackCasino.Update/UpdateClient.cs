namespace BlackjackCasino.Update;

public sealed record UpdateCheckResult(
    SemanticVersion CurrentVersion,
    SemanticVersion LatestVersion,
    bool UpdateAvailable,
    UpdateManifest Manifest);

public sealed class UpdateClient
{
    private readonly IUpdateSource source;
    private readonly string channel;

    public UpdateClient(IUpdateSource source, string channel = "stable")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        this.source = source;
        this.channel = channel;
    }

    public async Task<UpdateCheckResult> CheckAsync(SemanticVersion currentVersion, CancellationToken cancellationToken = default)
    {
        UpdateManifest manifest = await source.GetLatestAsync(cancellationToken);
        ValidateManifest(manifest);

        SemanticVersion latest = SemanticVersion.Parse(manifest.Version);
        return new UpdateCheckResult(currentVersion, latest, latest > currentVersion, manifest);
    }

    private void ValidateManifest(UpdateManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported update manifest schema {manifest.SchemaVersion}.");
        }

        if (!string.Equals(manifest.Channel, channel, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Expected update channel '{channel}', but manifest is for '{manifest.Channel}'.");
        }

        _ = SemanticVersion.Parse(manifest.Version);
        _ = SemanticVersion.Parse(manifest.MinimumUpdaterVersion);

        if (!Uri.TryCreate(manifest.Package.Url, UriKind.Absolute, out Uri? packageUri)
            || packageUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException("Update package URL must be an absolute HTTPS URL.");
        }

        if (manifest.Package.Sha256.Length != 64 || !manifest.Package.Sha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("Update package SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        if (manifest.Package.SizeBytes <= 0)
        {
            throw new InvalidDataException("Update package size must be positive.");
        }
    }
}
