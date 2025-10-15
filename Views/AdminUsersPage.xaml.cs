using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.Views;

public partial class AdminUsersPage : ContentPage
{
    public AdminUsersPage()
        : this(ServiceHelper.GetRequiredService<AdminUsersViewModel>())
    {
    }

    public AdminUsersPage(AdminUsersViewModel vm)
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

        if (BindingContext is AdminUsersViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}
