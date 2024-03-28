using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public void ExportSelectedToText(string path, Action<int, int> progressCallback)
    {
        int progressIndex = 1;
        int totalCases = CaseFolders.Count((x) => x.SelectedCount > 0);

        foreach (var folder in CaseFolders)
        {
            // Ignore empty folders
            if (folder.SelectedCount == 0)
            {
                continue;
            }
            
            bool needToLoad = folder.Spectrums == null;

            // If not loaded, then load the spectrums into memory
            if (needToLoad)
            {
                folder.LoadSpectrums();
            }

            int index = 1;
            foreach (var spectrum in folder.Spectrums)
            {
                if (folder.SelectedDict[spectrum.FilePath])
                {
                    spectrum.ExportToTextFormat(Path.Combine(path, $"{folder.DisplayName}_{index}.txt"));
                    index++;
                }
            }

            // Release spectrum after used if they are loaded in the loop
            if (needToLoad)
            {
                folder.ReleaseSpectrums();
            }

            progressCallback(progressIndex, totalCases);
            progressIndex++;
        }
        
        // FIXME: window specific-code
        Process.Start("explorer.exe", path);
    }
}