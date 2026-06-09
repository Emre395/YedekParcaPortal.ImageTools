namespace YedekParcaPortal.ImageTools.Configuration;

/// <summary>
/// ImageTools sabitleri ve yapılandırma değerleri.
/// </summary>
public sealed class ImageToolsOptions
{
    public const int MinImageWidthPx = 600;
    public const int MinImageHeightPx = 600;
    public const int MaxCandidatesPerSearch = 10;
    public const int DownloadTimeoutSeconds = 30;
    public const int MaxParallelRows = 3;
    public const string LargeImageFilter = "isz:l";

    public const string StockSiteExclusionsTier1 =
        "-watermark -site:shutterstock.com -site:alamy.com -site:istockphoto.com";

    public const string StockSiteExclusionsTier2 =
        "-site:shutterstock.com -site:alamy.com -site:istockphoto.com";
}
