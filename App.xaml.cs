using System.Threading.Tasks;
using GamerLinkApp.Helpers;
using GamerLinkApp.Services;
using GamerLinkApp.Models;

namespace GamerLinkApp;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private Window? _currentWindow;

    public App()
    {
        InitializeComponent();

        _authService = ServiceHelper.GetRequiredService<IAuthService>();
        _authService.CurrentUserChanged += OnCurrentUserChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var placeholder = new ContentPage();
        var window = new Window(placeholder);
        _currentWindow = window;

        _ = InitializeRootAsync();

        return window;
    }

    private async Task InitializeRootAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        var initialPage = user is null ? CreateLoginRootPage() : CreateAppShell();
        await SetRootPageAsync(initialPage);
    }

    private async void OnCurrentUserChanged(object? sender, User? user)
    {
        var nextPage = user is null ? CreateLoginRootPage() : CreateAppShell();
        await SetRootPageAsync(nextPage);
    }

    private Page CreateAppShell() => new AppShell();

    private Page CreateLoginRootPage()
    {
        var loginPage = ServiceHelper.GetRequiredService<Views.LoginPage>();
        return new NavigationPage(loginPage);
    }

    private Task SetRootPageAsync(Page page)
    {
        if (_currentWindow is null)
        {
            return Task.CompletedTask;
        }

        if (Dispatcher is null)
        {
            _currentWindow.Page = page;
            return Task.CompletedTask;
        }

        return Dispatcher.DispatchAsync(() => _currentWindow.Page = page);
    }
}

