using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using SmartImage.Lib.Model;
using SmartImage.UI.Model;

namespace SmartImage.UI.Controls.Converters;

public class BoolToValueConverter<T> : IValueConverter
{

    public T FalseValue { get; set; }

    public T TrueValue { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return FalseValue;

        return (bool)value ? TrueValue : FalseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
                              CultureInfo culture)
    {
        return value != null ? value.Equals(TrueValue) : false;
    }

}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToStringConverter : BoolToValueConverter<string> { }

public class BoolToBrushConverter : BoolToValueConverter<Brush> { }

public class BoolToVisibilityConverter : BoolToValueConverter<Visibility> { }

[ValueConversion(typeof(bool), typeof(object))]
public class BoolToObjectConverter : BoolToValueConverter<object> { }

public class EnumFlagsToStringConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string enumString;

        try
        {
            var x = value.ToString().Split(", ");
            StringBuilder sb = new(x.Length);

            foreach (var v in x)
            {
                sb.Append(v[0]);
            }

            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    // No need to implement converting back on a one-way binding 
    public object ConvertBack(object? value, Type targetType, object? parameter,
                              CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}

[ValueConversion(typeof(ImageSourceProperties), typeof(bool))]
public class ResultItemPropertiesToBoolConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            return ((ImageSourceProperties)value).HasFlag((ImageSourceProperties)parameter);
        }
        catch
        {
            return string.Empty;
        }
    }

    // No need to implement converting back on a one-way binding 
    public object ConvertBack(object? value, Type targetType, object? parameter,
                              CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}

public class EnumToStringConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string enumString;

        try
        {
            enumString = Enum.GetName(value.GetType(), value);
            return enumString;
        }
        catch
        {
            return string.Empty;
        }
    }

    // No need to implement converting back on a one-way binding 
    public object ConvertBack(object? value, Type targetType, object? parameter,
                              CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}