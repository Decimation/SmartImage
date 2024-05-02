﻿// Read S SmartImage.UI InvertableBooleanToVisibilityConverter.cs
// 2023-09-02 @ 3:46 AM

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartImage.UI.Controls.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InvertableBooleanToVisibilityConverter : IValueConverter
{
    public enum Parameters
    {
        Normal, Inverted
    }

    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
        var boolValue = (bool)value;
        var direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);

        if (direction == Parameters.Inverted)
            return !boolValue ? Visibility.Visible : Visibility.Hidden;

        return boolValue ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
    {
        return null;
    }
}