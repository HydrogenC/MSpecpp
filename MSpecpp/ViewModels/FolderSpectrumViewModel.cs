using System;
using System.Linq;
using System.Threading.Tasks;

namespace MSpecpp.ViewModels;

public class FolderSpectrumViewModel : ViewModelBase, IDisposable
{
    public CaseFolder AssociatedFolder { get; init; }

    public FolderSpectrumViewModel(CaseFolder associatedFolder)
    {
        AssociatedFolder = associatedFolder;
    }

    public SpectrumViewModel[] SpectrumViewModels { get; set; }

    public void CreateSpectrumViews(Action callback)
    {
        AssociatedFolder.LoadSpectrums();

        SpectrumViewModels = AssociatedFolder.Spectrums.Select((x) => new SpectrumViewModel(AssociatedFolder, x))
            .ToArray();
        var batchMax = SpectrumViewModels.Max((x) => x.MaxValue);
        MainViewModel.Instance.ViewportSize.YHigherBound = batchMax * 1.02f;
        MainViewModel.Instance.ViewportSize.YLowerBound = -batchMax * 0.02f;

        // Sort spectrums by rms from large to small
        Array.Sort(SpectrumViewModels, ((a, b) => b.Rms.CompareTo(a.Rms)));
        callback();
    }

    public void Dispose()
    {
        // Release the read spectrums from memory
        AssociatedFolder.ReleaseSpectrums();
    }
}