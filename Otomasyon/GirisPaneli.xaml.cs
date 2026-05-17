using System;
using System.Windows;
using System.Windows.Threading;

namespace Otomasyon
{
    public partial class GirisPaneli : Window
    {
        public GirisPaneli()
        {
            InitializeComponent();
            // Saati canlı tutmak için bir timer ekleyelim
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => TxtTarihSaat.Text = DateTime.Now.ToString("dd MMMM yyyy - HH:mm:ss");
            timer.Start();
        }

        private void BtnGunBaslat_Click(object sender, RoutedEventArgs e)
        {
            MainWindow anaEkran = new MainWindow();
            anaEkran.Show();
            this.Close(); // Giriş panelini kapat
        }

        private void BtnKapat_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}