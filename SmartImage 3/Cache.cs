using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;

namespace SmartImage
{
	internal static class Cache
	{
		public static readonly Func<SearchEngineOptions, SearchEngineOptions, SearchEngineOptions> EnumAggregator =
			(current, searchEngineOptions) => current | searchEngineOptions;
	}
}
