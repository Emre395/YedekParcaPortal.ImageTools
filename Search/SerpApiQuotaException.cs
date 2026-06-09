namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// SerpApi kotası tükendiğinde fırlatılır; tüm işlem güvenle durdurulur.
/// </summary>
public sealed class SerpApiQuotaException : Exception
{
    public string? LastProcessedOem { get; }

    public SerpApiQuotaException(string message, string? lastProcessedOem = null, Exception? inner = null)
        : base(message, inner)
    {
        LastProcessedOem = lastProcessedOem;
    }
}
