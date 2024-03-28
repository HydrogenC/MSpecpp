using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MSpecpp.Controls;
using MSpecpp.ViewModels;

namespace MSpecpp.Views;

public partial class FolderSpectrumView : UserControl
{
    private bool isLoaded = false;

    public FolderSpectrumView(FolderSpectrumViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        Task.Run(() => viewModel.CreateSpectrumViews(SpectrumLoadedCallback));
        MainStackPanel.Children.Add(new TextBlock
        {
            Text = "Loading...",
            HorizontalAlignment = HorizontalAlignment.Center
        });
    }

    public void SpectrumLoadedCallback()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var viewModel = DataContext as FolderSpectrumViewModel;
            MainStackPanel.Children.Clear();
            foreach (var spec in viewModel.SpectrumViewModels)
            {
                MainStackPanel.Children.Add(new SpectrumCard
                {
                    DataContext = spec
                });
            }
            
            isLoaded = true;
        });
    }

    public void SelectTop(int count)
    {
        if (!isLoaded)
        {
            return;
        }

        var viewModel = DataContext as FolderSpectrumViewModel;

        for (int i = 0; i < viewModel.SpectrumViewModels.Length; i++)
        {
            viewModel.SpectrumViewModels[i].IsSelected = i < count;
        }
    }
}