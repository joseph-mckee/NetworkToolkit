﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NetworkToolkitModern.App.ViewModels;

namespace NetworkToolkitModern.App.Views;

public partial class PingView : ReactiveUserControl<PingViewModel>
{
    private readonly ScrollViewer? _replyScrollViewer;

    public PingView()
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