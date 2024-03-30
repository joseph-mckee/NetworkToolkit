using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NetworkToolkitModern.App.Services;
using NetworkToolkitModern.App.ViewModels;
using NetworkToolkitModern.App.Views;

namespace NetworkToolkitModern.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddCommonServices();

        var services = collection.BuildServiceProvider();

        var vm = services.GetRequiredService<MainWindowViewModel>();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = vm
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}