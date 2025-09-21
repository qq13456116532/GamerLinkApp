using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

[QueryProperty(nameof(ServiceId), "id")]
public partial class ServiceDetailPage : ContentPage
{
    // 将 ViewModel 声明为属性，以便在 ServiceId setter 中访问
    private readonly ServiceDetailViewModel _viewModel;

    // 只保留这一个构造函数，用于依赖注入
    public ServiceDetailPage(ServiceDetailViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    // ServiceId 属性接收从列表页传来的 ID
    public string? ServiceId
    {
        set
        {
            // 确保 _viewModel 不为 null，然后设置其 ServiceId
            // 这个 ID 会触发 ViewModel 内部的数据加载逻辑
            if (int.TryParse(value, out var id) && _viewModel != null)
            {
                _viewModel.ServiceId = id;
            }
        }
    }
}