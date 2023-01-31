// Read Stanton SmartImage CliMain.cs
// 2023-01-30 @ 10:37 PM

using System.Collections.Concurrent;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Spectre.Console;

// ReSharper disable InconsistentNaming

namespace SmartImage;

public class CliMain : IDisposable
{
	private const int COMPLETE = 100;

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
	
	static CliMain()
	{
	}

	private static readonly Progress Prg_1 = AnsiConsole.Progress().Columns(PrgCol_1);

	public static async Task RunCliAsync(string c)
	{
		var q = await Prg_1.StartAsync(async ctx =>
		{
			var t = ctx.AddTask("Validating input");

			var qInner = await SearchQuery.TryCreateAsync(c);
			t.Increment(COMPLETE);
			return qInner;
		});

		AnsiConsole.WriteLine($"{q}");

		var url = await Prg_1.StartAsync(async (p) =>
		{
			var pt = p.AddTask($"Upload");
			var  urlInner= await q.UploadAsync();
			pt.Increment(COMPLETE);
			return urlInner;

		});

		AnsiConsole.MarkupLine($"[green]{q.Upload}[/]");

		var cfg = new SearchConfig();
		var sc  = new SearchClient(cfg);

		AnsiConsole.WriteLine($"{cfg}");

		var cts = new CancellationTokenSource();

		System.Console.CancelKeyPress += (sender, args) =>
		{
			args.Cancel = false;
			cts.Cancel();
			AnsiConsole.MarkupLine($"[red]Cancellation requested {args}[/]");
		};

		var results = await Prg_1.StartAsync(async ctx =>
		{
			var ptMap = new Dictionary<BaseSearchEngine, ProgressTask>();

			foreach (var e in sc.Engines) {
				var t = ctx.AddTask($"{e}");
				ptMap.Add(e, t);
			}

			var pt1 = ctx.AddTask("[yellow]Searching[/]");
			var rg  = new ConcurrentBag<SearchResult>();

			sc.OnComplete += (sender, searchResults) =>
			{
				pt1.Increment(COMPLETE);
			};

			sc.OnResult += (sender, result) =>
			{
				ptMap[result.Engine].Increment(COMPLETE);
				pt1.Increment((double) rg.Count / sc.Engines.Length);

			};

			var resultsInner = await sc.RunSearchAsync(q, cts.Token);

			return resultsInner;
		});

		foreach (SearchResult sr in results) {
			AnsiConsole.MarkupLine($"[cyan]{sr.Engine.Name}[/]");
			foreach (SearchResultItem sri in sr.Results) {
				AnsiConsole.MarkupLine($"[link={sri.Url}]{sr.Engine} #[/]");

			}
		}
	}

	public void Dispose() { }
}