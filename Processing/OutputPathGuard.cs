namespace YedekParcaPortal.ImageTools.Processing;

public sealed class OutputPathGuard : IOutputPathGuard
{
    public bool AlreadyExists(string outputDirectory, string baseFileName)
    {
        if (!Directory.Exists(outputDirectory)) return false;
        return Directory.EnumerateFiles(outputDirectory, $"{baseFileName}.*").Any();
    }

    public string SanitizeFileName(string oemCode)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var parts = (oemCode ?? "").Split(invalid, StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Join("_", parts).Trim();
        return string.IsNullOrEmpty(joined) ? "oem" : joined;
    }
}
