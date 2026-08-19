using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace LowmiaApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    // YENİ: "lowmiaapp://resetpassword?email=..." bağlantılarını bu Activity'nin yakalamasını sağlar
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "lowmiaapp",
        DataHost = "resetpassword")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Status bar krem renk zeminde saat/pil/sinyal ikonlarının okunur olması için
            // status bar ikonlarını koyu (dark) yapıyoruz.
            SetLightStatusBarIcons();

            // Uygulama kapalıyken mail linkine tıklanıp açıldıysa
            HandleDeepLink(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            // Uygulama zaten açıkken (arka planda) mail linkine tıklanırsa
            HandleDeepLink(intent);
        }

        private void SetLightStatusBarIcons()
        {
            if (Window == null) return;

            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#FCF5E3"));

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.SetSystemBarsAppearance(
                        (int)WindowInsetsControllerAppearance.LightStatusBars,
                        (int)WindowInsetsControllerAppearance.LightStatusBars);
                }
            }
            else
            {
#pragma warning disable CA1422
                var decorView = Window.DecorView;
                decorView.SystemUiFlags = decorView.SystemUiFlags | SystemUiFlags.LightStatusBar;
#pragma warning restore CA1422
            }
        }

        private void HandleDeepLink(Intent? intent)
        {
            if (intent == null) return;
            if (intent.Action != Intent.ActionView) return;

            Android.Net.Uri? uri = intent.Data;
            if (uri == null) return;

            // Beklenen biçim: lowmiaapp://resetpassword?email=kullanici%40mail.com
            string? email = uri.GetQueryParameter("email");
            if (string.IsNullOrEmpty(email)) return;

            if (Microsoft.Maui.Controls.Application.Current is App app)
            {
                app.NavigateToResetPassword(email);
            }
        }
    }
}
