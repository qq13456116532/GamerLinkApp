using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    public ProfilePage()
    {
        InitializeComponent();
    }

    private async void OnAllOrdersTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OrderListPage));
    }

    private async void OnFavoritesTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(FavoriteServicesPage));
    }
}
