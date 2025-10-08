using System;
using Microsoft.Maui.Controls;
using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(InitialStatusKey), "status")]
public partial class OrderListPage : ContentPage
{
    public OrderListPage()
        : this(ServiceHelper.GetRequiredService<OrderListViewModel>())
    {
    }

    public OrderListPage(OrderListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public string InitialStatusKey
    {
        set
        {
            if (BindingContext is OrderListViewModel vm && !string.IsNullOrEmpty(value))
            {
                vm.SetInitialFilter(value);
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is OrderListViewModel vm)
        {
            _ = vm.RefreshAsync();
        }
    }

    private async void OnPayOrderClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not OrderListViewModel.OrderListItem item)
        {
            return;
        }

        if (Shell.Current is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(OrderPaymentPage)}?orderId={item.OrderId}");
    }

    private async void OnReviewOrderClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not OrderListViewModel.OrderListItem item)
        {
            return;
        }

        if (Shell.Current is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(OrderReviewPage)}?orderId={item.OrderId}");
    }
}
