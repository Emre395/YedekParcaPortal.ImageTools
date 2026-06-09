using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// Görsel arama servisi arayüzü (ör. SerpApi Google Images).
/// Original görsel URL'leri ve varsa boyut bilgisi döner; 400x400 altı istenirse atlanır.
/// </summary>
public interface IImageSearchService
{
    /// <summary>
    /// Sorguya göre görsel araması yapar.
    /// </summary>
    /// <param name="query">Arama metni</param>
    /// <param name="maxResults">Maksimum sonuç sayısı</param>
    /// <param name="minWidthPx">Minimum genişlik (altı atlanır)</param>
    /// <param name="minHeightPx">Minimum yükseklik (altı atlanır)</param>
    /// <param name="ct">İptal tokenı</param>
    /// <returns>Original görsel URL'leri ve boyut bilgisi (boyut yoksa indirme aşamasında kontrol edilir)</returns>
    Task<IReadOnlyList<ImageResult>> SearchImagesAsync(
        string query,
        int maxResults = 10,
        int minWidthPx = 400,
        int minHeightPx = 400,
        CancellationToken ct = default);
}
