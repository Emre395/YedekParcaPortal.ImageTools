using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Google Görseller araması (SerpApi üzerinden).
/// </summary>
public interface IGoogleImageSearchService
{
    Task<IReadOnlyList<ImageResult>> SearchAsync(
        ImageSearchRequest request,
        int maxResults,
        CancellationToken ct = default);
}
