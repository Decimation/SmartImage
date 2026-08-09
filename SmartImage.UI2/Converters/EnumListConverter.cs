// Author: Deci | Project: SmartImage.UI2 | Name: EnumListConverter.cs
// Date: 2026/08/08 @ 15:08:21

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Kantan.Utilities;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.Converters;

public class EnumListConverter : IValueConverter
{

	public static readonly EnumListConverter Instance = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is SearchEngineOptions opt) {
			var setFlags = opt.GetSetFlags();
			return setFlags;
		}

		return default;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is SearchEngineOptions[] rg) {
			var newVal = rg.Aggregate(default(SearchEngineOptions), EnumHelper.Or);
			return newVal;
		}


		return default;
	}

}