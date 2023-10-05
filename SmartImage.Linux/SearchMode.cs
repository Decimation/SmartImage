// Read S SmartImage.Linux AppMode.cs
// 2023-07-05 @ 12:45 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using SmartImage.Lib;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Linux;

public sealed class SearchMode : IDisposable
{
	public SearchClient Client { get; }

	public SearchQuery Query { get; private set; }

	public SearchConfig Config => Client.Config;

	private readonly CancellationTokenSource m_cts;

	private readonly ConcurrentBag<SearchResult> m_results;

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
		                      new TableColumn("Similarity"),
		                      new TableColumn("Artist"),
		                      new TableColumn("Description"),
		                      new TableColumn("Character")
		);
	}

	private void OnResult(object sender, SearchResult sr)
	{
		m_results.Add(sr);
		// ptMap[sr.Engine].Item1.Increment(COMPLETE);
		// pt1.Increment(1.0);
		int i = 0;

		// var t = ptMap[sr.Engine].Item2;

		foreach (SearchResultItem sri in sr.Results) {
			m_resTable.Rows.Add(new IRenderable[]
			{
				new Text($"{i + 1}"),
				Markup.FromInterpolated($"[link={sri.Url}]{sr.Engine.Name} #{i + 1}[/]"),
				Markup.FromInterpolated($"{sri.Similarity}"),
				Markup.FromInterpolated($"{sri.Artist}"),
				Markup.FromInterpolated($"{sri.Description}"),
				Markup.FromInterpolated($"{sri.Character}"),
			});

			i++;
		}

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
				while (!run.IsCompleted) {
					ctx.Refresh();
					m_resTable.Caption = new TableTitle($"{sw.Elapsed.TotalSeconds:F3}");
					// await Task.Delay(1000);
				}

			});

		await run;

		await live;

	}

	private void OnComplete(object sender, SearchResult[] searchResults)
	{
		// pt1.Increment(COMPLETE);
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