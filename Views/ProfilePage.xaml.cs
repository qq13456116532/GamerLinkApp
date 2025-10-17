using GamerLinkApp.Helpers;
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
        : this(ServiceHelper.GetRequiredService<ProfileViewModel>())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }
    }

    private async void OnAllOrdersTapped(object sender, TappedEventArgs e)
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(OrderListPage));
    }

    private async void OnFavoritesTapped(object sender, TappedEventArgs e)
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(FavoriteServicesPage));
    }

    private async void OnSupportTapped(object sender, TappedEventArgs e)
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(SupportChatPage));
    }
}
