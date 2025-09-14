using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ServiceListPage : ContentPage
{
    public ServiceListPage(ServiceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // 通过依赖注入绑定 ViewModel
    }
}
