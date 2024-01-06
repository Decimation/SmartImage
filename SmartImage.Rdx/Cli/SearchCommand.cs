// Read S SmartImage.Rdx SearchCommand.cs
// 2023-07-05 @ 2:07 AM

global using R2 = SmartImage.Rdx.Resources;
global using R1 = SmartImage.Lib.Resources;
global using AC = Spectre.Console.AnsiConsole;
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
using Novus.Streams;
using SixLabors.ImageSharp.Processing;

namespace SmartImage.Rdx.Cli;

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
	}

	public async Task Interactive()
	{
		ConsoleKeyInfo cki;

		// var i = AC.Ask<int>("?");

		/*
		if (!char.IsNumber(cki.KeyChar)) {
			continue;
		}
		*/

		AC.Clear();

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
		// AC.Write(m_resTable);

		while (prompt != "") {
			prompt = AC.Prompt(choices);

			if (select.TryGetValue(prompt, out var v)) {

				AC.Clear();
				AC.Write(v.Table);

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
				AC.Clear();
				AC.Write(layout);

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
				AC.Clear();
				AC.Write(t);
				AC.Confirm("");
			});

			if (it is Table t) { }
		}*/

	}

	public override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
	{
		var prog = AConsole.Progress()
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

		await Client.ApplyConfigAsync();

		await prog;

		Config.SearchEngines   = settings.SearchEngines;
		Config.PriorityEngines = settings.PriorityEngines;
		Config.AutoSearch      = settings.AutoSearch;

		var dt = Config.ToTable();

		var t = CliFormat.DTableToSTable(dt);

		AC.Write(t);

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

		var grid = new Grid();

		grid.AddColumns(
			new GridColumn() { Alignment = Justify.Left },
			new GridColumn() { Alignment = Justify.Center },
			new GridColumn() { Alignment = Justify.Right }
		);

		grid.AddRow([
			new Text("Engine", new Style(Color.Red, decoration: Decoration.Bold | Decoration.Underline)),
			new Text("Count", new Style(Color.Green, decoration: Decoration.Bold | Decoration.Underline)),
			new Text("Status", new Style(Color.Blue, decoration: Decoration.Bold | Decoration.Underline))
		]);

		var live = AC.Live(grid)
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
					var i = (int) sr.Engine.EngineOption;

					grid.AddRow([
						new Text(sr.Engine.Name,
						         new Style(
							         Color.FromInt32(Math.Clamp(i % (int) byte.MaxValue, byte.MinValue, byte.MaxValue)),
							         decoration: Decoration.Italic)),

						new Text($"{sr.Results.Count}",
						         new Style(Color.Wheat1,
						                   decoration: Decoration.None)),

						new Text($"{sr.Status}",
						         new Style(Color.Cyan1,
						                   decoration: Decoration.None))
					]);

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
			await Interactive();
		}

		return 0;
	}

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{

		var b = SearchQuery.IsValidSourceType(settings.Query);

		return b ? ValidationResult.Success() : ValidationResult.Error();
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