using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ZonePage : ContentPage
{
	public ZonePage()
	{
		InitializeComponent();
	}
	// 修改构造函数以接收 ViewModel
	public ZonePage(ZoneViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm; // 设置数据上下文
	}
}