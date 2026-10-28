using System.Security.Cryptography;

namespace BlackjackCasino.Update;

public sealed class PackageDownloader
{
    private readonly HttpClient httpClient;

    public PackageDownloader(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.httpClient = httpClient;
    }

    public async Task<string> DownloadAndVerifyAsync(
        UpdatePackage package,
        string destinationDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        Directory.CreateDirectory(destinationDirectory);
        string destinationPath = Path.Combine(destinationDirectory, $"blackjack-update-{Guid.NewGuid():N}.zip");

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                package.Url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength is > 0 && package.SizeBytes > 0 && contentLength != package.SizeBytes)
            {
                throw new InvalidDataException($"Package size mismatch. Manifest: {package.SizeBytes}, server: {contentLength}.");
            }

            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream output = new(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 128,
                useAsync: true);

            byte[] buffer = new byte[1024 * 128];
            long downloaded = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;
                if (package.SizeBytes > 0)
                {
                    progress?.Report(Math.Clamp((double)downloaded / package.SizeBytes, 0d, 1d));
                }
            }

            await output.FlushAsync(cancellationToken);

            if (downloaded != package.SizeBytes)
            {
                throw new InvalidDataException($"Downloaded package size mismatch. Expected {package.SizeBytes}, got {downloaded}.");
            }

            await VerifySha256Async(destinationPath, package.Sha256, cancellationToken);
            progress?.Report(1d);
            return destinationPath;
        }
        catch
        {
            TryDelete(destinationPath);
            throw;
        }
    }

    public static async Task VerifySha256Async(string path, string expectedSha256, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, useAsync: true);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        string actual = Convert.ToHexString(hash);

        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"SHA-256 mismatch. Expected {expectedSha256}, got {actual}.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
