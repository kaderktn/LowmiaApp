using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

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

            // Uygulama kapalıyken mail linkine tıklanıp açıldıysa
            HandleDeepLink(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            // Uygulama zaten açıkken (arka planda) mail linkine tıklanırsa
            HandleDeepLink(intent);
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
