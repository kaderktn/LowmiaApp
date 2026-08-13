using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace LowmiaApp;

// ======================================================================
// VERİ MODELLERİ
// ======================================================================
public static class AnalysisData
{
    public static string? ImagePath { get; set; }
    public static string? ImageUrl { get; set; }
    public static string? AnalysisDateText { get; set; }
    public static List<QnAItem> QnAList { get; set; } = new();
    public static SkinAnalysisResult? LastResult { get; set; }

    public static ObservableCollection<HistoryItem> HistoryList { get; set; } = new();
}

public class QnAItem
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class SkinAnalysisResult
{
    public int AknePuani { get; set; }
    public int LekePuani { get; set; }
    public int KirisiklikPuani { get; set; }
    public int NemPuani { get; set; }
    public int TahminiCiltYasi { get; set; }
    public double IyilesmeYuzdesi { get; set; }
    public string CiltTonuUndertone { get; set; } = string.Empty;
    public string YapayZekaYorumu { get; set; } = string.Empty;
}

public class ApiChatDto
{
    public string? Soru { get; set; }
    public string? Cevap { get; set; }
}

public class ApiAnalysisDto
{
    public int AnalizId { get; set; }
    public string? KullaniciNotu { get; set; }
    public string? YapayZekaYorumu { get; set; }
    public int AknePuani { get; set; }
    public int LekePuani { get; set; }
    public int NemPuani { get; set; }
    public int KirisiklikPuani { get; set; }
    public int TahminiCiltYasi { get; set; }
    public double IyilesmeYuzdesi { get; set; }
    public string? CiltTonuUndertone { get; set; }

    public string? FotografYolu { get; set; }
    public string? ImageUrl { get; set; }

    public string? AnalizTarihi { get; set; }
    public List<ApiChatDto>? ChatMesajlari { get; set; }
}

// ======================================================================
// YENİ GEÇMİŞ SAYFASI KART MODELİ (Listede görünecek veriler)
// ======================================================================
public class HistoryItem
{
    public int Id { get; set; }
    public string DateText { get; set; }
    public string Title { get; set; }

    public SkinAnalysisResult Result { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public List<QnAItem> QnAList { get; set; } = new();
}

// ======================================================================
// GEÇMİŞ SAYFASI (LİSTE EKRANI) İŞLEMLERİ
// ======================================================================
public partial class HistoryPage : ContentPage
{
    public ObservableCollection<HistoryItem> HistoryList => AnalysisData.HistoryList;

    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadHistoryData();
    }

    private async void LoadHistoryData()
    {
        // YENİ: Liste sunucudan çekilirken yükleme ekranını gösteriyoruz
        LoadingOverlay.IsVisible = true;

        try
        {
            int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using HttpClient client = new HttpClient(clientHandler);
            var response = await client.GetAsync($"https://lowmia.gisa.com.tr/api/auth/analizler/{kullaniciId}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var raw = JsonSerializer.Deserialize<List<ApiAnalysisDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ApiAnalysisDto>();

                HistoryList.Clear();
                foreach (var x in raw)
                {
                    string garantiliFoto = !string.IsNullOrEmpty(x.ImageUrl) ? x.ImageUrl : x.FotografYolu;

                    string tamFotoLink = string.IsNullOrEmpty(garantiliFoto) ? null :
                                         (garantiliFoto.StartsWith("http") ? garantiliFoto : "https://lowmia.gisa.com.tr" + garantiliFoto);

                    var historyItem = new HistoryItem
                    {
                        Id = x.AnalizId,
                        DateText = x.AnalizTarihi ?? string.Empty,
                        Title = string.IsNullOrEmpty(x.KullaniciNotu) ? "\"Genel Cilt Analizi\"" : $"\"{x.KullaniciNotu}\"",

                        Result = new SkinAnalysisResult
                        {
                            AknePuani = x.AknePuani,
                            LekePuani = x.LekePuani,
                            KirisiklikPuani = x.KirisiklikPuani,
                            NemPuani = x.NemPuani,
                            TahminiCiltYasi = x.TahminiCiltYasi,
                            IyilesmeYuzdesi = x.IyilesmeYuzdesi,
                            CiltTonuUndertone = x.CiltTonuUndertone ?? string.Empty,
                            YapayZekaYorumu = x.YapayZekaYorumu ?? string.Empty
                        },

                        ImageUrl = tamFotoLink
                    };

                    if (x.ChatMesajlari != null)
                    {
                        foreach (var msg in x.ChatMesajlari)
                        {
                            historyItem.QnAList.Add(new QnAItem
                            {
                                Question = msg.Soru ?? string.Empty,
                                Answer = msg.Cevap ?? string.Empty
                            });
                        }
                    }

                    HistoryList.Add(historyItem);
                }
            }
            else
            {
                await DisplayAlert("Hata", "Geçmiş analizler yüklenemedi.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", $"Sunucuya ulaşılamadı: {ex.Message}", "Tamam");
        }
        finally
        {
            // YENİ: Liste yüklenmesi bitince (başarılı/başarısız fark etmeden) gizle
            LoadingOverlay.IsVisible = false;
        }

        HistoryCollectionView.ItemsSource = HistoryList;
    }

    private async void OnCardTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var clickedItem = border?.BindingContext as HistoryItem;

        if (clickedItem == null) return;

        await Navigation.PushAsync(new AnalysisDetailPage(clickedItem));
    }

    private async void OnThreeDotsTapped(object sender, TappedEventArgs e)
    {
        var label = sender as Label;
        var clickedItem = label?.BindingContext as HistoryItem;

        if (clickedItem == null) return;

        string action = await DisplayActionSheet("Seçenekler", "İptal", "Sil");

        if (action == "Sil")
        {
            bool confirm = await DisplayAlert("Sil",
                $"{clickedItem.DateText} tarihli analizi silmek istediğinize emin misiniz?",
                "Evet, Sil", "Vazgeç");

            if (confirm)
            {
                HistoryList.Remove(clickedItem);
            }
        }
    }

    private void OnMenuTapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
