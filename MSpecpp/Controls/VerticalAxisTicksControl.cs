using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using MSpecpp.ViewModels;

namespace MSpecpp.Controls;

public class VerticalAxisTicksControl : Control
{
    public static readonly StyledProperty<IPen> StrokeProperty =
        AvaloniaProperty.Register<SpectrumControl, IPen>(nameof(Stroke));

    private Typeface fontTypeface;
    private const int fontSize = 12;
    private bool shouldRedrawWhenShown = false;

    public IPen Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public VerticalAxisTicksControl()
    {
        // Since the spectrum viewport is passed by reference, we don't need to assign
        WeakReferenceMessenger.Default.Register<SpectrumViewportRefreshMessage>(this,
            (r, m) =>
            {
                if (!IsOnScreen())
                {
                    shouldRedrawWhenShown = true;
                    return;
                }

                Dispatcher.UIThread.Post(InvalidateVisual);
            });
        WeakReferenceMessenger.Default.Register<ScrollViewScrolledMessage>(this, (r, m) =>
        {
            // Dispose bitmap if not visible in screen
            if (IsOnScreen() && shouldRedrawWhenShown)
            {
                InvalidateVisual();
            }
        });
        fontTypeface = new Typeface("Arial");
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        e.Handled = true;
    }

    private bool IsOnScreen()
    {
        var clipRect = this.GetTransformedBounds()!.Value.Clip;
        return clipRect.Width * clipRect.Height > 0;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!IsOnScreen())
        {
            shouldRedrawWhenShown = true;
            return;
        }

        shouldRedrawWhenShown = false;
        var rect = Bounds.WithX(0).WithY(0);
        // Allow the control to be hittested
        context.FillRectangle(Brushes.Transparent, rect);

        var viewportSize = MainViewModel.Instance.ViewportSize;
        // one tenth of the magnitude level
        var tickSpan = MathF.Pow(10, MathF.Floor(MathF.Log10(viewportSize.YHigherBound)) - 1);
        float tickIntensity = 0;
        // I believe local variables are faster than properties
        float higherBound = viewportSize.YHigherBound,
            yAspect = viewportSize.YHigherBound - viewportSize.YLowerBound;
        if (yAspect == 0)
        {
            return;
        }

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