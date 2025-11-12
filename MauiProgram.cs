using System.IO;
using GamerLinkApp.Data;
using GamerLinkApp.Helpers;
using GamerLinkApp.Services;
using GamerLinkApp.ViewModels;
using GamerLinkApp.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Storage;
#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

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
            builder.Services.AddTransient<OrderDetailPage>();
            builder.Services.AddTransient<OrderDetailViewModel>();
            
            // Register RAG service as a singleton.
            builder.Services.AddSingleton<IRagService, RagService>();

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        const int DefaultWindowWidth = 1360;
                        const int DefaultWindowHeight = 900;

                        var appWindow = window.GetAppWindow();
                        if (appWindow is null)
                        {
                            return;
                        }

                        appWindow.Resize(new SizeInt32(DefaultWindowWidth, DefaultWindowHeight));

                        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
                        if (displayArea is not null)
                        {
                            var centeredPosition = new PointInt32(
                                displayArea.WorkArea.X + (displayArea.WorkArea.Width - DefaultWindowWidth) / 2,
                                displayArea.WorkArea.Y + (displayArea.WorkArea.Height - DefaultWindowHeight) / 2);

                            appWindow.Move(centeredPosition);
                        }

                        if (appWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsResizable = false;
                            presenter.IsMaximizable = true;
                            presenter.IsMinimizable = true;
                        }

                        window.SizeChanged += (_, args) =>
                        {
                            if (appWindow.Presenter is OverlappedPresenter overlappedPresenter &&
                                overlappedPresenter.State == OverlappedPresenterState.Maximized)
                            {
                                return;
                            }

                            var width = (int)Math.Round(args.Size.Width);
                            var height = (int)Math.Round(args.Size.Height);

                            if (width != DefaultWindowWidth || height != DefaultWindowHeight)
                            {
                                appWindow.Resize(new SizeInt32(DefaultWindowWidth, DefaultWindowHeight));
                            }
                        };
                    });
                });
            });
#endif

            var app = builder.Build();

            // Ensure the RAG service is initialized before first use.
            var ragService = app.Services.GetRequiredService<IRagService>();
            try
            {
                ragService.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RAG init failed at startup: {ex}");
            }

            ServiceHelper.Initialize(app.Services);
            return app;
        }
    }
}
