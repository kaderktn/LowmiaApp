using System.Text;
using System.Text.Json;

namespace LowmiaApp;

public partial class OnboardingPage : ContentPage
{
    private readonly string _baseUrl = "https://lowmia.gisa.com.tr/api";

    public OnboardingPage()
    {
        InitializeComponent();
    }

    private async void OnSaveProfileClicked(object sender, EventArgs e)
    {
        string currentUser = Preferences.Get("lowmia_current_user", string.Empty);

        string yasAraligi = AgePicker.SelectedItem?.ToString() ?? string.Empty;
        string ciltTipi = SkinTypePicker.SelectedItem?.ToString() ?? string.Empty;
        string ciltEndisesi = SkinConcernPicker.SelectedItem?.ToString() ?? string.Empty;
        string bakimRutini = RoutinePicker.SelectedItem?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(yasAraligi) || string.IsNullOrEmpty(ciltTipi))
        {
            await DisplayAlert("Uyarı", "Lütfen en azından yaş ve cilt tipi seçimini yapın.", "Tamam");
            return;
        }

        // YENİ: İstek başlamadan önce yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            var profileData = new
            {
                Eposta = currentUser,
                YasAraligi = yasAraligi,
                CiltTipi = ciltTipi,
                CiltEndisesi = ciltEndisesi,
                BakimRutini = bakimRutini
            };

            string json = JsonSerializer.Serialize(profileData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using HttpClient client = new HttpClient(clientHandler);

            HttpResponseMessage response = await client.PostAsync($"{_baseUrl}/auth/save-onboarding", content);

            if (response.IsSuccessStatusCode)
            {
                Preferences.Set($"lowmia_onboarding_{currentUser}", true);

                await DisplayAlert("Başarılı", "Profiliniz kaydedildi!", "Tamam");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new AppShell();
                });
            }
            else
            {
                await DisplayAlert("Hata", "Bilgiler kaydedilirken bir hata oluştu.", "Tamam");
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

    private async void OnSkipTapped(object sender, EventArgs e)
    {
        string currentUser = Preferences.Get("lowmia_current_user", string.Empty);
        Preferences.Set($"lowmia_onboarding_{currentUser}", true);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new AppShell();
        });
    }
}
