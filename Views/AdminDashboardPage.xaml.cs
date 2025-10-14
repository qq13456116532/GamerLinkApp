using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.Views;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage()
        : this(ServiceHelper.GetRequiredService<AdminDashboardViewModel>())
    {
    }

    public AdminDashboardPage(AdminDashboardViewModel vm)
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

        if (BindingContext is AdminDashboardViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}
