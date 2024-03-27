using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSpecpp.Controls;
using MSpecpp.ViewModels;

namespace MSpecpp.Views;

public partial class FolderSpectrumView : UserControl
{
    public FolderSpectrumView(FolderSpectrumViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        foreach (var topLevel in Directory.EnumerateDirectories(viewModel.Folder.Dir))
        {
            foreach (var secondLevel in Directory.EnumerateDirectories(topLevel))
            {
                MainStackPanel.Children.Add(new SpectrumCard
                {
                    DataContext = new SpectrumViewModel(secondLevel)
                });
            }
        }
    }
}