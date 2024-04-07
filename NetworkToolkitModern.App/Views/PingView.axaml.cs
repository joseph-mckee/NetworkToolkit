using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NetworkToolkitModern.App.ViewModels;

namespace NetworkToolkitModern.App.Views;

public partial class PingView : UserControl
{
    private readonly DataGrid? _dataGrid;

    public PingView()
    {
        InitializeComponent();
        _dataGrid = this.FindControl<DataGrid>("ReplyGrid");
        DataContextChanged += PingView_DataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void PingView_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PingViewModel viewModel)
            viewModel.ScrollToNewItemRequested += ViewModel_ScrollToNewItemRequested;
    }

    private void ViewModel_ScrollToNewItemRequested(object? sender, EventArgs e)
    {
        if (DataContext is PingViewModel { PingReplies.Count: > 0 } viewModel)
            _dataGrid?.ScrollIntoView(viewModel.PingReplies.Last(), null);
    }
}