using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SmartImage.UI.Controls;

public class BoolToValueConverter<T> : IValueConverter
{

	public T FalseValue { get; set; }

	public T TrueValue { get; set; }

	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value == null)
			return FalseValue;
		else
			return (bool) value ? TrueValue : FalseValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter,
	                          System.Globalization.CultureInfo culture)
	{
		return value != null ? value.Equals(TrueValue) : false;
	}

}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToStringConverter : BoolToValueConverter<String> { }

public class BoolToBrushConverter : BoolToValueConverter<Brush> { }

public class BoolToVisibilityConverter : BoolToValueConverter<Visibility> { }

[ValueConversion(typeof(bool), typeof(object))]
public class BoolToObjectConverter : BoolToValueConverter<Object> { }