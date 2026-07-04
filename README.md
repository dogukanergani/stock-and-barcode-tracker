# 🛒 Tekel Analitik Yönetim Sistemi

Bu proje, yerel bir işletmenin  tüm satış, stok ve envanter süreçlerini dijitalleştirmek; aynı zamanda arka planda ticari veri toplayarak akademik/istatistiksel analizler için gerçek dünya verisi oluşturmak amacıyla geliştirilmiş modern bir WPF masaüstü uygulamasıdır.

## 🚀 Öne Çıkan Özellikler

* **Işık Hızında Satış Modülü:** Fiziksel lazer barkod okuyucularla tam uyumlu çalışan, klavye emülasyonu destekleyen akıllı giriş sistemi.
* **Akıllı Ürün Arama:** Barkodsuz ürünler için kelime bazlı hızlı arama motoru 
* **Parçalı Ödeme Sistemi:** Müşterilerin hem nakit hem kart ile aynı anda ödeme yapabilmesine olanak tanıyan esnek kasa altyapısı.
* **Gelişmiş Veri Analitiği:** 
  * En çok kâr getiren ilk 10 ürün analizi.
  * Saatlik satış yoğunluğu dağılımı.
  * Haftanın günlerine göre ciro haritalandırması.
* **Dinamik Stok Yönetimi:** Kritik stok seviyelerine düşen ürünler için otomatik renkli bildirim sistemi.
* **Görsel Ajanda Panosu:** İşletme içi görevlerin ve hatırlatmaların tutulduğu, kart tabanlı (Post-it tarzı) not sistemi.
* **Sermaye Özeti:** Dükkandaki anlık malzemenin toplam geliş ve satış değerini hesaplayan finansal özet ekranı.

## 🛠️ Kullanılan Teknolojiler

* **Dil:** C# (Entity Framework / ADO.NET)
* **Arayüz (UI):** WPF (Windows Presentation Foundation) & XAML
* **Framework:** .NET 8.0
* **Veritabanı:** SQLite (Yerel ve sunucusuz mimari)
* **Tasarım Dili:** Modern Dark Mode, DropShadow efektleri, piksel tabanlı pürüzsüz kaydırma (Pixel Scrolling).

## 🗄️ Veritabanı Mimarisi

Sistem, verileri dışarıdan bağımsız ve güvenli bir şekilde üç farklı yerel SQLite dosyasında tutar:
* `stok.db`: Barkod, ürün adı, alış/satış fiyatları ve anlık stok miktarları.
* `ciro.db`: Kesilen fişler, ödeme türleri (Nakit/Kart/Veresiye), tarih/saat damgaları ve net kâr oranları.
* `notlar.db`: Görsel panoda yer alan işletme notları ve hatırlatmalar.

## 💻 Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda derlemek (build) ve tek bir çalıştırılabilir `.exe` dosyası haline getirmek için projenin kök dizininde aşağıdaki terminal komutunu çalıştırabilirsiniz:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:AssemblyName=Otomasyon_1.0 -o C:\OtomasyonBuild
