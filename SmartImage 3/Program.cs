#nullable disable

global using static Kantan.Diagnostics.LogCategories;
using Console = Spectre.Console.AnsiConsole;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
// using Windows.UI.Notifications;
// using CommunityToolkit.WinUI.Notifications;
using Novus;
using Terminal.Gui;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using SmartImage.Utilities;
using Spectre.Console;
using Command = System.CommandLine.Command;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Notes
/*
 * ...
 */

public static class Program
{

	[ModuleInitializer]
	public static void Init()
	{
		Global.Setup();
		Trace.WriteLine("Init", Resources.Name);
		// Gui.Init();
		System.Console.Title = R2.Name;

		if (Compat.IsWin) {
			ConsoleUtil.SetConsoleMode();
		}

		AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
		{
			Trace.WriteLine($"Exiting", R2.Name);
		};
	}

	public static async Task<int> Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;

		// ToastNotificationManagerCompat.OnActivated += AppNotification.OnActivated;

#if TEST
		// args = new String[] { null };
		// args = new[] { R2.Arg_Input, "https://i.imgur.com/QtCausw.png",R2.Arg_AutoSearch };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse

		Debug.WriteLine($"TEST");
#endif

		bool cli = args is { } && args.Any();

		if (cli && args.Contains(Resources.Arg_NoUI)) {
			
			var main = new CliMain();

			var rc = new RootCommand()
				{ };

			var arg = new Option<string>(Resources.Arg_Input)
				{ };

			var opt2 = new Option<bool>(Resources.Arg_NoUI)
			{
				Arity = ArgumentArity.Zero,

			};

			rc.AddOption(arg);
			rc.AddOption(opt2);

			rc.SetHandler(CliMain.RunCliAsync, arg);

			var i = await rc.InvokeAsync(args);

			return i;
		}
		else {
			main1:
			Application.Init();

			var main = new ShellMain(args);
			object status;

			var run = main.RunAsync(null);
			status = await run;

			if (status is bool { } and true) {
				main.Dispose();
				goto main1;
			}

			return 0;
		}
	}
}