using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;
using System.Net.Http;

namespace LowmiaApp;

public partial class HomePage : ContentPage
{
    private class SkinQuizItem
    {
        public string Text { get; set; }
        public bool IsTrue { get; set; }
        public string Explanation { get; set; }
    }

    private readonly SkinQuizItem[] _quizItems = new[]
    {
        new SkinQuizItem
        {
            Text = "Bol su içmek cildin esnekliğini artırır.",
            IsTrue = true,
            Explanation = "Yeterli su tüketimi cildin nem dengesini ve esnekliğini korur."
        },
        new SkinQuizItem
        {
            Text = "Güneş kremini sadece yazın, bulutlu ve kapalı havalarda sürmesek de olur.",
            IsTrue = false,
            Explanation = "Bulutlu havalarda bile UV ışınları bulutların %80'ini geçerek cildinize ulaşır. Güneş kremi dört mevsim şarttır!"
        },
        new SkinQuizItem
        {
            Text = "Cildinizi her akşam nemlendirerek uyumak hücre yenilenmesini destekler.",
            IsTrue = true,
            Explanation = "Gece boyunca cilt kendini yeniler ve nemlendirici bu süreci hızlandırır."
        },
        new SkinQuizItem
        {
            Text = "Cilt temizliği yaparken sıcak su yerine ılık su tercih etmek cildin bariyerini korur.",
            IsTrue = true,
            Explanation = "Sıcak su cildin doğal yağ dengesini bozarak kuruluk ve tahriş yaratır."
        },
        new SkinQuizItem
        {
            Text = "Düzenli uyku, cilt altındaki kan dolaşımını hızlandırarak doğal parlaklık kazandırır.",
            IsTrue = true,
            Explanation = "Kaliteli uyku cildin yenilenmesi ve canlı görünmesi için en temel unsurdur."
        },
        new SkinQuizItem
        {
            Text = "Makyajla uyumamak gözeneklerin tıkanmasını ve sivilce oluşumunu engeller.",
            IsTrue = true,
            Explanation = "Makyajla uyumak gözenekleri tıkar, nefes almasını engeller ve akneye yol açar."
        }
    };

    private SkinQuizItem _currentQuizItem;

    public HomePage()
    {
        InitializeComponent();
        LoadUserInfo();
        LoadDateAndRandomTip();
    }

    private void LoadUserInfo()
    {
        string email = Preferences.Get("lowmia_current_user", "Kullanıcı");
        if (!string.IsNullOrEmpty(email) && email.Contains("@"))
        {
            string namePart = email.Split('@')[0];
            if (namePart.Length > 0)
            {
                namePart = char.ToUpper(namePart[0]) + namePart.Substring(1);
            }
            UserNameLabel.Text = namePart;
        }
        else
        {
            UserNameLabel.Text = email;
        }
    }

    private void LoadDateAndRandomTip()
    {
        var culture = new CultureInfo("tr-TR");
        DateLabel.Text = DateTime.Now.ToString("dd MMMM dddd", culture);
        LoadNewRandomTip();
    }

    // ─── PROFİL FOTOĞRAFI ────────────────────────────────────────────
    private async void OnProfilePhotoTapped(object sender, EventArgs e)
    {
        string secim = await DisplayActionSheet("Profil Fotoğrafı", "Vazgeç", null, "📷 Kameradan Çek", "🖼 Galeriden Seç");

        try
        {
            FileResult photo = null;

            if (secim == "📷 Kameradan Çek")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                    photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else if (secim == "🖼 Galeriden Seç")
            {
                photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Profil fotoğrafı seçin"
                });
            }

            if (photo != null)
            {
                string localFile = Path.Combine(FileSystem.CacheDirectory, "profil_" + photo.FileName);
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.OpenWrite(localFile))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }

                Preferences.Set("lowmia_profil_fotografi", localFile);
                ShowProfilePhoto(localFile);

                await ProfilFotografiniSunucuyaYukle(localFile);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Fotoğraf ayarlanamadı: {ex.Message}", "Tamam");
        }
    }

    private void ShowProfilePhoto(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            ProfileImage.Source = ImageSource.FromFile(path);
            ProfileImage.IsVisible = true;
            ProfileEmojiLabel.IsVisible = false;
        }
    }

    private async Task ProfilFotografiniSunucuyaYukle(string localFile)
    {
        try
        {
            int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(localFile);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "photo", Path.GetFileName(localFile));
            content.Add(new StringContent(kullaniciId.ToString()), "kullaniciId");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using HttpClient client = new HttpClient(clientHandler);
            var response = await client.PostAsync("https://lowmia.gisa.com.tr/api/auth/profile-photo", content);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                string relativePath = doc.RootElement.TryGetProperty("profilFotografi", out var element) ? element.GetString() : null;

                if (!string.IsNullOrEmpty(relativePath))
                {
                    string fullUrl = relativePath.StartsWith("http") ? relativePath : "https://lowmia.gisa.com.tr" + relativePath;
                    Preferences.Set("lowmia_profil_foto_url", fullUrl);

                    // Yükleme biter bitmez resmi SSL kalkanını delerek çekiyoruz
                    await LoadProfileImageBypassingSsl(fullUrl);
                }
            }
        }
        catch (Exception)
        {
            // sessizce geç
        }
    }

    // --- İŞTE ANA SAYFADAKİ BEYAZ KALAN YUVARLAĞI ÇÖZEN METOT ---
    private async Task LoadProfileImageBypassingSsl(string url)
    {
        try
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;

            using HttpClient client = new HttpClient(handler);
            var imageBytes = await client.GetByteArrayAsync(url);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // İndirdiğimiz fotoğraf verisini (Stream) ekrana basıyoruz
                ProfileImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                ProfileImage.IsVisible = true;
                if (ProfileEmojiLabel != null) ProfileEmojiLabel.IsVisible = false;
            });
        }
        catch (Exception)
        {
            // Eğer indiremezse yerel yedeğe baksın
            string yerelProfilFoto = Preferences.Get("lowmia_profil_fotografi", string.Empty);
            if (!string.IsNullOrEmpty(yerelProfilFoto) && File.Exists(yerelProfilFoto))
            {
                MainThread.BeginInvokeOnMainThread(() => ShowProfilePhoto(yerelProfilFoto));
            }
        }
    }

    // ─── KAMERA / GALERİ İLE CİLT ANALİZİ ────────────────────────────
    private async void OnOpenCameraClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                string localFile = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.OpenWrite(localFile))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }

                LoadingOverlay.IsVisible = true; // YENİ: analiz başlarken göster
                var historyItem = await FotografiAnalizEt(localFile);
                LoadingOverlay.IsVisible = false; // YENİ: analiz bitince gizle

                if (historyItem != null)
                {
                    AnalysisData.LastResult = historyItem.Result;
                    UpdateMoistureCard(); // YENİ: analiz biter bitmez kartı güncelle
                    await Navigation.PushAsync(new AnalysisDetailPage(historyItem));
                }
            }
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Hata", $"Kamera açılamadı: {ex.Message}", "Tamam");
        }
    }

    private async void OnUploadFileClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Cilt fotoğrafı seçin"
            });

            if (photo != null)
            {
                string localFile = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.OpenWrite(localFile))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }

                LoadingOverlay.IsVisible = true; // YENİ: analiz başlarken göster
                var historyItem = await FotografiAnalizEt(localFile);
                LoadingOverlay.IsVisible = false; // YENİ: analiz bitince gizle

                if (historyItem != null)
                {
                    AnalysisData.LastResult = historyItem.Result;
                    UpdateMoistureCard(); // YENİ: analiz biter bitmez kartı güncelle
                    await Navigation.PushAsync(new AnalysisDetailPage(historyItem));
                }
            }
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Hata", $"Dosya seçilemedi: {ex.Message}", "Tamam");
        }
    }

    private async Task<HistoryItem?> FotografiAnalizEt(string imagePath)
    {
        try
        {
            int kullaniciId = Preferences.Get("lowmia_kullanici_id", 0);

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(imagePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "photo", Path.GetFileName(imagePath));
            content.Add(new StringContent(kullaniciId.ToString()), "kullaniciId");

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using HttpClient client = new HttpClient(clientHandler);
            client.Timeout = TimeSpan.FromSeconds(60);
            var response = await client.PostAsync("https://lowmia.gisa.com.tr/api/ai/analyze-skin", content);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new SkinAnalysisResult
                {
                    AknePuani = root.GetProperty("aknePuani").GetInt32(),
                    LekePuani = root.GetProperty("lekePuani").GetInt32(),
                    KirisiklikPuani = root.GetProperty("kirisiklikPuani").GetInt32(),
                    NemPuani = root.GetProperty("nemPuani").GetInt32(),
                    TahminiCiltYasi = root.GetProperty("tahminiCiltYasi").GetInt32(),
                    IyilesmeYuzdesi = root.GetProperty("iyilesmeYuzdesi").GetDouble(),
                    CiltTonuUndertone = root.TryGetProperty("ciltTonuUndertone", out var ct) ? ct.GetString() ?? "" : "",
                    YapayZekaYorumu = root.TryGetProperty("yapayZekaYorumu", out var yz) ? yz.GetString() ?? "" : ""
                };

                int analizId = root.TryGetProperty("analizId", out var idEl) ? idEl.GetInt32() : 0;

                string serverPhotoUrl = root.TryGetProperty("fotografYolu", out var fotoEl) ? fotoEl.GetString() : null;
                string fullImageUrl = string.IsNullOrEmpty(serverPhotoUrl) ? null :
                                      (serverPhotoUrl.StartsWith("http") ? serverPhotoUrl : "https://lowmia.gisa.com.tr" + serverPhotoUrl);

                return new HistoryItem
                {
                    Id = analizId,
                    DateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    Title = "\"Genel Cilt Analizi\"",
                    Result = result,
                    ImagePath = imagePath,
                    ImageUrl = fullImageUrl,
                    QnAList = new List<QnAItem>()
                };
            }
            else
            {
                await DisplayAlert("Hata", "Analiz servisinden yanıt alınamadı.", "Tamam");
                return null;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", $"Analiz yapılamadı: {ex.Message}", "Tamam");
            return null;
        }
    }

    private async void OnMoistureTapped(object sender, EventArgs e)
    {
        if (AnalysisData.LastResult != null)
        {
            await DisplayAlert("Nem Oranı", $"Cildinizin nem seviyesi son analizde %{AnalysisData.LastResult.NemPuani} olarak ölçüldü.", "Tamam");
        }
        else
        {
            await DisplayAlert("Nem Oranı", "Henüz bir analiz yapmadınız. Nem oranınızı görmek için bir fotoğraf analiz ettirin.", "Tamam");
        }
    }

    private async void OnUvTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert("UV İndeksi", "Bugün dışarı çıkarken mutlaka 50+ faktör güneş kremi kullanmanız önerilir.", "Tamam");
    }

    private async void OnTipTapped(object sender, TappedEventArgs e)
    {
        LoadNewRandomTip();
    }

    private async void OnTrueAnswerClicked(object sender, EventArgs e)
    {
        CheckAnswer(true);
    }

    private async void OnFalseAnswerClicked(object sender, EventArgs e)
    {
        CheckAnswer(false);
    }

    private async void CheckAnswer(bool userGuessedTrue)
    {
        if (_currentQuizItem == null) return;

        bool isCorrect = (userGuessedTrue == _currentQuizItem.IsTrue);

        string title = isCorrect ? "Tebrikler, Doğru Bildiniz! 🎉" : "Yanlış Bildiniz! ❌";
        string message = _currentQuizItem.Explanation;

        await DisplayAlert(title, message, "Tamam");
        LoadNewRandomTip();
    }

    private void LoadNewRandomTip()
    {
        int index = new Random().Next(_quizItems.Length);
        _currentQuizItem = _quizItems[index];
        TipLabel.Text = _currentQuizItem.Text;
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HistoryPage());
    }

    // YENİ: "Son Analiz: %68" sabit metniydi, artık gerçek veriyi gösteriyor
    private void UpdateMoistureCard()
    {
        if (AnalysisData.LastResult != null)
        {
            MoistureValueLabel.Text = $"Son Analiz: %{AnalysisData.LastResult.NemPuani}";
        }
        else
        {
            MoistureValueLabel.Text = "Henüz analiz yok";
        }
    }

    // ─── EKRAN HER AÇILDIĞINDA TETİKLENEN YER ────────────────────────────
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        string kullaniciAdi = Preferences.Get("lowmia_kullanici_adi", "Kullanıcı");
        UserNameLabel.Text = kullaniciAdi;

        string profilFotoUrl = Preferences.Get("lowmia_profil_foto_url", string.Empty);
        string yerelProfilFoto = Preferences.Get("lowmia_profil_fotografi", string.Empty);

        if (!string.IsNullOrEmpty(profilFotoUrl))
        {
            // Sayfa açılır açılmaz resmi arka planda Güvenliği aşarak indirip ekrana koyacak!
            await LoadProfileImageBypassingSsl(profilFotoUrl);
        }
        else if (!string.IsNullOrEmpty(yerelProfilFoto) && File.Exists(yerelProfilFoto))
        {
            ShowProfilePhoto(yerelProfilFoto);
        }

        // YENİ: Nem Oranı kartını gerçek son analiz verisiyle güncelle
        UpdateMoistureCard();
    }
}
