using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls.Shapes;
using System.IO;
using System.Net.Http;

namespace LowmiaApp;

public partial class AnalysisDetailPage : ContentPage
{
    private const int MaxQuestions = 3;
    private HistoryItem _currentItem;

    public AnalysisDetailPage(HistoryItem item)
    {
        InitializeComponent();
        _currentItem = item;
        LoadSpecificAnalysisData();
        RenderQnAs();
    }

    public AnalysisDetailPage()
    {
        InitializeComponent();
    }

    private async void LoadSpecificAnalysisData()
    {
        if (_currentItem == null) return;

        // YENİ: Detay verisi/fotoğraf yüklenirken göster
        LoadingOverlay.IsVisible = true;

        try
        {
            DateLabel.Text = $"📅 {_currentItem.DateText}";

            var result = _currentItem.Result;
            if (result != null)
            {
                SkinAgeLabel.Text = $"Cilt Yaşı: {result.TahminiCiltYasi}";
                UndertoneLabel.Text = $"Undertone: {result.CiltTonuUndertone}";
                ImprovementLabel.Text = $"📈 Gelişim: +%{result.IyilesmeYuzdesi:0.0}";

                AcneScoreLabel.Text = $"{result.AknePuani}/100";
                AcneProgressBar.Progress = result.AknePuani / 100.0;

                SpotScoreLabel.Text = $"{result.LekePuani}/100";
                SpotProgressBar.Progress = result.LekePuani / 100.0;

                MoistureScoreLabel.Text = $"{result.NemPuani}/100";
                MoistureProgressBar.Progress = result.NemPuani / 100.0;

                WrinkleScoreLabel.Text = $"{result.KirisiklikPuani}/100";
                WrinkleProgressBar.Progress = result.KirisiklikPuani / 100.0;

                CommentLabel.Text = result.YapayZekaYorumu;
            }

            UpdateQuestionLimitUI();

            string netUrl = _currentItem.ImageUrl;

            if (!string.IsNullOrEmpty(netUrl))
            {
                await LoadImageFromUrlBypassingSsl(netUrl);
            }
            else if (!string.IsNullOrEmpty(_currentItem.ImagePath) && File.Exists(_currentItem.ImagePath))
            {
                AnalyzedImage.Source = ImageSource.FromFile(_currentItem.ImagePath);
            }
        }
        finally
        {
            // YENİ: Yükleme bitince (hata olsa bile) gizle
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task LoadImageFromUrlBypassingSsl(string url)
    {
        try
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;

            using HttpClient client = new HttpClient(handler);
            var imageBytes = await client.GetByteArrayAsync(url);

            AnalyzedImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
        catch (Exception)
        {
            // Link bozuksa veya internet giderse uygulama çökmesin
        }
    }

    private void UpdateQuestionLimitUI()
    {
        int remaining = MaxQuestions - (_currentItem?.QnAList.Count ?? 0);
        QuestionLimitLabel.Text = $"💬 Bu Rapor Hakkında Uzmana Soru Sor (Kalan Hak: {remaining})";

        if (remaining <= 0)
        {
            QuestionInputContainer.IsVisible = false;
        }
    }

    private void RenderQnAs()
    {
        QnALayout.Children.Clear();
        if (_currentItem == null) return;

        foreach (var item in _currentItem.QnAList)
        {
            var qBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#D4AF37"),
                Padding = new Thickness(12, 8),
                HorizontalOptions = LayoutOptions.End,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };
            qBorder.Content = new Label { Text = item.Question, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            QnALayout.Children.Add(qBorder);

            var aBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#FFF9E6"),
                Stroke = Color.FromArgb("#E5E2DA"),
                StrokeThickness = 1,
                Padding = 12,
                HorizontalOptions = LayoutOptions.Fill,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };

            var stack = new VerticalStackLayout { Spacing = 4 };
            stack.Children.Add(new Label { Text = "✨ Lowmia Uzmanı", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#B8860B") });
            stack.Children.Add(new Label { Text = item.Answer, FontSize = 11, TextColor = Color.FromArgb("#4B5563") });
            aBorder.Content = stack;

            QnALayout.Children.Add(aBorder);
        }
    }

    private async void OnSendQuestionClicked(object sender, EventArgs e)
    {
        if (_currentItem == null) return;

        string question = QuestionEntry.Text?.Trim();
        if (string.IsNullOrEmpty(question))
        {
            await DisplayAlert("Uyarı", "Lütfen bir soru yazın.", "Tamam");
            return;
        }

        if (_currentItem.QnAList.Count >= MaxQuestions)
        {
            await DisplayAlert("Sınır Aşıldı", "Bu rapor için maksimum 3 soru sorma hakkınızı doldurdunuz.", "Tamam");
            return;
        }

        string url = "https://lowmia.gisa.com.tr/api/ai/ask";

        // YENİ: Soru gönderilirken göster
        LoadingOverlay.IsVisible = true;

        try
        {
            int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);

            var data = new
            {
                Question = question,
                KullaniciId = kullaniciId,
                AnalizId = _currentItem.Id
            };

            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using HttpClient client = new HttpClient(clientHandler);
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string resultJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resultJson);
                string answer = doc.RootElement.GetProperty("answer").GetString();

                _currentItem.QnAList.Add(new QnAItem { Question = question, Answer = answer });
                QuestionEntry.Text = string.Empty;

                RenderQnAs();
                UpdateQuestionLimitUI();
            }
            else
            {
                await DisplayAlert("Hata", "Yapay zeka servisinden yanıt alınamadı.", "Tamam");
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

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
