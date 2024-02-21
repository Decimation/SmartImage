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

internal sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; private set; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<ResultModel> m_results;

	// private readonly STable m_resTable;

	private const int COMPLETE = 100;

	private SearchCommandSettings m_scs;

	public SearchCommand()
	{
		Config            =  new SearchConfig();
		Client            =  new SearchClient(Config);
		Client.OnComplete += OnComplete;

		// Client.OnResult   += OnResult;
		m_cts     = new CancellationTokenSource();
		m_results = new ConcurrentBag<ResultModel>();

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

	public async Task RunInteractiveAsync()
	{
		ConsoleKeyInfo cki;

		// var i = AC.Ask<int>("?");

		/*
		if (!char.IsNumber(cki.KeyChar)) {
			continue;
		}
		*/

		AConsole.Clear();

		//todo

		var select = m_results
			.ToDictionary((x) =>
			{
				return x.Result.Engine.Name;
			});

		var choices = new SelectionPrompt<string>()
			.Title("Engine")
			.AddChoices(select.Keys);

		choices.AddChoice("Quit");
		choices.AddChoice("...");

		string prompt = null;

		// AConsole.Write(m_resTable);

		while (prompt != "") {
			prompt = AConsole.Prompt(choices);

			if (select.TryGetValue(prompt, out var v)) {

				AConsole.Clear();
				AConsole.Write(v.Table);

			}
			else {
				if (Query.Uni == null) {
					throw new SmartImageException();
				}

				var stream = Query.Uni.Stream;
				stream.TrySeek();

				// Create the layout
				var layout = new Layout("Root")
					.SplitColumns(
						new Layout("Left"),
						new Layout("Right")
							.SplitRows(
								new Layout("Top"),
								new Layout("Bottom")));

				// Update the left column
				layout["Left"].Update(
					new Panel(new CanvasImage(stream))
						.Expand());
				AConsole.Clear();
				AConsole.Write(layout);

			}
		}

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

				p.Increment(50);
				ctx.Refresh();

				p.Description = "Uploading query";
				var url = await Query.UploadAsync();

				if (url == null) {
					throw new SmartImageException(); //todo
				}

				p.Increment(50);
				ctx.Refresh();
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
			m_cts.Cancel();
			args.Cancel = false;

			Environment.Exit(-1);
		};

		// await Prg_1.StartAsync(RunSearchAsync);

		// pt1.MaxValue = m_client.Engines.Length;

		await RunTableAsync();

		if (settings.Interactive) {
			await RunInteractiveAsync();
		}

		return 0;
	}

	private async Task RunTableAsync()
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
					var rm = new ResultModel(sr) { };
					m_results.Add(rm);
					int i = 0;

					if (!sr.IsStatusSuccessful) {
						// Debugger.Break();
						var rows = CliFormat.GetRowsForFormat(sr, format);
						table.AddRow(rows);
					}
					else {
						var allResults = sr.GetAllResults();

						foreach (var item in allResults) {
							var rows = CliFormat.GetRowsForFormat(item, i, format);
							table.AddRow(rows);
							i++;
						}

					}

					l.Refresh();

				}
			});

		await live;
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{
		var r = base.Validate(context, settings);
		return r;

		// var v= base.Validate(context, settings);
		// return v;
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