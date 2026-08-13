namespace LowmiaApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Sayfa yönlendirmeleri için rotaları kaydediyoruz
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));

        // Hamburger menü başlığındaki kullanıcı adını yüklüyoruz
        LoadUserData();
    }

    private void LoadUserData()
    {
        // Hafızadaki kullanıcı adını alıp menü üst başlığına yazdırıyoruz
        string kullaniciAdi = Preferences.Get("lowmia_kullanici_adi", "Kullanıcı");
        if (FlyoutUserNameLabel != null)
        {
            FlyoutUserNameLabel.Text = kullaniciAdi;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Çıkış Yap", "Hesabınızdan çıkış yapmak istediğinize emin misiniz?", "Evet", "İptal");

        if (confirm)
        {
            // YENİ: Oturum verilerini TAM olarak temizle.
            // Önceden sadece 2 anahtar temizleniyordu; kullaniciId, ad, soyad ve
            // profil fotoğrafı gibi bilgiler telefonda kalıyor, bir sonraki hesap
            // (kayıt veya farklı bir giriş) bu eski verilerle karışıyordu.
            Preferences.Remove("lowmia_current_user");
            Preferences.Remove("lowmia_kullanici_adi");
            Preferences.Remove("lowmia_kullanici_id");
            Preferences.Remove("lowmia_ad");
            Preferences.Remove("lowmia_soyad");
            Preferences.Remove("lowmia_profil_foto_url");
            Preferences.Remove("lowmia_profil_fotografi");

            // Not: "lowmia_hatirlanan_mail" bilinçli olarak SİLİNMİYOR —
            // "Beni Hatırla" özelliği bu bilgiye bağlı, giriş ekranında
            // e-postanın otomatik doldurulmasını sağlıyor.

            // Kullanıcıyı güvenli bir şekilde giriş sayfasına yönlendir
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new NavigationPage(new MainPage());
            });
        }
    }
}
