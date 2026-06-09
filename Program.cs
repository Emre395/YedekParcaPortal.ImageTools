using System.Diagnostics;
using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Download;
using YedekParcaPortal.ImageTools.Filtering;
using YedekParcaPortal.ImageTools.Processing;
using YedekParcaPortal.ImageTools.Search;

namespace YedekParcaPortal.ImageTools;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var sw = Stopwatch.StartNew();
        var log = new Action<string>(Console.WriteLine);

        Console.WriteLine("════════════════════════════════════════════════════════════");
        log("  ImageTools – Kademeli (Cascade) Görsel Arama Motoru");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        var excelPath = PathConfig.GetSampleExcelPath();
        ExcelSeed.SeedSampleExcelIfMissing(excelPath);

        var outputDir = PathConfig.ResolveOutputPath();
        Directory.CreateDirectory(outputDir);

        log($"Çıktı klasörü: {outputDir}");
        log($"Excel dosyası: {excelPath}");
        log($"Minimum boyut: {ImageToolsOptions.MinImageWidthPx}x{ImageToolsOptions.MinImageHeightPx} px");
        log("Arama: SerpApi — Kademe 1 (Marka+OEM) → Kademe 2 (Yalnızca OEM)");

        if (!File.Exists(excelPath))
        {
            log("\n[UYARI] Excel bulunamadı. Sample_OEM_List.xlsx oluşturuldu; OEM kodlarını ekleyip tekrar çalıştırın.");
            return 1;
        }

        var rows = await new ExcelService().GetBrandOemRowsAsync(excelPath);
        var oemRows = rows.Where(r => !string.IsNullOrWhiteSpace(r.OemCode)).ToList();

        log($"\nExcel'den {oemRows.Count} OEM kodu okundu.");
        if (oemRows.Count == 0)
        {
            log("[UYARI] OEM_Code sütununda veri yok.");
            return 1;
        }

        using var searchService = new SerpApiGoogleImageSearchService();
        using var rawSaver = new RawImageSaver(new MinimumDimensionFilter());

        var cascadeSearch = new CascadeSearchExecutor(searchService, new CascadeQueryBuilder());
        var pathGuard = new OutputPathGuard();
        var rowProcessor = new OemRowProcessor(cascadeSearch, rawSaver, pathGuard, outputDir, log);
        var batchRunner = new OemBatchRunner(rowProcessor, log);

        log("\n>>> ÜRÜN GÖRSELLERİ İNDİRİLİYOR (ham, işlenmemiş)...");
        var summary = await batchRunner.RunAsync(oemRows);

        sw.Stop();
        log($"\n── Özet ──");
        log($"Kaydedilen : {summary.Saved}");
        log($"Atlanan    : {summary.Skipped} (zaten mevcut)");
        log($"Bulunamayan: {summary.NotFound}");
        if (summary.QuotaExhausted)
            log($"Kota durdu: [{summary.LastOemOnQuotaStop}]");
        log($"Süre       : {sw.ElapsedMilliseconds / 1000} sn");

        return summary.QuotaExhausted ? 2 : 0;
    }
}
