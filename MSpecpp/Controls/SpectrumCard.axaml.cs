using System;
using System.ComponentModel;
using System.Runtime.Loader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSpecpp.ViewModels;
using MSpecpp.Views;

namespace MSpecpp.Controls;

public partial class SpectrumCard : UserControl, IDisposable
{
    public SpectrumCard()
    {
        InitializeComponent();
        
        MainViewModel.Instance.PropertyChanged += HandleSpectrumViewUpdate;
    }

    void HandleSpectrumViewUpdate(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ViewScale) or nameof(SpectrumViewModel.MainSpectrum))
        {
            SpectrumView.InvalidateVisual();
        }
    }

    public void Dispose()
    {
        SpectrumViewModel viewModel = (SpectrumViewModel)DataContext!;
        viewModel.PropertyChanged -= HandleSpectrumViewUpdate;
        MainViewModel.Instance.PropertyChanged -= HandleSpectrumViewUpdate;
    }
}