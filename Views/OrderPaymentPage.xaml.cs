using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderPaymentPage : ContentPage
{
    private readonly OrderPaymentViewModel _viewModel;
    private int _orderId;

    public OrderPaymentPage()
        : this(ServiceHelper.GetRequiredService<OrderPaymentViewModel>())
    {
    }

    public OrderPaymentPage(OrderPaymentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public int OrderId
    {
        set
        {
            if (value <= 0)
            {
                return;
            }

            if (_orderId == value && _viewModel.HasOrder)
            {
                return;
            }

            _orderId = value;
            _ = _viewModel.LoadAsync(value);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_orderId > 0 && !_viewModel.HasOrder && !_viewModel.IsLoading)
        {
            _ = _viewModel.LoadAsync(_orderId);
        }
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        var (success, errorMessage) = await _viewModel.PayAsync();

        if (!success)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await DisplayAlert("提示", errorMessage, "确定");
            }

            return;
        }

        var navigateToOrders = await DisplayAlert("支付成功", "订单已支付，正在为你安排服务。", "前往订单", "留在此页");
        if (navigateToOrders && Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"{nameof(OrderListPage)}?status={nameof(OrderStatus.Ongoing)}");
        }
    }
}
