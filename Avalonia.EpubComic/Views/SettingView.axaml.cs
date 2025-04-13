using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Avalonia.EpubComic.Views;

public partial class SettingView1 : UserControl
{
    public SettingView1()
    {
        InitializeComponent();
    }
}

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