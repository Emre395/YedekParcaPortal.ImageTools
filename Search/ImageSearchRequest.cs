namespace YedekParcaPortal.ImageTools.Search;

/// <summary>
/// SerpApi Google Images arama isteği.
/// </summary>
public sealed record ImageSearchRequest(
    string Query,
    bool UseLargeImageFilter,
    string? TierLabel = null);
