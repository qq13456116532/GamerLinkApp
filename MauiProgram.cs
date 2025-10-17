using System.IO;
using GamerLinkApp.Data;
using GamerLinkApp.Helpers;
using GamerLinkApp.Services;
using GamerLinkApp.ViewModels;
using GamerLinkApp.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

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

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "gamerlink.db");

#if DEBUG
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
                Console.WriteLine($"Database reset: {databasePath}");
            }
            else
            {
                Console.WriteLine("Database file not found.");
            }
#endif

            builder.Services.AddDbContextFactory<ServiceDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            builder.Services.AddSingleton<IDataService, SqliteDataService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();

            builder.Services.AddSingleton<ServiceListPage>();
            builder.Services.AddSingleton<ServiceListViewModel>();

            builder.Services.AddSingleton<ZonePage>();
            builder.Services.AddSingleton<ZoneViewModel>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddSingleton<ProfileViewModel>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();

            builder.Services.AddTransient<OrderListPage>();
            builder.Services.AddTransient<OrderListViewModel>();
            builder.Services.AddTransient<OrderPaymentPage>();
            builder.Services.AddTransient<OrderPaymentViewModel>();
            builder.Services.AddTransient<ServiceDetailPage>();
            builder.Services.AddTransient<ServiceDetailViewModel>();
            builder.Services.AddTransient<OrderReviewPage>();
            builder.Services.AddTransient<OrderReviewViewModel>();
            builder.Services.AddTransient<FavoriteServicesPage>();
            builder.Services.AddTransient<FavoriteServicesViewModel>();
            builder.Services.AddTransient<SupportChatPage>();
            builder.Services.AddTransient<SupportChatViewModel>();
            builder.Services.AddTransient<AdminDashboardPage>();
            builder.Services.AddTransient<AdminDashboardViewModel>();
            builder.Services.AddTransient<AdminOrdersPage>();
            builder.Services.AddTransient<AdminOrdersViewModel>();
            builder.Services.AddTransient<AdminUsersViewModel>();
            builder.Services.AddTransient<AdminUsersPage>();

            // 注册 RAG 服务为单例
            builder.Services.AddSingleton<IRagService, RagService>();

            var app = builder.Build();

            // 在 App 启动后异步初始化 RAG 服务
            Task.Run(async () =>
            {
                var ragService = ServiceHelper.GetRequiredService<IRagService>();
                await ragService.InitializeAsync();
            });

            ServiceHelper.Initialize(app.Services);

            return app;
        }
    }
}
