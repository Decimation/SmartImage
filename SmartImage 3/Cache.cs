using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SmartImage.Lib;

namespace SmartImage;

internal static class Cache
{
	internal static readonly SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();

	internal static readonly Func<SearchEngineOptions, SearchEngineOptions, SearchEngineOptions> EnumAggregator =
		(current, searchEngineOptions) => current | searchEngineOptions;

	internal static readonly IntPtr HndWindow = Native.GetConsoleWindow();
}