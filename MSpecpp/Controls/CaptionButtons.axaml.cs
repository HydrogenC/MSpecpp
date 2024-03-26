﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace MSpecpp.Controls;

public partial class CaptionButtons : UserControl
{
    public CaptionButtons()
    {
        InitializeComponent();
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeOrRestoreWindow(object sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        if (window != null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        window?.Close();
    }
}