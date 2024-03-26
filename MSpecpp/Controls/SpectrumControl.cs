using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MSpecpp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MSpecpp.Controls
{
    internal class SpectrumControl : Control
    {
        private WriteableBitmap? bitmap;
        private int sampleCount;
        private int[] bitmapData = [];
        const int resolution = 2;
        
        public override void Render(DrawingContext context)
        {
            var bitmap = GetBitmap();
            if (bitmap != null)
            {
                Array.Clear(bitmapData, 0, bitmapData.Length);
                var viewModel = (SpectrumViewModel?)DataContext;
                if (viewModel?.MainSpectrum is { Values.Length: > 0 })
                {
                    SpectrumValue[] spec = viewModel.MainSpectrum!.Values;

                    float[] intensities = spec.Select((x) => x.Intensity).ToArray();
                    float maxHeight = intensities.Max();
                    sampleCount = (int)(spec.Length * MainViewModel.Instance.ViewScale);

                    int endSample = 0;
                    float prevYMax = 0, prevYMin = 0;
                    for (int i = 0; i < bitmap.PixelSize.Width; i += resolution)
                    {
                        int endPoint = Math.Clamp(i + resolution, 0, bitmap.PixelSize.Width);
                        int startSample = endSample;
                        endSample = (int)((double)endPoint / bitmap.PixelSize.Width * sampleCount) - 1;
                        var segment = new ArraySegment<float>(intensities, startSample, endSample - startSample);
                        float yMax = Math.Clamp(segment.Max() / maxHeight * bitmap.PixelSize.Height, 0,
                            bitmap.PixelSize.Height - 1);
                        float yMin = Math.Clamp(segment.Min() / maxHeight * bitmap.PixelSize.Height, 0,
                            bitmap.PixelSize.Height - 1);
                        DrawPeak(bitmapData, bitmap.PixelSize, endPoint - i, i,
                            (int)Math.Round(Math.Min(yMin, prevYMax)), (int)Math.Round(Math.Max(yMax, prevYMin)));
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
            const int color = 0x7F77FF77;

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