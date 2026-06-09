using YedekParcaPortal.ImageTools.Configuration;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Filtering;

/// <summary>
/// Boyut bilgisi bilinen ve 600x600 altındaki görselleri eler.
/// Boyut bilinmiyorsa aday korunur (indirme aşamasında değerlendirilir).
/// </summary>
public sealed class MinimumDimensionFilter : IImageCandidateFilter
{
    private readonly int _minWidth;
    private readonly int _minHeight;

    public MinimumDimensionFilter(
        int minWidth = ImageToolsOptions.MinImageWidthPx,
        int minHeight = ImageToolsOptions.MinImageHeightPx)
    {
        _minWidth = minWidth;
        _minHeight = minHeight;
    }

    public bool Accepts(ImageResult candidate)
    {
        if (!candidate.Width.HasValue || !candidate.Height.HasValue)
            return true;

        return candidate.Width.Value >= _minWidth && candidate.Height.Value >= _minHeight;
    }
}
