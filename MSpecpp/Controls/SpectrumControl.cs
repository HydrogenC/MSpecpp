﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MSpecpp.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;

namespace MSpecpp.Controls
{
    internal class SpectrumControl : Control
    {
        public static readonly StyledProperty<int> PeakCountProperty =
            AvaloniaProperty.Register<SpectrumControl, int>(nameof(PeakCount), 0);

        public static readonly StyledProperty<Color> ForegroundProperty =
            AvaloniaProperty.Register<SpectrumControl, Color>(nameof(Foreground));

        public static readonly StyledProperty<int> StartIndexProperty =
            AvaloniaProperty.Register<SpectrumControl, int>(nameof(StartIndex));

        public static readonly StyledProperty<int> EndIndexProperty =
            AvaloniaProperty.Register<SpectrumControl, int>(nameof(EndIndex));

        public SpectrumControl()
        {
            // Since the spectrum viewport is passed by reference, we don't need to assign
            WeakReferenceMessenger.Default.Register<SpectrumViewportRefreshMessage>(this,
                (r, m) => { Dispatcher.UIThread.Post(DoUpdate); });
            WeakReferenceMessenger.Default.Register<ScrollViewScrolledMessage>(this, (r, m) =>
            {
                // Dispose bitmap if not visible in screen
                if (IsOnScreen())
                {
                    if (bitmap == null)
                    {
                        Dispatcher.UIThread.Post(DoUpdate);
                    }
                }
                else
                {
                    if (bitmap != null)
                    {
                        // Dispose bitmap to save space
                        // Avoid data race with the render thread
                        lock (renderLock)
                        {
                            bitmap.Dispose();
                            bitmap = null;
                        }
                    }
                }
            });
        }

        public Color Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public int PeakCount
        {
            get => GetValue(PeakCountProperty);
            set => SetValue(PeakCountProperty, value);
        }

        public int StartIndex
        {
            get => GetValue(StartIndexProperty);
            set => SetValue(StartIndexProperty, value);
        }

        public int EndIndex
        {
            get => GetValue(EndIndexProperty);
            set => SetValue(EndIndexProperty, value);
        }

        private WriteableBitmap? bitmap;
        private Spectrum? spectrum;
        public const int Resolution = 1;

        // To avoid data race on bitmap when the bounds are changing quickly
        private readonly object renderLock = new();
        private readonly object incrementLock = new();
        private int renderTasksAlive = 0;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty ||
                change.Property == BoundsProperty ||
                change.Property == ForegroundProperty ||
                change.Property == PeakCountProperty)
            {
                // In case this is called from other threads
                Dispatcher.UIThread.Post(DoUpdate);
            }
        }

        private bool IsOnScreen()
        {
            var clipRect = this.GetTransformedBounds()!.Value.Clip;
            return clipRect.Width * clipRect.Height > 0;
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


            if (!IsOnScreen())
            {
                return;
            }

            lock (incrementLock)
            {
                // Allow up to two tasks for most: one running and one queued
                if (renderTasksAlive >= 2)
                {
                    return;
                }

                renderTasksAlive++;
            }

            SpectrumViewport size = MainViewModel.Instance.ViewportSize;
            uint foreground = Foreground.ToUInt32();
            int peakCount = PeakCount;
            int startIndex = StartIndex, endIndex = EndIndex;
            bitmap = null;
            Task.Run(() =>
            {
                RedrawBitmap(size, startIndex, endIndex, foreground, peakCount);
                Dispatcher.UIThread.Post(InvalidateVisual);
            });
        }

        private unsafe void RedrawBitmap(SpectrumViewport viewportSize, int startIndex, int endIndex, uint color,
            int peakCount = 0)
        {
            lock (renderLock)
            {
                var bitmap = GetBitmap();
                if (bitmap == null || spectrum == null)
                {
                    return;
                }

                using var frameBuffer = bitmap.Lock();
                using var skBitmap = new SKBitmap(bitmap.PixelSize.Width, bitmap.PixelSize.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Unpremul);
                skBitmap.SetPixels(frameBuffer.Address);
                uint* rawPtr = (uint*)skBitmap.GetPixels();

                float yLowerLimit = viewportSize.YLowerBound, yHigherLimit = viewportSize.YHigherBound;
                float yViewAspect = yHigherLimit - yLowerLimit;
                if (yViewAspect != 0)
                {
                    int sampleEnd = startIndex;
                    float prevYMax, prevYMin;
                    prevYMax = prevYMin = -yLowerLimit / yViewAspect * bitmap.PixelSize.Height;

                    // Draw spectrum
                    for (int i = 0; i < bitmap.PixelSize.Width; i += Resolution)
                    {
                        int endPoint = Math.Clamp(i + Resolution, 0, bitmap.PixelSize.Width);
                        float endMass =
                            MainViewModel.Instance.ViewportSize.InterpolateMass(endPoint /
                                (float)bitmap.PixelSize.Width);
                        int sampleStart = sampleEnd;
                        if (sampleStart < 0)
                        {
                            continue;
                        }

                        // Use larger-or-equal to guarantee that it can always find an index
                        sampleEnd = Array.FindIndex(spectrum.Masses, sampleStart, (x) => x >= endMass);
                        if (sampleEnd < 0)
                        {
                            continue;
                        }

                        // Make sure there is at least one sample
                        var (intensMin, intensMax) =
                            spectrum.QueryTree.QueryMinMax(sampleStart,
                                sampleEnd > sampleStart ? sampleEnd : (sampleStart + 1));
                        float yMax = Math.Clamp(
                            (intensMax - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        float yMin = Math.Clamp(
                            (intensMin - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        DrawPeak(rawPtr, bitmap.PixelSize, endPoint - i, i,
                            (int)MathF.Round(Math.Min(yMin, prevYMax)),
                            (int)MathF.Round(Math.Max(yMax, prevYMin)), color);
                        (prevYMax, prevYMin) = (yMax, yMin);
                    }

                    if (spectrum.Peaks == null || peakCount <= 0)
                    {
                        return;
                    }

                    // Annotate peaks
                    PriorityQueue<int, float> peaksToDraw = new();
                    // Peaks isn't too large, so I suppose O(n) finding is OK
                    int startPeakIndex = Array.FindIndex(spectrum.Peaks, (x) => x >= startIndex);
                    int endPeakIndex = Array.FindLastIndex(spectrum.Peaks, (x) => x < endIndex);

                    if (startPeakIndex >= 0 && endPeakIndex >= 0)
                    {
                        for (int i = startPeakIndex; i <= endPeakIndex; i++)
                        {
                            peaksToDraw.Enqueue(spectrum.Peaks[i], spectrum.Intensities[spectrum.Peaks[i]]);

                            if (peaksToDraw.Count > peakCount)
                            {
                                peaksToDraw.Dequeue();
                            }
                        }

                        using SKPaint textPaint = new SKPaint();
                        textPaint.TextSize = 13;
                        textPaint.IsAntialias = true;
                        textPaint.Color = new SKColor(color);
                        textPaint.Typeface = SKTypeface.FromFamilyName(
                            "Arial",
                            SKFontStyleWeight.Normal,
                            SKFontStyleWidth.Normal,
                            SKFontStyleSlant.Upright);

                        using SKCanvas bitmapCanvas = new SKCanvas(skBitmap);
                        SKRect bounds = new(), prevBounds = SKRect.Empty;
                        float startMass = MainViewModel.Instance.ViewportSize.StartMass,
                            massAspect = MainViewModel.Instance.ViewportSize.ViewportMassAspect;
                        var sortedPeaks = peaksToDraw.UnorderedItems.ToArray();
                        Array.Sort(sortedPeaks, (a, b) => a.Element.CompareTo(b.Element));
                        foreach (var peak in sortedPeaks)
                        {
                            float peakY = Math.Clamp(
                                (yHigherLimit - peak.Priority) / yViewAspect * bitmap.PixelSize.Height,
                                0, bitmap.PixelSize.Height - 1);
                            float peakX = (spectrum.Masses[peak.Element] - startMass) / massAspect *
                                          bitmap.PixelSize.Width;

                            string textToWrite = $"{spectrum.Masses[peak.Element]:0.000}";
                            textPaint.MeasureText(textToWrite, ref bounds);
                            bounds.Location = new SKPoint(
                                Math.Clamp(peakX - 0.5f * bounds.Width, 0, bitmap.PixelSize.Width),
                                Math.Clamp(peakY - bounds.Height, 0, bitmap.PixelSize.Height));
                            if (bounds.IntersectsWith(prevBounds))
                            {
                                bounds.Location = bounds.Location with
                                {
                                    Y = Math.Clamp(prevBounds.Location.Y - bounds.Height - 2, 0,
                                        bitmap.PixelSize.Height)
                                };
                            }

                            bitmapCanvas.DrawText(textToWrite, bounds.Location, textPaint);
                            prevBounds = bounds;
                        }
                    }
                }

                Interlocked.Decrement(ref renderTasksAlive);
            }
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
            }

            return bitmap;
        }

        /// <summary>
        /// Reference: https://github.com/stakira/OpenUtau/blob/master/OpenUtau/Controls/WaveformImage.cs#L139
        /// </summary>
        private unsafe void DrawPeak(uint* data, PixelSize size, int drawWidth, int x, int y1, int y2, uint color)
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