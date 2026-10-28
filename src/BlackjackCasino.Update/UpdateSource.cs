using System.Net.Http.Json;
using System.Text.Json;

namespace BlackjackCasino.Update;

public interface IUpdateSource
{
    Task<UpdateManifest> GetLatestAsync(CancellationToken cancellationToken = default);
}

public sealed class HttpManifestUpdateSource : IUpdateSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;
    private readonly Uri manifestUri;

    public HttpManifestUpdateSource(HttpClient httpClient, Uri manifestUri)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(manifestUri);

        this.httpClient = httpClient;
        this.manifestUri = manifestUri;
    }

    public async Task<UpdateManifest> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        UpdateManifest? manifest = await httpClient.GetFromJsonAsync<UpdateManifest>(manifestUri, JsonOptions, cancellationToken);
        return manifest ?? throw new InvalidDataException("The update manifest was empty.");
    }
}
