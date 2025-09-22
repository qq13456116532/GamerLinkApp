using System;
using Microsoft.Extensions.DependencyInjection;

namespace GamerLinkApp.Helpers;

public static class ServiceHelper
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider is not initialized.");
        }

        return _serviceProvider.GetRequiredService<T>();
    }
}
