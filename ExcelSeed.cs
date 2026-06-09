using ClosedXML.Excel;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// Örnek OEM listesi Excel dosyası yönetimi.
/// Dosya yoksa sadece başlıkları içeren temiz bir şablon oluşturur.
/// </summary>
public static class ExcelSeed
{
    public const string BrandHeader = "Brand";
    public const string OemHeader = "OEM_Code";

    public static void SeedSampleExcelIfMissing(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        // EĞER DOSYA VARSA HİÇBİR ŞEY YAPMA (Senin listeni korur)
        if (File.Exists(filePath)) return;

        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var book = new XLWorkbook();
            var sheet = book.Worksheets.Add("OEM List");

            // Sadece başlıkları oluşturuyoruz, kodları sen Excel'e yapıştıracaksın
            sheet.Cell(1, 1).Value = BrandHeader;
            sheet.Cell(1, 2).Value = OemHeader;

            // Hücre formatlarını biraz güzelleştirelim (isteğe bağlı)
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();

            book.SaveAs(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel şablonu oluşturulamadı: {ex.Message}", ex);
        }
    }
}