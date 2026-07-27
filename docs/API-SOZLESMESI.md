# API Sözleşmesi

Backend ve frontend ekiplerinin **birbirini beklemeden** paralel çalışabilmesi
için sabitlenen sözleşme. Canlı dokümantasyon: <http://localhost:5080/swagger>

## Ortak zarf (envelope)

Tüm uçlar aynı biçimde yanıt döner:

```jsonc
{
  "success": true,
  "message": "İşlem açıklaması",
  "data": { /* uca özel içerik */ },
  "errors": []
}
```

Hata durumunda `success: false`, `data: null` ve HTTP durum kodu uygun şekilde
ayarlanır (400 / 401 / 404 / 409 / 415 / 500).

---

## Kimlik doğrulama

| Metot | Uç | Yetki | Gövde | Yanıt (`data`) |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/login` | — | `{ username, password }` | `{ token, expiresIn, expiresAt, user }` |
| POST | `/api/auth/register` | — | `{ username, email, password, fullName? }` | aynı |
| GET | `/api/auth/me` | JWT | — | `{ id, username, email, role, fullName }` |

Korumalı tüm isteklerde başlık: `Authorization: Bearer <token>`

---

## Veri yükleme

| Metot | Uç | Gövde | Yanıt (`data`) |
| --- | --- | --- | --- |
| POST | `/api/data/upload` | `multipart/form-data`, alan adı `file` | `UploadResult` |
| POST | `/api/data/push` | `{ datasetName, category, rows: [ {...} ] }` | `UploadResult` |

**UploadResult**

```jsonc
{
  "fileId": 12,
  "fileName": "satis.csv",
  "sourceType": "csv",
  "sizeInBytes": 914,
  "rowCount": 15,
  "columnCount": 8,
  "uploadedAt": "2026-07-27T09:32:10Z",
  "columns": ["MusteriNo", "Ad", "Yas", "..."],
  "preview": [ { "MusteriNo": 1001, "Ad": "ahmet yılmaz" } ],
  "quality": {
    "qualityScore": 93,
    "rowCount": 15,
    "duplicateRowCount": 0,
    "totalNullCells": 6,
    "totalTypeMismatches": 1,
    "columns": [
      {
        "name": "Yas",
        "inferredType": "number",   // number | date | boolean | text | empty
        "nullCount": 1,
        "nullRatio": 0.0667,
        "distinctCount": 14,
        "typeMismatchCount": 0,
        "sampleValues": ["34", "67", "52"]
      }
    ],
    "warnings": ["'ToplamHarcama' kolonunda 1 hücre beklenen 'number' tipine uymuyor."]
  }
}
```

Kabul edilen uzantılar: `.csv`, `.xlsx`, `.xls`, `.json` — en fazla **25 MB**.

---

## Veri okuma

| Metot | Uç | Açıklama |
| --- | --- | --- |
| GET | `/api/data/files` | Kullanıcının tüm veri setlerinin özeti (satır verisi taşımaz) |
| GET | `/api/data/files/{id}?page=1&pageSize=50` | Sayfalı ham satırlar |
| DELETE | `/api/data/files/{id}` | Veri setini ve bağlı işlemleri siler |

---

## Kural motoru

| Metot | Uç | Açıklama |
| --- | --- | --- |
| GET | `/api/rules/catalog` | Desteklenen operatör ve aksiyonların listesi (arayüz sihirbazı bunu kullanır) |
| GET | `/api/rules/presets` | Hazır ve kullanıcıya ait kural şablonları |
| POST | `/api/rules/presets` | Yeni şablon kaydeder |
| DELETE | `/api/rules/presets/{id}` | Kullanıcının kendi şablonunu siler |
| POST | `/api/data/process` | Kural zincirini çalıştırır |

**POST /api/data/process — istek**

```jsonc
{
  "fileId": 12,
  "name": "Satış segmentasyonu v2",
  "dryRun": true,                  // true → sonuç kaydedilmez, sadece önizleme
  "rules": [
    {
      "order": 1,
      "name": "Doğum yeri boşsa Belirsiz",
      "enabled": true,
      "condition": {
        "logic": "AND",            // AND | OR
        "conditions": [
          { "column": "DogumYeri", "operator": "isNull" }
        ],
        "groups": []               // iç içe gruplar
      },
      "action": {
        "type": "setValue",
        "targetColumn": "DogumYeri",
        "value": "Belirsiz"
      }
    },
    {
      "order": 2,
      "name": "Yaş > 50 ise X segmenti",
      "enabled": true,
      "condition": {
        "logic": "AND",
        "conditions": [{ "column": "Yas", "operator": "gt", "value": "50" }],
        "groups": []
      },
      "action": { "type": "setValue", "targetColumn": "Segment", "value": "X" }
    }
  ]
}
```

**Yanıt**

```jsonc
{
  "processedDatasetId": 7,        // dryRun ise null
  "fileId": 12,
  "dryRun": false,
  "rowsBefore": 15,
  "rowsAfter": 14,
  "cellsModified": 36,
  "durationMs": 7,
  "columns": ["MusteriNo", "Ad", "...", "Segment"],
  "rows": [ /* temizlenmiş veri, ilk 100 satır */ ],
  "executionLog": [
    {
      "order": 1,
      "ruleName": "Doğum yeri boşsa Belirsiz",
      "summary": "4 satır koşula uydu, 4 hücrede 'Değer ata' uygulandı.",
      "rowsMatched": 4,
      "rowsBefore": 15,
      "rowsAfter": 15,
      "cellsModified": 4,
      "durationMs": 1,
      "skipped": false,
      "warning": null
    }
  ],
  "qualityAfter": { /* QualityReport */ }
}
```

---

## İşlem geçmişi

| Metot | Uç | Açıklama |
| --- | --- | --- |
| GET | `/api/data/processed` | Çalıştırılmış tüm kural setlerinin özeti |
| GET | `/api/data/processed/{id}` | Detay: uygulanan kurallar + log + temiz veri |
| GET | `/api/data/processed/{id}/export` | CSV indirir (UTF-8 BOM, Excel uyumlu) |

---

## Koşul operatörleri (20)

| Kod | Anlamı | Değer ister |
| --- | --- | --- |
| `eq` / `neq` | eşittir / eşit değildir | ✔ |
| `gt` / `gte` / `lt` / `lte` | büyüktür / ≥ / küçüktür / ≤ | ✔ |
| `between` | arasındadır | ✔ (iki değer) |
| `contains` / `notContains` | içerir / içermez | ✔ |
| `startsWith` / `endsWith` | ile başlar / biter | ✔ |
| `in` / `notIn` | listede var / yok (virgülle ayrık) | ✔ |
| `regex` | düzenli ifadeye uyar | ✔ |
| `isNull` / `isNotNull` | boş / dolu | ✘ |
| `isEmpty` / `isNotEmpty` | boş metin / değil | ✘ |
| `isNumeric` / `isNotNumeric` | sayısal / değil | ✘ |

> `isNull`, boş metnin yanı sıra `n/a`, `-`, `null`, `yok`, `bilinmiyor` gibi
> yaygın "eksik veri" ifadelerini de eksik sayar.

## Aksiyonlar (25)

**Satır seviyesi:** `deleteRow`, `keepRow`, `flagRow`

**Hücre seviyesi:** `setValue`, `fillNull`, `trim`, `toUpper`, `toLower`,
`toTitleCase`, `replace`, `removeSpaces`, `onlyDigits`

**Sayısal:** `multiply`, `divide`, `add`, `subtract`, `round`, `abs`

**Tip dönüşümü:** `castNumber`, `castDate`, `castText`

**Kolon seviyesi:** `renameColumn`, `dropColumn`, `copyColumn`, `deduplicate`
