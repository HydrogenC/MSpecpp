using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace MSpecpp.ViewModels;

public partial class SpectrumViewModel : ViewModelBase
{
    public SpectrumViewModel(CaseFolder associatedFolder, Spectrum spectrum)
    {
        AssociatedFolder = associatedFolder;
        MainSpectrum = spectrum;
        IsSelected = associatedFolder.SelectedDict[spectrum.FilePath];
        var pathsFolders = spectrum.FilePath.Split(['/', '\\']);
        Id = pathsFolders[^2] + '_' + pathsFolders[^1];
        UpdateSpectrumInfo();
    }

    // To notify changes of selected states
    private CaseFolder AssociatedFolder { get; init; }

    [ObservableProperty] private Spectrum mainSpectrum;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(MainSpectrum))
        {
            UpdateSpectrumInfo();
        }
        else if (e.PropertyName == nameof(IsSelected))
        {
            bool prev = AssociatedFolder.SelectedDict[MainSpectrum.FilePath];
            if (prev != IsSelected)
            {
                AssociatedFolder.SelectedDict[MainSpectrum.FilePath] = IsSelected;
                if (IsSelected)
                {
                    AssociatedFolder.SelectedCount++;
                }
                else
                {
                    AssociatedFolder.SelectedCount--;
                }
            }
        }
    }

    private void UpdateSpectrumInfo()
    {
        int len = MainSpectrum.Values.Length;

        // Assigning observale properties might be slow, so we use local variables in calculation
        float rmsSqared = 0, maxValueTemp = 0;
        foreach (var item in MainSpectrum.Values)
        {
            rmsSqared += item.Intensity * item.Intensity / len;
            maxValueTemp = MathF.Max(maxValueTemp, item.Intensity);
        }

        MaxValue = maxValueTemp;
        Score = Variance = Rms = MathF.Sqrt(rmsSqared);
    }

    [ObservableProperty] public string id;

    [ObservableProperty] public float rms;

    [ObservableProperty] public float maxValue;

    [ObservableProperty] public float score;

    [ObservableProperty] public float variance;

    [ObservableProperty] public bool isSelected;
}