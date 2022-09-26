#nullable disable

global using AC = Spectre.Console.AnsiConsole;
global using static Kantan.Diagnostics.LogCategories;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Hosting;
using SmartImage.Lib;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus.Win32;
using SmartImage.App;
using SmartImage.CommandLine;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;
using ConfigurationManager = System.Configuration.ConfigurationManager;

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

		// AC.Profile.Capabilities.Links   = true;
		// AC.Profile.Capabilities.Unicode = true;

#if TEST
		// args = new String[] { null };
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png" };
		// args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "Artwork", "-ontop" };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif

		bool cli = args is { } && args.Any();

		if (cli) {

			int r = await Cli.RunCli(args);

		}
		else {

			await Gui.RunGui();
		}

		await RunMain();
	}

	private static async Task RunMain()
	{
		//todo

		var table = new Table()
			{ };

		table.AddColumns(new TableColumn("Input"), new TableColumn("Value"))
		     .AddRow("[cyan]Search engines[/]", Config.SearchEngines.ToString())
		     .AddRow("[cyan]Priority engines[/]", Config.PriorityEngines.ToString())
		     .AddRow("[blue]Query value[/]", Query.Value)
		     .AddRow("[blue]Query upload[/]", Query.Upload.ToString());

		AC.Write(table);

		var now = Stopwatch.StartNew();

		var live = AC.Live(Gui.ResultsTable)
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
		AC.WriteLine($"Completed in ~{diff.TotalSeconds:F}");
		Native.FlashWindow(Cache.HndWindow);

		await Gui.AfterSearch();
	}

	internal static async Task RootHandler(SearchQuery t1, string t2, string t3, bool t4)
	{
		Query = t1;

		var t = AC.Status().Spinner(Spinner.Known.Star)
		          .StartAsync($"Uploading...", async ctx =>
		          {
			          await Query.UploadAsync();
			          ctx.Status = "Uploaded";
		          });

		await t;

		RootHandler(Enum.Parse<SearchEngineOptions>(t2), Enum.Parse<SearchEngineOptions>(t3), t4);
	}

	internal static void RootHandler(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{
		Config.Update();

		Config.SearchEngines   = t2;
		Config.PriorityEngines = t3;

		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(Cache.HndWindow);
		}
	}
}