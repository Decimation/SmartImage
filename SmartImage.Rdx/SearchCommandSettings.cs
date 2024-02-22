// Deci SmartImage.Rdx SearchCommandSettings.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:56

using System.ComponentModel;
using System.Globalization;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Rdx.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx;

internal sealed class SearchCommandSettings : CommandSettings
{

	[CommandArgument(0, "[query]")]
	[Description("Query")]
	public string? Query { get; internal set; }

	[CommandOption("-e|--search-engines")]
	[DefaultValue(SearchConfig.SE_DEFAULT)]
	public SearchEngineOptions SearchEngines { get; internal set; }

	[CommandOption("-p|--priority-engines")]
	[DefaultValue(SearchConfig.PE_DEFAULT)]
	public SearchEngineOptions PriorityEngines { get; internal set; }

	[CommandOption("-a|--autosearch")]
	[DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
	public bool AutoSearch { get; internal set; }

	#region 

	[CommandOption("-v|--interactive")]
	[DefaultValue(false)]
	public bool Interactive { get; internal set; }

	[CommandOption("-r|--shell-format")]
	[DefaultValue(ResultShellFormat.Full)]
	public ResultShellFormat ResultFormat { get; internal set; }

	#endregion

	#region 

	[CommandOption("-f|--output-format")]
	[DefaultValue(ResultFileFormat.None)]
	public ResultFileFormat OutputFormat { get; internal set; }

	[CommandOption("-o|--output")]
	public string? OutputFile { get; internal set; }

	[CommandOption("-d|--delim")]
	[DefaultValue(",")]
	public string? OutputFileDelimiter { get; internal set; }

	#endregion

	#region 

	[CommandOption("-x|--complete-exe")]
	public string? CompletionExecutable { get; internal set; }

	[CommandOption("-c|--complete-cmd")]
	public string? CompletionCommand { get; internal set; }

	#endregion

	public override ValidationResult Validate()
	{
		var result = base.Validate();

		if (!SearchQuery.IsValidSourceType(Query)) {
			return ValidationResult.Error($"Invalid query");
		}

		bool isOutputFormatDelim = OutputFormat == ResultFileFormat.Delimited;
		var  hasOutputFile       = !String.IsNullOrWhiteSpace(OutputFile);
		var  hasOutputFileDelim  = !String.IsNullOrEmpty(OutputFileDelimiter);

		if (!isOutputFormatDelim && (hasOutputFile || hasOutputFileDelim)) {
			OutputFormat        = ResultFileFormat.Delimited;
			isOutputFormatDelim = true;
		}

		if (isOutputFormatDelim) {
			if (!hasOutputFile) {
				return ValidationResult.Error(
					$"{nameof(OutputFile)} must be set if {nameof(OutputFormat)} == {nameof(ResultFileFormat.Delimited)}");
			}
			if (!hasOutputFileDelim) {
				return ValidationResult.Error(
					$"{nameof(OutputFileDelimiter)} must be set if {nameof(OutputFormat)} == {nameof(ResultFileFormat.Delimited)}");
			}
		}

		return result;
	}

}