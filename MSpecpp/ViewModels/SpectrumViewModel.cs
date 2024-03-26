using CommunityToolkit.Mvvm.ComponentModel;

namespace MSpecpp.ViewModels;

public partial class SpectrumViewModel : ViewModelBase
{
    private Spectrum? mainSpectrum;

    public Spectrum? MainSpectrum
    {
        get => mainSpectrum;
        set
        {
            SetProperty(ref mainSpectrum, value);
            
        }
    }

    [ObservableProperty] private int id;

    [ObservableProperty] private float rms;

    [ObservableProperty] private float score;

    [ObservableProperty] private float varience;
}