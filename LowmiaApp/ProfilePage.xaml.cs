using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        LoadCurrentUserData();
    }

    private void LoadCurrentUserData()
    {
        NameEntry.Text = Preferences.Get("lowmia_ad", "");
        SurnameEntry.Text = Preferences.Get("lowmia_soyad", "");
        EmailEntry.Text = Preferences.Get("lowmia_current_user", "");
    }

    private async void OnUpdateProfileClicked(object sender, EventArgs e)
    {
        int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);
        var data = new
        {
            KullaniciId = kullaniciId,
            Ad = NameEntry.Text?.Trim(),
            Soyad = SurnameEntry.Text?.Trim(),
            Eposta = EmailEntry.Text?.Trim()
        };

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            string url = "https://lowmia.gisa.com.tr/api/auth/update-profile";
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (s, c, chain, errors) => true;

            using HttpClient client = new HttpClient(handler);
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Preferences.Set("lowmia_ad", data.Ad);
                Preferences.Set("lowmia_soyad", data.Soyad);
                Preferences.Set("lowmia_current_user", data.Eposta);

                await DisplayAlert("Başarılı", "Profil bilgileriniz güncellendi.", "Tamam");
            }
            else
            {
                await DisplayAlert("Hata", "Güncellenemedi. E-posta adresi kullanımda olabilir.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", ex.Message, "Tamam");
        }
        finally
        {
            // YENİ: İşlem bitince (başarılı/başarısız fark etmeden) gizle
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);
        string eskiSifre = OldPasswordEntry.Text;
        string yeniSifre = NewPasswordEntry.Text;

        if (string.IsNullOrEmpty(eskiSifre) || string.IsNullOrEmpty(yeniSifre))
        {
            await DisplayAlert("Uyarı", "Lütfen tüm şifre alanlarını doldurun.", "Tamam");
            return;
        }

        var data = new
        {
            KullaniciId = kullaniciId,
            EskiSifre = eskiSifre,
            YeniSifre = yeniSifre
        };

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            string url = "https://lowmia.gisa.com.tr/api/auth/change-password";
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (s, c, chain, errors) => true;

            using HttpClient client = new HttpClient(handler);
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Başarılı", "Şifreniz başarıyla değiştirildi.", "Tamam");
                OldPasswordEntry.Text = string.Empty;
                NewPasswordEntry.Text = string.Empty;
            }
            else
            {
                await DisplayAlert("Hata", "Mevcut şifre hatalı veya işlem başarısız.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", ex.Message, "Tamam");
        }
        finally
        {
            // YENİ: İşlem bitince (başarılı/başarısız fark etmeden) gizle
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        try
        {
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Geri dönülemedi: {ex.Message}", "Tamam");
        }
    }
}
