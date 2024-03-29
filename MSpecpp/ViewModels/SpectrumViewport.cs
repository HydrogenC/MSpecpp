using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MSpecpp.ViewModels;

public class SpectrumViewportChangedMessage(SpectrumViewport s) : ValueChangedMessage<SpectrumViewport>(s);

// We make this readonly to ensure every field change is modified
public partial class SpectrumViewport : ObservableObject
{
    [ObservableProperty] private float startPos;

    [ObservableProperty] private float endPos;

    [ObservableProperty] private float yLowerBound;

    [ObservableProperty] private float yHigherBound;

    public static SpectrumViewport Dummy => new()
    {
        StartPos = 0,
        EndPos = 1,
        YHigherBound = 50,
        YLowerBound = -10
    };
};