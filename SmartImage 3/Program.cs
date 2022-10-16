#nullable disable

global using static Kantan.Diagnostics.LogCategories;
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
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus;
using Novus.FileTypes;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using SmartImage.App;
using Terminal.Gui;
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

// # Notes
/*
 * ...
 */

public static class Program
{
	private static BaseProgramMode _main;

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

		Cache._oldMode = lpMode;

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

		// AC.Write(Gui.NameFiglet);

		main1:
		bool cli = args is { } && args.Any();

		_main = new GuiMode(args);

		object status;

		var run = _main.RunAsync(null);
		status = await run;

		if (status is bool { } and true) {
			_main.Dispose();
			goto main1;
		}
	}
}