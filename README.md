# DataFlow — Veri Dönüşüm ve Kural Motoru Platformu

Karışık ve bozuk veri setlerini (CSV / XLSX / JSON) sisteme alıp, kullanıcı
tarafından arayüzden tanımlanan **sıralı kurallarla** temizleyen, her adımın
etkisini raporlayan tam yığın (full-stack) uygulama.

| Katman | Teknoloji |
| --- | --- |
| Sunum | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| Uygulama | .NET 9 Web API, katmanlı mimari, JWT Bearer |
| Veri | Entity Framework Core 9 (Code-First) + SQLite |

---

## Hızlı başlangıç

İki terminal gerekir.

**1) Backend** (http://localhost:5080)

```bash
cd backend && dotnet run --project src/DataFlow.API --urls http://localhost:5080
```

İlk çalıştırmada veritabanı otomatik oluşturulur ve demo verileri yüklenir.
Swagger arayüzü: <http://localhost:5080/swagger>

**2) Frontend** (http://localhost:3000)

```bash
cd frontend && npm install && npm run dev
```

**Demo hesabı:** `admin` / `Demo1234!`

`sample-data/` klasöründeki bilerek bozulmuş dosyalarla sistemi deneyebilirsiniz.

---

## Proje yapısı

```
DataFlow/
├─ backend/
│  ├─ DataFlow.sln
│  └─ src/
│     ├─ DataFlow.API/          # Controller, JWT, Swagger, Program.cs
│     ├─ DataFlow.Business/     # İş mantığı: parser, kural motoru, servisler
│     └─ DataFlow.DataAccess/   # EF Core, entity'ler, migration'lar
├─ frontend/
│  └─ src/
│     ├─ app/                   # Sayfalar + Next.js route handler'ları
│     ├─ components/            # Tasarım sistemi ve ekran bileşenleri
│     ├─ lib/                   # API istemcisi, tipler, oturum yardımcıları
│     └─ proxy.ts               # Route protection (yetkisiz erişim engeli)
├─ sample-data/                 # Bilerek bozulmuş örnek veri setleri
└─ docs/                        # Mimari, API sözleşmesi, iş dağılımı, sunum
```

---

## Uygulamanın üç bölümü

**1. Arayüz (Next.js).** Giriş, veri yükleme, kural stüdyosu, işlem geçmişi.
Kullanıcı hiç kod yazmadan kural kurar.

**2. Kimlik doğrulama (JWT).** .NET Core token üretir. Token, Next.js tarafında
**HttpOnly çerezde** saklanır — tarayıcıdaki JavaScript'e hiç inmez. Backend'e
giden her istek Next.js vekili üzerinden geçer ve `Authorization` başlığı orada
eklenir.

**3. Yükleme ve kural motoru.** Dosya (CSV/XLSX/JSON) veya `POST /api/data/push`
ile JSON gövdesi kabul edilir. Yükleme anında veri kalitesi profili çıkarılır
(eksik oran, tip tutarsızlığı, tekrar eden satır). Ardından kurallar sırayla
uygulanır ve her adım için ayrı rapor üretilir.

---

## Kural modeli

Bir kural, **koşul → aksiyon** çiftidir. Kurallar `order` alanına göre sırayla
çalışır; bir kuralın çıktısı bir sonraki kuralın girdisidir.

```jsonc
{
  "order": 2,
  "name": "Yaş 50'den büyükse X segmenti",
  "enabled": true,
  "condition": {
    "logic": "AND",
    "conditions": [{ "column": "Yas", "operator": "gt", "value": "50" }]
  },
  "action": { "type": "setValue", "targetColumn": "Segment", "value": "X" }
}
```

20 koşul operatörü ve 25 dönüşüm aksiyonu desteklenir; tam liste
`GET /api/rules/catalog` ile alınır — arayüz sihirbazı bu listeden üretilir.

---

## Veritabanı

SQLite kullanılır; kurulum gerektirmez, tek dosyada durur ve `.gitignore`
kapsamındadır. Her geliştirici kendi yerel veritabanını şu komutla üretir:

```bash
cd backend && dotnet ef database update --project src/DataFlow.DataAccess --startup-project src/DataFlow.API
```

Uygulama açılışta migration'ları kendisi de uygular, bu komut çoğu zaman gerekmez.

Kolon yapısı dosyadan dosyaya değiştiği için satırlar ilişkisel kolonlara değil,
JSON metin olarak saklanır (schema-less yaklaşım).

---

## Dokümantasyon

- [Sistem mimarisi](docs/MIMARI.md)
- [API sözleşmesi](docs/API-SOZLESMESI.md)
- [İş dağılımı ve zaman planı](docs/IS-DAGILIMI.md)
- [Sunum içeriği](docs/SUNUM-ICERIGI.md)
