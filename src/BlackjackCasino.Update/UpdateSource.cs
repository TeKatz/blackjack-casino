using System.Net.Http.Json;
using System.Net.Http.Headers;
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
        // GitHub's mutable /development/ release URL may retain a cached redirect
        // even when the request sends no-cache. A per-request unique query string
        // makes each manifest fetch an independent CDN cache key. Never remove
        // the integrity checks: the downloaded ZIP must still match this manifest.
        var freshUri = new UriBuilder(manifestUri);
        string nonce = "revision=" + Guid.NewGuid().ToString("N");
        freshUri.Query = string.IsNullOrEmpty(freshUri.Query.TrimStart('?'))
            ? nonce : freshUri.Query.TrimStart('?') + "&" + nonce;
        using var request = new HttpRequestMessage(HttpMethod.Get, freshUri.Uri);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        UpdateManifest? manifest = await response.Content.ReadFromJsonAsync<UpdateManifest>(JsonOptions, cancellationToken);
        return manifest ?? throw new InvalidDataException("The update manifest was empty.");
    }
}
