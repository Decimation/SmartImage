// Read Stanton SmartImage CliMain.cs
// 2023-01-30 @ 10:37 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

// ReSharper disable InconsistentNaming

namespace SmartImage.Mode;

public sealed class CliMode : IDisposable, IMode
{
	#region

	private const double COMPLETE = 100.0D;

	private static readonly ProgressColumn[] PrgCol_1 =
	{
		new TaskDescriptionColumn()
		{
			Alignment = Justify.Left
		},
		new SpinnerColumn(),
		new ElapsedTimeColumn(),
		new ProgressBarColumn()
	};

	private static readonly Progress Prg_1 = AnsiConsole.Progress()
		.AutoClear(true)
		.AutoRefresh(true)
		.HideCompleted(true)
		.Columns(PrgCol_1);

	static CliMode() { }

	#endregion

	private readonly ConcurrentBag<SearchResult> m_results = new();

	private SearchResult[] m_results2;

	private readonly SearchConfig m_cfg;

	private SearchQuery m_query;

	private readonly SearchClient m_client;

	private readonly CancellationTokenSource m_cts;

	public CliMode()
	{
		m_cfg    = new SearchConfig();
		m_client = new SearchClient(m_cfg);
		m_query  = SearchQuery.Null;
		m_cts    = new CancellationTokenSource();
	}

	public async Task<object?> RunAsync(object? c)
	{

		// await Prg_1.StartAsync(ctx => ValidateInputAsync(ctx, c as string));
		await ValidateInputAsync(null, c as string);
		AnsiConsole.WriteLine($"{m_query}");

		// var url = await Prg_1.StartAsync(UploadInputAsync);

		var url = await UploadInputAsync(null);
		AnsiConsole.MarkupLine($"[green]{m_query.Upload}[/]");

		AnsiConsole.WriteLine($"{m_cfg}");

		Console.CancelKeyPress += (sender, args) =>
		{
			args.Cancel = false;
			m_cts.Cancel();
			AnsiConsole.MarkupLine($"[red]Cancellation requested {args}[/]");
		};

		// await Prg_1.StartAsync(RunSearchAsync);

		await RunSearchAsync(null);
		return null;

	}

	private async Task ValidateInputAsync(ProgressContext ctx, string c)
	{
		// var t = ctx.AddTask("Validating input");
		// t.IsIndeterminate = true;

		m_query = await SearchQuery.TryCreateAsync(c);

		// t.Increment(COMPLETE);
	}

	private async Task<Url> UploadInputAsync(ProgressContext p)
	{
		// var pt = p.AddTask($"Upload");
		// pt.IsIndeterminate = true;
		var urlInner = await m_query.UploadAsync();
		// pt.Increment(COMPLETE);
		return urlInner;

	}

	private async Task RunSearchAsync(ProgressContext ctx)
	{
		/*var ptMap = new Dictionary<BaseSearchEngine, (ProgressTask, Table)>();

		foreach (var e in m_client.Engines) {
			var t  = ctx.AddTask($"{e}");
			var tt = get_table(e);
			t.IsIndeterminate = true;
			ptMap.Add(e, (t, tt));
		}

		var pt1 = ctx.AddTask("[yellow]Searching[/]");
		pt1.IsIndeterminate = false;*/

		void OnComplete(object sender, SearchResult[] searchResults)
		{
			// pt1.Increment(COMPLETE);
		}

		var ptMap = new Dictionary<BaseSearchEngine, (object, Table)>();

		foreach (var e in m_client.Engines) {
			var tt = get_table(e);
			ptMap.Add(e, (this, tt));
		}

		m_client.OnComplete += OnComplete;

		// pt1.MaxValue = m_client.Engines.Length;

		void OnResult(object sender, SearchResult sr)
		{
			m_results.Add(sr);
			// ptMap[sr.Engine].Item1.Increment(COMPLETE);
			// pt1.Increment(1.0);
			int i = 0;

			var t = ptMap[sr.Engine].Item2;

			foreach (SearchResultItem sri in sr.Results) {
				t.Rows.Add(new IRenderable[]
				{
					new Text($"{i + 1}"),
					Markup.FromInterpolated($"[link={sri.Url}]{sr.Engine.Name} #{i + 1}[/]")
				});

				i++;
			}

			// AnsiConsole.Write(t);
		}

		m_client.OnResult += OnResult;

		var ttt = m_client.RunSearchAsync(m_query, m_cts.Token);

		/*
		while (!pt1.IsFinished) { }*/
		var sw = Stopwatch.StartNew();

		var sp = AnsiConsole.Status().Spinner(Spinner.Known.Aesthetic).StartAsync("Wait...", async ctx =>
		{
			// await ttt;

			while (!ttt.IsCompleted) {
				ctx.Refresh();
				await Task.Delay(TimeSpan.FromMilliseconds(300));

				ctx.Status =
					$"{m_results.Count} | {m_results.Sum(c => c.Results.Count)} | {sw.Elapsed.TotalSeconds:3F}";
			}
			// m_results2 = await ttt;
		});

		await ttt;

		/*var ld = AnsiConsole.Live(new Table()
									  { });
		ld.StartAsync(async (x) =>
		{
			return;
		});*/
		// pt1.StopTask();

		/*while (!pt1.IsFinished || ptMap.Any(s => !s.Value.Item1.IsFinished)) {
			await Task.Delay(TimeSpan.FromMilliseconds(100));
		}*/

		// AnsiConsole.Clear();

		await sp;

		foreach (var vt in ptMap.Values) {
			// vt.Item1.StopTask();
			AnsiConsole.Write(vt.Item2);
		}

	}

	private static Table get_table(BaseSearchEngine bse)
	{
		var tt = new Table()
		{
			Border      = TableBorder.Heavy,
			Title       = new($"{bse.Name}"),
			ShowFooters = true,
			ShowHeaders = true,
		};

		tt.AddColumns(new TableColumn("#"), new TableColumn("Link"));

		return tt;
	}

	public void Dispose()
	{
		m_results.Clear();
		Array.Clear(m_results2);
		m_cts.Dispose();
		m_query.Dispose();
		m_client.Dispose();
	}
}