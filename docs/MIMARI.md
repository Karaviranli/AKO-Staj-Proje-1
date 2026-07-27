# Sistem Mimarisi

## 1. Katmanlar

```
┌───────────────────────────────────────────────────────────┐
│  SUNUM KATMANI — Next.js 16 (localhost:3000)              │
│  Sayfalar · Kural sihirbazı · Veri tabloları              │
│  proxy.ts (route protection) · Route Handler'lar          │
└───────────────────────────────┬───────────────────────────┘
                                │  HTTP + HttpOnly çerez
                                │  (token tarayıcı JS'ine hiç inmez)
┌───────────────────────────────▼───────────────────────────┐
│  UYGULAMA KATMANI — .NET 9 Web API (localhost:5080)       │
│                                                            │
│  DataFlow.API        Controller · JWT · Swagger · Hata mw. │
│  DataFlow.Business   Parser fabrikası · Kural motoru ·     │
│                      Kalite analizörü · Servisler          │
│  DataFlow.DataAccess EF Core · Entity · Migration          │
└───────────────────────────────┬───────────────────────────┘
                                │  EF Core
┌───────────────────────────────▼───────────────────────────┐
│  VERİ KATMANI — SQLite (dataflow.db)                      │
│  Users · UploadedFiles · ProcessedDatasets ·              │
│  RulePresets · AuditLogs                                  │
└───────────────────────────────────────────────────────────┘
```

Katmanlar arası bağımlılık tek yönlüdür: `API → Business → DataAccess`.
DataAccess, Business'ı tanımaz; Business, API'yi tanımaz. Bu sayede iş mantığı
web çatısından bağımsız olarak test edilebilir (Separation of Concerns).

---

## 2. Veri akışı — dosya yüklemeden sonuca

```
Kullanıcı dosyayı sürükler
        │
        ▼
Next.js  POST /api/backend/data/upload      (vekil, token'ı ekler)
        │
        ▼
.NET     DataController.Upload
        │  uzantı ve boyut doğrulaması
        ▼
        FileParserFactory.GetParser(".csv")   ◄── Factory Pattern
        │
        ▼
        CsvFileParser / ExcelFileParser / JsonFileParser
        │  → DatasetModel { Columns, Rows }
        ▼
        DataQualityAnalyzer.Analyze()
        │  → eksik oranı, tip tutarsızlığı, tekrar eden satır, kalite skoru
        ▼
        SQLite'a JSON olarak kaydet (UploadedFile)
        │
        ▼
Arayüz   Kalite raporu + ham veri önizlemesi
```

```
Kullanıcı kural zincirini kurar ve "Çalıştır" der
        │
        ▼
.NET     RuleEngine.Execute(dataset, rules)     ◄── Pipeline Pattern
        │
        │   for each rule (order'a göre SIRAYLA):
        │       ConditionEvaluator.Matches(row, condition)   → eşleşen satırlar
        │       ActionExecutor.ApplyToRow(row, action)       → hücreyi değiştir
        │       RuleExecutionLogDto üret                     → adım raporu
        │
        │   Not: her kural bir öncekinin ÇIKTISI üzerinde çalışır.
        ▼
        ProcessedDataset olarak kaydet (kurallar + log + temiz veri birlikte)
        │
        ▼
Arayüz   Yürütme raporu + temizlenmiş tablo + CSV indirme
```

---

## 3. Kullanılan tasarım kalıpları

| Kalıp | Nerede | Neden |
| --- | --- | --- |
| **Factory** | `FileParserFactory` | Yeni format (XML, Parquet) eklerken mevcut kod değişmez; yalnızca yeni bir `IFileParser` yazılır. |
| **Pipeline / Chain** | `RuleEngine` | Sıralı ve birbirine bağımlı kuralların doğal karşılığı. |
| **Strategy** | `ActionExecutor` | Her aksiyon tipi ayrı bir davranış; motor hangi aksiyonun ne yaptığını bilmek zorunda değil. |
| **Repository benzeri** | `DbContext` + servisler | Veri erişimi iş mantığından ayrı. |
| **Dependency Injection** | `ServiceRegistration` | Test edilebilirlik; somut sınıflar yerine arayüzler enjekte edilir. |
| **Envelope (ApiResponse)** | Tüm uçlar | Frontend tek noktada hata yönetir. |

---

## 4. Şemasız (schema-less) veri saklama kararı

**Problem.** Kullanıcının yükleyeceği dosyaların kolonları önceden bilinemez.
Her farklı dosya için yeni tablo oluşturmak mümkün değildir.

**Çözüm.** Satırlar `List<Dictionary<string, object?>>` olarak modellenir ve
veritabanına JSON metin olarak yazılır. Kolon adları ayrı bir JSON dizisinde
tutulur.

**Ödünler.**
- ✅ Herhangi bir kolon yapısı desteklenir, migration gerekmez.
- ✅ Ham veri ve işlenmiş veri birlikte saklanabilir (izlenebilirlik).
- ⚠️ SQL ile kolon bazlı sorgu yapılamaz — bu proje için gerekmiyor, tüm
  filtreleme kural motorunda bellek içinde yapılıyor.
- ⚠️ Çok büyük veri setlerinde bellek tüketimi artar. Sınır: 25 MB / dosya.

---

## 5. Güvenlik zinciri

1. Şifreler **BCrypt** ile hash'lenir; düz metin hiçbir yerde tutulmaz.
2. Var olmayan kullanıcıda da BCrypt maliyeti ödenir — yanıt süresinden
   kullanıcının varlığı anlaşılamaz (timing attack önlemi).
3. .NET, imzalı **JWT** üretir (`HmacSha256`, `ClockSkew = 0`).
4. Next.js bu token'ı **HttpOnly + SameSite=Lax** çereze yazar. Tarayıcıdaki
   hiçbir JavaScript token'a erişemez → XSS ile token çalınamaz.
5. İstemci `/api/backend/*` vekiline istek atar; `Authorization` başlığını
   **sunucu** ekler. Backend adresi tarayıcıya sızmaz.
6. `proxy.ts` korumalı sayfaları oturum yoksa `/giris`'e yönlendirir.
7. .NET tarafında `[Authorize]` ile Controller'lar korunur; geçersiz token
   Controller'a ulaşmadan **401** ile geri çevrilir.
8. **Yetki her zaman token'dan okunur** (`BaseApiController.CurrentUserId`).
   İstemciden gelen bir `userId` değerine asla güvenilmez — böylece bir kullanıcı
   başkasının veri setini göremez.
9. Tüm kritik işlemler `AuditLogs` tablosuna yazılır.
