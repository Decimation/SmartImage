using Kantan.Utilities;
using Novus.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using SmartImage.Lib.Results.Data;
using SmartImage.UI.Model;

namespace SmartImage.UI.Controls.Converters;

[ValueConversion(typeof(IBitmapImageSource), typeof(string))]
public class ImageDimensionConverter : IValueConverter
{

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var val = (IBitmapImageSource) value;

		if (val == null) {
			return null;
		}

		string dim;

		int? w = null, h = null;

		if (val is { } ri) {
			if (ri is { IsThumbnail: true, HasImage: true, Image: {  } i }) {
				w = i.PixelWidth;
				h = i.PixelHeight;
			}

		}
		else {
			w = val.Width;
			h = val.Height;

		}

		// w   ??= val.Width;
		// h   ??= val.Height;

		dim = ControlsHelper.FormatDimensions(w, h);

		return dim;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}

}