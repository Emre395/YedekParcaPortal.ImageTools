using Newtonsoft.Json.Linq;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// SerpApi Google Images API kullanarak görsel arama. Original URL ve boyut bilgisi döner;
/// 400x400 altı sonuçlar listeden çıkarılır.
/// </summary>
public sealed class SerpApiImageSearchService : IImageSearchService, IDisposable
{
    private const string ApiBase = "https://serpapi.com/search";
    private const string FallbackApiKey = "272460c352cbfdb85063f4707504e823a8b1c50e52e2d9d4dfd33f21896e288d";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public SerpApiImageSearchService(string? apiKey = null, HttpClient? httpClient = null)
    {
        _apiKey = apiKey
            ?? Environment.GetEnvironmentVariable("SERPAPI_KEY")
            ?? FallbackApiKey;
        _http = httpClient ?? new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImageResult>> SearchImagesAsync(
        string query,
        int maxResults = 10,
        int minWidthPx = 400,
        int minHeightPx = 400,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<ImageResult>();

        var results = new List<ImageResult>();
        var url = $"{ApiBase}?engine=google_images&q={Uri.EscapeDataString(query)}&api_key={Uri.EscapeDataString(_apiKey)}&ijn=0";

        try
        {
            var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var snippet = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                var msg = $"SerpApi HTTP {(int)response.StatusCode}. Sorgu: \"{query}\". Yanıt: {snippet}";
                throw new InvalidOperationException(msg);
            }

            var root = JObject.Parse(json);
            var arr = root?["images_results"] as JArray;
            if (arr == null)
                return results;

            foreach (var item in arr)
            {
                if (results.Count >= maxResults) break;
                var orig = item["original"]?.ToString();
                if (string.IsNullOrEmpty(orig)) continue;

                var w = item["original_width"]?.Value<int>();
                var h = item["original_height"]?.Value<int>();
                if (w.HasValue && h.HasValue && (w.Value < minWidthPx || h.Value < minHeightPx))
                    continue;

                results.Add(new ImageResult(orig, w, h));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"SerpApi görsel araması hata. Sorgu: \"{query}\". URL: {url}", ex);
        }

        return results;
    }

    public void Dispose() => _http.Dispose();
}
