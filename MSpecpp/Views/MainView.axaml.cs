using Avalonia.Controls;
using Avalonia.Interactivity;
using MSpecpp.ViewModels;
using System;
using System.Linq;

namespace MSpecpp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        MainViewModel.Instance.SpectrumViewModel.MainSpectrum = Spectrum.ReadFromBrukerFlex(@"C:\Code\FlexData\HC\316\0_K2\9");
        // MainViewModel.Instance.SpectrumViewModel.MainSpectrum = Spectrum.ReadFromTextFormat(@"C:\Users\x1398\Downloads\316_0_K2_9.txt");
        MainViewModel.Instance.SpectrumViewModel.Id = 9;
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        SpectrumView.InvalidateVisual();
    }
}
