using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MSpecpp.ViewModels;

namespace MSpecpp;

public partial class CaseFolder : ObservableObject
{
    public CaseFolder(string folderPath, bool loadSubDirs = true)
    {
        FolderPath = folderPath;
        Confirmed = false;
        if (loadSubDirs)
        {
            ReloadSubDirectories();
        }
    }

    public void ReloadSubDirectories()
    {
        foreach (var (k, v) in SelectedDict)
        {
            if (!Directory.Exists(k) || !Spectrum.ContainsBrukerFlex(k))
            {
                SelectedDict.Remove(k);
            }
        }

        foreach (var topLevel in Directory.EnumerateDirectories(FolderPath))
        {
            foreach (var secondLevel in Directory.EnumerateDirectories(topLevel))
            {
                if (Spectrum.ContainsBrukerFlex(secondLevel))
                {
                    SelectedDict.TryAdd(secondLevel, false);
                }
            }
        }

        SelectedCount = SelectedDict.Count(pair => pair.Value);
    }

    public string FolderPath { get; set; }

    public string DisplayName => FolderPath.Split(['/', '\\']).Last();

    [ObservableProperty] private int selectedCount = 0;

    [ObservableProperty] private bool confirmed;

    public Dictionary<string, bool> SelectedDict = new();

    public Spectrum[]? Spectrums { get; set; }

    public void LoadSpectrums(bool calcPeaks = false)
    {
        // Assume that every dict 
        Spectrums = new Spectrum[SelectedDict.Count];
        var iterator = SelectedDict.Zip(Enumerable.Range(0, SelectedDict.Count));
        if (calcPeaks)
        {
            Parallel.ForEach(iterator,
                (pair) =>
                {
                    Spectrums[pair.Second] = Spectrum.ReadFromBrukerFlex(pair.First.Key);
                    Spectrums[pair.Second].FindPeaks(MainViewModel.Instance.HalfWindowSize, MainViewModel.Instance.Snr);
                    Spectrums[pair.Second].CreateSegmentTree();
                });
        }
        else
        {
            Parallel.ForEach(iterator,
                (pair) =>
                {
                    Spectrums[pair.Second] = Spectrum.ReadFromBrukerFlex(pair.First.Key);
                    Spectrums[pair.Second].CreateSegmentTree();
                });
        }
    }

    public void ReleaseSpectrums()
    {
        Spectrums = null;
    }
}