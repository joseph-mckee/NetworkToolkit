using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NetworkToolkitModern.App.ViewModels;

namespace NetworkToolkitModern.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<MainWindowViewModel>();
    }
}