using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnRegisterTapped(object sender, TappedEventArgs e)
    {
        var registerPage = ServiceHelper.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}
