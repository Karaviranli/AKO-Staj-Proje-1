# İş Dağılımı ve Zaman Planı

6 kişi, 3 ikili ekip. Her ekibin **kendi dosyaları** vardır — böylece aynı
dosyada çakışma (merge conflict) yaşanmaz.

---

## Ekipler ve sorumluluk alanları

### Ekip A — Arayüz (2 kişi)

**Dosya alanı:** `frontend/src/app/`, `frontend/src/components/`

| Kişi | Sorumluluk |
| --- | --- |
| A1 | Giriş/kayıt ekranları, panel düzeni, tasarım sistemi (`components/ui.tsx`), genel bakış |
| A2 | Kural stüdyosu (`RuleBuilder`), veri tabloları, yürütme raporu, işlem geçmişi |

**Çıktılar:** Figma ekran tasarımları · çalışan arayüz · responsive kontrol

---

### Ekip B — Çekirdek ve Kimlik Doğrulama (2 kişi)

**Dosya alanı:** `backend/src/DataFlow.API/`, `backend/src/DataFlow.DataAccess/`

| Kişi | Sorumluluk |
| --- | --- |
| B1 | Veritabanı tasarımı, EF Core entity'leri, migration'lar, seed verileri |
| B2 | JWT üretimi/doğrulaması, `AuthService`, Controller'lar, Swagger, hata middleware'i |

**Çıktılar:** Çalışan login/register · korumalı uçlar · Swagger dokümantasyonu

---

### Ekip C — Veri İşleme Motoru (2 kişi)

**Dosya alanı:** `backend/src/DataFlow.Business/`

| Kişi | Sorumluluk |
| --- | --- |
| C1 | Dosya okuyucular (CSV/XLSX/JSON), `FileParserFactory`, `ValueHelper` tip zorlama, kalite analizörü |
| C2 | `RuleEngine`, `ConditionEvaluator`, `ActionExecutor`, `DataService`, CSV dışa aktarım |

**Çıktılar:** Çalışan kural motoru · Postman test koleksiyonu · örnek bozuk veri setleri

---

## Ekipler arası bağımlılık nasıl kırıldı

Üç ekibin birbirini beklememesi için **önce sözleşme** yazıldı:

1. `docs/API-SOZLESMESI.md` — hangi uç, hangi gövde, hangi yanıt.
2. `frontend/src/lib/types.ts` — sözleşmenin TypeScript karşılığı.
3. `GET /api/rules/catalog` — arayüz, desteklenen operatör/aksiyon listesini
   koda gömmez, backend'den alır. Motor yeni bir aksiyon kazandığında arayüzde
   **hiçbir değişiklik gerekmez**.

Bu sayede Ekip A, backend hazır olmadan sahte veriyle (mock) çalışabildi.

---

## Zaman planı

| Hafta | Ekip A (Arayüz) | Ekip B (Çekirdek) | Ekip C (Motor) | Ortak çıktı |
| --- | --- | --- | --- | --- |
| **1** | Figma tasarımları, tasarım sistemi | Veritabanı şeması, entity'ler | Sözleşme tasarımı, örnek veri üretimi | **Sunum** + API sözleşmesi |
| **2** | Giriş/kayıt, panel düzeni | JWT, login/register, route protection | CSV + JSON okuyucular, `ValueHelper` | Uçtan uca giriş çalışıyor |
| **3** | Yükleme ekranı, kalite paneli | Upload uçları, dosya doğrulama | XLSX okuyucu, kalite analizörü | Dosya yükleme çalışıyor |
| **4** | Kural stüdyosu, veri tabloları | `process` ucu, kayıt/geçmiş | Kural motoru, yürütme raporu | **Ana özellik tamam** |
| **5** | İşlem geçmişi, CSV indirme, cilalama | Audit log, Swagger, hata yönetimi | Dışa aktarım, uç birim testleri | Entegrasyon testi |
| **6** | Responsive düzeltmeler | Güvenlik gözden geçirme | Performans ölçümü | **Teslim + final sunum** |

---

## Git dal (branch) stratejisi

```
main          ← yalnızca çalışan, sunuma hazır kod
 └─ develop   ← ekiplerin birleştiği ana dal
     ├─ feature/ui-login
     ├─ feature/ui-rule-builder
     ├─ feature/auth-jwt
     ├─ feature/db-schema
     ├─ feature/parsers
     └─ feature/rule-engine
```

**Kurallar**

1. `main` ve `develop`'a doğrudan push **yok** — yalnızca Pull Request.
2. Her PR en az bir takım arkadaşı tarafından incelenir.
3. `*.db` dosyaları asla commit edilmez; herkes kendi yerelini migration ile üretir.
4. Kendi branch'inizde çalışmaya başlamadan önce `git pull origin develop` yapın.
5. Commit mesajları: `feat(motor): between operatörü eklendi` biçiminde.

---

## Günlük ritim

- **Her sabah 10 dk ayakta toplantı:** dün ne yaptım / bugün ne yapacağım / neyde takıldım.
- **Haftada bir entegrasyon günü:** tüm branch'ler `develop`'a birleştirilir ve
  uçtan uca senaryo baştan sona denenir.
- **Takıldığınızda 30 dakika kuralı:** 30 dakikada çözemediğiniz sorunu ekibe taşıyın.
