using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Loader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MSpecpp.ViewModels;
using MSpecpp.Views;

namespace MSpecpp.Controls;

public partial class SpectrumCard : UserControl
{
    public SpectrumCard()
    {

    }

    public SpectrumCard(SpectrumViewModel context)
    {
        InitializeComponent();
        DataContext = context;
        if (context.MainSpectrum.Peaks != null)
        {
            PeakCountLabel.Text = $"{context.MainSpectrum.Peaks.Length} Peaks";
        }
    }

    private async void MassTableButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as SpectrumViewModel;
        var dialog = new MassListWindow
        {
            DataContext = new MassListViewModel
            {
                Data = new ObservableCollection<SpectrumPair>(viewModel.MainSpectrum.Peaks.Select(
                    (x) => new SpectrumPair(viewModel.MainSpectrum.Masses[x], viewModel.MainSpectrum.Intensities[x])
                ))
            }
        };

        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await dialog.ShowDialog(desktop.MainWindow);
        }
    }
}