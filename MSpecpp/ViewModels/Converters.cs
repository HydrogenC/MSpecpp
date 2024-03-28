using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MSpecpp.ViewModels;

public static class Converters
{
    public static FuncMultiValueConverter<int, IBrush> CountTextColorConverter { get; } =
        new((x) =>
        {
            // x: count and target count
            object? brush;
            if (x.First() != x.Last())
            {
                Application.Current.TryGetResource("Brush.TooFew", Application.Current.ActualThemeVariant, out brush);
            }
            else
            {
                Application.Current.TryGetResource("Brush.Enough", Application.Current.ActualThemeVariant, out brush);
            }

            return brush as IBrush;
        });
}