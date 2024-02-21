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
	public string? Query { get; init; }

	[CommandOption("-e|--search-engines")]
	[DefaultValue(SearchConfig.SE_DEFAULT)]
	public SearchEngineOptions SearchEngines { get; init; }

	[CommandOption("-p|--priority-engines")]
	[DefaultValue(SearchConfig.PE_DEFAULT)]
	public SearchEngineOptions PriorityEngines { get; init; }

	[CommandOption("-a|--autosearch")]
	[DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
	public bool AutoSearch { get; init; }

	[CommandOption("-v|--interactive")]
	[DefaultValue(false)]
	public bool Interactive { get; init; }

	[CommandOption("-r|--result-format")]
	[DefaultValue(ResultTableFormat.Default)]
	public ResultTableFormat ResultFormat { get; init; }

	[CommandOption("-f|--output-format")]
	[DefaultValue(ResultFileFormat.None)]
	public ResultFileFormat OutputFormat { get; init; }

	[CommandOption("-o|--output")]
	public string? OutputFile { get; init; }

	[CommandOption("-x|--complete-exe")]
	public string? CompletionExecutable { get; init; }

	[CommandOption("-c|--complete-cmd")]
	public string? CompletionCommand { get; init; }

	public override ValidationResult Validate()
	{
		var result = base.Validate();

		if (!SearchQuery.IsValidSourceType(Query)) {
			return ValidationResult.Error($"Invalid query");
		}

		if (String.IsNullOrWhiteSpace(OutputFile) && OutputFormat != ResultFileFormat.None) {
			return ValidationResult.Error(
				$"{nameof(OutputFile)} must be set if {nameof(OutputFormat)} != {nameof(ResultFileFormat.None)}");
		}

		return result;
	}

}

