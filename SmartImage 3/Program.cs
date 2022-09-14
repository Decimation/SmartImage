#nullable disable
using System.CommandLine;
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

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static class Program
{
	private static readonly Option<SearchQuery> Opt_Query = new("-q", parseArgument: (x) =>
	{
		return SearchQuery.TryCreateAsync(x.Tokens.Single().Value).Result;

	}, isDefault: false, "Query (file or direct image URL)");

	private static readonly Option<string> Opt_Priority =
		new("-p", description: "Priority engines", 
		    getDefaultValue: () => SearchEngineOptions.All.ToString());

	private static readonly Option<string> Opt_Engines =
		new("-e", description: $"Search engines\n{Enum.GetValues<SearchEngineOptions>().QuickJoin("\n")}",
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
		Console.OutputEncoding = Encoding.Unicode;

		Config = new SearchConfig();
		Client = new SearchClient(Config);

#if TEST
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "All" };
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

			var r = await Cmd_Root.InvokeAsync(args);

			if (r!=0) {
				return;
			}
			Console.WriteLine(Config);
			Console.WriteLine(Client);
			Console.WriteLine(Query);

			var now = Stopwatch.StartNew();
			var results = await Client.RunSearchAsync(Query, CancellationToken.None, async (sender, result) =>
			{
				Console.WriteLine($">> {result}");

				foreach (SearchResultItem item in result.Results) {
					Console.WriteLine($"\t{item}");
				}
			});
			now.Stop();
			var diff = now.Elapsed;
			Console.WriteLine($"Completed in ~{diff.TotalSeconds}");
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