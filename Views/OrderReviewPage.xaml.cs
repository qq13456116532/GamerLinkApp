using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderReviewPage : ContentPage
{
    private readonly OrderReviewViewModel _viewModel;
    private int _orderId;

    public OrderReviewPage()
        : this(ServiceHelper.GetRequiredService<OrderReviewViewModel>())
    {
    }

    public OrderReviewPage(OrderReviewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public int OrderId
    {
        set
        {
            if (value <= 0 || (_orderId == value && _viewModel.HasOrder))
            {
                return;
            }

            _orderId = value;
            _ = _viewModel.LoadAsync(value);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        if (_orderId > 0 && !_viewModel.IsBusy && !_viewModel.HasOrder)
        {
            await _viewModel.LoadAsync(_orderId);
        }
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            return;
        }

        var (success, errorMessage) = await _viewModel.SubmitAsync();

        if (!success)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await DisplayAlert("提示", errorMessage, "确定");
            }

            return;
        }

        var goToOrders = await DisplayAlert(
            "评价成功",
            "感谢您的反馈，订单状态已更新为“已完成”。",
            "返回订单",
            "留在本页");

        if (goToOrders && Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private void OnRatingStarTapped(object sender, TappedEventArgs e)
    {
        if (_viewModel.IsRatingReadOnly)
        {
            return;
        }

        if (e.Parameter is int value && value is >= 1 and <= 5)
        {
            _viewModel.Rating = value;
        }
    }
}
