# YedekParcaPortal.ImageTools

Bağımsız konsol aracı: Excel'deki OEM kodları için Google Görseller'de arama yapar, bulunan görselleri **ham (işlenmemiş)** olarak diske kaydeder.

YedekParcaPortal web projesi veya ImageCore ile bağlantısı yoktur. Boyutlandırma ve filigran web sitesinde (`ImageCore`) uygulanır.

## Gereksinimler

- .NET 8
- SerpApi API anahtarı (`SERPAPI_KEY` ortam değişkeni; tanımlı değilse kod içindeki varsayılan kullanılır)

## Çalıştırma

```powershell
cd C:\Users\Administrator\source\repos\YedekParcaPortal.ImageTools
dotnet run
```

İsteğe bağlı çıktı klasörü:

```powershell
$env:IMAGE_TOOLS_OUTPUT = "D:\indirilen_gorseller"
dotnet run
```

## Girdi

Proje kökünde `Sample_OEM_List.xlsx` — `Brand` ve `OEM_Code` sütunları. Dosya yoksa ilk çalıştırmada boş şablon oluşturulur.

## Çıktı

- Klasör: `output/` (veya `IMAGE_TOOLS_OUTPUT`)
- Dosya adı: `{OEM}.{jpg|png|webp|...}` — kaynak formatı korunur
- Aynı OEM için dosya zaten varsa atlanır

## Akış

1. Excel'den OEM kodları okunur
2. SerpApi ile `"{OEM} spare part"` aranır
3. İlk indirilebilir görsel ham byte olarak kaydedilir
