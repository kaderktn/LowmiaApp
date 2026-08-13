namespace LowmiaApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Uygulama ilk açıldığında giriş ekranından başlayacak
        MainPage = new MainPage();
    }

    // YENİ: Mail'deki "lowmiaapp://resetpassword?email=..." bağlantısına
    // tıklanınca MainActivity buradan çağırır ve uygulamayı doğrudan
    // ResetPasswordPage'e yönlendiririz.
    public void NavigateToResetPassword(string email)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage = new NavigationPage(new ResetPasswordPage(email));
        });
    }
}
