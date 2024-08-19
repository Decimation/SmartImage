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
using System.Reflection;
using System.Runtime.CompilerServices;
using Kantan.Monad;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;

[assembly: InternalsVisibleTo("SmartImage.Lib.UnitTest")]

namespace SmartImage.Rdx;

#nullable disable

public sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<SearchResult> m_results;

	private SearchCommandSettings m_scs;

	private readonly STable m_table;

	private const double COMPLETE = 100.0d;

	public const int EC_ERROR = -1;
	public const int EC_OK    = 0;

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	public static readonly Version  Version  = Assembly.GetName().Version;

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnSearchComplete += OnSearchComplete;

		// Client.OnResultComplete   += OnResultComplete;
		m_cts     = new CancellationTokenSource();
		m_results = new ConcurrentBag<SearchResult>();
		m_scs     = null;
		m_table   = CreateResultTable();

		Query = SearchQuery.Null;
	}

	#region

	private async Task InitConfigAsync([CBN] object c)
	{
		//todo

		Config.SearchEngines   = m_scs.SearchEngines;
		Config.PriorityEngines = m_scs.PriorityEngines;

		if (m_scs.AutoSearch.HasValue) {
			Config.AutoSearch = m_scs.AutoSearch.Value;
		}

		if (m_scs.ReadCookies.HasValue) {
			Config.ReadCookies = m_scs.ReadCookies.Value;
		}

		await Client.LoadEnginesAsync();

	}

	private async Task<bool> SetupSearchAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;
		bool ok = true;

		Query = await SearchQuery.TryCreateAsync(m_scs.Query);

		if (Query == SearchQuery.Null) {
			// throw new SmartImageException($"Could not create query"); //todo

			ok = false;
			goto ret;
		}

		p.Increment(COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			// throw new SmartImageException("Could not upload query"); //todo
			ok = false;
			goto ret;
		}

		p.Increment(COMPLETE / 2);

	ret:
		return ok;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(SetupSearchAsync);

		try {
			var ok = await task;

			if (ok) {
				await InitConfigAsync(ok);
			}
			else {
				throw new SmartImageException("Could not upload query");
			}
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return EC_ERROR;
		}

		var gr = CreateConfigGrid();
		AConsole.Write(gr);

		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 * todo
		 */

		Task run;

		run = AConsole.Live(m_table)
			.StartAsync(RunSearchLiveAsync);

		if (m_scs.Interactive.HasValue && m_scs.Interactive.Value) {
			run = run.ContinueWith(ShowInteractivePromptAsync, m_cts.Token);
		}

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

		if (m_scs.KeepOpen.HasValue && m_scs.KeepOpen.Value) {
			run = run.ContinueWith((c) =>
			{
				AConsole.Confirm("Exit");
			});
		}

		await run;

		return EC_OK;
	}

	// TODO: Rewrite RunSearch counterparts


	private async Task RunSearchLiveAsync(LiveDisplayContext c)
	{
		var search = Client.RunSearchAsync(Query, token: m_cts.Token);

		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var result = await Client.ResultChannel.Reader.ReadAsync();

			m_results.Add(result);

			/*var txt  = new Text(result.Engine.Name, GetEngineStyle(result.Engine.EngineOption));
			var txt2 = new Text($"{result.Results.Count}");

			m_mainTable.AddRow(txt, txt2);*/

			var rows = CreateResultRows(result);

			foreach (IRenderable[] row in rows) {
				m_table.AddRow(row);
			}

			c.Refresh();
		}

		await search;

	}

	private async Task RunCompletionCommandAsync([CBN] object o)
	{
		Debug.WriteLine($"{nameof(RunCompletionCommandAsync)}");
		var command = Cli.Wrap(m_scs.Command);

		var cmdArgs      = m_scs.CommandArguments;
		var stdOutBuffer = new StringBuilder();
		var stdErrBuffer = new StringBuilder();

		if (!String.IsNullOrWhiteSpace(cmdArgs)) {
			command = command.WithArguments(cmdArgs);
		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(m_cts.Token);

		AConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AConsole.WriteLine($"Process successful: {result.IsSuccess}");
	}

	private async Task ShowInteractivePromptAsync(Task c, object x)
	{
		var prompt = new TextPrompt<string>("<name> <#>");

		string input;

		do {
			// AConsole.Clear();
			// AConsole.Write(m_table);
			input = AConsole.Prompt(prompt);

			if (!String.IsNullOrWhiteSpace(input)) {
				var inputSplit = input.Split(' ', StringSplitOptions.TrimEntries);
				var name       = inputSplit[0];

				var res = m_results.FirstOrDefault(
					sr => sr.Engine.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));

				if (res != default && inputSplit.Length > 1 && Int32.TryParse(inputSplit[1], out var idx)) {
					var sri = res.Results[idx];
					Client.OpenResult(sri);
				}
			}


		} while (input != "q");


		return;
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
				sw.WriteLine(String.Join(m_scs.OutputFileDelimiter, rg));
			}

		}

		sw.Dispose();
		fw.Dispose();

		AConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
	}

	#endregion

	#region

	private static STable CreateResultTable()
	{
		var col = new TableColumn[]
		{
			new("Result"),
			new("URL"),
			new("Similarity"),
			new("Artist"),
			new("Site"),

		};

		var tb = new STable()
		{
			Caption     = new TableTitle("Results", new Style(decoration: Decoration.Bold)),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};

		tb.AddColumns(col);

		return tb;
	}

	private static IEnumerable<IRenderable[]> CreateResultRows(SearchResult result)
	{
		Style style = ConsoleFormat.GetEngineStyle(result.Engine.EngineOption);

		var lr   = style.Foreground.GetLuminance();
		var lrr  = style.Foreground.GetContrastRatio(Color.White);
		var lrr2 = style.Foreground.GetContrastRatio(Color.Black);

		// Debug.WriteLine($"{lr} {lrr} {lrr2}");

		for (int i = 0; i < result.Results.Count; i++) {
			var res  = result.Results[i];
			var name = new Text($"{result.Engine.Name} #{i}", style);

			IRenderable url;
			var         link = res.Url;

			if (link != null) {
				url = new Markup(link.ToString(), new Style(link: link));
			}
			else {
				url = new Text("-");
			}

			var sim    = new Text($"{res.Similarity}");
			var artist = new Text($"{res.Artist}");
			var site   = new Text($"{res.Site}");
			yield return [name, url, sim, artist, site];
		}

	}

	private Grid CreateConfigGrid()
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
			dt.AddRow(new Text(o.Key, ConsoleFormat.Sty_Grid1),
			          new Text(o.Value.ToString()));
		}

		return dt;
	}

	#endregion

	[ContractAnnotation("=> halt")]
	private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
	{
		AConsole.MarkupLine($"[red]Cancellation requested[/]");
		AConsole.Clear();
		m_cts.Cancel();
		args.Cancel = false;

		Environment.Exit(EC_ERROR);
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;

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