// Read S SmartImage.Rdx AppMode.cs
// 2023-07-05 @ 12:45 PM

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using Kantan.Net.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx;

public sealed class SearchMode : IDisposable
{

	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config => Client.Config;

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<ResultModel> m_results;

	private readonly Table m_resTable;

	private const int COMPLETE = 100;

	private SearchMode(SearchQuery sq)
	{
		Client    = new SearchClient(new SearchConfig());
		m_results = new();
		m_cts     = new();
		Query     = sq;

		Client.OnComplete += OnComplete;
		Client.OnResult   += OnResult;

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

	public static async Task<SearchMode?> TryCreateAsync(string s)
	{

		var q = await SearchQuery.TryCreateAsync(s);

		if (q == null) {
			return null;
		}

		var m = new SearchMode(q);

		return m;
	}

	private async Task RunSearchAsync()
	{

		// pt1.MaxValue = m_client.Engines.Length;

		var run = Client.RunSearchAsync(Query, token: m_cts.Token);

		var sw = Stopwatch.StartNew();

		var live = AConsole.Live(m_resTable)
			.AutoClear(false)
			.Overflow(VerticalOverflow.Ellipsis)
			.StartAsync(async (ctx) =>
			{
				void OnResultComplete(object o, SearchResult searchResult)
				{
					ctx.Refresh();
					m_resTable.Caption = new TableTitle($"{sw.Elapsed.TotalSeconds:F3}");
					// await Task.Delay(1000);

				}

				Client.OnResult += OnResultComplete;

				while (!run.IsCompleted) { }

				Client.OnResult -= OnResultComplete;

			});

		await run;

		await live;

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

	public async Task<object?> RunAsync(object? c)
	{
		var cstr = (string?) c;
		Debug.WriteLine($"Input: {cstr}");

		AConsole.WriteLine($"Input: {Query}");

		await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
		{
			var p = ctx.AddTask("Uploading");
			p.IsIndeterminate = true;
			var url = await Query.UploadAsync();

			if (url == null) {
				throw new SmartImageException(); //todo
			}

			p.Increment(COMPLETE);
			ctx.Refresh();
		});

		AConsole.MarkupLine($"[green]{Query.Upload}[/]");

		AConsole.WriteLine($"{Config}");

		Console.CancelKeyPress += (sender, args) =>
		{
			AConsole.MarkupLine($"[red]Cancellation requested[/]");
			m_cts.Cancel();
			args.Cancel = false;

			Environment.Exit(-1);
		};

		// await Prg_1.StartAsync(RunSearchAsync);

		await RunSearchAsync();

		return null;

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

public class ResultModel : IDisposable
{

	public SearchResult Result { get; }

	public Table Table { get; }

	public int Id { get; }

	public ResultModel(SearchResult result) : this(result, Interlocked.Increment(ref cnt)) { }

	public ResultModel(SearchResult result, int id)
	{
		Result = result;
		Id     = id;
		Table  = Create();
	}

	public Table Create()
	{
		var table = CreateTable();

		int i = 0;

		foreach (SearchResultItem sri in Result.Results) {
			table.Rows.Add(new IRenderable[]
			{
				new Text($"{i + 1}"),
				Markup.FromInterpolated($"[link={sri.Url}]{sri.Root.Engine.Name} #{i + 1}[/]"),
				Markup.FromInterpolated($"{sri.Similarity}"),
				Markup.FromInterpolated($"{sri.Artist}"),
				Markup.FromInterpolated($"{sri.Description}"),
				Markup.FromInterpolated($"{sri.Character}"),
			});

			i++;
		}

		return table;
	}

	public static int cnt = 0;

	public void Dispose()
	{
		Result.Dispose();
	}

	public static Table CreateTable()
	{
		var table = new Table()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"Results"),
			ShowFooters = false,
			ShowHeaders = false,
			Expand      = false,
		};

		table.AddColumns(new TableColumn("#"),
		                 new TableColumn("Name"),
		                 new TableColumn("Similarity"),
		                 new TableColumn("Artist"),
		                 new TableColumn("Description"),
		                 new TableColumn("Character")
		);

		return table;
	}

}