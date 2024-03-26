using Avalonia.Controls;
using Avalonia.Interactivity;
using MSpecpp.ViewModels;
using System;
using System.Linq;
using MathNet.Numerics;

namespace MSpecpp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel?)DataContext;
        SpectrumView.InvalidateVisual();
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        SpectrumView.InvalidateVisual();
    }
}
