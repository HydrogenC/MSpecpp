using Avalonia.Controls;
using Avalonia.Interactivity;
using MSpecpp.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;

namespace MSpecpp.Views;

public partial class MainView : UserControl
{
    // To restore the peak counts when show peaks are enabled
    private int peakCountTemp = 5;

    public MainView()
    {
        InitializeComponent();

        ConfirmCommand = new(ConfirmCurrentChanges);
        SelectTopCommand = new RelayCommand(SelectTop);
    }

    private void BeginMoveWindow(object? sender, PointerPressedEventArgs e)
    {
        this.FindAncestorOfType<Window>()!.BeginMoveDrag(e);
    }

    private TextBlock PlaceholderBlock => new()
    {
        Text = "Nothing Selected",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

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

        SpectrumViewer.Content = PlaceholderBlock;
        MainViewModel.Instance.OpenFolder(selected[0].Path.LocalPath);
    }

    private void FolderSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FolderSelector.SelectedIndex < 0 ||
            FolderSelector.SelectedIndex >= MainViewModel.Instance.CaseFolders.Count)
        {
            SpectrumViewer.Content = PlaceholderBlock;
            return;
        }

        SpectrumViewer.Content = LoadFolder(FolderSelector.SelectedIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FolderSpectrumView LoadFolder(int index)
    {
        return new FolderSpectrumView
        {
            DataContext = new FolderSpectrumViewModel(MainViewModel.Instance.CaseFolders[index])
        };
    }

    private void MaximizeOrRestoreWindow(object? sender, TappedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        if (window != null)
        {
            window.WindowState =
                window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ConfirmCurrentChanges();
    }

    public void ConfirmCurrentChanges()
    {
        if (SpectrumViewer.Content is FolderSpectrumView view && FolderSelector.SelectedIndex >= 0 &&
            FolderSelector.SelectedIndex < MainViewModel.Instance.CaseFolders.Count)
        {
            MainViewModel.Instance.CaseFolders[FolderSelector.SelectedIndex].Confirmed = true;
            if (FolderSelector.SelectedIndex + 1 < MainViewModel.Instance.CaseFolders.Count)
            {
                FolderSelector.SelectedIndex++;
            }
        }

        // Save config
        MainViewModel.Instance.SaveConfig(SettingsModel.DefaultConfigPath);
    }

    public RelayCommand ConfirmCommand { get; private set; }

    public RelayCommand SelectTopCommand { get; private set; }

    private void SelectTopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectTop();
    }

    public void SelectTop()
    {
        if (SpectrumViewer.Content is FolderSpectrumView view)
        {
            view.SelectTop(MainViewModel.Instance.TargetSelectionCount);
        }
    }

    private async void ExportTxtButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };

        var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        // An extra guard to prevent multi-selection
        if (selected.Count != 1)
        {
            return;
        }

        FullscreenOverlay.IsVisible = true;
        _ = Task.Run(() => MainViewModel.Instance.ExportSelectedToText(selected[0].Path.LocalPath, ProgressCallback));
    }

    private void ProgressCallback(int current, int total)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (current < total)
            {
                ExportProgressBar.Value = (100.0 * current) / total;
                ExportLabel.Text = $"Exporting case {current} of {total}...";
            }
            else
            {
                FullscreenOverlay.IsVisible = false;
                ExportProgressBar.Value = 0;
                ExportLabel.Text = "Exporting";
            }
        });
    }

    private void ShowPeaksBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ShowPeaksBox.IsChecked.Value)
        {
            MainViewModel.Instance.PeakCount = peakCountTemp;
        }
        else
        {
            MainViewModel.Instance.PeakCount = 0;
        }
    }
}