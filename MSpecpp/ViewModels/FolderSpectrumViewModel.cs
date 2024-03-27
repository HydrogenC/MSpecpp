namespace MSpecpp.ViewModels;

public class FolderSpectrumViewModel : ViewModelBase
{
    public CaseFolder Folder { get; init; }

    public FolderSpectrumViewModel(CaseFolder associatedFolder)
    {
        Folder = associatedFolder;
    }
}