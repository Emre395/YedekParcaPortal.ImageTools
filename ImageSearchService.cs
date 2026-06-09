using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// Çok kaynaklı görsel arama: logolar için Wikipedia/Wikimedia önce, sonra DuckDuckGo (vqd+JSON),
/// yedekte Google Görseller (IE11 UA ile saf HTML). Bing devre dışı.
/// </summary>
public class ImageSearchService : IDisposable
{
    private readonly HttpClient _http;

    private static readonly string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    /// <summary>Google Görseller için: eski tarayıcıya JS'siz HTML sunulur.</summary>
    private static readonly string UserAgentIE11 =
        "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

    private static readonly string Accept =
        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";

    private static readonly string AcceptLanguage = "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7";

    private const string Referer = "https://www.google.com/";

    private static readonly Regex StrictImageUrlRegex = new Regex(
        @"https?://[^\s""'<>\]\)]+\.(jpe?g|png|webp)(\?[^\s""'<>\]\)]*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JsonImageUrlRegex = new Regex(
        @"""(?:murl|image|src|url)""\s*:\s*""(https?://[^""]+\.(?:jpe?g|png|webp)[^""]*)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>DuckDuckGo sayfa kaynağından vqd değerini yakalar (vqd='...' veya vqd=...&).</summary>
    private static readonly Regex VqdRegex = new Regex(
        @"vqd\s*=\s*[""']?([\d\-]+)[""']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Google /imgres?imgurl= veya JSON "ou":"..." içindeki görsel URL'leri.</summary>
    private static readonly Regex GoogleImgUrlRegex = new Regex(
        @"(?:imgurl|ou)[""\s:=]+(https?://[^\s""'&<>\]\)]+\.(?:jpe?g|png|webp))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ImageSearchService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", Accept);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", AcceptLanguage);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", Referer);
    }

    /// <summary>
    /// Sıra: (1) Logo ise Wikipedia/Wikimedia API, (2) DuckDuckGo vqd+JSON ve HTML, (3) Yeterli yoksa Google Görseller (IE11).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetImageUrlsAsync(
        string query,
        int maxUrls = 15,
        Action<string>? onSearchUrl = null,
        bool isLogoSearch = false,
        CancellationToken ct = default)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var encoded = Uri.EscapeDataString(query);

        // 1) Logolar için önce Wikimedia Commons (stabil, temiz)
        if (isLogoSearch)
        {
            var wikiUrl = "https://commons.wikimedia.org/w/api.php?action=query&generator=search"
                + "&gsrnamespace=6&gsrsearch=" + encoded + "&gsrlimit=10"
                + "&prop=imageinfo&iiprop=url&iiurlwidth=1200&format=json&origin=*";
            onSearchUrl?.Invoke("[Wikimedia] " + wikiUrl);
            await CollectFromWikimediaAsync(wikiUrl, candidates, maxUrls * 2, ct).ConfigureAwait(false);
        }

        // 2) DuckDuckGo: önce ana sayfa ile vqd al, sonra i.js JSON
        var ddgPageUrl = "https://duckduckgo.com/?q=" + encoded + "&iax=images&ia=images";
        onSearchUrl?.Invoke("[DuckDuckGo] " + ddgPageUrl);
        var html = await FetchAsync(ddgPageUrl, referer: Referer, ct: ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(html))
        {
            CollectFromHtml(html, candidates, maxUrls * 2);
            var vqd = VqdRegex.Match(html).Groups[1].Value;
            if (!string.IsNullOrEmpty(vqd))
            {
                var iJsUrl = "https://duckduckgo.com/i.js?l=us-en&o=json&q=" + encoded
                    + "&vqd=" + Uri.EscapeDataString(vqd) + "&f=,,,&p=1&v7exp=a";
                onSearchUrl?.Invoke("[DuckDuckGo i.js] " + iJsUrl);
                var json = await FetchAsync(iJsUrl, referer: "https://duckduckgo.com/", ct: ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(json))
                    CollectFromDuckDuckGoJson(json, candidates, maxUrls * 2);
            }
        }

        // 3) DuckDuckGo HTML yedek (genel arama)
        if (candidates.Count == 0)
        {
            var ddgHtmlUrl = "https://html.duckduckgo.com/html/?q=" + encoded;
            onSearchUrl?.Invoke("[DuckDuckGo HTML] " + ddgHtmlUrl);
            html = await FetchAsync(ddgHtmlUrl, referer: Referer, ct: ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(html))
                CollectFromHtml(html, candidates, maxUrls * 2);
        }

        // 4) Google Görseller (IE11 UA ile saf HTML)
        if (candidates.Count < maxUrls)
        {
            var googleUrl = "https://www.google.com/search?q=" + encoded + "&tbm=isch&sclient=img";
            onSearchUrl?.Invoke("[Google Images IE11] " + googleUrl);
            var googleHtml = await FetchAsync(googleUrl, userAgent: UserAgentIE11, referer: "", ct: ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(googleHtml))
                CollectFromGoogleHtml(googleHtml, candidates, maxUrls * 2);
        }

        return candidates.Take(maxUrls).ToList();
    }

    private async Task CollectFromWikimediaAsync(string apiUrl, HashSet<string> candidates, int limit, CancellationToken ct)
    {
        try
        {
            var json = await FetchAsync(apiUrl, referer: null, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) return;
            var root = JObject.Parse(json);
            var pages = root["query"]?["pages"] as JObject;
            if (pages == null) return;
            foreach (var p in pages.Properties())
            {
                if (candidates.Count >= limit) break;
                var info = p.Value?["imageinfo"] as JArray;
                var first = info?[0] as JObject;
                var url = first?["url"]?.ToString();
                var thumb = first?["thumburl"]?.ToString();
                foreach (var u in new[] { url, thumb })
                {
                    if (string.IsNullOrEmpty(u)) continue;
                    if (!StrictImageUrlRegex.IsMatch(u)) continue; // sadece jpg/png/webp
                    var n = NormalizeUrl(u);
                    if (n != null && !IsBadUrl(n))
                        candidates.Add(n);
                }
            }
        }
        catch
        {
            // API hatası sessizce atlanır
        }
    }

    private static void CollectFromDuckDuckGoJson(string json, HashSet<string> candidates, int limit)
    {
        try
        {
            var root = JObject.Parse(json);
            var results = root["results"] as JArray;
            if (results == null) return;
            foreach (var r in results)
            {
                if (candidates.Count >= limit) break;
                var obj = r as JObject;
                if (obj == null) continue;
                foreach (var key in new[] { "image", "thumbnail", "url" })
                {
                    var v = obj[key]?.ToString();
                    if (string.IsNullOrEmpty(v)) continue;
                    if (!StrictImageUrlRegex.IsMatch(v)) continue;
                    var u = NormalizeUrl(v.Replace("\\u002f", "/", StringComparison.Ordinal));
                    if (u != null && !IsBadUrl(u))
                        candidates.Add(u);
                }
            }
        }
        catch
        {
            // JSON parse hatası atlanır
        }
    }

    private static void CollectFromGoogleHtml(string html, HashSet<string> candidates, int limit)
    {
        foreach (Match m in GoogleImgUrlRegex.Matches(html))
        {
            if (candidates.Count >= limit) return;
            var u = NormalizeUrl(m.Groups[1].Value);
            if (u != null && !IsBadUrl(u))
                candidates.Add(u);
        }
        foreach (Match m in StrictImageUrlRegex.Matches(html))
        {
            if (candidates.Count >= limit) return;
            var u = NormalizeUrl(m.Value);
            if (u != null && !IsBadUrl(u))
                candidates.Add(u);
        }
        foreach (Match m in JsonImageUrlRegex.Matches(html))
        {
            if (candidates.Count >= limit) return;
            var s = m.Groups[1].Value.Replace("\\u002f", "/", StringComparison.Ordinal);
            var u = NormalizeUrl(s);
            if (u != null && !IsBadUrl(u))
                candidates.Add(u);
        }
    }

    private async Task<string?> FetchAsync(string url, string? userAgent = null, string? referer = null, CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", userAgent ?? UserAgent);
            req.Headers.TryAddWithoutValidation("Accept", Accept);
            req.Headers.TryAddWithoutValidation("Accept-Language", AcceptLanguage);
            // null = varsayılan Referer; "" = Referer gönderme (ör. Google IE11).
            var refVal = referer == null ? Referer : referer;
            if (refVal.Length > 0)
                req.Headers.TryAddWithoutValidation("Referer", refVal);
            var response = await _http.SendAsync(req, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static void CollectFromHtml(string html, HashSet<string> candidates, int limit)
    {
        foreach (Match m in StrictImageUrlRegex.Matches(html))
        {
            if (candidates.Count >= limit) return;
            var u = NormalizeUrl(m.Value);
            if (u != null && !IsBadUrl(u))
                candidates.Add(u);
        }
        foreach (Match m in JsonImageUrlRegex.Matches(html))
        {
            if (candidates.Count >= limit) return;
            var g = m.Groups[1];
            if (!g.Success) continue;
            var u = NormalizeUrl(g.Value.Replace("\\u002f", "/", StringComparison.Ordinal));
            if (u != null && !IsBadUrl(u))
                candidates.Add(u);
        }

        var doc = new HtmlDocument();
        try { doc.LoadHtml(html); } catch { return; }

        var attrNames = new[] { "data-src", "data-murl", "data-superiorsrc", "murl", "src", "href" };
        foreach (var node in doc.DocumentNode.Descendants())
        {
            if (candidates.Count >= limit) break;
            foreach (var name in attrNames)
            {
                var val = node.GetAttributeValue(name, null);
                if (string.IsNullOrWhiteSpace(val)) continue;
                if (StrictImageUrlRegex.IsMatch(val))
                {
                    var u = NormalizeUrl(StrictImageUrlRegex.Match(val).Value);
                    if (u != null && !IsBadUrl(u))
                        candidates.Add(u);
                }
            }
        }

        var imgSrc = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgSrc != null)
            foreach (var n in imgSrc)
                TryAddUrl(n.GetAttributeValue("src", null), candidates, limit);

        var aHref = doc.DocumentNode.SelectNodes("//a[@href]");
        if (aHref != null)
            foreach (var n in aHref)
                TryAddUrl(n.GetAttributeValue("href", null), candidates, limit);

        var iusc = doc.DocumentNode.SelectNodes("//a[contains(@class,'iusc')]");
        if (iusc != null)
            foreach (var n in iusc)
            {
                if (candidates.Count >= limit) break;
                foreach (var name in new[] { "m", "murl", "data-murl" })
                {
                    var val = n.GetAttributeValue(name, null);
                    if (string.IsNullOrEmpty(val)) continue;
                    foreach (Match m in StrictImageUrlRegex.Matches(val))
                    {
                        var u = NormalizeUrl(m.Value);
                        if (u != null && !IsBadUrl(u)) candidates.Add(u);
                    }
                    foreach (Match m in JsonImageUrlRegex.Matches(val))
                    {
                        if (m.Groups[1].Value is { } s)
                        {
                            var u = NormalizeUrl(s.Replace("\\u002f", "/", StringComparison.Ordinal));
                            if (u != null && !IsBadUrl(u)) candidates.Add(u);
                        }
                    }
                }
            }
    }

    private static void TryAddUrl(string? raw, HashSet<string> candidates, int limit)
    {
        if (candidates.Count >= limit || string.IsNullOrWhiteSpace(raw)) return;
        if (!StrictImageUrlRegex.IsMatch(raw)) return;
        var u = NormalizeUrl(StrictImageUrlRegex.Match(raw).Value);
        if (u != null && !IsBadUrl(u))
            candidates.Add(u);
    }

    private static bool IsBadUrl(string u)
    {
        if (u.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return true;
        // Sahte/kırık: https://www.png gibi (host görsel uzantısıyla bitiyorsa)
        try
        {
            var uri = new Uri(u);
            var host = (uri.Host ?? "").ToLowerInvariant();
            if (host.EndsWith(".png", StringComparison.Ordinal) || host.EndsWith(".jpg", StringComparison.Ordinal)
                || host.EndsWith(".jpeg", StringComparison.Ordinal) || host.EndsWith(".webp", StringComparison.Ordinal))
                return true;
        }
        catch { return true; }
        var lower = u.ToLowerInvariant();
        if (lower.Contains("bing.com", StringComparison.Ordinal)) return true;
        if (lower.Contains("bing.com/sa", StringComparison.Ordinal)) return true;
        if (lower.Contains("facebook_sharing", StringComparison.Ordinal)) return true;
        if (lower.Contains("placeholder", StringComparison.Ordinal)) return true;
        if (lower.Contains("logo-sharing", StringComparison.Ordinal)) return true;
        if (lower.Contains("favicon", StringComparison.Ordinal)) return true;
        if (lower.Contains("logo-icon", StringComparison.Ordinal)) return true;
        if (lower.Contains("button", StringComparison.Ordinal)) return true;
        if (lower.Contains("ad-pixel", StringComparison.Ordinal)) return true;
        if (lower.Contains("sprite", StringComparison.Ordinal)) return true;
        if (lower.Contains("/icon") || lower.Contains("icon.") || lower.Contains("-icon-") || lower.EndsWith("icon", StringComparison.Ordinal)) return true;
        return false;
    }

    private static string? NormalizeUrl(string u)
    {
        u = u.Trim();
        if (string.IsNullOrEmpty(u)) return null;
        if (u.StartsWith("//", StringComparison.Ordinal))
            u = "https:" + u;
        else if (!u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return null;
        return u;
    }

    public void Dispose() => _http.Dispose();
}
