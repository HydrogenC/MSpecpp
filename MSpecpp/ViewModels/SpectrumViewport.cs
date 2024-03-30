using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MSpecpp.ViewModels;

/// <summary>
/// 
/// </summary>
/// <param name="isHorizontal">If there is any horizontal changes</param>
public record SpectrumViewportChangedMessage(bool isHorizontal);

public record SpectrumViewportRefreshMessage;

// We make this readonly to ensure every field change is modified
public class SpectrumViewport
{
    
    public float StartPos { get; private set; }

    public float EndPos { get; private set; }

    public float YLowerBound { get; private set; }

    public float YHigherBound { get; private set; }

    public void UpdateViewport(float? start = null, float? end = null, float? yHigher = null, float? yLower = null)
    {
        bool isHorizontal = start != null || end != null;
        UpdateViewportNoNotify(start, end, yHigher, yLower);
        WeakReferenceMessenger.Default.Send(new SpectrumViewportChangedMessage(isHorizontal));
    }

    public void UpdateViewportNoNotify(float? start = null, float? end = null, float? yHigher = null,
        float? yLower = null)
    {
        StartPos = start ?? StartPos;
        EndPos = end ?? EndPos;
        YHigherBound = yHigher ?? YHigherBound;
        YLowerBound = yLower ?? YLowerBound;
    }

    public static SpectrumViewport Dummy => new()
    {
        StartPos = 0,
        EndPos = 1,
        YHigherBound = 50,
        YLowerBound = -10
    };
};