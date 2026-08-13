using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class ResetPasswordPage : ContentPage
{
    private readonly string _baseUrl = "https://lowmia.gisa.com.tr/api/auth";
    private readonly string _email;

    public ResetPasswordPage(string email)
    {
        InitializeComponent();
        _email = email;
    }

    public ResetPasswordPage() : this(string.Empty)
    {
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_email))
        {
            await DisplayAlert("Hata", "Geçersiz bağlantı. Lütfen 'Şifremi Unuttum' işlemini tekrar başlatın.", "Tamam");
            return;
        }

        string yeniSifre = NewPasswordEntry.Text?.Trim() ?? string.Empty;
        string tekrar = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < 6)
        {
            await DisplayAlert("Uyarı", "Şifre en az 6 karakter olmalıdır.", "Tamam");
            return;
        }

        if (yeniSifre != tekrar)
        {
            await DisplayAlert("Uyarı", "Şifreler eşleşmiyor.", "Tamam");
            return;
        }

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            var data = new { Eposta = _email, YeniSifre = yeniSifre };
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using HttpClient client = new HttpClient(clientHandler);
            var response = await client.PostAsync($"{_baseUrl}/reset-password", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Başarılı", "Şifreniz başarıyla güncellendi!", "Tamam");
                GoToLogin();
            }
            else
            {
                string errJson = await response.Content.ReadAsStringAsync();
                string mesaj = "Şifre güncellenemedi.";
                try
                {
                    using var doc = JsonDocument.Parse(errJson);
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                        mesaj = msgEl.GetString() ?? mesaj;
                }
                catch { /* varsayılan mesaj kalsın */ }

                await DisplayAlert("Hata", mesaj, "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", $"Sunucuya ulaşılamadı: {ex.Message}", "Tamam");
        }
        finally
        {
            // YENİ: İşlem bitince (başarılı/başarısız fark etmeden) gizle
            LoadingOverlay.IsVisible = false;
        }
    }

    private void OnBackToLoginTapped(object sender, EventArgs e)
    {
        GoToLogin();
    }

    private void OnToggleNewPasswordVisibility(object sender, EventArgs e)
    {
        NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;
        NewPasswordEyeIcon.Text = NewPasswordEntry.IsPassword ? "👁" : "🙈";
    }

    private void OnToggleConfirmPasswordVisibility(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ConfirmPasswordEyeIcon.Text = ConfirmPasswordEntry.IsPassword ? "👁" : "🙈";
    }

    private void GoToLogin()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new NavigationPage(new MainPage());
        });
    }
}
