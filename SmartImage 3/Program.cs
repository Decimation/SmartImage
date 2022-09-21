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
using static SmartImage.UI_Old.Gui;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus.Win32;
using SmartImage.CommandLine;
using SmartImage.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static class Program
{
	#region

	internal static readonly SearchConfig Config = new();

	internal static readonly SearchClient Client = new(Config);

	internal static SearchQuery Query { get; set; }

	//todo
	internal static List<SearchResult> Results { get; private set; }

	//todo
	internal static volatile bool Status = false;

	#endregion

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

			Cli.Cmd_Root.SetHandler(RootHandler, Cli.Opt_Query, Cli.Opt_Engines, Cli.Opt_Priority,
			                        Cli.Opt_OnTop);

			var parser = new CommandLineBuilder(Cli.Cmd_Root).UseDefaults().UseHelp(Cli.HelpHandler).Build();

			var r = await parser.InvokeAsync(args);

			if (r != 0 || Query == null) {
				return;
			}

		}

		else {
			/*var configuration = new ConfigurationBuilder();

			configuration.AddJsonFile("smartimage.json", optional: true, reloadOnChange: true)
			             .AddCommandLine(args);

			IConfigurationRoot configurationRoot = configuration.Build();
			configurationRoot.Bind(Config);*/

			/*Application.Init();
			Gui.Init();
			Application.Run();
			Application.Shutdown();*/

			var q  = AnsiConsole.Prompt(Gui.Prompt);
			var t2 = AnsiConsole.Prompt(Gui.Prompt2);
			var t3 = AnsiConsole.Prompt(Gui.Prompt3);
			var t4 = AnsiConsole.Prompt(Gui.Prompt4);

			SearchEngineOptions a = t2.Aggregate(SearchEngineOptions.None, EnumAggregator);
			SearchEngineOptions b = t3.Aggregate(SearchEngineOptions.None, EnumAggregator);

			await RootHandler(Query, a.ToString(), b.ToString(), t4);
		}

		await RunMain();
		Native.FlashWindow(_hndWindow);

	}

	private static async Task RunMain()
	{
		var table = new Table()
			{ };

		table.AddColumns(new TableColumn("Input"), new TableColumn("Value"))
		     .AddRow("[cyan]Search engines[/]", Config.SearchEngines.ToString())
		     .AddRow("[cyan]Priority engines[/]", Config.PriorityEngines.ToString())
		     .AddRow("[blue]Query value[/]", Query.Value)
		     .AddRow("[blue]Query upload[/]", Query.Upload.ToString());

		AnsiConsole.Write(table);

		var now = Stopwatch.StartNew();

		var live = AnsiConsole.Live(Gui.ResultsTable)
		                      .AutoClear(false)
		                      .Overflow(VerticalOverflow.Ellipsis)
		                      .Cropping(VerticalOverflowCropping.Top)
		                      .StartAsync(Gui.LiveCallback);

		Status  = false;
		Results = await Client.RunSearchAsync(Query, CancellationToken.None, Gui.SearchCallback);
		Status  = true;

		await live;

		now.Stop();
		var diff = now.Elapsed;
		AnsiConsole.WriteLine($"Completed in ~{diff.TotalSeconds:F}");
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

		RootHandler(Enum.Parse<SearchEngineOptions>(t2), Enum.Parse<SearchEngineOptions>(t3), t4);
	}

	private static void RootHandler(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{
		Config.SearchEngines   = t2;
		Config.PriorityEngines = t3;

		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(_hndWindow);
		}
	}

	private static readonly Func<SearchEngineOptions, SearchEngineOptions, SearchEngineOptions> EnumAggregator =
		(current, searchEngineOptions) => current | searchEngineOptions;

	private static readonly IntPtr _hndWindow = Native.GetConsoleWindow();
}