using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly string _baseUrl = "https://lowmia.gisa.com.tr/api/auth";

    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnSendLinkClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            await DisplayAlert("Uyarı", "Lütfen kayıtlı e-posta adresinizi girin.", "Tamam");
            return;
        }

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            var data = new { Eposta = email };
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using HttpClient client = new HttpClient(clientHandler);
            var response = await client.PostAsync($"{_baseUrl}/forgot-password", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Başarılı", "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi. Lütfen gelen kutunuzu (ve spam klasörünü) kontrol edin.", "Tamam");
                await Navigation.PopModalAsync();
            }
            else
            {
                string errJson = await response.Content.ReadAsStringAsync();
                string mesaj = "Bu e-posta adresine ait bir hesap bulunamadı.";
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

    private async void OnBackToLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
