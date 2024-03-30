using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.SelectedItem = null;
        }
    }

    private void StyledElement_OnInitialized(object? sender, EventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.SelectedItem = null;
        }
    }
}