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
using System.Threading.Tasks;

namespace MSpecpp.Controls
{
    internal class SpectrumControl : Control
    {
        public static readonly StyledProperty<float> StartMassProperty =
            AvaloniaProperty.Register<SpectrumControl, float>(nameof(StartMass), 80);

        public static readonly StyledProperty<float> EndMassProperty =
            AvaloniaProperty.Register<SpectrumControl, float>(nameof(EndMass), 1000);

        public static readonly StyledProperty<Color> ForegroundProperty =
            AvaloniaProperty.Register<SpectrumControl, Color>(nameof(Foreground), Colors.White);

        public float StartMass
        {
            get => GetValue(StartMassProperty);
            set => SetValue(StartMassProperty, value);
        }

        public float EndMass
        {
            get => GetValue(EndMassProperty);
            set => SetValue(EndMassProperty, value);
        }

        public Color Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private WriteableBitmap? bitmap;
        private int sampleCount;
        private int[] bitmapData = [];
        const int resolution = 1;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty ||
                change.Property == BoundsProperty ||
                change.Property == StartMassProperty ||
                change.Property == EndMassProperty ||
                change.Property == ForegroundProperty)
            {
                InvalidateVisual();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GraphYToImageY(float y, float yLowerBound, float yAspect, float bitmapHeight)
        {
            return (int)MathF.Round((y - yLowerBound) * bitmapHeight / yAspect);
        }

        public override void Render(DrawingContext context)
        {
            var bitmap = GetBitmap();
            if (bitmap != null)
            {
                Array.Clear(bitmapData, 0, bitmapData.Length);
                if (DataContext is Spectrum { Values.Length: > 0 } spectrum)
                {
                    // Assign these to local variables since properties are slow
                    float startMass = StartMass, endMass = EndMass;
                    int startIndex = Array.FindIndex(spectrum.Values, (x) => x.Mass >= startMass);
                    int endIndex = Array.FindLastIndex(spectrum.Values, (x) => x.Mass <= endMass);
                    var spec = new ArraySegment<SpectrumValue>(spectrum.Values, startIndex, endIndex - startIndex + 1);

                    float[] intensities = spec.Select((x) => x.Intensity).ToArray();
                    float maxHeight = intensities.Max();
                    float yLowerLimit = -maxHeight * 0.05f;
                    float yViewAspect = maxHeight * 1.1f;
                    sampleCount = spec.Count;

                    int endSample = 0;
                    float prevYMax, prevYMin;
                    prevYMax = prevYMin = -yLowerLimit / yViewAspect * bitmap.PixelSize.Height;
                    for (int i = 0; i < bitmap.PixelSize.Width; i += resolution)
                    {
                        int endPoint = Math.Clamp(i + resolution, 0, bitmap.PixelSize.Width);
                        int startSample = endSample;
                        endSample = (int)((double)endPoint / bitmap.PixelSize.Width * sampleCount) - 1;
                        var segment = new ArraySegment<float>(intensities, startSample, endSample - startSample);
                        float yMax = Math.Clamp((segment.Max() - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        float yMin = Math.Clamp((segment.Min() - yLowerLimit) / yViewAspect * bitmap.PixelSize.Height,
                            0, bitmap.PixelSize.Height - 1);
                        DrawPeak(bitmapData, bitmap.PixelSize, endPoint - i, i,
                            (int)MathF.Round(Math.Min(yMin, prevYMax)),
                            (int)MathF.Round(Math.Max(yMax, prevYMin)));
                        (prevYMax, prevYMin) = (yMax, yMin);
                    }
                }

                using var frameBuffer = bitmap.Lock();
                Marshal.Copy(bitmapData, 0, frameBuffer.Address, bitmapData.Length);
            }

            base.Render(context);
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
        private void DrawPeak(int[] data, PixelSize size, int drawWidth, int x, int y1, int y2)
        {
            int color = (int)Foreground.ToUInt32();

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