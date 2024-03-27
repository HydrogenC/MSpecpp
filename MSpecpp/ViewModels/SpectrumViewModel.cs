using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MSpecpp.ViewModels;

public partial class SpectrumViewModel : ViewModelBase
{
    public SpectrumViewModel(string spectrumPath)
    {
        MainSpectrum = Spectrum.ReadFromBrukerFlex(spectrumPath);
        var pathsFolders = spectrumPath.Split(['/', '\\']);
        Id = pathsFolders[^2] + '_' + pathsFolders[^1];
        UpdateSpectrumInfo();
    }

    [ObservableProperty] private Spectrum? mainSpectrum;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(MainSpectrum))
        {
            UpdateSpectrumInfo();
        }
    }

    private void UpdateSpectrumInfo()
    {
        int len = MainSpectrum.Values.Length;
        float rmsSqared = 0;
        foreach (var item in MainSpectrum.Values)
        {
            rmsSqared += item.Intensity * item.Intensity / len;
        }

        Score = Variance = Rms = MathF.Sqrt(rmsSqared);
    }

    [ObservableProperty] public string id;

    [ObservableProperty] public float rms;

    [ObservableProperty] public float score;

    [ObservableProperty] public float variance;
}