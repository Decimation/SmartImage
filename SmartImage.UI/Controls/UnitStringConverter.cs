using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Kantan.Utilities;

namespace SmartImage.UI.Controls;

[ValueConversion(typeof(long), typeof(string))]
internal class UnitStringConverter : IValueConverter
{

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var val   = (long) value;
		var bytes = FormatHelper.FormatBytes(val);

		return bytes;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}

}