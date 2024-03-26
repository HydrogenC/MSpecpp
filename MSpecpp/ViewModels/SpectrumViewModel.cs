using CommunityToolkit.Mvvm.ComponentModel;

namespace MSpecpp.ViewModels;

public partial class SpectrumViewModel : ViewModelBase
{
    [ObservableProperty] private Spectrum? mainSpectrum;

    [ObservableProperty] private int id;

    [ObservableProperty] private float rms;

    [ObservableProperty] private float score;

    [ObservableProperty] private float variance;
}