namespace MSpecpp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public static MainViewModel Instance
    {
        get;
        set;
    }
    
    private string info = "Press button to read mass spectrum!";
    private string title = "MSpec++";

    public string Information
    {
        get => info;
        set => SetProperty(ref info, value);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public double ViewScale { get; set; } = 1;
}