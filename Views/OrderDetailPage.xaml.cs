using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderDetailPage : ContentPage
{
    private readonly OrderDetailViewModel _viewModel;
    private int _orderId;

    public OrderDetailPage(OrderDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public int OrderId
    {
        set
        {
            if (_orderId == value) return;
            _orderId = value;
            _ = _viewModel.LoadOrderAsync(value);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (_orderId > 0)
        {
            await _viewModel.LoadOrderAsync(_orderId);
        }
    }
}