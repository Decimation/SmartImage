// Deci SmartImage.Rdx SearchCommandSettings.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:56

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Novus.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images;
using SmartImage.Rdx.Shell;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace SmartImage.Rdx;

internal sealed class SearchCommandSettings : CommandSettings
{

	[CommandArgument(0, "<query>")]
	[Description("Query: file or URL; see wiki")]
	public string? Query { get; internal set; }

	[CommandOption("-e|--search-engines")]
	[DefaultValue(SearchConfig.SE_DEFAULT)]
	[Description("Search engines (comma-delimited)")]
	public SearchEngineOptions SearchEngines { get; internal set; }

	[CommandOption("-p|--priority-engines")]
	[DefaultValue(SearchConfig.PE_DEFAULT)]
	[Description("Engines whose results to open (comma-delimited)")]
	public SearchEngineOptions PriorityEngines { get; internal set; }

	[CommandOption("-a|--autosearch")]
	[DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
	[Description("N/A")]
	public bool? AutoSearch { get; internal set; }

	[CommandOption("--read-cookies")]
	[DefaultValue(SearchConfig.READCOOKIES_DEFAULT)]
	[Description("Read cookies from browser")]
	public bool? ReadCookies { get; internal set; }

	[CommandOption("--live")]
	[DefaultValue(true)]
	[Description("Live result display")]
	public bool? LiveDisplay { get; internal set; }

	#region

	[CommandOption("-f|--output-format")]
	[DefaultValue(OutputFileFormat.None)]
	[Description("Output file format")]
	public OutputFileFormat OutputFileFormat { get; internal set; }

	[CommandOption("-o|--output-file")]
	[Description("Output file name")]
	public string? OutputFile { get; internal set; }

	[CommandOption("-d|--output-delim")]
	[DefaultValue(",")]
	[Description("Output file delimiter")]
	public string? OutputFileDelimiter { get; internal set; }

	[CommandOption("--output-fields")]
	[DefaultValue(OUTPUT_FIELDS_DEFAULT)]
	[Description("Output fields (comma-delimited)")]
	public OutputFields OutputFields { get; internal set; }

	public const OutputFields OUTPUT_FIELDS_DEFAULT =
		OutputFields.Name | OutputFields.Similarity | OutputFields.Url;

	#endregion

	#region

	[CommandOption("-x|--command-exe")]
	[Description($"Command/executable to invoke upon completion")]
	public string? Command { get; internal set; }

	[CommandOption("-c|--command-args")]
	[Description($"Arguments to pass to command")]
	public string? CommandArguments { get; internal set; }

	#endregion

	[CommandOption("--keep-open")]
	[DefaultValue(false)]
	[Description("Waits for input before terminating")]
	public bool? KeepOpen { get; internal set; }

	// public bool? Silent { get; internal set; } //todo

	// public const string PROP_ARG_RESULTS = "$all_results";

	public override ValidationResult Validate()
	{
		var result = base.Validate();

		if (!UniImage.IsValidSourceType(Query, false)) {
			return ValidationResult.Error("Invalid query");
		}

		var  hasOutputFile       = !String.IsNullOrWhiteSpace(OutputFile);
		var  hasOutputFileDelim  = !String.IsNullOrEmpty(OutputFileDelimiter);
		bool isOutputFormatDelim = OutputFileFormat == OutputFileFormat.Delimited;

		if (!isOutputFormatDelim && hasOutputFile) {
			OutputFileFormat    = OutputFileFormat.Delimited;
			isOutputFormatDelim = true;
		}

		if (isOutputFormatDelim) {
			if (!hasOutputFile) {
				return ValidationResult.Error(
					$"{nameof(OutputFile)} must be set if {nameof(OutputFileFormat)} == {nameof(OutputFileFormat.Delimited)}");
			}

			if (!hasOutputFileDelim) {
				return ValidationResult.Error(
					$"{nameof(OutputFileDelimiter)} must be set if {nameof(OutputFileFormat)} == {nameof(OutputFileFormat.Delimited)}");
			}
		}

		return result;
	}

}