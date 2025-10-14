using System;
using System.Threading.Tasks;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GamerLinkApp;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ServiceDetailPage), typeof(ServiceDetailPage));
        Routing.RegisterRoute(nameof(OrderListPage), typeof(OrderListPage));
        Routing.RegisterRoute(nameof(OrderPaymentPage), typeof(OrderPaymentPage));
        Routing.RegisterRoute(nameof(OrderReviewPage), typeof(OrderReviewPage));
        Routing.RegisterRoute(nameof(FavoriteServicesPage), typeof(FavoriteServicesPage));
        Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
        Routing.RegisterRoute(nameof(AdminOrdersPage), typeof(AdminOrdersPage));
        Routing.RegisterRoute(nameof(AdminUsersPage), typeof(AdminUsersPage));

        _authService = ServiceHelper.GetRequiredService<IAuthService>();
        _authService.CurrentUserChanged += OnCurrentUserChanged;

        _ = InitializeTabsAsync();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is null)
        {
            _authService.CurrentUserChanged -= OnCurrentUserChanged;
        }
    }

    private async Task InitializeTabsAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        await MainThread.InvokeOnMainThreadAsync(() => UpdateTabs(user));
    }

    private void OnCurrentUserChanged(object? sender, User? user) =>
        _ = MainThread.InvokeOnMainThreadAsync(() => UpdateTabs(user));

    private void UpdateTabs(User? user)
    {
        Items.Clear();

        if (user?.IsAdmin == true)
        {
            Items.Add(CreateTab("服务管理", "tab_shop.png", () => ServiceHelper.GetRequiredService<AdminDashboardPage>()));
            Items.Add(CreateTab("订单管理", "tab_zone.png", () => ServiceHelper.GetRequiredService<AdminOrdersPage>()));
            Items.Add(CreateTab("用户管理", "tab_mine.png", () => ServiceHelper.GetRequiredService<AdminUsersPage>()));
        }
        else
        {
            Items.Add(CreateTab("服务", "tab_shop.png", () => ServiceHelper.GetRequiredService<ServiceListPage>()));
            Items.Add(CreateTab("专区", "tab_zone.png", () => ServiceHelper.GetRequiredService<ZonePage>()));
            Items.Add(CreateTab("个人", "tab_mine.png", () => ServiceHelper.GetRequiredService<ProfilePage>()));
        }

        if (Items.Count > 0)
        {
            CurrentItem = Items[0];
        }
    }

    private static Tab CreateTab(string title, string icon, Func<Page> pageFactory)
    {
        var content = new ShellContent
        {
            Title = title,
            ContentTemplate = new DataTemplate(pageFactory)
        };

        var tab = new Tab
        {
            Title = title,
            Icon = icon
        };

        tab.Items.Add(content);
        return tab;
    }
}
