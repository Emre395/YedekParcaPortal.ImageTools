namespace YedekParcaPortal.ImageTools.Models;

/// <summary>
/// Görsel arama sonucu: orijinal URL ve isteğe bağlı boyut bilgisi (SerpApi'den gelebilir).
/// </summary>
public sealed record ImageResult(
    string Url,
    int? Width = null,
    int? Height = null
);
