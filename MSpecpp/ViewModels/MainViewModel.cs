using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    [ObservableProperty] private string title = "MSpec++";

    [ObservableProperty] private SpectrumViewport viewportSize = new()
    {
        StartPos = 0,
        EndPos = 1,
        YHigherBound = 160000,
        YLowerBound = -1000
    };

    [ObservableProperty] private string openedDir = "";

    [ObservableProperty] private ObservableCollection<CaseFolder> caseFolders = [];

    [ObservableProperty] private int targetSelectionCount = 4;

    [ObservableProperty] private int peakCount = 5;

    public bool HasPeaks => PeakCount > 0;

    [ObservableProperty] private int halfWindowSize = 70;

    [ObservableProperty] private float snr = 2;

    public MainViewModel(SettingsModel settings)
    {
        viewportSize.PropertyChanged += ViewportSizeOnPropertyChanged;
        if (Directory.Exists(settings.OpenPath))
        {
            OpenFolder(settings.OpenPath, false);
        }

        // Align config with newly acquired folders
        foreach (var folder in CaseFolders)
        {
            int indexInJson = Array.FindIndex(settings.CaseFolders, (x) => x.FolderPath == folder.FolderPath);
            // folder found in config
            if (indexInJson >= 0)
            {
                foreach (var path in settings.CaseFolders[indexInJson].SelectedSpectrumPaths)
                {
                    folder.SelectedDict[path] = true;
                }
            }

            // Read first then reload
            folder.ReloadSubDirectories();
        }
    }

    public void OpenFolder(string path, bool loadSubDirs = true)
    {
        if (Directory.Exists(path))
        {
            OpenedDir = path;
        }

        CaseFolders.Clear();
        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            CaseFolders.Add(new CaseFolder(dir, loadSubDirs));
        }
    }

    public void SaveConfig(string configPath)
    {
        // Make sure it is clean
        foreach (var folder in CaseFolders)
        {
            folder.ReloadSubDirectories();
        }

        var settingsModel = new SettingsModel
        {
            OpenPath = OpenedDir,
            CaseFolders = CaseFolders.Select((x) => new SettingsCaseFolderModel(x)).ToArray()
        };

        string jsonString = JsonSerializer.Serialize(settingsModel, SourceGenerationContext.Default.SettingsModel);
        File.WriteAllText(configPath, jsonString);
    }

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
                // We don't need peaks for that
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