using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(ServiceId), "id")]
public partial class ServiceDetailPage : ContentPage
{
    private readonly ServiceDetailViewModel _viewModel;

    public ServiceDetailPage()
        : this(ServiceHelper.GetRequiredService<ServiceDetailViewModel>())
    {
    }

    public ServiceDetailPage(ServiceDetailViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    public string? ServiceId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _viewModel.ServiceId = id;
            }
        }
    }

    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        var (success, errorMessage, order) = await _viewModel.PlaceOrderAsync();

        if (!success)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await DisplayAlert("提示", errorMessage, "确定");
            }

            return;
        }

        if (order is null)
        {
            return;
        }

        var orderNumber = $"订单号：{order.Id:D6}\n";
        var navigateToPayment = await DisplayAlert("下单成功", $"{orderNumber}已生成待支付订单，是否立即支付？", "去支付", "稍后再说");
        if (navigateToPayment && Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"{nameof(OrderPaymentPage)}?orderId={order.Id}");
            return;
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"{nameof(OrderListPage)}?status={nameof(OrderStatus.PendingPayment)}");
        }
    }

    private async void OnToggleFavoriteClicked(object sender, EventArgs e)
    {
        var result = await _viewModel.ToggleFavoriteAsync();

        if (result is null)
        {
            await DisplayAlert("提示", "操作失败，请稍后再试。", "确定");
        }
    }

}
