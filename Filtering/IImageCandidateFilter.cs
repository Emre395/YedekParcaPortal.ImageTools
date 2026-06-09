using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Filtering;

/// <summary>
/// Arama sonucu aday görselleri filtreler.
/// </summary>
public interface IImageCandidateFilter
{
    bool Accepts(ImageResult candidate);
}
