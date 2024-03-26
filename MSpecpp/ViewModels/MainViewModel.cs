using CommunityToolkit.Mvvm.ComponentModel;

namespace MSpecpp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public static MainViewModel Instance { get; set; }

    [ObservableProperty] private SpectrumViewModel spectrumViewModel = new SpectrumViewModel();

    [ObservableProperty] private string information = "Press button to read mass spectrum!";

    [ObservableProperty] private string title = "MSpec++";

    [ObservableProperty] public double viewScale = 1;
}