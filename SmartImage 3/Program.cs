#nullable disable

global using static Kantan.Diagnostics.LogCategories;
using System.Diagnostics;
using System.Runtime.CompilerServices;
// using Windows.UI.Notifications;
// using CommunityToolkit.WinUI.Notifications;
using Novus;
using Terminal.Gui;
using SmartImage.Shell;
using SmartImage.App;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Notes
/*
 * ...
 */

public static class Program
{
	private static ShellMain _main;

	[ModuleInitializer]
	public static void Init()
	{
		Global.Setup();
		Trace.WriteLine("Init", Resources.Name);
		// Gui.Init();
		Console.Title = R2.Name;

		if (Compat.IsWin) {
			ConsoleUtil.SetConsoleMode();
		}

		Application.Init();

		AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
		{
			Trace.WriteLine($"Exiting", R2.Name);
		};
	}

	public static async Task Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;

		// ToastNotificationManagerCompat.OnActivated += AppNotification.OnActivated;

#if TEST
		// args = new String[] { null };
		args = new[] { R2.Arg_Input, "https://i.imgur.com/QtCausw.png",R2.Arg_AutoSearch };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif
		bool cli = args is { } && args.Any();

		_main = new ShellMain(args);

		main1:

		object status;

		var run = _main.RunAsync(null);
		status = await run;

		if (status is bool { } and true) {
			_main.Dispose();
			goto main1;
		}
	}
}