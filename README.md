# YedekParcaPortal.ImageTools

Kademeli (Cascade) SerpApi görsel arama motoru: Excel'deki OEM kodları için otomatik arama, 600×600+ filtre ve ham kayıt.

## Mimari

```
Program
  └── OemBatchRunner          (Excel döngüsü, kota durdurma)
        └── OemRowProcessor   (Kademe 1 → indir → Kademe 2 → indir)
              ├── CascadeSearchExecutor
              ├── RawImageSaver
              └── OutputPathGuard
```

## Kademeli arama

| Kademe | Sorgu | Filtreler |
|--------|-------|-----------|
| 1 | `{Brand} {OEM} -watermark -site:shutterstock.com ...` | `tbs=isz:l` (büyük görsel) |
| 2 | `{OEM}` (yalnızca) | `tbs=isz:l` |

Kademe 2 yalnızca Kademe 1 **0 sonuç** döndürürse veya tüm URL'ler **404/bağlantı hatası** verirse çalışır (boyut yetersizliğinde kota harcanmaz).

## Kurallar

- Minimum boyut: **600×600 px** (SerpApi metadata + indirme sonrası Image.Identify)
- Paralellik: **3 satır** eşzamanlı (`SemaphoreSlim`)
- Kayıt: `output/{OEM}.{uzantı}` ham byte
- Mevcut dosya varsa satır atlanır
- SerpApi kota (HTTP 402 / json error): program durur, son OEM konsola yazılır

## Çalıştırma

```powershell
dotnet run
```

```powershell
$env:IMAGE_TOOLS_OUTPUT = "D:\gorseller"
$env:SERPAPI_KEY = "anahtariniz"   # isteğe bağlı; yoksa kod içi varsayılan
dotnet run
```

## Girdi

`Sample_OEM_List.xlsx` — `Brand` ve `OEM_Code` sütunları.
