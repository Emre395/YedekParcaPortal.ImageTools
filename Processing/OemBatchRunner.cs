using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;
using YedekParcaPortal.ImageTools.Search;

namespace YedekParcaPortal.ImageTools.Processing;

/// <summary>
/// Excel satırlarını kontrollü paralellik ile işler; SerpApi kota hatasında güvenle durur.
/// </summary>
public sealed class OemBatchRunner : IDisposable
{
    private readonly IOemRowProcessor _processor;
    private readonly Action<string> _log;
    private readonly object _logLock = new();
    private readonly SemaphoreSlim _parallelLimit;

    public OemBatchRunner(
        IOemRowProcessor processor,
        Action<string> log,
        int maxParallel = ImageToolsOptions.MaxParallelRows)
    {
        _processor = processor;
        _log = log;
        _parallelLimit = new SemaphoreSlim(maxParallel, maxParallel);
    }

    public async Task<BatchRunSummary> RunAsync(
        IReadOnlyList<BrandOemRow> rows,
        CancellationToken ct = default)
    {
        var summary = new BatchRunSummary();
        using var stopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var workCt = stopCts.Token;

        var validRows = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.OemCode))
            .ToList();

        var tasks = validRows.Select(row => ProcessRowAsync(row, summary, stopCts, workCt));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        return summary;
    }

    public void Dispose() => _parallelLimit.Dispose();

    private async Task ProcessRowAsync(
        BrandOemRow row,
        BatchRunSummary summary,
        CancellationTokenSource stopCts,
        CancellationToken ct)
    {
        await _parallelLimit.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (summary.QuotaExhausted)
                return;

            var oem = row.OemCode.Trim();
            summary.SetLastAttemptedOem(oem);

            try
            {
                var result = await _processor.ProcessAsync(row, ct).ConfigureAwait(false);
                summary.Register(result);
            }
            catch (SerpApiQuotaException)
            {
                summary.MarkQuotaExhausted(oem);
                SafeLog($"\n[STOP] SerpApi kotası tükendi. Excel'de [{oem}] değerinde kalındı.");
                stopCts.Cancel();
            }
        }
        finally
        {
            _parallelLimit.Release();
        }
    }

    private void SafeLog(string message)
    {
        lock (_logLock)
            _log(message);
    }
}

public sealed class BatchRunSummary
{
    private int _saved;
    private int _skipped;
    private int _notFound;
    private string? _lastAttemptedOem;

    public bool QuotaExhausted { get; private set; }
    public string? LastOemOnQuotaStop { get; private set; }

    public int Saved => _saved;
    public int Skipped => _skipped;
    public int NotFound => _notFound;

    public void SetLastAttemptedOem(string oem) =>
        Interlocked.Exchange(ref _lastAttemptedOem, oem);

    public void MarkQuotaExhausted(string oem)
    {
        if (Interlocked.CompareExchange(ref _quotaFlag, 1, 0) != 0)
            return;
        QuotaExhausted = true;
        LastOemOnQuotaStop = oem;
    }

    private int _quotaFlag;

    public void Register(OemProcessResult result)
    {
        switch (result.Status)
        {
            case OemProcessStatus.Saved:
                Interlocked.Increment(ref _saved);
                break;
            case OemProcessStatus.SkippedAlreadyExists:
                Interlocked.Increment(ref _skipped);
                break;
            case OemProcessStatus.NotFound:
                Interlocked.Increment(ref _notFound);
                break;
        }
    }
}
