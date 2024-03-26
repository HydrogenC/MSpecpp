namespace MSpecpp.ViewModels;

public class SpectrumViewModel : ViewModelBase
{
    private Spectrum? _mainSpectrum;
    
    public Spectrum? MainSpectrum
    {
        get => _mainSpectrum; 
        set => SetProperty(ref _mainSpectrum, value);
    }

    private int _id; 
    
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
}