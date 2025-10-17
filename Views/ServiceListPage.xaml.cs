using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;
using System.Timers; // 引入Timer命名空间


namespace GamerLinkApp.Views;

public partial class ServiceListPage : ContentPage
{
    private System.Timers.Timer _carouselTimer; // 定义一个计时器
    public ServiceListPage(ServiceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        InitializeTimer(); // 初始化计时器
    }

    public ServiceListPage()
    {
        InitializeComponent();
        InitializeTimer(); // 初始化计时器
    }
    private void InitializeTimer()
    {
        // 设置计时器，每3秒（3000毫秒）触发一次
        _carouselTimer = new System.Timers.Timer(3000);
        _carouselTimer.Elapsed += OnTimerElapsed;
        _carouselTimer.AutoReset = true; // 持续触发
    }

    private void OnSearchAreaTapped(object sender, TappedEventArgs e)
    {
        SearchEntry.Focus();
    }

    private async void OnServiceTapped(object sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not Service tappedService)
            return;

        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={tappedService.Id}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ServiceListViewModel vm)
        {
            await vm.RefreshFavoritesAsync();
        }
        // 页面可见时，启动计时器
        if (BannerCarouselView?.ItemsSource?.Cast<object>().Any() == true)
        {
            _carouselTimer.Start();
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // 页面不可见时，停止计时器，以节省资源
        _carouselTimer.Stop();
    }
    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // 计时器事件在后台线程触发，UI更新需要切换到主线程
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (BannerCarouselView == null || BannerCarouselView.ItemsSource == null)
            {
                return;
            }

            var items = BannerCarouselView.ItemsSource.Cast<object>().ToList();
            if (items.Count < 2) // 如果少于2个项目，则无需滚动
            {
                return;
            }

            var currentPosition = BannerCarouselView.Position;
            var next = currentPosition + 1;

            if (next >= items.Count)
            {
                // 从最后一张“绕回到第 1 张”，用无动画跳转更稳
                BannerCarouselView.ScrollTo(0, position: ScrollToPosition.Center, animate: false);
            }
            else
            {
                BannerCarouselView.ScrollTo(next, position: ScrollToPosition.Center, animate: true);
            }
        });
    }
}
