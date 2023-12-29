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

namespace SmartImage.Rdx.Cli;

internal sealed class SearchCommand : AsyncCommand<SearchCommandSettings>, IDisposable
{

	public override ValidationResult Validate(CommandContext context, SearchCommandSettings settings)
	{

		var b = SearchQuery.IsValidSourceType(settings.Query);

		return b ? ValidationResult.Success() : ValidationResult.Error();
		// var v= base.Validate(context, settings);
		// return v;
	}

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config { get; private set; }

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<ResultModel> m_results;

	private readonly STable m_resTable;

	private const int COMPLETE = 100;

	public SearchCommand()
	{
		Config            =  new SearchConfig();
		Client            =  new SearchClient(Config);
		Client.OnComplete += OnComplete;
		Client.OnResult   += OnResult;
		m_cts             =  new CancellationTokenSource();
		m_results         =  new ConcurrentBag<ResultModel>();

		m_resTable = new Table()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"Results"),
			ShowFooters = true,
			ShowHeaders = true,
		};

		m_resTable.AddColumns(new TableColumn("#"),
		                      new TableColumn("Name"),
		                      new TableColumn("Count")
		);
		Query = SearchQuery.Null;
	}

	private void OnResult(object sender, SearchResult sr)
	{
		// Interlocked.Increment(ref ResultModel.cnt);
		var rm = new ResultModel(sr) { };
		m_results.Add(rm);

		m_resTable.Rows.Add(new IRenderable[]
		{
			new Text($"{rm.Id}"),
			Markup.FromInterpolated($"[bold]{sr.Engine.Name}[/]"),
			new Text($"{sr.Results.Count}")
		});

		// AnsiConsole.Write(t);
	}

	private void OnComplete(object sender, SearchResult[] searchResults)
	{
		// pt1.Increment(COMPLETE);
	}

	public async Task Interactive()
	{
		ConsoleKeyInfo cki;

		do {

			// var i = AC.Ask<int>("?");

			/*
			if (!char.IsNumber(cki.KeyChar)) {
				continue;
			}
			*/

			AC.Clear();

			//todo
			var prompt = AC.Prompt(new SelectionPrompt<string>()
				                       .Title("Engine"));

			AC.Write(m_resTable);

			var i = AC.Ask<int>("?", 0);

			if (i == 0) {
				break;
			}

			// var i = (int) char.GetNumericValue(cki.KeyChar);

			var rows = m_resTable.Rows;

			if (rows.Count == 0 || (i < 0 || i > m_results.Count)) {
				continue;
			}

			var rr = m_results.FirstOrDefault(x => x.Id == i);

			if (rr == null) {
				continue;
			}

			AConsole.AlternateScreen(() =>
			{
				AC.Write(rr.Table);
				// Console.ReadKey();
				var n = AC.Ask<int>("?");

				if (n == 0 || (n < 0 || n > rr.Result.Results.Count)) {
					return;
				}

				var res = rr.Result.Results[n];
				HttpUtilities.TryOpenUrl(res.Url);
			});

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

			switch (i) { }

		} while (true);
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

		var run = await Client.RunSearchAsync(Query, token: m_cts.Token);
		
		if (settings.Interactive) {
			await Interactive();
		}

		return 0;
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