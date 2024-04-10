// Read S SmartImage.Rdx SearchCommand.cs
// 2023-07-05 @ 2:07 AM

global using R2 = SmartImage.Rdx.Resources;
global using R1 = SmartImage.Lib.Resources;

// global using AC = Spectre.Console.AnsiConsole;
global using AConsole = Spectre.Console.AnsiConsole;
global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
using SmartImage.Lib;
using Spectre.Console;
using Spectre.Console.Cli;
using Kantan.Net.Utilities;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Flurl;
using JetBrains.Annotations;
using Kantan.Diagnostics;
using Kantan.Model.MemberIndex;
using Kantan.Utilities;
using Microsoft;
using Novus.Streams;
using Novus.Utilities;
using SixLabors.ImageSharp.Processing;
using CliWrap;
using Kantan.Text;
using SmartImage.Rdx.Shell;
using System.Linq;

namespace SmartImage.Rdx;

#nullable disable

internal sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<SearchResult> m_results;

	// private readonly STable m_resTable;

	private SearchCommandSettings m_scs;

	private const double COMPLETE = 100.0d;

	public const int EC_ERROR = -1;
	public const int EC_OK    = 0;

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnComplete += OnComplete;

		// Client.OnResult   += OnResult;
		m_cts     = new CancellationTokenSource();
		m_results = new ConcurrentBag<SearchResult>();
		m_scs     = null;

		Query = SearchQuery.Null;
	}

	#region

	private async Task SetupSearchAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;
		Query             = await SearchQuery.TryCreateAsync(m_scs.Query);

		p.Increment(COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			throw new SmartImageException(); //todo
		}

		p.Increment(COMPLETE / 2);
	}

	private async Task InitConfigAsync([CBN] object c)
	{
		Config.SearchEngines   = m_scs.SearchEngines;
		Config.PriorityEngines = m_scs.PriorityEngines;

		if (m_scs.AutoSearch.HasValue) {
			Config.AutoSearch = m_scs.AutoSearch.Value;
		}

		if (m_scs.ReadCookies.HasValue) {
			Config.ReadCookies = m_scs.ReadCookies.Value;
		}

		await Client.ApplyConfigAsync();

	}

	private async Task RunSearchAsync(ProgressContext c)
	{
		var cnt = (double) Client.Engines.Length;
		var pt  = c.AddTask("Running search", maxValue: cnt);
		pt.IsIndeterminate = true;

		// var p2  = c.AddTask("Engines", maxValue: cnt);

		// Client.OnResult += OnResultComplete;

		var search = Client.RunSearchAsync(Query, token: m_cts.Token, reload: false);

		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var result = await Client.ResultChannel.Reader.ReadAsync();

			m_results.Add(result);
			pt.Description = $"{result.Engine.Name} {m_results.Count} / {cnt}";
			pt.Increment(1);
			c.Refresh();
		}

		await search;

		// Debug.WriteLine($"{nameof(RunSearchAsync)} complete");

		return;

	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(SetupSearchAsync)
			.ContinueWith(InitConfigAsync);

		await task;

		var gr = CreateInfoGrid();
		AConsole.Write(gr);

		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 */

		var run = AConsole.Progress()
			.AutoRefresh(false)
			.StartAsync(RunSearchAsync);

		if (!String.IsNullOrWhiteSpace(m_scs.Command)) {
			run = run.ContinueWith(RunCompletionCommandAsync, m_cts.Token,
			                       TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
		}

		if (!String.IsNullOrWhiteSpace(m_scs.OutputFile)) {
			switch (m_scs.OutputFileFormat) {

				case OutputFileFormat.None:
					break;

				case OutputFileFormat.Delimited:
					run = run.ContinueWith(WriteOutputFileAsync, m_cts.Token,
					                       TaskContinuationOptions.OnlyOnRanToCompletion,
					                       TaskScheduler.Default);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

		}

		await run;

		return EC_OK;
	}

	private async Task WriteOutputFileAsync([CBN] object o)
	{
		Debug.WriteLine($"{nameof(WriteOutputFileAsync)}");

		var fw = File.OpenWrite(m_scs.OutputFile);

		var sw = new StreamWriter(fw)
		{
			AutoFlush = true
		};
		var res    = m_results.ToArray();
		var fields = m_scs.OutputFields;

		bool fName   = fields.HasFlag(OutputFields.Name);
		var  fUrl    = fields.HasFlag(OutputFields.Url);
		var  fSim    = fields.HasFlag(OutputFields.Similarity);
		var  fArtist = fields.HasFlag(OutputFields.Artist);
		var  fSite   = fields.HasFlag(OutputFields.Site);

		var names = Enum.GetValues<OutputFields>()
			.Where(f => fields.HasFlag(f) && !f.Equals(default(OutputFields)))
			.Select(f => Enum.GetName(f));

		sw.WriteLine(String.Join(m_scs.OutputFileDelimiter, names));

		for (int i = 0; i < res.Length; i++) {
			var sr = res[i];

			for (int j = 0; j < sr.Results.Count; j++) {
				var sri = sr.Results[j];

				var rg = new List<string>();

				if (fName) {
					rg.Add($"{sr.Engine.Name} #{j + 1}");
				}

				if (fUrl) {
					rg.Add(sri.Url);
				}

				if (fSim) {
					rg.Add($"{sri.Similarity}");
				}

				if (fArtist) {
					rg.Add($"{sri.Artist}");
				}

				if (fSite) {
					rg.Add($"{sri.Site}");
				}

				// string[] items  = [$"{sr.Engine.Name} #{j + 1}", sri.Url?.ToString()];
				sw.WriteLine(String.Join(m_scs.OutputFileDelimiter, rg));
			}

		}

		sw.Dispose();
		fw.Dispose();

		AConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
	}

	private async Task RunCompletionCommandAsync([CBN] object o)
	{
		Debug.WriteLine($"{nameof(RunCompletionCommandAsync)}");
		var command = Cli.Wrap(m_scs.Command);

		var cmdArgs      = m_scs.CommandArguments;
		var stdOutBuffer = new StringBuilder();
		var stdErrBuffer = new StringBuilder();

		/*var buf1         = new StringBuilder();

			foreach (ResultModel model in m_results) {
				foreach (SearchResultItem item in model.Result.Results) {
					buf1.AppendLine(item.Url);
				}
			}*/

		if (!String.IsNullOrWhiteSpace(cmdArgs)) {

			// cmdArgs = cmdArgs.Replace(SearchCommandSettings.PROP_ARG_RESULTS, buf1.ToString());

			command = command.WithArguments(cmdArgs);

		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(m_cts.Token);

		AConsole.WriteLine($"Process: {commandTask.ProcessId}");

		var result = await commandTask;

		AConsole.WriteLine($"Process: {result.IsSuccess}");
	}

	[ContractAnnotation("=> halt")]
	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		AConsole.MarkupLine($"[red]Cancellation requested[/]");
		AConsole.Clear();
		m_cts.Cancel();
		args.Cancel = false;

		Environment.Exit(EC_ERROR);
	}

	#endregion

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;

	}

	private Grid CreateInfoGrid()
	{
		var dt = new Grid();
		dt.AddColumns(2);

		var kv = new Dictionary<string, object>()
		{
			[R1.S_SearchEngines]   = Config.SearchEngines,
			[R1.S_PriorityEngines] = Config.PriorityEngines,
			[R1.S_AutoSearch]      = Config.AutoSearch,
			[R1.S_ReadCookies]     = Config.ReadCookies,

			["Input"]  = Query,
			["Upload"] = Query.Upload
		};

		foreach (var o in kv) {
			dt.AddRow(new Text(o.Key, CliFormat.Sty_Grid1),
			          new Text(o.Value.ToString()));
		}

		return dt;
	}

	public void Dispose()
	{
		foreach (var sr in m_results) {
			sr.Dispose();
		}

		m_results.Clear();
		m_cts.Dispose();
		m_scs = null;
		Client.Dispose();
		Query.Dispose();
	}

}