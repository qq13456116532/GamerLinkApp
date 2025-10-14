using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class FavoriteServicesPage : ContentPage
{
    private readonly FavoriteServicesViewModel _viewModel;

    public FavoriteServicesPage()
        : this(ServiceHelper.GetRequiredService<FavoriteServicesViewModel>())
    {
    }

    public FavoriteServicesPage(FavoriteServicesViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        await _viewModel.LoadAsync();
    }

    private async void OnServiceTapped(object sender, TappedEventArgs e)
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        if ((sender as Element)?.BindingContext is not Service service)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={service.Id}");
    }
}
