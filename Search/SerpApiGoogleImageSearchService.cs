using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// SerpApi Google Images API istemcisi. Kota hatalarını <see cref="SerpApiQuotaException"/> olarak yükseltir.
/// </summary>
public sealed class SerpApiGoogleImageSearchService : IGoogleImageSearchService, IDisposable
{
    private const string ApiBase = "https://serpapi.com/search";
    private const string DefaultApiKey = "a11308ba13dfaf7a35ad036e5f87dbe3b57d8ec6c240c26659810b8164bffffc";

    private static readonly Regex QuotaErrorPattern = new(
        @"quota|limit|exhausted|billing|payment|402|out of searches|ran out",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public SerpApiGoogleImageSearchService(string? apiKey = null, HttpClient? httpClient = null)
    {
        _apiKey = apiKey
            ?? Environment.GetEnvironmentVariable("SERPAPI_KEY")
            ?? DefaultApiKey;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<IReadOnlyList<ImageResult>> SearchAsync(
        ImageSearchRequest request,
        int maxResults,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Array.Empty<ImageResult>();

        var url = BuildUrl(request);
        var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        EnsureNotQuotaError(response.StatusCode, json);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = json.Length > 200 ? json[..200] + "..." : json;
            throw new InvalidOperationException(
                $"SerpApi HTTP {(int)response.StatusCode}. Sorgu: \"{request.Query}\". Yanıt: {snippet}");
        }

        return ParseResults(json, maxResults);
    }

    private string BuildUrl(ImageSearchRequest request)
    {
        var parts = new List<string>
        {
            $"{ApiBase}?engine=google_images",
            $"q={Uri.EscapeDataString(request.Query)}",
            $"api_key={Uri.EscapeDataString(_apiKey)}",
            "ijn=0"
        };

        if (request.UseLargeImageFilter)
            parts.Add($"tbs={Uri.EscapeDataString(ImageToolsOptions.LargeImageFilter)}");

        return string.Join("&", parts);
    }

    private static IReadOnlyList<ImageResult> ParseResults(string json, int maxResults)
    {
        var results = new List<ImageResult>();
        var root = JObject.Parse(json);
        var arr = root["images_results"] as JArray;
        if (arr == null)
            return results;

        foreach (var item in arr)
        {
            if (results.Count >= maxResults) break;
            var orig = item["original"]?.ToString();
            if (string.IsNullOrEmpty(orig)) continue;

            var w = item["original_width"]?.Value<int>();
            var h = item["original_height"]?.Value<int>();
            results.Add(new ImageResult(orig, w, h));
        }

        return results;
    }

    private static void EnsureNotQuotaError(HttpStatusCode statusCode, string json)
    {
        if ((int)statusCode == 402 || statusCode == HttpStatusCode.PaymentRequired)
            throw new SerpApiQuotaException(BuildQuotaMessage(json));

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var root = JObject.Parse(json);
            var error = root["error"]?.ToString();
            if (!string.IsNullOrEmpty(error) && QuotaErrorPattern.IsMatch(error))
                throw new SerpApiQuotaException(BuildQuotaMessage(error));
        }
        catch (SerpApiQuotaException)
        {
            throw;
        }
        catch
        {
            if (QuotaErrorPattern.IsMatch(json))
                throw new SerpApiQuotaException(BuildQuotaMessage(json));
        }
    }

    private static string BuildQuotaMessage(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? "SerpApi kotası tükendi."
            : $"SerpApi kotası tükendi. Detay: {detail.Trim()}";

    public void Dispose() => _http.Dispose();
}
