# Kurumsal Sunum İçeriği

14 slayt · ~20 dakika. Her slaytta **ne söyleyeceğiniz** de yazılı.

> **Altın kural:** Yöneticiler kod görmek istemez. Problem, çözüm, mimari ve
> ölçülebilir sonuç isterler. Tek kod slaytı yeter — o da kural motoru olsun.

---

## 1 · Kapak

**DataFlow — Veri Dönüşüm ve Kural Motoru Platformu**
Ekip adları · Tarih · Şirket logosu

---

## 2 · Problem

> "Kurumlarda veri, nadiren temiz gelir."

- Farklı departmanlar farklı formatlarda veri gönderir (CSV, Excel, JSON, API).
- Aynı bilgi farklı yazılır: `İSTANBUL`, `istanbul`, `Istanbul`.
- Eksik alanlar boş, `-`, `n/a`, `null` gibi farklı şekillerde ifade edilir.
- Sayılar metin olarak gelir: `"1.250,50 TL"`.
- Aynı kayıt birden fazla kez girilir.

**Sonuç:** Analistler zamanlarının büyük kısmını analiz yapmaya değil, veriyi
elle temizlemeye harcar. Her seferinde baştan, hiçbir iz bırakmadan.

---

## 3 · Çözümümüz

**DataFlow**, veri temizleme işini üç adıma indirger:

1. **Yükle** — CSV, Excel, JSON veya doğrudan API gövdesi.
2. **Gör** — Sistem, veri kalitesi profilini otomatik çıkarır.
3. **Kurala bağla** — Kullanıcı kod yazmadan sıralı kurallar tanımlar; kurallar
   kaydedilir ve başka veri setlerine tekrar uygulanır.

**Ayırt edici yön:** Sistem kara kutu değildir. Her kuralın kaç satırı
etkilediği adım adım raporlanır.

---

## 4 · Sistem Mimarisi

`docs/MIMARI.md` içindeki katman şemasını görsele dönüştürün.

**Anlatım:**
> "Üç katmanlı bir mimari kurduk. Sunum katmanı Next.js, uygulama katmanı .NET
> Core, veri katmanı Entity Framework Core üzerinden SQLite. Katmanlar arası
> bağımlılık tek yönlü: API iş katmanını tanır, iş katmanı veri katmanını tanır,
> tersi geçerli değil. Bu **Separation of Concerns** prensibidir ve iş
> mantığımızı web çatısından bağımsız test edebilmemizi sağlar."

---

## 5 · Veri Akışı

`docs/MIMARI.md` bölüm 2'deki iki akış diyagramı.

**Anlatım:**
> "Dosya geldiğinde önce **Factory** kalıbı devreye girer ve uzantıya göre doğru
> okuyucuyu seçer. Yarın XML desteği istenirse mevcut koda hiç dokunmadan tek bir
> sınıf ekliyoruz — **Open/Closed Principle**."

---

## 6 · Kural Motoru (tek kod slaytı)

Ekranda kural JSON'u ve yanında Türkçe karşılığı:

```jsonc
{
  "order": 2,
  "condition": { "conditions": [{ "column": "Yas", "operator": "gt", "value": "50" }] },
  "action": { "type": "setValue", "targetColumn": "Segment", "value": "X" }
}
```
→ *"Yaşı 50'den büyük olan satırlarda Segment kolonuna X yaz."*

**Anlatım:**
> "Kurallar bir **boru hattından (pipeline)** sırayla geçer. 2. kural, 1. kuralın
> çıktısı üzerinde çalışır. Bu sayede 'önce boşlukları doldur, sonra segmentle'
> gibi birbirine bağımlı kurgular mümkün. Şu an 20 koşul operatörü ve 25 dönüşüm
> aksiyonu destekliyoruz."

---

## 7 · Ekran: Giriş

`/giris` ekran görüntüsü.

**Anlatım:** JWT tabanlı kimlik doğrulama. Bir sonraki slaytta detayı var.

---

## 8 · Ekran: Veri Yükleme + Kalite Analizi

Yükleme ekranı ve kalite raporu tablosu.

**Anlatım:**
> "Dosya yüklendiği anda sistem her kolonun veri tipini tahmin ediyor, eksik
> oranını, farklı değer sayısını ve tip uyumsuzluklarını çıkarıyor. Bu örnekte
> 15 satırlık dosyada `ToplamHarcama` kolonunda sayısal olmayan bir değer
> yakalandı ve 4 hücrenin farklı biçimlerde boş olduğu tespit edildi."

**Vurgulanacak sayı:** Kalite skoru **%93**.

---

## 9 · Ekran: Kural Stüdyosu

Kural zinciri + koşul/aksiyon panelleri.

**Anlatım:**
> "Kullanıcı kod yazmıyor. Kolonu seçiyor, operatörü seçiyor, aksiyonu seçiyor.
> Kuralların sırasını ok tuşlarıyla değiştirebiliyor. 'Önizle' dediğinde sonuç
> hesaplanıyor ama **veritabanına yazılmıyor** — deneme yanılma serbest."

---

## 10 · Ekran: Yürütme Raporu (en güçlü slayt)

Yürütme raporu ekran görüntüsü:

```
1. Ad boşluklarını kırp        → 15 satır koşula uydu, 0 hücre değişti
2. Ad'ı düzgün yaz             → 15 satır koşula uydu, 8 hücre değişti
4. Doğum yeri boşsa Belirsiz   →  4 satır koşula uydu, 4 hücre değişti
6. Yaş > 50 ise segment X      →  6 satır koşula uydu, 6 hücre değişti
9. Adı boş olan satırları sil  →  1 satır silindi (15 → 14)
```

**Anlatım:**
> "Bu, projenin en önemli çıktısı. Sistem sadece 'temizledim' demiyor; hangi
> kuralın kaç satıra dokunduğunu, kaç hücreyi değiştirdiğini ve ne kadar sürdüğünü
> gösteriyor. **9 kural, 15 satır, 36 hücre düzeltmesi, 7 milisaniye.**
> Denetlenebilirlik kurumsal yazılımda pazarlık konusu değildir."

---

## 11 · Güvenlik

`docs/MIMARI.md` bölüm 5'teki 9 maddeyi 4'e indirin:

1. Şifreler **BCrypt** ile hash'lenir, düz metin hiçbir yerde yok.
2. .NET imzalı **JWT** üretir; süresi dolan token anında geçersiz olur.
3. Token **HttpOnly çerezde** durur — tarayıcıdaki JavaScript ona erişemez,
   dolayısıyla XSS ile çalınamaz.
4. Yetki her zaman token'dan okunur; istemciden gelen kullanıcı kimliğine
   **asla güvenilmez**. Bir kullanıcı başkasının verisini göremez.

---

## 12 · Neden SQLite?

**Anlatım (soru gelmeden önce siz açın):**
> "Veri tabanı mimarisinde Entity Framework Core Code-First yaklaşımını
> kullandık. Geliştirme aşamasında sıfır konfigürasyonla çalışabilmek için
> SQLite tercih ettik — ekip arkadaşlarımız projeyi klonladıkları anda çalışan
> bir veritabanına sahip oluyorlar. EF Core'un provider yapısı sayesinde canlıya
> geçerken `Program.cs` içindeki **tek bir satırı** değiştirerek PostgreSQL veya
> SQL Server'a geçebiliyoruz. Hiçbir iş mantığı kodu değişmiyor."

Yanına o tek satırı koyun:

```csharp
options.UseSqlite(connectionString);      // geliştirme
// options.UseNpgsql(connectionString);   // canlı
```

---

## 13 · İş Dağılımı ve Takvim

`docs/IS-DAGILIMI.md` içindeki ekip tablosu ve 6 haftalık plan (Gantt görünümü).

**Anlatım:**
> "6 kişiyi 3 ikili ekibe böldük. Her ekibin kendi dosya alanı var, bu yüzden
> aynı dosyada çakışma yaşamıyoruz. En kritik kararımız: kod yazmadan **önce API
> sözleşmesini** sabitlemek oldu. Bu sayede arayüz ekibi, backend hazır olmadan
> sahte veriyle çalışabildi."

---

## 14 · Yol Haritası (v2)

Yapmadıklarınızı bilmek, yaptıklarınız kadar değerlidir:

- Kural şablonlarının departmanlar arası paylaşımı ve sürümlenmesi
- Zamanlanmış otomatik işleme (her gece belirli klasörü tara)
- Büyük dosyalar için akış tabanlı (streaming) işleme — 25 MB sınırını kaldırır
- İki veri setini birleştirme (JOIN) aksiyonu
- Rol bazlı yetkilendirme: Görüntüleyen / Analist / Yönetici

---

## Sunum öncesi kontrol listesi

- [ ] Backend ve frontend **açık ve çalışır** durumda (canlı demo B planı: video kaydı)
- [ ] `sample-data/` dosyaları hazır, demo hesabıyla giriş test edildi
- [ ] Ekran görüntüleri yüksek çözünürlüklü ve **gerçek uygulamadan** alınmış
- [ ] Her ekip üyesi kendi bölümünü anlatabiliyor
- [ ] Swagger sayfası açılabiliyor (teknik soru gelirse)
- [ ] Süre provası yapıldı — 20 dakikayı aşmıyor

## Gelmesi muhtemel sorular

**"Neden mikroservis değil?"**
> Tek bir iş akışı ve altı kişilik bir ekip için mikroservis, çözdüğünden fazla
> sorun üretirdi. Katmanlı monolit kurduk; katmanlar zaten ayrı olduğu için
> ileride ihtiyaç olursa motor ayrı bir servise taşınabilir.

**"Çok büyük dosyalarda ne olur?"**
> Şu an sınır 25 MB ve veri belleğe alınıyor. Yol haritamızda akış tabanlı
> işleme var; okuyucularımız zaten Stream üzerinden çalışıyor, bu geçişi
> kolaylaştırıyor.

**"Kullanıcı yanlış kural yazarsa?"**
> İki koruma var. Birincisi `dryRun` — sonuç kaydedilmeden önizlenir. İkincisi,
> ham veri hiçbir zaman değiştirilmez; her çalıştırma ayrı bir kayıt üretir, yani
> geri dönüş her zaman mümkün.

**"Bir kural hata verirse tüm işlem çöker mi?"**
> Hayır. Motor her kuralı ayrı ayrı korumaya alır; hatalı kural atlanır, raporda
> uyarı olarak görünür ve zincir kalan kurallarla devam eder.
