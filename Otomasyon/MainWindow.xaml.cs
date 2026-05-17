using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace Otomasyon
{
    public partial class MainWindow : Window
    {
        private List<SepetUrun> sepet = new List<SepetUrun>();
        private List<StokBilgisi> tumStokListesi = new List<StokBilgisi>();
        private string sonOkunanBarkod = "";
        private bool isParcaliHesaplaniyor = false;
        private bool isStokBildirimAktif = false;

        public MainWindow()
        {
            InitializeComponent();
            VeritabaniHazirla();
            UrunListele();
            TarihAgaciniDoldur();
            BildirimleriGuncelle();
            NotlariListele();

            this.Loaded += (s, e) => TxtHiddenInput.Focus();

            this.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (OverlayParcali.Visibility == Visibility.Visible ||
                    OverlayAyarlar.Visibility == Visibility.Visible ||
                    OverlaySermaye.Visibility == Visibility.Visible ||
                    OverlayYeniNot.Visibility == Visibility.Visible)
                {
                    return;
                }

                var tiklanan = e.OriginalSource as DependencyObject;
                if (tiklanan != null)
                {
                    if (FindAncestor<Button>(tiklanan) != null ||
                        FindAncestor<TextBox>(tiklanan) != null ||
                        FindAncestor<ComboBox>(tiklanan) != null ||
                        FindAncestor<ListBoxItem>(tiklanan) != null)
                    {
                        return;
                    }
                }
                Dispatcher.BeginInvoke(new Action(() => TxtHiddenInput.Focus()));
            };
        }

        private void VeritabaniHazirla()
        {
            using (var connStok = new SqliteConnection("Data Source=stok.db"))
            {
                connStok.Open();
                var cmd = new SqliteCommand("CREATE TABLE IF NOT EXISTS Urunler (Barkod TEXT PRIMARY KEY, Ad TEXT, Fiyat REAL, GelisFiyati REAL, Stok INTEGER, Kategori TEXT, AltKategori TEXT);", connStok);
                cmd.ExecuteNonQuery();
            }

            using (var connCiro = new SqliteConnection("Data Source=ciro.db"))
            {
                connCiro.Open();
                var cmd = new SqliteCommand("CREATE TABLE IF NOT EXISTS Satislar (ID INTEGER PRIMARY KEY AUTOINCREMENT, Ad TEXT, Fiyat REAL, Tarih DATETIME, OdemeTuru TEXT, Kar REAL);", connCiro);
                cmd.ExecuteNonQuery();
            }

            using (var connNot = new SqliteConnection("Data Source=notlar.db"))
            {
                connNot.Open();
                var cmd = new SqliteCommand("CREATE TABLE IF NOT EXISTS Notlar (ID INTEGER PRIMARY KEY AUTOINCREMENT, Metin TEXT, Tarih DATETIME);", connNot);
                cmd.ExecuteNonQuery();
            }
        }

        // YENİ NOT SİSTEMİ FONKSİYONLARI
        private void NotlariListele()
        {
            var liste = new List<NotBilgisi>();
            using (var conn = new SqliteConnection("Data Source=notlar.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT ID, Metin, Tarih FROM Notlar ORDER BY Tarih DESC", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        liste.Add(new NotBilgisi
                        {
                            ID = rdr.GetInt32(0),
                            Metin = rdr.GetString(1),
                            Tarih = rdr.GetDateTime(2).ToString("dd.MM.yyyy HH:mm")
                        });
                    }
                }
            }
            if (ListNotlar != null) ListNotlar.ItemsSource = liste;
        }

        private void BtnYeniNotAc_Click(object sender, RoutedEventArgs e)
        {
            OverlayYeniNot.Visibility = Visibility.Visible;
            InpNotMetin.Focus();
        }

        private void BtnYeniNotKapat_Click(object sender, RoutedEventArgs e)
        {
            OverlayYeniNot.Visibility = Visibility.Collapsed;
            InpNotMetin.Clear();
            TxtHiddenInput.Focus();
        }

        private void BtnNotKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InpNotMetin.Text)) return;

            using (var conn = new SqliteConnection("Data Source=notlar.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("INSERT INTO Notlar (Metin, Tarih) VALUES (@m, @t)", conn);
                cmd.Parameters.AddWithValue("@m", InpNotMetin.Text);
                cmd.Parameters.AddWithValue("@t", DateTime.Now);
                cmd.ExecuteNonQuery();
            }

            InpNotMetin.Clear();
            OverlayYeniNot.Visibility = Visibility.Collapsed;
            NotlariListele();
            TxtHiddenInput.Focus();
        }

        private void BtnNotSil_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is NotBilgisi secilenNot)
            {
                using (var conn = new SqliteConnection("Data Source=notlar.db"))
                {
                    conn.Open();
                    var cmd = new SqliteCommand("DELETE FROM Notlar WHERE ID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", secilenNot.ID);
                    cmd.ExecuteNonQuery();
                }
                NotlariListele();
            }
        }

        // --- DİĞER FONKSİYONLAR ---
        private void UrunListele()
        {
            tumStokListesi.Clear();
            using (var conn = new SqliteConnection("Data Source=stok.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Urunler", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        tumStokListesi.Add(new StokBilgisi
                        {
                            Barkod = rdr.GetString(0),
                            Ad = rdr.GetString(1),
                            Fiyat = rdr.GetDouble(2),
                            GelisFiyati = rdr.IsDBNull(3) ? 0 : rdr.GetDouble(3),
                            Stok = rdr.GetInt32(4),
                            Kategori = rdr.FieldCount > 5 && !rdr.IsDBNull(5) ? rdr.GetString(5) : "Diğer",
                            AltKategori = rdr.FieldCount > 6 && !rdr.IsDBNull(6) ? rdr.GetString(6) : ""
                        });
                    }
                }
            }
            Filtrele();
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void TxtUrunAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            string aranan = TxtUrunAra.Text.ToLower().Trim();
            if (aranan.Length < 2)
            {
                ListAramaSonuclari.Visibility = Visibility.Collapsed;
                return;
            }
            string[] kelimeler = aranan.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sonuclar = tumStokListesi.Where(u =>
            {
                string urunAdi = u.Ad.ToLower();
                return kelimeler.All(kelime => urunAdi.Contains(kelime));
            }).Take(15).ToList();

            if (sonuclar.Count > 0)
            {
                ListAramaSonuclari.ItemsSource = sonuclar;
                ListAramaSonuclari.Visibility = Visibility.Visible;
            }
            else
            {
                ListAramaSonuclari.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAramaEkle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is StokBilgisi secilenUrun)
            {
                UrunuGetir(secilenUrun.Barkod);
                TxtUrunAra.Text = "";
                ListAramaSonuclari.Visibility = Visibility.Collapsed;
                TxtHiddenInput.Focus();
            }
        }

        private void TreeKategoriler_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;
            var treeViewItem = FindAncestor<TreeViewItem>(clickedElement);
            if (treeViewItem != null && treeViewItem.HasItems)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
            }
        }

        private void Filtrele()
        {
            if (GridUrunler == null || tumStokListesi == null) return;
            var filtreliListe = tumStokListesi.AsEnumerable();

            if (TreeKategoriler.SelectedItem is TreeViewItem item && item.Tag != null)
            {
                string tag = item.Tag.ToString();
                string[] parcalar = tag.Split('-');

                if (parcalar[0] == "Ana")
                {
                    filtreliListe = filtreliListe.Where(x => x.Kategori == parcalar[1]);
                }
                else if (parcalar[0] == "Alt")
                {
                    filtreliListe = filtreliListe.Where(x => x.Kategori == parcalar[1] && x.AltKategori == parcalar[2]);
                }
            }

            if (TxtStokAra != null && !string.IsNullOrWhiteSpace(TxtStokAra.Text))
            {
                string aranan = TxtStokAra.Text.ToLower().Trim();
                string[] kelimeler = aranan.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filtreliListe = filtreliListe.Where(u =>
                {
                    string urunAdi = u.Ad.ToLower();
                    return kelimeler.All(x => urunAdi.Contains(x));
                });
            }
            GridUrunler.ItemsSource = filtreliListe.ToList();
        }

        private void TreeKategoriler_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (GridUrunler != null) Filtrele();
        }

        private void TxtStokAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filtrele();
        }

        private void CmbKategori_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbKategori.SelectedItem is ComboBoxItem item && CmbAltKategori != null)
            {
                CmbAltKategori.Items.Clear();
                string secim = item.Content.ToString();

                if (secim == "Sigaralar") AddItemsToCombo("JTI", "BAT", "Philip Morris", "Imperial", "Diğer");
                else if (secim == "Alkoller") AddItemsToCombo("Bira", "Rakı", "Viski", "Votka", "Şarap", "Cin-Tekila-Likör", "Diğer");
                else if (secim == "İçecekler") AddItemsToCombo("Gazlı İçecekler", "Soğuk Çaylar", "Enerji İçecekleri", "Soğuk Kahveler", "Meyve Suları", "Sular", "Diğer");
                else if (secim == "Çerezler") AddItemsToCombo("Cipsler", "Kuruyemişler", "Diğer");
                else if (secim == "Dondurmalar") AddItemsToCombo("Algida", "Diğer");
                else if (secim == "Atıştırmalıklar") AddItemsToCombo("Ülker", "Eti", "Jelibon", "Diğer");
                else CmbAltKategori.Items.Add("Genel");
            }
        }

        private void AddItemsToCombo(params string[] items)
        {
            foreach (var i in items) CmbAltKategori.Items.Add(i);
        }

        private void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new SqliteConnection("Data Source=stok.db"))
                {
                    conn.Open();
                    var cmd = new SqliteCommand("INSERT OR REPLACE INTO Urunler VALUES (@b, @a, @f, @g, @s, @k, @ak)", conn);
                    string barkod = InpBarkod.Text.Trim();
                    if (string.IsNullOrEmpty(barkod)) barkod = string.IsNullOrEmpty(sonOkunanBarkod) ? "EL-İLE" : sonOkunanBarkod;

                    cmd.Parameters.AddWithValue("@b", barkod);
                    cmd.Parameters.AddWithValue("@a", InpUrunAdi.Text);
                    cmd.Parameters.AddWithValue("@f", double.Parse(InpFiyat.Text.Replace(".", ",")));
                    cmd.Parameters.AddWithValue("@g", double.Parse(InpGelisFiyat.Text.Replace(".", ",")));
                    cmd.Parameters.AddWithValue("@s", int.Parse(InpStok.Text));
                    cmd.Parameters.AddWithValue("@k", CmbKategori.Text);
                    string altKategori = CmbAltKategori.SelectedItem != null ? CmbAltKategori.SelectedItem.ToString() : "";
                    cmd.Parameters.AddWithValue("@ak", altKategori);
                    cmd.ExecuteNonQuery();
                }
                UrunListele();
                MessageBox.Show("Ürün Başarıyla Kaydedildi.");
                sonOkunanBarkod = "";
                InpBarkod.Clear();
                BildirimleriGuncelle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void BtnStokUrunSil_Click(object sender, RoutedEventArgs e)
        {
            string barkod = InpBarkod.Text.Trim();
            if (string.IsNullOrEmpty(barkod))
            {
                MessageBox.Show("Lütfen silmek istediğiniz ürünü seçin veya barkodunu yazın.");
                return;
            }

            if (MessageBox.Show("Bu ürünü tamamen silmek istediğinize emin misiniz?", "Ürün Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var conn = new SqliteConnection("Data Source=stok.db"))
                {
                    conn.Open();
                    var cmd = new SqliteCommand("DELETE FROM Urunler WHERE Barkod=@b", conn);
                    cmd.Parameters.AddWithValue("@b", barkod);
                    cmd.ExecuteNonQuery();
                }
                UrunListele();
                InpBarkod.Clear();
                InpUrunAdi.Clear();
                InpFiyat.Clear();
                InpGelisFiyat.Clear();
                InpStok.Clear();
                MessageBox.Show("Ürün sistemden silindi.");
                BildirimleriGuncelle();
            }
        }

        private void GridUrunler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridUrunler.SelectedItem is StokBilgisi secilenUrun)
            {
                InpBarkod.Text = secilenUrun.Barkod;
                InpUrunAdi.Text = secilenUrun.Ad;
                InpFiyat.Text = secilenUrun.Fiyat.ToString();
                InpGelisFiyat.Text = secilenUrun.GelisFiyati.ToString();
                InpStok.Text = secilenUrun.Stok.ToString();
                sonOkunanBarkod = secilenUrun.Barkod;
                CmbKategori.Text = secilenUrun.Kategori;
                CmbAltKategori.Text = secilenUrun.AltKategori;
            }
        }

        private void TxtHiddenInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(TxtHiddenInput.Text))
                {
                    UrunuGetir(TxtHiddenInput.Text);
                    TxtHiddenInput.Clear();
                }
            }
            if (e.Key == Key.F1) SatisKaydet("NAKİT");
            if (e.Key == Key.F2) SatisKaydet("KART");
            if (e.Key == Key.F3) SatisKaydet("VERESİYE");
            if (e.Key == Key.F4) BtnParcali_Click(null, null);
            if (e.Key == Key.Escape) BtnSatisIptal_Click(null, null);
        }

        private void UrunuGetir(string barkod)
        {
            using (var conn = new SqliteConnection("Data Source=stok.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT Ad, Fiyat FROM Urunler WHERE Barkod=@b", conn);
                cmd.Parameters.AddWithValue("@b", barkod);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        string ad = rdr.GetString(0);
                        double fiyat = rdr.GetDouble(1);

                        var sepettekiUrun = sepet.FirstOrDefault(x => x.Ad == ad);
                        if (sepettekiUrun != null)
                        {
                            sepettekiUrun.Adet++;
                        }
                        else
                        {
                            sepet.Add(new SepetUrun { Ad = ad, Fiyat = fiyat, Adet = 1 });
                        }

                        UpdateCart();
                        TxtSonUrun.Text = ad;
                        TxtSonFiyat.Text = fiyat.ToString("N2") + " ₺";
                    }
                    else
                    {
                        sonOkunanBarkod = barkod;
                        TxtSonUrun.Text = "KAYITSIZ ÜRÜN!";
                        TxtSonFiyat.Text = "0.00 ₺";
                    }
                }
            }
        }

        private void BtnAdetArtir_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is SepetUrun urun)
            {
                urun.Adet++;
                UpdateCart();
                TxtHiddenInput.Focus();
            }
        }

        private void BtnAdetAzalt_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is SepetUrun urun)
            {
                if (urun.Adet > 1)
                {
                    urun.Adet--;
                }
                else
                {
                    sepet.Remove(urun);
                }
                UpdateCart();
                TxtHiddenInput.Focus();
            }
        }

        private void PopulerUrunleriGuncelle()
        {
            var liste = new List<AnalizVerisi>();
            using (var conn = new SqliteConnection("Data Source=ciro.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT Ad, COUNT(*) as Adet, SUM(Kar) as TopKar FROM Satislar GROUP BY Ad ORDER BY TopKar DESC LIMIT 10", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        liste.Add(new AnalizVerisi
                        {
                            Ad = rdr.GetString(0),
                            Adet = rdr.GetInt32(1),
                            ToplamKar = rdr.GetDouble(2)
                        });
                    }
                }
            }
            if (GridPopuler != null) GridPopuler.ItemsSource = liste;
        }

        private void SaatlikAnalizGuncelle()
        {
            var liste = new List<SaatlikAnaliz>();
            double maksimumCiro = 1;

            using (var conn = new SqliteConnection("Data Source=ciro.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT strftime('%H', Tarih) as Saat, SUM(Fiyat) as TopCiro FROM Satislar GROUP BY Saat ORDER BY Saat", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var analiz = new SaatlikAnaliz
                        {
                            Saat = rdr.GetString(0) + ":00",
                            ToplamCiro = rdr.GetDouble(1)
                        };
                        if (analiz.ToplamCiro > maksimumCiro) maksimumCiro = analiz.ToplamCiro;
                        liste.Add(analiz);
                    }
                }
            }

            foreach (var item in liste)
            {
                item.BarHeight = (item.ToplamCiro / maksimumCiro) * 180;
                if (item.BarHeight < 5) item.BarHeight = 5;
            }

            if (ListSaatlik != null) ListSaatlik.ItemsSource = liste;
        }

        private void GunlukAnalizGuncelle()
        {
            var gunler = new List<GunlukAnaliz>
            {
                new GunlukAnaliz { GunGosterim = "Pazartesi", GunIndex = "1" },
                new GunlukAnaliz { GunGosterim = "Salı", GunIndex = "2" },
                new GunlukAnaliz { GunGosterim = "Çarşamba", GunIndex = "3" },
                new GunlukAnaliz { GunGosterim = "Perşembe", GunIndex = "4" },
                new GunlukAnaliz { GunGosterim = "Cuma", GunIndex = "5" },
                new GunlukAnaliz { GunGosterim = "Cumartesi", GunIndex = "6" },
                new GunlukAnaliz { GunGosterim = "Pazar", GunIndex = "0" }
            };

            double maksimumCiro = 1;

            using (var conn = new SqliteConnection("Data Source=ciro.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT strftime('%w', Tarih) as Gun, SUM(Fiyat) FROM Satislar WHERE Tarih >= date('now', '-3 month') GROUP BY Gun", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string index = rdr.GetString(0);
                        double ciro = rdr.GetDouble(1);
                        var gun = gunler.FirstOrDefault(x => x.GunIndex == index);
                        if (gun != null)
                        {
                            gun.ToplamCiro = ciro;
                            if (ciro > maksimumCiro) maksimumCiro = ciro;
                        }
                    }
                }
            }

            foreach (var item in gunler)
            {
                item.BarHeight = (item.ToplamCiro / maksimumCiro) * 180;
                if (item.BarHeight < 5) item.BarHeight = 5;
            }

            if (ListGunluk != null) ListGunluk.ItemsSource = gunler;
        }

        private void ListIstatistikMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridPopulerUrunlerPaneli == null || GridSaatlikAnalizPaneli == null || GridGunlukAnalizPaneli == null) return;

            GridPopulerUrunlerPaneli.Visibility = Visibility.Collapsed;
            GridSaatlikAnalizPaneli.Visibility = Visibility.Collapsed;
            GridGunlukAnalizPaneli.Visibility = Visibility.Collapsed;

            if (ListIstatistikMenu.SelectedIndex == 0) GridPopulerUrunlerPaneli.Visibility = Visibility.Visible;
            else if (ListIstatistikMenu.SelectedIndex == 1) GridSaatlikAnalizPaneli.Visibility = Visibility.Visible;
            else if (ListIstatistikMenu.SelectedIndex == 2) GridGunlukAnalizPaneli.Visibility = Visibility.Visible;
        }

        private void BtnAyarlarAc_Click(object sender, RoutedEventArgs e)
        {
            OverlayAyarlar.Visibility = Visibility.Visible;
        }

        private void BtnAyarlarKapat_Click(object sender, RoutedEventArgs e)
        {
            OverlayAyarlar.Visibility = Visibility.Collapsed;
            TxtHiddenInput.Focus();
        }

        private void BtnTamEkranToggle_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowStyle == WindowStyle.None)
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
                BtnTamEkranToggle.Content = "TAM EKRAN MODU: KAPALI";
                BtnTamEkranToggle.Background = new SolidColorBrush(Color.FromRgb(82, 82, 91));
            }
            else
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
                BtnTamEkranToggle.Content = "TAM EKRAN MODU: AÇIK";
                BtnTamEkranToggle.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
        }

        private void BtnSermayeAc_Click(object sender, RoutedEventArgs e)
        {
            var liste = new List<SermayeVerisi>();
            double toplamMaliyet = 0;
            double toplamSatis = 0;

            var gruplar = tumStokListesi.Where(u => u.Stok > 0).GroupBy(u => u.Kategori);

            foreach (var grup in gruplar)
            {
                double grupMaliyeti = grup.Sum(u => u.GelisFiyati * u.Stok);
                double grupSatisi = grup.Sum(u => u.Fiyat * u.Stok);

                liste.Add(new SermayeVerisi
                {
                    Kategori = grup.Key,
                    Maliyet = grupMaliyeti,
                    Satis = grupSatisi
                });

                toplamMaliyet += grupMaliyeti;
                toplamSatis += grupSatisi;
            }

            GridSermaye.ItemsSource = liste.OrderByDescending(x => x.Maliyet).ToList();
            TxtSermayeMaliyet.Text = toplamMaliyet.ToString("N2") + " ₺";
            TxtSermayeSatis.Text = toplamSatis.ToString("N2") + " ₺";
            OverlaySermaye.Visibility = Visibility.Visible;
        }

        private void BtnSermayeKapat_Click(object sender, RoutedEventArgs e)
        {
            OverlaySermaye.Visibility = Visibility.Collapsed;
            TxtHiddenInput.Focus();
        }

        private void BtnStokBildirimToggle_Click(object sender, RoutedEventArgs e)
        {
            isStokBildirimAktif = !isStokBildirimAktif;
            if (isStokBildirimAktif)
            {
                BtnStokBildirimToggle.Content = "STOK BİLDİRİMLERİ: AÇIK";
                BtnStokBildirimToggle.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else
            {
                BtnStokBildirimToggle.Content = "STOK BİLDİRİMLERİ: KAPALI";
                BtnStokBildirimToggle.Background = new SolidColorBrush(Color.FromRgb(82, 82, 91));
            }
            BildirimleriGuncelle();
        }

        private void BildirimleriGuncelle()
        {
            if (ListBildirimler == null || TxtBildirimDurum == null) return;

            if (!isStokBildirimAktif)
            {
                ListBildirimler.ItemsSource = null;
                TxtBildirimDurum.Text = "Stok takibi kapalı. Sayım zorunluluğu olmadan dükkanı çalıştırabilirsiniz.";
                TxtBildirimDurum.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                return;
            }

            var liste = new List<Bildirim>();
            using (var conn = new SqliteConnection("Data Source=stok.db"))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT Ad, Stok FROM Urunler WHERE Stok <= 5 ORDER BY Stok ASC", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        liste.Add(new Bildirim
                        {
                            Mesaj = $"{rdr.GetString(0)} ürünü kritik seviyede! (Kalan Stok: {rdr.GetInt32(1)})",
                            Renk = rdr.GetInt32(1) <= 0 ? "#EF4444" : "#F59E0B"
                        });
                    }
                }
            }

            ListBildirimler.ItemsSource = liste;

            if (liste.Count > 0)
            {
                TxtBildirimDurum.Text = $"{liste.Count} adet üründe stok uyarısı var!";
                TxtBildirimDurum.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else
            {
                TxtBildirimDurum.Text = "Tüm stoklar yeterli seviyede. Sorun yok.";
                TxtBildirimDurum.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
        }

        private void TarihAgaciniDoldur()
        {
            TreeTarihler.Items.Clear();
            using (var conn = new SqliteConnection("Data Source=ciro.db"))
            {
                conn.Open();
                var cmdYil = new SqliteCommand("SELECT DISTINCT strftime('%Y', Tarih) FROM Satislar ORDER BY Tarih DESC", conn);
                using (var rdrYil = cmdYil.ExecuteReader())
                {
                    while (rdrYil.Read())
                    {
                        string yil = rdrYil.GetString(0);
                        TreeViewItem yilNode = new TreeViewItem { Header = yil, Tag = "YIL-" + yil };

                        var cmdAy = new SqliteCommand($"SELECT DISTINCT strftime('%m', Tarih) FROM Satislar WHERE strftime('%Y', Tarih) = '{yil}'", conn);
                        using (var rdrAy = cmdAy.ExecuteReader())
                        {
                            while (rdrAy.Read())
                            {
                                string ay = rdrAy.GetString(0);
                                TreeViewItem ayNode = new TreeViewItem { Header = GetAyAdi(ay), Tag = $"AY-{yil}-{ay}" };

                                var cmdGun = new SqliteCommand($"SELECT DISTINCT strftime('%d', Tarih) FROM Satislar WHERE strftime('%Y-%m', Tarih) = '{yil}-{ay}'", conn);
                                using (var rdrGun = cmdGun.ExecuteReader())
                                {
                                    while (rdrGun.Read())
                                    {
                                        string gun = rdrGun.GetString(0);
                                        ayNode.Items.Add(new TreeViewItem { Header = gun + " " + GetAyAdi(ay), Tag = $"GUN-{yil}-{ay}-{gun}" });
                                    }
                                }
                                yilNode.Items.Add(ayNode);
                            }
                        }
                        TreeTarihler.Items.Add(yilNode);
                    }
                }
            }
        }

        private void TreeTarihler_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                string[] parcalar = item.Tag.ToString().Split('-');
                using (var conn = new SqliteConnection("Data Source=ciro.db"))
                {
                    conn.Open();
                    string filtre = "";
                    if (parcalar[0] == "YIL") filtre = $"strftime('%Y', Tarih) = '{parcalar[1]}'";
                    else if (parcalar[0] == "AY") filtre = $"strftime('%Y-%m', Tarih) = '{parcalar[1]}-{parcalar[2]}'";
                    else if (parcalar[0] == "GUN") filtre = $"strftime('%Y-%m-%d', Tarih) = '{parcalar[1]}-{parcalar[2]}-{parcalar[3]}'";

                    double ciro = GetSqlSum($"SELECT SUM(Fiyat) FROM Satislar WHERE {filtre}", conn);
                    double kar = GetSqlSum($"SELECT SUM(Kar) FROM Satislar WHERE {filtre}", conn);

                    TxtCiroSonuc.Text = ciro.ToString("N2") + " ₺";
                    TxtKarSonuc.Text = kar.ToString("N2") + " ₺";
                    TxtKarYuzde.Text = ciro > 0 ? "%" + ((kar / ciro) * 100).ToString("N1") : "%0.0";

                    TxtNakitDetay.Text = GetSqlSum($"SELECT SUM(Fiyat) FROM Satislar WHERE {filtre} AND OdemeTuru='NAKİT'", conn).ToString("N2") + " ₺";
                    TxtKartDetay.Text = GetSqlSum($"SELECT SUM(Fiyat) FROM Satislar WHERE {filtre} AND OdemeTuru='KART'", conn).ToString("N2") + " ₺";
                    TxtVeresiyeDetay.Text = GetSqlSum($"SELECT SUM(Fiyat) FROM Satislar WHERE {filtre} AND OdemeTuru='VERESİYE'", conn).ToString("N2") + " ₺";

                    TxtSeciliDonem.Text = item.Header.ToString();
                }
            }
        }

        private void BtnSigaraFark_Click(object sender, RoutedEventArgs e)
        {
            var urun = sepet.FirstOrDefault(x => x.Ad == "Sigara Farkı");
            if (urun != null) urun.Adet++;
            else sepet.Add(new SepetUrun { Ad = "Sigara Farkı", Fiyat = 3, Adet = 1 });
            UpdateCart();
            TxtHiddenInput.Focus();
        }

        private void UpdateCart()
        {
            ListSatis.ItemsSource = null;
            ListSatis.ItemsSource = sepet;
            double toplam = sepet.Sum(x => x.ToplamFiyat);
            TxtToplam.Text = "TOPLAM: " + toplam.ToString("N2") + " ₺";
            if (OverlayParcali.Visibility == Visibility.Visible)
            {
                TxtParcaliToplam.Text = "Toplam: " + toplam.ToString("N2") + " ₺";
            }
        }

        private void SatisKaydet(string tur)
        {
            if (sepet.Count == 0) return;

            using (var connStok = new SqliteConnection("Data Source=stok.db"))
            using (var connCiro = new SqliteConnection("Data Source=ciro.db"))
            {
                connStok.Open();
                connCiro.Open();

                foreach (var urun in sepet)
                {
                    for (int i = 0; i < urun.Adet; i++)
                    {
                        double kar = 0;
                        if (urun.Ad == "Sigara Farkı")
                        {
                            kar = tur == "KART" ? 3 * 0.975 : 3;
                        }
                        else
                        {
                            double gelis = GetGelisFiyati(connStok, urun.Ad);
                            double netSatis = tur == "KART" ? urun.Fiyat * 0.975 : urun.Fiyat;
                            kar = netSatis - gelis;
                        }
                        InsertSatisDB(connCiro, urun.Ad, urun.Fiyat, tur, kar);
                        if (urun.Ad != "Sigara Farkı") StokDus(connStok, urun.Ad);
                    }
                }
            }
            IslemiTamamla(tur + " Satışı Tamamlandı.");
        }

        private void BtnParcali_Click(object sender, RoutedEventArgs e)
        {
            if (sepet.Count == 0) return;
            TxtParcaliToplam.Text = "Toplam: " + sepet.Sum(x => x.ToplamFiyat).ToString("N2") + " ₺";
            InpParcaliNakit.Text = "";
            InpParcaliKart.Text = "";
            OverlayParcali.Visibility = Visibility.Visible;
            InpParcaliNakit.Focus();
        }

        private void BtnParcaliIptal_Click(object sender, RoutedEventArgs e)
        {
            OverlayParcali.Visibility = Visibility.Collapsed;
            TxtHiddenInput.Focus();
        }

        private void InpParcaliNakit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OverlayParcali.Visibility != Visibility.Visible || isParcaliHesaplaniyor) return;
            isParcaliHesaplaniyor = true;
            double toplam = sepet.Sum(x => x.ToplamFiyat);
            double nakit = 0;

            if (double.TryParse(InpParcaliNakit.Text.Replace(".", ","), out nakit))
            {
                if (nakit > toplam) nakit = toplam;
                InpParcaliKart.Text = (toplam - nakit).ToString("N2");
            }
            else
            {
                InpParcaliKart.Text = toplam.ToString("N2");
            }
            isParcaliHesaplaniyor = false;
        }

        private void InpParcaliKart_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OverlayParcali.Visibility != Visibility.Visible || isParcaliHesaplaniyor) return;
            isParcaliHesaplaniyor = true;
            double toplam = sepet.Sum(x => x.ToplamFiyat);
            double kart = 0;

            if (double.TryParse(InpParcaliKart.Text.Replace(".", ","), out kart))
            {
                if (kart > toplam) kart = toplam;
                InpParcaliNakit.Text = (toplam - kart).ToString("N2");
            }
            else
            {
                InpParcaliNakit.Text = toplam.ToString("N2");
            }
            isParcaliHesaplaniyor = false;
        }

        private void BtnParcaliOnayla_Click(object sender, RoutedEventArgs e)
        {
            double nakit = 0;
            double.TryParse(InpParcaliNakit.Text.Replace(".", ","), out nakit);
            SatisKaydetParcali(nakit);
            OverlayParcali.Visibility = Visibility.Collapsed;
        }

        private void SatisKaydetParcali(double alinanNakit)
        {
            double kalan = alinanNakit;
            using (var connStok = new SqliteConnection("Data Source=stok.db"))
            using (var connCiro = new SqliteConnection("Data Source=ciro.db"))
            {
                connStok.Open();
                connCiro.Open();

                var genisletilmisSepet = new List<SepetUrun>();
                foreach (var urun in sepet)
                {
                    for (int i = 0; i < urun.Adet; i++)
                    {
                        genisletilmisSepet.Add(new SepetUrun { Ad = urun.Ad, Fiyat = urun.Fiyat, Adet = 1 });
                    }
                }

                foreach (var urun in genisletilmisSepet)
                {
                    if (urun.Ad != "Sigara Farkı") StokDus(connStok, urun.Ad);
                    double gelisFiyati = GetGelisFiyati(connStok, urun.Ad);

                    if (kalan >= urun.Fiyat)
                    {
                        InsertSatisDB(connCiro, urun.Ad, urun.Fiyat, "NAKİT", urun.Fiyat - gelisFiyati);
                        kalan -= urun.Fiyat;
                    }
                    else if (kalan > 0)
                    {
                        double nakitKisim = kalan;
                        double kartKisim = urun.Fiyat - nakitKisim;

                        InsertSatisDB(connCiro, urun.Ad, nakitKisim, "NAKİT", nakitKisim - (gelisFiyati * (nakitKisim / urun.Fiyat)));
                        InsertSatisDB(connCiro, urun.Ad, kartKisim, "KART", (kartKisim * 0.975) - (gelisFiyati * (kartKisim / urun.Fiyat)));
                        kalan = 0;
                    }
                    else
                    {
                        InsertSatisDB(connCiro, urun.Ad, urun.Fiyat, "KART", (urun.Fiyat * 0.975) - gelisFiyati);
                    }
                }
            }
            IslemiTamamla("Parçalı Ödeme Tamamlandı.");
        }

        private double GetGelisFiyati(SqliteConnection conn, string urunAdi)
        {
            var cmd = new SqliteCommand($"SELECT GelisFiyati FROM Urunler WHERE Ad='{urunAdi.Replace("'", "''")}'", conn);
            var sonuc = cmd.ExecuteScalar();
            return sonuc != null && sonuc != DBNull.Value ? Convert.ToDouble(sonuc) : 0;
        }

        private void StokDus(SqliteConnection conn, string urunAdi)
        {
            var cmd = new SqliteCommand($"UPDATE Urunler SET Stok = Stok - 1 WHERE Ad = '{urunAdi.Replace("'", "''")}'", conn);
            cmd.ExecuteNonQuery();
        }

        private void InsertSatisDB(SqliteConnection conn, string ad, double fiyat, string tur, double kar)
        {
            var cmd = new SqliteCommand("INSERT INTO Satislar (Ad, Fiyat, Tarih, OdemeTuru, Kar) VALUES (@a, @f, @t, @o, @k)", conn);
            cmd.Parameters.AddWithValue("@a", ad);
            cmd.Parameters.AddWithValue("@f", fiyat);
            cmd.Parameters.AddWithValue("@t", DateTime.Now);
            cmd.Parameters.AddWithValue("@o", tur);
            cmd.Parameters.AddWithValue("@k", kar);
            cmd.ExecuteNonQuery();
        }

        private void IslemiTamamla(string mesaj)
        {
            sepet.Clear();
            UpdateCart();
            TarihAgaciniDoldur();
            UrunListele();
            BildirimleriGuncelle();
            MessageBox.Show(mesaj);
            TxtHiddenInput.Focus();
        }

        private double GetSqlSum(string sql, SqliteConnection conn)
        {
            var cmd = new SqliteCommand(sql, conn);
            var sonuc = cmd.ExecuteScalar();
            return sonuc != DBNull.Value && sonuc != null ? Convert.ToDouble(sonuc) : 0;
        }

        private string GetAyAdi(string ayNumarasi)
        {
            string[] aylar = { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            return aylar[int.Parse(ayNumarasi)];
        }

        private void BtnUrunSil_Click(object sender, RoutedEventArgs e)
        {
            if (ListSatis.SelectedItem is SepetUrun secilenUrun)
            {
                sepet.Remove(secilenUrun);
                UpdateCart();
            }
            TxtHiddenInput.Focus();
        }

        private void BtnSatisIptal_Click(object sender, RoutedEventArgs e)
        {
            sepet.Clear();
            UpdateCart();
            TxtSonUrun.Text = "Satış İptal Edildi";
            TxtHiddenInput.Focus();
        }

        private void BtnGunKapat_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TxtHiddenInput.Focus();

            if (MainTabs.SelectedIndex == 3)
            {
                PopulerUrunleriGuncelle();
                SaatlikAnalizGuncelle();
                GunlukAnalizGuncelle();
            }
            else if (MainTabs.SelectedIndex == 4)
            {
                BildirimleriGuncelle();
            }
            else if (MainTabs.SelectedIndex == 5)
            {
                NotlariListele();
            }
        }

        private void BtnNakit_Click(object sender, RoutedEventArgs e) => SatisKaydet("NAKİT");
        private void BtnKart_Click(object sender, RoutedEventArgs e) => SatisKaydet("KART");
        private void BtnVeresiye_Click(object sender, RoutedEventArgs e) => SatisKaydet("VERESİYE");
    }

    // MODELLER
    public class SepetUrun
    {
        public string Ad { get; set; }
        public double Fiyat { get; set; }
        public int Adet { get; set; } = 1;
        public double ToplamFiyat => Fiyat * Adet;
        public string ToplamFiyatStr => ToplamFiyat.ToString("N2") + " ₺";
    }

    public class StokBilgisi
    {
        public string Barkod { get; set; }
        public string Ad { get; set; }
        public double Fiyat { get; set; }
        public double GelisFiyati { get; set; }
        public int Stok { get; set; }
        public string Kategori { get; set; }
        public string AltKategori { get; set; }
    }

    public class AnalizVerisi
    {
        public string Ad { get; set; }
        public int Adet { get; set; }
        public double ToplamKar { get; set; }
        public string ToplamKarStr => ToplamKar.ToString("N2") + " ₺";
    }

    public class SaatlikAnaliz
    {
        public string Saat { get; set; }
        public double ToplamCiro { get; set; }
        public double BarHeight { get; set; }
        public string CiroStr => ToplamCiro.ToString("N2") + " ₺";
    }

    public class Bildirim
    {
        public string Mesaj { get; set; }
        public string Renk { get; set; }
    }

    public class GunlukAnaliz
    {
        public string GunGosterim { get; set; }
        public string GunIndex { get; set; }
        public double ToplamCiro { get; set; }
        public double BarHeight { get; set; }
        public string CiroStr => ToplamCiro.ToString("N2") + " ₺";
    }

    public class SermayeVerisi
    {
        public string Kategori { get; set; }
        public double Maliyet { get; set; }
        public double Satis { get; set; }
        public string MaliyetStr => Maliyet.ToString("N2") + " ₺";
        public string SatisStr => Satis.ToString("N2") + " ₺";
    }

    public class NotBilgisi
    {
        public int ID { get; set; }
        public string Metin { get; set; }
        public string Tarih { get; set; }
    }
}