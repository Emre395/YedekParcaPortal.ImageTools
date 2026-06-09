namespace YedekParcaPortal.ImageTools.Processing;

/// <summary>
/// Disk üzerinde OEM dosyası varlığını kontrol eder.
/// </summary>
public interface IOutputPathGuard
{
    bool AlreadyExists(string outputDirectory, string baseFileName);
    string SanitizeFileName(string oemCode);
}
