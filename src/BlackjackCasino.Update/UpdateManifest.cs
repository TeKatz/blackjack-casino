using System.Text.Json.Serialization;

namespace BlackjackCasino.Update;

public sealed record UpdateManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("minimumUpdaterVersion")]
    public string MinimumUpdaterVersion { get; init; } = "0.1.0";

    [JsonPropertyName("package")]
    public required UpdatePackage Package { get; init; }

    [JsonPropertyName("notesUrl")]
    public string? NotesUrl { get; init; }
}

public sealed record UpdatePackage
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }
}
