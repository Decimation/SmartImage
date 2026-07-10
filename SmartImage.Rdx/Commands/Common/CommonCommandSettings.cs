// Author: Deci | Project: SmartImage.Rdx | Name: CommonCommandSettings.cs
// Date: 2025/02/25 @ 10:02:23

using System.ComponentModel;
using SmartImage.Lib;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Engines.Upload.Base;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands.Common;

public class CommonCommandSettings : CommandSettings
{

	[CommandOption("-e|--search-engines")]
	[DefaultValue(SearchConfig.SE_DEFAULT)]
	[Description("Search engines (comma-delimited)")]
	public SearchEngineOptions SearchEngines { get; internal set; }

	[CommandOption("-p|--priority-engines")]
	[DefaultValue(SearchConfig.PE_DEFAULT)]
	[Description("Engines whose results to open (comma-delimited)")]
	public SearchEngineOptions PriorityEngines { get; internal set; }

	[CommandOption("-u|--upload-engine")]
	[DefaultValue(SearchConfig.UE_DEFAULT)]
	[Description("Upload engine")]
	public UploadEngineOption UploadEngine { get; internal set; }

	[CommandOption("--read-cookies")]
	[DefaultValue(SearchConfig.READCOOKIES_DEFAULT)]
	[Description("Read cookies from browser")]
	public bool ReadCookies { get; internal set; }

	[CommandOption("--flaresolverr")]
	[DefaultValue(SearchConfig.FLARESOLVERR_DEFAULT)]
	[Description("Use FlareSolverr")]
	public bool FlareSolverr { get; internal set; }

	[CommandOption("--flaresolverr-api")]
	[DefaultValue(FlareSolverrClient.FLARE_SOLVERR_API_URL_DEFAULT)]
	[Description("FlareSolverr API URL")]
	public string? FlareSolverrApiUrl { get; internal set; }

}