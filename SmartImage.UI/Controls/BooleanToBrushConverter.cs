// Read S SmartImage.UI BooleanToBrushConverter.cs
// 2023-08-20 @ 11:38 AM

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartImage.UI.Controls;

#pragma warning disable CS8618
[ValueConversion(typeof(bool), typeof(Brush))]
public class BooleanToBrushConverter : IValueConverter
{
	public Brush TrueBrush  { get; set; }
	public Brush FalseBrush { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool boolValue)
		{
			return boolValue ? TrueBrush : FalseBrush;
		}
		return FalseBrush;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}