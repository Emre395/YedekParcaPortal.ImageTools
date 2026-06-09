using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Kademe 1: Marka + OEM + büyük görsel filtresi + stok sitesi dışlama.
/// Kademe 2: Yalnızca OEM (temiz sorgu) + büyük görsel filtresi.
/// </summary>
public sealed class CascadeQueryBuilder : ICascadeQueryBuilder
{
    public ImageSearchRequest BuildTier1(BrandOemRow row)
    {
        var brand = (row.Brand ?? "").Trim();
        var oem = (row.OemCode ?? "").Trim();

        var terms = new List<string>();
        if (!string.IsNullOrEmpty(brand))
            terms.Add(brand);
        terms.Add(oem);
        terms.Add(ImageToolsOptions.StockSiteExclusions);

        return new ImageSearchRequest(
            Query: string.Join(" ", terms),
            UseLargeImageFilter: true,
            TierLabel: "Kademe 1 (Marka+OEM)");
    }

    public ImageSearchRequest BuildTier2(BrandOemRow row)
    {
        var oem = (row.OemCode ?? "").Trim();
        return new ImageSearchRequest(
            Query: oem,
            UseLargeImageFilter: true,
            TierLabel: "Kademe 2 (Yalnızca OEM)");
    }
}
