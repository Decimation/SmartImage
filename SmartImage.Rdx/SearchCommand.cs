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
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;

[assembly: InternalsVisibleTo("SmartImage.Lib.UnitTest")]

namespace SmartImage.Rdx;

#nullable disable

public sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<SearchResult>                             m_results;
	private readonly ConcurrentDictionary<SearchResultItem, IList<UniImage>> m_results2;

	private SearchCommandSettings m_scs;

	private readonly STable m_table;

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	public static readonly Version  Version  = Assembly.GetName().Version;

	public SearchCommand()
	{
		Config = new SearchConfig();

		// Config = (SearchConfig) cfg;
		Client = new SearchClient(Config);

		// Client.OnSearchComplete += OnSearchComplete;

		// Client.OnResultComplete   += OnResultComplete;
		m_cts      = new CancellationTokenSource();
		m_results  = new ConcurrentBag<SearchResult>();
		m_results2 = new ConcurrentDictionary<SearchResultItem, IList<UniImage>>();
		m_scs      = null;
		m_table    = CreateResultTable();

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

		Config.FlareSolverr = m_scs.FlareSolverr;
		Config.FlareSolverrApiUrl = m_scs.FlareSolverrApiUrl;

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


		p.Increment(ConsoleItems.COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			// throw new SmartImageException("Could not upload query"); //todo
			ok = false;
			goto ret;
		}

		p.Increment(ConsoleItems.COMPLETE / 2);

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
				var ci = GetQueryCanvasImage();

				var panel = new Panel(ci)
				{
					Header = new PanelHeader($"{Query.Uni.ValueString}")
				};
				AConsole.Write(panel);
				await InitConfigAsync(ok);

			}
			else {
				throw new SmartImageException("Could not upload query");
			}
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return ConsoleItems.EC_ERROR;
		}

		var gr = CreateConfigGrid();
		AConsole.Write(gr);

		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 * todo
		 */

		Task run;

#if !UNITTEST
		run = AConsole.Live(m_table)
			.StartAsync(RunSearchLiveAsync);
#else
		run = RunSearchLiveAsync(null);

#endif

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

		Task run2;

		if (m_scs.Interactive.HasValue && m_scs.Interactive.Value) {
			string cmd = null;

			do {
				cmd = GetCommandPrompt();

				if (cmd != ConsoleItems.s_commandChoices[^1]) {
					var sr  = GetResultPrompt();
					var num = GetNumberPrompt(sr);
					var res = sr.Results[num];

					if (cmd == ConsoleItems.s_commandChoices[0]) {
						SearchClient.OpenResult(res.Url);
					}
					else if (cmd == ConsoleItems.s_commandChoices[1]) {
						run2 = ShowImageScanResultsAsync(res);
						await run2;
					}
				}
				else { }

			} while (cmd != ConsoleItems.s_commandChoices[^1]);

			// run2 = ShowImageScanResultsAsync(item);
			// await run2;
		}

		if (m_scs.KeepOpen.HasValue && m_scs.KeepOpen.Value) {
			while (!AConsole.Confirm("Exit")) {
				// ...
			}
		}

		return ConsoleItems.EC_OK;
	}

	// TODO: Rewrite RunSearch counterparts


	private async Task RunSearchLiveAsync(LiveDisplayContext c)
	{

#if UNITTEST
		return;
#endif
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

	private int GetNumberPrompt(SearchResult result)
	{
		int input = default;

		var prompt = new TextPrompt<int>("<#>")
		{
			ShowChoices      = false,
			ShowDefaultValue = false,
			AllowEmpty       = false,
			Validator = i =>
			{
				if (i < result.Results.Count && i >= 0) {
					return ValidationResult.Success();
				}

				return ValidationResult.Error("Out of range");
			}
		};
		input = AConsole.Prompt(prompt);


		return input;
	}

	private SearchResult GetResultPrompt()
	{
		var prompt = new TextPrompt<SearchResult>("<result>")
		{
			ShowChoices      = false,
			ShowDefaultValue = false,
			AllowEmpty       = false,
			Converter        = s => { return s.Engine.Name; }
		};

		prompt = prompt.AddChoices(m_results);

		SearchResult input = default;

		input = AConsole.Prompt(prompt);

		return input;
	}

	private string GetCommandPrompt()
	{
		var prompt = new TextPrompt<string>("<cmd>")
		{
			ShowChoices      = true,
			ShowDefaultValue = true,
			AllowEmpty       = false,
		};

		prompt = prompt.AddChoices(ConsoleItems.s_commandChoices);

		string input = null;

		input = AConsole.Prompt(prompt);

		return input;
	}

	private Task WriteOutputFileAsync([CBN] object o)
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
			.Select(Enum.GetName);

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
		return Task.CompletedTask;
	}

	private Task ShowImageScanResultsAsync(SearchResultItem item)
	{
		Task run2;
		/*var  gr2 = new Grid();
		gr2.AddColumns(5);
		gr2.AddRow(CreateResultItemRows(item, 0, Style.Plain));

		// AConsole.Write(gr2);

		var tr = new Tree(gr2);
		var ld = AConsole.Live(tr);*/

		var table2 = CreateResultTable();
		table2.AddRow(CreateResultItemRows(item, 0, Style.Plain));
		var ld = AConsole.Live(table2);

		run2 = ld.StartAsync(async f =>
		{
			if (m_results2.TryGetValue(item, out IList<UniImage> list)) {
				Trace.WriteLine($"{item} cached");

				var list2 = list.OfType<UniImageUri>();
				int i     = 0;

				foreach (var ui in list2) {
					table2.AddRow(CreateUniImageRow(ui, item, i++));
				}
			}
			else {
				// var ok = await r.ScanAsync();
				Trace.WriteLine($"Scanning {item}");
				var resOk = await ImageScanner.ScanImagesAsync(item.Url);

				var buf = new List<UniImage>();

				while (resOk.Count != 0) {
					var t = await Task.WhenAny(resOk);
					resOk.Remove(t);

					var r = await t;

					if (r == null) {
						continue;
					}

					if (r is UniImageUri ru) {
						buf.Add(ru);

						// tr.AddNode(new Text(ru.ValueString, new Style(link: ru.Url)));

						table2.AddRow(CreateUniImageRow(ru, item, buf.Count));
					}

					f.Refresh();

				}

				m_results2.TryAdd(item, buf);

			}
		});

		return run2;
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

	#endregion

	#region

	private static IRenderable[] CreateUniImageRow(UniImageUri ui, SearchResultItem sri, int idx)
	{
		return
		[
			new Text($"{sri.Root.Engine.Name} {idx}", new Style(link: ui.Url)),
			new Text(ui.Url),
			new Text($"{0f}"),
			new Text("-"),
			new Text("-")
		];
	}

	private SearchResultItem ParseResultFromPrompt(string input)
	{
		var inputSplit = input.Split(' ', StringSplitOptions.TrimEntries);
		var name       = inputSplit[0];

		var res = m_results.FirstOrDefault(
			sr => sr.Engine.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));

		if (res != default && inputSplit.Length > 1 && Int32.TryParse(inputSplit[1], out var idx)) {
			var sri = res.Results[idx];
			return sri;
		}

		return null;
	}

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
			var res = result.Results[i];

			yield return CreateResultItemRows(res, i, style);
		}

	}

	private static IRenderable[] CreateResultItemRows(SearchResultItem res, int i, Style style)
	{
		var name = new Text($"{res.Root.Engine.Name} #{i}", style);

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
		return [name, url, sim, artist, site];
	}

	private Layout CreateConfigLayout()
	{
		// Create the layout
		var layout = new Layout("Root")
			.SplitColumns(
				new Layout("Left"),
				new Layout("Right"));

		// Update the left column
		layout["Right"].Update(
			new Panel(Align.Center(new Text("---"))).Expand());


		return layout;
	}

	private CanvasImage GetQueryCanvasImage()
	{
		var ci = new CanvasImage(Query.Uni.Stream)
		{
			MaxWidth = AConsole.Profile.Width / 6,

			// PixelWidth = 2
		};
		Query.Uni.Stream.TrySeek();
		return ci;
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

		// Render the layout
		// AnsiConsole.Write(layout);


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

		Environment.Exit(ConsoleItems.EC_ERROR);
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