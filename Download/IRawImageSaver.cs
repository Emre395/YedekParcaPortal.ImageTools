using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools.Download;

public interface IRawImageSaver
{
    Task<RawSaveAttemptResult> TrySaveFirstDownloadableAsync(
        IReadOnlyList<ImageResult> candidates,
        string outputDirectory,
        string baseFileName,
        CancellationToken ct = default);
}
