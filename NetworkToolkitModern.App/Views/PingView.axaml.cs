using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NetworkToolkitModern.App.Views;

public partial class PingView : UserControl
{
    private readonly ScrollViewer replyScrollViewer;

    public PingView()
    {
        InitializeComponent();
        replyScrollViewer = this.FindControl<ScrollViewer>("ReplyScrollViewer");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        replyScrollViewer.ScrollToEnd();
    }
}