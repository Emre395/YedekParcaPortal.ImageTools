using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// Excel dosyasından marka ve OEM listesi okuma arayüzü.
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Excel dosyasından Brand ve OEM_Code sütunlarını okur.
    /// İlk satır başlık kabul edilir; sütun adları büyük/küçük harf duyarsız aranır.
    /// </summary>
    /// <param name="filePath">.xlsx dosya yolu</param>
    /// <param name="ct">İptal tokenı</param>
    /// <returns>Marka ve OEM kodu listesi; hata durumunda boş liste veya exception.</returns>
    Task<IReadOnlyList<BrandOemRow>> GetBrandOemRowsAsync(string filePath, CancellationToken ct = default);
}
