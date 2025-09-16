using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ServiceDetailPage : ContentPage
{
    public ServiceDetailPage(ServiceDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}