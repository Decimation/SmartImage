// Deci SmartImage.Rdx SearchCommandSettings.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 0:56

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Novus.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Commands;
using SmartImage.Rdx.Commands.Common;
using SmartImage.Rdx.Shell;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace SmartImage.Rdx.Commands.Search;

public sealed class SearchCommandSettings : CommonCommandSettings
{

	[CommandArgument(0, "<query>")]
	[Description($"Query: file or URL. Specify {QUERY_DEFAULT_CLIPBOARD} to read from clipboard (Windows only)")]
	public string? Query { get; private set; }

	public const string QUERY_DEFAULT_CLIPBOARD = "<clipboard>";

#region

	[CommandOption("--keep-open")]
	[DefaultValue(false)]
	[Description("Waits for input before terminating")]
	public bool KeepOpen { get; private set; }

#endregion

#region

	[CommandOption("-f|--output-format")]
	[DefaultValue(OutputFileFormat.None)]
	[Description("Output file format")]
	public OutputFileFormat OutputFileFormat { get; private set; }

	[CommandOption("-o|--output-file")]
	[Description("Output file name")]
	public string? OutputFile { get; private set; }

	[CommandOption("-d|--output-delim")]
	[DefaultValue(",")]
	[Description("Output file delimiter")]
	public string? OutputFileDelimiter { get; private set; }

	[CommandOption("--output-fields")]
	[DefaultValue(OUTPUT_FIELDS_DEFAULT)]
	[Description("Output fields (comma-delimited)")]
	public OutputFields OutputFields { get; private set; }

	public const OutputFields OUTPUT_FIELDS_DEFAULT =
		OutputFields.Name | OutputFields.Similarity | OutputFields.Url;

	[MNNW(true, nameof(OutputFile))]
	internal bool HasOutputFile => !String.IsNullOrWhiteSpace(OutputFile);

#endregion

#region

	[CommandOption("--cmd-exe")]
	[Description($"Command/executable to invoke upon completion")]
	public string? Command { get; private set; }

	[CommandOption("--cmd-args <ARGS>")]
	[Description($"Arguments to pass to command")]
	public string[]? CommandArguments { get; private set; }

	[MNNW(true, nameof(Command))]
	internal bool HasCommand => !String.IsNullOrWhiteSpace(Command);

#endregion

	[CommandOption("--interactive")]
	[DefaultValue(true)]
	[Description("Interactive results")]
	public bool Interactive { get; private set; }

	public override ValidationResult Validate()
	{
		var result = base.Validate();

		if (Query == QUERY_DEFAULT_CLIPBOARD) {
			if (!OperatingSystem.IsWindows()) {
				return ValidationResult.Error("Clipboard input is only supported on Windows");
			}

			string? data = WindowsOSIntegration.TryGetClipboardData();

			if (data != null) {
				Query = data;
			}
			else {
				return ValidationResult.Error($"No valid clipboard format detected");
			}

		}

		if (!AllocImage.IsValidSourceType(Query)) {
			return ValidationResult.Error($"Invalid query: {Query}");
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