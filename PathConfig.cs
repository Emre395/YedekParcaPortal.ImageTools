namespace YedekParcaPortal.ImageTools;

/// <summary>
/// ImageTools çıktı ve Excel yolları.
/// Varsayılan: proje kökündeki <c>output</c> klasörü.
/// Ortam değişkeni: IMAGE_TOOLS_OUTPUT
/// </summary>
public static class PathConfig
{
    private const string ProjectFileName = "YedekParcaPortal.ImageTools.csproj";

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
    /// .csproj dosyasının bulunduğu klasör (dotnet run ile bin/ altına düşmez).
    /// </summary>
    public static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ProjectFileName)))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    }

    public const string SampleExcelFileName = "Sample_OEM_List.xlsx";

    public static string GetSampleExcelPath() => Path.Combine(GetProjectRoot(), SampleExcelFileName);
}
