using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NetworkToolkitModern.App.ViewModels;

namespace NetworkToolkitModern.App.Views;

public partial class IpConfigView : UserControl
{
    public IpConfigView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}