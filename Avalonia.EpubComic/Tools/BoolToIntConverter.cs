using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.EpubComic.Tools;

public class BoolToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? 0 : 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value == 0;
    }
}