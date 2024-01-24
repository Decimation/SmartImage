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
using SmartImage.Lib.Model;
using SmartImage.UI.Model;

namespace SmartImage.UI.Controls;

[ValueConversion(typeof(IBaseImageSource), typeof(string))]
public class ImageDimensionConverter : IValueConverter
{

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var val = (IBaseImageSource) value;

		if (val == null) {
			return null;
		}

		string dim;

		int? w = null, h = null;

		if (val is ResultItem ri) {
			if (ri is { IsThumbnail: true } and { HasImage: true }) {
				w = ri.Image.PixelWidth;
				h = ri.Image.PixelHeight;
			}

		}
		else {
			w = (val.Width);
			h = val.Height;

		}

		dim = ControlsHelper.FormatDimensions(w, h);

		return dim;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}

}