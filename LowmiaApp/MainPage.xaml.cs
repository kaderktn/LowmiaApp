using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class MainPage : ContentPage
{
    // Emülatörün bilgisayarına bağlanabilmesi için doğru adres (10.0.2.2) ayarlandı
    private readonly string _baseUrl = "https://lowmia.gisa.com.tr/api/auth";

    public MainPage()
    {
        InitializeComponent();
        LoadRememberedEmail();
    }

    private void LoadRememberedEmail()
    {
        if (Preferences.ContainsKey("lowmia_hatirlanan_mail"))
        {
            EmailEntry.Text = Preferences.Get("lowmia_hatirlanan_mail", string.Empty);
            RememberMeCheckbox.IsChecked = true;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        // Yüklenme ekranını aç
        LoadingOverlay.IsVisible = true;

        try
        {
            // API modelinle birebir aynı isimler (Eposta ve Sifre)
            var loginData = new { Eposta = email, Sifre = password };

            string json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using HttpClient client = new HttpClient(clientHandler);
            HttpResponseMessage response = await client.PostAsync($"{_baseUrl}/login", content);

            if (response.IsSuccessStatusCode)
            {
                // API'den dönen JSON verisini okuyup Ad-Soyad bilgisini alıyoruz
                string responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                string gelenAdSoyad = "Kullanıcı"; // Varsayılan değer
                if (responseObj.TryGetProperty("adSoyad", out JsonElement adSoyadElement))
                {
                    gelenAdSoyad = adSoyadElement.GetString();
                }

                // --- YENİ EKLENEN KISIM: Ad ve Soyadı ayrı ayrı alıyoruz ---
                string gelenAd = string.Empty;
                if (responseObj.TryGetProperty("ad", out JsonElement adElement) && adElement.ValueKind == JsonValueKind.String)
                {
                    gelenAd = adElement.GetString();
                }

                string gelenSoyad = string.Empty;
                if (responseObj.TryGetProperty("soyad", out JsonElement soyadElement) && soyadElement.ValueKind == JsonValueKind.String)
                {
                    gelenSoyad = soyadElement.GetString();
                }
                // -------------------------------------------------------------

                // Kullanıcı ID'sini de alıyoruz (analiz kaydı ve fotoğraf analizi için gerekli)
                int gelenKullaniciId = 0;
                if (responseObj.TryGetProperty("kullaniciId", out JsonElement idElement))
                {
                    gelenKullaniciId = idElement.GetInt32();
                }

                // Profil fotoğrafı URL'ini de alıyoruz (varsa)
                string gelenProfilFoto = string.Empty;
                if (responseObj.TryGetProperty("profilFotografi", out JsonElement fotoElement) && fotoElement.ValueKind == JsonValueKind.String)
                {
                    gelenProfilFoto = fotoElement.GetString();
                }

                // MainPage.xaml.cs içerisindeki if (response.IsSuccessStatusCode) bloğunun son kısmı:

                Preferences.Set("lowmia_current_user", email);
                Preferences.Set("lowmia_kullanici_adi", gelenAdSoyad);
                Preferences.Set("lowmia_kullanici_id", gelenKullaniciId);

                // --- YENİ EKLENEN KISIM: Ad ve Soyadı telefonun hafızasına kaydediyoruz ---
                Preferences.Set("lowmia_ad", gelenAd);
                Preferences.Set("lowmia_soyad", gelenSoyad);
                // -------------------------------------------------------------------------

                if (!string.IsNullOrEmpty(gelenProfilFoto))
                {
                    Preferences.Set("lowmia_profil_foto_url", "https://lowmia.gisa.com.tr" + gelenProfilFoto);
                }
                else
                {
                    Preferences.Remove("lowmia_profil_foto_url");
                }

                if (RememberMeCheckbox.IsChecked)
                {
                    Preferences.Set("lowmia_hatirlanan_mail", email);
                }
                else
                {
                    Preferences.Remove("lowmia_hatirlanan_mail");
                }

                LoadingOverlay.IsVisible = false;
                await Task.Delay(50);

                // KULLANICI DAHA ÖNCE ONBOARDING'İ YAPMIŞ MI KONTROL EDELİM:
                bool onboardingTamamlandi = Preferences.Get($"lowmia_onboarding_{email}", false);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (onboardingTamamlandi)
                    {
                        // Daha önce doldurmuş, direkt ana uygulamaya git
                        Application.Current.MainPage = new AppShell();
                    }
                    else
                    {
                        // İlk defa giriyor veya anketi atlamış, onboarding'i göster
                        Application.Current.MainPage = new OnboardingPage();
                    }
                });
                return;
            }
            else
            {
                LoadingOverlay.IsVisible = false;

                // YENİ: Sunucudan dönen gerçek hata mesajını okuyup gösteriyoruz
                // (örn. "kalan hakkınız X", "hesabınız bloke edildi" gibi özel mesajlar)
                string hataMesaji = "E-posta veya şifre hatalı!";
                try
                {
                    string errContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(errContent);
                    if (doc.RootElement.TryGetProperty("message", out JsonElement msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    {
                        hataMesaji = msgEl.GetString() ?? hataMesaji;
                    }
                }
                catch
                {
                    // JSON parse edilemezse varsayılan mesaj kullanılır
                }

                await DisplayAlert("Hata", hataMesaji, "Tamam");
            }
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Bağlantı Hatası", $"Sunucuya ulaşılamadı: {ex.Message}", "Tamam");
        }
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ForgotPasswordPage());
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPage());
    }

    // YENİ: Göz ikonuna tıklayınca şifreyi göster/gizle
    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordEyeIcon.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
    }
}
