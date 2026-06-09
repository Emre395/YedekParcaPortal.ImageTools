using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Download;

/// <summary>
/// Ham görsel baytlarını diske kaydeder.
/// </summary>
public interface IRawImageSaver
{
    /// <returns>Kaydedilen dosyanın tam yolu; başarısızsa null.</returns>
    Task<string?> TrySaveFirstDownloadableAsync(
        IReadOnlyList<ImageResult> candidates,
        string outputDirectory,
        string baseFileName,
        CancellationToken ct = default);
}
