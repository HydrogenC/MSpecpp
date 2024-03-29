using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using MSpecpp.ViewModels;

namespace MSpecpp.Controls;

public class VerticalAxisTicksControl : Control
{
    public static readonly StyledProperty<SpectrumViewport> ViewportSizeProperty =
        AvaloniaProperty.Register<SpectrumControl, SpectrumViewport>(nameof(ViewportSize), SpectrumViewport.Dummy);

    public static readonly StyledProperty<IPen> StrokeProperty =
        AvaloniaProperty.Register<SpectrumControl, IPen>(nameof(Stroke));

    private Typeface fontTypeface;
    private const int fontSize = 12;

    public SpectrumViewport ViewportSize
    {
        get => GetValue(ViewportSizeProperty);
        set => SetValue(ViewportSizeProperty, value);
    }

    public IPen Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public VerticalAxisTicksControl()
    {
        // Since the spectrum viewport is passed by reference, we don't need to assign
        WeakReferenceMessenger.Default.Register<SpectrumViewport>(this,
            (r, m) => { Dispatcher.UIThread.Post(InvalidateVisual); });
        fontTypeface = new Typeface("Arial");
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // one tenth of the magnitude level
        var tickSpan = MathF.Pow(10, MathF.Floor(MathF.Log10(ViewportSize.YHigherBound)) - 1);
        var rect = Bounds.WithX(0).WithY(0);

        float tickIntensity = 0;
        // I believe local variables are faster than properties
        float higherBound = ViewportSize.YHigherBound,
            yAspect = ViewportSize.YHigherBound - ViewportSize.YLowerBound;
        var textBrush = Stroke.Brush;
        for (int i = 0; tickIntensity <= higherBound; i++)
        {
            float yPos = (higherBound - tickIntensity) / yAspect * (float)rect.Height;
            float lineLength = i % 5 == 0 ? 0.2f : 0.12f;

            var originOfLine = Math.Clamp(yPos - Stroke.Thickness / 2, 0, rect.Height - 1);
            context.DrawLine(Stroke, new Point(rect.Width * (1f - lineLength), originOfLine),
                new Point(rect.Width, originOfLine));
            if (i % 10 == 0 || tickIntensity + tickSpan > higherBound)
            {
                var labelText = tickIntensity == 0 ? "0" : tickIntensity.ToString(i % 10 == 0 ? "0e0" : "0.0e0");
                var formattedText = new FormattedText(labelText, CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    fontTypeface, fontSize, textBrush);

                context.DrawText(formattedText, new Point(0, yPos - formattedText.Height * .5f));
            }

            tickIntensity += tickSpan;
        }
    }
}