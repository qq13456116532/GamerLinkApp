using System;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.Helpers;

public static class AuthNavigationHelper
{
    private static bool _isPresentingLogin;

    public static Task<bool> EnsureAuthenticatedAsync() =>
        EnsureAuthenticatedAsync(ServiceHelper.GetRequiredService<IAuthService>());

    public static async Task<bool> EnsureAuthenticatedAsync(IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);

        if (authService.IsAuthenticated)
        {
            return true;
        }

        var user = await authService.GetCurrentUserAsync();
        if (user is not null)
        {
            return true;
        }

        return await PresentLoginAsync();
    }

    private static async Task<bool> PresentLoginAsync()
    {
        if (_isPresentingLogin)
        {
            return false;
        }

        try
        {
            _isPresentingLogin = true;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var navigation = GetCurrentNavigation();
                if (navigation is null)
                {
                    return;
                }

                if (navigation.ModalStack.Any(IsLoginModal))
                {
                    return;
                }

                var loginPage = ServiceHelper.GetRequiredService<LoginPage>();
                await navigation.PushModalAsync(new NavigationPage(loginPage));
            });
        }
        finally
        {
            _isPresentingLogin = false;
        }

        return false;
    }

    private static INavigation? GetCurrentNavigation()
    {
        var application = Application.Current;
        if (application is not null)
        {
            var window = application.Windows.FirstOrDefault();
            if (window?.Page is not null)
            {
                return window.Page.Navigation;
            }
        }

        return Shell.Current?.Navigation;
    }

    private static bool IsLoginModal(Page page) =>
        page switch
        {
            NavigationPage navigationPage => navigationPage.RootPage is LoginPage,
            LoginPage => true,
            _ => false
        };
}
