using GamerLinkApp.Services;
using Microsoft.Extensions.Logging;
using GamerLinkApp.ViewModels;
using GamerLinkApp.Views;
namespace GamerLinkApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // 注册服务
            builder.Services.AddSingleton<IDataService, MockDataService>();

            // 注册视图和视图模型
            builder.Services.AddSingleton<ServiceListPage>();
            builder.Services.AddSingleton<ServiceListViewModel>();

            // ... 注册其他页面和视图模型
            builder.Services.AddSingleton<ZonePage>();
            builder.Services.AddSingleton<ZoneViewModel>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddSingleton<ProfileViewModel>();

            builder.Services.AddTransient<ServiceDetailPage>();
            builder.Services.AddTransient<ServiceDetailViewModel>();

            return builder.Build();
        }
    }
}
