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
using Terminal.Gui;
using Color = Spectre.Console.Color;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using SmartImage.Modes;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Test values
/*
 *
 * https://i.imgur.com/QtCausw.png
 */

public static partial class Program
{
	#region

	//todo
	private static List<SearchResult> _results;

	private static BaseProgramMode _main;

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

		/*Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
		Native.GetConsoleMode(Cache.StdIn, out ConsoleModes lpMode);

		Cache.OldMode = lpMode;

		Native.SetConsoleMode(Cache.StdIn, lpMode | ((ConsoleModes.ENABLE_MOUSE_INPUT &
		                                              ~ConsoleModes.ENABLE_QUICK_EDIT_MODE) |
		                                             ConsoleModes.ENABLE_EXTENDED_FLAGS |
		                                             ConsoleModes.ENABLE_ECHO_INPUT |
		                                             ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING));*/

#if TEST
		// args = new String[] { null };
		args = new[] { "-q", "https://i.imgur.com/QtCausw.png" };
		// args = new[] { "-q", "https://i.imgur.com/QtCausw.png", "-p", "Artwork", "-ontop" };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif

		// AC.Write(Gui.NameFiglet);

		bool cli = args is { } && args.Any();

		_main = cli ? new CliMode() : new Gui2Mode();

		Task ret;

		//todo

		var now = Stopwatch.StartNew();

		var pre = _main.PreSearchAsync(now);

		_main.Client.OnResult += _main.OnResult;

		_main.Client.OnComplete += _main.OnComplete;

		await pre;

		_main.Status = 0;
		
		var run = _main.RunAsync(args);
		_main.IsReady.WaitOne();
		_results = await _main.Client.RunSearchAsync(_main.Query, CancellationToken.None);

		_main.Status = 1;

		var post = _main.PostSearchAsync(now, _results);

		await post;

		// await run;

		Application.Run();

	}
}