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
        int len = MainSpectrum.Length;
        Mean = MainSpectrum.CalcMean();

        // Assigning observable properties might be slow, so we use local variables in calculation
        float rmsSqared = 0, maxValueTemp = 0, sdTemp = 0;
        foreach (var item in MainSpectrum.Intensities)
        {
            sdTemp += (item - Mean) * (item - Mean) / len;
            rmsSqared += item * item / len;
            maxValueTemp = MathF.Max(maxValueTemp, item);
        }

        Sd = MathF.Sqrt(sdTemp);
        Rms = MathF.Sqrt(rmsSqared);
        MaxValue = maxValueTemp;
    }

    public float GetMaxValue(float startPos, float endPos)
    {
        float maxValueTemp = 0;

        int startIndex = (int)(MainSpectrum.Length * startPos);
        int endIndex = (int)(MainSpectrum.Length * endPos);
        if (startIndex < 0 || endPos < 0)
        {
            return 0;
        }

        ArraySegment<float> segment = new(MainSpectrum.Intensities, startIndex, endIndex - startIndex);
        foreach (var item in segment)
        {
            maxValueTemp = MathF.Max(maxValueTemp, item);
        }

        return maxValueTemp;
    }

    [ObservableProperty] private string id;

    [ObservableProperty] private float rms;

    [ObservableProperty] private float mean;

    [ObservableProperty] private float maxValue;

    [ObservableProperty] private float score;

    [ObservableProperty] private float sd;

    [ObservableProperty] private bool isSelected;
}