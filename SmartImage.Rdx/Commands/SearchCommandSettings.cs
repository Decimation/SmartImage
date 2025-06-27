// Deci SmartImage.Rdx SearchCommandSettings.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:56

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Novus.Win32;
using SmartImage.Lib.Images.Uni;
using SmartImage.Rdx.Shell;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace SmartImage.Rdx.Commands;

public sealed class SearchCommandSettings : CommonCommandSettings
{

	[CommandArgument(0, "<query>")]
	[Description("Query: file or URL; see wiki")]
	public string? Query { get; internal set; }

#region

	[CommandOption("--keep-open")]
	[DefaultValue(false)]
	[Description("Waits for input before terminating")]
	public bool KeepOpen { get; internal set; }

#endregion

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

	public const string QUERY_DEFAULT_CLIPBOARD = "<clipboard>";

#endregion

#region

	[CommandOption("-x|--command-exe")]
	[Description($"Command/executable to invoke upon completion")]
	public string? Command { get; internal set; }

	[CommandOption("-c|--command-args")]
	[Description($"Arguments to pass to command")]
	public string? CommandArguments { get; internal set; }

#endregion

	// public bool? Silent { get; internal set; } //todo

	// public const string PROP_ARG_RESULTS = "$all_results";

	[CommandOption("--interactive")]
	[DefaultValue(false)]
	[Description("Interactive results")]
	public bool Interactive { get; internal set; }

	[CommandOption("--clipboard")]
	[DefaultValue(false)]
	[Description("Clipboard")]
	public bool UseClipboard { get; internal set; }

	public override ValidationResult Validate()
	{
		var result = base.Validate();

		if (UseClipboard && OperatingSystem.IsWindows()) {
			Clipboard.Open();

			var data = Novus.Win32.Clipboard.GetFileName();

			// var data2 = Novus.Win32.Clipboard.GetData((uint) ClipboardFormat.BMP2);
			Query = data;
			Clipboard.Close();
		}

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