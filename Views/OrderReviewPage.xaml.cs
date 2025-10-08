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

        if (_orderId > 0 && !_viewModel.IsBusy && !_viewModel.HasOrder)
        {
            _ = _viewModel.LoadAsync(_orderId);
        }
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        var (success, errorMessage) = await _viewModel.SubmitAsync();

        if (!success)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await DisplayAlert("��ʾ", errorMessage, "ȷ��");
            }

            return;
        }

        var goToOrders = await DisplayAlert("���۳ɹ�", "��л���ķ���������״̬�Ѹ���Ϊ����ɡ�", "���ض���", "���ڱ�ҳ");
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

        if (e.Parameter is int value && value >= 1 && value <= 5)
        {
            _viewModel.Rating = value;
        }
    }


}

