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

    public FolderSpectrumViewModel(CaseFolder associatedFolder)
    {
        AssociatedFolder = associatedFolder;
        WeakReferenceMessenger.Default.Register<SpectrumViewportChangedMessage>(this,
            (r, m) =>
            {
                if (m.isHorizontal)
                {
                    var batchMax = SpectrumViewModels.Max((x) => x.GetMaxValue(
                        MainViewModel.Instance.ViewportSize.StartPos, MainViewModel.Instance.ViewportSize.EndPos));

                    // Avoid notifying update for twice
                    MainViewModel.Instance.ViewportSize.UpdateViewportNoNotify(
                        yHigher: batchMax * 1.14f, yLower: -batchMax * 0.02f);
                }

                WeakReferenceMessenger.Default.Send(new SpectrumViewportRefreshMessage());
            });
    }

    public SpectrumViewModel[] SpectrumViewModels { get; set; }

    public void CreateSpectrumViews(Action callback)
    {
        AssociatedFolder.LoadSpectrums(MainViewModel.Instance.PeakCount > 0);

        SpectrumViewModels = AssociatedFolder.Spectrums.Select((x) => new SpectrumViewModel(AssociatedFolder, x))
            .ToArray();
        var batchMax = SpectrumViewModels.Max((x) => x.MaxValue);

        // Since the views are not created yet, it's not necessary to notify
        MainViewModel.Instance.ViewportSize.UpdateViewportNoNotify(
            yHigher: batchMax * 1.14f, yLower: -batchMax * 0.02f);

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