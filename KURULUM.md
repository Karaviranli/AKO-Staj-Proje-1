# Sıfırdan Kurulum Rehberi (Windows)

Bu rehber, projeyi **hiç kurulu olmayan** bir bilgisayarda baştan sona
çalıştırmak içindir. Adımları sırayla takip et.

---

## 1. Gerekli programları kur

Aşağıdaki üç programı kur. Kurduktan sonra **tüm terminal pencerelerini kapat**
(PATH değişiklikleri yeni terminalde geçerli olur).

### a) .NET 9 SDK
- İndir: <https://dotnet.microsoft.com/download/dotnet/9.0>
- "SDK" (Installer, x64) sürümünü indir ve kur.
- Kontrol: yeni bir terminal aç, `dotnet --version` yaz → `9.x.x` görmelisin.

### b) Node.js 20 veya üzeri (LTS)
- İndir: <https://nodejs.org/> → "LTS" sürümü.
- Kurarken tüm varsayılanları kabul et.
- Kontrol: `node --version` → `v20.x` ya da üzeri.

### c) Git
- İndir: <https://git-scm.com/download/win>
- Varsayılanlarla kur.
- Kontrol: `git --version`.

---

## 2. Projeyi indir (clone)

Projeyi koymak istediğin klasörde bir terminal aç ve:

```bash
git clone https://github.com/Karaviranli/AKO-Staj-Proje-1.git
cd AKO-Staj-Proje-1
```

---

## 3. Backend'i kur ve çalıştır

**Birinci terminal** (bu terminali AÇIK bırak):

```bash
cd backend
dotnet restore
dotnet run --project src/DataFlow.API --urls http://localhost:5080
```

- İlk çalıştırmada NuGet paketleri iner (biraz sürebilir).
- Veritabanı (`dataflow.db`) **otomatik oluşur** ve demo verileri yüklenir.
- Şu satırı görünce hazırdır:
  `Now listening on: http://localhost:5080`
- Test: tarayıcıda <http://localhost:5080/swagger> açılıyorsa backend çalışıyor.

> Bu terminali kapatma — kapatırsan backend durur.

---

## 4. Frontend'i kur ve çalıştır

**İkinci terminal aç** (backend'inki açık kalsın):

```bash
cd frontend
copy .env.example .env.local
npm install
npm run build
npm run start
```

- `npm install` bağımlılıkları indirir (birkaç dakika sürebilir, sabırlı ol).
- `npm run build` üretim (production) sürümünü hazırlar — **dev modundan daha kararlıdır**.
- `npm run start` siteyi ayağa kaldırır.
- Şunu görünce hazırdır: `Ready in ...`

> **Neden `dev` değil `build + start`?** `npm run dev` (Turbopack) Windows'ta
> ara sıra donuyor. Sunum ve günlük kullanım için `build + start` çok daha kararlı.
> Kod üzerinde aktif geliştirme yapacaksan `npm run dev` kullanabilirsin.

---

## 5. Siteye gir

Tarayıcıda:

## 👉 http://localhost:3000

**Demo hesabı:**
- Kullanıcı adı: `admin`
- Şifre: `Demo1234!`

Kendi hesabını da "Kayıt ol" ile oluşturabilirsin.

---

## 6. Sistemi dene

1. Sol menü → **Veri Yükle**
2. `sample-data/satis-verileri-bozuk.csv` dosyasını sürükle-bırak
3. Kalite analizi çıkacak → **Kural tanımla →**
4. Sağ üstten **Hazır şablon seç → "Satış — Yaş Segmentasyonu"** → **Önizle**

---

## Sık karşılaşılan sorunlar

| Sorun | Çözüm |
| --- | --- |
| Sitede **500 hatası** / veriler gelmiyor | Backend kapanmıştır. Birinci terminalde `dotnet run` çalışıyor mu bak. |
| `dotnet` / `node` / `git` **komutu bulunamadı** | Program kurulmamış veya terminali kurulumdan sonra yeniden açmamışsın. |
| Backend'de **"DLL is being used by another process"** | Backend zaten başka bir yerde açık. Görev Yöneticisi → `DataFlow.API` görevini bitir, tekrar dene. |
| **Port 3000/5080 zaten kullanımda** | O portu kullanan eski süreci kapat ya da bilgisayarı yeniden başlat. |
| `npm run start` **"Could not find a production build"** diyor | Önce `npm run build` çalıştırmayı unutmuşsun. |

---

## Her seferinde çalıştırmak için (kurulum bir kez yapıldıktan sonra)

Bilgisayarı her açtığında sadece iki terminal:

**Terminal 1 — Backend:**
```bash
cd AKO-Staj-Proje-1/backend
dotnet run --project src/DataFlow.API --urls http://localhost:5080
```

**Terminal 2 — Frontend:**
```bash
cd AKO-Staj-Proje-1/frontend
npm run start
```

> Kodda değişiklik yaptıysan frontend'i başlatmadan önce tekrar `npm run build` yap.
