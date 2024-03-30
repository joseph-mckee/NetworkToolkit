using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NetworkToolkitModern.App.Views;

public partial class ScanView : UserControl
{
    public ScanView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var address = (sender as MenuItem)?.CommandParameter?.ToString();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"http://{address}",
                UseShellExecute = true // Necessary for .NET Core/.NET 5+ applications
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var address = (sender as Grid)?.Tag?.ToString();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"http://{address}",
                UseShellExecute = true // Necessary for .NET Core/.NET 5+ applications
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
        }
    }
}