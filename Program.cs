using System.Diagnostics;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools;

internal static class Program
{
    private const int IndirmeTimeoutSaniye = 30;
    private const int MaxAdayPerArama = 5;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var sw = Stopwatch.StartNew();
        var log = new Action<string>(m => Console.WriteLine(m));

        Console.WriteLine("════════════════════════════════════════════════════════════");
        log("  ImageTools – İnternetten görsel arama ve ham kayıt");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        var excelPath = PathConfig.GetSampleExcelPath();
        ExcelSeed.SeedSampleExcelIfMissing(excelPath);

        var outputDir = PathConfig.ResolveOutputPath();
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        log($"Çıktı klasörü: {outputDir}");
        log($"Excel dosyası: {excelPath}");

        using var search = new SerpApiImageSearchService();
        using var downloader = CreateDownloadHttpClient();

        log("\n>>> ÜRÜN GÖRSELLERİ İNDİRİLİYOR (ham, işlenmemiş)...");
        await UrunGorselleriniIndirAsync(excelPath, search, downloader, outputDir, log);

        sw.Stop();
        log($"\nİŞLEM TAMAMLANDI | Süre: {sw.ElapsedMilliseconds / 1000} sn");
        return 0;
    }

    private static async Task UrunGorselleriniIndirAsync(
        string excelPath,
        SerpApiImageSearchService search,
        HttpClient downloader,
        string outputDir,
        Action<string> log)
    {
        if (!File.Exists(excelPath))
        {
            log($"\n[UYARI] Excel bulunamadı: {excelPath}");
            log("Proje kökünde Sample_OEM_List.xlsx oluşturuldu; OEM kodlarını ekleyip tekrar çalıştırın.");
            return;
        }

        var rows = await new ExcelService().GetBrandOemRowsAsync(excelPath);

        foreach (var row in rows)
        {
            var oem = (row.OemCode ?? "").Trim();
            if (string.IsNullOrEmpty(oem)) continue;

            var baseName = SanitizeFileName(oem);
            if (OemDosyasiVarMi(outputDir, baseName)) continue;

            log($"\n[ARANIYOR] OEM: {oem}");

            var productQuery = $"{oem} spare part";
            var results = await search.SearchImagesAsync(
                productQuery,
                maxResults: MaxAdayPerArama,
                minWidthPx: 300,
                minHeightPx: 300);

            if (results.Count > 0)
                await DownloadAndSaveRawAsync(downloader, results, outputDir, baseName, log);
            else
                log("    => Sonuç bulunamadı.");
        }
    }

    private static async Task<bool> DownloadAndSaveRawAsync(
        HttpClient http,
        IReadOnlyList<ImageResult> results,
        string outputDir,
        string baseFileName,
        Action<string> log)
    {
        foreach (var r in results)
        {
            try
            {
                var resp = await http.GetAsync(r.Url).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) continue;

                var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                if (bytes.Length == 0) continue;

                var ext = GuessExtension(r.Url, resp.Content.Headers.ContentType?.MediaType);
                var filePath = Path.Combine(outputDir, $"{baseFileName}.{ext}");
                await File.WriteAllBytesAsync(filePath, bytes).ConfigureAwait(false);

                log($"    => [KAYDEDİLDİ] {baseFileName}.{ext} ({bytes.Length:N0} byte, ham)");
                return true;
            }
            catch
            {
                continue;
            }
        }

        log("    => İndirilebilir görsel bulunamadı.");
        return false;
    }

    private static bool OemDosyasiVarMi(string outputDir, string baseFileName)
    {
        if (!Directory.Exists(outputDir)) return false;
        return Directory.EnumerateFiles(outputDir, $"{baseFileName}.*").Any();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var parts = value.Split(invalid, StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Join("_", parts).Trim();
        return string.IsNullOrEmpty(joined) ? "oem" : joined;
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
            // URL parse edilemezse content-type veya varsayılan kullanılır.
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

    private static HttpClient CreateDownloadHttpClient()
    {
        var h = new HttpClient();
        h.Timeout = TimeSpan.FromSeconds(IndirmeTimeoutSaniye);
        h.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return h;
    }
}
