using GamerLinkApp.Helpers;
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
}
