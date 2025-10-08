using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SmartImage.UI2;

internal class ImageToBitmapImageConverter : IValueConverter
{

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
			
		return new Avalonia.Data.BindingNotification(value);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return new Avalonia.Data.BindingNotification(value);
	}

}