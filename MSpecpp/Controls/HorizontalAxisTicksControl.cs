using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private bool isPressing = false;
    private Point? pressedPosition = null;

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
        WeakReferenceMessenger.Default.Register<SpectrumViewportRefreshMessage>(this,
            (r, m) => { Dispatcher.UIThread.Post(InvalidateVisual); });
        fontTypeface = new Typeface("Arial");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount >= 2)
        {
            // Reset viewport
            MainViewModel.Instance.ViewportSize.UpdateViewport(start: 0, end: 1);
        }
        else
        {
            isPressing = true;
            pressedPosition = e.GetPosition(this);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        isPressing = false;
        pressedPosition = null;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        isPressing = false;
        pressedPosition = null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (isPressing && pressedPosition != null)
        {
            var rect = Bounds.WithX(0).WithY(0);
            var pos = e.GetPosition(this);
            float deltaMass = (float)((pressedPosition.Value.X - pos.X) / rect.Width) *
                              MainViewModel.Instance.ViewportSize.ViewportMassAspect;

            float seriesMassAspect = MainViewModel.Instance.ViewportSize.SeriesMassAspect;
            MainViewModel.Instance.ViewportSize.UpdateViewport(
                start: Math.Clamp(
                    MainViewModel.Instance.ViewportSize.StartPos +
                    deltaMass / seriesMassAspect, 0, 1),
                end: Math.Clamp(
                    MainViewModel.Instance.ViewportSize.EndPos +
                    deltaMass / seriesMassAspect, 0, 1));

            pressedPosition = pos;
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Cursor = new Cursor(StandardCursorType.SizeWestEast);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        int direction = Math.Sign(e.Delta.Y);
        if (direction == 0)
        {
            return;
        }

        var rect = Bounds.WithX(0).WithY(0);
        var pos = e.GetPosition(this);
        float startMass = MainViewModel.Instance.ViewportSize.StartMass,
            seriesMassAspect = MainViewModel.Instance.ViewportSize.SeriesMassAspect;
        float pointedMass =
            startMass + (float)(pos.X / rect.Width) * MainViewModel.Instance.ViewportSize.ViewportMassAspect;

        float factor = direction > 0 ? (1 / 0.8f) : 0.8f;
        float newStartMass = pointedMass - (pointedMass - startMass) * factor;
        float newEndMass = pointedMass + (MainViewModel.Instance.ViewportSize.EndMass - pointedMass) * factor;

        float seriesMinMass = MainViewModel.Instance.ViewportSize.SeriesMinMass;
        MainViewModel.Instance.ViewportSize.UpdateViewport(
            start: Math.Clamp(
                (newStartMass - seriesMinMass) / seriesMassAspect, 0, 1),
            end: Math.Clamp(
                (newEndMass - seriesMinMass) / seriesMassAspect, 0, 1));

        // Avoid wheel to be captured by scrollview
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var rect = Bounds.WithX(0).WithY(0);
        // Allow the control to be hittested
        context.FillRectangle(Brushes.Transparent, rect);

        // Start and end mass of viewport
        float startMass = MainViewModel.Instance.ViewportSize.StartMass;
        float endMass = MainViewModel.Instance.ViewportSize.EndMass;
        float massAspect = endMass - startMass;
        var tickSpan = massAspect < 200 ? 1 : 10;
        float massPosition = MathF.Ceiling(startMass / tickSpan) * tickSpan;
        var textBrush = Stroke.Brush;

        while (massPosition <= endMass)
        {
            float xPos = (massPosition - startMass) / massAspect * (float)rect.Width;
            float lineLength = massPosition % (5 * tickSpan) == 0 ? 0.4f : 0.2f;

            var originOfLine = Math.Clamp(xPos - Stroke.Thickness / 2, 0, rect.Width - 1);
            context.DrawLine(Stroke, new Point(originOfLine, 0),
                new Point(originOfLine, lineLength * rect.Height));
            if (massPosition % (10 * tickSpan) == 0)
            {
                var formattedText = new FormattedText($"{massPosition:0}", CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    fontTypeface, fontSize, textBrush);

                context.DrawText(formattedText, new Point(xPos, 0.4 * rect.Height));
            }

            massPosition += tickSpan;
        }
    }
}