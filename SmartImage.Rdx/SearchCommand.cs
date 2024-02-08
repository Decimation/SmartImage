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
		if (!String.IsNullOrWhiteSpace(m_cc)) {
			var proc = new Process()
			{
				StartInfo =
				{
					FileName  = m_ce,
					Arguments = m_cc,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};
			proc.Start();

			Debug.WriteLine($"starting {proc.Id}");
			// proc.WaitForExit(TimeSpan.FromSeconds(3));
			// proc.Dispose();
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
				var stream = Query.Uni.Stream;
				stream.TrySeek(0);

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

		/*var fn = rows.GetType()
			.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.FirstOrDefault(x => x.Name.Contains("get_Item"));

		var res = (TableRow) fn.Invoke(rows, new Object[] { i-1 });
		var en  = res.GetEnumerator();

		while (en.MoveNext()) {
			var it = en.Current;

			AConsole.AlternateScreen(() =>
			{
				AConsole.Clear();
				AConsole.Write(t);
				AConsole.Confirm("");
			});

			if (it is Table t) { }
		}*/

	}

	private string m_cc, m_ce;

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
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
		m_cc                   = settings.CompletionCommand;
		m_ce                   = settings.CompletionExecutable;
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

		var format = settings.Format;
		var grid   = CliFormat.GetGridForFormat(format);

		var live = AConsole.Live(grid)
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

					var allResults = sr.GetAllResults();

					foreach (var item in allResults) {
						var rows = CliFormat.GetRowsForFormat(item, i, format);
						grid.AddRow(rows);
						i++;
					}

					/*m_resTable.Rows.Add(new IRenderable[]
					{
						new Text($"{rm.Id}"),
						Markup.FromInterpolated($"[bold]{sr.Engine.Name}[/]"),
						new Text($"{sr.Results.Count}")
					});*/

					l.Refresh();

				}
			});

		await live;

		if (settings.Interactive) {
			await RunInteractiveAsync();
		}

		return 0;
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
		Client.Dispose();
		Query.Dispose();
	}

}