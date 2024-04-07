using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NetworkToolkitModern.App.Services;

namespace NetworkToolkitModern.App.Views;

public partial class ScanView : UserControl
{
    public ScanView()
    {
        InitializeComponent();
        var dataGrid = this.FindControl<DataGrid>("HostGrid");
        var ipColumn = dataGrid?.Columns
            .FirstOrDefault(c => c.Header.ToString() == "IP Address");
        if (ipColumn != null) ipColumn.CustomSortComparer = new IpAddressComparer();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}