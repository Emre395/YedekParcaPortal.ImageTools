using ClosedXML.Excel;
using YedekParcaPortal.ImageTools.Models;

namespace YedekParcaPortal.ImageTools;

/// <summary>
/// Excel dosyasından Brand ve OEM_Code sütunlarını okur (ClosedXML).
/// </summary>
public sealed class ExcelService : IExcelService
{
    private const string BrandHeader = "Brand";
    private const string OemHeader = "OEM_Code";

    /// <inheritdoc />
    public Task<IReadOnlyList<BrandOemRow>> GetBrandOemRowsAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult<IReadOnlyList<BrandOemRow>>(Array.Empty<BrandOemRow>());

        var list = new List<BrandOemRow>();
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var book = new XLWorkbook(stream);
            var sheet = book.Worksheets.Worksheet(1);
            var used = sheet.RangeUsed();
            if (used == null) return Task.FromResult<IReadOnlyList<BrandOemRow>>(list);

            var firstRow = used.FirstRowUsed();
            if (firstRow == null) return Task.FromResult<IReadOnlyList<BrandOemRow>>(list);

            int brandCol = -1, oemCol = -1;
            foreach (var cell in firstRow.CellsUsed())
            {
                var v = (cell.GetString() ?? "").Trim();
                if (string.IsNullOrEmpty(v)) continue;
                if (string.Equals(v, BrandHeader, StringComparison.OrdinalIgnoreCase))
                    brandCol = cell.Address.ColumnNumber;
                else if (string.Equals(v, OemHeader, StringComparison.OrdinalIgnoreCase))
                    oemCol = cell.Address.ColumnNumber;
            }

            if (brandCol < 0 || oemCol < 0)
                throw new InvalidOperationException($"Excel dosyasında '{BrandHeader}' veya '{OemHeader}' sütunları bulunamadı. Dosya: {filePath}");

            var lastRow = used.LastRowUsed();
            if (lastRow == null) return Task.FromResult<IReadOnlyList<BrandOemRow>>(list);

            for (int r = firstRow.RowNumber() + 1; r <= lastRow.RowNumber(); r++)
            {
                ct.ThrowIfCancellationRequested();
                var brand = (sheet.Cell(r, brandCol).GetString() ?? "").Trim();
                var oem = (sheet.Cell(r, oemCol).GetString() ?? "").Trim();
                if (string.IsNullOrEmpty(brand) && string.IsNullOrEmpty(oem)) continue;
                list.Add(new BrandOemRow(brand, oem));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel okuma hatası. Dosya: {filePath}. Hata: {ex.Message}", ex);
        }

        return Task.FromResult<IReadOnlyList<BrandOemRow>>(list);
    }
}
