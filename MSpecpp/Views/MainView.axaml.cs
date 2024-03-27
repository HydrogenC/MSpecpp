using Avalonia.Controls;
using Avalonia.Interactivity;
using MSpecpp.ViewModels;
using System;
using System.IO;
using System.Linq;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace MSpecpp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void BeginMoveWindow(object? sender, PointerPressedEventArgs e)
    {
        this.FindAncestorOfType<Window>()!.BeginMoveDrag(e);
    }

    private async void OpenFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };

        var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        // An extra guard to prevent multi-selection
        if (selected.Count != 1)
        {
            return;
        }

        MainViewModel.Instance.CaseFolders.Clear();
        MainViewModel.Instance.OpenedDir = selected[0].Path.LocalPath;
        foreach (var dir in Directory.EnumerateDirectories(selected[0].Path.LocalPath))
        {
            MainViewModel.Instance.CaseFolders.Add(new CaseFolder
            {
                Dir = dir
            });
        }
    }

    private void FolderSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FolderSelector.SelectedIndex < 0 ||
            FolderSelector.SelectedIndex >= MainViewModel.Instance.CaseFolders.Count)
        {
            SpectrumViewer.Content = new TextBlock
            {
                Text = "Nothing Selected",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return;
        }

        SpectrumViewer.Content =
            new FolderSpectrumView(
                new FolderSpectrumViewModel(MainViewModel.Instance.CaseFolders[FolderSelector.SelectedIndex]));
    }

    private void MaximizeOrRestoreWindow(object? sender, TappedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        if (window != null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}