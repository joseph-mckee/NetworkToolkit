using Microsoft.Extensions.DependencyInjection;
using NetworkToolkitModern.App.ViewModels;

namespace NetworkToolkitModern.App.Services;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<ScanViewModel>();
        collection.AddSingleton<PingViewModel>();
        collection.AddSingleton<TracerouteViewModel>();
        collection.AddSingleton<IpConfigViewModel>();
        collection.AddSingleton<MainWindowViewModel>();
    }
}