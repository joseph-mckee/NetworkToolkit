using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NetworkToolkitModern.App.Views;

public partial class SnmpView : UserControl
{
    public SnmpView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}