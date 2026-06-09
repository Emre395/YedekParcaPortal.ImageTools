namespace YedekParcaPortal.ImageTools;

/// <summary>
/// ImageTools çıktı ve Excel yolları.
/// Varsayılan: proje kökündeki <c>output</c> klasörü.
/// Ortam değişkeni: IMAGE_TOOLS_OUTPUT
/// </summary>
public static class PathConfig
{
    /// <summary>
    /// İndirilen ham görsellerin kaydedileceği klasör.
    /// </summary>
    public static string ResolveOutputPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("IMAGE_TOOLS_OUTPUT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return Path.GetFullPath(fromEnv);

        return Path.Combine(GetProjectRoot(), "output");
    }

    /// <summary>
    /// Proje kökü (ImageTools .csproj klasörü). bin/Debug/net8.0'dan iki üst dizin.
    /// </summary>
    public static string GetProjectRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", ".."));
    }

    public const string SampleExcelFileName = "Sample_OEM_List.xlsx";

    public static string GetSampleExcelPath() => Path.Combine(GetProjectRoot(), SampleExcelFileName);
}
