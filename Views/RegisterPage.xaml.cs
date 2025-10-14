using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnLoginTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}
