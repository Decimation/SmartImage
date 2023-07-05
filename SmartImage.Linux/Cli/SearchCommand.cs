// Read S SmartImage.Linux SearchCommand.cs
// 2023-07-05 @ 2:07 AM

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace SmartImage.Linux.Cli;

internal sealed class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    private SearchClient m_client;
    private SearchQuery? m_query;
    private SearchConfig m_config;
    private readonly CancellationTokenSource m_cts = new();
    private readonly ConcurrentBag<SearchResult> m_results = new();

    public sealed class Settings : CommandSettings
    {
        [Description("Query")]
        [CommandArgument(0, "[query]")]
        public string? Query { get; init; }

        [CommandOption("-e|--engines")]
        [DefaultValue(SearchConfig.SE_DEFAULT)]
        public SearchEngineOptions Engines { get; init; }

        [CommandOption("--autosearch")]
        [DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
        public bool AutoSearch { get; init; }
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
            Border = TableBorder.Heavy,
            Title = new($"Results"),
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

            foreach (SearchResultItem sri in sr.Results)
            {
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

        var run = m_client.RunSearchAsync(m_query, token: m_cts.Token);

        var sw = Stopwatch.StartNew();

        var live = AConsole.Live(mt)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async (ctx) =>
            {
                while (!run.IsCompleted)
                {
                    ctx.Refresh();
                    mt.Caption = new TableTitle($"{sw.Elapsed.TotalSeconds:F3}");
                    // await Task.Delay(1000);
                }

            });

        await run;

        await live;

    }

    private const int COMPLETE = 100;

    public async Task<object?> RunAsync(object? c)
    {
        var cstr = (string?)c;
        Debug.WriteLine($"Input: {cstr}");

        await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
        {
            var p = ctx.AddTask("Creating query");
            p.IsIndeterminate = true;
            m_query = await SearchQuery.TryCreateAsync(cstr);
            p.Increment(COMPLETE);
            ctx.Refresh();
        });

        AConsole.WriteLine($"Input: {m_query}");

        await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
        {
            var p = ctx.AddTask("Uploading");
            p.IsIndeterminate = true;
            var url = await m_query.UploadAsync();

            if (url == null)
            {
                throw new SmartImageException(); //todo
            }

            p.Increment(COMPLETE);
            ctx.Refresh();
        });

        AConsole.MarkupLine($"[green]{m_query.Upload}[/]");

        AConsole.WriteLine($"{m_config}");

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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        m_config = new SearchConfig()
        {
            SearchEngines = settings.Engines,
            AutoSearch = settings.AutoSearch
        };

        m_client = new SearchClient(m_config);

        var r = await RunAsync(settings.Query);
        AC.WriteLine($"{m_query}");

        foreach (var sr in m_results)
        {
            AC.WriteLine($"{sr}");
            Console.ReadKey();

            foreach (SearchResultItem sri in sr.AllResults)
            {
                AC.WriteLine($"{sri}");

            }
        }

        return 0;
    }
}