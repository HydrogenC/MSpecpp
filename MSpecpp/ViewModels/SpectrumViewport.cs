using Avalonia;
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

public record ScrollViewScrolledMessage;

// We make this readonly to ensure every field change is modified
public class SpectrumViewport
{
    public float SeriesMinMass { get; set; }

    public float SeriesMaxMass { get; set; }

    public float StartPos { get; private set; }

    public float EndPos { get; private set; }

    public float SeriesMassAspect => SeriesMaxMass - SeriesMinMass;

    public float StartMass => SeriesMinMass + StartPos * SeriesMassAspect;

    public float EndMass => SeriesMinMass + EndPos * SeriesMassAspect;

    public float ViewportMassAspect => SeriesMassAspect * (EndPos - StartPos);

    public float YLowerBound { get; private set; }

    public float YHigherBound { get; private set; }

    public float InterpolateMass(float factor)
    {
        return StartMass + factor * ViewportMassAspect;
    }

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