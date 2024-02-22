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
using System.Diagnostics;
using Flurl;
using JetBrains.Annotations;
using Kantan.Diagnostics;
using Kantan.Utilities;
using Novus.Streams;
using SixLabors.ImageSharp.Processing;
using SmartImage.Rdx.Cli;

namespace SmartImage.Rdx;
#nullable disable
internal sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; private set; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<ResultModel> m_results;

	// private readonly STable m_resTable;

	private SearchCommandSettings m_scs;

	private const double COMPLETE = 100.0d;

	public const int EC_ERROR = -1;
	public const int EC_OK    = 0;

	public SearchCommand()
	{
		Config            =  new SearchConfig();
		Client            =  new SearchClient(Config);
		Client.OnComplete += OnComplete;

		// Client.OnResult   += OnResult;
		m_cts     = new CancellationTokenSource();
		m_results = new ConcurrentBag<ResultModel>();
		m_scs     = null;
		/*m_resTable = new Table()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"Results"),
			ShowFooters = true,
			ShowHeaders = true,
		};

		m_resTable.AddColumns(new TableColumn("#"),
							  new TableColumn("Name"),
							  new TableColumn("Count")
		);*/

		Query = SearchQuery.Null;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		m_scs = settings;

		var task = AConsole.Progress()
			.AutoRefresh(true)
			.StartAsync(async ctx =>
			{
				var p = ctx.AddTask("Creating query");
				p.IsIndeterminate = true;
				Query             = await SearchQuery.TryCreateAsync(settings.Query);

				p.Increment(COMPLETE / 2);
				// ctx.Refresh();

				p.Description = "Uploading query";
				var url = await Query.UploadAsync();

				if (url == null) {
					throw new SmartImageException(); //todo
				}

				p.Increment(COMPLETE / 2);
				// ctx.Refresh();
			});

		Config.SearchEngines   = settings.SearchEngines;
		Config.PriorityEngines = settings.PriorityEngines;
		Config.AutoSearch      = settings.AutoSearch;

		await Client.ApplyConfigAsync();

		await task;

		var dt = Config.ToTable();

		var t = CliFormat.DTableToSTable(dt);

		AConsole.Write(t);

		AConsole.WriteLine($"Input: {Query}");

		Console.CancelKeyPress += (sender, args) =>
		{
			AConsole.MarkupLine($"[red]Cancellation requested[/]");
			AConsole.Clear();
			m_cts.Cancel();
			args.Cancel = false;

			Environment.Exit(EC_ERROR);
		};

		// await Prg_1.StartAsync(RunSearchAsync);

		// pt1.MaxValue = m_client.Engines.Length;

		Act act;

		if (m_scs.ResultFormat == ResultTableFormat.None) {
			act = await RunSimpleAsync();
		}
		else {
			act = await RunTableAsync();

		}
		
		if (settings.Interactive) {
			act = await RunInteractiveAsync();

		}

		return (int) act;
	}

	private async Task<Act> RunSimpleAsync()
	{
		var prog = AConsole.Progress()
			.AutoRefresh(false)
			.StartAsync(async c =>
		{
			var p = c.AddTask("Running search");
			p.IsIndeterminate = true;
			var cnt = (double) Client.Engines.Length;
			var p2  = c.AddTask("Engines", maxValue: cnt);

			Client.OnResult += OnResultComplete;

			var run = Client.RunSearchAsync(Query, token: m_cts.Token);

			await run;

			Client.OnResult -= OnResultComplete;

			return;

			void OnResultComplete(object sender, SearchResult sr)
			{
				int i = 0;

				var rm = new ResultModel(sr)
					{ };

				m_results.Add(rm);
				p2.Description = $"{rm.Result.Engine.Name} {m_results.Count} / {cnt}";
				p2.Increment(1);
				c.Refresh();
			}
		});

		await prog;

		return Act.None;
	}

	private async Task<Act> RunTableAsync()
	{
		var format = m_scs.ResultFormat;
		var table  = CliFormat.GetTableForFormat(format);

		var live = AConsole.Live(table)
			.StartAsync(async (l) =>
			{

				Client.OnResult += OnResultComplete;

				var run = Client.RunSearchAsync(Query, token: m_cts.Token);

				await run;

				Client.OnResult -= OnResultComplete;
				return;

				void OnResultComplete(object sender, SearchResult sr)
				{
					int i = 0;

					var rm = new ResultModel(sr)
						{ };

					m_results.Add(rm);

					if (!sr.IsStatusSuccessful) {
						// Debugger.Break();
						var rows = rm.GetRowsForFormat(format);
						table.AddRow(rows);
					}
					else {
						var results = rm.GetRowsForFormat2(format);

						foreach (IRenderable[] allResult in results) {
							table.AddRow(allResult);

						}
					}

					rm.UpdateGrid();

					l.Refresh();

				}
			});

		await live;

		return Act.None;
	}

	public async Task<Act> RunInteractiveAsync()
	{

		AConsole.Clear();

		//todo

		int i   = 0;
		var gr1 = new Grid();
		gr1.AddColumns(2);

		foreach (ResultModel result in m_results) {
			gr1.AddRow($"{result.Result.Engine.Name}", $"{i++}");
		}

		var res = m_results.ToArray();

		ConsoleKeyInfo prompt = default;

		// Create the layout
		var layout = new Layout("Root")
			.SplitColumns(
				new Layout("Left", gr1) { },
				new Layout("Right") { }
					.SplitRows(
						new Layout("Top") { },
						new Layout("Bottom") { }));

		do {
			// prompt = AConsole.Prompt(choices);

			AConsole.Clear();
			layout["Left"].Update(new Panel(gr1));
			AConsole.Write(layout);

			prompt = Console.ReadKey(true);

			var keyChar = prompt.KeyChar;
			var idx     = (int) Char.GetNumericValue(keyChar);

			var b = idx >= 0 && idx < res.Length;

			if (b) {
				var rm = res[idx];
				Debug.WriteLine($"{prompt} {idx} {rm}");

				if (Query.Uni == null) {
					throw new SmartImageException();
				}

				var stream = Query.Uni.Stream;
				stream.TrySeek();

				var gr2 = rm.UpdateGrid(clear: true);

				// Update the left column
				layout["Left"].Update(new Panel(rm.Grid));

				// layout["Top"].Update(new Panel(gr2));

				AConsole.Clear();
				AConsole.Write(layout);

				do {
					prompt = Console.ReadKey(true);

					if (prompt.Key == ConsoleKey.Backspace) {
						break;
					}

					/*
					if (prompt.Key == ConsoleKey.Backspace) {
						continue;
					}*/
					var keyChar2 = prompt.KeyChar;
					var idx2     = (int) Char.GetNumericValue(keyChar2);

					// var val      = rm.Grid.Rows[choice2i][0];
					// var val2     = new Text(val.ToString(), style: new Style(Color.Yellow));
					// Debug.WriteLine($"{val} {val2}");

					gr2 = rm.UpdateGrid(idx2, true);

					layout["Left"].Update(new Panel(rm.Grid));
					layout["Top"].Update(new Panel(gr2));

					AConsole.Clear();
					AConsole.Write(layout);

				} while (true);

			}
			else { }

		} while (prompt.Key != ConsoleKey.OemMinus);

		return Act.None;
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;

		// var v= base.Validate(context, settings);
		// return v;
	}

	private void OnComplete(object sender, SearchResult[] searchResults)
	{
		// pt1.Increment(COMPLETE);
		if (!String.IsNullOrWhiteSpace(m_scs.CompletionCommand)) {
			var proc = new Process()
			{
				StartInfo =
				{
					FileName               = m_scs.CompletionExecutable,
					Arguments              = m_scs.CompletionCommand,
					UseShellExecute        = false,
					CreateNoWindow         = true,
					RedirectStandardError  = true,
					RedirectStandardOutput = true
				}
			};
			proc.Start();

			Debug.WriteLine($"starting {proc.Id}");

			// proc.WaitForExit(TimeSpan.FromSeconds(3));
			// proc.Dispose();
		}

		switch (m_scs.OutputFormat) {

			case ResultFileFormat.None:
				break;

			case ResultFileFormat.Csv:
				var fw = File.OpenWrite(m_scs.OutputFile);

				var sw = new StreamWriter(fw)
				{
					AutoFlush = true
				};
				var res = m_results.ToArray();

				for (int i = 0; i < res.Length; i++) {
					var sr = res[i].Result;

					for (int j = 0; j < sr.Results.Count; j++) {
						var sri = sr.Results[j];

						string[] items = [$"{sr.Engine.Name} #{j + 1}", sri.Url?.ToString()];
						sw.WriteLine(String.Join(',', items));
					}

				}

				sw.Dispose();
				fw.Dispose();
				AConsole.WriteLine($"Wrote to {m_scs.OutputFile}");
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}

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

public enum Act
{

	None = 0,
	Exit,
	Back,
	Restart

}