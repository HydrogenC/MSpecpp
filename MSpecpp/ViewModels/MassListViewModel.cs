using System.Collections.ObjectModel;

namespace MSpecpp.ViewModels;

public record struct SpectrumPair(float Mass, float Intensity);

public class MassListViewModel : ViewModelBase
{
    public ObservableCollection<SpectrumPair> Data { get; init; }
}