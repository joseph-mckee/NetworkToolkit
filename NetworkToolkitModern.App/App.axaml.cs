using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NetworkToolkitModern.App.ViewModels;
using NetworkToolkitModern.App.Views;

namespace NetworkToolkitModern.App;

public class App : Application
{
    public App()
    {
        Services = ConfigureServices();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ScanViewModel>();
        services.AddSingleton<PingViewModel>();
        services.AddSingleton<TracerouteViewModel>();
        services.AddSingleton<IpConfigViewModel>();
        services.AddSingleton<SnmpViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        return services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}