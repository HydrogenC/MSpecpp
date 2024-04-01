using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MSpecpp.ViewModels;
using MSpecpp.Views;

namespace MSpecpp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        SettingsModel? settings = null;
        if (File.Exists(SettingsModel.GlobalConfigPath))
        {
            string jsonString = File.ReadAllText(SettingsModel.GlobalConfigPath);
            settings = JsonSerializer.Deserialize(jsonString, SourceGenerationContext.Default.SettingsModel);
        }

        if (settings != null)
        {
            MainViewModel.Instance = new MainViewModel(settings!);
        }
        else
        {
            MainViewModel.Instance = new MainViewModel();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainViewModel.Instance
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = MainViewModel.Instance
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}