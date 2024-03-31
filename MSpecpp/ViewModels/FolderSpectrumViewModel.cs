using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace MSpecpp.ViewModels;

public class FolderSpectrumViewModel : ViewModelBase
{
    public CaseFolder AssociatedFolder { get; init; }
    public SpectrumViewModel[] SpectrumViewModels { get; set; }

    public FolderSpectrumViewModel(CaseFolder associatedFolder)
    {
        AssociatedFolder = associatedFolder;
        WeakReferenceMessenger.Default.Register<SpectrumViewportChangedMessage>(this,
            (r, m) =>
            {
                // Assume that the mass aspect of spectrums is the same
                float startMass = MainViewModel.Instance.ViewportSize.StartMass;
                float endMass = MainViewModel.Instance.ViewportSize.EndMass;

                if (m.isHorizontal)
                {
                    var batchMax = SpectrumViewModels.Max((x) => x.UpdateHorizontalBounds(startMass, endMass));

                    // Avoid notifying update for twice
                    MainViewModel.Instance.ViewportSize.UpdateViewportNoNotify(
                        yHigher: batchMax * 1.14f, yLower: -batchMax * 0.02f);
                }

                WeakReferenceMessenger.Default.Send(new SpectrumViewportRefreshMessage());
            });
    }

    public void CreateSpectrumViews(Action callback)
    {
        AssociatedFolder.LoadSpectrums(MainViewModel.Instance.PeakCount > 0);

        SpectrumViewModels = AssociatedFolder.Spectrums.Select((x) => new SpectrumViewModel(AssociatedFolder, x))
            .ToArray();
        var batchMax = SpectrumViewModels.Max((x) => x.MaxValue);

        // Since the views are not created yet, it's not necessary to notify
        MainViewModel.Instance.ViewportSize.UpdateViewportNoNotify(
            yHigher: batchMax * 1.14f, yLower: -batchMax * 0.02f);

        float batchMean = 0f, seriesMinMass = float.MaxValue, seriesMaxMass = float.MinValue;
        foreach (var spec in SpectrumViewModels)
        {
            batchMean += spec.Mean / SpectrumViewModels.Length;
            seriesMinMass = Math.Min(seriesMinMass, spec.MainSpectrum.Masses.First());
            seriesMaxMass = Math.Max(seriesMaxMass, spec.MainSpectrum.Masses.Last());
        }

        MainViewModel.Instance.ViewportSize.SeriesMinMass = seriesMinMass;
        MainViewModel.Instance.ViewportSize.SeriesMaxMass = seriesMaxMass;

        Func<SpectrumViewModel, float> scoringFunc = (x) => x.Rms;
        switch (MainViewModel.Instance.ScoringCriteriaIndex)
        {
            // Closest to mean
            case 0:
                scoringFunc = (x) => (1 - MathF.Tanh(MathF.Abs(batchMean - x.Mean) * 0.01f)) * 100f;
                break;
            // Largest mean
            case 1:
                scoringFunc = (x) => x.Mean / 10f;
                break;
            // Largest rms
            case 2:
                scoringFunc = (x) => x.Rms / 10f;
                break;
            // Largest s.d.
            case 3:
                scoringFunc = (x) => x.Sd / 10f;
                break;
            // Most peaks
            case 4:
                scoringFunc = (x) => x.MainSpectrum.Peaks?.Length ?? 0;
                break;
            // Least peaks
            case 5:
                // Assume that the peaks won't be over 400
                scoringFunc = (x) => (1 - MathF.Tanh((x.MainSpectrum.Peaks?.Length ?? 0) * 0.01f)) * 100f;
                break;
        }

        foreach (var spec in SpectrumViewModels)
        {
            spec.Score = scoringFunc(spec);
        }


        // Sort spectrums by rms from large to small
        Array.Sort(SpectrumViewModels, ((a, b) => b.Score.CompareTo(a.Score)));
        callback();
    }
}