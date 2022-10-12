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

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static partial class Program
{
	#region

	private static readonly SearchConfig Config = new();

	private static readonly SearchClient Client = new(Config);

	//todo
	private static List<SearchResult> _results;

	//todo
	private static volatile bool _status = false;

	private static ProgramMode _prgm;

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

		_prgm = cli ? new Cli() : new Gui2();

		Task ret;
		//todo

		var now = Stopwatch.StartNew();

		var pre = _prgm.PreSearch(Config, now);

		Client.OnResult += _prgm.OnResult;

		Client.OnComplete += _prgm.OnComplete;

		await pre;

		_prgm.Status = false;

		var run = _prgm.Run(Client, args);

		_results = await Client.RunSearchAsync(_prgm.Query, CancellationToken.None);

		_prgm.Status  = true;
		
		await run;

		var post = _prgm.PostSearch(Config, now, _results);

		await post;
	}
}