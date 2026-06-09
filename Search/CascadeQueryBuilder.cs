using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// Kademe 1: Marka + OEM + büyük görsel + watermark/stok dışlama.
/// Kademe 2: OEM + stok sitesi dışlama + büyük görsel.
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
        terms.Add(ImageToolsOptions.StockSiteExclusionsTier1);

        return new ImageSearchRequest(
            Query: string.Join(" ", terms),
            UseLargeImageFilter: true,
            TierLabel: "Kademe 1 (Marka+OEM)");
    }

    public ImageSearchRequest BuildTier2(BrandOemRow row)
    {
        var oem = (row.OemCode ?? "").Trim();
        var query = $"{oem} {ImageToolsOptions.StockSiteExclusionsTier2}";

        return new ImageSearchRequest(
            Query: query.Trim(),
            UseLargeImageFilter: true,
            TierLabel: "Kademe 2 (Yalnızca OEM)");
    }
}
