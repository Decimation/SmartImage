#nullable disable

global using AC = Spectre.Console.AnsiConsole;
global using static Kantan.Diagnostics.LogCategories;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Kantan.Console;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Hosting;
using SmartImage.Lib;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus;
using Novus.FileTypes;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using SmartImage.App;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;
using ConfigurationManager = System.Configuration.ConfigurationManager;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static partial class Program
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
		Global.Setup();
		Trace.WriteLine("Init", Resources.Name);

		// Gui.Init();
	}

	public static async Task Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;
		Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
		Native.GetConsoleMode(Cache.StdIn, out ConsoleModes lpMode);

		Cache.OldMode = lpMode;

		Native.SetConsoleMode(Cache.StdIn, lpMode | ((ConsoleModes.ENABLE_MOUSE_INPUT &
		                                              ~ConsoleModes.ENABLE_QUICK_EDIT_MODE) |
		                                             ConsoleModes.ENABLE_EXTENDED_FLAGS |
		                                             ConsoleModes.ENABLE_ECHO_INPUT |
		                                             ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING));

#if TEST
		// args = new String[] { null };
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png" };
		// args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "Artwork", "-ontop" };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif
		/*Native.OpenClipboard();

		var c = (string) Native.GetClipboard((uint?) ClipboardFormat.CF_UNICODETEXT);

		try {
			var task = await QFileHandle.GetInfoAsync(c, true);

			if (task.IsFile || task.IsUri) {
				Cache.Clipboard = task;

				Query = new(Cache.Clipboard.Value, Cache.Clipboard.Stream);

			}
		}
		catch (Exception e) { }*/

		AC.Write(Gui.NameFiglet);

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
		{
			Border    = TableBorder.Heavy,
			Alignment = Justify.Center
		};

		//NOTE: WTF
		table.AddColumns(new TableColumn("Input".T()), new TableColumn("Value".T()))
		     .AddRow(new Text(Resources.S_SearchEngines, Gui.S_Generic1),
		             new Text(Config.SearchEngines.ToString(), Gui.S_Generic2))
		     .AddRow(new Text(Resources.S_PriorityEngines, Gui.S_Generic1),
		             new Text(Config.PriorityEngines.ToString(), Gui.S_Generic2))
		     .AddRow(new Text(Resources.S_OnTop, Gui.S_Generic1), new Text(Config.OnTop.ToString(), Gui.S_Generic2))
		     .AddRow(new Text("Query input", Gui.S_Generic1), new Text(Query.Value, Gui.S_Generic2))
		     .AddRow(new Text("Query upload", Gui.S_Generic1), new Text(Query.Upload.ToString(), Gui.S_Generic2));

		AC.Write(table);

		var now = Stopwatch.StartNew();

		var live = AC.Live(Gui.Tb_Results)
		             .AutoClear(false)
		             .Overflow(VerticalOverflow.Ellipsis)
		             .Cropping(VerticalOverflowCropping.Top)
		             .StartAsync(Gui.LiveCallback);

		Client.OnResult += Gui.OnResultCallback;

		Client.OnComplete += async (sender, list) =>
		{
			Native.FlashWindow(Cache.HndWindow);

			return;
		};

		Status  = false;
		Results = await Client.RunSearchAsync(Query, CancellationToken.None);
		Status  = true;

		await live;

		now.Stop();

		var diff = now.Elapsed;

		AC.WriteLine($"Completed in ~{diff.TotalSeconds:F}");

		await Gui.AfterSearchCallback();
	}

	private static async Task RootHandler(SearchQuery t1, string t2, string t3, bool t4)
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

	private static void RootHandler(SearchEngineOptions t2, SearchEngineOptions t3, bool t4)
	{

		Config.SearchEngines   = t2;
		Config.PriorityEngines = t3;

		Config.OnTop = t4;

		if (Config.OnTop) {
			Native.KeepWindowOnTop(Cache.HndWindow);
		}
	}
}