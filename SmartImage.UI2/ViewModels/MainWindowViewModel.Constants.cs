// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.Constants.cs
// Date: 2026/10/17 @ 15:10:55

using System;
using System.Linq;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload.Base;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel
{

	internal static readonly UploadEngineOption[] ValidUploadOptions =
	[
		.. Enum.GetValues<UploadEngineOption>()
		       .Where(static e => e != UploadEngineOption.None && !BaseUploadEngine.ObsoleteUploadEngines.Contains(e))
	];

	internal static readonly SearchEngineOptions[] ValidSearchOptions =
	[
		.. Enum.GetValues<SearchEngineOptions>()
		       .Where(static e => e is not (SearchEngineOptions.Auto or SearchEngineOptions.None) && !e.HasFlag(SearchEngineOptions.Obsolete))
	];

	internal static readonly SearchEngineOptions[] ValidPriorityOptions =
	[
		.. Enum.GetValues<SearchEngineOptions>()
		       .Where(static e => e != SearchEngineOptions.None && !e.HasFlag(SearchEngineOptions.Obsolete))
	];

}