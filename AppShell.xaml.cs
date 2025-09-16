using GamerLinkApp.Views;

namespace GamerLinkApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // 为服务详情页注册路由, 以便导航服务可以找到
            Routing.RegisterRoute(nameof(ServiceDetailPage), typeof(ServiceDetailPage));
        }
    }
}
