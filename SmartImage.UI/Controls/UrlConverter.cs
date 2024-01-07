// Read S SmartImage.UI UrlConverter.cs
// 2023-09-27 @ 3:20 AM

using System;
using System.Globalization;
using System.Windows.Data;
using Flurl;

namespace SmartImage.UI.Controls;

[ValueConversion(typeof(Url), typeof(string))]
public class UrlConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null) {
			return null;
		}

		var date = (Url) value;
		return date.ToString();
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null) {
			return null;
		}

		string strValue = value as string;
		Url    resultDateTime;

		return (Url) strValue;
	}
}