// Read S SmartImage.Rdx SearchCommand.cs
// 2023-07-05 @ 2:07 AM

global using R2 = SmartImage.Rdx.Resources;
global using R1 = SmartImage.Lib.Resources;

// global using AC = Spectre.Console.AnsiConsole;
// global using AnsiConsole = Spectre.Console.AnsiConsole;
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

	private readonly ConcurrentBag<SearchResult>             m_results;
	private readonly ConcurrentDictionary<object, int> m_results2;
	private          SearchCommandSettings                   m_scs;

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
		m_scs      = null;
		m_table    = CreateResultTable();
		m_results2 = new ();

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

		Config.FlareSolverr       = m_scs.FlareSolverr;
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


		p.Increment(ConsoleFormat.COMPLETE / 2);

		// ctx.Refresh();

		p.Description = "Uploading query";
		var url = await Query.UploadAsync();

		if (url == null) {
			// throw new SmartImageException("Could not upload query"); //todo
			ok = false;
			goto ret;
		}

		p.Increment(ConsoleFormat.COMPLETE / 2);

	ret:
		return ok;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AnsiConsole.Progress()
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
				AnsiConsole.Write(panel);
				await InitConfigAsync(ok);

			}
			else {
				throw new SmartImageException("Could not upload query");
			}
		}
		catch (Exception e) {
			AnsiConsole.WriteException(e);
			return ConsoleFormat.EC_ERROR;
		}

		var gr = CreateConfigGrid();
		AnsiConsole.Write(gr);

		Console.CancelKeyPress += OnCancelKeyPress;

		/*
		 *
		 * todo
		 */

		Task run;

#if !UNITTEST
		run = AnsiConsole.Live(m_table)
			.StartAsync(RunSearchLiveAsync);
#else
		run = RunSearchLiveAsync(null);

#endif

		run = run.ContinueWith((c) =>
		{
			//
			Prm_Engine.Choices.AddRange(m_results);
		});

		if (!String.IsNullOrWhiteSpace(m_scs.Command)) {
			run = run.ContinueWith(RunCompletionCommandAsync, m_cts.Token,
			                       TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
		}

		if (!String.IsNullOrWhiteSpace(m_scs.OutputFile)) {
			switch (m_scs.OutputFileFormat) {

				case OutputFileFormat.None:
					break;

				case OutputFileFormat.Delimited:
					run = run.ContinueWith(WriteOutputFile, m_cts.Token,
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

				if (cmd != R2.Chc_Exit) {
					var sr  = GetResultPrompt();
					var num = GetNumberPrompt(sr);
					var res = sr.Results[num];

					if (cmd == R2.Chc_Open) {
						SearchClient.OpenResult(res.Url);
					}
					else if (cmd == R2.Chc_Scan) {
						var run3 = ShowImageScanResultsAsync(res);
						await run3;

						var cmd2 = GetCommand2Prompt();

						if (cmd2 == "back") {
							goto cont;

						}
						else if (cmd2 == "calculate") {
							var hashOk = res.HashAsync(Query);

							// var row    = dict[item];
							// table2.Rows.Update(row, 2, new Text(item.Similarity.ToString()));
						}

					}
				}
				else { }

			cont:
				continue;
			} while (cmd != R2.Chc_Exit);

			// run2 = ShowImageScanResultsAsync(item);
			// await run2;
		}

		if (m_scs.KeepOpen.HasValue && m_scs.KeepOpen.Value) {
			while (!AnsiConsole.Confirm("Exit")) {
				// ...
			}
		}

		return ConsoleFormat.EC_OK;
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

			m_results2.TryAdd(result, -1);

			/*var txt  = new Text(result.Engine.Name, GetEngineStyle(result.Engine.EngineOption));
			var txt2 = new Text($"{result.Results.Count}");

			m_mainTable.AddRow(txt, txt2);*/

			var rows = CreateResultRows(result);

			foreach (IRenderable[] row in rows) {
				m_table.AddRow(row);
				m_results2[result] = m_table.Rows.Count-1;

			}

			c.Refresh();
		}

		await search;

	}

	private int GetRow(object o)
	{
		if (o is SearchResultItem sri) {
			var sr = sri.Root;
			return m_results2[sr] + sr.Results.IndexOf(sri);


		}
	}


	private async ValueTask ShowImageScanResultsAsync(SearchResultItem item)
	{
		/*var  gr2 = new Grid();
		gr2.AddColumns(5);
		gr2.AddRow(CreateResultItemRows(item, 0, Style.Plain));

		// AnsiConsole.Write(gr2);

		var tr = new Tree(gr2);
		var ld = AnsiConsole.Live(tr);*/

		// var table2 = CreateResultTable();
		// table2.AddRow(CreateResultItemRows(item, 0, Style.Plain));
		// var ld   = AnsiConsole.Live(table2);

		if (item.HasUni) {
			Trace.WriteLine($"{item} cached");

			int i   = 0;
			var row = GetRow(item);

			foreach (var ui in item.Uni) {

				m_table.InsertRow(row++, CreateUniImageRow(ui, item, i));
				i++;
			}
		}
		else {

			// var ok = await r.ScanAsync();
			Trace.WriteLine($"Scanning {item}");
			var resOk = await item.ScanAsync();

			if (resOk) {
				var row = GetRow(item);

				for (int i = 0; i < item.Uni.Length; i++) {

					m_table.InsertRow(row++, CreateUniImageRow(item.Uni[i], item, i));
					
				}
			}

		}


	}

	private static string GetCommand2Prompt()
	{
		var textPrompt = new TextPrompt<string>(Markup.Escape("[Command 2]"))
		{
			ShowChoices = true,
			Choices =
			{
				"calculate", "back"
			}
		};

		var prompt = AnsiConsole.Prompt(textPrompt);

		return prompt;
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

		AnsiConsole.WriteLine($"Process id: {commandTask.ProcessId}");

		var result = await commandTask;

		AnsiConsole.WriteLine($"Process successful: {result.IsSuccess}");
	}

	private void WriteOutputFile([CBN] object o)
	{
		Debug.WriteLine($"{nameof(WriteOutputFile)}");

		var fw = File.OpenWrite(m_scs.OutputFile);

		var sw = new StreamWriter(fw)
		{
			AutoFlush = true
		};

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

		foreach (SearchResult sr in m_results) {
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

		AnsiConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
	}

	#endregion

	#region

	private int GetNumberPrompt(SearchResult result)
	{
		var prompt = Prm_Num.Validate(i =>
		{
			if (i < result.Results.Count && i >= 0) {
				return ValidationResult.Success();
			}

			return ValidationResult.Error("Out of range");
		});


		return AnsiConsole.Prompt(prompt);
	}

	private string GetCommandPrompt()
	{
		return AnsiConsole.Prompt(Prm_Command);
	}

	private SearchResult GetResultPrompt()
	{

		return AnsiConsole.Prompt(Prm_Engine);
	}

	private static IRenderable[] CreateUniImageRow(UniImage ui, SearchResultItem sri, int idx)
	{
		// var url = ui is UniImageUri uiu ? uiu.Url.ToString() : String.Empty;

		if (ui is UniImageUri uiu) {
			Debug.Assert(uiu.Url == ui.ValueString);
		}

		return
		[
			new Text($"{sri.Root.Engine.Name} {idx}", new Style(link: ui.ValueString)),
			new Text(ui.ValueString),
			ConsoleFormat.Txt_Empty,
			ConsoleFormat.Txt_Empty,
			ConsoleFormat.Txt_Empty
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
			url = new Markup(Markup.Escape(link.ToString()), new Style(link: link));
		}
		else {
			url = ConsoleFormat.Txt_Default;
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
			MaxWidth = AnsiConsole.Profile.Width / 6,

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
		AnsiConsole.MarkupLine($"[red]Cancellation requested[/]");
		AnsiConsole.Clear();
		m_cts.Cancel();
		args.Cancel = false;

		Environment.Exit(ConsoleFormat.EC_ERROR);
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

		Prm_Num.Validator = null;
		Prm_Engine.Choices.Clear();
		m_results.Clear();
		m_results2.Clear();
		m_cts.Dispose();
		m_scs = null;
		Client.Dispose();
		Query.Dispose();
	}

	private static readonly TextPrompt<string> Prm_Command = new(Markup.Escape("[Command]"))
	{
		ShowChoices      = true,
		ShowDefaultValue = true,
		AllowEmpty       = false,
		Choices =
		{
			R2.Chc_Open, R2.Chc_Scan, R2.Chc_Exit
		}
	};

	private static readonly TextPrompt<SearchResult> Prm_Engine = new(Markup.Escape("[Engine]"))
	{
		ShowChoices      = false,
		ShowDefaultValue = false,
		AllowEmpty       = false,
		Converter = s =>
		{
			//
			return s.Engine.Name;
		}
	};

	private static readonly TextPrompt<int> Prm_Num = new(Markup.Escape("[#]"))
	{
		ShowChoices      = false,
		ShowDefaultValue = false,
		AllowEmpty       = false,
	};

}