using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace MSpecpp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        viewportSize.PropertyChanged += ViewportSizeOnPropertyChanged;
    }

    private void ViewportSizeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(ViewportSize);
    }

    public static MainViewModel Instance { get; set; }

    [ObservableProperty] private string information = "Press button to read mass spectrum!";

    [ObservableProperty] private string title = "MSpec++";

    [ObservableProperty] private SpectrumViewport viewportSize = new()
    {
        StartPos = 0,
        EndPos = 1,
        YHigherBound = 160000,
        YLowerBound = -1000
    };

    [ObservableProperty] public string openedDir = "";

    [ObservableProperty] public ObservableCollection<CaseFolder> caseFolders = [];

    [ObservableProperty] public int targetSelectionCount = 4;
}