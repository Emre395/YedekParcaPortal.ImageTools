using YedekParcaPortal.ImageTools.Models;
using YedekParcaPortal.ImageTools.Search;

namespace YedekParcaPortal.ImageTools.Processing;

/// <summary>
/// Excel satırlarını sırayla işler; SerpApi kota hatasında güvenle durur.
/// </summary>
public sealed class OemBatchRunner
{
    private readonly IOemRowProcessor _processor;
    private readonly Action<string> _log;

    public OemBatchRunner(IOemRowProcessor processor, Action<string> log)
    {
        _processor = processor;
        _log = log;
    }

    public async Task<BatchRunSummary> RunAsync(
        IReadOnlyList<BrandOemRow> rows,
        CancellationToken ct = default)
    {
        var summary = new BatchRunSummary();
        string? lastAttemptedOem = null;

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var oem = (row.OemCode ?? "").Trim();
            if (string.IsNullOrEmpty(oem)) continue;

            lastAttemptedOem = oem;

            try
            {
                var result = await _processor.ProcessAsync(row, ct).ConfigureAwait(false);
                summary.Register(result);
            }
            catch (SerpApiQuotaException ex)
            {
                var oemLabel = ex.LastProcessedOem ?? lastAttemptedOem ?? oem;
                _log($"\n[STOP] SerpApi kotası tükendi. Excel'de [{oemLabel}] değerinde kalındı.");
                summary.QuotaExhausted = true;
                summary.LastOemOnQuotaStop = oemLabel;
                break;
            }
        }

        return summary;
    }
}

public sealed class BatchRunSummary
{
    public int Saved { get; private set; }
    public int Skipped { get; private set; }
    public int NotFound { get; private set; }
    public bool QuotaExhausted { get; set; }
    public string? LastOemOnQuotaStop { get; set; }

    public void Register(OemProcessResult result)
    {
        switch (result.Status)
        {
            case OemProcessStatus.Saved: Saved++; break;
            case OemProcessStatus.SkippedAlreadyExists: Skipped++; break;
            case OemProcessStatus.NotFound: NotFound++; break;
        }
    }
}
