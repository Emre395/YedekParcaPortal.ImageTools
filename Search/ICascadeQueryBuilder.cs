using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Kademeli arama sorgularını üretir.
/// </summary>
public interface ICascadeQueryBuilder
{
    ImageSearchRequest BuildTier1(BrandOemRow row);
    ImageSearchRequest BuildTier2(BrandOemRow row);
}
