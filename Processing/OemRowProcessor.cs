using YedekParcaPortal.ImageTools.Download;
using YedekParcaPortal.ImageTools.Models;
using YedekParcaPortal.ImageTools.Search;

namespace YedekParcaPortal.ImageTools.Processing;

/// <summary>
/// Kademe 1 arama → indirme → başarısızsa Kademe 2 arama → indirme.
/// </summary>
public sealed class OemRowProcessor : IOemRowProcessor
{
    private readonly ICascadeSearchExecutor _cascadeSearch;
    private readonly IRawImageSaver _saver;
    private readonly IOutputPathGuard _pathGuard;
    private readonly string _outputDirectory;
    private readonly Action<string> _log;

    public OemRowProcessor(
        ICascadeSearchExecutor cascadeSearch,
        IRawImageSaver saver,
        IOutputPathGuard pathGuard,
        string outputDirectory,
        Action<string> log)
    {
        _cascadeSearch = cascadeSearch;
        _saver = saver;
        _pathGuard = pathGuard;
        _outputDirectory = outputDirectory;
        _log = log;
    }

    public async Task<OemProcessResult> ProcessAsync(BrandOemRow row, CancellationToken ct = default)
    {
        var oem = (row.OemCode ?? "").Trim();
        if (string.IsNullOrEmpty(oem))
            return new OemProcessResult(oem, OemProcessStatus.Failed, Message: "OEM kodu boş.");

        var baseName = _pathGuard.SanitizeFileName(oem);
        if (_pathGuard.AlreadyExists(_outputDirectory, baseName))
            return new OemProcessResult(oem, OemProcessStatus.SkippedAlreadyExists);

        _log($"\n[ARANIYOR] OEM: {oem} | Marka: {row.Brand}");

        var tier1Results = await _cascadeSearch.SearchTier1Async(row, ct).ConfigureAwait(false);
        if (tier1Results.Count > 0)
        {
            _log("    => Kademe 1 (Marka+OEM) — aday sayısı: " + tier1Results.Count);
            var saved = await _saver.TrySaveFirstDownloadableAsync(
                tier1Results, _outputDirectory, baseName, ct).ConfigureAwait(false);
            if (saved != null)
            {
                _log($"    => [KAYDEDİLDİ] {Path.GetFileName(saved)} (ham)");
                return new OemProcessResult(oem, OemProcessStatus.Saved, saved);
            }
            _log("    => Kademe 1 adayları indirilemedi.");
        }
        else
        {
            _log("    => Kademe 1 sonuç döndürmedi.");
        }

        _log("    => Kademe 2 (Yalnızca OEM) deneniyor...");
        var tier2Results = await _cascadeSearch.SearchTier2Async(row, ct).ConfigureAwait(false);
        if (tier2Results.Count == 0)
        {
            _log("    => Sonuç bulunamadı.");
            return new OemProcessResult(oem, OemProcessStatus.NotFound);
        }

        _log("    => Kademe 2 — aday sayısı: " + tier2Results.Count);
        var tier2Saved = await _saver.TrySaveFirstDownloadableAsync(
            tier2Results, _outputDirectory, baseName, ct).ConfigureAwait(false);

        if (tier2Saved != null)
        {
            _log($"    => [KAYDEDİLDİ] {Path.GetFileName(tier2Saved)} (ham, Kademe 2)");
            return new OemProcessResult(oem, OemProcessStatus.Saved, tier2Saved);
        }

        _log("    => İndirilebilir görsel bulunamadı.");
        return new OemProcessResult(oem, OemProcessStatus.NotFound);
    }
}
