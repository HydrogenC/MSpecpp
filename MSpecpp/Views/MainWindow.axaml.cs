using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup;
using Avalonia.Media;
using MSpecpp.ViewModels;

namespace MSpecpp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    public static FuncValueConverter<WindowState, StreamGeometry> ToMaxOrRestoreIcon =
        new (state =>
        {
            if (state == WindowState.Maximized)
            {
                return Application.Current?.FindResource("Icons.Window.Restore") as StreamGeometry;
            }
            else
            {
                return Application.Current?.FindResource("Icons.Window.Maximize") as StreamGeometry;
            }
        });
}
