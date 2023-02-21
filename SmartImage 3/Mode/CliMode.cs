// Read Stanton SmartImage CliMain.cs
// 2023-01-30 @ 10:37 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SmartImage.Lib;
using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Rendering;
using AConsole = Spectre.Console.AnsiConsole;

// ReSharper disable InconsistentNaming

namespace SmartImage.Mode;

public sealed class CliMode : IDisposable, IMode, IProgress<int>
{
	private const int COMPLETE = 100;

	#region

	static CliMode()
	{
		Debug.WriteLine($"{AConsole.Profile.Capabilities.Unicode} {AConsole.Profile.Capabilities.Links}");

	}

	#endregion

	private readonly ConcurrentBag<SearchResult> m_results = new();

	private SearchResult[] m_results2;

	private SearchQuery m_query;

	private readonly SearchClient m_client;

	private readonly CancellationTokenSource m_cts;

	public SearchConfig Config { get; }

	public CliMode()
	{
		Config   = new SearchConfig();
		m_client = new SearchClient(Config);
		m_query  = SearchQuery.Null;
		m_cts    = new CancellationTokenSource();
	}

	public async Task<object?> RunAsync(object? c)
	{
		await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
		{
			var p = ctx.AddTask("Creating query");
			p.IsIndeterminate = true;
			m_query           = await SearchQuery.TryCreateAsync((string) c!);
			p.Increment(COMPLETE);
			ctx.Refresh();
		});

		AConsole.WriteLine($"Input: {m_query}");
		
		await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
		{
			var p = ctx.AddTask("Uploading");
			p.IsIndeterminate = true;
			var url = await m_query.UploadAsync();

			p.Increment(COMPLETE);
			ctx.Refresh();
		});

		AConsole.MarkupLine($"[green]{m_query.Upload}[/]");

		AConsole.WriteLine($"{Config}");

		SConsole.CancelKeyPress += (sender, args) =>
		{
			args.Cancel = false;
			m_cts.Cancel();
			AConsole.MarkupLine($"[red]Cancellation requested[/]");
			Environment.Exit(-1);
		};

		// await Prg_1.StartAsync(RunSearchAsync);

		await RunSearchAsync();

		return null;

	}

	private async Task RunSearchAsync()
	{

		void OnComplete(object sender, SearchResult[] searchResults)
		{
			// pt1.Increment(COMPLETE);
		}

		m_client.OnComplete += OnComplete;

		var mt = new Table()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"Results"),
			ShowFooters = true,
			ShowHeaders = true,
		};

		mt.AddColumns(new TableColumn("#"),
		              new TableColumn("Name"),
		              new TableColumn("Similarity"),
		              new TableColumn("Artist"),
		              new TableColumn("Description"),
		              new TableColumn("Character")
		);

		// pt1.MaxValue = m_client.Engines.Length;

		void OnResult(object sender, SearchResult sr)
		{
			m_results.Add(sr);
			// ptMap[sr.Engine].Item1.Increment(COMPLETE);
			// pt1.Increment(1.0);
			int i = 0;

			// var t = ptMap[sr.Engine].Item2;

			foreach (SearchResultItem sri in sr.Results) {
				mt.Rows.Add(new IRenderable[]
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

		m_client.OnResult += OnResult;

		var run = m_client.RunSearchAsync(m_query, m_cts.Token);

		var sw = Stopwatch.StartNew();

		var live = AConsole.Live(mt)
			.AutoClear(false)
			.Overflow(VerticalOverflow.Ellipsis)
			.StartAsync(async (ctx) =>
			{
				while (!run.IsCompleted) {
					ctx.Refresh();
					mt.Caption = new TableTitle($"{sw.Elapsed.TotalSeconds:F3}");
					// await Task.Delay(1000);
				}

			});

		await run;

		await live;

	}

	public void Dispose()
	{
		m_results.Clear();
		Array.Clear(m_results2);
		m_cts.Dispose();
		m_query.Dispose();
		m_client.Dispose();
	}

	public void Report(int value)
	{
		Debug.WriteLine($"{value}");
	}
}