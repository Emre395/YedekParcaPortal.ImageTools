using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Önce Kademe 1 arar; sonuç yoksa Kademe 2'ye geçer.
/// İndirme başarısızlığı çağıran tarafında ikinci kademeyi tetikler.
/// </summary>
public sealed class CascadeSearchExecutor : ICascadeSearchExecutor
{
    private readonly IGoogleImageSearchService _search;
    private readonly ICascadeQueryBuilder _queryBuilder;
    private readonly int _maxCandidates;

    public CascadeSearchExecutor(
        IGoogleImageSearchService search,
        ICascadeQueryBuilder queryBuilder,
        int maxCandidates = ImageToolsOptions.MaxCandidatesPerSearch)
    {
        _search = search;
        _queryBuilder = queryBuilder;
        _maxCandidates = maxCandidates;
    }

    public Task<IReadOnlyList<ImageResult>> SearchTier1Async(BrandOemRow row, CancellationToken ct = default)
        => _search.SearchAsync(_queryBuilder.BuildTier1(row), _maxCandidates, ct);

    public Task<IReadOnlyList<ImageResult>> SearchTier2Async(BrandOemRow row, CancellationToken ct = default)
        => _search.SearchAsync(_queryBuilder.BuildTier2(row), _maxCandidates, ct);

}
