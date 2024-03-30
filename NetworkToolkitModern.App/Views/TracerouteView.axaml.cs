using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NetworkToolkitModern.App.Views;

public partial class TracerouteView : UserControl
{
    private readonly ScrollViewer? _replyScrollViewer;

    public TracerouteView()
    {
        InitializeComponent();
        _replyScrollViewer = this.FindControl<ScrollViewer>("ReplyScrollViewer");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _replyScrollViewer?.ScrollToEnd();
    }
}