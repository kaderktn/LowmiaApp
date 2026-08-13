using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class RegisterPage : ContentPage
{
    private readonly string _baseUrl = "https://lowmia.gisa.com.tr/api/auth";

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string ad = NameEntry.Text?.Trim() ?? string.Empty;
        string soyad = SurnameEntry.Text?.Trim() ?? string.Empty;
        string eposta = EmailEntry.Text?.Trim() ?? string.Empty;
        string sifre = PasswordEntry.Text?.Trim() ?? string.Empty;
        string sifreTekrar = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(ad) || string.IsNullOrEmpty(soyad) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        if (sifre != sifreTekrar)
        {
            await DisplayAlert("Hata", "Şifreler birbiriyle uyuşmuyor.", "Tamam");
            return;
        }

        var registerData = new
        {
            Ad = ad,
            Soyad = soyad,
            Eposta = eposta,
            Sifre = sifre
        };

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            string json = JsonSerializer.Serialize(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using HttpClient client = new HttpClient(clientHandler);

            HttpResponseMessage response = await client.PostAsync($"{_baseUrl}/register", content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                int yeniKullaniciId = 0;
                if (responseObj.TryGetProperty("kullaniciId", out JsonElement idElement))
                {
                    yeniKullaniciId = idElement.GetInt32();
                }

                Preferences.Remove("lowmia_kullanici_id");
                Preferences.Remove("lowmia_ad");
                Preferences.Remove("lowmia_soyad");
                Preferences.Remove("lowmia_profil_foto_url");
                Preferences.Remove("lowmia_profil_fotografi");

                Preferences.Set("lowmia_current_user", eposta);
                Preferences.Set("lowmia_kullanici_adi", $"{ad} {soyad}");
                Preferences.Set("lowmia_kullanici_id", yeniKullaniciId);
                Preferences.Set("lowmia_ad", ad);
                Preferences.Set("lowmia_soyad", soyad);
                Preferences.Set($"lowmia_onboarding_{eposta}", false);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new OnboardingPage();
                });
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Kayıt Reddedildi", $"Sunucudan gelen cevap:\n{responseContent}", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", $"Sunucuya ulaşılamadı: {ex.Message}", "Tamam");
        }
        finally
        {
            // YENİ: İstek başarılı da olsa, hata da verse, sonunda mutlaka gizle
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // YENİ: Şifre alanındaki göz ikonuna tıklayınca göster/gizle
    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordEyeIcon.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
    }

    // YENİ: Şifre tekrar alanındaki göz ikonuna tıklayınca göster/gizle
    private void OnToggleConfirmPasswordVisibility(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ConfirmPasswordEyeIcon.Text = ConfirmPasswordEntry.IsPassword ? "👁" : "🙈";
    }
}
