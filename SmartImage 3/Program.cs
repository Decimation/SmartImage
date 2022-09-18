#nullable disable
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Kantan.Text;
using Microsoft.Extensions.Hosting;
using SmartImage.Lib;
using Terminal.Gui;
using static SmartImage.UI.Gui;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus.OS.Win32;
using SmartImage.UI;
using Spectre.Console;
using Spectre.Console.Rendering;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static class Program
{
	#region Console

	private static readonly Option<SearchQuery> Opt_Query = new("-q", parseArgument: (ar) =>
	{
		return SearchQuery.TryCreateAsync(ar.Tokens.Single().Value).Result;

	}, isDefault: false, "Query (file or direct image URL)");

	private static readonly Option<string> Opt_Priority =
		new("-p", description: "Priority engines", getDefaultValue: () => SearchConfig.PE_DEFAULT.ToString());

	private static readonly Option<string> Opt_Engines = new(
		"-e", description: $"Search engines\n{Enum.GetValues<SearchEngineOptions>().QuickJoin("\n")}",
		getDefaultValue: () => SearchConfig.SE_DEFAULT.ToString());

	public static readonly Option<bool> Opt_OnTop = new(name: "-ontop", description: "Stay on top");

	private static readonly RootCommand Cmd_Root = new("Run a search")
	{
		Opt_Query,
		Opt_Priority,
		Opt_Engines,
		Opt_OnTop
	};

	public static readonly Table tr = new();

	#endregion

	#region

	internal static readonly SearchConfig Config = new();

	internal static readonly SearchClient Client = new(Config);

	internal static SearchQuery Query { get; set; }

	//todo
	internal static List<SearchResult> Results { get; private set; }

	#endregion

	//todo

	[ModuleInitializer]
	public static void Init()
	{
		Trace.WriteLine("Init", Resources.Name);

		// Gui.Init();
	}

	public static async Task Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;
		Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

#if TEST
		// args = new String[] { null };
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "Artwork", "-ontop" };
#endif

		bool cli = args is { } && args.Any();

		if (cli) {

			Cmd_Root.SetHandler(RootHandler, Opt_Query, Opt_Priority, Opt_Engines, Opt_OnTop);

			var parser = new CommandLineBuilder(Cmd_Root).UseDefaults().UseHelp(HelpHandler).Build();

			var r = await parser.InvokeAsync(args);

			if (r != 0 || Query == null) {
				return;
			}

			var table = new Table()
				{ };

			table.AddColumns(new TableColumn("Input"), new TableColumn("Value"))
			     .AddRow("[cyan]Search engines[/]", Config.SearchEngines.ToString())
			     .AddRow("[cyan]Priority engines[/]", Config.PriorityEngines.ToString())
			     .AddRow("[blue]Query value[/]", Query.Value)
			     .AddRow("[blue]Query upload[/]", Query.Upload.ToString());

			AnsiConsole.Write(table);

			var now = Stopwatch.StartNew();

			var t = AnsiConsole.Live(tr)
			                   .AutoClear(false)
			                   .Overflow(VerticalOverflow.Ellipsis)
			                   .Cropping(VerticalOverflowCropping.Top)
			                   .StartAsync(Func);

			_b      = false;
			Results = await Client.RunSearchAsync(Query, CancellationToken.None, Callback);
			_b      = true;
			await t;

			now.Stop();
			var diff = now.Elapsed;
			AnsiConsole.WriteLine($"Completed in ~{diff.TotalSeconds:F}");
		}

		else {
			/*var configuration = new ConfigurationBuilder();

			configuration.AddJsonFile("smartimage.json", optional: true, reloadOnChange: true)
			             .AddCommandLine(args);

			IConfigurationRoot configurationRoot = configuration.Build();
			configurationRoot.Bind(Config);*/

			Application.Init();

			Gui.Init();

			Application.Run();
			Application.Shutdown();
		}

	}

	private static async Task Func(LiveDisplayContext ctx)
	{
		tr.AddColumns("Engine", "Raw", nameof(SearchResult.Results));

		while (!_b) {
			ctx.Refresh();
			await Task.Delay(TimeSpan.FromMilliseconds(100));
		}
	}

	private static volatile bool _b = false;

	private static async Task Callback(object sender, SearchResult result)
	{

		// AnsiConsole.MarkupLine($"[green]{result.Engine.Name}[/] | [link={result.RawUrl}]Raw[/]");

		var tx = new Table();

		tx.AddColumns(new TableColumn(nameof(SearchResultItem.Url)),
		              new TableColumn(nameof(SearchResultItem.Similarity)));

		foreach (SearchResultItem item in result.Results) {
			/*AnsiConsole.MarkupLine(
				$"\t[link={item.Url}]{item.Root.Engine.Name}[/] | {item.Similarity / 100:P} {item.Artist} " +
				$"{item.Description} [italic]{item.Title}[/] {item.Width}x{item.Height}");*/
			tx.AddRow(($"[link={item.Url}]Link[/]"), ($"{item.Similarity / 100:P}"));
		}

		var rawText  = new Text(result.RawUrl.ToString()) { Overflow = Overflow.Ellipsis };
		var nameText = new Text(result.Engine.Name);

		tr.AddRow(nameText, rawText, tx);

		return;
	}

	private static void HelpHandler(HelpContext ctx)
	{
		ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default.GetLayout()
		                                                .Skip(1) // Skip the default command description section.
		                                                .Prepend(_ => AnsiConsole.Write(
			                                                         new FigletText(Resources.Name))));
	}

	private static async Task RootHandler(SearchQuery t1, string t2, string t3, bool t4)
	{
		Query = t1;

		await AnsiConsole.Status()
		                 .Spinner(Spinner.Known.Star)
		                 .StartAsync($"Uploading {Query}...", async ctx =>
		                 {
			                 await Query.UploadAsync();
			                 ctx.Status = "Uploaded";
		                 });

		Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(t2);
		Config.SearchEngines   = Enum.Parse<SearchEngineOptions>(t3);

		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(Native.GetConsoleWindow());
		}
	}
}