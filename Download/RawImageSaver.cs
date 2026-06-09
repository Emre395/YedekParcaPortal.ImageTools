using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Filtering;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Download;

/// <summary>
/// Aday URL'leri indirir; Image.Identify ile 600×600 doğrulaması yapar, ham byte yazar.
/// </summary>
public sealed class RawImageSaver : IRawImageSaver, IDisposable
{
    private readonly HttpClient _http;
    private readonly IImageCandidateFilter _metadataFilter;
    private readonly IImageDimensionInspector _dimensionInspector;
    private readonly int _minWidth;
    private readonly int _minHeight;

    public RawImageSaver(
        IImageCandidateFilter metadataFilter,
        IImageDimensionInspector dimensionInspector,
        HttpClient? httpClient = null,
        int minWidth = ImageToolsOptions.MinImageWidthPx,
        int minHeight = ImageToolsOptions.MinImageHeightPx)
    {
        _metadataFilter = metadataFilter;
        _dimensionInspector = dimensionInspector;
        _minWidth = minWidth;
        _minHeight = minHeight;
        _http = httpClient ?? CreateHttpClient();
    }

    public async Task<RawSaveAttemptResult> TrySaveFirstDownloadableAsync(
        IReadOnlyList<ImageResult> candidates,
        string outputDirectory,
        string baseFileName,
        CancellationToken ct = default)
    {
        if (candidates.Count == 0)
            return RawSaveAttemptResult.Failed(RawSaveFailureKind.NoCandidates);

        var hadTransportAttempt = false;
        var allFailuresWereTransport = true;

        foreach (var candidate in candidates)
        {
            if (!_metadataFilter.Accepts(candidate))
            {
                allFailuresWereTransport = false;
                continue;
            }

            try
            {
                var resp = await _http.GetAsync(candidate.Url, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    hadTransportAttempt = true;
                    continue;
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                if (bytes.Length == 0)
                {
                    hadTransportAttempt = true;
                    continue;
                }

                var dimensions = _dimensionInspector.TryIdentify(bytes);
                if (dimensions is { } d)
                {
                    if (d.Width < _minWidth || d.Height < _minHeight)
                    {
                        allFailuresWereTransport = false;
                        continue;
                    }
                }
                else
                {
                    allFailuresWereTransport = false;
                    continue;
                }

                var ext = GuessExtension(candidate.Url, resp.Content.Headers.ContentType?.MediaType);
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                var filePath = Path.Combine(outputDirectory, $"{baseFileName}.{ext}");
                await File.WriteAllBytesAsync(filePath, bytes, ct).ConfigureAwait(false);
                return RawSaveAttemptResult.Saved(filePath);
            }
            catch (HttpRequestException)
            {
                hadTransportAttempt = true;
                continue;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                hadTransportAttempt = true;
                continue;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                hadTransportAttempt = true;
                continue;
            }
        }

        if (hadTransportAttempt && allFailuresWereTransport)
            return RawSaveAttemptResult.Failed(RawSaveFailureKind.AllTransportErrors);

        return RawSaveAttemptResult.Failed(RawSaveFailureKind.QualityRejected);
    }

    private static HttpClient CreateHttpClient()
    {
        var h = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ImageToolsOptions.DownloadTimeoutSeconds)
        };
        h.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return h;
    }

    private static string GuessExtension(string url, string? contentType)
    {
        try
        {
            var fromUrl = Path.GetExtension(new Uri(url).AbsolutePath);
            if (!string.IsNullOrEmpty(fromUrl))
            {
                var ext = fromUrl.TrimStart('.').ToLowerInvariant();
                if (ext is "jpg" or "jpeg" or "png" or "webp" or "gif" or "bmp")
                    return ext == "jpeg" ? "jpg" : ext;
            }
        }
        catch
        {
            // URL parse edilemezse content-type kullanılır.
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            var ct = contentType.ToLowerInvariant();
            if (ct.Contains("jpeg")) return "jpg";
            if (ct.Contains("png")) return "png";
            if (ct.Contains("webp")) return "webp";
            if (ct.Contains("gif")) return "gif";
            if (ct.Contains("bmp")) return "bmp";
        }

        return "jpg";
    }

    public void Dispose() => _http.Dispose();
}
