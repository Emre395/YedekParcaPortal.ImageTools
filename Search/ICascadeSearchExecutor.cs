using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Kademe 1 ve Kademe 2 aramalarını ayrı ayrı yürütür.
/// </summary>
public interface ICascadeSearchExecutor
{
    Task<IReadOnlyList<ImageResult>> SearchTier1Async(BrandOemRow row, CancellationToken ct = default);
    Task<IReadOnlyList<ImageResult>> SearchTier2Async(BrandOemRow row, CancellationToken ct = default);
}
