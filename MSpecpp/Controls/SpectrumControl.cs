using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MSpecpp.ViewModels;
using System;
using System.Collections.Generic;
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
            AvaloniaProperty.Register<SpectrumControl, SpectrumViewport>(nameof(ViewportSize));

        public static readonly StyledProperty<Color> ForegroundProperty =
            AvaloniaProperty.Register<SpectrumControl, Color>(nameof(Foreground), Colors.White);

        public SpectrumControl()
        {
            // Since the spectrum viewport is passed by reference, we don't need to assign
            WeakReferenceMessenger.Default.Register<SpectrumViewport>(this,
                (r, m) => { Dispatcher.UIThread.Post(DoUpdate); });
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
        private int[] bitmapData = [];
        public const int Resolution = 1;

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
            if (DataContext is Spectrum { Values.Length: > 0 } newSpec)
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
                RedrawBitmap(size, (int)foreground.ToUInt32());
                Dispatcher.UIThread.Post(InvalidateVisual);
            });
        }

        private void RedrawBitmap(SpectrumViewport viewportSize, int color)
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
                    int startIndex = (int)(spectrum.Values.Length * viewportSize.StartPos);
                    int endIndex = (int)(spectrum.Values.Length * viewportSize.EndPos);
                    float yLowerLimit = viewportSize.YLowerBound;
                    float yViewAspect = viewportSize.YHigherBound - viewportSize.YLowerBound;

                    var spec = new ArraySegment<SpectrumValue>(spectrum.Values, startIndex,
                        Math.Clamp(endIndex - startIndex, 1, int.MaxValue));
                    float[] intensities = spec.Select((x) => x.Intensity).ToArray();
                    sampleCount = spec.Count;

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
                        var segment = new ArraySegment<float>(intensities, startSample,
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
                }

                using var frameBuffer = bitmap.Lock();
                Marshal.Copy(bitmapData, 0, frameBuffer.Address, bitmapData.Length);
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
                bitmapData = new int[size.Width * size.Height];
            }

            return bitmap;
        }

        /// <summary>
        /// Reference: https://github.com/stakira/OpenUtau/blob/master/OpenUtau/Controls/WaveformImage.cs#L139
        /// </summary>
        private void DrawPeak(int[] data, PixelSize size, int drawWidth, int x, int y1, int y2, int color)
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