using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.Views;

public partial class AdminOrdersPage : ContentPage
{
    public AdminOrdersPage()
        : this(ServiceHelper.GetRequiredService<AdminOrdersViewModel>())
    {
    }

    public AdminOrdersPage(AdminOrdersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        if (BindingContext is AdminOrdersViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}
