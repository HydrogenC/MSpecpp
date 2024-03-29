using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MSpecpp.ViewModels;

public class FolderSpectrumViewModel : ViewModelBase
{
    public CaseFolder AssociatedFolder { get; init; }

    public FolderSpectrumViewModel(CaseFolder associatedFolder)
    {
        AssociatedFolder = associatedFolder;
    }

    public SpectrumViewModel[] SpectrumViewModels { get; set; }

    public void CreateSpectrumViews(Action callback)
    {
        AssociatedFolder.LoadSpectrums(MainViewModel.Instance.ShowPeaks);

        SpectrumViewModels = AssociatedFolder.Spectrums.Select((x) => new SpectrumViewModel(AssociatedFolder, x))
            .ToArray();
        var batchMax = SpectrumViewModels.Max((x) => x.MaxValue);
        MainViewModel.Instance.ViewportSize.YHigherBound = batchMax * 1.02f;
        MainViewModel.Instance.ViewportSize.YLowerBound = -batchMax * 0.02f;

        float batchMean = 0f;
        foreach (var spec in SpectrumViewModels)
        {
            batchMean += spec.Mean / SpectrumViewModels.Length;
        }

        foreach (var spec in SpectrumViewModels)
        {
            spec.Score = MathF.Tanh(MathF.Abs(batchMean - spec.Mean) * 0.01f) * 100f;
        }

        // Sort spectrums by rms from large to small
        Array.Sort(SpectrumViewModels, ((a, b) => b.Score.CompareTo(a.Score)));
        callback();
    }
}