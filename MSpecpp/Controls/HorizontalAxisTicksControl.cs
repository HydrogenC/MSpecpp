using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using MSpecpp.ViewModels;

namespace MSpecpp.Controls;

public class HorizontalAxisTicksControl : Control
{
    public static readonly StyledProperty<Spectrum> AssociatedSpectrumProperty =
        AvaloniaProperty.Register<SpectrumControl, Spectrum>(nameof(AssociatedSpectrum));

    public static readonly StyledProperty<IPen> StrokeProperty =
        AvaloniaProperty.Register<SpectrumControl, IPen>(nameof(Stroke));

    private Typeface fontTypeface;
    private const int fontSize = 12;

    public Spectrum AssociatedSpectrum
    {
        get => GetValue(AssociatedSpectrumProperty);
        set => SetValue(AssociatedSpectrumProperty, value);
    }

    public IPen Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public HorizontalAxisTicksControl()
    {
        // Since the spectrum viewport is passed by reference, we don't need to assign
        WeakReferenceMessenger.Default.Register<SpectrumViewport>(this,
            (r, m) => { Dispatcher.UIThread.Post(InvalidateVisual); });
        fontTypeface = new Typeface("Arial");
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = Bounds.WithX(0).WithY(0);

        // I believe local variables are faster than properties
        float seriesMinMass = AssociatedSpectrum.Masses.First();
        float seriesMassAspect = AssociatedSpectrum.Masses.Last() - seriesMinMass;
        float startMass = seriesMinMass + (MainViewModel.Instance.ViewportSize.StartPos * seriesMassAspect);
        float endMass = seriesMinMass + (MainViewModel.Instance.ViewportSize.EndPos * seriesMassAspect);
        float xAspect = endMass - startMass;
        var tickSpan = xAspect < 200 ? 1 : 10;
        float massPosition = MathF.Ceiling(startMass / tickSpan) * tickSpan;
        var textBrush = Stroke.Brush;

        while (massPosition <= endMass)
        {
            float xPos = (massPosition - startMass) / xAspect * (float)rect.Width;
            float lineLength = massPosition % (5 * tickSpan) == 0 ? 0.4f : 0.2f;

            var originOfLine = Math.Clamp(xPos - Stroke.Thickness / 2, 0, rect.Width - 1);
            context.DrawLine(Stroke, new Point(originOfLine, 0),
                new Point(originOfLine, lineLength * rect.Height));
            if (massPosition % (10 * tickSpan) == 0)
            {
                var formattedText = new FormattedText($"{massPosition:0}", CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    fontTypeface, fontSize, textBrush);

                context.DrawText(formattedText, new Point(xPos, 0.4*rect.Height));
            }

            massPosition += tickSpan;
        }
    }
}