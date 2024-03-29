using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MSpecpp.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace MSpecpp.Controls
{
    internal class SpectrumControl : Control
    {
        public static readonly StyledProperty<SpectrumViewport> ViewportSizeProperty =
            AvaloniaProperty.Register<SpectrumControl, SpectrumViewport>(nameof(ViewportSize), SpectrumViewport.Dummy);

        public static readonly StyledProperty<Color> ForegroundProperty =
            AvaloniaProperty.Register<SpectrumControl, Color>(nameof(Foreground), Colors.White);

        public SpectrumControl()
        {
            // Since the spectrum viewport is passed by reference, we don't need to assign
            WeakReferenceMessenger.Default.Register<SpectrumViewport>(this,
                (r, m) => { Dispatcher.UIThread.Post(DoUpdate); });
            fontTypeface = new Typeface("Arial");

            object? brushResource;
            TryGetResource("Brush.FG2", ActualThemeVariant, out brushResource);
            textBrush = brushResource as IBrush;
        }

        public SpectrumViewport ViewportSize
        {
            get => GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        public Color Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private WriteableBitmap? bitmap;
        private Spectrum? spectrum;
        private int sampleCount;
        private uint[] bitmapData = [];
        public const int Resolution = 1;
        private Typeface fontTypeface;
        private IBrush textBrush;
        private const int fontSize = 12;
        PriorityQueue<int, float> peaksToDraw = new();

        // To avoid data race on bitmap when the bounds are changing quickly
        private Mutex renderMutex = new(false);
        private int renderTasksAlive = 0;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty ||
                change.Property == BoundsProperty ||
                change.Property == ViewportSizeProperty ||
                change.Property == ForegroundProperty)
            {
                // In case this is called from other threads
                Dispatcher.UIThread.Post(DoUpdate);
            }
        }

        public void DoUpdate()
        {
            if (DataContext is Spectrum { Length: > 0 } newSpec)
            {
                spectrum = newSpec;
            }
            else
            {
                spectrum = null;
            }

            // Allow up to two tasks for most: one running and one queued
            if (renderTasksAlive >= 2)
            {
                return;
            }

            SpectrumViewport size = ViewportSize;
            Color foreground = Foreground;
            bitmap = null;
            Task.Run(() =>
            {
                RedrawBitmap(size, foreground.ToUInt32());
                Dispatcher.UIThread.Post(InvalidateVisual);
            });
        }

        private unsafe void RedrawBitmap(SpectrumViewport viewportSize, uint color)
        {
            Interlocked.Increment(ref renderTasksAlive);
            renderMutex.WaitOne();
            var bitmap = GetBitmap();
            if (bitmap != null)
            {
                Array.Clear(bitmapData, 0, bitmapData.Length);
                if (spectrum != null)
                {
                    // Viewport-related
                    int startIndex = (int)(spectrum.Length * viewportSize.StartPos);
                    int endIndex = (int)(spectrum.Length * viewportSize.EndPos);
                    float yLowerLimit = viewportSize.YLowerBound;
                    float yViewAspect = viewportSize.YHigherBound - yLowerLimit;
                    sampleCount = Math.Clamp(endIndex - startIndex, 1, int.MaxValue);

                    int endSample = 0;
                    float prevYMax, prevYMin;
                    prevYMax = prevYMin = -yLowerLimit / yViewAspect * bitmap.PixelSize.Height;
                    for (int i = 0; i < bitmap.PixelSize.Width; i += Resolution)
                    {
                        int endPoint = Math.Clamp(i + Resolution, 0, bitmap.PixelSize.Width);
                        int startSample = endSample;
                        // Avoid negative values
                        endSample = Math.Clamp((int)((double)endPoint / bitmap.PixelSize.Width * sampleCount) - 1,
                            startSample, int.MaxValue);
                        // Make sure there is at least one sample
                        var segment = new ArraySegment<float>(spectrum.Intensities, startIndex + startSample,
                            Math.Clamp(endSample - startSample, 1, int.MaxValue));
                        float yMax = Math.Clamp((segment.Max() - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        float yMin = Math.Clamp((segment.Min() - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        DrawPeak(bitmapData, bitmap.PixelSize, endPoint - i, i,
                            (int)MathF.Round(Math.Min(yMin, prevYMax)),
                            (int)MathF.Round(Math.Max(yMax, prevYMin)), color);
                        (prevYMax, prevYMin) = (yMax, yMin);
                    }

                    peaksToDraw.Clear();
                    if (spectrum.Peaks != null && MainViewModel.Instance.ShowPeaks)
                    {
                        const int peakCount = 5;
                        int startPeakIndex = Array.FindIndex(spectrum.Peaks, (x) => x >= startIndex);
                        int endPeakIndex = Array.FindLastIndex(spectrum.Peaks, (x) => x < endIndex);

                        for (int i = startPeakIndex; i <= endPeakIndex; i++)
                        {
                            peaksToDraw.Enqueue(spectrum.Peaks[i], spectrum.Intensities[spectrum.Peaks[i]]);

                            if (peaksToDraw.Count > peakCount)
                            {
                                peaksToDraw.Dequeue();
                            }
                        }
                    }
                }

                using var frameBuffer = bitmap.Lock();
                fixed (uint* bitmapPtr = &bitmapData[0])
                {
                    long sizeToCopy = bitmapData.Length * sizeof(uint);
                    Buffer.MemoryCopy((void*)bitmapPtr, (void*)frameBuffer.Address, sizeToCopy, sizeToCopy);
                }
            }

            renderMutex.ReleaseMutex();
            Interlocked.Decrement(ref renderTasksAlive);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // context.DrawRectangle(new SolidColorBrush(Colors.Red), null, Bounds.WithX(0).WithY(0));
            if (bitmap != null)
            {
                var rect = Bounds.WithX(0).WithY(0);
                context.DrawImage(bitmap, rect, rect);

                if (peaksToDraw.Count > 0)
                {
                    float yHigherLimit = ViewportSize.YHigherBound;
                    float yViewAspect = yHigherLimit - ViewportSize.YLowerBound;
                    int startIndex = (int)(spectrum.Length * ViewportSize.StartPos);
                    int xIndexAspect = (int)(spectrum.Length * (ViewportSize.EndPos - ViewportSize.StartPos));

                    var peaks = peaksToDraw.UnorderedItems.ToArray();
                    foreach (var peak in peaks)
                    {
                        float peakY = Math.Clamp(
                            (yHigherLimit - peak.Priority) / yViewAspect * (float)rect.Height,
                            0, (float)rect.Height - 1);
                        float peakX = (peak.Element - startIndex) / xIndexAspect * (float)rect.Width;
                        var formattedText = new FormattedText($"{spectrum.Masses[peak.Element]:0.000}",
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            fontTypeface, fontSize, textBrush);

                        context.DrawText(formattedText, new Point(
                            Math.Clamp(peakX - 0.5f * formattedText.Width, 0, rect.Width),
                            Math.Clamp(peakY - formattedText.Height, 0, rect.Height)));
                    }
                }
            }
        }

        private WriteableBitmap? GetBitmap()
        {
            int desiredWidth = (int)Bounds.Width;
            int desiredHeight = (int)Bounds.Height;
            if (desiredWidth == 0 || desiredHeight == 0)
            {
                return null;
            }

            if (bitmap == null || bitmap.Size.Width < desiredWidth)
            {
                bitmap?.Dispose();
                var size = new PixelSize(desiredWidth, desiredHeight);
                bitmap = new WriteableBitmap(
                    size, new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Rgba8888,
                    Avalonia.Platform.AlphaFormat.Unpremul);
                bitmapData = new uint[size.Width * size.Height];
            }

            return bitmap;
        }

        /// <summary>
        /// Reference: https://github.com/stakira/OpenUtau/blob/master/OpenUtau/Controls/WaveformImage.cs#L139
        /// </summary>
        private void DrawPeak(uint[] data, PixelSize size, int drawWidth, int x, int y1, int y2, uint color)
        {
            // Draw the spectrum from down to up
            (y1, y2) = (size.Height - y1 - 1, size.Height - y2 - 1);
            if (y1 > y2)
            {
                (y1, y2) = (y2, y1);
            }

            for (var w = x; w < x + drawWidth; ++w)
            {
                for (var y = y1; y <= y2; ++y)
                {
                    data[w + size.Width * y] = color;
                }
            }
        }
    }
}