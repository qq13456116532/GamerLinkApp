using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ServiceListPage : ContentPage
{
    public ServiceListPage(ServiceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // 通过依赖注入绑定 ViewModel
    }

    // 无参数的构造函数可以保留，以便XAML预览器正常工作
    public ServiceListPage()
    {
        InitializeComponent();
    }

    // 新增: 处理服务项目选择事件
    private async void OnServiceSelected(object sender, SelectionChangedEventArgs e)
    {
        // 确保有项目被选中
        if (e.CurrentSelection.FirstOrDefault() is not Service selectedService)
            return;

        // 使用 Shell 导航到详情页，并通过查询参数传递服务ID
        // "id" 必须与 ServiceDetailViewModel 中的 QueryProperty 名称匹配
        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={selectedService.Id}");

        // 取消选中，以便用户可以再次选择同一个项目
        ((CollectionView)sender).SelectedItem = null;
    }
}