using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Processing;

/// <summary>
/// Tek bir Excel satırını kademeli arama + indirme ile işler.
/// </summary>
public interface IOemRowProcessor
{
    Task<OemProcessResult> ProcessAsync(BrandOemRow row, CancellationToken ct = default);
}
