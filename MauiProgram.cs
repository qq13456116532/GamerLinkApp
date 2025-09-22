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
                Console.WriteLine($"数据库已删除: {databasePath}");
            }
            else
            {
                Console.WriteLine("数据库不存在。");
            }
#endif

            builder.Services.AddDbContextFactory<ServiceDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            builder.Services.AddSingleton<IDataService, SqliteDataService>();

            // ע���б�ҳ����ͼģ��
            builder.Services.AddSingleton<ServiceListPage>();
            builder.Services.AddSingleton<ServiceListViewModel>();

            // ע������ҳ����ͼģ��
            builder.Services.AddSingleton<ZonePage>();
            builder.Services.AddSingleton<ZoneViewModel>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddSingleton<ProfileViewModel>();

            builder.Services.AddTransient<ServiceDetailPage>();
            builder.Services.AddTransient<ServiceDetailViewModel>();

            var app = builder.Build();
            ServiceHelper.Initialize(app.Services);

            return app;
        }
    }
}
