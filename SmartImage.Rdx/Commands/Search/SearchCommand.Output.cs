using System.Text;
using CliWrap;
using SmartImage.Lib.Engines.Results;

#pragma warning disable CS8604 // Possible null reference argument.

namespace SmartImage.Rdx.Commands.Search;

[Flags]
public enum OutputFields
{

	None = 0,

	Name       = 1 << 0,
	Url        = 1 << 1,
	Similarity = 1 << 2,
	Artist     = 1 << 3,
	Site       = 1 << 4,

	// Default = Name | Url | Similarity

}

public enum OutputFileFormat
{
	None = 0,
	Delimited,
}

public partial class SearchCommand
{

#region

	private async Task RunCompletionCommandAsync(CancellationToken ct = default)
	{
		var command = Cli.Wrap(CommandSettings.Command);

		var cmdArgs      = CommandSettings.CommandArguments;
		var stdOutBuffer = new StringBuilder();
		var stdErrBuffer = new StringBuilder();

		if (cmdArgs is not null) {
			command = command.WithArguments(cmdArgs);
		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(ct);

		AnsiConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AnsiConsole.WriteLine($"Process successful: {result.IsSuccess}");
	}

	private void WriteOutputFile()
	{
		var fw = File.OpenWrite(CommandSettings.OutputFile);

		using var sw = new StreamWriter(fw);
		sw.AutoFlush = true;

		var fields = CommandSettings.OutputFields;

		bool fName   = fields.HasFlag(OutputFields.Name);
		var  fUrl    = fields.HasFlag(OutputFields.Url);
		var  fSim    = fields.HasFlag(OutputFields.Similarity);
		var  fArtist = fields.HasFlag(OutputFields.Artist);
		var  fSite   = fields.HasFlag(OutputFields.Site);

		var names = Enum.GetValues<OutputFields>()
			.Where(f => fields.HasFlag(f) && !f.Equals(default(OutputFields)))
			.Select(Enum.GetName);

		sw.WriteLine(String.Join(CommandSettings.OutputFileDelimiter, names));

		foreach (SearchResult sr in m_resultTables.Keys) {
			for (int j = 0; j < sr.Results.Count; j++) {
				var sri = sr.Results[j];

				var rg = new List<string>();

				if (fName)
					rg.Add($"{sr.Engine.Name} #{j + 1}");

				if (fUrl)
					rg.Add(sri.Url);

				if (fSim)
					rg.Add($"{sri.Similarity}");

				if (fArtist)
					rg.Add($"{sri.Artist}");

				if (fSite)
					rg.Add($"{sri.Site}");

				// string[] items  = [$"{sr.Engine.Name} #{j + 1}", sri.Url?.ToString()];
				sw.WriteLine(String.Join(CommandSettings.OutputFileDelimiter, rg));
			}
		}

		AnsiConsole.WriteLine($"Wrote to {CommandSettings.OutputFile}");
	}

#endregion

}