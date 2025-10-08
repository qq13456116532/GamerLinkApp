using GamerLinkApp.Views;

namespace GamerLinkApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ServiceDetailPage), typeof(ServiceDetailPage));
            Routing.RegisterRoute(nameof(OrderListPage), typeof(OrderListPage));
            Routing.RegisterRoute(nameof(OrderPaymentPage), typeof(OrderPaymentPage));
            Routing.RegisterRoute(nameof(OrderReviewPage), typeof(OrderReviewPage));
            Routing.RegisterRoute(nameof(FavoriteServicesPage), typeof(FavoriteServicesPage));
        }
    }
}
