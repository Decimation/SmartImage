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
using Kantan.Monad;

namespace SmartImage.Rdx;

#nullable disable

internal sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<SearchResult> m_results;

	private SearchCommandSettings m_scs;

	private readonly STable m_table;

	private const    double COMPLETE = 100.0d;

	public const int EC_ERROR = -1;
	public const int EC_OK    = 0;

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnComplete += OnComplete;

		// Client.OnResult   += OnResult;
		m_cts         =  new CancellationTokenSource();
		m_results     =  new ConcurrentBag<SearchResult>();
		m_scs         =  null;
		m_table       =  CreateResultTable();
		
		Client.OnOpen += (sender, item) =>
		{
			Debug.WriteLine($"Opening {item}");
		};

		Query         =  SearchQuery.Null;
	}

	#region

	private async Task SetupSearchAsync(ProgressContext ctx)
	{
		var p = ctx.AddTask("Creating query");
		p.IsIndeterminate = true;

		Query = await SearchQuery.TryCreateAsync(m_scs.Query);

		if (Query == SearchQuery.Null) {
			throw new SmartImageException($"Could not create query"); //todo

		}
		p.Increment(COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			throw new SmartImageException("Could not upload query"); //todo
		}

		p.Increment(COMPLETE / 2);
	}

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

		await Client.ApplyConfigAsync();

	}

	private async Task RunSearchLiveAsync(LiveDisplayContext c)
	{

		var search = Client.RunSearchAsync(Query, token: m_cts.Token, reload: false);

		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var result = await Client.ResultChannel.Reader.ReadAsync();

			m_results.Add(result);

			if (m_scs.LiveDisplay.HasValue && m_scs.LiveDisplay.Value) {
				UpdateResultTable(result);
			}

			c.Refresh();
		}

		await search;

	}

	// TODO: Rewrite RunSearch counterparts

	private async Task RunSearchWithProgressAsync(ProgressContext c)
	{
		var cnt = (double) Client.Engines.Length;
		var pt  = c.AddTask("Running search", maxValue: cnt);
		pt.IsIndeterminate = true;

		var search = Client.RunSearchAsync(Query, token: m_cts.Token, reload: false);

		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var result = await Client.ResultChannel.Reader.ReadAsync();

			m_results.Add(result);

			pt.Description = $"{Strings.Constants.CHECK_MARK} {result.Engine.Name} - {result.Results.Count} ({m_results.Count} / {cnt})";
			pt.Increment(1);
			c.Refresh();

		}

		await search;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(SetupSearchAsync)
			.ContinueWith(InitConfigAsync);

		try {
			await task;
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return EC_ERROR;
		}

		var gr = CreateInfoGrid();
		AConsole.Write(gr);

		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 * todo
		 */

		Task run;

		if (m_scs.LiveDisplay.HasValue && m_scs.LiveDisplay.Value) {
			run = AConsole.Live(m_table)
				.StartAsync(RunSearchLiveAsync);

		}
		else {
			run = AConsole.Progress()
				.StartAsync(RunSearchWithProgressAsync);
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

		if (!String.IsNullOrWhiteSpace(cmdArgs)) {

			// cmdArgs = cmdArgs.Replace(SearchCommandSettings.PROP_ARG_RESULTS, buf1.ToString());

			command = command.WithArguments(cmdArgs);

		}

		command = command.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

		var commandTask = command.ExecuteAsync(m_cts.Token);

		AConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AConsole.WriteLine($"Process successful: {result.IsSuccess}");
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

	private void UpdateResultTable(SearchResult result)
	{

		if (!ConsoleFormat.EngineStyles.TryGetValue(result.Engine.EngineOption, out var style)) {
			style = Style.Plain;
		}

		var lr   = style.Foreground.GetLuminance();
		var lrr  = style.Foreground.GetContrastRatio(Color.White);
		var lrr2 = style.Foreground.GetContrastRatio(Color.Black);

		Debug.WriteLine($"{lr} {lrr} {lrr2}");

		for (int i = 0; i < result.Results.Count; i++) {
			var res    = result.Results[i];
			var name   = new Text($"{result.Engine.Name} #{i}", style);
			var url    = new Markup($"[link]{res.Url}[/]");
			var sim    = new Text($"{res.Similarity}");
			var artist = new Text($"{res.Artist}");
			var site   = new Text($"{res.Site}");
			m_table.AddRow(name, url, sim, artist, site);
		}

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
			dt.AddRow(new Text(o.Key, ConsoleFormat.Sty_Grid1),
			          new Text(o.Value.ToString()));
		}

		return dt;
	}

	#endregion

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