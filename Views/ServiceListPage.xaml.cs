using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ServiceListPage : ContentPage
{
    public ServiceListPage(ServiceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public ServiceListPage()
    {
        InitializeComponent();
    }

    private void OnSearchAreaTapped(object sender, TappedEventArgs e)
    {
        // 点击搜索区域时，让输入框获得焦点
        SearchEntry.Focus();
    }

    private async void OnServiceTapped(object sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not Service tappedService)
            return;

        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={tappedService.Id}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ServiceListViewModel vm)
        {
            await vm.RefreshFavoritesAsync();
        }
    }
}
