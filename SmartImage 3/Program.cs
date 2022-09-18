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
using static SmartImage.Gui;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static class Program
{
	private static readonly Option<SearchQuery> Opt_Query = new("-q", parseArgument: (ar) =>
	{
		return SearchQuery.TryCreateAsync(ar.Tokens.Single().Value).Result;

	}, isDefault: false, "Query (file or direct image URL)");

	private static readonly Option<string> Opt_Priority =
		new("-p", description: "Priority engines", getDefaultValue: () => SearchEngineOptions.All.ToString());

	private static readonly Option<string> Opt_Engines = new(
		"-e", description: $"Search engines\n{Enum.GetValues<SearchEngineOptions>().QuickJoin("\n")}",
		getDefaultValue: () => SearchEngineOptions.All.ToString());

	private static readonly RootCommand Cmd_Root = new("Run a search")
	{
		Opt_Query,
		Opt_Priority,
		Opt_Engines
	};

	#region

	internal static SearchConfig Config { get; private set; }

	internal static SearchClient Client { get; private set; }

	internal static SearchQuery Query { get; set; }

	//todo
	internal static CancellationTokenSource Cts { get; } = new();

	//todo
	internal static List<SearchResult> Results { get; } = new();

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

		Config = new SearchConfig();
		Client = new SearchClient(Config);

#if TEST
		// args = new String[] { null };
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "Artwork" };
#endif

		bool cli = args is { } && args.Any();

		if (cli) {

			Cmd_Root.SetHandler(async (t1, t2, t3) =>
			{
				Query = t1;
				Console.WriteLine($"Uploading {Query}...");
				await Query.UploadAsync();
				Console.WriteLine($"{Query.Upload}");

				Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(t2);
				Config.SearchEngines   = Enum.Parse<SearchEngineOptions>(t3);

			}, Opt_Query, Opt_Priority, Opt_Engines);

			var parser = new CommandLineBuilder(Cmd_Root).UseDefaults().UseHelp(async (ctx) =>
			{
				ctx.HelpBuilder.CustomizeLayout(
					_ =>
						HelpBuilder.Default.GetLayout()
						           .Skip(1) // Skip the default command description section.
						           .Prepend(
							           _ => AnsiConsole.Write(
								           new FigletText("SmartImage"))
						           ));
			}).Build();

			var r = await parser.InvokeAsync(args);

			if (r != 0 || Query == null) {
				return;
			}

			AnsiConsole.WriteLine(Config.ToString());
			AnsiConsole.WriteLine(Client.ToString());
			AnsiConsole.WriteLine(Query.ToString());

			var now = Stopwatch.StartNew();

			var results = await Client.RunSearchAsync(Query, CancellationToken.None, async (sender, result) =>
			{
				AnsiConsole.MarkupLine($"[green]{result.Engine.Name}[/] | [link={result.RawUrl}]Raw[/]");

				foreach (SearchResultItem item in result.Results) {
					// Console.WriteLine($"\t{item}");
					AnsiConsole.MarkupLine(
						$"\t[link={item.Url}]{item.Root.Engine.Name}[/] | {item.Similarity / 100:P} {item.Artist} " +
						$"{item.Description} [italic]{item.Title}[/] {item.Width}x{item.Height}");
				}
			});

			now.Stop();
			var diff = now.Elapsed;
			AnsiConsole.WriteLine($"Completed in ~{diff.TotalSeconds}");
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
}