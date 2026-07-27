# -*- coding: utf-8 -*-
"""
DataFlow için bilerek "kirli" 5 örnek veri seti üretir.
Her set 500 satır, farklı kolonlar ve farklı kirlilik türleri içerir:
eksik değerler (çeşitli null token'ları), tutarsız harf, karışık sayı
formatları (TR/EN), tekrar eden satırlar, tip uyumsuzlukları, boşluklar.
"""
import csv, json, random, os
from datetime import date, timedelta
import openpyxl

random.seed(2026)
HERE = os.path.dirname(os.path.abspath(__file__))

# Farklı biçimlerde "eksik değer" — kalite analizörünün hepsini yakalaması için.
NULLS = ["", " ", "null", "N/A", "n/a", "-", "yok", "bilinmiyor", "NaN"]

def maybe_null(value, p=0.12):
    return random.choice(NULLS) if random.random() < p else value

def messy_case(text):
    r = random.random()
    if r < 0.25: return text.upper()
    if r < 0.45: return text.lower()
    if r < 0.55: return "  " + text + " "        # baştaki/sondaki boşluk
    return text

def tr_money(value):
    """1234.5 -> bazen '1.234,50' (TR), bazen '1234.50' (EN), bazen bozuk."""
    r = random.random()
    if r < 0.05: return random.choice(["abc", "?", "yaklasik 1000"])  # tip uyumsuz
    if r < 0.55:
        s = f"{value:,.2f}".replace(",", "#").replace(".", ",").replace("#", ".")
        return s
    return f"{value:.2f}"

def rnd_date(start_year=2023):
    start = date(start_year, 1, 1)
    d = start + timedelta(days=random.randint(0, 900))
    fmt = random.choice(["%Y-%m-%d", "%d.%m.%Y", "%d/%m/%Y"])  # karışık tarih formatı
    return d.strftime(fmt)

ADLAR = ["Ahmet","Ayşe","Mehmet","Fatma","Ali","Zeynep","Mustafa","Elif","Hüseyin",
         "Emine","Can","Selin","Burak","Merve","Onur","Deniz","Kemal","Pınar","Serkan",
         "Gizem","Hakan","Nuray","Emre","Aylin","Okan","Seda","Barış","Ece","Volkan","Derya"]
SOYADLAR = ["Yılmaz","Demir","Kaya","Şahin","Çelik","Arslan","Doğan","Öztürk","Aydın",
            "Koç","Kurt","Özdemir","Aksoy","Taş","Yıldız","Bal","Tan","Uçar","Ateş","Gül"]
SEHIRLER = ["İstanbul","Ankara","İzmir","Bursa","Antalya","Konya","Adana","Trabzon",
            "Kocaeli","Eskişehir","Samsun","Denizli","Kayseri","Malatya","Gaziantep"]

def ad_soyad():
    return f"{random.choice(ADLAR)} {random.choice(SOYADLAR)}"

# ---------------------------------------------------------------- 1) SATIŞ (CSV, ;)
def gen_satis():
    rows = []
    for i in range(1, 501):
        rows.append({
            "MusteriNo": 10000 + i,
            "AdSoyad": messy_case(ad_soyad()),
            "Yas": maybe_null(random.randint(3, 82), 0.10),
            "Cinsiyet": maybe_null(random.choice(["Kadın","Erkek","K","E","kadin","erkek"]), 0.08),
            "Sehir": messy_case(random.choice(SEHIRLER)),
            "DogumYeri": maybe_null(random.choice(SEHIRLER), 0.20),
            "ToplamHarcama": tr_money(round(random.uniform(50, 25000), 2)),
            "SiparisSayisi": maybe_null(random.randint(1, 60), 0.06),
            "SonAlimTarihi": maybe_null(rnd_date(), 0.10),
            "Aktif": random.choice(["evet","hayır","E","H","1","0","true","false"]),
        })
    # ~15 tekrar eden satır
    for _ in range(15):
        rows.append(dict(random.choice(rows[:200])))
    random.shuffle(rows)
    write_csv("01-satis-musteri.csv", rows, delimiter=";")

# ---------------------------------------------------------------- 2) ÇALIŞAN (JSON)
def gen_calisan():
    deps = ["Üretim","Kalite","Muhasebe","Lojistik","İnsan Kaynakları","Ar-Ge","Satış","BT"]
    rows = []
    for i in range(1, 501):
        rows.append({
            "SicilNo": 50000 + i,
            "Ad": messy_case(ad_soyad()),
            "Departman": maybe_null(random.choice(deps), 0.14),
            "Unvan": maybe_null(random.choice(["Uzman","Kıdemli Uzman","Şef","Müdür","Stajyer","Operatör"]), 0.08),
            "Maas": tr_money(round(random.uniform(17000, 95000), 2)),
            "IseGirisTarihi": maybe_null(rnd_date(2015), 0.05),
            "Yas": maybe_null(random.randint(19, 64), 0.09),
            "Sehir": messy_case(random.choice(SEHIRLER)),
            "PrimYuzdesi": maybe_null(random.choice([0, 5, 10, 15, 20, "%10", "%5"]), 0.15),
            "Email": maybe_null(f"kullanici{i}@sirket.com", 0.10),
        })
    for _ in range(12):
        rows.append(dict(random.choice(rows[:200])))
    random.shuffle(rows)
    # JSON: {"data": [...]} sarmalı — parser bunu da destekliyor
    path = os.path.join(HERE, "02-calisan-ik.json")
    # Değerleri normalize et: maybe_null sayıları int bırakabilir, JSON'a olduğu gibi yaz
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"data": rows}, f, ensure_ascii=False, indent=2)
    print("02-calisan-ik.json", len(rows), "satır")

# ---------------------------------------------------------------- 3) ÜRÜN/STOK (CSV, ,)
def gen_urun():
    kats = ["Elektronik","Giyim","Gıda","Kırtasiye","Mobilya","Kozmetik","Oyuncak","Spor"]
    rows = []
    for i in range(1, 501):
        fiyat = round(random.uniform(5, 8000), 2)
        rows.append({
            "UrunKodu": f"URN-{1000+i}",
            "UrunAdi": messy_case(random.choice(kats) + " Ürün " + str(random.randint(1,999))),
            "Kategori": maybe_null(random.choice(kats), 0.10),
            "BirimFiyat": tr_money(fiyat),
            "StokAdedi": maybe_null(random.randint(0, 1500), 0.08),
            "KritikStok": random.choice([10, 20, 50, 100]),
            "Tedarikci": maybe_null(random.choice(["Alfa Ltd","Beta A.Ş","Gama Tic","Delta San","Epsilon"]), 0.13),
            "KDV": random.choice([1, 8, 10, 18, 20, "%18", "%20"]),
            "SonGuncelleme": maybe_null(rnd_date(2024), 0.07),
            "Aktif": random.choice(["evet","hayır","E","H","true","false","1","0"]),
        })
    for _ in range(18):
        rows.append(dict(random.choice(rows[:200])))
    random.shuffle(rows)
    write_csv("03-urun-stok.csv", rows, delimiter=",")

# ---------------------------------------------------------------- 4) SİPARİŞ/LOJİSTİK (CSV, ;)
def gen_siparis():
    durumlar = ["Hazırlanıyor","Kargoda","Teslim Edildi","İptal","İade","hazirlaniyor","KARGODA"]
    kargolar = ["Yurtiçi","Aras","MNG","PTT","Sürat","UPS"]
    rows = []
    for i in range(1, 501):
        tutar = round(random.uniform(30, 15000), 2)
        rows.append({
            "SiparisNo": f"SIP{2026000+i}",
            "MusteriAdi": messy_case(ad_soyad()),
            "Sehir": messy_case(random.choice(SEHIRLER)),
            "SiparisTarihi": maybe_null(rnd_date(2025), 0.06),
            "Tutar": tr_money(tutar),
            "KargoFirmasi": maybe_null(random.choice(kargolar), 0.11),
            "KargoUcreti": tr_money(round(random.uniform(0, 250), 2)),
            "Durum": random.choice(durumlar),
            "UrunAdedi": maybe_null(random.randint(1, 25), 0.07),
            "OdemeTipi": maybe_null(random.choice(["Kredi Kartı","Havale","Kapıda Nakit","Kapıda Kart"]), 0.10),
        })
    for _ in range(14):
        rows.append(dict(random.choice(rows[:200])))
    random.shuffle(rows)
    write_csv("04-siparis-lojistik.csv", rows, delimiter=";")

# ---------------------------------------------------------------- 5) ÖĞRENCI/SINAV (XLSX)
def gen_ogrenci():
    bolumler = ["Bilgisayar","Makine","Elektrik","Endüstri","İnşaat","Mekatronik","Yazılım"]
    rows = []
    for i in range(1, 501):
        v = round(random.uniform(0, 4), 2)
        rows.append({
            "OgrenciNo": 202600000 + i,
            "AdSoyad": messy_case(ad_soyad()),
            "Bolum": maybe_null(random.choice(bolumler), 0.09),
            "Sinif": maybe_null(random.choice([1,2,3,4,"1","2","Hazırlık"]), 0.07),
            "Vize": maybe_null(random.randint(0, 100), 0.12),
            "Final": maybe_null(random.randint(0, 100), 0.14),
            "Ortalama": maybe_null(round(random.uniform(0, 100), 1), 0.10),
            "GNO": tr_money(v) if random.random() < 0.5 else str(round(v,2)),
            "DevamsizlikSaat": maybe_null(random.randint(0, 40), 0.08),
            "Sehir": messy_case(random.choice(SEHIRLER)),
        })
    for _ in range(10):
        rows.append(dict(random.choice(rows[:200])))
    random.shuffle(rows)
    # XLSX yaz
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "Ogrenciler"
    cols = list(rows[0].keys())
    ws.append(cols)
    for r in rows:
        ws.append([r[c] for c in cols])
    path = os.path.join(HERE, "05-ogrenci-sinav.xlsx")
    wb.save(path)
    print("05-ogrenci-sinav.xlsx", len(rows), "satır")

def write_csv(name, rows, delimiter=","):
    path = os.path.join(HERE, name)
    cols = list(rows[0].keys())
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=cols, delimiter=delimiter)
        w.writeheader()
        w.writerows(rows)
    print(name, len(rows), "satır,", "ayırıcı='" + delimiter + "'")

if __name__ == "__main__":
    gen_satis()
    gen_calisan()
    gen_urun()
    gen_siparis()
    gen_ogrenci()
    print("--- tamam ---")
